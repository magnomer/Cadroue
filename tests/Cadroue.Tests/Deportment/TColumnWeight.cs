using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TColumnWeight
{
    private static LColumn TColumnBuild(
        IReadOnlyList<double>? stored = null, IReadOnlyList<bool>? compact = null, int flex = -1) =>
        TInterface.TColumnCreate([100, 100, 100], stored, compact, flex);

    [Fact]
    public void Hide_KeepsWeightForShow()
    {
        LColumn column = TColumnBuild([200, 200, 400]);

        Assert.True(TInterface.TColumnHiddenSet(column, 2, true));
        Assert.False(TInterface.TColumnHiddenSet(column, 2, true));
        Assert.True(TInterface.TColumnHiddenCheck(column, 2));
        Assert.Equal(0, TInterface.TColumnWeightRead(column, 2));
        Assert.Equal(0.5, TInterface.TColumnWeightsRead(column)[2]);

        Assert.True(TInterface.TColumnHiddenSet(column, 2, false));
        Assert.Equal(0.5, TInterface.TColumnWeightRead(column, 2));
    }

    [Fact]
    public void WidthSet_FixesAndRestoresWeight()
    {
        LColumn column = TColumnBuild([100, 100, 200]);

        Assert.True(TInterface.TColumnWidthSet(column, 0, 48));
        Assert.Equal(48, TInterface.TColumnFixedRead(column, 0));
        Assert.Equal(0, TInterface.TColumnWeightRead(column, 0));
        Assert.False(TInterface.TColumnWidthSet(column, 1, 0));

        Assert.True(TInterface.TColumnWidthSet(column, 0, 0));
        Assert.Equal(0.25, TInterface.TColumnWeightRead(column, 0));
    }

    [Fact]
    public void Total_SumsMinimumsFixedAndSplitters()
    {
        LColumn column = TColumnBuild();
        TInterface.TColumnWidthSet(column, 0, 48);
        TInterface.TColumnHiddenSet(column, 2, true);

        Assert.Equal(48 + 100 + 6, TInterface.TColumnTotalRead(column, 6));
    }

    [Fact]
    public void Minimum_ScalesWhenAvailableIsShort()
    {
        LColumn column = TColumnBuild();
        TInterface.TColumnHiddenSet(column, 1, true);

        Assert.Equal([50, 0, 50], TInterface.TColumnMinimumRead(column, 100));
        Assert.Equal([100, 0, 100], TInterface.TColumnMinimumRead(column, 400));
    }

    [Fact]
    public void Defaults_SplitRemainderAmongFlexible_OnceOnly()
    {
        LColumn column = TColumnBuild(compact: [true, false, false]);

        Assert.True(TInterface.TColumnDefaultsRead(column));
        Assert.False(TInterface.TColumnDefaultsRead(column));
        Assert.Equal([100, 300, 300], TInterface.TColumnDefaultResolve(column, 700, [100, 100, 100])!);

        Assert.False(TInterface.TColumnDefaultsRead(TColumnBuild([1, 1, 1], [true, false, false])));
    }

    [Fact]
    public void Drag_MovesBudgetBetweenNeighbours_WithinMinimums()
    {
        LColumn column = TColumnBuild();
        double[] widths = [200, 200, 200];

        Assert.True(TInterface.TColumnDragResolve(column, 0, 150, widths, [100, 100, 100]));
        Assert.Equal([300, 100, 200], widths);
        Assert.False(TInterface.TColumnDragResolve(column, 0, 50, widths, [100, 100, 100]));
    }

    [Fact]
    public void Drag_WithFlexColumn_DonatesFromFlex()
    {
        LColumn column = TColumnBuild(flex: 2);
        double[] widths = [200, 200, 200];

        Assert.True(TInterface.TColumnDragResolve(column, 0, 50, widths, [100, 100, 100]));
        Assert.Equal([250, 200, 150], widths);

        Assert.True(TInterface.TColumnWeightsCommit(column, widths));
        Assert.True(column.LColumnPixelsReady);
        Assert.Equal(150, TInterface.TColumnPixelRead(column, 2));
    }

    [Fact]
    public void Applied_ThrottlesSmallChanges()
    {
        LColumn column = TColumnBuild();

        Assert.False(TInterface.TColumnAppliedSet(column, 0));
        Assert.True(TInterface.TColumnAppliedSet(column, 800));
        Assert.False(TInterface.TColumnAppliedSet(column, 800.2));
        Assert.True(TInterface.TColumnAppliedSet(column, 801));
    }

    [Fact]
    public void SlotsResolve_HiddenFixedFlexAndStar()
    {
        LColumnPlan plan = TInterface.TColumnPlanCreate([100, 100, 100], [200, 200, 400], null, 2);
        int plans = 0;
        TInterface.TColumnPlanAttach(plan, () => plans++);

        IReadOnlyList<LColumnSlot> slots = TInterface.TColumnSlotsResolve(plan);
        Assert.Equal([true, true, true], slots.Select(slot => slot.LColumnSlotStar));
        Assert.Equal([0.25, 0.25, 0.5], slots.Select(slot => slot.LColumnSlotWidth));
        Assert.Equal([100, 100, 100], slots.Select(slot => slot.LColumnSlotMinimum));

        TInterface.TColumnHiddenSet(plan, 1, true);
        TInterface.TColumnWidthSet(plan, 0, 48);
        Assert.Equal(2, plans);
        slots = TInterface.TColumnSlotsResolve(plan);
        Assert.Equal(
            (48, false, 100),
            (slots[0].LColumnSlotWidth, slots[0].LColumnSlotStar, slots[0].LColumnSlotMinimum));
        Assert.Equal((0, false, 0), (slots[1].LColumnSlotWidth, slots[1].LColumnSlotStar, slots[1].LColumnSlotMinimum));
        Assert.True(slots[2].LColumnSlotStar);

        TInterface.TColumnHiddenSet(plan, 1, true);
        Assert.Equal(2, plans);
    }

    [Fact]
    public void LayoutHandle_AppliesMinimums_CommitsDefaultsOnce()
    {
        LColumnPlan plan = TInterface.TColumnPlanCreate([100, 100, 100], null, [true, false, false], -1);
        int plans = 0;
        int widths = 0;
        TInterface.TColumnPlanAttach(plan, () => plans++);
        TInterface.TColumnWidthAttach(plan, () => widths++);

        TInterface.TColumnLayoutHandle(plan, double.NaN, 0);
        Assert.Equal((0, 0), (plans, widths));

        TInterface.TColumnLayoutHandle(plan, double.NaN, 712);
        Assert.Equal((1, 1), (plans, widths));
        Assert.Equal(
            [100, 300, 300],
            TInterface.TColumnWeightsRead(TInterface.TColumnOwnerRead(plan)).Select(w => Math.Round(w * 700)));

        TInterface.TColumnLayoutHandle(plan, 712, 0);
        Assert.Equal((1, 1), (plans, widths));

        TInterface.TColumnLayoutHandle(plan, 812, 0);
        Assert.Equal((2, 1), (plans, widths));
        Assert.Equal(700, TInterface.TColumnGridResolve(double.PositiveInfinity, 700));
        Assert.Equal(812, TInterface.TColumnGridResolve(812, 700));
    }

    [Fact]
    public void DragHandle_MovesWidthBetweenNeighbours()
    {
        LColumnPlan plan = TInterface.TColumnPlanCreate([100, 100, 100], null, null, -1);
        int plans = 0;
        int widths = 0;
        TInterface.TColumnPlanAttach(plan, () => plans++);
        TInterface.TColumnWidthAttach(plan, () => widths++);

        TInterface.TColumnDragHandle(plan, 2, 50, 612, 0, [200, 200, 200]);
        TInterface.TColumnDragHandle(plan, 0, 0, 612, 0, [200, 200, 200]);
        TInterface.TColumnDragHandle(plan, 0, 50, 0, 0, [200, 200, 200]);
        Assert.Equal((0, 0), (plans, widths));

        TInterface.TColumnDragHandle(plan, 0, 50, 612, 0, [200, 200, 200]);
        Assert.Equal((1, 1), (plans, widths));
        Assert.Equal(
            [250, 150, 200],
            TInterface.TColumnWeightsRead(TInterface.TColumnOwnerRead(plan)).Select(w => Math.Round(w * 600)));
        Assert.Equal(312, TInterface.TColumnTotalRead(plan));
    }
}
