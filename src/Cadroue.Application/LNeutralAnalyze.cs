using Cadroue.Core;

namespace Cadroue.Application;

public static partial class LNeutral
{
    // Estimate where an automatic method's neutral point falls in the current frame,
    // for the display-only wheel dot. Average = per-channel mean, Median = per-channel
    // median, Minmax = per-channel midpoint. The actual export correction is computed
    // by ffmpeg's colorcorrect; this only previews the resulting cast direction.
    public static LNeutralWheel LNeutralAnalyzeResolve(
        IReadOnlyList<byte> lNeutralPixels,
        int lNeutralWidth,
        int lNeutralHeight,
        LWhitebalanceMethod lNeutralMethod)
    {
        if (lNeutralPixels is null
            || lNeutralWidth <= 0
            || lNeutralHeight <= 0
            || lNeutralPixels.Count < lNeutralWidth * lNeutralHeight * 4)
        {
            return new LNeutralWheel(0, 0, false);
        }

        List<int> lNeutralRedList = new();
        List<int> lNeutralGreenList = new();
        List<int> lNeutralBlueList = new();
        for (int lNeutralIndex = 0; lNeutralIndex + 3 < lNeutralPixels.Count; lNeutralIndex += 4)
        {
            if (lNeutralPixels[lNeutralIndex + 3] == 0)
            {
                continue;
            }

            lNeutralRedList.Add(lNeutralPixels[lNeutralIndex]);
            lNeutralGreenList.Add(lNeutralPixels[lNeutralIndex + 1]);
            lNeutralBlueList.Add(lNeutralPixels[lNeutralIndex + 2]);
        }

        if (lNeutralRedList.Count == 0)
        {
            return new LNeutralWheel(0, 0, false);
        }

        (int lNeutralRed, int lNeutralGreen, int lNeutralBlue) = lNeutralMethod switch
        {
            LWhitebalanceMethod.LWhitebalanceMethodAverage => (
                LNeutralMeanResolve(lNeutralRedList),
                LNeutralMeanResolve(lNeutralGreenList),
                LNeutralMeanResolve(lNeutralBlueList)),
            LWhitebalanceMethod.LWhitebalanceMethodMinmax => (
                LNeutralMidResolve(lNeutralRedList),
                LNeutralMidResolve(lNeutralGreenList),
                LNeutralMidResolve(lNeutralBlueList)),
            _ => (
                LNeutralMedianResolve(lNeutralRedList),
                LNeutralMedianResolve(lNeutralGreenList),
                LNeutralMedianResolve(lNeutralBlueList))
        };

        return LNeutralWheelResolve(lNeutralRed, lNeutralGreen, lNeutralBlue);
    }

    private static int LNeutralMeanResolve(List<int> lNeutralValues)
    {
        long lNeutralSum = 0;
        foreach (int lNeutralValue in lNeutralValues)
        {
            lNeutralSum += lNeutralValue;
        }

        return (int)Math.Round(lNeutralSum / (double)lNeutralValues.Count);
    }

    private static int LNeutralMidResolve(List<int> lNeutralValues)
    {
        int lNeutralLow = 255;
        int lNeutralHigh = 0;
        foreach (int lNeutralValue in lNeutralValues)
        {
            if (lNeutralValue < lNeutralLow)
            {
                lNeutralLow = lNeutralValue;
            }

            if (lNeutralValue > lNeutralHigh)
            {
                lNeutralHigh = lNeutralValue;
            }
        }

        return (int)Math.Round((lNeutralLow + lNeutralHigh) / 2.0);
    }
}
