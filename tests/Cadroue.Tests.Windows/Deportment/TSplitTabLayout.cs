using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TSplitTabLayout
{
    private static (LSplitTab, LInspector, LProcessing) TSplitBuild()
    {
        LList list = TInterfaceList.TListCreate();
        LSplitTab tab = TInterface.TSplitTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"),
            TInterfaceViewer.TViewerCreate(),
            list,
            TInterface.TFlowCreate());
        LInspector inspector = TInterface.TSplitInspectorRead(tab);
        LProcessing processing = TInterface.TSplitProcessingRead(tab);
        foreach (LProcessingRow row in TInterface.TSplitRowsRead())
        {
            TInterface.TProcessingStepAdd(processing, row.LProcessingRowKey);
        }

        return (tab, inspector, processing);
    }

    [Fact]
    public void Rows_OneStepPerKind_KeyedBySensorName()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        IReadOnlyList<LProcessingRow> rows = TInterface.TSplitRowsRead();

        Assert.Equal(6, rows.Count);
        foreach (LDetectorKind kind in LDetector.LDetectorKinds)
        {
            Assert.Contains(rows, row => row.LProcessingRowKey == TInterface.TSensorNameRead(kind));
            Assert.Equal(kind, TInterface.TSensorKindRead(TInterface.TSensorNameRead(kind)));
        }
    }

    [Fact]
    public void LayoutRead_CarriesEveryDetectorAndPersistent()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, LInspector inspector, _) = TSplitBuild();
        TInterface.TSensorStepSet(
            inspector.LInspectorSensor,
            TInterface.TDetectorStepCreate(LDetectorKind.LDetectorKindSilence, true, -40, 1.5, 0));
        TInterface.TBlankStepSet(
            inspector.LInspectorBlank,
            TInterface.TDetectorBlankCreate(true, LDetectorType.LDetectorTypeColor, 90, 0.4, 0.7, 0.1, 0.9, 1));
        TInterface.TSensorPersistentSet(inspector.LInspectorSensor, true);

        LSceneTabEntry layout = TInterface.TSplitLayoutRead(tab);

        Assert.Equal(6, layout.LSceneDetectors.Count);
        Assert.True(layout.LSceneDetectPersistent);
        LSceneDetector silence = layout.LSceneDetectors.Single(
            detector => detector.LSceneDetectorKind == (int)LDetectorKind.LDetectorKindSilence);
        Assert.True(silence.LSceneDetectorEnabled);
        Assert.Equal(-40, silence.LSceneDetectorThreshold);
        LSceneDetector blank = layout.LSceneDetectors.Single(
            detector => detector.LSceneDetectorKind == (int)LDetectorKind.LDetectorKindBlank);
        Assert.Equal((int)LDetectorType.LDetectorTypeColor, blank.LSceneDetectorType);
        Assert.Equal(90, blank.LSceneDetectorHue);
    }

    [Fact]
    public void LayoutApply_RoundTripsIntoSecondTab()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab source, LInspector inspector, _) = TSplitBuild();
        LSensor sensor = inspector.LInspectorSensor;
        TInterface.TSensorPresetSelect(sensor, LDetectorKind.LDetectorKindScene, "Sensitive");
        TInterface.TSensorStepSet(sensor, TInterface.TSensorStepRead(sensor, LDetectorKind.LDetectorKindScene) with
        {
            LDetectorStepEnabled = true
        });
        TInterface.TSensorModeSet(sensor, LDetectorStillMode.LDetectorStillTreat);
        TInterface.TSensorSpeedSet(sensor, LDetectorLuminanceMode.LDetectorLuminanceFull);
        TInterface.TSensorMetricSet(sensor, LDetectorMetricMode.LDetectorMetricRms);
        TInterface.TBlankStepSet(
            inspector.LInspectorBlank, TInterface.TBlankStepCreate(LDetectorType.LDetectorTypeColor));

        LSceneTabEntry layout = TInterface.TSplitLayoutRead(source);
        (LSplitTab target, LInspector restored, LProcessing processing) = TSplitBuild();
        TInterface.TSplitLayoutApply(target, layout);

        LSensor restoredSensor = restored.LInspectorSensor;
        Assert.Equal(
            TInterface.TSensorStepRead(sensor, LDetectorKind.LDetectorKindScene),
            TInterface.TSensorStepRead(restoredSensor, LDetectorKind.LDetectorKindScene));
        Assert.Equal("Sensitive", TInterface.TSensorTokenRead(restoredSensor, LDetectorKind.LDetectorKindScene));
        Assert.Equal(LDetectorStillMode.LDetectorStillTreat, restoredSensor.LSensorMode);
        Assert.Equal(LDetectorLuminanceMode.LDetectorLuminanceFull, restoredSensor.LSensorSpeed);
        Assert.Equal(LDetectorMetricMode.LDetectorMetricRms, restoredSensor.LSensorMetric);
        Assert.Equal(
            TInterface.TBlankStepRead(inspector.LInspectorBlank), TInterface.TBlankStepRead(restored.LInspectorBlank));
        Assert.True(TInterface.TProcessingActiveCheck(
            processing, TInterface.TSensorNameRead(LDetectorKind.LDetectorKindScene)));
        Assert.False(TInterface.TProcessingActiveCheck(
            processing, TInterface.TSensorNameRead(LDetectorKind.LDetectorKindBlank)));
        Assert.False(restored.LInspectorSaveSuspended);
    }

    [Fact]
    public void LayoutApply_Null_MinimizesProcessingAndInspector()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, LInspector inspector, LProcessing processing) = TSplitBuild();

        TInterface.TSplitLayoutApply(tab, null);

        Assert.True(inspector.LInspectorMinimized);
        Assert.True(processing.LProcessingMinimized);
    }

    [Fact]
    public void SensorChange_UpdatesProcessingActive()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (_, LInspector inspector, LProcessing processing) = TSplitBuild();
        string key = TInterface.TSensorNameRead(LDetectorKind.LDetectorKindVolume);

        TInterface.TSensorEnabledSet(inspector.LInspectorSensor, LDetectorKind.LDetectorKindVolume, true);

        Assert.True(TInterface.TProcessingActiveCheck(processing, key));
    }
}
