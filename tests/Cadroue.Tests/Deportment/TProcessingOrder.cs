using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TProcessingOrder
{
    private static LProcessing TProcessingBuild(params string[] steps)
    {
        LProcessing processing = TInterface.TProcessingCreate();
        TInterface.TProcessingOrderedSet(processing, true);
        foreach (string step in steps)
        {
            TInterface.TProcessingStepAdd(processing, step);
        }

        return processing;
    }

    [Fact]
    public void MoveSelected_ByDelta_StopsAtEdges()
    {
        LProcessing processing = TProcessingBuild("A", "B", "C");
        int orders = 0;
        TInterface.TProcessingOrderAttach(processing, () => orders++);
        TInterface.TProcessingStepSelect(processing, "C");

        Assert.True(TInterface.TProcessingStepMove(processing, -1));
        Assert.Equal(["A", "C", "B"], processing.LProcessingSteps);
        Assert.True(TInterface.TProcessingStepMove(processing, -1));
        Assert.False(TInterface.TProcessingStepMove(processing, -1));

        Assert.Equal(["C", "A", "B"], processing.LProcessingSteps);
        Assert.Equal(2, orders);
    }

    [Fact]
    public void DisabledStep_CannotBeSelectedOrPassed()
    {
        LProcessing processing = TProcessingBuild("A", "B", "C");
        TInterface.TProcessingEnabledSet(processing, "B", false);

        Assert.False(TInterface.TProcessingStepSelect(processing, "B"));
        Assert.Null(processing.LProcessingStep);

        TInterface.TProcessingStepSelect(processing, "A");

        Assert.False(TInterface.TProcessingStepMove(processing, 1));
        Assert.Equal(["A", "B", "C"], processing.LProcessingSteps);
    }

    [Fact]
    public void DragMove_TracksDragIndex()
    {
        LProcessing processing = TProcessingBuild("A", "B", "C");
        TInterface.TProcessingDragStart(processing, "A", 0, 0);

        Assert.True(TInterface.TProcessingIndexMove(processing, 0, 2));

        Assert.Equal(["B", "C", "A"], processing.LProcessingSteps);
        Assert.Equal(2, processing.LProcessingDragIndex);
        Assert.False(TInterface.TProcessingIndexMove(processing, 2, 2));
        Assert.False(TInterface.TProcessingIndexMove(processing, 2, 5));
    }

    [Fact]
    public void SkipStep_AlwaysSelectable_NotifiesName()
    {
        LProcessing processing = TProcessingBuild("A");
        List<string> notices = [];
        TInterface.TProcessingStepAttach(processing, notices.Add);

        Assert.True(TInterface.TProcessingStepSelect(processing, LProcessing.LProcessingSkipStep));

        Assert.Equal(LProcessing.LProcessingSkipStep, processing.LProcessingStep);
        Assert.Equal([LProcessing.LProcessingSkipStep], notices);
    }

    [Fact]
    public void DragMove_ArmsAfterThreshold_ReordersByRowMiddles()
    {
        LProcessing processing = TProcessingBuild("A", "B", "C");
        int changes = 0;
        TInterface.TProcessingAttach(processing, () => changes++);
        TInterface.TProcessingDragStart(processing, "A", 10, 10);
        int baseline = changes;

        TInterface.TProcessingDragMove(processing, 12, 12, true, [0, 20, 40], [20, 20, 20]);
        Assert.False(processing.LProcessingDragActive);
        Assert.Equal(baseline, changes);

        TInterface.TProcessingDragMove(processing, 12, 55, false, [0, 20, 40], [20, 20, 20]);
        Assert.False(processing.LProcessingDragActive);

        TInterface.TProcessingDragMove(processing, 12, 55, true, [0, 20, 40], [20, 20, 20]);
        Assert.True(processing.LProcessingDragActive);
        Assert.Equal(["B", "C", "A"], processing.LProcessingSteps);
        Assert.Equal(0.72, TInterface.TProcessingOpacityRead(processing, "A"));
        Assert.Equal(1, TInterface.TProcessingOpacityRead(processing, "B"));
        Assert.Equal("3", TInterface.TProcessingNumberRead(processing, "A"));

        TInterface.TProcessingDragClear(processing);
        Assert.False(processing.LProcessingDragActive);
        Assert.Null(processing.LProcessingDragIndex);
        Assert.Equal(1, TInterface.TProcessingOpacityRead(processing, "A"));
    }

    [Fact]
    public void DragMove_IgnoredWhenUnordered_CancelledWhenDisabled()
    {
        LProcessing processing = TProcessingBuild("A", "B");
        TInterface.TProcessingOrderedSet(processing, false);
        TInterface.TProcessingDragStart(processing, "A", 0, 0);

        TInterface.TProcessingDragMove(processing, 0, 100, true, [0, 20], [20, 20]);
        Assert.Equal(["A", "B"], processing.LProcessingSteps);

        TInterface.TProcessingOrderedSet(processing, true);
        int cancels = 0;
        TInterface.TProcessingCancelAttach(processing, () => cancels++);
        TInterface.TProcessingEnabledSet(processing, "A", false, "busy");

        Assert.Equal(1, cancels);
        Assert.Null(processing.LProcessingDragIndex);
        Assert.Equal(0.4, TInterface.TProcessingOpacityRead(processing, "A"));
        Assert.Equal(2, TInterface.TProcessingIndexResolve(100, [0, 20, 40], [20, 20, 20]));
        Assert.Equal(1, TInterface.TProcessingIndexResolve(25, [0, 20, 40], [20, 20, 20]));
    }

    [Fact]
    public void RowAdd_RecordsRowAndStep_KeyHandleSelectsOnActivate()
    {
        LProcessing processing = TInterface.TProcessingCreate();
        List<string> notices = [];
        TInterface.TProcessingStepAttach(processing, notices.Add);

        TInterface.TProcessingRowAdd(processing, "Crop");

        Assert.Equal(["Crop"], processing.LProcessingSteps);
        Assert.Equal(["Crop"], processing.LProcessingRows.Select(row => row.LProcessingRowKey));

        TInterface.TProcessingKeyHandle(processing, "Crop", false);
        Assert.Empty(notices);

        TInterface.TProcessingKeyHandle(processing, "Crop", true);
        Assert.Equal(["Crop"], notices);
    }
}
