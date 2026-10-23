using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TFixTabPlan
{
    private const string TFixSource = @"C:\media\clip.mp4";
    private const string TFixOther = @"C:\media\other.mp4";

    private static (QFixTab, CClinic, QViewer, LDocket, QProcessing) TFixBuild(string preset = "Alpha")
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        QList list = TInterfaceList.TListCreate();
        LDocket docket = list.QListDocket;
        QFixTab tab = TInterface.TFixTabCreate(TInterface.TPresetSelectionCreate(preset), viewer, list);
        CClinic clinic = TInterface.TFixClinicRead(tab);
        QProcessing processing = TInterface.TFixProcessingRead(tab);
        foreach (QProcessingRow row in TInterface.TFixRowsRead())
        {
            TInterface.TProcessingStepAdd(processing, row.QProcessingRowKey);
        }

        return (tab, clinic, viewer, docket, processing);
    }

    private static void TFixSourceSet(QViewer viewer, string path)
    {
        LMediaInfo info = TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080);
        TInterfaceViewer.TViewerMediaCommit(viewer, TInterfaceViewer.TCargoCreate(path, info, true), false);
    }

    private static LWorkFix TFixPlanCreate(LFlawKind kind, bool repair, bool persistent) =>
        TInterface.TFixPlanCreate([TInterface.TFixStepCreate(kind, repair, persistent)]);

    [Fact]
    public void Rows_OneStepPerKindPlusSalvage_InFixOrder()
    {
        IReadOnlyList<QProcessingRow> rows = TInterface.TFixRowsRead();
        IReadOnlyList<QFixRow> kinds = TInterface.TFixKindsRead();

        Assert.Equal(12, rows.Count);
        Assert.Equal(11, kinds.Count);
        Assert.Equal("Truncation", rows[0].QProcessingRowKey);
        Assert.Equal("Salvage", rows[11].QProcessingRowKey);
        Assert.Equal(LFlawKind.LFlawKindConfig, kinds[7].QFixRowKind);
        Assert.Equal("/PAsset/PPanel/PProcessingFixConfiguration.svg", kinds[7].QFixRowStep.QProcessingRowIcon);
        Assert.Equal(kinds.Select(row => row.QFixRowKind).Distinct().Count(), kinds.Count);
    }

    [Fact]
    public void LayoutRead_NothingPersistent_LeavesInspectorNull()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QFixTab tab, CClinic clinic, _, _, _) = TFixBuild();
        TInterface.TClinicPlanApply(clinic, TFixPlanCreate(LFlawKind.LFlawKindIndex, true, false));

        Assert.Null(TInterface.TFixLayoutRead(tab).LSceneInspector);
    }

    [Fact]
    public void LayoutRoundTrip_CarriesPersistentStepAndSalvage()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QFixTab source, CClinic sourceClinic, _, _, _) = TFixBuild();
        TInterface.TClinicPlanApply(
            sourceClinic,
            TInterface.TFixPlanCreate(
                [TInterface.TFixStepCreate(LFlawKind.LFlawKindTiming, true, true)],
                TInterface.TClinicSalvageCreate(
                    true, LSalvageMode.LSalvageModeSeparate, LSalvageBasis.LSalvageBasisSource, true)));

        LSceneTabEntry layout = TInterface.TFixLayoutRead(source);
        (QFixTab target, CClinic targetClinic, _, _, QProcessing processing) = TFixBuild();
        TInterface.TFixLayoutApply(target, layout);
        LWorkFix plan = TInterface.TClinicPlanRead(targetClinic);

        Assert.NotNull(layout.LSceneInspector);
        Assert.True(layout.LSceneInspector!.LSceneInspectorSalvage);
        Assert.Contains(
            plan.LWorkFixSteps,
            step => step.LWorkFixKind == LFlawKind.LFlawKindTiming && step.LWorkFixRepair && step.LWorkFixPersistent);
        Assert.True(plan.LWorkFixSalvage.LWorkSalvageActive);
        Assert.True(TInterface.TProcessingActiveCheck(processing, "Timing"));
        Assert.True(TInterface.TProcessingActiveCheck(processing, "Salvage"));
        Assert.False(TInterface.TProcessingActiveCheck(processing, "Index"));
    }

    [Fact]
    public void LayoutApply_Null_MinimizesClinic()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QFixTab tab, CClinic clinic, _, _, _) = TFixBuild();

        TInterface.TFixLayoutApply(tab, null);

        Assert.True(clinic.CClinicMinimized);
    }

    [Fact]
    public void StateSave_ActivePlan_WritesOnce_SuspendedOrLockedWritesNothing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QFixTab tab, CClinic clinic, QViewer viewer, LDocket docket, _) = TFixBuild();
        var written = new List<string>();
        TInterface.TFixLibrarianAttach(_ => null, (path, _) => { written.Add(path); return true; });
        try
        {
            TInterfaceList.TDocketPathsAdd(docket, TFixSource);
            TFixSourceSet(viewer, TFixSource);
            TInterface.TClinicPlanApply(clinic, TFixPlanCreate(LFlawKind.LFlawKindIndex, true, false));

            TInterface.TFixStateSave(tab);
            Assert.Equal([TFixSource], written);

            TInterface.TClinicSaveSuspend(clinic);
            TInterface.TFixStateSave(tab);
            TInterface.TClinicSaveResume(clinic);
            Assert.Single(written);

            TInterfaceList.TDocketDeliveredAdd(docket, TFixOther, true);
            TFixSourceSet(viewer, TFixOther);
            TInterface.TFixStateSave(tab);
            Assert.Single(written);
        }
        finally
        {
            TInterface.TFixLibrarianAttach(null, null);
        }
    }

    [Fact]
    public void StateSave_InactivePlanWithoutSidecar_WritesNothing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QFixTab tab, _, QViewer viewer, LDocket docket, _) = TFixBuild();
        int writes = 0;
        TInterface.TFixLibrarianAttach(_ => null, (_, _) => { writes++; return true; });
        try
        {
            TInterfaceList.TDocketPathsAdd(docket, TFixSource);
            TFixSourceSet(viewer, TFixSource);

            TInterface.TFixStateSave(tab);

            Assert.Equal(0, writes);
        }
        finally
        {
            TInterface.TFixLibrarianAttach(null, null);
        }
    }

    [Fact]
    public void PlanRestore_PersistentWinsOverSaved_UpdatesActive()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QFixTab tab, CClinic clinic, _, _, QProcessing processing) = TFixBuild();
        LSidecarFixEntry saved = TInterface.TFixRecordCreate(TFixPlanCreate(LFlawKind.LFlawKindIndex, true, false));
        TInterface.TFixLibrarianAttach(_ => saved, (_, _) => true);
        try
        {
            TInterface.TClinicPlanApply(clinic, TFixPlanCreate(LFlawKind.LFlawKindIndex, false, true));

            TInterface.TFixPlanRestore(tab, TFixSource);
            LWorkFix plan = TInterface.TClinicPlanRead(clinic);

            Assert.Contains(
                plan.LWorkFixSteps, step => step.LWorkFixKind == LFlawKind.LFlawKindIndex && !step.LWorkFixRepair);
            Assert.False(TInterface.TProcessingActiveCheck(processing, "Index"));
        }
        finally
        {
            TInterface.TFixLibrarianAttach(null, null);
        }
    }

    [Fact]
    public void ItemsHandle_PersistentStep_FansOutToEveryAddedPath()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (_, CClinic clinic, _, LDocket docket, _) = TFixBuild();
        var written = new List<string>();
        TInterface.TFixLibrarianAttach(_ => null, (path, _) => { written.Add(path); return true; });
        try
        {
            TInterface.TClinicPlanApply(clinic, TFixPlanCreate(LFlawKind.LFlawKindCoded, true, true));
            TInterfaceList.TDocketPathsAdd(docket, TFixSource, TFixOther);

            Assert.Equal([TFixSource, TFixOther], written);
        }
        finally
        {
            TInterface.TFixLibrarianAttach(null, null);
        }
    }

    [Fact]
    public void PathHandle_BlankOrSameSource_DoesNothing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (_, CClinic clinic, QViewer viewer, _, _) = TFixBuild();
        TInterfaceViewer.TViewerRequestSet(viewer, TFixSource);
        TFixSourceSet(viewer, TFixSource);

        TInterface.TFixPathHandle(viewer, "  ");
        TInterface.TFixPathHandle(viewer, TFixSource);

        Assert.Null(clinic.CClinicSource);
    }

    [Fact]
    public void Run_NoPreset_RaisesMissing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate();
        (QFixTab tab, _, _, _, _) = TFixBuild("Missing");
        int missing = 0;
        TInterface.TFixMissingAttach(tab, () => missing++);

        TInterface.TFixRun(tab, false);
        TInterface.TFixAllRun(tab);

        Assert.Equal(2, missing);
    }

    [Fact]
    public void Close_ThenItemsAdded_DoesNotSavePersistentPlan()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QFixTab tab, CClinic clinic, _, LDocket docket, _) = TFixBuild();
        TInterface.TClinicPlanApply(clinic, TFixPlanCreate(LFlawKind.LFlawKindIndex, true, true));
        var written = new List<string>();
        TInterface.TFixLibrarianAttach(_ => null, (path, _) => { written.Add(path); return true; });
        try
        {
            TInterface.TFixClose(tab);

            TInterfaceList.TDocketPathsAdd(docket, TFixSource);

            Assert.Empty(written);
        }
        finally
        {
            TInterface.TFixLibrarianAttach(null, null);
        }
    }

    [Fact]
    public void RepeatedCreateAndClose_ItemsAddedReachesOnlyCurrentTab()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        QList list = TInterfaceList.TListCreate();
        LDocket docket = list.QListDocket;
        QFixTab closed = TInterface.TFixTabCreate(TInterface.TPresetSelectionCreate("Alpha"), viewer, list);
        TInterface.TClinicPlanApply(
            TInterface.TFixClinicRead(closed),
            TFixPlanCreate(LFlawKind.LFlawKindIndex, true, true));
        TInterface.TFixClose(closed);
        QFixTab current = TInterface.TFixTabCreate(TInterface.TPresetSelectionCreate("Alpha"), viewer, list);
        TInterface.TClinicPlanApply(
            TInterface.TFixClinicRead(current),
            TFixPlanCreate(LFlawKind.LFlawKindIndex, true, true));
        var written = new List<string>();
        TInterface.TFixLibrarianAttach(_ => null, (path, _) => { written.Add(path); return true; });
        try
        {
            TInterfaceList.TDocketPathsAdd(docket, TFixSource);

            Assert.Equal([TFixSource], written);
        }
        finally
        {
            TInterface.TFixClose(current);
            TInterface.TFixLibrarianAttach(null, null);
        }
    }

    [Fact]
    public void Close_ThenStepSelectionDoesNotReachClinic()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QFixTab tab, _, _, _, _) = TFixBuild();
        Assert.True(tab.QFixProcessing.QProcessingStepSelect("Index"));
        string title = tab.QFixClinic.QClinicTitleRead();

        TInterface.TFixClose(tab);
        Assert.True(tab.QFixProcessing.QProcessingStepSelect("Timing"));

        Assert.Equal(title, tab.QFixClinic.QClinicTitleRead());
    }
}
