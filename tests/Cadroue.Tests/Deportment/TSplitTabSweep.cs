using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TSplitTabSweep
{
    private const string TSplitSource = @"C:\media\clip.mp4";

    private static (LSplitTab, LInspector, LList, LDocket, LFlow) TSplitBuild()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        LDocket docket = TInterface.TDocketCreate();
        LList list = TInterface.TListCreate(docket);
        LFlow flow = TInterface.TFlowCreate();
        LSplitTab tab = TInterface.TSplitTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"),
            inspector,
            TInterface.TViewerCreate(),
            list,
            docket,
            TInterface.TProcessingCreate(),
            flow);
        return (tab, inspector, list, docket, flow);
    }

    [Fact]
    public void StepsRead_EnabledKindsInCatalogOrder()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, LInspector inspector, _, _, _) = TSplitBuild();
        LSensor sensor = inspector.LInspectorSensor;
        TInterface.TSensorEnabledSet(sensor, LDetectorKind.LDetectorKindVolume, true);
        TInterface.TSensorEnabledSet(sensor, LDetectorKind.LDetectorKindScene, true);
        TInterface.TBlankStepSet(
            inspector.LInspectorBlank,
            TInterface.TBlankStepCreate(LDetectorType.LDetectorTypeBlack) with { LDetectorBlankEnabled = true });

        IReadOnlyList<LDetectorKind> steps = TInterface.TSplitStepsRead(tab);

        Assert.Equal(
            [LDetectorKind.LDetectorKindBlank, LDetectorKind.LDetectorKindScene, LDetectorKind.LDetectorKindVolume],
            steps);
    }

    [Fact]
    public void StepsRead_NothingEnabled_IsEmpty()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, _, _, _, _) = TSplitBuild();

        Assert.Empty(TInterface.TSplitStepsRead(tab));
    }

    [Fact]
    public async Task SweepStart_NoSelection_DoesNotRun()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, LInspector inspector, _, _, _) = TSplitBuild();
        TInterface.TSensorEnabledSet(inspector.LInspectorSensor, LDetectorKind.LDetectorKindScene, true);
        var busy = new List<bool>();
        TInterface.TSplitBusyAttach(tab, busy.Add);

        await TInterface.TSplitSweepStart(tab);

        Assert.Empty(busy);
        Assert.False(TInterface.TSplitSweepCheck(tab));
        Assert.False(inspector.LInspectorSensor.LSensorRunning);
    }

    [Fact]
    public async Task SweepStart_NoSpool_DoesNotRun()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, LInspector inspector, LList list, LDocket docket, _) = TSplitBuild();
        TInterface.TDocketPathsAdd(docket, TSplitSource);
        TInterface.TListSelect(list, TSplitSource);
        TInterface.TSensorEnabledSet(inspector.LInspectorSensor, LDetectorKind.LDetectorKindScene, true);
        var busy = new List<bool>();
        TInterface.TSplitBusyAttach(tab, busy.Add);

        await TInterface.TSplitSweepStart(tab);

        Assert.Empty(busy);
        Assert.False(TInterface.TSplitSweepCheck(tab));
    }

    [Fact]
    public async Task SweepStart_NothingEnabled_DoesNotRun()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, _, LList list, LDocket docket, LFlow flow) = TSplitBuild();
        TInterface.TDocketPathsAdd(docket, TSplitSource);
        TInterface.TListSelect(list, TSplitSource);
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowAttach(
            flow, TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080), TSplitSource, TimeSpan.Zero);
        var busy = new List<bool>();
        TInterface.TSplitBusyAttach(tab, busy.Add);

        await TInterface.TSplitSweepStart(tab);

        Assert.Empty(busy);
        Assert.False(TInterface.TSplitSweepCheck(tab));
    }

    [Fact]
    public void SweepCancel_Idle_IsNoop()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, _, _, _, _) = TSplitBuild();

        TInterface.TSplitSweepCancel(tab);
        TInterface.TSplitClose(tab);

        Assert.False(TInterface.TSplitSweepCheck(tab));
    }

    [Fact]
    public void LockHandle_LockedDisablesSectionEditing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LSplitTab tab, _, _, _, LFlow flow) = TSplitBuild();

        TInterface.TSplitLockHandle(tab, true);
        Assert.False(flow.LFlowSectionEditable);

        TInterface.TSplitLockHandle(tab, false);
        Assert.True(flow.LFlowSectionEditable);
    }
}
