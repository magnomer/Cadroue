using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TEditTabPlan
{
    private const string TEditSource = @"C:\media\clip.mp4";

    private static (LEditTab, LInspector, LViewer, LDocket) TEditBuild()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        LViewer viewer = TInterface.TViewerCreate();
        LDocket docket = TInterface.TDocketCreate();
        LEditTab tab = TInterface.TEditTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"),
            inspector,
            viewer,
            TInterface.TCropCreate(),
            TInterface.TListCreate(docket),
            docket,
            TInterface.TProcessingCreate());
        return (tab, inspector, viewer, docket);
    }

    private static void TEditSourceSet(LViewer viewer)
    {
        LMediaInfo info = TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080);
        TInterface.TViewerMediaCommit(viewer, TInterface.TCargoCreate(TEditSource, info, true), false);
    }

    [Fact]
    public void CarriedRead_NothingPersistent_IsNull()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector, _, _) = TEditBuild();
        TInterface.TCropboxCropSet(
            inspector.LInspectorCrop.LInspectorCropbox, TInterface.TWorkCropCreate(1, 2, 3, 4, 0, false, false));
        TInterface.TToneActiveSet(inspector.LInspectorTone, LColorKind.LColorKindContrast, true);

        Assert.Null(TInterface.TEditCarriedRead(tab));
    }

    [Fact]
    public void CarriedRead_CropPersistentOnly_CarriesCropNotVideo()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector, _, _) = TEditBuild();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);
        TInterface.TCropboxCropSet(
            inspector.LInspectorCrop.LInspectorCropbox, TInterface.TWorkCropCreate(10, 0, 0, 0, 0, false, false));
        TInterface.TCropboxApplySet(inspector.LInspectorCrop.LInspectorCropbox, true);
        TInterface.TCropboxPersistentSet(inspector.LInspectorCrop.LInspectorCropbox, true);
        TInterface.TCropboxRatioSet(inspector.LInspectorCrop.LInspectorCropbox, true, true, 4, 3);
        TInterface.TToneActiveSet(inspector.LInspectorTone, LColorKind.LColorKindContrast, true);
        TInterface.TSkipActiveSet(inspector.LInspectorSkip, true);

        LEditPlan? carried = TInterface.TEditCarriedRead(tab);

        Assert.NotNull(carried);
        Assert.Equal(10, carried!.LEditCrop.LWorkCropLeft);
        Assert.True(carried.LEditCropActive);
        Assert.True(carried.LEditRatioFixed);
        Assert.True(carried.LEditRatioLenient);
        Assert.Equal((4, 3), (carried.LEditRatioWidth, carried.LEditRatioHeight));
        Assert.Empty(carried.LEditVideo.LWorkVideoSteps);
        Assert.False(carried.LEditSkip);
    }

    [Fact]
    public void CarriedRead_VideoPersistent_CarriesOnlyPersistentKinds()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector, _, _) = TEditBuild();
        TInterface.TToneActiveSet(inspector.LInspectorTone, LColorKind.LColorKindBrightness, true);
        TInterface.TToneActiveSet(inspector.LInspectorTone, LColorKind.LColorKindContrast, true);
        TInterface.TInspectorPersistentSet(inspector, LColorKind.LColorKindContrast, true);

        LEditPlan? carried = TInterface.TEditCarriedRead(tab);

        Assert.NotNull(carried);
        Assert.Single(carried!.LEditVideo.LWorkVideoSteps);
        Assert.Equal(LColorKind.LColorKindContrast, carried.LEditVideo.LWorkVideoSteps[0].LWorkStepKind);
        Assert.False(carried.LEditCropActive);
        Assert.Equal(0, carried.LEditRatioWidth);
    }

    [Fact]
    public void StateSave_SourceLoaded_WritesRecordThroughLibrarian()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector, LViewer viewer, _) = TEditBuild();
        var written = new List<(string, LSidecarEditRecord?)>();
        TInterface.TEditLibrarianAttach(_ => null, (path, record) =>
        {
            written.Add((path, record));
            return true;
        });
        try
        {
            TEditSourceSet(viewer);
            TInterface.TInspectorSourceSet(inspector, 1920, 1080);
            TInterface.TCropboxCropSet(
                inspector.LInspectorCrop.LInspectorCropbox,
                TInterface.TWorkCropCreate(10, 20, 30, 40, 180, false, true));
            TInterface.TCropboxApplySet(inspector.LInspectorCrop.LInspectorCropbox, true);
            written.Clear();

            TInterface.TEditStateSave(tab);
        }
        finally
        {
            TInterface.TEditLibrarianAttach(null, null);
        }

        (string path, LSidecarEditRecord? record) = Assert.Single(written);
        Assert.Equal(TEditSource, path);
        Assert.NotNull(record);
        Assert.Equal(10, record!.LSidecarCropLeft);
        Assert.Equal(180, record.LSidecarRotation);
        Assert.True(record.LSidecarFlipVertical);
        Assert.True(record.LSidecarCropActive);
    }

    [Fact]
    public void StateSave_NoSourceOrEmptyPlan_WritesNothing()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, _, LViewer viewer, _) = TEditBuild();
        int writes = 0;
        TInterface.TEditLibrarianAttach(_ => null, (_, _) =>
        {
            writes++;
            return true;
        });
        try
        {
            TInterface.TEditStateSave(tab);
            TEditSourceSet(viewer);
            TInterface.TEditStateSave(tab);
        }
        finally
        {
            TInterface.TEditLibrarianAttach(null, null);
        }

        Assert.Equal(0, writes);
    }

    [Fact]
    public void PersistentSave_CropPersistent_WritesEveryUnlockedPath()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector, _, LDocket docket) = TEditBuild();
        var written = new List<string>();
        TInterface.TEditLibrarianAttach(_ => null, (path, _) =>
        {
            written.Add(path);
            return true;
        });
        try
        {
            TInterface.TDocketPathsAdd(docket, @"C:\a.mp4", @"C:\b.mp4");
            TInterface.TCropboxPersistentSet(inspector.LInspectorCrop.LInspectorCropbox, true);
            written.Clear();

            TInterface.TEditPersistentSave(tab);
        }
        finally
        {
            TInterface.TEditLibrarianAttach(null, null);
        }

        Assert.Equal(new[] { @"C:\a.mp4", @"C:\b.mp4" }, written);
    }

    [Fact]
    public void Formats_DescribeCropAndVideo()
    {
        LWorkCrop crop = TInterface.TWorkCropCreate(1, 2, 3, 4, 90, true, false);
        LEditPlan plan = TInterface.TEditPlanCreate(
            crop,
            TInterface.TWorkVideoCreate([TInterface.TWorkBrightnessCreate(true, 12)]),
            true);

        Assert.Equal("none", TInterface.TEditCropFormat(null));
        Assert.Equal("inactive", TInterface.TEditCropFormat(TInterface.TWorkCropCreate()));
        Assert.Equal("edges 1/2/3/4, rotate 90, flip H", TInterface.TEditCropFormat(crop));
        Assert.Equal("none", TInterface.TEditPlanFormat(null));
        Assert.StartsWith("edges 1/2/3/4, rotate 90, flip H, ", TInterface.TEditPlanFormat(plan));
        Assert.Equal("rect none", TInterface.TEditRectFormat(null));
        Assert.Equal("rect 1,2 30x40", TInterface.TEditRectFormat(TInterface.TCropboxCreate(1, 2, 30, 40)));
    }
}
