namespace Cadroue.ShellEngine;

public readonly record struct LSweepSample(TimeSpan LSweepSampleTime, double LSweepSampleLuma);

public static class LSweepDelta
{
    public static IReadOnlyList<TimeSpan> LSweepBoundaryResolve(
        IReadOnlyList<LSweepSample> lSweepSamples,
        double lSweepWindow,
        double lSweepThresholdUnits)
    {
        var lSweepBoundaries = new List<TimeSpan>();
        if (lSweepSamples is null || lSweepSamples.Count < 2)
        {
            return lSweepBoundaries;
        }

        double lSweepSpan = lSweepWindow > 0 ? lSweepWindow : double.Epsilon;
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
            bool lSweepQualify = lSweepDiffs[lSweepIndex] is { } lSweepDiff
                && lSweepDiff > lSweepFloor
                && lSweepDiff >= lSweepThresholdUnits;
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

    public static IReadOnlyList<TimeSpan> LSweepMinimumResolve(
        IReadOnlyList<TimeSpan> lSweepBoundaries,
        double lSweepMinimum)
    {
        if (lSweepMinimum <= 0 || lSweepBoundaries.Count == 0)
        {
            return lSweepBoundaries;
        }

        var lSweepKept = new List<TimeSpan>();
        TimeSpan lSweepGap = TimeSpan.FromSeconds(lSweepMinimum);
        TimeSpan? lSweepLast = null;
        foreach (TimeSpan lSweepBoundary in lSweepBoundaries)
        {
            if (lSweepLast is { } lSweepPrevious && lSweepBoundary - lSweepPrevious < lSweepGap)
            {
                continue;
            }

            lSweepKept.Add(lSweepBoundary);
            lSweepLast = lSweepBoundary;
        }

        return lSweepKept;
    }

    private static double? LSweepMeanResolve(
        double[] lSweepTimes,
        double[] lSweepPrefix,
        double lSweepLow,
        double lSweepHigh)
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
