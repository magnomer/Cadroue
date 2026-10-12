using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Schedule")]
public sealed class TWorkspaceSnapshot
{
    private static QWorkspace TWorkspaceBuild(string key = "Convert")
    {
        QWorkspace workspace = TInterface.TWorkspaceCreate(key, null);
        TInterface.TWorkspaceAttach(workspace);
        TInterface.TFlowEditSet(workspace.QWorkspaceFlow!, true);
        return workspace;
    }

    [Fact]
    public void Create_DerivesFactsFromKey()
    {
        using var schedule = new TSchedule();
        TInterfaceRoster.TStationAttach(schedule.TScheduleRead());
        QWorkspace split = TInterface.TWorkspaceCreate("Split", null);
        QWorkspace audio = TInterface.TWorkspaceCreate("Audio", null);
        QWorkspace merge = TInterface.TWorkspaceCreate("Merge", null);
        QWorkspace worklist = TInterface.TWorkspaceCreate("Worklist", null);

        try
        {
            Assert.True(split.QWorkspaceFacts.QWorkspaceSectionVisible);
            Assert.False(audio.QWorkspaceFacts.QWorkspaceSectionVisible);
            Assert.True(audio.QWorkspaceFacts.QWorkspaceAudioOnly);
            Assert.True(split.QWorkspaceFacts.QWorkspaceSourcePresent);
            Assert.False(merge.QWorkspaceFacts.QWorkspaceSourcePresent);
            Assert.False(worklist.QWorkspaceFacts.QWorkspaceSourcePresent);
            Assert.True(split.QWorkspaceFlowPresent);
            Assert.False(worklist.QWorkspaceFlowPresent);
            Assert.True(split.QWorkspaceFacts.QWorkspaceListPresent);
            Assert.False(worklist.QWorkspaceFacts.QWorkspaceListPresent);
            Assert.False(TInterface.TWorkspaceBusyCheck(split));
        }
        finally
        {
            TInterface.TWorkspaceClose(worklist);
        }
    }

    [Fact]
    public void PresetChange_RecordsHistory_UndoAndRedoRoundTrip()
    {
        QWorkspace workspace = TWorkspaceBuild();
        string initial = workspace.QWorkspacePreset.LPresetDisplay;
        Assert.False(TInterface.TWorkspaceUndo(workspace));

        TInterface.TWorkspacePresetSelect(workspace, TInterface.TPresetRecordCreate("first"));
        Assert.Equal("first", workspace.QWorkspacePreset.LPresetDisplay);
        TInterface.TWorkspacePresetSelect(workspace, TInterface.TPresetRecordCreate("second"));
        Assert.Equal("second", workspace.QWorkspacePreset.LPresetDisplay);

        Assert.True(TInterface.TWorkspaceUndo(workspace));
        Assert.Equal("first", workspace.QWorkspacePreset.LPresetDisplay);
        Assert.True(TInterface.TWorkspaceUndo(workspace));
        Assert.Equal(initial, workspace.QWorkspacePreset.LPresetDisplay);
        Assert.False(TInterface.TWorkspaceUndo(workspace));

        Assert.True(TInterface.TWorkspaceRedo(workspace));
        Assert.Equal("first", workspace.QWorkspacePreset.LPresetDisplay);
        Assert.True(TInterface.TWorkspaceRedo(workspace));
        Assert.Equal("second", workspace.QWorkspacePreset.LPresetDisplay);
        Assert.False(TInterface.TWorkspaceRedo(workspace));
        TInterface.TWorkspaceClose(workspace);
    }

    [Fact]
    public void Undo_WhileApplying_DoesNotRecordTheApply()
    {
        QWorkspace workspace = TWorkspaceBuild();
        TInterface.TWorkspacePresetSelect(workspace, TInterface.TPresetRecordCreate("first"));
        TInterface.TWorkspacePresetSelect(workspace, TInterface.TPresetRecordCreate("second"));

        Assert.True(TInterface.TWorkspaceUndo(workspace));
        Assert.True(TInterface.TWorkspaceRedo(workspace));
        Assert.Equal("second", workspace.QWorkspacePreset.LPresetDisplay);
        Assert.False(TInterface.TWorkspaceRedo(workspace));
        TInterface.TWorkspaceClose(workspace);
    }

    [Fact]
    public void Snapshot_CarriesSegmentSections()
    {
        QWorkspace workspace = TInterface.TWorkspaceCreate("Split", null);
        TInterface.TWorkspaceAttach(workspace);
        LSegment segment = TInterface.TWorkspaceSegmentRead(workspace);
        Interface.TInterface.TSegmentBoundSet(
            segment,
            [TInterface.TPieceCreate(TimeSpan.Zero, TimeSpan.FromSeconds(2))],
            0,
            TimeSpan.FromSeconds(10));

        LHistoryEntry snapshot = TInterface.TWorkspaceSnapshotRead(workspace);

        Assert.Single(snapshot.LHistorySections);
        Assert.Equal(0, snapshot.LHistorySectionIndex);
        TInterface.TWorkspaceClose(workspace);
    }

    [Fact]
    public void PathHandle_BlankPath_AsksMediaClose()
    {
        QWorkspace workspace = TWorkspaceBuild();
        int closes = 0;
        TInterface.TWorkspaceCloseAttach(workspace, () => closes++);

        TInterface.TWorkspacePathHandle(workspace, "C:/media/a.mp4");
        Assert.Equal(0, closes);
        TInterface.TWorkspacePathHandle(workspace, "  ");
        Assert.Equal(1, closes);
        TInterface.TWorkspacePathHandle(workspace, null);
        Assert.Equal(2, closes);
    }

    [Fact]
    public void MediaClear_RemovesStaleDocketPaths_AndAsksClose()
    {
        QWorkspace workspace = TInterface.TWorkspaceCreate("Convert", null);
        LDocket docket = workspace.QWorkspaceDocket!;
        TInterfaceList.TDocketPathsAdd(docket, "C:/media/a.mp4", "C:/media/b.mp4");
        TInterface.TWorkspaceAttach(workspace);
        int closes = 0;
        TInterface.TWorkspaceCloseAttach(workspace, () => closes++);

        Assert.True(TInterface.TWorkspaceMediaClear(workspace, new HashSet<Guid>()));

        Assert.Equal(2, closes);
        Assert.Empty(TInterface.TDocketPathsRead(docket));
        Assert.False(TInterface.TWorkspaceMediaClear(workspace, new HashSet<Guid>()));
    }
}
