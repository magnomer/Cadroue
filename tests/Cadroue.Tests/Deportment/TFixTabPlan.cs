using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TFixTabPlan
{
    private const string TFixSource = @"C:\media\clip.mp4";
    private const string TFixOther = @"C:\media\other.mp4";

    private static (LFixTab, LClinic, LViewer, LDocket, LProcessing) TFixBuild(string preset = "Alpha")
    {
        LClinic clinic = TInterface.TClinicCreate();
        LViewer viewer = TInterface.TViewerCreate();
        LDocket docket = TInterface.TDocketCreate();
        LProcessing processing = TInterface.TProcessingCreate();
        foreach (LProcessingRow row in TInterface.TFixRowsRead())
        {
            TInterface.TProcessingStepAdd(processing, row.LProcessingRowKey);
        }

        LFixTab tab = TInterface.TFixTabCreate(
            TInterface.TPresetSelectionCreate(preset),
            clinic,
            viewer,
            TInterface.TListCreate(docket),
            docket,
            processing);
        return (tab, clinic, viewer, docket, processing);
    }

    private static void TFixSourceSet(LViewer viewer, string path)
    {
        LMediaInfo info = TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080);
        TInterface.TViewerMediaCommit(viewer, TInterface.TCargoCreate(path, info, true), false);
    }

    private static LWorkFix TFixPlanCreate(LFlawKind kind, bool repair, bool persistent) =>
        TInterface.TFixPlanCreate([TInterface.TFixStepCreate(kind, repair, persistent)]);

    [Fact]
    public void Rows_OneStepPerKindPlusSalvage_InFixOrder()
    {
        IReadOnlyList<LProcessingRow> rows = TInterface.TFixRowsRead();
        IReadOnlyList<LFixRow> kinds = TInterface.TFixKindsRead();

        Assert.Equal(12, rows.Count);
        Assert.Equal(11, kinds.Count);
        Assert.Equal("Truncation", rows[0].LProcessingRowKey);
        Assert.Equal("Salvage", rows[11].LProcessingRowKey);
        Assert.Equal(LFlawKind.LFlawKindConfig, kinds[7].LFixRowKind);
        Assert.Equal("/PAsset/PPanel/PProcessingFixConfiguration.svg", kinds[7].LFixRowStep.LProcessingRowIcon);
        Assert.Equal(kinds.Select(row => row.LFixRowKind).Distinct().Count(), kinds.Count);
    }

    [Fact]
    public void LayoutRead_NothingPersistent_LeavesInspectorNull()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LFixTab tab, LClinic clinic, _, _, _) = TFixBuild();
        TInterface.TClinicPlanApply(clinic, TFixPlanCreate(LFlawKind.LFlawKindIndex, true, false));

        Assert.Null(TInterface.TFixLayoutRead(tab).LSceneInspector);
    }

    [Fact]
    public void LayoutRoundTrip_CarriesPersistentStepAndSalvage()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LFixTab source, LClinic sourceClinic, _, _, _) = TFixBuild();
        TInterface.TClinicPlanApply(
            sourceClinic,
            TInterface.TFixPlanCreate(
                [TInterface.TFixStepCreate(LFlawKind.LFlawKindTiming, true, true)],
                TInterface.TClinicSalvageCreate(
                    true, LSalvageMode.LSalvageModeSeparate, LSalvageBasis.LSalvageBasisSource, true)));

        LSceneTabRecord layout = TInterface.TFixLayoutRead(source);
        (LFixTab target, LClinic targetClinic, _, _, LProcessing processing) = TFixBuild();
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
        (LFixTab tab, LClinic clinic, _, _, _) = TFixBuild();

        TInterface.TFixLayoutApply(tab, null);

        Assert.True(clinic.LClinicMinimized);
    }

    [Fact]
    public void StateSave_ActivePlan_WritesOnce_SuspendedOrLockedWritesNothing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LFixTab tab, LClinic clinic, LViewer viewer, LDocket docket, _) = TFixBuild();
        var written = new List<string>();
        TInterface.TFixLibrarianAttach(_ => null, (path, _) => { written.Add(path); return true; });
        try
        {
            TInterface.TDocketPathsAdd(docket, TFixSource);
            TFixSourceSet(viewer, TFixSource);
            TInterface.TClinicPlanApply(clinic, TFixPlanCreate(LFlawKind.LFlawKindIndex, true, false));

            TInterface.TFixStateSave(tab);
            Assert.Equal([TFixSource], written);

            TInterface.TClinicSaveSuspend(clinic);
            TInterface.TFixStateSave(tab);
            TInterface.TClinicSaveResume(clinic);
            Assert.Single(written);

            TInterface.TDocketDeliveredAdd(docket, TFixOther, true);
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
        (LFixTab tab, _, LViewer viewer, LDocket docket, _) = TFixBuild();
        int writes = 0;
        TInterface.TFixLibrarianAttach(_ => null, (_, _) => { writes++; return true; });
        try
        {
            TInterface.TDocketPathsAdd(docket, TFixSource);
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
        (LFixTab tab, LClinic clinic, _, _, LProcessing processing) = TFixBuild();
        LSidecarFixRecord saved = TInterface.TFixRecordCreate(TFixPlanCreate(LFlawKind.LFlawKindIndex, true, false));
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
        (LFixTab tab, LClinic clinic, _, LDocket docket, _) = TFixBuild();
        var written = new List<string>();
        TInterface.TFixLibrarianAttach(_ => null, (path, _) => { written.Add(path); return true; });
        try
        {
            TInterface.TClinicPlanApply(clinic, TFixPlanCreate(LFlawKind.LFlawKindCoded, true, true));
            TInterface.TDocketPathsAdd(docket, TFixSource, TFixOther);

            TInterface.TFixItemsHandle(tab, TInterface.TDocketItemsRead(docket));

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
        (LFixTab tab, LClinic clinic, LViewer viewer, _, _) = TFixBuild();
        TInterface.TViewerRequestSet(viewer, TFixSource);
        TFixSourceSet(viewer, TFixSource);

        TInterface.TFixPathHandle(tab, "  ");
        TInterface.TFixPathHandle(tab, TFixSource);

        Assert.Null(clinic.LClinicSource);
    }

    [Fact]
    public void Run_NoPreset_RaisesMissing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate();
        (LFixTab tab, _, _, _, _) = TFixBuild("Missing");
        int missing = 0;
        TInterface.TFixMissingAttach(tab, () => missing++);

        TInterface.TFixRun(tab, LWorkPriority.LWorkPriorityNormal);
        TInterface.TFixAllRun(tab);

        Assert.Equal(2, missing);
    }
}
