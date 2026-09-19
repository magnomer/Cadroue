using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TEditTabCrop
{
    private const string TEditSource = @"C:\media\clip.mp4";

    private static (LEditTab, LInspector, LViewer, LCrop, LProcessing) TEditBuild()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        LViewer viewer = TInterface.TViewerCreate();
        LCrop crop = TInterface.TCropCreate();
        LDocket docket = TInterface.TDocketCreate();
        LProcessing processing = TInterface.TProcessingCreate();
        LEditTab tab = TInterface.TEditTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"),
            inspector,
            viewer,
            crop,
            TInterface.TListCreate(docket),
            docket,
            processing);
        return (tab, inspector, viewer, crop, processing);
    }

    private static LCargo TEditCargoCreate() =>
        TInterface.TCargoCreate(TEditSource, TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080), true);

    [Theory]
    [InlineData(0, LRotateKind.LRotateNone, 1920, 1080)]
    [InlineData(90, LRotateKind.LRotate90, 1080, 1920)]
    [InlineData(180, LRotateKind.LRotate180, 1920, 1080)]
    [InlineData(270, LRotateKind.LRotate270, 1080, 1920)]
    public void MediaChange_SidecarPlan_ReachesViewerRotateAndRect(
        int rotation, LRotateKind expected, double width, double height)
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector, LViewer viewer, _, _) = TEditBuild();
        LEditPlan saved = TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(100, 50, 100, 50, rotation, false, false),
            TInterface.TWorkVideoCreate(),
            true);
        var rotates = new List<LRotateFlip>();
        var rects = new List<LCropbox?>();
        TInterface.TEditRotateAttach(tab, rotate =>
        {
            rotates.Add(rotate);
            TInterface.TViewerRotateSet(viewer, rotate);
        });
        TInterface.TEditRectAttach(tab, rects.Add);
        TInterface.TEditLibrarianAttach(_ => TInterface.TEditPersistentCreate(saved), (_, _) => true);
        try
        {
            LCargo cargo = TEditCargoCreate();
            TInterface.TViewerMediaCommit(viewer, cargo, false);
            TInterface.TViewerMediaRaise(viewer, cargo);
        }
        finally
        {
            TInterface.TEditLibrarianAttach(null, null);
        }

        Assert.Equal(expected, rotates[^1].LRotateKind);
        Assert.Equal(expected, viewer.LViewerPreview.LRotateFlip.LRotateKind);
        Assert.Equal((width, height), (inspector.LInspectorSourceWidth, inspector.LInspectorSourceHeight));
        Assert.NotNull(rects[^1]);
        Assert.Equal(100, rects[^1]!.LCropboxX);
        Assert.Equal(width - 200, rects[^1]!.LCropboxWidth);
        Assert.Equal(height - 100, rects[^1]!.LCropboxHeight);
        Assert.True(inspector.LInspectorCropbox.LCropboxStateActive);
    }

    [Fact]
    public void MediaChange_SkipSaved_PushesNeutralViewer()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, _, LViewer viewer, _, LProcessing processing) = TEditBuild();
        LEditPlan saved = TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(10, 10, 10, 10, 90, true, false), TInterface.TWorkVideoCreate(), true)
            with { LEditSkip = true };
        LRotateFlip? last = null;
        LCropbox? rect = TInterface.TCropboxCreate(1, 1, 1, 1);
        TInterface.TEditRotateAttach(tab, rotate => last = rotate);
        TInterface.TEditRectAttach(tab, value => rect = value);
        TInterface.TEditLibrarianAttach(_ => TInterface.TEditPersistentCreate(saved), (_, _) => true);
        try
        {
            LCargo cargo = TEditCargoCreate();
            TInterface.TViewerMediaCommit(viewer, cargo, false);
            TInterface.TViewerMediaRaise(viewer, cargo);
        }
        finally
        {
            TInterface.TEditLibrarianAttach(null, null);
        }

        Assert.Equal(LRotateKind.LRotateNone, last!.LRotateKind);
        Assert.Null(rect);
        Assert.True(processing.LProcessingSkipActive);
    }

    [Fact]
    public void CropHandle_RatioAndPersistent_ReachOverlayOwner()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector, _, LCrop crop, LProcessing processing) = TEditBuild();
        var actives = new List<bool>();
        TInterface.TEditActiveAttach(tab, actives.Add);
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);

        TInterface.TCropboxRatioSet(inspector.LInspectorCropbox, true, false, 16, 9);
        TInterface.TCropboxPersistentSet(inspector.LInspectorCropbox, true);
        TInterface.TCropboxApplySet(inspector.LInspectorCropbox, true);

        Assert.Equal((16, 9), (crop.LCropRatioWidth, crop.LCropRatioHeight));
        Assert.True(crop.LCropPersistent);
        Assert.True(actives[^1]);
        Assert.True(TInterface.TProcessingActiveCheck(processing, "Crop"));

        TInterface.TCropboxRatioSet(inspector.LInspectorCropbox, false, false, 16, 9);

        Assert.Equal((0, 0), (crop.LCropRatioWidth, crop.LCropRatioHeight));
    }

    [Fact]
    public void LockHandle_Locked_CancelsNeutralAndDisarmsTool()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector, _, _, _) = TEditBuild();
        int cancels = 0;
        var locks = new List<bool>();
        var tools = new List<bool>();
        TInterface.TEditNeutralAttach(tab, () => cancels++);
        TInterface.TEditLockAttach(tab, locks.Add);
        TInterface.TEditToolAttach(tab, tools.Add);
        TInterface.TInspectorToolSet(inspector, true);

        TInterface.TEditLockHandle(tab, true);
        TInterface.TEditLockHandle(tab, false);

        Assert.Equal(1, cancels);
        Assert.Equal(new[] { true, false }, locks);
        Assert.Equal(new[] { false, true }, tools);
    }

    [Fact]
    public void RotateCropResolve_MapsRotationAndFlips()
    {
        LRotateFlip rotate = TInterface.TRotateCropResolve(TInterface.TWorkCropCreate(0, 0, 0, 0, 270, true, true));

        Assert.Equal(LRotateKind.LRotate270, rotate.LRotateKind);
        Assert.True(rotate.LRotateFlipHorizontal);
        Assert.True(rotate.LRotateFlipVertical);
        Assert.Equal(
            LRotateKind.LRotateNone,
            TInterface.TRotateCropResolve(TInterface.TWorkCropCreate(0, 0, 0, 0, 45, false, false)).LRotateKind);
    }
}
