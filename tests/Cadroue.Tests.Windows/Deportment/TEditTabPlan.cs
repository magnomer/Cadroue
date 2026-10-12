using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TEditTabPlan
{
    private const string TEditSource = @"C:\media\clip.mp4";

    private static (QEditTab, QInspector, QViewer, LDocket) TEditBuild()
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        QList list = TInterfaceList.TListCreate();
        LDocket docket = list.QListDocket;
        QEditTab tab = TInterface.TEditTabCreate(TInterface.TPresetSelectionCreate("Alpha"), viewer, list);
        QInspector inspector = TInterface.TEditInspectorRead(tab);
        return (tab, inspector, viewer, docket);
    }

    private static void TEditSourceSet(QViewer viewer)
    {
        LMediaInfo info = TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080);
        TInterfaceViewer.TViewerMediaCommit(viewer, TInterfaceViewer.TCargoCreate(TEditSource, info, true), false);
    }

    [Fact]
    public void CarriedRead_NothingPersistent_IsNull()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QEditTab tab, QInspector inspector, _, _) = TEditBuild();
        LWorkCrop edges = Interface.TInterface.TWorkCropCreate(1, 2, 3, 4, 0, false, false);
        TInterfaceCrop.TCropboxCropSet(inspector.QInspectorCrop.QInspectorCropbox, edges);
        TInterface.TToneActiveSet(inspector.QInspectorVideo.QInspectorTone, LColorKind.LColorKindContrast, true);

        Assert.Null(TInterface.TEditCarriedRead(tab));
    }

    [Fact]
    public void CarriedRead_CropPersistentOnly_CarriesCropNotVideo()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QEditTab tab, QInspector inspector, _, _) = TEditBuild();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        LWorkCrop edges = Interface.TInterface.TWorkCropCreate(10, 0, 0, 0, 0, false, false);
        TInterfaceCrop.TCropboxCropSet(inspector.QInspectorCrop.QInspectorCropbox, edges);
        TInterfaceCrop.TCropboxApplySet(inspector.QInspectorCrop.QInspectorCropbox, true);
        TInterfaceCrop.TCropboxPersistentSet(inspector.QInspectorCrop.QInspectorCropbox, true);
        TInterfaceCrop.TCropboxRatioSet(inspector.QInspectorCrop.QInspectorCropbox, true, true, 4, 3);
        TInterface.TToneActiveSet(inspector.QInspectorVideo.QInspectorTone, LColorKind.LColorKindContrast, true);
        TInterface.TSkipActiveSet(inspector.QInspectorSkip, true);

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
        (QEditTab tab, QInspector inspector, _, _) = TEditBuild();
        TInterface.TToneActiveSet(inspector.QInspectorVideo.QInspectorTone, LColorKind.LColorKindBrightness, true);
        TInterface.TToneActiveSet(inspector.QInspectorVideo.QInspectorTone, LColorKind.LColorKindContrast, true);
        TInterfaceInspector.TInspectorPersistentSet(inspector, LColorKind.LColorKindContrast, true);

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
        (QEditTab tab, QInspector inspector, QViewer viewer, _) = TEditBuild();
        var written = new List<(string, LSidecarEditEntry?)>();
        TInterface.TEditLibrarianAttach(_ => null, (path, record) =>
        {
            written.Add((path, record));
            return true;
        });
        try
        {
            TEditSourceSet(viewer);
            TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
            TInterfaceCrop.TCropboxCropSet(
                inspector.QInspectorCrop.QInspectorCropbox,
                Interface.TInterface.TWorkCropCreate(10, 20, 30, 40, 180, false, true));
            TInterfaceCrop.TCropboxApplySet(inspector.QInspectorCrop.QInspectorCropbox, true);
            written.Clear();

            TInterface.TEditStateSave(tab);
        }
        finally
        {
            TInterface.TEditLibrarianAttach(null, null);
        }

        (string path, LSidecarEditEntry? record) = Assert.Single(written);
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
        (QEditTab tab, _, QViewer viewer, _) = TEditBuild();
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
        (QEditTab tab, QInspector inspector, _, LDocket docket) = TEditBuild();
        var written = new List<string>();
        TInterface.TEditLibrarianAttach(_ => null, (path, _) =>
        {
            written.Add(path);
            return true;
        });
        try
        {
            TInterfaceList.TDocketPathsAdd(docket, @"C:\a.mp4", @"C:\b.mp4");
            TInterfaceCrop.TCropboxPersistentSet(inspector.QInspectorCrop.QInspectorCropbox, true);
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
        LWorkCrop crop = Interface.TInterface.TWorkCropCreate(1, 2, 3, 4, 90, true, false);
        LEditPlan plan = Interface.TInterface.TEditPlanCreate(
            crop,
            Interface.TInterface.TWorkVideoCreate([Interface.TInterface.TWorkBrightnessCreate(true, 12)]),
            true);

        Assert.Equal("none", TInterface.TEditCropFormat(null));
        Assert.Equal("inactive", TInterface.TEditCropFormat(Interface.TInterface.TWorkCropCreate()));
        Assert.Equal("edges 1/2/3/4, rotate 90, flip H", TInterface.TEditCropFormat(crop));
        Assert.Equal("none", TInterface.TEditPlanFormat(null));
        Assert.StartsWith("edges 1/2/3/4, rotate 90, flip H, ", TInterface.TEditPlanFormat(plan));
        Assert.Equal("rect none", TInterface.TEditRectFormat(null));
        Assert.Equal("rect 1,2 30x40", TInterface.TEditRectFormat(Interface.TInterface.TCropboxCreate(1, 2, 30, 40)));
    }
}
