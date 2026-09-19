using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TViewfinderTicks
{
    private static LFlow TFlowBuild(double seconds)
    {
        LFlow flow = TInterface.TFlowCreate();
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowSourceSet(
            flow, TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(seconds), 640, 360), "C:\\media\\clip.mp4");
        return flow;
    }

    [Fact]
    public void Ticks_PickTheStepFromTheZoom_AndLabelEachOne()
    {
        LFlow flow = TFlowBuild(60);
        LViewfinder viewfinder = TInterface.TViewfinderCreate(flow);

        IReadOnlyList<LViewfinderTick> ticks =
            TInterface.TViewfinderFrameResolve(viewfinder, 600, 100).LViewfinderFrameTicks;
        Assert.Equal(7, ticks.Count);
        Assert.Equal(0, ticks[0].LViewfinderTickX);
        Assert.Equal(100, ticks[1].LViewfinderTickX);
        Assert.Equal("0:10", ticks[1].LViewfinderTickLabel);
        Assert.Equal(10, ticks[1].LViewfinderTickTop);
        Assert.Equal(20, ticks[1].LViewfinderTickBottom);

        TInterface.TFlowRangeSet(flow, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        Assert.Equal(6, TInterface.TViewfinderFrameResolve(viewfinder, 600, 100).LViewfinderFrameTicks.Count);

        LFlow hours = TFlowBuild(3600);
        LViewfinder wide = TInterface.TViewfinderCreate(hours);
        IReadOnlyList<LViewfinderTick> coarse =
            TInterface.TViewfinderFrameResolve(wide, 300, 100).LViewfinderFrameTicks;
        Assert.Equal(3, coarse.Count);
        Assert.Equal("1:00:00", coarse[2].LViewfinderTickLabel);

        LViewfinderPoint point = TInterface.TViewfinderTickResolve(ticks[1], 10);
        Assert.Equal(102, point.LViewfinderPointX);
        Assert.Equal(5, point.LViewfinderPointY);
    }

    [Fact]
    public void Frame_PlacesTheCursorChip_AndDropsItOutsideTheRange()
    {
        LFlow flow = TFlowBuild(60);
        LViewfinder viewfinder = TInterface.TViewfinderCreate(flow);
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(30));

        LViewfinderFrame frame = TInterface.TViewfinderFrameResolve(viewfinder, 600, 100);
        LViewfinderCursor cursor = Assert.Single(frame.LViewfinderFrameCursor);
        Assert.Equal(300, cursor.LViewfinderCursorX);
        Assert.Equal("0:30", cursor.LViewfinderCursorText);
        Assert.Equal(3, frame.LViewfinderFrameUnder.Count);
        Assert.Equal(LViewfinderShapeKind.LViewfinderShapeRail, frame.LViewfinderFrameUnder[1].LViewfinderShapeKind);
        Assert.Equal(22, frame.LViewfinderFrameUnder[1].LViewfinderShapeTop);
        Assert.Equal(93, frame.LViewfinderFrameUnder[1].LViewfinderShapeBottom);

        LViewfinderChip chip = TInterface.TViewfinderChipResolve(cursor, 20, 10, 600, 100);
        Assert.Equal(286, chip.LViewfinderChipLeft);
        Assert.Equal(314, chip.LViewfinderChipRight);
        Assert.Equal(53, chip.LViewfinderChipTop);
        Assert.Equal(67, chip.LViewfinderChipBottom);
        Assert.Equal(290, chip.LViewfinderChipText.LViewfinderPointX);
        Assert.Equal(55, chip.LViewfinderChipText.LViewfinderPointY);

        TInterface.TFlowRangeSet(flow, TimeSpan.FromSeconds(40), TimeSpan.FromSeconds(60));
        Assert.Empty(TInterface.TViewfinderFrameResolve(viewfinder, 600, 100).LViewfinderFrameCursor);
        Assert.Single(TInterface.TViewfinderFrameResolve(viewfinder, 600, 20).LViewfinderFrameUnder);
    }
}
