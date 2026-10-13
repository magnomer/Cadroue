using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[CollectionDefinition("Preset", DisableParallelization = true)]
public sealed class TPresetCollection { }

[Collection("Preset")]
public sealed class TAudioTabPlan
{
    private const string TAudioSource = @"C:\media\clip.mp4";
    private const string TAudioOther = @"C:\media\other.mp4";

    private static (QAudioTab, QInspector, QViewer, LDocket, QProcessing) TAudioBuild(string preset = "Alpha")
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        QList list = TInterfaceList.TListCreate();
        LDocket docket = list.QListDocket;
        QAudioTab tab = TInterface.TAudioTabCreate(TInterface.TPresetSelectionCreate(preset), viewer, list);
        QInspector inspector = TInterface.TAudioInspectorRead(tab);
        QProcessing processing = TInterface.TAudioProcessingRead(tab);
        foreach (QProcessingRow row in TInterface.TAudioRowsRead())
        {
            TInterface.TProcessingStepAdd(processing, row.QProcessingRowKey);
        }

        return (tab, inspector, viewer, docket, processing);
    }

    private static void TAudioSourceSet(QViewer viewer, string path)
    {
        LMediaInfo info = TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080);
        LCargo cargo = TInterfaceViewer.TCargoCreate(path, info, true);
        TInterfaceViewer.TViewerMediaCommit(viewer, cargo, false);
        TInterfaceViewer.TViewerMediaRaise(viewer, cargo);
    }

    [Fact]
    public void Rows_SixSteps_EveryKeyMapsToAKind()
    {
        IReadOnlyList<QProcessingRow> rows = TInterface.TAudioRowsRead();

        Assert.Equal(6, rows.Count);
        Assert.All(rows, row => Assert.NotNull(TInterface.TAudioKindRead(row.QProcessingRowKey)));
        Assert.Equal(LAudioKind.LAudioKindLeveling, TInterface.TAudioKindRead("Normalize"));
        Assert.Null(TInterface.TAudioKindRead("Salvage"));
    }

    [Fact]
    public void PlanRead_FollowsProcessingOrder_CarriesSkip()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QAudioTab tab, QInspector inspector, _, _, QProcessing processing) = TAudioBuild();
        TInterface.TProcessingIndexMove(processing, 4, 0);
        TInterface.TSkipActiveSet(inspector.QInspectorSkip, true);

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
        (QAudioTab tab, QInspector inspector, _, _, QProcessing processing) = TAudioBuild();
        TInterface.TVolumeActiveSet(inspector.QInspectorAudio.QInspectorVolume, true);

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
        (QAudioTab tab, QInspector inspector, QViewer viewer, _, _) = TAudioBuild();
        var graphs = new List<string>();
        TInterface.TAudioFilterAttach(viewer, graphs.Add);
        TInterface.TVolumeActiveSet(inspector.QInspectorAudio.QInspectorVolume, true);
        TInterface.TVolumeGainSet(inspector.QInspectorAudio.QInspectorVolume, 3);

        TInterface.TAudioViewerApply(tab);
        TInterface.TSkipActiveSet(inspector.QInspectorSkip, true);
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
        (QAudioTab source, QInspector sourceInspector, _, _, _) = TAudioBuild();
        QInspectorAudio sections = sourceInspector.QInspectorAudio;
        TInterface.TVolumeActiveSet(sections.QInspectorVolume, true);
        TInterface.TVolumeGainSet(sections.QInspectorVolume, 4);
        TInterface.TVolumePersistentSet(sections.QInspectorVolume, true);
        TInterface.TSkipActiveSet(sourceInspector.QInspectorSkip, true);
        TInterface.TSkipPersistentSet(sourceInspector.QInspectorSkip, true);

        LSceneTabEntry layout = TInterface.TAudioLayoutRead(source);
        (QAudioTab target, QInspector targetInspector, _, _, QProcessing processing) = TAudioBuild();
        TInterface.TAudioLayoutApply(target, layout);
        QInspectorAudio targetSections = targetInspector.QInspectorAudio;

        Assert.NotNull(layout.LSceneInspector?.LSceneInspectorAudio);
        Assert.True(layout.LSceneInspector!.LSceneInspectorSkip);
        Assert.True(TInterfaceInspector.TInspectorPersistentCheck(targetSections));
        Assert.Equal(4, TInterface.TVolumeGainRead(targetSections.QInspectorVolume));
        Assert.True(TInterfaceInspector.TInspectorStepRead(
            targetSections, LAudioKind.LAudioKindVolume).LWorkStepActive);
        Assert.True(targetInspector.QInspectorSkip.QSkipActive);
        Assert.True(targetInspector.QInspectorSkip.QSkipPersistent);
        Assert.True(TInterface.TProcessingActiveCheck(processing, "Volume"));
        TInterface.TAudioClose(source);
        TInterface.TAudioClose(target);
    }

    [Fact]
    public void LayoutRead_NothingPersistent_LeavesInspectorNull()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QAudioTab tab, QInspector inspector, _, _, _) = TAudioBuild();
        TInterface.TVolumeActiveSet(inspector.QInspectorAudio.QInspectorVolume, true);

        Assert.Null(TInterface.TAudioLayoutRead(tab).LSceneInspector);
        TInterface.TAudioClose(tab);
    }

    [Fact]
    public void StateSave_OwnerPath_WritesOnce_NoOwnerWritesNothing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QAudioTab tab, QInspector inspector, QViewer viewer, LDocket docket, _) = TAudioBuild();
        var written = new List<string>();
        TInterface.TAudioLibrarianAttach(_ => null, (path, _) => { written.Add(path); return true; });
        try
        {
            TInterfaceList.TDocketPathsAdd(docket, TAudioSource);
            TInterface.TAudioStateSave(tab);
            TAudioSourceSet(viewer, TAudioSource);
            Assert.Empty(written);

            TInterface.TVolumeActiveSet(inspector.QInspectorAudio.QInspectorVolume, true);

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
        (QAudioTab tab, QInspector inspector, QViewer viewer, LDocket docket, _) = TAudioBuild();
        var written = new List<string>();
        TInterface.TAudioLibrarianAttach(_ => null, (path, _) => { written.Add(path); return true; });
        try
        {
            TInterfaceList.TDocketPathsAdd(docket, TAudioSource);
            TInterface.TVolumeActiveSet(inspector.QInspectorAudio.QInspectorVolume, true);

            TAudioSourceSet(viewer, TAudioSource);

            Assert.Equal(TAudioSource, inspector.QInspectorOwnerPath);
            Assert.Contains(TAudioSource, written);
            Assert.True(
                TInterfaceInspector.TInspectorStepRead(
                    inspector.QInspectorAudio, LAudioKind.LAudioKindVolume).LWorkStepActive);
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
        (QAudioTab tab, QInspector inspector, _, _, QProcessing processing) = TAudioBuild();
        LSidecarAudioEntry saved = TInterface.TAudioRecordCreate(
            TInterface.TWorkAudioCreate(false, Interface.TInterface.TWorkVolumeCreate(true, 6)));
        TInterface.TAudioLibrarianAttach(_ => saved, (_, _) => true);
        try
        {
            TInterface.TAudioPlanRestore(tab, TAudioSource, false);

            Assert.Equal(6, TInterface.TVolumeGainRead(inspector.QInspectorAudio.QInspectorVolume));
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
        (QAudioTab tab, QInspector inspector, _, LDocket docket, _) = TAudioBuild();
        var written = new List<string>();
        TInterface.TAudioLibrarianAttach(_ => null, (path, _) => { written.Add(path); return true; });
        try
        {
            TInterfaceList.TDocketPathsAdd(docket, TAudioSource, TAudioOther);
            TInterface.TAudioItemsHandle(tab, TInterfaceList.TDocketItemsRead(docket));
            Assert.Empty(written);

            TInterface.TVolumePersistentSet(inspector.QInspectorAudio.QInspectorVolume, true);
            written.Clear();
            TInterface.TAudioItemsHandle(tab, TInterfaceList.TDocketItemsRead(docket));

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
        (QAudioTab tab, _, _, _, _) = TAudioBuild("Missing");
        int missing = 0;
        TInterface.TAudioMissingAttach(tab, () => missing++);

        TInterface.TAudioRun(tab, LWorkPriority.LWorkPriorityNormal);
        TInterface.TAudioAllRun(tab);

        Assert.Equal(2, missing);
        TInterface.TAudioClose(tab);
    }
}
