using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TProcessingOrder
{
    private static QProcessing TProcessingBuild(params string[] steps)
    {
        QProcessing processing = TInterface.TProcessingCreate();
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
        QProcessing processing = TProcessingBuild("A", "B", "C");
        int orders = 0;
        TInterface.TProcessingOrderAttach(processing, () => orders++);
        TInterface.TProcessingStepSelect(processing, "C");

        Assert.True(TInterface.TProcessingStepMove(processing, -1));
        Assert.Equal(["A", "C", "B"], processing.QProcessingSteps);
        Assert.True(TInterface.TProcessingStepMove(processing, -1));
        Assert.False(TInterface.TProcessingStepMove(processing, -1));

        Assert.Equal(["C", "A", "B"], processing.QProcessingSteps);
        Assert.Equal(2, orders);
    }

    [Fact]
    public void DisabledStep_CannotBeSelectedOrPassed()
    {
        QProcessing processing = TProcessingBuild("A", "B", "C");
        TInterface.TProcessingEnabledSet(processing, "B", false);

        Assert.False(TInterface.TProcessingStepSelect(processing, "B"));
        Assert.Null(processing.QProcessingStep);

        TInterface.TProcessingStepSelect(processing, "A");

        Assert.False(TInterface.TProcessingStepMove(processing, 1));
        Assert.Equal(["A", "B", "C"], processing.QProcessingSteps);
    }

    [Fact]
    public void DragMove_TracksDragIndex()
    {
        QProcessing processing = TProcessingBuild("A", "B", "C");
        TInterface.TProcessingDragStart(processing, "A", 0, 0);

        Assert.True(TInterface.TProcessingIndexMove(processing, 0, 2));

        Assert.Equal(["B", "C", "A"], processing.QProcessingSteps);
        Assert.Equal(2, processing.QProcessingDrag.QProcessingDragIndex);
        Assert.False(TInterface.TProcessingIndexMove(processing, 2, 2));
        Assert.False(TInterface.TProcessingIndexMove(processing, 2, 5));
    }

    [Fact]
    public void SkipStep_AlwaysSelectable_NotifiesName()
    {
        QProcessing processing = TProcessingBuild("A");
        List<string> notices = [];
        TInterface.TProcessingStepAttach(processing, notices.Add);

        Assert.True(TInterface.TProcessingStepSelect(processing, QProcessing.QProcessingSkipStep));

        Assert.Equal(QProcessing.QProcessingSkipStep, processing.QProcessingStep);
        Assert.Equal([QProcessing.QProcessingSkipStep], notices);
    }

    [Fact]
    public void DragMove_ArmsAfterThreshold_ReordersByRowMiddles()
    {
        QProcessing processing = TProcessingBuild("A", "B", "C");
        int changes = 0;
        TInterface.TProcessingAttach(processing, () => changes++);
        TInterface.TProcessingDragStart(processing, "A", 10, 10);
        int baseline = changes;

        TInterface.TProcessingDragMove(processing, 12, 12, true, [0, 20, 40], [20, 20, 20]);
        Assert.False(processing.QProcessingDrag.QProcessingDragActive);
        Assert.Equal(baseline, changes);

        TInterface.TProcessingDragMove(processing, 12, 55, false, [0, 20, 40], [20, 20, 20]);
        Assert.False(processing.QProcessingDrag.QProcessingDragActive);

        TInterface.TProcessingDragMove(processing, 12, 55, true, [0, 20, 40], [20, 20, 20]);
        Assert.True(processing.QProcessingDrag.QProcessingDragActive);
        Assert.Equal(["B", "C", "A"], processing.QProcessingSteps);
        Assert.Equal(0.72, TInterface.TProcessingOpacityRead(processing, "A"));
        Assert.Equal(1, TInterface.TProcessingOpacityRead(processing, "B"));
        Assert.Equal("3", TInterface.TProcessingNumberRead(processing, "A"));

        TInterface.TProcessingDragClear(processing);
        Assert.False(processing.QProcessingDrag.QProcessingDragActive);
        Assert.Null(processing.QProcessingDrag.QProcessingDragIndex);
        Assert.Equal(1, TInterface.TProcessingOpacityRead(processing, "A"));
    }

    [Fact]
    public void DragMove_IgnoredWhenUnordered_CancelledWhenDisabled()
    {
        QProcessing processing = TProcessingBuild("A", "B");
        TInterface.TProcessingOrderedSet(processing, false);
        TInterface.TProcessingDragStart(processing, "A", 0, 0);

        TInterface.TProcessingDragMove(processing, 0, 100, true, [0, 20], [20, 20]);
        Assert.Equal(["A", "B"], processing.QProcessingSteps);

        TInterface.TProcessingOrderedSet(processing, true);
        int cancels = 0;
        TInterface.TProcessingCancelAttach(processing, () => cancels++);
        TInterface.TProcessingEnabledSet(processing, "A", false, "busy");

        Assert.Equal(1, cancels);
        Assert.Null(processing.QProcessingDrag.QProcessingDragIndex);
        Assert.Equal(0.4, TInterface.TProcessingOpacityRead(processing, "A"));
        Assert.Equal(2, TInterface.TProcessingIndexResolve(100, [0, 20, 40], [20, 20, 20]));
        Assert.Equal(1, TInterface.TProcessingIndexResolve(25, [0, 20, 40], [20, 20, 20]));
    }

    [Fact]
    public void RowAdd_RecordsRowAndStep_KeyHandleSelectsOnActivate()
    {
        QProcessing processing = TInterface.TProcessingCreate();
        List<string> notices = [];
        TInterface.TProcessingStepAttach(processing, notices.Add);

        TInterface.TProcessingRowAdd(processing, "Crop");

        Assert.Equal(["Crop"], processing.QProcessingSteps);
        Assert.Equal(["Crop"], processing.QProcessingRows.Select(row => row.QProcessingRowKey));

        TInterface.TProcessingKeyHandle(processing, "Crop", false);
        Assert.Empty(notices);

        TInterface.TProcessingKeyHandle(processing, "Crop", true);
        Assert.Equal(["Crop"], notices);
    }
}
