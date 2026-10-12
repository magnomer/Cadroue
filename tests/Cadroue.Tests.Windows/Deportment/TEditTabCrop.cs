using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TEditTabCrop
{
    private const string TEditSource = @"C:\media\clip.mp4";

    private static (LEditTab, LInspector, LViewer, LCrop, LProcessing) TEditBuild()
    {
        LViewer viewer = TInterfaceViewer.TViewerCreate();
        LCrop crop = TInterfaceCrop.TCropRead(viewer);
        LList list = TInterfaceList.TListCreate();
        LEditTab tab = TInterface.TEditTabCreate(TInterface.TPresetSelectionCreate("Alpha"), viewer, list);
        LInspector inspector = TInterface.TEditInspectorRead(tab);
        LProcessing processing = TInterface.TEditProcessingRead(tab);
        return (tab, inspector, viewer, crop, processing);
    }

    private static LCargo TEditCargoCreate() =>
        TInterfaceViewer.TCargoCreate(
            TEditSource, TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(60), 1920, 1080), true);

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
        LEditPlan saved = Interface.TInterface.TEditPlanCreate(
            Interface.TInterface.TWorkCropCreate(100, 50, 100, 50, rotation, false, false),
            Interface.TInterface.TWorkVideoCreate(),
            true);
        TInterface.TEditLibrarianAttach(_ => Interface.TInterface.TEditPersistentCreate(saved), (_, _) => true);
        try
        {
            LCargo cargo = TEditCargoCreate();
            TInterfaceViewer.TViewerMediaCommit(viewer, cargo, false);
            TInterfaceViewer.TViewerMediaRaise(viewer, cargo);
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
        LWorkCrop edges = Interface.TInterface.TWorkCropCreate(10, 10, 10, 10, 90, true, false);
        LEditPlan saved = Interface.TInterface.TEditPlanCreate(edges, Interface.TInterface.TWorkVideoCreate(), true)
            with { LEditSkip = true };
        TInterface.TEditLibrarianAttach(_ => Interface.TInterface.TEditPersistentCreate(saved), (_, _) => true);
        try
        {
            LCargo cargo = TEditCargoCreate();
            TInterfaceViewer.TViewerMediaCommit(viewer, cargo, false);
            TInterfaceViewer.TViewerMediaRaise(viewer, cargo);
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
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);

        TInterfaceCrop.TCropboxRatioSet(inspector.LInspectorCrop.LInspectorCropbox, true, false, 16, 9);
        TInterfaceCrop.TCropboxPersistentSet(inspector.LInspectorCrop.LInspectorCropbox, true);
        TInterfaceCrop.TCropboxApplySet(inspector.LInspectorCrop.LInspectorCropbox, true);

        Assert.Equal((16, 9), (crop.LCropRatioWidth, crop.LCropRatioHeight));
        Assert.True(crop.LCropPersistent);
        Assert.True(crop.LCropActive);
        Assert.True(TInterface.TProcessingActiveCheck(processing, "Crop"));

        TInterfaceCrop.TCropboxRatioSet(inspector.LInspectorCrop.LInspectorCropbox, false, false, 16, 9);

        Assert.Equal((0, 0), (crop.LCropRatioWidth, crop.LCropRatioHeight));
    }

    [Fact]
    public void LockHandle_Locked_CancelsNeutralAndDisarmsTool()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector, LViewer viewer, LCrop crop, _) = TEditBuild();
        LViewerNeutral neutral = TInterfaceViewer.TViewerNeutralRead(viewer);
        var tools = new List<LViewerTool>();
        TInterfaceCrop.TCropAttach(crop, () => tools.Add(neutral.LViewerTool));
        TInterfaceInspector.TInspectorToolSet(inspector, true);
        TInterfaceViewer.TViewerToolSet(neutral, true, LNeutralTarget.LNeutralTargetGrey);
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
        LWorkCrop turned = Interface.TInterface.TWorkCropCreate(0, 0, 0, 0, 270, true, true);
        LRotateFlip rotate = TInterfaceInspector.TRotateCropResolve(turned);

        Assert.Equal(LRotateKind.LRotate270, rotate.LRotateKind);
        Assert.True(rotate.LRotateFlipHorizontal);
        Assert.True(rotate.LRotateFlipVertical);
        LWorkCrop slanted = Interface.TInterface.TWorkCropCreate(0, 0, 0, 0, 45, false, false);
        Assert.Equal(LRotateKind.LRotateNone, TInterfaceInspector.TRotateCropResolve(slanted).LRotateKind);
    }
}
