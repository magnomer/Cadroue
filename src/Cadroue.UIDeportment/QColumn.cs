namespace Cadroue.UIDeportment;

public sealed class QColumn
{
    private readonly double[] qColumnMinimumWidths;
    private readonly double[] qColumnWeights;
    private readonly double[] qColumnStoredWeights;
    private readonly bool[] qColumnHiddenFlags;
    private readonly bool[] qColumnCompactFlags;
    private readonly double[] qColumnFixedWidths;
    private readonly double[] qColumnPixelWidths;
    private readonly int qColumnFlexIndex;
    private double qColumnAppliedWidth = -1;
    private bool qColumnDefaultsPending;
    private bool qColumnPixelsReady;

    public QColumn(
        IReadOnlyList<double> lMinimumWidths,
        IReadOnlyList<double>? lStoredWidths,
        IReadOnlyList<bool>? lCompactPanels,
        int lFlexIndex)
    {
        int lCount = lMinimumWidths.Count;
        qColumnMinimumWidths = lMinimumWidths.ToArray();
        qColumnWeights = QColumnWeightCreate(lStoredWidths, lCount);
        qColumnStoredWeights = qColumnWeights.ToArray();
        qColumnHiddenFlags = new bool[lCount];
        qColumnCompactFlags = new bool[lCount];
        for (int lIndex = 0; lCompactPanels is not null && lIndex < lCount && lIndex < lCompactPanels.Count; lIndex++)
        {
            qColumnCompactFlags[lIndex] = lCompactPanels[lIndex];
        }

        qColumnFixedWidths = new double[lCount];
        qColumnPixelWidths = new double[lCount];
        qColumnFlexIndex = lFlexIndex >= 0 && lFlexIndex < lCount ? lFlexIndex : -1;
        qColumnDefaultsPending =
            !QColumnStoredCheck(lStoredWidths, lCount) && qColumnCompactFlags.Any(lCompact => lCompact);
    }

    public int QColumnCount => qColumnMinimumWidths.Length;

    public int QColumnFlexIndex => qColumnFlexIndex;

    public bool QColumnPixelsReady => qColumnPixelsReady;

    public bool QColumnHiddenCheck(int lIndex) => qColumnHiddenFlags[lIndex];

    public double QColumnFixedRead(int lIndex) => qColumnFixedWidths[lIndex];

    public double QColumnPixelRead(int lIndex) => qColumnPixelWidths[lIndex];

    public double QColumnWeightRead(int lIndex) => qColumnWeights[lIndex];

    public bool QColumnFlexCheck() =>
        qColumnFlexIndex >= 0 && !qColumnHiddenFlags[qColumnFlexIndex] && qColumnFixedWidths[qColumnFlexIndex] <= 0;

    public bool QColumnPixelCheck() => QColumnFlexCheck() && qColumnPixelsReady;

    public bool QColumnAppliedSet(double lAvailableWidth)
    {
        if (lAvailableWidth <= 0 || Math.Abs(lAvailableWidth - qColumnAppliedWidth) < 0.5)
        {
            return false;
        }

        qColumnAppliedWidth = lAvailableWidth;
        return true;
    }

    public bool QColumnDefaultsRead()
    {
        if (!qColumnDefaultsPending)
        {
            return false;
        }

        qColumnDefaultsPending = false;
        return true;
    }

    public bool QColumnWidthSet(int lIndex, double lFixedWidth)
    {
        if (lIndex < 0 || lIndex >= QColumnCount)
        {
            return false;
        }

        if (lFixedWidth > 0)
        {
            if (qColumnFixedWidths[lIndex] <= 0)
            {
                QColumnStoredSet(lIndex);
            }

            qColumnFixedWidths[lIndex] = lFixedWidth;
            qColumnWeights[lIndex] = 0;
        }
        else
        {
            if (qColumnFixedWidths[lIndex] <= 0)
            {
                return false;
            }

            qColumnFixedWidths[lIndex] = 0;
            qColumnWeights[lIndex] = qColumnStoredWeights[lIndex] > 0 ? qColumnStoredWeights[lIndex] : 1;
        }

        qColumnAppliedWidth = -1;
        return true;
    }

    public bool QColumnHiddenSet(int lIndex, bool lHidden)
    {
        if (lIndex < 0 || lIndex >= QColumnCount || qColumnHiddenFlags[lIndex] == lHidden)
        {
            return false;
        }

        if (lHidden)
        {
            QColumnStoredSet(lIndex);
            qColumnHiddenFlags[lIndex] = true;
            qColumnWeights[lIndex] = 0;
        }
        else
        {
            qColumnHiddenFlags[lIndex] = false;
            qColumnWeights[lIndex] = qColumnStoredWeights[lIndex] > 0 ? qColumnStoredWeights[lIndex] : 1;
        }

        qColumnAppliedWidth = -1;
        return true;
    }

    private void QColumnStoredSet(int lIndex)
    {
        if (qColumnWeights[lIndex] > 0)
        {
            qColumnStoredWeights[lIndex] = qColumnWeights[lIndex];
        }
    }

    public IReadOnlyList<double> QColumnWeightsRead()
    {
        var lWeights = new double[QColumnCount];
        for (int lIndex = 0; lIndex < lWeights.Length; lIndex++)
        {
            double lWeight = qColumnHiddenFlags[lIndex] || qColumnFixedWidths[lIndex] > 0
                ? qColumnStoredWeights[lIndex]
                : qColumnWeights[lIndex];
            lWeights[lIndex] = lWeight > 0 ? lWeight : 1;
        }

        return lWeights;
    }

    public bool QColumnWeightsCommit(IReadOnlyList<double> lWidths)
    {
        double lWidthTotal = lWidths.Sum();
        if (lWidthTotal <= 0)
        {
            return false;
        }

        for (int lIndex = 0; lIndex < lWidths.Count; lIndex++)
        {
            qColumnWeights[lIndex] = qColumnHiddenFlags[lIndex] ? 0 : lWidths[lIndex] / lWidthTotal;
            qColumnPixelWidths[lIndex] = qColumnHiddenFlags[lIndex] ? 0 : Math.Max(0, lWidths[lIndex]);
        }

        if (QColumnFlexCheck())
        {
            qColumnPixelsReady = true;
        }

        return true;
    }

    public void QColumnWeightsNormalize()
    {
        if (qColumnWeights.Sum(lWeight => Math.Max(0, lWeight)) > 0)
        {
            return;
        }

        for (int lIndex = 0; lIndex < qColumnWeights.Length; lIndex++)
        {
            qColumnWeights[lIndex] = qColumnHiddenFlags[lIndex] ? 0 : 1;
        }
    }

    public bool QColumnPixelsCreate(double lAvailableWidth)
    {
        double lWeightTotal = qColumnWeights.Sum(lWeight => Math.Max(0, lWeight));
        if (lWeightTotal <= 0 || lAvailableWidth <= 0)
        {
            return false;
        }

        for (int lIndex = 0; lIndex < QColumnCount; lIndex++)
        {
            qColumnPixelWidths[lIndex] = Math.Max(0, qColumnWeights[lIndex]) / lWeightTotal * lAvailableWidth;
        }

        qColumnPixelsReady = true;
        return true;
    }

    public double QColumnTotalRead(double lSplitterWidth)
    {
        double lMinimumTotal = 0;
        int lVisibleCount = 0;
        bool lFlexActive = QColumnPixelCheck();
        for (int lIndex = 0; lIndex < QColumnCount; lIndex++)
        {
            if (qColumnHiddenFlags[lIndex])
            {
                continue;
            }

            lVisibleCount++;
            if (qColumnFixedWidths[lIndex] > 0)
            {
                lMinimumTotal += qColumnFixedWidths[lIndex];
            }
            else if (lFlexActive && lIndex != qColumnFlexIndex)
            {
                lMinimumTotal += Math.Max(qColumnMinimumWidths[lIndex], qColumnPixelWidths[lIndex]);
            }
            else
            {
                lMinimumTotal += qColumnMinimumWidths[lIndex];
            }
        }

        return lMinimumTotal + lSplitterWidth * Math.Max(0, lVisibleCount - 1);
    }

    public double QColumnAvailableResolve(double lGridWidth, double lSplitterWidth)
    {
        int lVisibleCount = qColumnHiddenFlags.Count(lHidden => !lHidden);
        return Math.Max(0, lGridWidth - lSplitterWidth * Math.Max(0, lVisibleCount - 1));
    }

    public double[] QColumnMinimumRead(double lAvailableWidth)
    {
        double[] lMinimumWidths = qColumnMinimumWidths.ToArray();
        for (int lIndex = 0; lIndex < lMinimumWidths.Length; lIndex++)
        {
            if (qColumnHiddenFlags[lIndex])
            {
                lMinimumWidths[lIndex] = 0;
            }
            else if (qColumnFixedWidths[lIndex] > 0)
            {
                lMinimumWidths[lIndex] = qColumnFixedWidths[lIndex];
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

    public double[]? QColumnDefaultResolve(double lAvailableWidth, IReadOnlyList<double> lMinimumWidths)
    {
        double lCompactTotal = 0;
        int lFlexibleCount = 0;
        for (int lIndex = 0; lIndex < QColumnCount; lIndex++)
        {
            if (qColumnHiddenFlags[lIndex])
            {
                continue;
            }

            if (qColumnCompactFlags[lIndex])
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
        var lWidths = new double[QColumnCount];
        for (int lIndex = 0; lIndex < QColumnCount; lIndex++)
        {
            if (qColumnHiddenFlags[lIndex])
            {
                continue;
            }

            lWidths[lIndex] = qColumnCompactFlags[lIndex]
                ? lMinimumWidths[lIndex]
                : Math.Max(lMinimumWidths[lIndex], lFlexibleWidth);
        }

        return lWidths;
    }

    public bool QColumnDragResolve(
        int lLeftIndex, double lDelta, double[] lWidths, IReadOnlyList<double> lMinimumWidths)
    {
        int lReceiverIndex;
        int lDonorIndex;
        double lReceiverSign;
        if (!QColumnFlexCheck())
        {
            lReceiverIndex = lLeftIndex;
            lDonorIndex = lLeftIndex + 1;
            lReceiverSign = 1;
        }
        else
        {
            lDonorIndex = qColumnFlexIndex;
            lReceiverIndex = qColumnFlexIndex > lLeftIndex ? lLeftIndex : lLeftIndex + 1;
            lReceiverSign = qColumnFlexIndex > lLeftIndex ? 1 : -1;
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

    private static bool QColumnStoredCheck(IReadOnlyList<double>? lStoredWidths, int lCount) =>
        lStoredWidths is not null
        && lStoredWidths.Count == lCount
        && lStoredWidths.Sum(lWidth => Math.Max(0, lWidth)) > 0;

    private static double[] QColumnWeightCreate(IReadOnlyList<double>? lStoredWidths, int lCount)
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
