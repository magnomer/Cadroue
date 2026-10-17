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
        qColumnCompactFlags = QColumnCompactCreate(lCompactPanels, lCount);
        qColumnFixedWidths = new double[lCount];
        qColumnPixelWidths = new double[lCount];
        qColumnFlexIndex = QColumnFlexNormalize(lFlexIndex, lCount);
        qColumnDefaultsPending = QColumnDefaultsCheck(lStoredWidths, lCount, qColumnCompactFlags);
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
        if (!QColumnAppliedCheck(lAvailableWidth))
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
        if (!QColumnIndexCheck(lIndex))
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
            qColumnWeights[lIndex] = QColumnStoredResolve(lIndex);
        }

        qColumnAppliedWidth = -1;
        return true;
    }

    public bool QColumnHiddenSet(int lIndex, bool lHidden)
    {
        if (!QColumnIndexCheck(lIndex) || qColumnHiddenFlags[lIndex] == lHidden)
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
            qColumnWeights[lIndex] = QColumnStoredResolve(lIndex);
        }

        qColumnAppliedWidth = -1;
        return true;
    }

    // A width change below half a pixel leaves the applied width as it is.
    private bool QColumnAppliedCheck(double lAvailableWidth) =>
        !(lAvailableWidth <= 0 || Math.Abs(lAvailableWidth - qColumnAppliedWidth) < 0.5);

    private bool QColumnIndexCheck(int lIndex) => lIndex >= 0 && lIndex < QColumnCount;

    private double QColumnStoredResolve(int lIndex) =>
        qColumnStoredWeights[lIndex] > 0 ? qColumnStoredWeights[lIndex] : 1;

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

    public double QColumnAvailableResolve(double lGridWidth, double lSplitterWidth) =>
        QColumnWidth.QColumnAvailableResolve(qColumnHiddenFlags, lGridWidth, lSplitterWidth);

    public double[] QColumnMinimumRead(double lAvailableWidth) =>
        QColumnWidth.QColumnMinimumRead(qColumnMinimumWidths, qColumnHiddenFlags, qColumnFixedWidths, lAvailableWidth);

    public double[]? QColumnDefaultResolve(double lAvailableWidth, IReadOnlyList<double> lMinimumWidths) =>
        QColumnWidth.QColumnDefaultResolve(qColumnHiddenFlags, qColumnCompactFlags, lAvailableWidth, lMinimumWidths);

    public bool QColumnDragResolve(
        int lLeftIndex, double lDelta, double[] lWidths, IReadOnlyList<double> lMinimumWidths) =>
        QColumnWidth.QColumnDragResolve(
            QColumnFlexCheck() ? qColumnFlexIndex : -1, lLeftIndex, lDelta, lWidths, lMinimumWidths);

    private static bool[] QColumnCompactCreate(IReadOnlyList<bool>? lCompactPanels, int lCount)
    {
        var lCompactFlags = new bool[lCount];
        for (int lIndex = 0; lCompactPanels is not null && lIndex < lCount && lIndex < lCompactPanels.Count; lIndex++)
        {
            lCompactFlags[lIndex] = lCompactPanels[lIndex];
        }

        return lCompactFlags;
    }

    private static int QColumnFlexNormalize(int lFlexIndex, int lCount) =>
        lFlexIndex >= 0 && lFlexIndex < lCount ? lFlexIndex : -1;

    private static bool QColumnDefaultsCheck(
        IReadOnlyList<double>? lStoredWidths, int lCount, IReadOnlyList<bool> lCompactFlags) =>
        !QColumnStoredCheck(lStoredWidths, lCount) && lCompactFlags.Any(lCompact => lCompact);

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
