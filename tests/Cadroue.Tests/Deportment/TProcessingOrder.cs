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
        TInterface.TProcessingDragSet(processing, 0);

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
}
