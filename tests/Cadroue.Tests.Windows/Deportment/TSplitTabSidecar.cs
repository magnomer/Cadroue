using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TSplitTabSidecar
{
    private const string TSplitSource = @"C:\media\clip.mp4";

    private static (QSplitTab, QInspector, QViewer, LDocket) TSplitBuild()
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        QList list = TInterfaceList.TListCreate();
        LDocket docket = list.QListDocket;
        QSplitTab tab = TInterface.TSplitTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"), viewer, list, TInterface.TFlowCreate());
        QInspector inspector = TInterface.TSplitInspectorRead(tab);
        return (tab, inspector, viewer, docket);
    }

    private static void TSplitSourceSet(QViewer viewer)
    {
        LMediaInfo info = TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080);
        TInterfaceViewer.TViewerMediaCommit(viewer, TInterfaceViewer.TCargoCreate(TSplitSource, info, true), false);
    }

    private static LSidecarSplitEntry TSplitRecordCreate(bool enabled) => Interface.TInterface.TDetectorSidecarFormat(
        Interface.TInterface.TDetectorSetCreate(
            [TInterface.TDetectorStepCreate(LDetectorKind.LDetectorKindSilence, enabled, -35, 2, 0)],
            TInterface.TDetectorBlankCreate(false, LDetectorType.LDetectorTypeColor, 200, 0.6, 0.9, 0.2, 0.8, 3),
            LDetectorStillMode.LDetectorStillTreat,
            LDetectorLuminanceMode.LDetectorLuminanceFast,
            LDetectorMetricMode.LDetectorMetricRms,
            new Dictionary<LDetectorKind, string>()));

    private static List<(string, LSidecarSplitEntry?)> TSplitWriterAttach(LSidecarSplitEntry? stored)
    {
        var written = new List<(string, LSidecarSplitEntry?)>();
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
        (QSplitTab tab, QInspector inspector, QViewer viewer, _) = TSplitBuild();
        List<(string, LSidecarSplitEntry?)> written = TSplitWriterAttach(null);
        try
        {
            TSplitSourceSet(viewer);
            TInterface.TSensorEnabledSet(inspector.QInspectorSensor, LDetectorKind.LDetectorKindScene, true);
            written.Clear();

            TInterface.TSplitStateSave(tab);
        }
        finally
        {
            TInterface.TSplitLibrarianAttach(null, null);
        }

        (string path, LSidecarSplitEntry? record) = Assert.Single(written);
        Assert.Equal(TSplitSource, path);
        Assert.True(record!.LSidecarSplitActive);
        Assert.Equal(6, record.LSidecarSplitDetectors.Count);
    }

    [Fact]
    public void StateSave_Suspended_WritesNothing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, QInspector inspector, QViewer viewer, _) = TSplitBuild();
        List<(string, LSidecarSplitEntry?)> written = TSplitWriterAttach(null);
        try
        {
            TSplitSourceSet(viewer);
            TInterface.TSensorEnabledSet(inspector.QInspectorSensor, LDetectorKind.LDetectorKindScene, true);
            written.Clear();
            TInterfaceInspector.TInspectorSaveSuspend(inspector);

            TInterface.TSplitStateSave(tab);
            TInterfaceInspector.TInspectorSaveResume(inspector);
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
        (QSplitTab tab, QInspector inspector, _, _) = TSplitBuild();
        List<(string, LSidecarSplitEntry?)> written = TSplitWriterAttach(null);
        try
        {
            TInterface.TSensorEnabledSet(inspector.QInspectorSensor, LDetectorKind.LDetectorKindScene, true);

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
        (QSplitTab tab, QInspector inspector, QViewer viewer, LDocket docket) = TSplitBuild();
        List<(string, LSidecarSplitEntry?)> written = TSplitWriterAttach(null);
        try
        {
            TInterfaceList.TDocketDeliveredAdd(docket, TSplitSource, true);
            TSplitSourceSet(viewer);
            TInterface.TSensorEnabledSet(inspector.QInspectorSensor, LDetectorKind.LDetectorKindScene, true);

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
        (QSplitTab tab, _, QViewer viewer, _) = TSplitBuild();
        List<(string, LSidecarSplitEntry?)> written = TSplitWriterAttach(null);
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
        (QSplitTab tab, _, QViewer viewer, _) = TSplitBuild();
        List<(string, LSidecarSplitEntry?)> written = TSplitWriterAttach(TSplitRecordCreate(true));
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

        (_, LSidecarSplitEntry? record) = Assert.Single(written);
        Assert.False(record!.LSidecarSplitActive);
    }

    [Fact]
    public void StateLoad_AppliesSensorAndBlankInsideSuspendScope()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, QInspector inspector, _, _) = TSplitBuild();
        List<(string, LSidecarSplitEntry?)> written = TSplitWriterAttach(TSplitRecordCreate(true));
        try
        {
            TInterface.TSplitStateLoad(tab, TSplitSource);
        }
        finally
        {
            TInterface.TSplitLibrarianAttach(null, null);
        }

        QSensor sensor = inspector.QInspectorSensor;
        LDetectorStep silence = TInterface.TSensorStepRead(sensor, LDetectorKind.LDetectorKindSilence);
        Assert.True(silence.LDetectorStepEnabled);
        Assert.Equal(-35, silence.LDetectorStepThreshold);
        Assert.Equal(2, silence.LDetectorStepMinimum);
        Assert.Equal(LDetectorStillMode.LDetectorStillDiscard, sensor.QSensorMode);
        Assert.Equal(200, TInterface.TBlankStepRead(inspector.QInspectorBlank).LDetectorBlankHue);
        Assert.False(inspector.QInspectorSaveSuspended);
        Assert.Empty(written);
    }

    [Fact]
    public void StateLoad_NothingStored_LeavesStateAlone()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, QInspector inspector, _, _) = TSplitBuild();
        List<(string, LSidecarSplitEntry?)> written = TSplitWriterAttach(null);
        try
        {
            TInterface.TSplitStateLoad(tab, TSplitSource);
        }
        finally
        {
            TInterface.TSplitLibrarianAttach(null, null);
        }

        Assert.False(TInterface.TSensorStepRead(
            inspector.QInspectorSensor, LDetectorKind.LDetectorKindSilence).LDetectorStepEnabled);
        Assert.Empty(written);
    }

    [Fact]
    public void PathHandle_NotPersistent_LoadsSidecar()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (_, QInspector inspector, QViewer viewer, _) = TSplitBuild();
        TSplitWriterAttach(TSplitRecordCreate(true));
        try
        {
            TInterface.TSplitPathHandle(viewer, TSplitSource);
        }
        finally
        {
            TInterface.TSplitLibrarianAttach(null, null);
        }

        Assert.True(TInterface.TSensorStepRead(
            inspector.QInspectorSensor, LDetectorKind.LDetectorKindSilence).LDetectorStepEnabled);
    }

    [Fact]
    public void PathHandle_Persistent_KeepsState()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (_, QInspector inspector, QViewer viewer, _) = TSplitBuild();
        TInterface.TSensorPersistentSet(inspector.QInspectorSensor, true);
        TSplitWriterAttach(TSplitRecordCreate(true));
        try
        {
            TInterface.TSplitPathHandle(viewer, TSplitSource);
        }
        finally
        {
            TInterface.TSplitLibrarianAttach(null, null);
        }

        Assert.False(TInterface.TSensorStepRead(
            inspector.QInspectorSensor, LDetectorKind.LDetectorKindSilence).LDetectorStepEnabled);
    }
}
