using Cadroue.Core;

namespace Cadroue.Application;

// A whole frame's visible pixels reduced, by the chosen method, to one point on the neutral wheel.
public static class LNeutralAnalysis
{
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
                LNeutralChannel.LNeutralMeanResolve(lNeutralRedList),
                LNeutralChannel.LNeutralMeanResolve(lNeutralGreenList),
                LNeutralChannel.LNeutralMeanResolve(lNeutralBlueList)),
            LWhitebalanceMethod.LWhitebalanceMethodMinmax => (
                LNeutralChannel.LNeutralMidResolve(lNeutralRedList),
                LNeutralChannel.LNeutralMidResolve(lNeutralGreenList),
                LNeutralChannel.LNeutralMidResolve(lNeutralBlueList)),
            _ => (
                LNeutralChannel.LNeutralMedianResolve(lNeutralRedList),
                LNeutralChannel.LNeutralMedianResolve(lNeutralGreenList),
                LNeutralChannel.LNeutralMedianResolve(lNeutralBlueList))
        };

        return LNeutralColor.LNeutralWheelResolve(lNeutralRed, lNeutralGreen, lNeutralBlue);
    }
}
