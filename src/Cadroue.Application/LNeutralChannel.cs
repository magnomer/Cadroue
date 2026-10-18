namespace Cadroue.Application;

// One channel's samples reduced to one value; the whole-frame analysis and the point sample both reduce here.
internal static class LNeutralChannel
{
    internal static int LNeutralMeanResolve(List<int> lNeutralValues)
    {
        long lNeutralSum = 0;
        foreach (int lNeutralValue in lNeutralValues)
        {
            lNeutralSum += lNeutralValue;
        }

        return (int)Math.Round(lNeutralSum / (double)lNeutralValues.Count);
    }

    internal static int LNeutralMidResolve(List<int> lNeutralValues)
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

    internal static int LNeutralMedianResolve(List<int> lNeutralValues)
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
}
