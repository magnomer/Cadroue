using Cadroue.Application;

namespace Cadroue.ShellEngine;

public readonly record struct LSweepSample(TimeSpan LSweepSampleTime, double LSweepSampleLuma);

public static partial class LSweep
{
    public static string LSweepLuminanceFormat(string lSweepSource)
    {
        const string lSweepFilter = "signalstats,metadata=print:key=lavfi.signalstats.YAVG";
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

        var lSweepDiffs = new double?[lSweepSamples.Count];
        for (int lSweepIndex = 0; lSweepIndex < lSweepSamples.Count; lSweepIndex++)
        {
            double lSweepAt = lSweepSamples[lSweepIndex].LSweepSampleTime.TotalSeconds;
            double? lSweepBefore = LSweepMeanResolve(lSweepSamples, lSweepAt - lSweepSpan, lSweepAt);
            double? lSweepAfter = LSweepMeanResolve(lSweepSamples, lSweepAt, lSweepAt + lSweepSpan);
            if (lSweepBefore is { } lSweepLeft && lSweepAfter is { } lSweepRight)
            {
                lSweepDiffs[lSweepIndex] = Math.Abs(lSweepRight - lSweepLeft);
            }
        }

        int lSweepBest = -1;
        for (int lSweepIndex = 0; lSweepIndex < lSweepSamples.Count; lSweepIndex++)
        {
            bool lSweepQualify = lSweepDiffs[lSweepIndex] is { } lSweepDiff && lSweepDiff >= lSweepThresholdUnits;
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

    private static double? LSweepMeanResolve(IReadOnlyList<LSweepSample> lSweepSamples, double lSweepLow, double lSweepHigh)
    {
        double lSweepSum = 0;
        int lSweepCount = 0;
        foreach (LSweepSample lSweepSample in lSweepSamples)
        {
            double lSweepAt = lSweepSample.LSweepSampleTime.TotalSeconds;
            if (lSweepAt >= lSweepLow && lSweepAt < lSweepHigh)
            {
                lSweepSum += lSweepSample.LSweepSampleLuma;
                lSweepCount++;
            }
        }

        return lSweepCount > 0 ? lSweepSum / lSweepCount : null;
    }
}
