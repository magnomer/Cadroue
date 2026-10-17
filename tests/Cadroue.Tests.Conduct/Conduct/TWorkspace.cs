using System;
using System.Collections.Generic;
using Cadroue.Conduct;
using Cadroue.Tests.Interface;
using Xunit;

namespace Cadroue.Tests;

public sealed class TWorkspace
{
    private readonly TWorkspacePort tWorkspacePort = new();
    private readonly CWorkspace tWorkspaceArea;
    private readonly List<string> tWorkspaceHeard = [];

    public TWorkspace()
    {
        void TWorkspaceHeardRecord(string kind, TimeSpan position)
        {
            tWorkspaceHeard.Add($"{kind}:{position.TotalSeconds}");
        }
        tWorkspaceArea = TAtelier.TAtelierCreate(port => port with { LAtelierWorkspace = tWorkspacePort })
            .CAtelierWorkspaceCreate();
        tWorkspaceArea.CWorkspaceOpened += path => tWorkspaceHeard.Add("open:" + path);
        tWorkspaceArea.CWorkspacePlayed += () => tWorkspaceHeard.Add("play");
        tWorkspaceArea.CWorkspacePaused += () => tWorkspaceHeard.Add("pause");
        tWorkspaceArea.CWorkspaceSought += position => TWorkspaceHeardRecord("seek", position);
        tWorkspaceArea.CWorkspaceIntentSought += position => TWorkspaceHeardRecord("kept", position);
        tWorkspaceArea.CWorkspaceRestored += path => tWorkspaceHeard.Add("restore:" + path);
        tWorkspaceArea.CWorkspaceCommitPlayed += position => TWorkspaceHeardRecord("commit-play", position);
        tWorkspaceArea.CWorkspaceCommitPaused += position => TWorkspaceHeardRecord("commit-pause", position);
    }

    [Fact]
    public void AtelierWorkspaceCreate_EachCall_GivesItsOwnWorkspace()
    {
        CAtelier atelier = TAtelier.TAtelierCreate(port => port with { LAtelierWorkspace = tWorkspacePort });

        Assert.NotSame(atelier.CAtelierWorkspaceCreate(), atelier.CAtelierWorkspaceCreate());
    }

    [Fact]
    public void WorkspaceRemovalRead_ReportedIntent_AsksThePortOnceWithItsOwnState()
    {
        tWorkspaceArea.CWorkspaceStart("a");

        (bool cancelled, bool closed) = tWorkspaceArea.CWorkspaceRemovalRead(["full:a", "b"]);

        Assert.True(cancelled);
        Assert.False(closed);
        Assert.Equal(["removal:full:a,b|full:a|"], tWorkspacePort.TWorkspaceCalls);
    }

    [Fact]
    public void WorkspaceRemovalRead_NullPaths_ThrowsWithoutAskingThePort()
    {
        Assert.Throws<ArgumentNullException>(() => tWorkspaceArea.CWorkspaceRemovalRead(null!));
        Assert.Empty(tWorkspacePort.TWorkspaceCalls);
    }

    [Fact]
    public void WorkspaceClearRead_ReportedMedia_AsksThePortOnceAndReturnsItsAnswer()
    {
        tWorkspaceArea.CWorkspaceMediaSet("src", false);
        tWorkspaceArea.CWorkspaceStart("int");

        (bool closed, bool cancelled, bool cleared) = tWorkspaceArea.CWorkspaceClearRead(["k"]);

        Assert.False(closed);
        Assert.True(cancelled);
        Assert.True(cleared);
        Assert.Equal(["clear:k|src|full:int|True"], tWorkspacePort.TWorkspaceCalls);
    }

    [Fact]
    public void WorkspaceClearRead_MediaInfoOnly_CountsAsLoaded()
    {
        tWorkspaceArea.CWorkspaceMediaSet(null, true);

        tWorkspaceArea.CWorkspaceClearRead([]);

        Assert.Equal(["clear:|||True"], tWorkspacePort.TWorkspaceCalls);
    }

    [Fact]
    public void WorkspaceMediaSet_PendingIntent_ConsumesIt()
    {
        tWorkspaceArea.CWorkspaceStart("int");
        tWorkspaceArea.CWorkspaceMediaSet("src", true);

        tWorkspaceArea.CWorkspaceRemovalRead(["int"]);

        Assert.Equal(["removal:int||src"], tWorkspacePort.TWorkspaceCalls);
    }

    [Fact]
    public void WorkspaceMediaClear_LoadedMedia_ForgetsRequestSourceIntentAndMedia()
    {
        tWorkspaceArea.CWorkspaceRequestSet("req");
        tWorkspaceArea.CWorkspaceMediaSet("src", true);
        tWorkspaceArea.CWorkspaceStart("int");
        tWorkspaceArea.CWorkspaceMediaClear();

        tWorkspaceArea.CWorkspaceClearRead([]);

        Assert.Equal(["clear:|||False"], tWorkspacePort.TWorkspaceCalls);
        tWorkspaceArea.CWorkspaceOpen("req");
        Assert.Equal(["open:req"], tWorkspaceHeard);
    }

    [Fact]
    public void WorkspaceClearRead_NullKept_ThrowsWithoutAskingThePort()
    {
        Assert.Throws<ArgumentNullException>(() => tWorkspaceArea.CWorkspaceClearRead(null!));
        Assert.Empty(tWorkspacePort.TWorkspaceCalls);
    }

    [Fact]
    public void WorkspacePlaySeek_PendingIntent_EditsTheOnlyCopyAndRaisesOnlyTheKeptSeek()
    {
        tWorkspaceArea.CWorkspaceStart("a");

        tWorkspaceArea.CWorkspacePlay(true);
        Assert.True(tWorkspaceArea.CWorkspaceIntent!.CWorkspaceIntentPlaying);
        tWorkspaceArea.CWorkspacePlay(false);
        tWorkspaceArea.CWorkspaceSeek(TimeSpan.FromSeconds(9));

        Assert.Equal(new CWorkspaceIntent("full:a", TimeSpan.FromSeconds(9), false), tWorkspaceArea.CWorkspaceIntent);
        Assert.Equal(["kept:9"], tWorkspaceHeard);
    }

    [Fact]
    public void WorkspacePlaySeek_NoPendingIntent_RaisesForThePlayerAndKeepsNoIntent()
    {
        tWorkspaceArea.CWorkspacePlay(true);
        tWorkspaceArea.CWorkspacePlay(false);
        tWorkspaceArea.CWorkspaceSeek(TimeSpan.FromSeconds(1));

        Assert.Null(tWorkspaceArea.CWorkspaceIntent);
        Assert.Equal(["play", "pause", "seek:1"], tWorkspaceHeard);
    }

    [Fact]
    public void WorkspaceMediaSet_Committed_ReadsSourceAndMediaAndConsumesIntent()
    {
        tWorkspaceArea.CWorkspaceStart("int");
        tWorkspaceArea.CWorkspaceMediaSet("src", true);

        Assert.Equal("src", tWorkspaceArea.CWorkspaceSource);
        Assert.True(tWorkspaceArea.CWorkspaceMedia);
        Assert.Null(tWorkspaceArea.CWorkspaceIntent);
    }

    [Fact]
    public void WorkspaceStart_Pending_ReplacesTheIntentWholeAtTheStart()
    {
        tWorkspaceArea.CWorkspaceStart("a");
        tWorkspaceArea.CWorkspaceSeek(TimeSpan.FromSeconds(4));

        tWorkspaceArea.CWorkspaceStart("b");

        Assert.Equal(new CWorkspaceIntent("full:b", TimeSpan.Zero, null), tWorkspaceArea.CWorkspaceIntent);
    }

    [Fact]
    public void WorkspaceRestore_HeldSource_LoadsItAgainAtThePlace()
    {
        Assert.False(tWorkspaceArea.CWorkspaceRestore(TimeSpan.FromSeconds(2), true));
        Assert.Null(tWorkspaceArea.CWorkspaceIntent);

        tWorkspaceArea.CWorkspaceMediaSet("src", true);

        Assert.True(tWorkspaceArea.CWorkspaceRestore(TimeSpan.FromSeconds(2), true));
        Assert.Equal(new CWorkspaceIntent("full:src", TimeSpan.FromSeconds(2), true), tWorkspaceArea.CWorkspaceIntent);
        Assert.Equal(["restore:full:src"], tWorkspaceHeard);
    }

    [Fact]
    public void WorkspaceRestore_PendingIntent_KeepsIt()
    {
        tWorkspaceArea.CWorkspaceMediaSet("src", true);
        tWorkspaceArea.CWorkspaceStart("a");

        Assert.True(tWorkspaceArea.CWorkspaceRestore(TimeSpan.FromSeconds(2), true));
        Assert.Equal(new CWorkspaceIntent("full:a", TimeSpan.Zero, null), tWorkspaceArea.CWorkspaceIntent);
        Assert.Equal(["restore:full:a"], tWorkspaceHeard);
    }

    [Fact]
    public void WorkspaceIdleRestore_PendingIntent_AsksNothingAgain()
    {
        Assert.False(tWorkspaceArea.CWorkspaceIdleRestore(TimeSpan.FromSeconds(2), false));

        tWorkspaceArea.CWorkspaceMediaSet("src", true);
        tWorkspaceArea.CWorkspaceStart("a");

        Assert.True(tWorkspaceArea.CWorkspaceIdleRestore(TimeSpan.FromSeconds(2), false));
        Assert.Equal(new CWorkspaceIntent("full:a", TimeSpan.Zero, null), tWorkspaceArea.CWorkspaceIntent);
        Assert.Empty(tWorkspaceHeard);

        tWorkspaceArea.CWorkspaceCancel();

        Assert.True(tWorkspaceArea.CWorkspaceIdleRestore(TimeSpan.FromSeconds(2), false));
        Assert.Equal(["restore:full:src"], tWorkspaceHeard);
    }

    [Fact]
    public void WorkspaceCommitApply_ConsumedIntent_RaisesItsPlaceOnce()
    {
        tWorkspaceArea.CWorkspaceStart("a");
        tWorkspaceArea.CWorkspaceSeek(TimeSpan.FromSeconds(3));
        tWorkspaceArea.CWorkspacePlay(false);
        tWorkspaceHeard.Clear();

        tWorkspaceArea.CWorkspaceMediaSet("full:a", true);
        tWorkspaceArea.CWorkspaceCommitApply(true, null);
        tWorkspaceArea.CWorkspaceCommitApply(true, null);

        Assert.Null(tWorkspaceArea.CWorkspaceIntent);
        Assert.Equal(["commit-pause:3", "commit-play:0"], tWorkspaceHeard);
    }

    [Fact]
    public void WorkspaceIntentMatch_PendingPath_MatchesInAnyCaseOnly()
    {
        Assert.False(tWorkspaceArea.CWorkspaceIntentMatch("full:a"));

        tWorkspaceArea.CWorkspaceStart("a");

        Assert.True(tWorkspaceArea.CWorkspaceIntentMatch("FULL:A"));
        Assert.False(tWorkspaceArea.CWorkspaceIntentMatch("full:b"));
    }

    [Fact]
    public void WorkspaceCancel_PendingOnly_DropsTheIntentAndKeepsTheMedia()
    {
        tWorkspaceArea.CWorkspaceMediaSet("src", true);

        Assert.False(tWorkspaceArea.CWorkspaceCancel());

        tWorkspaceArea.CWorkspaceStart("a");

        Assert.True(tWorkspaceArea.CWorkspaceCancel());
        Assert.Null(tWorkspaceArea.CWorkspaceIntent);
        Assert.Equal("src", tWorkspaceArea.CWorkspaceSource);
    }

    [Fact]
    public void WorkspaceOpen_AskedPath_OpensUnlessBlankOrHeldOrLoading()
    {
        tWorkspaceArea.CWorkspaceRequestSet("C:\\clip.cad");

        tWorkspaceArea.CWorkspaceOpen("  ");
        tWorkspaceArea.CWorkspaceOpen(null);
        tWorkspaceArea.CWorkspaceOpen("C:\\clip.cad");
        Assert.Equal(["open:C:\\clip.cad"], tWorkspaceHeard);

        tWorkspaceArea.CWorkspaceStart("C:\\clip.mp4");
        tWorkspaceArea.CWorkspaceOpen("c:\\CLIP.CAD");
        tWorkspaceArea.CWorkspaceOpen("C:\\clip.mp4");

        Assert.Equal(["open:C:\\clip.cad", "open:C:\\clip.mp4"], tWorkspaceHeard);
    }
}
