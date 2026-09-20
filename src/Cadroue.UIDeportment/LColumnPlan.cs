namespace Cadroue.UIDeportment;

public sealed record LColumnSlot(double LColumnSlotWidth, bool LColumnSlotStar, double LColumnSlotMinimum);

public sealed class LColumnPlan
{
    public const double LColumnSplitterWidth = 6;
    private const double LColumnStarFloor = 0.0001;

    private readonly LColumn lColumn;
    private readonly double[] lColumnMinimums;

    public event Action? LColumnPlanChange;
    public event Action? LColumnWidthChange;

    public LColumnPlan(
        IReadOnlyList<double> lMinimumWidths,
        IReadOnlyList<double>? lStoredWidths,
        IReadOnlyList<bool>? lCompactPanels,
        int lFlexIndex)
    {
        lColumn = new LColumn(lMinimumWidths, lStoredWidths, lCompactPanels, lFlexIndex);
        lColumnMinimums = lMinimumWidths.ToArray();
    }

    public LColumn LColumn => lColumn;

    public IReadOnlyList<double> LColumnWeightsRead() => lColumn.LColumnWeightsRead();

    public double LColumnTotalRead() => lColumn.LColumnTotalRead(LColumnSplitterWidth);

    public IReadOnlyList<LColumnSlot> LColumnSlotsResolve()
    {
        lColumn.LColumnWeightsNormalize();
        bool lFlexActive = lColumn.LColumnPixelCheck();
        var lSlots = new LColumnSlot[lColumn.LColumnCount];
        for (int lIndex = 0; lIndex < lSlots.Length; lIndex++)
        {
            lSlots[lIndex] = LColumnSlotResolve(lIndex, lFlexActive);
        }

        return lSlots;
    }

    public void LColumnWidthSet(int lIndex, double lFixedWidth)
    {
        if (lColumn.LColumnWidthSet(lIndex, lFixedWidth))
        {
            LColumnPlanChange?.Invoke();
        }
    }

    public void LColumnHiddenSet(int lIndex, bool lHidden)
    {
        if (!lColumn.LColumnHiddenSet(lIndex, lHidden))
        {
            return;
        }

        lColumnMinimums[lIndex] = 0;
        LColumnPlanChange?.Invoke();
    }

    public void LColumnLayoutHandle(double lSlotWidth, double lActualWidth)
    {
        double lAvailableWidth = LColumnAvailableResolve(lSlotWidth, lActualWidth);
        if (!lColumn.LColumnAppliedSet(lAvailableWidth))
        {
            return;
        }

        double[] lMinimumWidths = lColumn.LColumnMinimumRead(lAvailableWidth);
        Array.Copy(lMinimumWidths, lColumnMinimums, lMinimumWidths.Length);
        bool lCommitted = lColumn.LColumnDefaultsRead()
            && lColumn.LColumnDefaultResolve(lAvailableWidth, lMinimumWidths) is { } lDefaultWidths
            && lColumn.LColumnWeightsCommit(lDefaultWidths);
        bool lCreated = lColumn.LColumnFlexCheck()
            && !lColumn.LColumnPixelsReady
            && lColumn.LColumnPixelsCreate(lAvailableWidth);
        LColumnPlanChange?.Invoke();
        if (lCommitted || lCreated)
        {
            LColumnWidthChange?.Invoke();
        }
    }

    public void LColumnDragHandle(
        int lLeftIndex,
        double lDelta,
        double lSlotWidth,
        double lActualWidth,
        IReadOnlyList<double> lActualWidths)
    {
        if (lLeftIndex < 0 || lLeftIndex >= lColumn.LColumnCount - 1 || Math.Abs(lDelta) <= 0)
        {
            return;
        }

        double lAvailableWidth = LColumnAvailableResolve(lSlotWidth, lActualWidth);
        if (lAvailableWidth <= 0)
        {
            return;
        }

        double[] lWidths = LColumnCurrentResolve(lAvailableWidth, lActualWidths);
        double[] lMinimumWidths = lColumn.LColumnMinimumRead(lAvailableWidth);
        if (!lColumn.LColumnDragResolve(lLeftIndex, lDelta, lWidths, lMinimumWidths)
            || !lColumn.LColumnWeightsCommit(lWidths))
        {
            return;
        }

        LColumnPlanChange?.Invoke();
        LColumnWidthChange?.Invoke();
    }

    public static double LColumnGridResolve(double lSlotWidth, double lActualWidth) =>
        double.IsNaN(lSlotWidth) || double.IsInfinity(lSlotWidth) || lSlotWidth <= 0 ? lActualWidth : lSlotWidth;

    private double LColumnAvailableResolve(double lSlotWidth, double lActualWidth) =>
        lColumn.LColumnAvailableResolve(LColumnGridResolve(lSlotWidth, lActualWidth), LColumnSplitterWidth);

    private LColumnSlot LColumnSlotResolve(int lIndex, bool lFlexActive)
    {
        double lMinimum = lColumnMinimums[lIndex];
        if (lColumn.LColumnHiddenCheck(lIndex))
        {
            return new LColumnSlot(0, false, lMinimum);
        }

        if (lColumn.LColumnFixedRead(lIndex) > 0)
        {
            return new LColumnSlot(lColumn.LColumnFixedRead(lIndex), false, lMinimum);
        }

        if (lFlexActive)
        {
            return lIndex == lColumn.LColumnFlexIndex
                ? new LColumnSlot(1, true, lMinimum)
                : new LColumnSlot(Math.Max(0, lColumn.LColumnPixelRead(lIndex)), false, lMinimum);
        }

        return new LColumnSlot(Math.Max(LColumnStarFloor, lColumn.LColumnWeightRead(lIndex)), true, lMinimum);
    }

    private static double[] LColumnCurrentResolve(double lAvailableWidth, IReadOnlyList<double> lActualWidths)
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
