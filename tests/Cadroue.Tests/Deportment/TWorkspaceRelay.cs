using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TWorkspaceRelay
{
    [Fact]
    public void Create_CarriesKeyPresetAndDocketPaths()
    {
        LDocket docket = TInterface.TDocketCreate();
        TInterface.TDocketPathsAdd(docket, "C:/media/a.mp4", "C:/media/b.mp4");
        LWorkspace workspace = TInterface.TWorkspaceCreate("Convert", null);
        TInterface.TWorkspaceAttach(workspace, docket, null, null, null);

        LRelay relay = TInterface.TWorkspaceRelayCreate(workspace, "Custom", 40, 60);

        Assert.Equal("Convert", relay.LRelayLayoutKey);
        Assert.Equal("Custom", relay.LRelayCustomName);
        Assert.Equal(40, relay.LRelayDropLeft);
        Assert.Equal(60, relay.LRelayDropTop);
        Assert.Equal(["C:/media/a.mp4", "C:/media/b.mp4"], relay.LRelayPaths);
        Assert.Equal(string.Empty, relay.LRelaySourcePath);
        Assert.Empty(relay.LRelaySections);
    }

    [Fact]
    public void Apply_WithPaths_DefersToPathsAdd_ThenSelectsListedSource()
    {
        LDocket source = TInterface.TDocketCreate();
        TInterface.TDocketPathsAdd(source, "C:/media/a.mp4");
        LWorkspace origin = TInterface.TWorkspaceCreate("Convert", null);
        TInterface.TWorkspaceAttach(origin, source, null, null, null);
        LRelay relay = TInterface.TWorkspaceRelayCreate(origin, string.Empty, 0, 0);
        relay.LRelaySourcePath = "C:/media/a.mp4";

        LDocket target = TInterface.TDocketCreate();
        LViewer viewer = TInterface.TViewerCreate();
        LWorkspace workspace = TInterface.TWorkspaceCreate("Convert", null);
        TInterface.TWorkspaceAttach(workspace, target, null, null, viewer);
        List<string> added = [];
        List<string> selected = [];
        List<string> opened = [];
        TInterface.TWorkspacePathsAttach(workspace, paths => added.AddRange(paths));
        TInterface.TWorkspaceSelectAttach(workspace, selected.Add);
        TInterface.TWorkspaceOpenAttach(workspace, opened.Add);

        TInterface.TWorkspaceRelayApply(workspace, relay);
        Assert.Equal(["C:/media/a.mp4"], added);
        Assert.Empty(selected);
        Assert.True(workspace.LWorkspaceRelayPending);

        TInterface.TDocketPathsAdd(target, "C:/media/a.mp4");
        TInterface.TWorkspaceSourceRun(workspace);
        Assert.Equal(["C:/media/a.mp4"], selected);
        Assert.Empty(opened);
    }

    [Fact]
    public void Apply_WithoutList_OpensSourceDirectly()
    {
        LViewer viewer = TInterface.TViewerCreate();
        LWorkspace workspace = TInterface.TWorkspaceCreate("Convert", null);
        TInterface.TWorkspaceAttach(workspace, null, null, null, viewer);
        LRelay relay = TInterface.TWorkspaceRelayCreate(workspace, string.Empty, 0, 0);
        relay.LRelaySourcePath = "C:/media/a.mp4";
        List<string> opened = [];
        TInterface.TWorkspaceOpenAttach(workspace, opened.Add);

        TInterface.TWorkspaceRelayApply(workspace, relay);

        Assert.Equal(["C:/media/a.mp4"], opened);
    }

    [Fact]
    public void Restore_RaisesVolumeSeekAndRange_OnlyWhenCarried()
    {
        LWorkspace workspace = TInterface.TWorkspaceCreate("Convert", null);
        TInterface.TWorkspaceAttach(workspace, null, null, null, null);
        LRelay relay = TInterface.TWorkspaceRelayCreate(workspace, string.Empty, 0, 0);
        List<double> volumes = [];
        List<TimeSpan> seeks = [];
        List<(TimeSpan, TimeSpan)> ranges = [];
        TInterface.TWorkspaceVolumeAttach(workspace, volumes.Add);
        TInterface.TWorkspaceSeekAttach(workspace, seeks.Add);
        TInterface.TWorkspaceRangeAttach(workspace, (origin, limit) => ranges.Add((origin, limit)));

        TInterface.TWorkspaceRelayRestore(workspace, relay, TimeSpan.FromSeconds(10));
        Assert.Empty(volumes);
        Assert.Empty(seeks);
        Assert.Empty(ranges);

        relay.LRelayVolume = 0.5;
        relay.LRelayPositionTicks = TimeSpan.FromSeconds(3).Ticks;
        relay.LRelayOriginTicks = TimeSpan.FromSeconds(1).Ticks;
        relay.LRelayLimitTicks = TimeSpan.FromSeconds(9).Ticks;
        TInterface.TWorkspaceRelayRestore(workspace, relay, TimeSpan.FromSeconds(10));
        Assert.Equal([0.5], volumes);
        Assert.Equal([TimeSpan.FromSeconds(3)], seeks);
        Assert.Empty(ranges);
    }

    [Fact]
    public void Restore_WithFlow_AppliesRange()
    {
        LFlow flow = TInterface.TFlowCreate();
        LSegment segment = TInterface.TSegmentCreate();
        LWorkspace workspace = TInterface.TWorkspaceCreate("Split", null);
        TInterface.TWorkspaceAttach(workspace, null, segment, flow, null);
        LRelay relay = TInterface.TWorkspaceRelayCreate(workspace, string.Empty, 0, 0);
        relay.LRelayOriginTicks = TimeSpan.FromSeconds(1).Ticks;
        relay.LRelayLimitTicks = TimeSpan.FromSeconds(9).Ticks;
        List<(TimeSpan, TimeSpan)> ranges = [];
        TInterface.TWorkspaceRangeAttach(workspace, (origin, limit) => ranges.Add((origin, limit)));

        TInterface.TWorkspaceRelayRestore(workspace, relay, TimeSpan.FromSeconds(10));

        Assert.Equal([(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(9))], ranges);
    }
}
