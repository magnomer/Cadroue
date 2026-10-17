using System;
using System.Collections.Generic;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TViewerGate
{
    private readonly TViewerPort tViewerPort = new();
    private readonly CWorkspace tViewerWorkspace;
    private readonly CViewer tViewerGate;
    private readonly List<string> tViewerHeard = [];

    public TViewerGate()
    {
        tViewerWorkspace = TAtelier.TAtelierCreate(port => port with { LAtelierViewer = tViewerPort })
            .CAtelierWorkspaceCreate();
        tViewerGate = tViewerWorkspace.CWorkspaceViewer;
        tViewerWorkspace.CWorkspacePlayed += () => tViewerHeard.Add("play");
        tViewerWorkspace.CWorkspacePaused += () => tViewerHeard.Add("pause");
        tViewerWorkspace.CWorkspaceSought += position => tViewerHeard.Add($"seek:{position.TotalSeconds}");
    }

    private void TViewerCommandSet(bool active, bool restored = true) =>
        tViewerGate.CViewerCommandSet(
            active,
            () => tViewerHeard.Add($"suspend:{tViewerGate.CViewerCommandActive}"),
            () =>
            {
                tViewerHeard.Add($"restore:{tViewerGate.CViewerCommandActive}");
                return restored;
            },
            () => tViewerHeard.Add("resume"));

    [Fact]
    public void CommandSet_On_RestoresWithCommandOn_ResumesOnlyWhenNothingRestored()
    {
        TViewerCommandSet(true, restored: false);

        Assert.True(tViewerGate.CViewerCommandActive);
        Assert.Equal(["restore:True", "resume"], tViewerHeard);
    }

    [Fact]
    public void CommandSet_Off_SuspendsFirst_ThenOutdatesTheSerial()
    {
        TViewerCommandSet(true);
        int serial = tViewerGate.CViewerSerial;
        tViewerHeard.Clear();

        TViewerCommandSet(false);

        Assert.Equal(["suspend:True"], tViewerHeard);
        Assert.False(tViewerGate.CViewerCommandActive);
        Assert.Equal(serial + 1, tViewerGate.CViewerSerial);
    }

    [Fact]
    public void CommandSet_RepeatOrUnloaded_RunsNothing()
    {
        TViewerCommandSet(true);
        tViewerHeard.Clear();

        TViewerCommandSet(true);
        tViewerWorkspace.CWorkspaceClose(() => { }, () => { });
        TViewerCommandSet(false);

        Assert.Empty(tViewerHeard);
        Assert.True(tViewerGate.CViewerCommandActive);
    }

    [Fact]
    public void CommandSet_Verbose_RecordsTheChangeWithItsDetail()
    {
        tViewerPort.TViewerVerbose = true;
        tViewerGate.CViewerHostAttach(() => "facts");

        TViewerCommandSet(true);

        Assert.Equal(["host:1|command set on|facts"], tViewerPort.TViewerCalls);
    }

    [Fact]
    public void HostShow_RaisesWithTheNewState_RecordsAChangeForFlyleafOnly()
    {
        tViewerPort.TViewerVerbose = true;
        tViewerGate.CViewerHostAttach(() => "facts");

        tViewerGate.CViewerHostShow(true, () => tViewerHeard.Add($"raise:{tViewerGate.CViewerHostVisible}"));
        tViewerGate.CViewerHostShow(true, () => { });
        tViewerGate.CViewerMpvSet(true);
        tViewerGate.CViewerHostShow(false, () => { });

        Assert.Equal(["raise:True"], tViewerHeard);
        Assert.False(tViewerGate.CViewerHostVisible);
        Assert.Equal(["host:1|host shown|facts"], tViewerPort.TViewerCalls);
    }

    [Fact]
    public void HostRecord_NoDetailAttached_CountsTheStampOnly()
    {
        tViewerPort.TViewerVerbose = true;

        tViewerGate.CViewerHostRecord("panel loaded");
        tViewerGate.CViewerHostAttach(() => "facts");
        tViewerGate.CViewerHostRecord("host loaded");

        Assert.Equal(["host:2|host loaded|facts"], tViewerPort.TViewerCalls);
    }

    [Fact]
    public void Close_Once_UnloadsOutdatesAndRunsEachStage_AFailingStageTraced()
    {
        tViewerWorkspace.CWorkspaceMediaSet("a.mp4", true);
        int serial = tViewerGate.CViewerSerial;

        tViewerWorkspace.CWorkspaceClose(
            () => throw new InvalidOperationException("probe gone"), () => tViewerHeard.Add("player"));
        tViewerWorkspace.CWorkspaceClose(() => tViewerHeard.Add("again"), () => tViewerHeard.Add("again"));

        Assert.True(tViewerGate.CViewerUnloaded);
        Assert.Equal(serial + 1, tViewerGate.CViewerSerial);
        Assert.Null(tViewerWorkspace.CWorkspaceSource);
        Assert.False(tViewerWorkspace.CWorkspaceMedia);
        Assert.Equal(["player"], tViewerHeard);
        Assert.Equal(["stage:media probe|probe gone"], tViewerPort.TViewerCalls);
    }

    [Fact]
    public void MediaClose_OutdatesTheSerialAndForgetsTheMedia()
    {
        tViewerWorkspace.CWorkspaceMediaSet("a.mp4", true);
        int serial = tViewerGate.CViewerSerial;

        tViewerWorkspace.CWorkspaceMediaClose();

        Assert.Equal(serial + 1, tViewerGate.CViewerSerial);
        Assert.Null(tViewerWorkspace.CWorkspaceSource);
        Assert.False(tViewerWorkspace.CWorkspaceMedia);
    }

    [Fact]
    public void SerialCheck_HoldsOnlyTheCurrentSerialWhileLoadedAndActive()
    {
        int serial = tViewerGate.CViewerSerialChange();
        Assert.False(tViewerGate.CViewerSerialCheck(serial));

        TViewerCommandSet(true);
        Assert.True(tViewerGate.CViewerSerialCheck(serial));
        Assert.False(tViewerGate.CViewerSerialCheck(serial - 1));

        tViewerWorkspace.CWorkspaceClose(() => { }, () => { });
        Assert.False(tViewerGate.CViewerSerialCheck(tViewerGate.CViewerSerial));
    }

    [Fact]
    public void LoupeAttach_HidesTheHostThenSuspends()
    {
        TViewerCommandSet(true);
        tViewerGate.CViewerHostShow(true, () => { });
        tViewerHeard.Clear();

        tViewerGate.CViewerLoupeAttach(
            () => tViewerHeard.Add($"raise:{tViewerGate.CViewerHostVisible}"),
            () => tViewerHeard.Add($"suspend:{tViewerGate.CViewerCommandActive}"));

        Assert.Equal(["raise:False", "suspend:True"], tViewerHeard);
        Assert.False(tViewerGate.CViewerCommandActive);
    }

    [Fact]
    public void LoupeDetach_TurnsOnShowsThenSeeksAndPlaysOrPauses()
    {
        void TViewerDetach(double seconds, bool playing) =>
            tViewerWorkspace.CWorkspaceLoupeDetach(
                TimeSpan.FromSeconds(seconds),
                playing,
                () =>
                {
                    tViewerHeard.Add("restore");
                    return false;
                },
                () => tViewerHeard.Add("resume"),
                () => tViewerHeard.Add($"raise:{tViewerGate.CViewerHostVisible}"));

        TViewerDetach(3, true);
        TViewerDetach(5, false);

        Assert.Equal(
            ["restore", "resume", "raise:True", "seek:3", "play", "raise:True", "seek:5", "pause"], tViewerHeard);
    }

    [Fact]
    public void LoupeDetach_Unloaded_ShowsTheHostOnly()
    {
        tViewerWorkspace.CWorkspaceClose(() => { }, () => { });

        tViewerWorkspace.CWorkspaceLoupeDetach(
            TimeSpan.FromSeconds(3),
            true,
            () => true,
            () => tViewerHeard.Add("resume"),
            () => tViewerHeard.Add("raise"));

        Assert.Equal(["raise"], tViewerHeard);
    }
}
