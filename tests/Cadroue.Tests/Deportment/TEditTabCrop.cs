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
        LCrop crop = TInterface.TCropRead(viewer);
        LDocket docket = TInterface.TDocketCreate();
        LProcessing processing = TInterface.TProcessingCreate();
        LEditTab tab = TInterface.TEditTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"),
            inspector,
            viewer,
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

        Assert.Equal(expected, viewer.LViewerPreview.LRotateFlip.LRotateKind);
        Assert.Equal((width, height), (inspector.LInspectorSourceWidth, inspector.LInspectorSourceHeight));
        LCropbox? rect = viewer.LViewerPreview.LCropbox;
        Assert.NotNull(rect);
        Assert.Equal(100, rect!.LCropboxX);
        Assert.Equal(width - 200, rect.LCropboxWidth);
        Assert.Equal(height - 100, rect.LCropboxHeight);
        Assert.True(inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateActive);
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

        Assert.Equal(LRotateKind.LRotateNone, viewer.LViewerPreview.LRotateFlip.LRotateKind);
        Assert.Null(viewer.LViewerPreview.LCropbox);
        Assert.True(processing.LProcessingSkipActive);
    }

    [Fact]
    public void CropHandle_RatioAndPersistent_ReachOverlayOwner()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (_, LInspector inspector, _, LCrop crop, LProcessing processing) = TEditBuild();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);

        TInterface.TCropboxRatioSet(inspector.LInspectorCrop.LInspectorCropbox, true, false, 16, 9);
        TInterface.TCropboxPersistentSet(inspector.LInspectorCrop.LInspectorCropbox, true);
        TInterface.TCropboxApplySet(inspector.LInspectorCrop.LInspectorCropbox, true);

        Assert.Equal((16, 9), (crop.LCropRatioWidth, crop.LCropRatioHeight));
        Assert.True(crop.LCropPersistent);
        Assert.True(crop.LCropActive);
        Assert.True(TInterface.TProcessingActiveCheck(processing, "Crop"));

        TInterface.TCropboxRatioSet(inspector.LInspectorCrop.LInspectorCropbox, false, false, 16, 9);

        Assert.Equal((0, 0), (crop.LCropRatioWidth, crop.LCropRatioHeight));
    }

    [Fact]
    public void LockHandle_Locked_CancelsNeutralAndDisarmsTool()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector, LViewer viewer, LCrop crop, _) = TEditBuild();
        LViewerNeutral neutral = TInterface.TViewerNeutralRead(viewer);
        var tools = new List<LViewerTool>();
        TInterface.TCropAttach(crop, () => tools.Add(neutral.LViewerTool));
        TInterface.TInspectorToolSet(inspector, true);
        TInterface.TViewerToolSet(neutral, true, LNeutralTarget.LNeutralTargetGrey);
        tools.Clear();

        TInterface.TEditLockHandle(tab, true);
        bool locked = crop.LCropLocked;
        TInterface.TEditLockHandle(tab, false);

        Assert.True(locked);
        Assert.False(crop.LCropLocked);
        Assert.Equal(LViewerTool.LViewerToolCrop, neutral.LViewerTool);
        Assert.Equal(LViewerTool.LViewerToolNone, tools[0]);
        Assert.Contains(LViewerTool.LViewerToolCrop, tools);
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
