using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TSplitTabSidecar
{
    private const string TSplitSource = @"C:\media\clip.mp4";

    private static (LSplitTab, LInspector, LViewer, LDocket) TSplitBuild()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        LViewer viewer = TInterface.TViewerCreate();
        LDocket docket = TInterface.TDocketCreate();
        LSplitTab tab = TInterface.TSplitTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"),
            inspector,
            viewer,
            TInterface.TListCreate(docket),
            docket,
            TInterface.TProcessingCreate(),
            TInterface.TFlowCreate());
        return (tab, inspector, viewer, docket);
    }

    private static void TSplitSourceSet(LViewer viewer)
    {
        LMediaInfo info = TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080);
        TInterface.TViewerMediaCommit(viewer, TInterface.TCargoCreate(TSplitSource, info, true), false);
    }

    private static LSidecarSplitRecord TSplitRecordCreate(bool enabled) => TInterface.TDetectorSidecarFormat(
        TInterface.TDetectorSetCreate(
            [TInterface.TDetectorStepCreate(LDetectorKind.LDetectorKindSilence, enabled, -35, 2, 0)],
            TInterface.TDetectorBlankCreate(false, LDetectorType.LDetectorTypeColor, 200, 0.6, 0.9, 0.2, 0.8, 3),
            LDetectorStillMode.LDetectorStillTreat,
            LDetectorLuminanceMode.LDetectorLuminanceFast,
            LDetectorMetricMode.LDetectorMetricRms,
            new Dictionary<LDetectorKind, string>()));

    private static List<(string, LSidecarSplitRecord?)> TSplitWriterAttach(LSidecarSplitRecord? stored)
    {
        var written = new List<(string, LSidecarSplitRecord?)>();
        TInterface.TSplitLibrarianAttach(_ => stored, (path, record) =>
        {
            written.Add((path, record));
            return true;
        });
        return written;
    }

    [Fact]
    public void StateSave_ActiveDetector_WritesRecordThroughLibrarian()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, LInspector inspector, LViewer viewer, _) = TSplitBuild();
        List<(string, LSidecarSplitRecord?)> written = TSplitWriterAttach(null);
        try
        {
            TSplitSourceSet(viewer);
            TInterface.TSensorEnabledSet(inspector.LInspectorSensor, LDetectorKind.LDetectorKindScene, true);
            written.Clear();

            TInterface.TSplitStateSave(tab);
        }
        finally
        {
            TInterface.TSplitLibrarianAttach(null, null);
        }

        (string path, LSidecarSplitRecord? record) = Assert.Single(written);
        Assert.Equal(TSplitSource, path);
        Assert.True(record!.LSidecarSplitActive);
        Assert.Equal(6, record.LSidecarSplitDetectors.Count);
    }

    [Fact]
    public void StateSave_Suspended_WritesNothing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, LInspector inspector, LViewer viewer, _) = TSplitBuild();
        List<(string, LSidecarSplitRecord?)> written = TSplitWriterAttach(null);
        try
        {
            TSplitSourceSet(viewer);
            TInterface.TSensorEnabledSet(inspector.LInspectorSensor, LDetectorKind.LDetectorKindScene, true);
            written.Clear();
            TInterface.TInspectorSaveSuspend(inspector);

            TInterface.TSplitStateSave(tab);
            TInterface.TInspectorSaveResume(inspector);
        }
        finally
        {
            TInterface.TSplitLibrarianAttach(null, null);
        }

        Assert.Empty(written);
    }

    [Fact]
    public void StateSave_NoSource_WritesNothing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, LInspector inspector, _, _) = TSplitBuild();
        List<(string, LSidecarSplitRecord?)> written = TSplitWriterAttach(null);
        try
        {
            TInterface.TSensorEnabledSet(inspector.LInspectorSensor, LDetectorKind.LDetectorKindScene, true);

            TInterface.TSplitStateSave(tab);
        }
        finally
        {
            TInterface.TSplitLibrarianAttach(null, null);
        }

        Assert.Empty(written);
    }

    [Fact]
    public void StateSave_LockedPath_WritesNothing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, LInspector inspector, LViewer viewer, LDocket docket) = TSplitBuild();
        List<(string, LSidecarSplitRecord?)> written = TSplitWriterAttach(null);
        try
        {
            TInterface.TDocketDeliveredAdd(docket, TSplitSource, true);
            TSplitSourceSet(viewer);
            TInterface.TSensorEnabledSet(inspector.LInspectorSensor, LDetectorKind.LDetectorKindScene, true);

            TInterface.TSplitStateSave(tab);
        }
        finally
        {
            TInterface.TSplitLibrarianAttach(null, null);
        }

        Assert.Empty(written);
    }

    [Fact]
    public void StateSave_InactiveAndAbsent_WritesNothing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, _, LViewer viewer, _) = TSplitBuild();
        List<(string, LSidecarSplitRecord?)> written = TSplitWriterAttach(null);
        try
        {
            TSplitSourceSet(viewer);

            TInterface.TSplitStateSave(tab);
        }
        finally
        {
            TInterface.TSplitLibrarianAttach(null, null);
        }

        Assert.Empty(written);
    }

    [Fact]
    public void StateSave_InactiveButStored_OverwritesRecord()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, _, LViewer viewer, _) = TSplitBuild();
        List<(string, LSidecarSplitRecord?)> written = TSplitWriterAttach(TSplitRecordCreate(true));
        try
        {
            TSplitSourceSet(viewer);
            written.Clear();

            TInterface.TSplitStateSave(tab);
        }
        finally
        {
            TInterface.TSplitLibrarianAttach(null, null);
        }

        (_, LSidecarSplitRecord? record) = Assert.Single(written);
        Assert.False(record!.LSidecarSplitActive);
    }

    [Fact]
    public void StateLoad_AppliesSensorAndBlankInsideSuspendScope()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, LInspector inspector, _, _) = TSplitBuild();
        List<(string, LSidecarSplitRecord?)> written = TSplitWriterAttach(TSplitRecordCreate(true));
        try
        {
            TInterface.TSplitStateLoad(tab, TSplitSource);
        }
        finally
        {
            TInterface.TSplitLibrarianAttach(null, null);
        }

        LSensor sensor = inspector.LInspectorSensor;
        LDetectorStep silence = TInterface.TSensorStepRead(sensor, LDetectorKind.LDetectorKindSilence);
        Assert.True(silence.LDetectorStepEnabled);
        Assert.Equal(-35, silence.LDetectorStepThreshold);
        Assert.Equal(2, silence.LDetectorStepMinimum);
        Assert.Equal(LDetectorStillMode.LDetectorStillDiscard, sensor.LSensorMode);
        Assert.Equal(200, TInterface.TBlankStepRead(inspector.LInspectorBlank).LDetectorBlankHue);
        Assert.False(inspector.LInspectorSaveSuspended);
        Assert.Empty(written);
    }

    [Fact]
    public void StateLoad_NothingStored_LeavesStateAlone()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, LInspector inspector, _, _) = TSplitBuild();
        List<(string, LSidecarSplitRecord?)> written = TSplitWriterAttach(null);
        try
        {
            TInterface.TSplitStateLoad(tab, TSplitSource);
        }
        finally
        {
            TInterface.TSplitLibrarianAttach(null, null);
        }

        Assert.False(TInterface.TSensorStepRead(
            inspector.LInspectorSensor, LDetectorKind.LDetectorKindSilence).LDetectorStepEnabled);
        Assert.Empty(written);
    }

    [Fact]
    public void PathHandle_NotPersistent_LoadsSidecar()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, LInspector inspector, _, _) = TSplitBuild();
        TSplitWriterAttach(TSplitRecordCreate(true));
        try
        {
            TInterface.TSplitPathHandle(tab, TSplitSource);
        }
        finally
        {
            TInterface.TSplitLibrarianAttach(null, null);
        }

        Assert.True(TInterface.TSensorStepRead(
            inspector.LInspectorSensor, LDetectorKind.LDetectorKindSilence).LDetectorStepEnabled);
    }

    [Fact]
    public void PathHandle_Persistent_KeepsState()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, LInspector inspector, _, _) = TSplitBuild();
        TInterface.TSensorPersistentSet(inspector.LInspectorSensor, true);
        TSplitWriterAttach(TSplitRecordCreate(true));
        try
        {
            TInterface.TSplitPathHandle(tab, TSplitSource);
        }
        finally
        {
            TInterface.TSplitLibrarianAttach(null, null);
        }

        Assert.False(TInterface.TSensorStepRead(
            inspector.LInspectorSensor, LDetectorKind.LDetectorKindSilence).LDetectorStepEnabled);
    }
}
