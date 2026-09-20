using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TAudioTabPlan
{
    private const string TAudioSource = @"C:\media\clip.mp4";
    private const string TAudioOther = @"C:\media\other.mp4";

    private static (LAudioTab, LInspector, LViewer, LDocket, LProcessing) TAudioBuild(string preset = "Alpha")
    {
        LInspector inspector = TInterface.TInspectorCreate();
        LViewer viewer = TInterface.TViewerCreate();
        LDocket docket = TInterface.TDocketCreate();
        LProcessing processing = TInterface.TProcessingCreate();
        foreach (LProcessingRow row in TInterface.TAudioRowsRead())
        {
            TInterface.TProcessingStepAdd(processing, row.LProcessingRowKey);
        }

        LAudioTab tab = TInterface.TAudioTabCreate(
            TInterface.TPresetSelectionCreate(preset),
            inspector,
            viewer,
            TInterface.TListCreate(docket),
            docket,
            processing);
        return (tab, inspector, viewer, docket, processing);
    }

    private static void TAudioSourceSet(LViewer viewer, string path)
    {
        LMediaInfo info = TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080);
        LCargo cargo = TInterface.TCargoCreate(path, info, true);
        TInterface.TViewerMediaCommit(viewer, cargo, false);
        TInterface.TViewerMediaRaise(viewer, cargo);
    }

    [Fact]
    public void Rows_SixSteps_EveryKeyMapsToAKind()
    {
        IReadOnlyList<LProcessingRow> rows = TInterface.TAudioRowsRead();

        Assert.Equal(6, rows.Count);
        Assert.All(rows, row => Assert.NotNull(TInterface.TAudioKindRead(row.LProcessingRowKey)));
        Assert.Equal(LAudioKind.LAudioKindLeveling, TInterface.TAudioKindRead("Normalize"));
        Assert.Null(TInterface.TAudioKindRead("Salvage"));
    }

    [Fact]
    public void PlanRead_FollowsProcessingOrder_CarriesSkip()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LAudioTab tab, LInspector inspector, _, _, LProcessing processing) = TAudioBuild();
        TInterface.TProcessingIndexMove(processing, 4, 0);
        TInterface.TSkipActiveSet(inspector.LInspectorSkip, true);

        LWorkAudio plan = TInterface.TAudioPlanRead(tab);

        Assert.Equal(6, plan.LWorkAudioSteps.Count);
        Assert.Equal(LAudioKind.LAudioKindVolume, plan.LWorkAudioSteps[0].LWorkStepKind);
        Assert.True(plan.LWorkAudioSkip);
        TInterface.TAudioClose(tab);
    }

    [Fact]
    public void ActiveUpdate_MirrorsSectionActiveOntoProcessing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LAudioTab tab, LInspector inspector, _, _, LProcessing processing) = TAudioBuild();
        TInterface.TVolumeActiveSet(inspector.LInspectorAudio.LInspectorVolume, true);

        TInterface.TAudioChangeHandle(tab);

        Assert.True(TInterface.TProcessingActiveCheck(processing, "Volume"));
        Assert.False(TInterface.TProcessingActiveCheck(processing, "Equalizer"));
        TInterface.TAudioClose(tab);
    }

    [Fact]
    public void ViewerApply_SkipGivesEmptyGraph_ActiveGivesFilter()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LAudioTab tab, LInspector inspector, _, _, _) = TAudioBuild();
        var graphs = new List<string>();
        TInterface.TAudioFilterAttach(tab, graphs.Add);
        TInterface.TVolumeActiveSet(inspector.LInspectorAudio.LInspectorVolume, true);
        TInterface.TVolumeGainSet(inspector.LInspectorAudio.LInspectorVolume, 3);

        TInterface.TAudioViewerApply(tab);
        TInterface.TSkipActiveSet(inspector.LInspectorSkip, true);
        TInterface.TAudioViewerApply(tab);

        Assert.Equal(2, graphs.Count);
        Assert.NotEqual(string.Empty, graphs[0]);
        Assert.Equal(string.Empty, graphs[1]);
        TInterface.TAudioClose(tab);
    }

    [Fact]
    public void LayoutRoundTrip_CarriesPersistentKindsAndSkip()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LAudioTab source, LInspector sourceInspector, _, _, _) = TAudioBuild();
        LInspectorAudio sections = sourceInspector.LInspectorAudio;
        TInterface.TVolumeActiveSet(sections.LInspectorVolume, true);
        TInterface.TVolumeGainSet(sections.LInspectorVolume, 4);
        TInterface.TVolumePersistentSet(sections.LInspectorVolume, true);
        TInterface.TSkipActiveSet(sourceInspector.LInspectorSkip, true);
        TInterface.TSkipPersistentSet(sourceInspector.LInspectorSkip, true);

        LSceneTabRecord layout = TInterface.TAudioLayoutRead(source);
        (LAudioTab target, LInspector targetInspector, _, _, LProcessing processing) = TAudioBuild();
        TInterface.TAudioLayoutApply(target, layout);
        LInspectorAudio targetSections = targetInspector.LInspectorAudio;

        Assert.NotNull(layout.LSceneInspector?.LSceneInspectorAudio);
        Assert.True(layout.LSceneInspector!.LSceneInspectorSkip);
        Assert.True(TInterface.TInspectorPersistentCheck(targetSections));
        Assert.Equal(4, TInterface.TVolumeGainRead(targetSections.LInspectorVolume));
        Assert.True(TInterface.TInspectorStepRead(targetSections, LAudioKind.LAudioKindVolume).LWorkStepActive);
        Assert.True(targetInspector.LInspectorSkip.LSkipActive);
        Assert.True(targetInspector.LInspectorSkip.LSkipPersistent);
        Assert.True(TInterface.TProcessingActiveCheck(processing, "Volume"));
        TInterface.TAudioClose(source);
        TInterface.TAudioClose(target);
    }

    [Fact]
    public void LayoutRead_NothingPersistent_LeavesInspectorNull()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LAudioTab tab, LInspector inspector, _, _, _) = TAudioBuild();
        TInterface.TVolumeActiveSet(inspector.LInspectorAudio.LInspectorVolume, true);

        Assert.Null(TInterface.TAudioLayoutRead(tab).LSceneInspector);
        TInterface.TAudioClose(tab);
    }

    [Fact]
    public void StateSave_OwnerPath_WritesOnce_NoOwnerWritesNothing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LAudioTab tab, LInspector inspector, LViewer viewer, LDocket docket, _) = TAudioBuild();
        var written = new List<string>();
        TInterface.TAudioLibrarianAttach(_ => null, (path, _) => { written.Add(path); return true; });
        try
        {
            TInterface.TDocketPathsAdd(docket, TAudioSource);
            TInterface.TAudioStateSave(tab);
            TAudioSourceSet(viewer, TAudioSource);
            Assert.Empty(written);

            TInterface.TVolumeActiveSet(inspector.LInspectorAudio.LInspectorVolume, true);

            Assert.Equal([TAudioSource], written);
        }
        finally
        {
            TInterface.TAudioLibrarianAttach(null, null);
            TInterface.TAudioClose(tab);
        }
    }

    [Fact]
    public void MediaHandle_FirstOwner_AdoptsPendingPlan_SavesIt()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LAudioTab tab, LInspector inspector, LViewer viewer, LDocket docket, _) = TAudioBuild();
        var written = new List<string>();
        TInterface.TAudioLibrarianAttach(_ => null, (path, _) => { written.Add(path); return true; });
        try
        {
            TInterface.TDocketPathsAdd(docket, TAudioSource);
            TInterface.TVolumeActiveSet(inspector.LInspectorAudio.LInspectorVolume, true);

            TAudioSourceSet(viewer, TAudioSource);

            Assert.Equal(TAudioSource, inspector.LInspectorOwnerPath);
            Assert.Contains(TAudioSource, written);
            Assert.True(
                TInterface.TInspectorStepRead(inspector.LInspectorAudio, LAudioKind.LAudioKindVolume).LWorkStepActive);
        }
        finally
        {
            TInterface.TAudioLibrarianAttach(null, null);
            TInterface.TAudioClose(tab);
        }
    }

    [Fact]
    public void PlanRestore_SavedSidecar_AppliesSteps()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LAudioTab tab, LInspector inspector, _, _, LProcessing processing) = TAudioBuild();
        LSidecarAudioRecord saved = TInterface.TAudioRecordCreate(
            TInterface.TWorkAudioCreate(false, TInterface.TWorkVolumeCreate(true, 6)));
        TInterface.TAudioLibrarianAttach(_ => saved, (_, _) => true);
        try
        {
            TInterface.TAudioPlanRestore(tab, TAudioSource, false);

            Assert.Equal(6, TInterface.TVolumeGainRead(inspector.LInspectorAudio.LInspectorVolume));
            Assert.True(TInterface.TProcessingActiveCheck(processing, "Volume"));
        }
        finally
        {
            TInterface.TAudioLibrarianAttach(null, null);
            TInterface.TAudioClose(tab);
        }
    }

    [Fact]
    public void ItemsHandle_PersistentKind_FansOutToAddedPaths_NothingPersistentWritesNothing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LAudioTab tab, LInspector inspector, _, LDocket docket, _) = TAudioBuild();
        var written = new List<string>();
        TInterface.TAudioLibrarianAttach(_ => null, (path, _) => { written.Add(path); return true; });
        try
        {
            TInterface.TDocketPathsAdd(docket, TAudioSource, TAudioOther);
            TInterface.TAudioItemsHandle(tab, TInterface.TDocketItemsRead(docket));
            Assert.Empty(written);

            TInterface.TVolumePersistentSet(inspector.LInspectorAudio.LInspectorVolume, true);
            written.Clear();
            TInterface.TAudioItemsHandle(tab, TInterface.TDocketItemsRead(docket));

            Assert.Equal([TAudioSource, TAudioOther], written);
        }
        finally
        {
            TInterface.TAudioLibrarianAttach(null, null);
            TInterface.TAudioClose(tab);
        }
    }

    [Fact]
    public void Run_NoPreset_RaisesMissing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate();
        (LAudioTab tab, _, _, _, _) = TAudioBuild("Missing");
        int missing = 0;
        TInterface.TAudioMissingAttach(tab, () => missing++);

        TInterface.TAudioRun(tab, LWorkPriority.LWorkPriorityNormal);
        TInterface.TAudioAllRun(tab);

        Assert.Equal(2, missing);
        TInterface.TAudioClose(tab);
    }
}
