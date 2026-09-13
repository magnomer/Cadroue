namespace Cadroue.Application;

public static partial class LNeutral
{
    private const int LNeutralRadius = 5;
    private const int LNeutralDarkFloor = 16;
    private const double LNeutralGainLeast = 0;
    private const double LNeutralGainMost = 2;
    private const double LNeutralEpsilon = 1e-6;

    public static LNeutralSample LNeutralResolve(
        IReadOnlyList<byte> lNeutralPixels,
        int lNeutralWidth,
        int lNeutralHeight,
        int lNeutralCenterX,
        int lNeutralCenterY,
        LNeutralTarget lNeutralTarget = LNeutralTarget.LNeutralTargetGrey)
    {
        if (lNeutralPixels is null
            || lNeutralWidth <= 0
            || lNeutralHeight <= 0
            || lNeutralPixels.Count < lNeutralWidth * lNeutralHeight * 4
            || lNeutralCenterX < 0
            || lNeutralCenterY < 0
            || lNeutralCenterX >= lNeutralWidth
            || lNeutralCenterY >= lNeutralHeight)
        {
            return LNeutralFailCreate(LNeutralOutcome.LNeutralOutcomeOutside);
        }

        int lNeutralLeft = Math.Max(0, lNeutralCenterX - LNeutralRadius);
        int lNeutralTop = Math.Max(0, lNeutralCenterY - LNeutralRadius);
        int lNeutralRight = Math.Min(lNeutralWidth - 1, lNeutralCenterX + LNeutralRadius);
        int lNeutralBottom = Math.Min(lNeutralHeight - 1, lNeutralCenterY + LNeutralRadius);

        List<int> lNeutralRedList = new();
        List<int> lNeutralGreenList = new();
        List<int> lNeutralBlueList = new();
        for (int lNeutralY = lNeutralTop; lNeutralY <= lNeutralBottom; lNeutralY++)
        {
            for (int lNeutralX = lNeutralLeft; lNeutralX <= lNeutralRight; lNeutralX++)
            {
                int lNeutralIndex = ((lNeutralY * lNeutralWidth) + lNeutralX) * 4;
                if (lNeutralPixels[lNeutralIndex + 3] == 0)
                {
                    continue;
                }

                lNeutralRedList.Add(lNeutralPixels[lNeutralIndex]);
                lNeutralGreenList.Add(lNeutralPixels[lNeutralIndex + 1]);
                lNeutralBlueList.Add(lNeutralPixels[lNeutralIndex + 2]);
            }
        }

        if (lNeutralRedList.Count == 0)
        {
            return LNeutralFailCreate(LNeutralOutcome.LNeutralOutcomeEmpty);
        }

        int lNeutralRed = LNeutralMedianResolve(lNeutralRedList);
        int lNeutralGreen = LNeutralMedianResolve(lNeutralGreenList);
        int lNeutralBlue = LNeutralMedianResolve(lNeutralBlueList);

        if (Math.Max(lNeutralRed, Math.Max(lNeutralGreen, lNeutralBlue)) < LNeutralDarkFloor)
        {
            return LNeutralFailCreate(LNeutralOutcome.LNeutralOutcomeDark);
        }

        return LNeutralSampleCreate(lNeutralRed, lNeutralGreen, lNeutralBlue, lNeutralTarget);
    }

    private static LNeutralSample LNeutralSampleCreate(
        int lNeutralRed, int lNeutralGreen, int lNeutralBlue, LNeutralTarget lNeutralTargetKind)
    {
        double lNeutralRedUnit = LNeutralUnitResolve(lNeutralRed);
        double lNeutralGreenUnit = LNeutralUnitResolve(lNeutralGreen);
        double lNeutralBlueUnit = LNeutralUnitResolve(lNeutralBlue);

        double lNeutralTarget = lNeutralTargetKind == LNeutralTarget.LNeutralTargetWhite
            ? Math.Max(lNeutralRedUnit, Math.Max(lNeutralGreenUnit, lNeutralBlueUnit))
            : (0.2126 * lNeutralRedUnit)
                + (0.7152 * lNeutralGreenUnit)
                + (0.0722 * lNeutralBlueUnit);

        double lNeutralRedGain = LNeutralGainResolve(lNeutralTarget, lNeutralRedUnit);
        double lNeutralGreenGain = LNeutralGainResolve(lNeutralTarget, lNeutralGreenUnit);
        double lNeutralBlueGain = LNeutralGainResolve(lNeutralTarget, lNeutralBlueUnit);

        return new LNeutralSample(
            LNeutralOutcome.LNeutralOutcomeResolved,
            lNeutralRed,
            lNeutralGreen,
            lNeutralBlue,
            lNeutralRedGain,
            lNeutralGreenGain,
            lNeutralBlueGain);
    }

    private static double LNeutralGainResolve(double lNeutralTarget, double lNeutralChannel)
    {
        if (lNeutralChannel < LNeutralEpsilon)
        {
            return LNeutralGainMost;
        }

        double lNeutralGain = lNeutralTarget / lNeutralChannel;
        if (!double.IsFinite(lNeutralGain))
        {
            return 1;
        }

        return Math.Clamp(lNeutralGain, LNeutralGainLeast, LNeutralGainMost);
    }

    private static double LNeutralUnitResolve(int lNeutralChannel) =>
        Math.Clamp(lNeutralChannel, 0, 255) / 255.0;

    private static int LNeutralMedianResolve(List<int> lNeutralValues)
    {
        lNeutralValues.Sort();
        int lNeutralCount = lNeutralValues.Count;
        int lNeutralMiddle = lNeutralCount / 2;
        if ((lNeutralCount & 1) == 1)
        {
            return lNeutralValues[lNeutralMiddle];
        }

        return (int)Math.Round(
            (lNeutralValues[lNeutralMiddle - 1] + lNeutralValues[lNeutralMiddle]) / 2.0,
            MidpointRounding.AwayFromZero);
    }

    private static LNeutralSample LNeutralFailCreate(LNeutralOutcome lNeutralOutcome) =>
        new(lNeutralOutcome, 0, 0, 0, 1, 1, 1);
}
