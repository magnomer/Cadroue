using System;
using System.Collections.Generic;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TFlowGate
{
    private readonly TFlowPort tFlowPort = new() { TFlowWaveform = true };
    private readonly CFlow tFlowGate;

    public TFlowGate()
    {
        tFlowGate = TAtelier.TAtelierCreate(port => port with { LAtelierFlow = tFlowPort })
            .CAtelierWorkspaceCreate()
            .CWorkspaceFlow;
    }

    [Fact]
    public void FlowCreate_Port_StartsFromTheWaveformPreference()
    {
        Assert.True(tFlowGate.CFlowWaveformActive);
        Assert.Equal(1, tFlowGate.CFlowPaletteCount);
        Assert.False(tFlowGate.CFlowSourceCheck());
    }

    [Fact]
    public void FlowAttach_CommandsStopped_RefusesAndHoldsNothing()
    {
        Assert.False(tFlowGate.CFlowAttach(null!, "full:a", TimeSpan.FromSeconds(5)));

        Assert.False(tFlowGate.CFlowLoaded);
        Assert.Null(tFlowGate.CFlowSource);
        Assert.DoesNotContain("media", tFlowPort.TFlowCalls);
    }

    [Fact]
    public void FlowAttach_SameSource_ResumesAtTheHeldCursor()
    {
        tFlowGate.CFlowCommandSet(true);
        Assert.True(tFlowGate.CFlowAttach(null!, "full:a", TimeSpan.FromSeconds(5)));
        Assert.False(tFlowGate.CFlowResumed);
        Assert.Equal(TimeSpan.FromSeconds(5), tFlowGate.CFlowCursor);

        Assert.True(tFlowGate.CFlowAttach(null!, "FULL:A", TimeSpan.FromSeconds(9)));

        Assert.True(tFlowGate.CFlowResumed);
        Assert.Equal(TimeSpan.FromSeconds(5), tFlowGate.CFlowCursor);
        Assert.True(tFlowGate.CFlowWaveformAudio);
        Assert.True(tFlowGate.CFlowScanCheck());
    }

    [Fact]
    public void FlowCursorClamp_Range_HoldsTheCursorInside()
    {
        Assert.Equal(TimeSpan.Zero, tFlowGate.CFlowCursorClamp(TimeSpan.FromSeconds(3)));

        tFlowGate.CFlowCommandSet(true);
        tFlowGate.CFlowAttach(null!, "full:a", TimeSpan.Zero);

        Assert.Equal(TimeSpan.Zero, tFlowGate.CFlowCursorClamp(TimeSpan.FromSeconds(-1)));
        Assert.Equal(TimeSpan.FromSeconds(60), tFlowGate.CFlowCursorClamp(TimeSpan.FromSeconds(99)));
        Assert.True(tFlowGate.CFlowCursorUpdate(TimeSpan.FromSeconds(30)));
        Assert.Equal(TimeSpan.FromSeconds(30), tFlowGate.CFlowCursor);
    }

    [Fact]
    public void FlowClear_Held_ClearsOnceThenRefuses()
    {
        Assert.False(tFlowGate.CFlowClear(sectionsEmpty: true));
        Assert.True(tFlowGate.CFlowClear(sectionsEmpty: false));

        tFlowGate.CFlowCommandSet(true);
        tFlowGate.CFlowAttach(null!, "full:a", TimeSpan.FromSeconds(4));
        tFlowGate.CFlowKeyframe.CKeyframeDirectionSet(1);

        Assert.True(tFlowGate.CFlowClear(sectionsEmpty: true));
        Assert.Null(tFlowGate.CFlowSource);
        Assert.Equal(TimeSpan.Zero, tFlowGate.CFlowCursor);
        Assert.Null(tFlowGate.CFlowKeyframe.CKeyframeDirection);
        Assert.False(tFlowGate.CFlowClear(sectionsEmpty: true));
    }

    [Fact]
    public void FlowActionResolve_Token_NeedsCommandsRangeAndEdit()
    {
        Assert.Equal(CFlowAction.CFlowActionNone, tFlowGate.CFlowActionResolve("ZoomIn", true));

        tFlowGate.CFlowCommandSet(true);
        tFlowGate.CFlowAttach(null!, "full:a", TimeSpan.Zero);

        Assert.Equal(CFlowAction.CFlowActionInward, tFlowGate.CFlowActionResolve("ZoomIn", false));
        Assert.Equal(CFlowAction.CFlowActionNone, tFlowGate.CFlowActionResolve("SectionAdd", false));
        Assert.Equal(CFlowAction.CFlowActionAddition, tFlowGate.CFlowActionResolve("SectionAdd", true));
        Assert.Equal(CFlowAction.CFlowActionNext, tFlowGate.CFlowActionResolve("KeyframeNext", false));
        Assert.Equal(CFlowAction.CFlowActionNone, tFlowGate.CFlowActionResolve("Unknown", true));
    }

    [Fact]
    public void FlowCommandSet_Stopped_DropsTheWaitingMoveAndThePlay()
    {
        int played = 0;
        tFlowGate.CFlowPlayed += () => played++;
        tFlowGate.CFlowCommandSet(true);
        tFlowGate.CFlowKeyframe.CKeyframeDirectionSet(-1);
        tFlowGate.CFlowPlayRaise();

        tFlowGate.CFlowCommandSet(false);
        tFlowGate.CFlowPlayRaise();

        Assert.Equal(1, played);
        Assert.Null(tFlowGate.CFlowKeyframe.CKeyframeDirection);
        Assert.False(tFlowGate.CFlowCursorUpdate(TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void FlowWaveformSet_Change_IsToldOnce()
    {
        int told = 0;
        tFlowGate.CFlowWaveformChanged += _ => told++;

        Assert.False(tFlowGate.CFlowWaveformSet(true));
        Assert.True(tFlowGate.CFlowWaveformSet(false));

        Assert.Equal(1, told);
        Assert.False(tFlowGate.CFlowWaveformActive);
    }

    [Fact]
    public void FlowCloseAndPalette_Facts_HoldTheirFloor()
    {
        tFlowGate.CFlowPaletteApply(0);

        Assert.Equal(1, tFlowGate.CFlowPaletteCount);
        Assert.True(tFlowGate.CFlowClose());
        Assert.False(tFlowGate.CFlowClose());
        Assert.True(tFlowGate.CFlowMapFirst);
    }

    [Fact]
    public void FlowDragSet_Playing_PausesOnceAndPlaysAtTheEnd()
    {
        int paused = 0;
        int played = 0;
        int asked = 0;
        List<bool> drags = [];
        tFlowGate.CFlowPaused += () => paused++;
        tFlowGate.CFlowPlayed += () => played++;
        tFlowGate.CFlowDrag.CFlowDragChanged += drags.Add;
        Func<bool> playing = () => ++asked > 0;
        tFlowGate.CFlowCommandSet(true);

        tFlowGate.CFlowDrag.CFlowDragSet(true, playing);
        tFlowGate.CFlowDrag.CFlowDragSet(true, playing);
        Assert.True(tFlowGate.CFlowDrag.CFlowDragPaused);
        tFlowGate.CFlowDrag.CFlowDragSet(false, playing);
        tFlowGate.CFlowDrag.CFlowDragSet(false, playing);

        Assert.Equal([true, true, false, false], drags);
        Assert.Equal((1, 1, 1), (paused, played, asked));
        Assert.False(tFlowGate.CFlowDrag.CFlowDragPaused);
    }

    [Fact]
    public void FlowDragSet_PreferenceOffOrNotPlaying_LeavesThePlay()
    {
        int paused = 0;
        tFlowGate.CFlowPaused += () => paused++;
        tFlowGate.CFlowCommandSet(true);

        tFlowGate.CFlowDrag.CFlowDragSet(true, null);
        tFlowPort.TFlowDrag = false;
        tFlowGate.CFlowDrag.CFlowDragSet(true, () => true);

        Assert.Equal(0, paused);
        Assert.False(tFlowGate.CFlowDrag.CFlowDragPaused);
    }

    [Fact]
    public void FlowNameStart_NoSectionAtTheSelection_Refuses()
    {
        CFlowName name = tFlowGate.CFlowName;

        Assert.False(name.CFlowNameStart());
        Assert.DoesNotContain("name:0", tFlowPort.TFlowCalls);

        tFlowPort.TFlowSection.TSectionSelection = 0;

        Assert.False(name.CFlowNameStart());
        Assert.Contains("name:0", tFlowPort.TFlowCalls);
        Assert.Null(name.CFlowNameIndex);
    }

    [Fact]
    public void FlowNameStart_Open_ClosesThenShowsTheNames()
    {
        CFlowName name = tFlowGate.CFlowName;
        List<string> told = [];
        name.CFlowNameShown += () => told.Add($"shown:{name.CFlowNameIndex}:{name.CFlowNameText}");
        name.CFlowNameClosed += () => told.Add("closed");
        tFlowPort.TFlowSection.TSectionSelection = 1;
        tFlowPort.TFlowName = ("Intro", "", "b");

        Assert.True(name.CFlowNameStart());
        Assert.True(name.CFlowNameStart());

        Assert.Equal(["shown:1:Intro", "closed", "shown:1:Intro"], told);
        Assert.Equal(("", "b"), (name.CFlowNamePrefix, name.CFlowNameSuffix));
        Assert.Equal((false, true), (name.CFlowPrefixShown, name.CFlowSuffixShown));
    }

    [Fact]
    public void FlowNameCommit_Open_KeepsTrimmedNamesAndCloses()
    {
        CFlowName name = tFlowGate.CFlowName;
        int closed = 0;
        name.CFlowNameClosed += () => closed++;
        tFlowPort.TFlowSection.TSectionSelection = 0;
        tFlowPort.TFlowSection.TSectionCount = 1;
        tFlowPort.TFlowName = ("a", "", "");

        name.CFlowNameCommit(true, " lost ", "", "");
        name.CFlowNameStart();
        name.CFlowNameCommit(true, " named ", " p ", "");
        name.CFlowNameHide();

        Assert.Contains("name:0:named:p:", tFlowPort.TFlowSection.TSectionCalls);
        Assert.DoesNotContain("name:0:lost::", tFlowPort.TFlowSection.TSectionCalls);
        Assert.Equal(1, closed);
        Assert.Null(name.CFlowNameIndex);
    }
}
