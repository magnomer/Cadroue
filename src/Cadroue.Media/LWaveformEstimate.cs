namespace Cadroue.Media;

public static class LWaveformEstimate
{
    private const double LWaveformDynamicCeiling = 0.95;

    public static double[] LWaveformEnvelopeRead(byte[] lWaveformPeaks)
    {
        if (lWaveformPeaks.Length == 0)
        {
            return Array.Empty<double>();
        }

        var lWaveformEnvelope = new double[lWaveformPeaks.Length];
        for (int lWaveformIndex = 0; lWaveformIndex < lWaveformPeaks.Length; lWaveformIndex++)
        {
            lWaveformEnvelope[lWaveformIndex] = lWaveformPeaks[lWaveformIndex] / (double)LWaveform.LWaveformPeakMaximum;
        }

        return lWaveformEnvelope;
    }

    public static double[] LWaveformGainApply(double[] lWaveformEnvelope, double lWaveformFactor)
    {
        var lWaveformResult = new double[lWaveformEnvelope.Length];
        for (int lWaveformIndex = 0; lWaveformIndex < lWaveformEnvelope.Length; lWaveformIndex++)
        {
            lWaveformResult[lWaveformIndex] = Math.Min(1.0, lWaveformEnvelope[lWaveformIndex] * lWaveformFactor);
        }

        return lWaveformResult;
    }

    public static double LWaveformLoudnessRead(double[] lWaveformEnvelope)
    {
        if (lWaveformEnvelope.Length == 0)
        {
            return double.NegativeInfinity;
        }

        double lWaveformSquares = 0;
        foreach (double lWaveformSample in lWaveformEnvelope)
        {
            lWaveformSquares += lWaveformSample * lWaveformSample;
        }

        double lWaveformRms = Math.Sqrt(lWaveformSquares / lWaveformEnvelope.Length);
        return 20.0 * Math.Log10(Math.Max(lWaveformRms, 1e-6));
    }

    public static double[] LWaveformLoudnessApply(
        double[] lWaveformPeakEnvelope, double[] lWaveformRmsEnvelope,
        double lWaveformTargetLufs, double lWaveformPeakDbtp,
        double lWaveformRange, bool lWaveformTwoPass)
    {
        if (lWaveformPeakEnvelope.Length == 0)
        {
            return Array.Empty<double>();
        }

        bool lWaveformHasRms = lWaveformRmsEnvelope.Length == lWaveformPeakEnvelope.Length;
        double[] lWaveformLoud = lWaveformHasRms ? lWaveformRmsEnvelope : lWaveformPeakEnvelope;
        double lWaveformMeasured = LWaveformLoudnessRead(lWaveformLoud);

        double[] lWaveformWork = lWaveformPeakEnvelope;
        if (lWaveformTwoPass && lWaveformRange > 0 && lWaveformHasRms)
        {
            lWaveformWork = LWaveformRangeApply(lWaveformPeakEnvelope, lWaveformRmsEnvelope, lWaveformRange, lWaveformMeasured);
        }

        double lWaveformGain = Math.Pow(10.0, (lWaveformTargetLufs - lWaveformMeasured) / 20.0);
        double lWaveformCeiling = Math.Pow(10.0, lWaveformPeakDbtp / 20.0);
        double lWaveformMost = 0;
        foreach (double lWaveformSample in lWaveformWork)
        {
            if (lWaveformSample > lWaveformMost)
            {
                lWaveformMost = lWaveformSample;
            }
        }

        if (lWaveformMost * lWaveformGain > lWaveformCeiling && lWaveformMost > 0)
        {
            lWaveformGain = lWaveformCeiling / lWaveformMost;
        }

        return LWaveformGainApply(lWaveformWork, lWaveformGain);
    }

    public static double[] LWaveformDynamicApply(
        double[] lWaveformEnvelope,
        double lWaveformFrameMs, double lWaveformGauss,
        double lWaveformMaxGain, double lWaveformCompress)
    {
        if (lWaveformEnvelope.Length == 0)
        {
            return Array.Empty<double>();
        }

        int lWaveformWindow = Math.Max(1, (int)Math.Round(lWaveformFrameMs / LWaveform.LWaveformBucketMilliseconds));
        var lWaveformGains = new double[lWaveformEnvelope.Length];
        for (int lWaveformIndex = 0; lWaveformIndex < lWaveformEnvelope.Length; lWaveformIndex++)
        {
            double lWaveformLocal = LWaveformWindowRead(lWaveformEnvelope, lWaveformIndex, lWaveformWindow);
            lWaveformGains[lWaveformIndex] = lWaveformLocal > 1e-6
                ? Math.Min(lWaveformMaxGain, LWaveformDynamicCeiling / lWaveformLocal)
                : lWaveformMaxGain;
        }

        double[] lWaveformSmoothed = LWaveformGaussApply(lWaveformGains, lWaveformGauss);
        var lWaveformResult = new double[lWaveformEnvelope.Length];
        double lWaveformKnee = 1.0 - lWaveformCompress / 60.0;
        for (int lWaveformIndex = 0; lWaveformIndex < lWaveformEnvelope.Length; lWaveformIndex++)
        {
            double lWaveformValue = Math.Min(1.0, lWaveformEnvelope[lWaveformIndex] * lWaveformSmoothed[lWaveformIndex]);
            lWaveformResult[lWaveformIndex] = LWaveformKneeApply(lWaveformValue, lWaveformKnee);
        }

        return lWaveformResult;
    }

    private static double[] LWaveformRangeApply(
        double[] lWaveformPeakEnvelope, double[] lWaveformRmsEnvelope, double lWaveformRange, double lWaveformPivot)
    {
        var lWaveformLevels = new List<double>(lWaveformRmsEnvelope.Length);
        foreach (double lWaveformSample in lWaveformRmsEnvelope)
        {
            if (lWaveformSample > 1e-4)
            {
                lWaveformLevels.Add(20.0 * Math.Log10(lWaveformSample));
            }
        }

        if (lWaveformLevels.Count < 2)
        {
            return lWaveformPeakEnvelope;
        }

        lWaveformLevels.Sort();
        double lWaveformLow = LWaveformPercentileRead(lWaveformLevels, 0.10);
        double lWaveformHigh = LWaveformPercentileRead(lWaveformLevels, 0.95);
        double lWaveformCurrent = lWaveformHigh - lWaveformLow;
        if (lWaveformCurrent <= lWaveformRange)
        {
            return lWaveformPeakEnvelope;
        }

        double lWaveformFactor = lWaveformRange / lWaveformCurrent;
        var lWaveformResult = new double[lWaveformPeakEnvelope.Length];
        for (int lWaveformIndex = 0; lWaveformIndex < lWaveformPeakEnvelope.Length; lWaveformIndex++)
        {
            double lWaveformRms = lWaveformRmsEnvelope[lWaveformIndex];
            if (lWaveformRms <= 1e-4)
            {
                lWaveformResult[lWaveformIndex] = lWaveformPeakEnvelope[lWaveformIndex];
                continue;
            }

            double lWaveformRmsDb = 20.0 * Math.Log10(lWaveformRms);
            double lWaveformShaped = lWaveformPivot + (lWaveformRmsDb - lWaveformPivot) * lWaveformFactor;
            double lWaveformBucketGain = Math.Pow(10.0, (lWaveformShaped - lWaveformRmsDb) / 20.0);
            lWaveformResult[lWaveformIndex] = Math.Min(1.0, lWaveformPeakEnvelope[lWaveformIndex] * lWaveformBucketGain);
        }

        return lWaveformResult;
    }

    private static double LWaveformPercentileRead(List<double> lWaveformSorted, double lWaveformFraction)
    {
        int lWaveformIndex = Math.Clamp(
            (int)Math.Round(lWaveformFraction * (lWaveformSorted.Count - 1)), 0, lWaveformSorted.Count - 1);
        return lWaveformSorted[lWaveformIndex];
    }

    private static double LWaveformWindowRead(double[] lWaveformEnvelope, int lWaveformCenter, int lWaveformWindow)
    {
        int lWaveformHalf = lWaveformWindow / 2;
        int lWaveformFrom = Math.Max(0, lWaveformCenter - lWaveformHalf);
        int lWaveformTo = Math.Min(lWaveformEnvelope.Length - 1, lWaveformCenter + lWaveformHalf);
        double lWaveformPeak = 0;
        for (int lWaveformIndex = lWaveformFrom; lWaveformIndex <= lWaveformTo; lWaveformIndex++)
        {
            if (lWaveformEnvelope[lWaveformIndex] > lWaveformPeak)
            {
                lWaveformPeak = lWaveformEnvelope[lWaveformIndex];
            }
        }

        return lWaveformPeak;
    }

    private static double[] LWaveformGaussApply(double[] lWaveformGains, double lWaveformGauss)
    {
        int lWaveformRadius = Math.Max(1, (int)Math.Round(lWaveformGauss) / 2);
        double lWaveformSigma = Math.Max(1.0, lWaveformRadius / 2.0);
        var lWaveformKernel = new double[lWaveformRadius * 2 + 1];
        double lWaveformSum = 0;
        for (int lWaveformOffset = -lWaveformRadius; lWaveformOffset <= lWaveformRadius; lWaveformOffset++)
        {
            double lWaveformWeight = Math.Exp(-(lWaveformOffset * lWaveformOffset) / (2.0 * lWaveformSigma * lWaveformSigma));
            lWaveformKernel[lWaveformOffset + lWaveformRadius] = lWaveformWeight;
            lWaveformSum += lWaveformWeight;
        }

        var lWaveformResult = new double[lWaveformGains.Length];
        for (int lWaveformIndex = 0; lWaveformIndex < lWaveformGains.Length; lWaveformIndex++)
        {
            double lWaveformAccum = 0;
            for (int lWaveformOffset = -lWaveformRadius; lWaveformOffset <= lWaveformRadius; lWaveformOffset++)
            {
                int lWaveformPick = Math.Clamp(lWaveformIndex + lWaveformOffset, 0, lWaveformGains.Length - 1);
                lWaveformAccum += lWaveformGains[lWaveformPick] * lWaveformKernel[lWaveformOffset + lWaveformRadius];
            }

            lWaveformResult[lWaveformIndex] = lWaveformAccum / lWaveformSum;
        }

        return lWaveformResult;
    }

    private static double LWaveformKneeApply(double lWaveformValue, double lWaveformKnee)
    {
        if (lWaveformKnee >= 1.0 || lWaveformValue <= lWaveformKnee)
        {
            return lWaveformValue;
        }

        double lWaveformOver = lWaveformValue - lWaveformKnee;
        double lWaveformRoom = 1.0 - lWaveformKnee;
        return lWaveformKnee + lWaveformRoom * Math.Tanh(lWaveformOver / lWaveformRoom);
    }
}
