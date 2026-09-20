using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TSensorPlan
{
    [Fact]
    public void Plans_CoverEveryKindButBlank_WithRowsAndGroups()
    {
        LSensor sensor = TInterface.TSensorCreate();
        IReadOnlyList<LSensorPlan> plans = TInterface.TSensorPlansRead(sensor);

        Assert.Equal(5, plans.Count);
        Assert.DoesNotContain(plans, plan => plan.LSensorPlanKind == LDetectorKind.LDetectorKindBlank);
        LSensorPlan luminance = plans.Single(plan => plan.LSensorPlanKind == LDetectorKind.LDetectorKindLuminance);
        Assert.Equal(3, luminance.LSensorPlanRows.Count);
        Assert.True(luminance.LSensorPlanSpeed.LSensorGroupShown);
        Assert.False(luminance.LSensorPlanMode.LSensorGroupShown);
        Assert.Equal(3, luminance.LSensorPlanSpeed.LSensorGroupNames.Count);
        LSensorPlan scene = plans.Single(plan => plan.LSensorPlanKind == LDetectorKind.LDetectorKindScene);
        Assert.Equal(2, scene.LSensorPlanRows.Count);
        Assert.True(scene.LSensorPlanChoice__B);
        LSensorPlan still = plans.Single(plan => plan.LSensorPlanKind == LDetectorKind.LDetectorKindStill);
        Assert.True(still.LSensorPlanMode.LSensorGroupShown);
        LSensorPlan volume = plans.Single(plan => plan.LSensorPlanKind == LDetectorKind.LDetectorKindVolume);
        Assert.True(volume.LSensorPlanMetric.LSensorGroupShown);
    }

    [Fact]
    public void ValueSet_ByRow_RoundTripsThroughStep()
    {
        LSensor sensor = TInterface.TSensorCreate();
        const LDetectorKind kind = LDetectorKind.LDetectorKindLuminance;

        TInterface.TSensorValueSet(sensor, kind, LSensorPlan.LSensorPlanThreshold, 33);
        TInterface.TSensorValueSet(sensor, kind, LSensorPlan.LSensorPlanWindow, 2);
        TInterface.TSensorValueSet(sensor, kind, LSensorPlan.LSensorPlanMinimum, 4);

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
        LSensor sensor = TInterface.TSensorCreate();

        TInterface.TSensorModeSelect(sensor, 1);
        Assert.Equal(LDetectorStillMode.LDetectorStillTreat, sensor.LSensorMode);
        Assert.Equal(1, sensor.LSensorModeIndex);
        TInterface.TSensorModeSelect(sensor, 7);
        Assert.Equal(1, sensor.LSensorModeIndex);

        TInterface.TSensorSpeedSelect(sensor, 0);
        Assert.Equal(LDetectorLuminanceMode.LDetectorLuminanceFast, sensor.LSensorSpeed);
        TInterface.TSensorSpeedSelect(sensor, -1);
        Assert.Equal(0, sensor.LSensorSpeedIndex);

        TInterface.TSensorMetricSelect(sensor, 1);
        Assert.Equal(LDetectorMetricMode.LDetectorMetricRms, sensor.LSensorMetric);
        Assert.Equal("dB", TInterface.TSensorUnitRead(sensor, LDetectorKind.LDetectorKindVolume, 0));
        TInterface.TSensorMetricSelect(sensor, 0);
        Assert.Equal("LU", TInterface.TSensorUnitRead(sensor, LDetectorKind.LDetectorKindVolume, 0));
        Assert.Equal("s", TInterface.TSensorUnitRead(sensor, LDetectorKind.LDetectorKindVolume, 2));
    }

    [Fact]
    public void Choice_IndexFollowsMatch_SelectAppliesPreset()
    {
        LSensor sensor = TInterface.TSensorCreate();
        const LDetectorKind kind = LDetectorKind.LDetectorKindScene;
        IReadOnlyList<string> tokens = TInterface.TDetectorTokensRead(kind);

        LInspectorChoice choice = TInterface.TSensorChoiceRead(sensor, kind);
        Assert.Equal(tokens.Count + 1, choice.LInspectorChoiceNames.Count);
        Assert.Equal(tokens.Count, choice.LInspectorChoiceCustom);
        Assert.Equal(tokens.ToList().IndexOf(LDetector.LDetectorTokenDefault), choice.LInspectorChoiceIndex);

        TInterface.TSensorChoiceSelect(sensor, kind, 0);
        Assert.Equal(tokens[0], TInterface.TSensorTokenRead(sensor, kind));
        Assert.Equal(0, TInterface.TSensorChoiceRead(sensor, kind).LInspectorChoiceIndex);

        TInterface.TSensorChoiceSelect(sensor, kind, tokens.Count);
        Assert.Equal(tokens[0], TInterface.TSensorTokenRead(sensor, kind));
    }

    [Fact]
    public void RunHandle_RaisesRunWhenIdle_StopWhenRunning()
    {
        LSensor sensor = TInterface.TSensorCreate();
        int runs = 0;
        int stops = 0;
        TInterface.TSensorRunAttach(sensor, () => runs++, () => stops++);

        TInterface.TSensorRunHandle(sensor);
        TInterface.TSensorRunningSet(sensor, true);
        TInterface.TSensorRunHandle(sensor);

        Assert.Equal(1, runs);
        Assert.Equal(1, stops);
        Assert.Equal(TInterface.TLocalizationTextRead("Inspector.Detect.Stop"), sensor.LSensorRunText);
    }
}
