using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TSensorPlan
{
    [Fact]
    public void Plans_CoverEveryKindButBlank_WithRowsAndGroups()
    {
        QSensor sensor = TInterface.TSensorCreate();
        IReadOnlyList<QSensorPlan> plans = TInterface.TSensorPlansRead(sensor);

        Assert.Equal(5, plans.Count);
        Assert.DoesNotContain(plans, plan => plan.QSensorPlanKind == LDetectorKind.LDetectorKindBlank);
        QSensorPlan luminance = plans.Single(plan => plan.QSensorPlanKind == LDetectorKind.LDetectorKindLuminance);
        Assert.Equal(3, luminance.QSensorPlanRows.Count);
        Assert.True(luminance.QSensorPlanSpeed.QSensorGroupShown);
        Assert.False(luminance.QSensorPlanMode.QSensorGroupShown);
        Assert.Equal(3, luminance.QSensorPlanSpeed.QSensorGroupNames.Count);
        QSensorPlan scene = plans.Single(plan => plan.QSensorPlanKind == LDetectorKind.LDetectorKindScene);
        Assert.Equal(2, scene.QSensorPlanRows.Count);
        Assert.True(scene.QSensorPlanPreset);
        QSensorPlan still = plans.Single(plan => plan.QSensorPlanKind == LDetectorKind.LDetectorKindStill);
        Assert.True(still.QSensorPlanMode.QSensorGroupShown);
        QSensorPlan volume = plans.Single(plan => plan.QSensorPlanKind == LDetectorKind.LDetectorKindVolume);
        Assert.True(volume.QSensorPlanMetric.QSensorGroupShown);
    }

    [Fact]
    public void ValueSet_ByRow_RoundTripsThroughStep()
    {
        QSensor sensor = TInterface.TSensorCreate();
        const LDetectorKind kind = LDetectorKind.LDetectorKindLuminance;

        TInterface.TSensorValueSet(sensor, kind, QSensorPlan.QSensorPlanThreshold, 33);
        TInterface.TSensorValueSet(sensor, kind, QSensorPlan.QSensorPlanWindow, 2);
        TInterface.TSensorValueSet(sensor, kind, QSensorPlan.QSensorPlanMinimum, 4);

        LDetectorStep step = TInterface.TSensorStepRead(sensor, kind);
        Assert.Equal(step.LDetectorStepThreshold, TInterface.TSensorValueRead(sensor, kind, 0));
        Assert.Equal(step.LDetectorStepWindow, TInterface.TSensorValueRead(sensor, kind, 1));
        Assert.Equal(step.LDetectorStepMinimum, TInterface.TSensorValueRead(sensor, kind, 2));
        Assert.Equal(33, step.LDetectorStepThreshold);
        Assert.Equal(
            TInterface.TDetectorThresholdRead(kind).LDetectorBoundDefault,
            TInterface.TSensorDefaultRead(sensor, kind, 0));
    }

    [Fact]
    public void RadioSelects_MapIndexToMode_IgnoreOutOfRange()
    {
        QSensor sensor = TInterface.TSensorCreate();

        TInterface.TSensorModeSelect(sensor, 1);
        Assert.Equal(LDetectorStillMode.LDetectorStillTreat, sensor.QSensorMode);
        Assert.Equal(1, sensor.QSensorModeIndex);
        TInterface.TSensorModeSelect(sensor, 7);
        Assert.Equal(1, sensor.QSensorModeIndex);

        TInterface.TSensorSpeedSelect(sensor, 0);
        Assert.Equal(LDetectorLuminanceMode.LDetectorLuminanceFast, sensor.QSensorSpeed);
        TInterface.TSensorSpeedSelect(sensor, -1);
        Assert.Equal(0, sensor.QSensorSpeedIndex);

        TInterface.TSensorMetricSelect(sensor, 1);
        Assert.Equal(LDetectorMetricMode.LDetectorMetricRms, sensor.QSensorMetric);
        Assert.Equal("dB", TInterface.TSensorUnitRead(sensor, LDetectorKind.LDetectorKindVolume, 0));
        TInterface.TSensorMetricSelect(sensor, 0);
        Assert.Equal("LU", TInterface.TSensorUnitRead(sensor, LDetectorKind.LDetectorKindVolume, 0));
        Assert.Equal("s", TInterface.TSensorUnitRead(sensor, LDetectorKind.LDetectorKindVolume, 2));
    }

    [Fact]
    public void Choice_IndexFollowsMatch_SelectAppliesPreset()
    {
        QSensor sensor = TInterface.TSensorCreate();
        const LDetectorKind kind = LDetectorKind.LDetectorKindScene;
        IReadOnlyList<string> tokens = TInterface.TDetectorTokensRead(kind);

        QInspectorChoice choice = TInterface.TSensorChoiceRead(sensor, kind);
        Assert.Equal(tokens.Count + 1, choice.QInspectorChoiceNames.Count);
        Assert.Equal(tokens.Count, choice.QInspectorChoiceCustom);
        Assert.Equal(tokens.ToList().IndexOf(LDetector.LDetectorTokenDefault), choice.QInspectorChoiceIndex);

        TInterface.TSensorChoiceSelect(sensor, kind, 0);
        Assert.Equal(tokens[0], TInterface.TSensorTokenRead(sensor, kind));
        Assert.Equal(0, TInterface.TSensorChoiceRead(sensor, kind).QInspectorChoiceIndex);

        TInterface.TSensorChoiceSelect(sensor, kind, tokens.Count);
        Assert.Equal(tokens[0], TInterface.TSensorTokenRead(sensor, kind));
    }

    [Fact]
    public void RunHandle_RaisesRunWhenIdle_StopWhenRunning()
    {
        QSensor sensor = TInterface.TSensorCreate();
        int runs = 0;
        int stops = 0;
        TInterface.TSensorRunAttach(sensor, () => runs++, () => stops++);

        TInterface.TSensorRunHandle(sensor);
        TInterface.TSensorRunningSet(sensor, true);
        TInterface.TSensorRunHandle(sensor);

        Assert.Equal(1, runs);
        Assert.Equal(1, stops);
        Assert.Equal(Interface.TInterface.TLocalizationTextRead("Inspector.Detect.Stop"), sensor.QSensorRunText);
    }
}
