using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TSplitTabSweep
{
    private const string TSplitSource = @"C:\media\clip.mp4";

    private static (QSplitTab, QInspector, QList, LDocket, QFlow) TSplitBuild()
    {
        QList list = TInterfaceList.TListCreate();
        LDocket docket = list.QListDocket;
        QFlow flow = TInterface.TFlowCreate();
        QSplitTab tab = TInterface.TSplitTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"), TInterfaceViewer.TViewerCreate(), list, flow);
        QInspector inspector = TInterface.TSplitInspectorRead(tab);
        return (tab, inspector, list, docket, flow);
    }

    [Fact]
    public void StepsRead_EnabledKindsInCatalogOrder()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, QInspector inspector, _, _, _) = TSplitBuild();
        QSensor sensor = inspector.QInspectorSensor;
        TInterface.TSensorEnabledSet(sensor, LDetectorKind.LDetectorKindVolume, true);
        TInterface.TSensorEnabledSet(sensor, LDetectorKind.LDetectorKindScene, true);
        TInterface.TBlankStepSet(
            inspector.QInspectorBlank,
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
        (QSplitTab tab, _, _, _, _) = TSplitBuild();

        Assert.Empty(TInterface.TSplitStepsRead(tab));
    }

    [Fact]
    public async Task SweepStart_NoSelection_DoesNotRun()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, QInspector inspector, _, _, _) = TSplitBuild();
        TInterface.TSensorEnabledSet(inspector.QInspectorSensor, LDetectorKind.LDetectorKindScene, true);
        var busy = new List<bool>();
        TInterface.TSplitBusyAttach(tab, busy.Add);

        await TInterface.TSplitSweepStart(tab);

        Assert.Empty(busy);
        Assert.False(TInterface.TSplitSweepCheck(tab));
        Assert.False(inspector.QInspectorSensor.QSensorRunning);
    }

    [Fact]
    public async Task SweepStart_NoSpool_DoesNotRun()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, QInspector inspector, QList list, LDocket docket, _) = TSplitBuild();
        TInterfaceList.TDocketPathsAdd(docket, TSplitSource);
        TInterfaceList.TListSelect(list, TSplitSource);
        TInterface.TSensorEnabledSet(inspector.QInspectorSensor, LDetectorKind.LDetectorKindScene, true);
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
        (QSplitTab tab, _, QList list, LDocket docket, QFlow flow) = TSplitBuild();
        TInterfaceList.TDocketPathsAdd(docket, TSplitSource);
        TInterfaceList.TListSelect(list, TSplitSource);
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowAttach(
            flow, TInterfaceViewer.TViewerInfoCreate(
                TimeSpan.FromSeconds(60), 1920, 1080), TSplitSource, TimeSpan.Zero);
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
        (QSplitTab tab, _, _, _, _) = TSplitBuild();

        TInterface.TSplitSweepCancel(tab);
        TInterface.TSplitClose(tab);

        Assert.False(TInterface.TSplitSweepCheck(tab));
    }

    [Fact]
    public void LockHandle_LockedDisablesSectionEditing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QSplitTab tab, _, _, _, QFlow flow) = TSplitBuild();

        TInterface.TSplitLockHandle(tab, true);
        Assert.False(flow.QFlowEdit.QFlowSectionEditable);

        TInterface.TSplitLockHandle(tab, false);
        Assert.True(flow.QFlowEdit.QFlowSectionEditable);
    }
}
