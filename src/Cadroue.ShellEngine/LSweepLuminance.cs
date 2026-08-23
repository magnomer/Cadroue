using Cadroue.Application;

namespace Cadroue.ShellEngine;

public readonly record struct LSweepSample(TimeSpan LSweepSampleTime, double LSweepSampleLuma);

public static partial class LSweep
{
    public static string LSweepLuminanceFormat(string lSweepSource)
    {
        const string lSweepFilter = "format=yuv420p,signalstats,metadata=print:key=lavfi.signalstats.YAVG";
        return $"-hide_banner -stats -i {LEncode.LEncodeFormat(lSweepSource)} -map 0:v:0 -vf {LEncode.LEncodeFormat(lSweepFilter)} -an -f null -";
    }

    public static IReadOnlyList<LSweepSample> LSweepLuminanceParse(IEnumerable<string> lSweepLines)
    {
        var lSweepSamples = new List<LSweepSample>();
        double? lSweepTime = null;
        foreach (string lSweepLine in lSweepLines)
        {
            if (lSweepLine is null)
            {
                continue;
            }

            double? lSweepAt = LSweepFieldRead(lSweepLine, "pts_time:");
            if (lSweepAt is { } lSweepPts)
            {
                lSweepTime = lSweepPts;
                continue;
            }

            double? lSweepLuma = LSweepFieldRead(lSweepLine, "lavfi.signalstats.YAVG=");
            if (lSweepLuma is { } lSweepValue && lSweepTime is { } lSweepStamp)
            {
                lSweepSamples.Add(new LSweepSample(TimeSpan.FromSeconds(lSweepStamp), lSweepValue));
            }
        }

        return lSweepSamples;
    }

    public static IReadOnlyList<TimeSpan> LSweepLuminanceResolve(IReadOnlyList<LSweepSample> lSweepSamples, double lSweepWindow, double lSweepThreshold)
    {
        var lSweepBoundaries = new List<TimeSpan>();
        if (lSweepSamples is null || lSweepSamples.Count < 2)
        {
            return lSweepBoundaries;
        }

        double lSweepSpan = lSweepWindow > 0 ? lSweepWindow : double.Epsilon;
        double lSweepThresholdUnits = lSweepThreshold / 100.0 * 255.0;
        const double lSweepFloor = 1e-6;

        int lSweepTotal = lSweepSamples.Count;
        var lSweepTimes = new double[lSweepTotal];
        var lSweepPrefix = new double[lSweepTotal + 1];
        for (int lSweepIndex = 0; lSweepIndex < lSweepTotal; lSweepIndex++)
        {
            lSweepTimes[lSweepIndex] = lSweepSamples[lSweepIndex].LSweepSampleTime.TotalSeconds;
            lSweepPrefix[lSweepIndex + 1] = lSweepPrefix[lSweepIndex] + lSweepSamples[lSweepIndex].LSweepSampleLuma;
        }

        var lSweepDiffs = new double?[lSweepTotal];
        for (int lSweepIndex = 0; lSweepIndex < lSweepTotal; lSweepIndex++)
        {
            double lSweepAt = lSweepTimes[lSweepIndex];
            double? lSweepBefore = LSweepMeanResolve(lSweepTimes, lSweepPrefix, lSweepAt - lSweepSpan, lSweepAt);
            double? lSweepAfter = LSweepMeanResolve(lSweepTimes, lSweepPrefix, lSweepAt, lSweepAt + lSweepSpan);
            if (lSweepBefore is { } lSweepLeft && lSweepAfter is { } lSweepRight)
            {
                lSweepDiffs[lSweepIndex] = Math.Abs(lSweepRight - lSweepLeft);
            }
        }

        int lSweepBest = -1;
        for (int lSweepIndex = 0; lSweepIndex < lSweepSamples.Count; lSweepIndex++)
        {
            bool lSweepQualify = lSweepDiffs[lSweepIndex] is { } lSweepDiff && lSweepDiff > lSweepFloor && lSweepDiff >= lSweepThresholdUnits;
            if (lSweepQualify)
            {
                if (lSweepBest < 0 || lSweepDiffs[lSweepIndex] > lSweepDiffs[lSweepBest])
                {
                    lSweepBest = lSweepIndex;
                }
                continue;
            }

            if (lSweepBest >= 0)
            {
                lSweepBoundaries.Add(lSweepSamples[lSweepBest].LSweepSampleTime);
                lSweepBest = -1;
            }
        }

        if (lSweepBest >= 0)
        {
            lSweepBoundaries.Add(lSweepSamples[lSweepBest].LSweepSampleTime);
        }

        return lSweepBoundaries;
    }

    private static double? LSweepMeanResolve(double[] lSweepTimes, double[] lSweepPrefix, double lSweepLow, double lSweepHigh)
    {
        int lSweepFrom = LSweepBoundFind(lSweepTimes, lSweepLow);
        int lSweepTo = LSweepBoundFind(lSweepTimes, lSweepHigh);
        int lSweepCount = lSweepTo - lSweepFrom;
        if (lSweepCount <= 0)
        {
            return null;
        }

        double lSweepSum = lSweepPrefix[lSweepTo] - lSweepPrefix[lSweepFrom];
        return lSweepSum / lSweepCount;
    }

    private static int LSweepBoundFind(double[] lSweepTimes, double lSweepValue)
    {
        int lSweepLow = 0;
        int lSweepHigh = lSweepTimes.Length;
        while (lSweepLow < lSweepHigh)
        {
            int lSweepMid = lSweepLow + (lSweepHigh - lSweepLow) / 2;
            if (lSweepTimes[lSweepMid] < lSweepValue)
            {
                lSweepLow = lSweepMid + 1;
            }
            else
            {
                lSweepHigh = lSweepMid;
            }
        }

        return lSweepLow;
    }
}
