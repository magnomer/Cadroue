namespace Cadroue.UIDeportment;

public sealed class LColumn
{
    private readonly double[] lColumnMinimumWidths;
    private readonly double[] lColumnWeights;
    private readonly double[] lColumnStoredWeights;
    private readonly bool[] lColumnHiddenFlags;
    private readonly bool[] lColumnCompactFlags;
    private readonly double[] lColumnFixedWidths;
    private readonly double[] lColumnPixelWidths;
    private readonly int lColumnFlexIndex;
    private double lColumnAppliedWidth = -1;
    private bool lColumnDefaultsPending;
    private bool lColumnPixelsReady;

    public LColumn(
        IReadOnlyList<double> lMinimumWidths,
        IReadOnlyList<double>? lStoredWidths,
        IReadOnlyList<bool>? lCompactPanels,
        int lFlexIndex)
    {
        int lCount = lMinimumWidths.Count;
        lColumnMinimumWidths = lMinimumWidths.ToArray();
        lColumnWeights = LColumnWeightCreate(lStoredWidths, lCount);
        lColumnStoredWeights = lColumnWeights.ToArray();
        lColumnHiddenFlags = new bool[lCount];
        lColumnCompactFlags = new bool[lCount];
        for (int lIndex = 0; lCompactPanels is not null && lIndex < lCount && lIndex < lCompactPanels.Count; lIndex++)
        {
            lColumnCompactFlags[lIndex] = lCompactPanels[lIndex];
        }

        lColumnFixedWidths = new double[lCount];
        lColumnPixelWidths = new double[lCount];
        lColumnFlexIndex = lFlexIndex >= 0 && lFlexIndex < lCount ? lFlexIndex : -1;
        lColumnDefaultsPending =
            !LColumnStoredCheck(lStoredWidths, lCount) && lColumnCompactFlags.Any(lCompact => lCompact);
    }

    public int LColumnCount => lColumnMinimumWidths.Length;

    public int LColumnFlexIndex => lColumnFlexIndex;

    public bool LColumnPixelsReady => lColumnPixelsReady;

    public bool LColumnHiddenCheck(int lIndex) => lColumnHiddenFlags[lIndex];

    public double LColumnFixedRead(int lIndex) => lColumnFixedWidths[lIndex];

    public double LColumnPixelRead(int lIndex) => lColumnPixelWidths[lIndex];

    public double LColumnWeightRead(int lIndex) => lColumnWeights[lIndex];

    public bool LColumnFlexCheck() =>
        lColumnFlexIndex >= 0 && !lColumnHiddenFlags[lColumnFlexIndex] && lColumnFixedWidths[lColumnFlexIndex] <= 0;

    public bool LColumnPixelCheck() => LColumnFlexCheck() && lColumnPixelsReady;

    public bool LColumnAppliedSet(double lAvailableWidth)
    {
        if (lAvailableWidth <= 0 || Math.Abs(lAvailableWidth - lColumnAppliedWidth) < 0.5)
        {
            return false;
        }

        lColumnAppliedWidth = lAvailableWidth;
        return true;
    }

    public bool LColumnDefaultsRead()
    {
        if (!lColumnDefaultsPending)
        {
            return false;
        }

        lColumnDefaultsPending = false;
        return true;
    }

    public bool LColumnWidthSet(int lIndex, double lFixedWidth)
    {
        if (lIndex < 0 || lIndex >= LColumnCount)
        {
            return false;
        }

        if (lFixedWidth > 0)
        {
            if (lColumnFixedWidths[lIndex] <= 0)
            {
                LColumnStoredSet(lIndex);
            }

            lColumnFixedWidths[lIndex] = lFixedWidth;
            lColumnWeights[lIndex] = 0;
        }
        else
        {
            if (lColumnFixedWidths[lIndex] <= 0)
            {
                return false;
            }

            lColumnFixedWidths[lIndex] = 0;
            lColumnWeights[lIndex] = lColumnStoredWeights[lIndex] > 0 ? lColumnStoredWeights[lIndex] : 1;
        }

        lColumnAppliedWidth = -1;
        return true;
    }

    public bool LColumnHiddenSet(int lIndex, bool lHidden)
    {
        if (lIndex < 0 || lIndex >= LColumnCount || lColumnHiddenFlags[lIndex] == lHidden)
        {
            return false;
        }

        if (lHidden)
        {
            LColumnStoredSet(lIndex);
            lColumnHiddenFlags[lIndex] = true;
            lColumnWeights[lIndex] = 0;
        }
        else
        {
            lColumnHiddenFlags[lIndex] = false;
            lColumnWeights[lIndex] = lColumnStoredWeights[lIndex] > 0 ? lColumnStoredWeights[lIndex] : 1;
        }

        lColumnAppliedWidth = -1;
        return true;
    }

    private void LColumnStoredSet(int lIndex)
    {
        if (lColumnWeights[lIndex] > 0)
        {
            lColumnStoredWeights[lIndex] = lColumnWeights[lIndex];
        }
    }

    public IReadOnlyList<double> LColumnWeightsRead()
    {
        var lWeights = new double[LColumnCount];
        for (int lIndex = 0; lIndex < lWeights.Length; lIndex++)
        {
            double lWeight = lColumnHiddenFlags[lIndex] || lColumnFixedWidths[lIndex] > 0
                ? lColumnStoredWeights[lIndex]
                : lColumnWeights[lIndex];
            lWeights[lIndex] = lWeight > 0 ? lWeight : 1;
        }

        return lWeights;
    }

    public bool LColumnWeightsCommit(IReadOnlyList<double> lWidths)
    {
        double lWidthTotal = lWidths.Sum();
        if (lWidthTotal <= 0)
        {
            return false;
        }

        for (int lIndex = 0; lIndex < lWidths.Count; lIndex++)
        {
            lColumnWeights[lIndex] = lColumnHiddenFlags[lIndex] ? 0 : lWidths[lIndex] / lWidthTotal;
            lColumnPixelWidths[lIndex] = lColumnHiddenFlags[lIndex] ? 0 : Math.Max(0, lWidths[lIndex]);
        }

        if (LColumnFlexCheck())
        {
            lColumnPixelsReady = true;
        }

        return true;
    }

    public void LColumnWeightsNormalize()
    {
        if (lColumnWeights.Sum(lWeight => Math.Max(0, lWeight)) > 0)
        {
            return;
        }

        for (int lIndex = 0; lIndex < lColumnWeights.Length; lIndex++)
        {
            lColumnWeights[lIndex] = lColumnHiddenFlags[lIndex] ? 0 : 1;
        }
    }

    public bool LColumnPixelsCreate(double lAvailableWidth)
    {
        double lWeightTotal = lColumnWeights.Sum(lWeight => Math.Max(0, lWeight));
        if (lWeightTotal <= 0 || lAvailableWidth <= 0)
        {
            return false;
        }

        for (int lIndex = 0; lIndex < LColumnCount; lIndex++)
        {
            lColumnPixelWidths[lIndex] = Math.Max(0, lColumnWeights[lIndex]) / lWeightTotal * lAvailableWidth;
        }

        lColumnPixelsReady = true;
        return true;
    }

    public double LColumnTotalRead(double lSplitterWidth)
    {
        double lMinimumTotal = 0;
        int lVisibleCount = 0;
        bool lFlexActive = LColumnPixelCheck();
        for (int lIndex = 0; lIndex < LColumnCount; lIndex++)
        {
            if (lColumnHiddenFlags[lIndex])
            {
                continue;
            }

            lVisibleCount++;
            if (lColumnFixedWidths[lIndex] > 0)
            {
                lMinimumTotal += lColumnFixedWidths[lIndex];
            }
            else if (lFlexActive && lIndex != lColumnFlexIndex)
            {
                lMinimumTotal += Math.Max(lColumnMinimumWidths[lIndex], lColumnPixelWidths[lIndex]);
            }
            else
            {
                lMinimumTotal += lColumnMinimumWidths[lIndex];
            }
        }

        return lMinimumTotal + lSplitterWidth * Math.Max(0, lVisibleCount - 1);
    }

    public double LColumnAvailableResolve(double lGridWidth, double lSplitterWidth)
    {
        int lVisibleCount = lColumnHiddenFlags.Count(lHidden => !lHidden);
        return Math.Max(0, lGridWidth - lSplitterWidth * Math.Max(0, lVisibleCount - 1));
    }

    public double[] LColumnMinimumRead(double lAvailableWidth)
    {
        double[] lMinimumWidths = lColumnMinimumWidths.ToArray();
        for (int lIndex = 0; lIndex < lMinimumWidths.Length; lIndex++)
        {
            if (lColumnHiddenFlags[lIndex])
            {
                lMinimumWidths[lIndex] = 0;
            }
            else if (lColumnFixedWidths[lIndex] > 0)
            {
                lMinimumWidths[lIndex] = lColumnFixedWidths[lIndex];
            }
        }

        double lMinimumTotal = lMinimumWidths.Sum();
        if (lMinimumTotal <= lAvailableWidth || lMinimumTotal <= 0)
        {
            return lMinimumWidths;
        }

        double lScale = lAvailableWidth / lMinimumTotal;
        for (int lIndex = 0; lIndex < lMinimumWidths.Length; lIndex++)
        {
            lMinimumWidths[lIndex] *= lScale;
        }

        return lMinimumWidths;
    }

    public double[]? LColumnDefaultResolve(double lAvailableWidth, IReadOnlyList<double> lMinimumWidths)
    {
        double lCompactTotal = 0;
        int lFlexibleCount = 0;
        for (int lIndex = 0; lIndex < LColumnCount; lIndex++)
        {
            if (lColumnHiddenFlags[lIndex])
            {
                continue;
            }

            if (lColumnCompactFlags[lIndex])
            {
                lCompactTotal += lMinimumWidths[lIndex];
                continue;
            }

            lFlexibleCount++;
        }

        if (lFlexibleCount == 0)
        {
            return null;
        }

        double lFlexibleWidth = Math.Max(0, lAvailableWidth - lCompactTotal) / lFlexibleCount;
        var lWidths = new double[LColumnCount];
        for (int lIndex = 0; lIndex < LColumnCount; lIndex++)
        {
            if (lColumnHiddenFlags[lIndex])
            {
                continue;
            }

            lWidths[lIndex] = lColumnCompactFlags[lIndex]
                ? lMinimumWidths[lIndex]
                : Math.Max(lMinimumWidths[lIndex], lFlexibleWidth);
        }

        return lWidths;
    }

    public bool LColumnDragResolve(
        int lLeftIndex, double lDelta, double[] lWidths, IReadOnlyList<double> lMinimumWidths)
    {
        int lReceiverIndex;
        int lDonorIndex;
        double lReceiverSign;
        if (!LColumnFlexCheck())
        {
            lReceiverIndex = lLeftIndex;
            lDonorIndex = lLeftIndex + 1;
            lReceiverSign = 1;
        }
        else
        {
            lDonorIndex = lColumnFlexIndex;
            lReceiverIndex = lColumnFlexIndex > lLeftIndex ? lLeftIndex : lLeftIndex + 1;
            lReceiverSign = lColumnFlexIndex > lLeftIndex ? 1 : -1;
        }

        double lClampedDelta = Math.Clamp(
            lReceiverSign * lDelta,
            lMinimumWidths[lReceiverIndex] - lWidths[lReceiverIndex],
            lWidths[lDonorIndex] - lMinimumWidths[lDonorIndex]);
        if (Math.Abs(lClampedDelta) <= 0)
        {
            return false;
        }

        lWidths[lReceiverIndex] += lClampedDelta;
        lWidths[lDonorIndex] -= lClampedDelta;
        return true;
    }

    private static bool LColumnStoredCheck(IReadOnlyList<double>? lStoredWidths, int lCount) =>
        lStoredWidths is not null
        && lStoredWidths.Count == lCount
        && lStoredWidths.Sum(lWidth => Math.Max(0, lWidth)) > 0;

    private static double[] LColumnWeightCreate(IReadOnlyList<double>? lStoredWidths, int lCount)
    {
        if (lStoredWidths is null || lStoredWidths.Count != lCount)
        {
            return Enumerable.Repeat(1d, lCount).ToArray();
        }

        double lWidthTotal = lStoredWidths.Sum(lWidth => Math.Max(0, lWidth));
        if (lWidthTotal <= 0)
        {
            return Enumerable.Repeat(1d, lCount).ToArray();
        }

        return lStoredWidths.Select(lWidth => Math.Max(0, lWidth) / lWidthTotal).ToArray();
    }
}
