using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TViewfinderTicks
{
    private static QFlow TFlowBuild(double seconds)
    {
        QFlow flow = TInterface.TFlowCreate();
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowSourceSet(
            flow, TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(seconds), 640, 360), "C:\\media\\clip.mp4");
        return flow;
    }

    [Fact]
    public void Ticks_PickTheStepFromTheZoom_AndLabelEachOne()
    {
        QFlow flow = TFlowBuild(60);
        QViewfinder viewfinder = TInterface.TViewfinderRead(flow);

        IReadOnlyList<QViewfinderGraduation> ticks =
            TInterface.TViewfinderFrameResolve(viewfinder, 600, 100).QViewfinderFrameTicks;
        Assert.Equal(7, ticks.Count);
        Assert.Equal(0, ticks[0].QViewfinderTickX);
        Assert.Equal(100, ticks[1].QViewfinderTickX);
        Assert.Equal("0:10", ticks[1].QViewfinderTickLabel);
        Assert.Equal(10, ticks[1].QViewfinderTickTop);
        Assert.Equal(20, ticks[1].QViewfinderTickBottom);

        TInterface.TFlowRangeSet(flow, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        Assert.Equal(6, TInterface.TViewfinderFrameResolve(viewfinder, 600, 100).QViewfinderFrameTicks.Count);

        QFlow hours = TFlowBuild(3600);
        QViewfinder wide = TInterface.TViewfinderRead(hours);
        IReadOnlyList<QViewfinderGraduation> coarse =
            TInterface.TViewfinderFrameResolve(wide, 300, 100).QViewfinderFrameTicks;
        Assert.Equal(3, coarse.Count);
        Assert.Equal("1:00:00", coarse[2].QViewfinderTickLabel);

        QViewfinderPoint point = TInterface.TViewfinderTickResolve(ticks[1], 10);
        Assert.Equal(102, point.QViewfinderPointX);
        Assert.Equal(5, point.QViewfinderPointY);
    }

    [Fact]
    public void Frame_PlacesTheCursorChip_AndDropsItOutsideTheRange()
    {
        QFlow flow = TFlowBuild(60);
        QViewfinder viewfinder = TInterface.TViewfinderRead(flow);
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(30));

        QViewfinderFrame frame = TInterface.TViewfinderFrameResolve(viewfinder, 600, 100);
        QViewfinderCursor cursor = Assert.Single(frame.QViewfinderFrameCursor);
        Assert.Equal(300, cursor.QViewfinderCursorX);
        Assert.Equal("0:30", cursor.QViewfinderCursorText);
        Assert.Equal(3, frame.QViewfinderFrameUnder.Count);
        Assert.Equal(QViewfinderShapeKind.QViewfinderShapeRail, frame.QViewfinderFrameUnder[1].QViewfinderShapeKind);
        Assert.Equal(22, frame.QViewfinderFrameUnder[1].QViewfinderShapeTop);
        Assert.Equal(93, frame.QViewfinderFrameUnder[1].QViewfinderShapeBottom);

        QViewfinderChip chip = TInterface.TViewfinderChipResolve(cursor, 20, 10, 600, 100);
        Assert.Equal(286, chip.QViewfinderChipLeft);
        Assert.Equal(314, chip.QViewfinderChipRight);
        Assert.Equal(53, chip.QViewfinderChipTop);
        Assert.Equal(67, chip.QViewfinderChipBottom);
        Assert.Equal(290, chip.QViewfinderChipText.QViewfinderPointX);
        Assert.Equal(55, chip.QViewfinderChipText.QViewfinderPointY);

        TInterface.TFlowRangeSet(flow, TimeSpan.FromSeconds(40), TimeSpan.FromSeconds(60));
        Assert.Empty(TInterface.TViewfinderFrameResolve(viewfinder, 600, 100).QViewfinderFrameCursor);
        Assert.Single(TInterface.TViewfinderFrameResolve(viewfinder, 600, 20).QViewfinderFrameUnder);
    }
}
