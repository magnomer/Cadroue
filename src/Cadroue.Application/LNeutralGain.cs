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

    // Resolve the correction sample for one gray triple, whether it came from a
    // sampled region, a colour-wheel pick, or a whole-frame analysis.
    //
    // Work in linear light so the correction matches how the sensor mixes colour,
    // not the gamma-compressed sRGB byte values. Two targets drive the diagonal gain
    // of target / channel:
    //   Grey  — target = the sample's own Rec.709 luminance. Every channel is driven
    //           to that single luminance, so a truly neutral sample yields gains of 1,
    //           the corrected channels come out equal, and brightness is preserved.
    //           Strict: assumes the pick is genuinely neutral grey.
    //   White — target = the sample's brightest linear channel. Only the deficient
    //           channels are lifted to that max (gains >= 1); nothing is pushed down.
    //           Lenient: the pick need only sit on the black-to-white axis (its own
    //           channel ratio names the cast); brightness rises slightly.
    // Clamping each gain to 0..2 is the only cap on amplification — a near-black
    // channel would otherwise demand an unbounded multiplier and blow out highlights;
    // the clamp bounds that to a controlled 2x while leaving ordinary casts untouched.
    private static LNeutralSample LNeutralSampleCreate(
        int lNeutralRed, int lNeutralGreen, int lNeutralBlue, LNeutralTarget lNeutralTargetKind)
    {
        double lNeutralRedLinear = LNeutralLinearResolve(lNeutralRed);
        double lNeutralGreenLinear = LNeutralLinearResolve(lNeutralGreen);
        double lNeutralBlueLinear = LNeutralLinearResolve(lNeutralBlue);

        double lNeutralTarget = lNeutralTargetKind == LNeutralTarget.LNeutralTargetWhite
            ? Math.Max(lNeutralRedLinear, Math.Max(lNeutralGreenLinear, lNeutralBlueLinear))
            : (0.2126 * lNeutralRedLinear)
                + (0.7152 * lNeutralGreenLinear)
                + (0.0722 * lNeutralBlueLinear);

        double lNeutralRedGain = LNeutralGainResolve(lNeutralTarget, lNeutralRedLinear);
        double lNeutralGreenGain = LNeutralGainResolve(lNeutralTarget, lNeutralGreenLinear);
        double lNeutralBlueGain = LNeutralGainResolve(lNeutralTarget, lNeutralBlueLinear);

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

    private static double LNeutralLinearResolve(int lNeutralChannel)
    {
        double lNeutralValue = lNeutralChannel / 255.0;
        return lNeutralValue <= 0.04045
            ? lNeutralValue / 12.92
            : Math.Pow((lNeutralValue + 0.055) / 1.055, 2.4);
    }

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
