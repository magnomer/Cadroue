namespace Cadroue.UIDeportment;

internal static class QColumnWidth
{
    public static double QColumnAvailableResolve(
        IReadOnlyList<bool> lHiddenFlags, double lGridWidth, double lSplitterWidth)
    {
        int lVisibleCount = lHiddenFlags.Count(lHidden => !lHidden);
        return Math.Max(0, lGridWidth - lSplitterWidth * Math.Max(0, lVisibleCount - 1));
    }

    public static double[] QColumnMinimumRead(
        IReadOnlyList<double> lBaseWidths,
        IReadOnlyList<bool> lHiddenFlags,
        IReadOnlyList<double> lFixedWidths,
        double lAvailableWidth)
    {
        double[] lMinimumWidths = lBaseWidths.ToArray();
        for (int lIndex = 0; lIndex < lMinimumWidths.Length; lIndex++)
        {
            if (lHiddenFlags[lIndex])
            {
                lMinimumWidths[lIndex] = 0;
            }
            else if (lFixedWidths[lIndex] > 0)
            {
                lMinimumWidths[lIndex] = lFixedWidths[lIndex];
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

    public static double[]? QColumnDefaultResolve(
        IReadOnlyList<bool> lHiddenFlags,
        IReadOnlyList<bool> lCompactFlags,
        double lAvailableWidth,
        IReadOnlyList<double> lMinimumWidths)
    {
        int lCount = lHiddenFlags.Count;
        double lCompactTotal = 0;
        int lFlexibleCount = 0;
        for (int lIndex = 0; lIndex < lCount; lIndex++)
        {
            if (lHiddenFlags[lIndex])
            {
                continue;
            }

            if (lCompactFlags[lIndex])
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
        var lWidths = new double[lCount];
        for (int lIndex = 0; lIndex < lCount; lIndex++)
        {
            if (lHiddenFlags[lIndex])
            {
                continue;
            }

            lWidths[lIndex] = lCompactFlags[lIndex]
                ? lMinimumWidths[lIndex]
                : Math.Max(lMinimumWidths[lIndex], lFlexibleWidth);
        }

        return lWidths;
    }

    public static bool QColumnDragResolve(
        int lFlexIndex, int lLeftIndex, double lDelta, double[] lWidths, IReadOnlyList<double> lMinimumWidths)
    {
        int lReceiverIndex;
        int lDonorIndex;
        double lReceiverSign;
        if (lFlexIndex < 0)
        {
            lReceiverIndex = lLeftIndex;
            lDonorIndex = lLeftIndex + 1;
            lReceiverSign = 1;
        }
        else
        {
            lDonorIndex = lFlexIndex;
            lReceiverIndex = lFlexIndex > lLeftIndex ? lLeftIndex : lLeftIndex + 1;
            lReceiverSign = lFlexIndex > lLeftIndex ? 1 : -1;
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
}
