using System;
using System.Collections.Generic;
using Cadroue.Conduct;
using Cadroue.Core;
using Xunit;

namespace Cadroue.Tests;

public sealed class TKeyframeGate
{
    private readonly TFlowPort tKeyframeFlowPort = new();
    private readonly CFlow tKeyframeFlow;
    private readonly CKeyframe tKeyframeGate;
    private readonly List<string> tKeyframeTold = [];

    public TKeyframeGate()
    {
        tKeyframeFlow = TAtelier.TAtelierCreate(port => port with { LAtelierFlow = tKeyframeFlowPort })
            .CAtelierWorkspaceCreate()
            .CWorkspaceFlow;
        tKeyframeGate = tKeyframeFlow.CFlowKeyframe;
        tKeyframeGate.CKeyframeDeferred += () => tKeyframeTold.Add("defer");
        tKeyframeGate.CKeyframeResumed += delay => tKeyframeTold.Add($"resume:{delay.TotalSeconds}");
        tKeyframeGate.CKeyframeStopped += () => tKeyframeTold.Add("stop");
        tKeyframeGate.CKeyframeArrived += () => tKeyframeTold.Add("arrive");
        tKeyframeGate.CKeyframeChanged += () => tKeyframeTold.Add("change");
        tKeyframeFlow.CFlowCursorMoved += () => tKeyframeTold.Add("moved");
        tKeyframeFlow.CFlowCursorSought += cursor => tKeyframeTold.Add($"sought:{cursor.TotalSeconds}");
    }

    private TKeyframePort TKeyframePortRead() => tKeyframeFlowPort.TFlowKeyframe;

    private void TKeyframeLoad()
    {
        tKeyframeFlow.CFlowCommandSet(true);
        tKeyframeFlow.CFlowSourceSet(null!, "full:a");
        tKeyframeFlow.CFlowCursorSet(TimeSpan.FromSeconds(5));
        tKeyframeTold.Clear();
        TKeyframePortRead().TKeyframeCalls.Clear();
    }

    private static LKeyframeNotice TKeyframeNoticeCreate(int serial) =>
        new(serial, [new LKeyframeEntry(TimeSpan.FromSeconds(2))], [new LKeyframeScanRange(TimeSpan.Zero,
            TimeSpan.FromSeconds(10))]);

    [Fact]
    public void FlowCreate_Keyframe_TracesTheFlowsSourceText()
    {
        Assert.Equal(tKeyframeFlow.CFlowSourceFormat(), TKeyframePortRead().TKeyframeSource!());
    }

    [Fact]
    public void KeyframeDefer_WaitsOnlyWithCommandsSourceAndNoResume()
    {
        tKeyframeGate.CKeyframeDefer();
        Assert.Equal(["stop"], tKeyframeTold);

        TKeyframeLoad();
        tKeyframeGate.CKeyframeDefer();
        tKeyframeGate.CKeyframeSuspend(true);
        tKeyframeGate.CKeyframeDefer();

        Assert.Equal(["defer", "stop", "resume:2"], tKeyframeTold);
        Assert.True(tKeyframeGate.CKeyframeResumePending);
        Assert.Equal(["suspend", "delay"], TKeyframePortRead().TKeyframeCalls);
    }

    [Fact]
    public void KeyframeRun_LoadedFlow_StartsTheScanAtTheCursor()
    {
        TKeyframeLoad();
        tKeyframeGate.CKeyframeSuspend(true);
        tKeyframeGate.CKeyframeRun();

        Assert.False(tKeyframeGate.CKeyframeResumePending);
        Assert.Equal("start:full:a:5:60", TKeyframePortRead().TKeyframeCalls[^1]);

        tKeyframeFlow.CFlowUnloadSet();
        tKeyframeGate.CKeyframeRun();
        Assert.Equal(["suspend", "delay", "start:full:a:5:60"], TKeyframePortRead().TKeyframeCalls);
    }

    [Fact]
    public void KeyframeApply_EachNoticeOnce_StaleOnesChangeNothing()
    {
        TKeyframePortRead().TKeyframeRaise(TKeyframeNoticeCreate(0));
        Assert.Empty(tKeyframeTold);

        TKeyframeLoad();
        TKeyframePortRead().TKeyframeRaise(TKeyframeNoticeCreate(7));
        TKeyframePortRead().TKeyframeRaise(TKeyframeNoticeCreate(0));
        tKeyframeGate.CKeyframeApply();
        tKeyframeGate.CKeyframeApply();
        tKeyframeGate.CKeyframeApply();

        Assert.Equal(["arrive", "arrive", "change"], tKeyframeTold);
        Assert.Equal(["apply:7", "apply:0"], TKeyframePortRead().TKeyframeCalls);
        Assert.Single(tKeyframeGate.CKeyframeEntries);
    }

    [Fact]
    public void KeyframeMove_NotReady_WaitsAndTheNoticeLandsIt()
    {
        TKeyframeLoad();
        TKeyframePortRead().TKeyframeAnswer = (false, false, null);
        tKeyframeGate.CKeyframeNextMove();

        Assert.Equal(1, tKeyframeGate.CKeyframeDirection);
        Assert.Equal(["move:1:5", "start:full:a:5:60"], TKeyframePortRead().TKeyframeCalls);

        TKeyframePortRead().TKeyframeAnswer = (false, true, TimeSpan.FromSeconds(8));
        TKeyframePortRead().TKeyframeRaise(TKeyframeNoticeCreate(0));
        tKeyframeGate.CKeyframeApply();

        Assert.Null(tKeyframeGate.CKeyframeDirection);
        Assert.Equal(TimeSpan.FromSeconds(8), tKeyframeFlow.CFlowCursor);
        Assert.Equal(["stop", "arrive", "change", "moved", "stop", "sought:8"], tKeyframeTold);
    }

    [Fact]
    public void KeyframeMove_Failed_DropsTheWaitingMove()
    {
        TKeyframeLoad();
        tKeyframeGate.CKeyframeDirectionSet(-1);
        TKeyframePortRead().TKeyframeAnswer = (true, false, null);
        tKeyframeGate.CKeyframeNearestMove();

        Assert.Null(tKeyframeGate.CKeyframeDirection);
        Assert.Equal(["move:0:5"], TKeyframePortRead().TKeyframeCalls);
        Assert.Empty(tKeyframeTold);
    }

    [Fact]
    public void FlowGates_CarryTheKeyframeSteps()
    {
        TKeyframeLoad();
        tKeyframeFlow.CFlowCursorUpdate(TimeSpan.FromSeconds(6));
        tKeyframeFlow.CFlowCursorSeek(TimeSpan.FromSeconds(7));
        tKeyframeFlow.CFlowCommandSet(false);
        Assert.True(tKeyframeFlow.CFlowClear(true));
        Assert.True(tKeyframeFlow.CFlowClose());

        Assert.Equal(
            ["moved", "defer", "stop", "resume:2", "moved", "sought:7", "stop", "stop", "stop", "stop", "stop"],
            tKeyframeTold);
        Assert.Equal(
            ["sync:6", "suspend", "delay", "suspend", "suspend", "clear", "close"],
            TKeyframePortRead().TKeyframeCalls);
    }
}
