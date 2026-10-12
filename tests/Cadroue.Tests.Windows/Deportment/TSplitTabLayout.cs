using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TSplitTabLayout
{
    private static (QSplitTab, QInspector, QProcessing) TSplitBuild()
    {
        QList list = TInterfaceList.TListCreate();
        QSplitTab tab = TInterface.TSplitTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"),
            TInterfaceViewer.TViewerCreate(),
            list,
            TInterface.TFlowCreate());
        QInspector inspector = TInterface.TSplitInspectorRead(tab);
        QProcessing processing = TInterface.TSplitProcessingRead(tab);
        foreach (QProcessingRow row in TInterface.TSplitRowsRead())
        {
            TInterface.TProcessingStepAdd(processing, row.QProcessingRowKey);
        }

        return (tab, inspector, processing);
    }

    [Fact]
    public void Rows_OneStepPerKind_KeyedBySensorName()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        IReadOnlyList<QProcessingRow> rows = TInterface.TSplitRowsRead();

        Assert.Equal(6, rows.Count);
        foreach (LDetectorKind kind in LDetector.LDetectorKinds)
        {
            Assert.Contains(rows, row => row.QProcessingRowKey == TInterface.TSensorNameRead(kind));
            Assert.Equal(kind, TInterface.TSensorKindRead(TInterface.TSensorNameRead(kind)));
        }
    }

    [Fact]
    public void LayoutRead_CarriesEveryDetectorAndPersistent()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, QInspector inspector, _) = TSplitBuild();
        TInterface.TSensorStepSet(
            inspector.QInspectorSensor,
            TInterface.TDetectorStepCreate(LDetectorKind.LDetectorKindSilence, true, -40, 1.5, 0));
        TInterface.TBlankStepSet(
            inspector.QInspectorBlank,
            TInterface.TDetectorBlankCreate(true, LDetectorType.LDetectorTypeColor, 90, 0.4, 0.7, 0.1, 0.9, 1));
        TInterface.TSensorPersistentSet(inspector.QInspectorSensor, true);

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
        (QSplitTab source, QInspector inspector, _) = TSplitBuild();
        QSensor sensor = inspector.QInspectorSensor;
        TInterface.TSensorPresetSelect(sensor, LDetectorKind.LDetectorKindScene, "Sensitive");
        TInterface.TSensorStepSet(sensor, TInterface.TSensorStepRead(sensor, LDetectorKind.LDetectorKindScene) with
        {
            LDetectorStepEnabled = true
        });
        TInterface.TSensorModeSet(sensor, LDetectorStillMode.LDetectorStillTreat);
        TInterface.TSensorSpeedSet(sensor, LDetectorLuminanceMode.LDetectorLuminanceFull);
        TInterface.TSensorMetricSet(sensor, LDetectorMetricMode.LDetectorMetricRms);
        TInterface.TBlankStepSet(
            inspector.QInspectorBlank, TInterface.TBlankStepCreate(LDetectorType.LDetectorTypeColor));

        LSceneTabEntry layout = TInterface.TSplitLayoutRead(source);
        (QSplitTab target, QInspector restored, QProcessing processing) = TSplitBuild();
        TInterface.TSplitLayoutApply(target, layout);

        QSensor restoredSensor = restored.QInspectorSensor;
        Assert.Equal(
            TInterface.TSensorStepRead(sensor, LDetectorKind.LDetectorKindScene),
            TInterface.TSensorStepRead(restoredSensor, LDetectorKind.LDetectorKindScene));
        Assert.Equal("Sensitive", TInterface.TSensorTokenRead(restoredSensor, LDetectorKind.LDetectorKindScene));
        Assert.Equal(LDetectorStillMode.LDetectorStillTreat, restoredSensor.QSensorMode);
        Assert.Equal(LDetectorLuminanceMode.LDetectorLuminanceFull, restoredSensor.QSensorSpeed);
        Assert.Equal(LDetectorMetricMode.LDetectorMetricRms, restoredSensor.QSensorMetric);
        Assert.Equal(
            TInterface.TBlankStepRead(inspector.QInspectorBlank), TInterface.TBlankStepRead(restored.QInspectorBlank));
        Assert.True(TInterface.TProcessingActiveCheck(
            processing, TInterface.TSensorNameRead(LDetectorKind.LDetectorKindScene)));
        Assert.False(TInterface.TProcessingActiveCheck(
            processing, TInterface.TSensorNameRead(LDetectorKind.LDetectorKindBlank)));
        Assert.False(restored.QInspectorSaveSuspended);
    }

    [Fact]
    public void LayoutApply_Null_MinimizesProcessingAndInspector()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, QInspector inspector, QProcessing processing) = TSplitBuild();

        TInterface.TSplitLayoutApply(tab, null);

        Assert.True(inspector.QInspectorMinimized);
        Assert.True(processing.QProcessingMinimized);
    }

    [Fact]
    public void SensorChange_UpdatesProcessingActive()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (_, QInspector inspector, QProcessing processing) = TSplitBuild();
        string key = TInterface.TSensorNameRead(LDetectorKind.LDetectorKindVolume);

        TInterface.TSensorEnabledSet(inspector.QInspectorSensor, LDetectorKind.LDetectorKindVolume, true);

        Assert.True(TInterface.TProcessingActiveCheck(processing, key));
    }
}
