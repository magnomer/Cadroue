using System;
using System.Collections.Generic;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TMapGate
{
    private readonly TMapPort tMapPort = new();
    private readonly CFlow tMapFlow;
    private readonly CMap tMapGate;
    private readonly List<bool> tMapDrags = [];
    private int tMapSpools;

    public TMapGate()
    {
        tMapFlow = TAtelier.TAtelierCreate(port => port with { LAtelierMap = tMapPort })
            .CAtelierWorkspaceCreate()
            .CWorkspaceFlow;
        tMapGate = tMapFlow.CFlowMap;
        tMapGate.CMapDrag.CMapDragChanged += tMapDrags.Add;
        tMapGate.CMapDrag.CMapSpoolChanged += () => tMapSpools++;
    }

    [Fact]
    public void MapDraw_NoRange_OnlyTheBackground_RecordsTheDraw()
    {
        CMapFrame frame = TMapFrameRead(600, 40);

        Assert.True(frame.CMapFrameSized);
        Assert.False(frame.CMapFrameRailed);
        Assert.Empty(frame.CMapFrameBands);
        Assert.Null(frame.CMapFrameBody);
        Assert.Equal([(TimeSpan.Zero, (string?)null, "attach", 5d, 0)], tMapPort.TMapRecords);
    }

    [Fact]
    public void MapDraw_EmptySize_DrawsNothing()
    {
        TMapRangeSet();

        Assert.False(TMapFrameRead(0, 40).CMapFrameSized);
        Assert.False(TMapFrameRead(600, 8).CMapFrameRailed);
    }

    [Fact]
    public void MapDraw_Range_AnswersBandsScansBodyAndCursor()
    {
        TMapRangeSet();
        tMapFlow.CFlowCursorSet(TimeSpan.FromSeconds(15));
        tMapPort.TMapSections =
        [
            (TimeSpan.Zero, TimeSpan.FromSeconds(30), 2, false),
            (TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30), 1, false),
            (TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60), 3, true),
        ];
        tMapPort.TMapCoverage = [(TimeSpan.Zero, TimeSpan.FromSeconds(6))];
        tMapGate.CMapTriggerSet("sections");

        CMapFrame frame = TMapFrameRead(600, 40);

        Assert.True(frame.CMapFrameRailed);
        Assert.Equal(34, frame.CMapFrameBottom);
        Assert.Equal((36d, 39d), frame.CMapFrameCoverage);
        Assert.Equal(
            [new CMapBand(0, 4, 300, 33, 2, "1", true, false), new CMapBand(300, 4, 600, 33, 3, "3", false, false)],
            frame.CMapFrameBands);
        Assert.Equal([(0d, 60d)], frame.CMapFrameScans);
        Assert.Equal((100d, 300d, 31d), frame.CMapFrameBody);
        Assert.Equal([150d], frame.CMapFrameCursor);
        Assert.Equal(2, tMapGate.CMapGlyphCount);
        Assert.Equal("sections", tMapPort.TMapRecords[0].Item3);
    }

    [Fact]
    public void MapPress_NoRange_Refuses()
    {
        Assert.False(tMapGate.CMapDrag.CMapPressHandle(103, 10, 600, 40));
        Assert.False(tMapGate.CMapDrag.CMapMoveHandle(160, 600));
        Assert.Empty(tMapDrags);
    }

    [Fact]
    public void MapHover_FindsHandles_Body_AndCursor()
    {
        TMapRangeSet();

        Assert.Equal(CMapHit.CMapHitOrigin, tMapGate.CMapDrag.CMapHoverResolve(103, 10, 600, 40));
        Assert.Equal(CMapHit.CMapHitLimit, tMapGate.CMapDrag.CMapHoverResolve(397, 10, 600, 40));
        Assert.Equal(CMapHit.CMapHitBody, tMapGate.CMapDrag.CMapHoverResolve(250, 5, 600, 40));
        Assert.Equal(CMapHit.CMapHitCursor, tMapGate.CMapDrag.CMapHoverResolve(50, 10, 600, 40));
        Assert.Equal(CMapHit.CMapHitNone, tMapGate.CMapDrag.CMapHoverResolve(50, 10, 0, 40));
        Assert.Equal(CMapHit.CMapHitNone, tMapGate.CMapDrag.CMapLeaveResolve());
    }

    [Fact]
    public void MapMove_OriginHandle_SetsTheHeldEdgePlusTheDrag()
    {
        TMapRangeSet();

        Assert.True(tMapGate.CMapDrag.CMapPressHandle(103, 10, 600, 40));
        Assert.True(tMapGate.CMapDrag.CMapMoveHandle(163, 600));

        Assert.Equal(CMapHit.CMapHitOrigin, tMapGate.CMapDrag.CMapDragKind);
        Assert.Equal(["origin 00:00:16"], tMapPort.TMapCalls);
        Assert.Equal([true], tMapDrags);
        Assert.Equal(1, tMapSpools);
        Assert.Equal(CMapHit.CMapHitOrigin, tMapGate.CMapDrag.CMapLeaveResolve());
    }

    [Fact]
    public void MapMove_Body_StepsFromThePreviousPoint()
    {
        TMapRangeSet();

        tMapGate.CMapDrag.CMapPressHandle(250, 5, 600, 40);
        tMapGate.CMapDrag.CMapMoveHandle(280, 600);
        tMapGate.CMapDrag.CMapMoveHandle(280, 600);

        Assert.Equal(["move 00:00:03", "move 00:00:00"], tMapPort.TMapCalls);
        Assert.Equal(2, tMapSpools);
    }

    [Fact]
    public void MapPress_Cursor_SeeksTheFlowAfterTellingTheDrag()
    {
        TMapRangeSet();
        tMapFlow.CFlowCommandSet(true);

        tMapGate.CMapDrag.CMapPressHandle(50, 10, 600, 40);

        Assert.Equal(CMapHit.CMapHitCursor, tMapGate.CMapDrag.CMapDragKind);
        Assert.Equal(TimeSpan.FromSeconds(5), tMapFlow.CFlowCursor);
        Assert.Equal([true], tMapDrags);
        Assert.Empty(tMapPort.TMapCalls);
    }

    [Fact]
    public void MapDragClear_TellsTheEndOnce()
    {
        TMapRangeSet();
        tMapGate.CMapDrag.CMapPressHandle(250, 5, 600, 40);

        tMapGate.CMapDrag.CMapDragClear();
        tMapGate.CMapDrag.CMapDragClear();

        Assert.Equal([true, false], tMapDrags);
        Assert.Equal(CMapHit.CMapHitNone, tMapGate.CMapDrag.CMapDragKind);
        Assert.False(tMapGate.CMapDrag.CMapMoveHandle(300, 600));
    }

    [Fact]
    public void MapMoveResolve_NoRoomBesideTheSides_IsEmpty() =>
        Assert.Equal((0d, 0d, 0d), CMap.CMapMoveResolve(100, 20, 31, 12, 600));

    [Fact]
    public void AtelierCreate_NoMapPort_Throws() =>
        Assert.Throws<ArgumentNullException>(() => TAtelier.TAtelierCreate(port => port with { LAtelierMap = null! }));

    private void TMapRangeSet()
    {
        tMapFlow.CFlowSourceSet(null!, "full:a");
        Conduct.TInterface.TSpoolRangeSet(tMapFlow.CFlowRange!, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(40));
    }

    private CMapFrame TMapFrameRead(double width, double height)
    {
        CMapFrame? frame = null;
        tMapGate.CMapDraw(width, height, painted => frame = painted, () => 5);
        return frame!;
    }
}
