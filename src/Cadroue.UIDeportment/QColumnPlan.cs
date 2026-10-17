namespace Cadroue.UIDeportment;

public sealed record QColumnSlot(double QColumnSlotWidth, bool QColumnSlotStar, double QColumnSlotMinimum);

public sealed class QColumnPlan
{
    public const double QColumnSplitterWidth = 6;
    private const double QColumnStarFloor = 0.0001;

    private readonly QColumn qColumn;
    private readonly double[] qColumnMinimums;

    public event Action? QColumnPlanChanged;
    public event Action? QColumnWidthChanged;

    public QColumnPlan(
        IReadOnlyList<double> lMinimumWidths,
        IReadOnlyList<double>? lStoredWidths,
        IReadOnlyList<bool>? lCompactPanels,
        int lFlexIndex)
    {
        qColumn = new QColumn(lMinimumWidths, lStoredWidths, lCompactPanels, lFlexIndex);
        qColumnMinimums = lMinimumWidths.ToArray();
    }

    public QColumn QColumn => qColumn;

    public IReadOnlyList<double> QColumnWeightsRead() => qColumn.QColumnWeightsRead();

    public double QColumnTotalRead() => qColumn.QColumnTotalRead(QColumnSplitterWidth);

    public IReadOnlyList<QColumnSlot> QColumnSlotsResolve()
    {
        qColumn.QColumnWeightsNormalize();
        bool lFlexActive = qColumn.QColumnPixelCheck();
        var lSlots = new QColumnSlot[qColumn.QColumnCount];
        for (int lIndex = 0; lIndex < lSlots.Length; lIndex++)
        {
            lSlots[lIndex] = QColumnSlotResolve(lIndex, lFlexActive);
        }

        return lSlots;
    }

    public void QColumnWidthSet(int lIndex, double lFixedWidth)
    {
        if (qColumn.QColumnWidthSet(lIndex, lFixedWidth))
        {
            QColumnPlanChanged?.Invoke();
        }
    }

    public void QColumnHiddenSet(int lIndex, bool lHidden)
    {
        if (!qColumn.QColumnHiddenSet(lIndex, lHidden))
        {
            return;
        }

        qColumnMinimums[lIndex] = 0;
        QColumnPlanChanged?.Invoke();
    }

    public void QColumnLayoutHandle(double lSlotWidth, double lActualWidth)
    {
        double lAvailableWidth = QColumnAvailableResolve(lSlotWidth, lActualWidth);
        if (!qColumn.QColumnAppliedSet(lAvailableWidth))
        {
            return;
        }

        double[] lMinimumWidths = qColumn.QColumnMinimumRead(lAvailableWidth);
        QColumnMinimumSet(lMinimumWidths);
        bool lCommitted = QColumnDefaultsCommit(lAvailableWidth, lMinimumWidths);
        bool lCreated = QColumnPixelsPrepare(lAvailableWidth);
        QColumnPlanChanged?.Invoke();
        if (lCommitted || lCreated)
        {
            QColumnWidthChanged?.Invoke();
        }
    }

    public void QColumnDragHandle(
        int lLeftIndex,
        double lDelta,
        double lSlotWidth,
        double lActualWidth,
        IReadOnlyList<double> lActualWidths)
    {
        if (!QColumnDragCheck(lLeftIndex, lDelta))
        {
            return;
        }

        double lAvailableWidth = QColumnAvailableResolve(lSlotWidth, lActualWidth);
        if (lAvailableWidth <= 0)
        {
            return;
        }

        double[] lWidths = QColumnCurrentResolve(lAvailableWidth, lActualWidths);
        double[] lMinimumWidths = qColumn.QColumnMinimumRead(lAvailableWidth);
        if (!qColumn.QColumnDragResolve(lLeftIndex, lDelta, lWidths, lMinimumWidths)
            || !qColumn.QColumnWeightsCommit(lWidths))
        {
            return;
        }

        QColumnPlanChanged?.Invoke();
        QColumnWidthChanged?.Invoke();
    }

    private void QColumnMinimumSet(double[] lMinimumWidths) =>
        Array.Copy(lMinimumWidths, qColumnMinimums, lMinimumWidths.Length);

    // Pending defaults are committed once, when the panel widths resolve.
    private bool QColumnDefaultsCommit(double lAvailableWidth, double[] lMinimumWidths) =>
        qColumn.QColumnDefaultsRead()
        && qColumn.QColumnDefaultResolve(lAvailableWidth, lMinimumWidths) is { } lDefaultWidths
        && qColumn.QColumnWeightsCommit(lDefaultWidths);

    // The flex column's pixel widths are created once, at the first layout that has them active.
    private bool QColumnPixelsPrepare(double lAvailableWidth) =>
        qColumn.QColumnFlexCheck()
        && !qColumn.QColumnPixelsReady
        && qColumn.QColumnPixelsCreate(lAvailableWidth);

    // A drag needs a splitter with a column on each side and a real movement.
    private bool QColumnDragCheck(int lLeftIndex, double lDelta) =>
        !(lLeftIndex < 0 || lLeftIndex >= qColumn.QColumnCount - 1 || Math.Abs(lDelta) <= 0);

    public static double QColumnGridResolve(double lSlotWidth, double lActualWidth) =>
        double.IsNaN(lSlotWidth) || double.IsInfinity(lSlotWidth) || lSlotWidth <= 0 ? lActualWidth : lSlotWidth;

    private double QColumnAvailableResolve(double lSlotWidth, double lActualWidth) =>
        qColumn.QColumnAvailableResolve(QColumnGridResolve(lSlotWidth, lActualWidth), QColumnSplitterWidth);

    private QColumnSlot QColumnSlotResolve(int lIndex, bool lFlexActive)
    {
        double lMinimum = qColumnMinimums[lIndex];
        if (qColumn.QColumnHiddenCheck(lIndex))
        {
            return new QColumnSlot(0, false, lMinimum);
        }

        if (qColumn.QColumnFixedRead(lIndex) > 0)
        {
            return new QColumnSlot(qColumn.QColumnFixedRead(lIndex), false, lMinimum);
        }

        if (lFlexActive)
        {
            return lIndex == qColumn.QColumnFlexIndex
                ? new QColumnSlot(1, true, lMinimum)
                : new QColumnSlot(Math.Max(0, qColumn.QColumnPixelRead(lIndex)), false, lMinimum);
        }

        return new QColumnSlot(Math.Max(QColumnStarFloor, qColumn.QColumnWeightRead(lIndex)), true, lMinimum);
    }

    private static double[] QColumnCurrentResolve(double lAvailableWidth, IReadOnlyList<double> lActualWidths)
    {
        double[] lWidths = lActualWidths.ToArray();
        if (lWidths.Sum() > 0)
        {
            return lWidths;
        }

        double lEqualWidth = lAvailableWidth / lWidths.Length;
        for (int lIndex = 0; lIndex < lWidths.Length; lIndex++)
        {
            lWidths[lIndex] = lEqualWidth;
        }

        return lWidths;
    }
}
