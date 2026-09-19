using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TWorkspaceSnapshot
{
    private static LWorkspace TWorkspaceBuild(string key = "Convert")
    {
        LWorkspace workspace = TInterface.TWorkspaceCreate(key, null);
        TInterface.TWorkspaceAttach(workspace, null, null, null, null);
        return workspace;
    }

    [Fact]
    public void Create_DerivesFactsFromKey()
    {
        LWorkspace split = TInterface.TWorkspaceCreate("Split", null);
        LWorkspace audio = TInterface.TWorkspaceCreate("Audio", null);
        LWorkspace merge = TInterface.TWorkspaceCreate("Merge", null);
        LWorkspace worklist = TInterface.TWorkspaceCreate("Worklist", null);

        Assert.True(split.LWorkspaceSectionVisible);
        Assert.False(audio.LWorkspaceSectionVisible);
        Assert.True(audio.LWorkspaceAudioOnly);
        Assert.True(split.LWorkspaceSourcePresent);
        Assert.False(merge.LWorkspaceSourcePresent);
        Assert.False(worklist.LWorkspaceSourcePresent);
        Assert.False(split.LWorkspaceFlowPresent);
        Assert.False(split.LWorkspaceListPresent);
        Assert.False(TInterface.TWorkspaceBusyCheck(split));
    }

    [Fact]
    public void PresetChange_RecordsHistory_UndoAndRedoRoundTrip()
    {
        LWorkspace workspace = TWorkspaceBuild();
        string initial = workspace.LWorkspacePreset.LPresetDisplay;
        Assert.False(TInterface.TWorkspaceUndo(workspace));

        TInterface.TWorkspacePresetSelect(workspace, TInterface.TPresetRecordCreate("first"));
        Assert.Equal("first", workspace.LWorkspacePreset.LPresetDisplay);
        TInterface.TWorkspacePresetSelect(workspace, TInterface.TPresetRecordCreate("second"));
        Assert.Equal("second", workspace.LWorkspacePreset.LPresetDisplay);

        Assert.True(TInterface.TWorkspaceUndo(workspace));
        Assert.Equal("first", workspace.LWorkspacePreset.LPresetDisplay);
        Assert.True(TInterface.TWorkspaceUndo(workspace));
        Assert.Equal(initial, workspace.LWorkspacePreset.LPresetDisplay);
        Assert.False(TInterface.TWorkspaceUndo(workspace));

        Assert.True(TInterface.TWorkspaceRedo(workspace));
        Assert.Equal("first", workspace.LWorkspacePreset.LPresetDisplay);
        Assert.True(TInterface.TWorkspaceRedo(workspace));
        Assert.Equal("second", workspace.LWorkspacePreset.LPresetDisplay);
        Assert.False(TInterface.TWorkspaceRedo(workspace));
        TInterface.TWorkspaceClose(workspace);
    }

    [Fact]
    public void Undo_WhileApplying_DoesNotRecordTheApply()
    {
        LWorkspace workspace = TWorkspaceBuild();
        TInterface.TWorkspacePresetSelect(workspace, TInterface.TPresetRecordCreate("first"));
        TInterface.TWorkspacePresetSelect(workspace, TInterface.TPresetRecordCreate("second"));

        Assert.True(TInterface.TWorkspaceUndo(workspace));
        Assert.True(TInterface.TWorkspaceRedo(workspace));
        Assert.Equal("second", workspace.LWorkspacePreset.LPresetDisplay);
        Assert.False(TInterface.TWorkspaceRedo(workspace));
        TInterface.TWorkspaceClose(workspace);
    }

    [Fact]
    public void Snapshot_CarriesSegmentSections()
    {
        LSegment segment = TInterface.TSegmentCreate();
        LWorkspace workspace = TInterface.TWorkspaceCreate("Split", null);
        TInterface.TWorkspaceAttach(workspace, null, segment, null, null);
        TInterface.TSegmentBoundSet(
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
        LWorkspace workspace = TWorkspaceBuild();
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
        LDocket docket = TInterface.TDocketCreate();
        TInterface.TDocketPathsAdd(docket, "C:/media/a.mp4", "C:/media/b.mp4");
        LWorkspace workspace = TInterface.TWorkspaceCreate("Convert", null);
        TInterface.TWorkspaceAttach(workspace, docket, null, null, null);
        int closes = 0;
        TInterface.TWorkspaceCloseAttach(workspace, () => closes++);

        Assert.True(TInterface.TWorkspaceMediaClear(workspace, new HashSet<Guid>()));

        Assert.Equal(1, closes);
        Assert.Empty(TInterface.TDocketPathsRead(docket));
        Assert.False(TInterface.TWorkspaceMediaClear(workspace, new HashSet<Guid>()));
    }
}
