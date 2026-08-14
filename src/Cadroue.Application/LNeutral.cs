namespace Cadroue.Application;

public enum LNeutralOutcome
{
    LNeutralOutcomeResolved,
    LNeutralOutcomeOutside,
    LNeutralOutcomeEmpty,
    LNeutralOutcomeDark
}

public sealed record LNeutralSample(
    LNeutralOutcome LNeutralOutcome,
    int LNeutralRed,
    int LNeutralGreen,
    int LNeutralBlue,
    double LNeutralRedGain,
    double LNeutralGreenGain,
    double LNeutralBlueGain)
{
    public bool LNeutralResolved => LNeutralOutcome == LNeutralOutcome.LNeutralOutcomeResolved;
}

public static class LNeutral
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
        int lNeutralCenterY)
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

        // Work in linear light so the correction matches how the sensor mixes colour,
        // not the gamma-compressed sRGB byte values.
        double lNeutralRedLinear = LNeutralLinearResolve(lNeutralRed);
        double lNeutralGreenLinear = LNeutralLinearResolve(lNeutralGreen);
        double lNeutralBlueLinear = LNeutralLinearResolve(lNeutralBlue);

        // Neutral target = the sample's own Rec.709 luminance. A diagonal gain of
        // target / channel drives every channel to that single luminance, so a truly
        // neutral sample yields gains of 1 and the corrected channels come out equal.
        // Because the target equals the sample luminance, brightness is preserved: the
        // corrected pixel keeps the same luminance it started with. Clamping each gain to
        // 0..2 is the only cap on amplification — a near-black channel would otherwise
        // demand an unbounded multiplier and blow out highlights; the clamp bounds that
        // to a controlled 2x while leaving ordinary casts untouched.
        double lNeutralTarget =
            (0.2126 * lNeutralRedLinear)
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
