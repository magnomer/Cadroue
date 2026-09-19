using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TMapNavigator
{
    private static LFlow TFlowBuild()
    {
        LFlow flow = TInterface.TFlowCreate();
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowSourceSet(
            flow, TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(60), 640, 360), "C:\\media\\clip.mp4");
        TInterface.TFlowRangeSet(flow, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(40));
        return flow;
    }

    [Fact]
    public void Hover_FindsGrips_Body_AndCursorArea()
    {
        LFlow flow = TFlowBuild();
        LMap map = TInterface.TMapCreate(flow);

        Assert.Equal(LMapHitKind.LMapHitOrigin, TInterface.TMapHoverResolve(map, 103, 10, 600, 40));
        Assert.Equal(LMapHitKind.LMapHitLimit, TInterface.TMapHoverResolve(map, 397, 10, 600, 40));
        Assert.Equal(LMapHitKind.LMapHitBody, TInterface.TMapHoverResolve(map, 250, 5, 600, 40));
        Assert.Equal(LMapHitKind.LMapHitCursor, TInterface.TMapHoverResolve(map, 250, 30, 600, 40));
        Assert.Equal(LMapHitKind.LMapHitCursor, TInterface.TMapHoverResolve(map, 50, 10, 600, 40));
        Assert.Equal(LMapHitKind.LMapHitNone, TInterface.TMapHoverResolve(map, 50, 10, 0, 40));
        Assert.Equal(LMapHitKind.LMapHitNone, TInterface.TMapLeaveResolve(map));
    }

    [Fact]
    public void BodyDrag_MovesTheSpool_AndRaisesDragNotices()
    {
        LFlow flow = TFlowBuild();
        LMap map = TInterface.TMapCreate(flow);
        List<bool> drags = [];
        TInterface.TFlowPlayAttach(flow, () => { }, () => { }, drags.Add, _ => { });

        Assert.True(TInterface.TMapPressHandle(map, 250, 5, 600, 40));
        Assert.Equal(LMapHitKind.LMapHitBody, map.LMapDragKind);
        Assert.True(TInterface.TMapMoveHandle(map, 260, 600));
        Assert.Equal(TimeSpan.FromSeconds(11), flow.LFlowSpool!.LSpoolRangeOrigin);
        Assert.Equal(TimeSpan.FromSeconds(41), flow.LFlowSpool.LSpoolRangeLimit);
        Assert.Equal(LMapHitKind.LMapHitBody, TInterface.TMapLeaveResolve(map));

        TInterface.TMapDragClear(map);
        Assert.Equal(LMapHitKind.LMapHitNone, map.LMapDragKind);
        Assert.False(TInterface.TMapMoveHandle(map, 300, 600));
        Assert.Equal([true, false], drags);
    }

    [Fact]
    public void GripDrag_ResizesOneEnd_AndCursorPressSeeks()
    {
        LFlow flow = TFlowBuild();
        LMap map = TInterface.TMapCreate(flow);
        List<TimeSpan> seeks = [];
        TInterface.TFlowCursorAttach(flow, () => { }, seeks.Add);

        TInterface.TMapPressHandle(map, 103, 10, 600, 40);
        TInterface.TMapMoveHandle(map, 163, 600);
        Assert.Equal(TimeSpan.FromSeconds(16), flow.LFlowSpool!.LSpoolRangeOrigin);
        Assert.Equal(TimeSpan.FromSeconds(40), flow.LFlowSpool.LSpoolRangeLimit);
        TInterface.TMapDragClear(map);

        TInterface.TMapPressHandle(map, 50, 30, 600, 40);
        Assert.Equal(LMapHitKind.LMapHitCursor, map.LMapDragKind);
        Assert.Equal(TimeSpan.FromSeconds(5), flow.LFlowCursor);
        TInterface.TMapMoveHandle(map, 600, 600);
        Assert.Equal(TimeSpan.FromSeconds(60), flow.LFlowCursor);
        Assert.Equal([TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(60)], seeks);

        LFlow bare = TInterface.TFlowCreate();
        Assert.False(TInterface.TMapPressHandle(TInterface.TMapCreate(bare), 10, 10, 600, 40));
    }

    [Fact]
    public void Frame_CarriesNavigatorShapes_AndRedrawsOnFlowNotices()
    {
        LFlow flow = TFlowBuild();
        LMap map = TInterface.TMapCreate(flow);
        int redraws = 0;
        TInterface.TMapFrameAttach(map, () => redraws++);

        LMapFrame frame = TInterface.TMapFrameResolve(map, 600, 40);
        Assert.Equal(2, frame.LMapFrameUnder.Count);
        Assert.Equal(LMapShapeKind.LMapShapeRail, frame.LMapFrameUnder[1].LMapShapeKind);
        LMapShape fill = Assert.Single(
            frame.LMapFrameOver, shape => shape.LMapShapeKind == LMapShapeKind.LMapShapeFill);
        Assert.Equal(100, fill.LMapShapeLeft);
        Assert.Equal(400, fill.LMapShapeRight);
        Assert.Contains(frame.LMapFrameOver, shape => shape.LMapShapeKind == LMapShapeKind.LMapShapeBorder);
        Assert.Contains(frame.LMapFrameOver, shape => shape.LMapShapeKind == LMapShapeKind.LMapShapeDot);
        Assert.Equal([0], frame.LMapFrameCursor);

        TInterface.TFlowCursorSeek(flow, TimeSpan.FromSeconds(30));
        Assert.Equal(1, redraws);
        Assert.Equal("cursor", map.LMapTrigger);
        Assert.Equal([300], TInterface.TMapFrameResolve(map, 600, 40).LMapFrameCursor);

        Assert.Empty(TInterface.TMapFrameResolve(map, 600, 8).LMapFrameOver);
        Assert.Empty(TInterface.TMapFrameResolve(map, 0, 40).LMapFrameUnder);
    }
}
