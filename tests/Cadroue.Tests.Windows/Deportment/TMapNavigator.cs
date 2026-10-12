using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TMapNavigator
{
    private static QFlow TFlowBuild()
    {
        QFlow flow = TInterface.TFlowCreate();
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowSourceSet(
            flow, TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(60), 640, 360), "C:\\media\\clip.mp4");
        TInterface.TFlowRangeSet(flow, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(40));
        return flow;
    }

    [Fact]
    public void Hover_FindsGrips_Body_AndCursorArea()
    {
        QFlow flow = TFlowBuild();
        QMap map = TInterface.TMapRead(flow);

        Assert.Equal(QMapHitKind.QMapHitOrigin, TInterface.TMapHoverResolve(map, 103, 10, 600, 40));
        Assert.Equal(QMapHitKind.QMapHitLimit, TInterface.TMapHoverResolve(map, 397, 10, 600, 40));
        Assert.Equal(QMapHitKind.QMapHitBody, TInterface.TMapHoverResolve(map, 250, 5, 600, 40));
        Assert.Equal(QMapHitKind.QMapHitCursor, TInterface.TMapHoverResolve(map, 250, 30, 600, 40));
        Assert.Equal(QMapHitKind.QMapHitCursor, TInterface.TMapHoverResolve(map, 50, 10, 600, 40));
        Assert.Equal(QMapHitKind.QMapHitNone, TInterface.TMapHoverResolve(map, 50, 10, 0, 40));
        Assert.Equal(QMapHitKind.QMapHitNone, TInterface.TMapLeaveResolve(map));
    }

    [Fact]
    public void BodyDrag_MovesTheSpool_AndRaisesDragNotices()
    {
        QFlow flow = TFlowBuild();
        QMap map = TInterface.TMapRead(flow);
        List<bool> drags = [];
        TInterface.TFlowPlayAttach(flow, () => { }, () => { }, drags.Add, _ => { });

        Assert.True(TInterface.TMapPressHandle(map, 250, 5, 600, 40));
        Assert.Equal(QMapHitKind.QMapHitBody, map.QMapDragKind);
        Assert.True(TInterface.TMapMoveHandle(map, 260, 600));
        Assert.Equal(TimeSpan.FromSeconds(11), flow.QFlowRange!.LSpoolRangeOrigin);
        Assert.Equal(TimeSpan.FromSeconds(41), flow.QFlowRange.LSpoolRangeLimit);
        Assert.Equal(QMapHitKind.QMapHitBody, TInterface.TMapLeaveResolve(map));

        TInterface.TMapDragClear(map);
        Assert.Equal(QMapHitKind.QMapHitNone, map.QMapDragKind);
        Assert.False(TInterface.TMapMoveHandle(map, 300, 600));
        Assert.Equal([true, false], drags);
    }

    [Fact]
    public void GripDrag_ResizesOneEnd_AndCursorPressSeeks()
    {
        QFlow flow = TFlowBuild();
        QMap map = TInterface.TMapRead(flow);
        List<TimeSpan> seeks = [];
        TInterface.TFlowCursorAttach(flow, () => { }, seeks.Add);

        TInterface.TMapPressHandle(map, 103, 10, 600, 40);
        TInterface.TMapMoveHandle(map, 163, 600);
        Assert.Equal(TimeSpan.FromSeconds(16), flow.QFlowRange!.LSpoolRangeOrigin);
        Assert.Equal(TimeSpan.FromSeconds(40), flow.QFlowRange.LSpoolRangeLimit);
        TInterface.TMapDragClear(map);

        TInterface.TMapPressHandle(map, 50, 30, 600, 40);
        Assert.Equal(QMapHitKind.QMapHitCursor, map.QMapDragKind);
        Assert.Equal(TimeSpan.FromSeconds(5), flow.QFlowCursor);
        TInterface.TMapMoveHandle(map, 600, 600);
        Assert.Equal(TimeSpan.FromSeconds(60), flow.QFlowCursor);
        Assert.Equal([TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(60)], seeks);

        QFlow bare = TInterface.TFlowCreate();
        Assert.False(TInterface.TMapPressHandle(TInterface.TMapRead(bare), 10, 10, 600, 40));
    }

    [Fact]
    public void Frame_CarriesNavigatorShapes_AndRedrawsOnFlowNotices()
    {
        QFlow flow = TFlowBuild();
        QMap map = TInterface.TMapRead(flow);
        int redraws = 0;
        TInterface.TMapFrameAttach(map, () => redraws++);

        QMapFrame frame = TInterface.TMapFrameResolve(map, 600, 40);
        Assert.Equal(2, frame.QMapFrameUnder.Count);
        Assert.Equal(QMapShapeKind.QMapShapeRail, frame.QMapFrameUnder[1].QMapShapeKind);
        QMapShape fill = Assert.Single(
            frame.QMapFrameOver, shape => shape.QMapShapeKind == QMapShapeKind.QMapShapeFill);
        Assert.Equal(100, fill.QMapShapeLeft);
        Assert.Equal(400, fill.QMapShapeRight);
        Assert.Contains(frame.QMapFrameOver, shape => shape.QMapShapeKind == QMapShapeKind.QMapShapeBorder);
        Assert.Contains(frame.QMapFrameOver, shape => shape.QMapShapeKind == QMapShapeKind.QMapShapeDot);
        Assert.Equal([0], frame.QMapFrameCursor);

        TInterface.TFlowCursorSeek(flow, TimeSpan.FromSeconds(30));
        Assert.Equal(1, redraws);
        Assert.Equal("cursor", map.QMapTrigger);
        Assert.Equal([300], TInterface.TMapFrameResolve(map, 600, 40).QMapFrameCursor);

        Assert.Empty(TInterface.TMapFrameResolve(map, 600, 8).QMapFrameOver);
        Assert.Empty(TInterface.TMapFrameResolve(map, 0, 40).QMapFrameUnder);
    }
}
