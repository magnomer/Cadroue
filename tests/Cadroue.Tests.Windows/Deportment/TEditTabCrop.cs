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

    private static (QEditTab, QInspector, QViewer, QCrop, QProcessing) TEditBuild()
    {
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        QCrop crop = TInterfaceCrop.TCropRead(viewer);
        QList list = TInterfaceList.TListCreate();
        QEditTab tab = TInterface.TEditTabCreate(TInterface.TPresetSelectionCreate("Alpha"), viewer, list);
        QInspector inspector = TInterface.TEditInspectorRead(tab);
        QProcessing processing = TInterface.TEditProcessingRead(tab);
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
        (QEditTab tab, QInspector inspector, QViewer viewer, _, _) = TEditBuild();
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

        Assert.Equal(expected, viewer.QViewerPreview.LRotateFlip.LRotateKind);
        Assert.Equal((width, height), (inspector.QInspectorSourceWidth, inspector.QInspectorSourceHeight));
        LCropbox? rect = viewer.QViewerPreview.LCropbox;
        Assert.NotNull(rect);
        Assert.Equal(100, rect!.LCropboxX);
        Assert.Equal(width - 200, rect.LCropboxWidth);
        Assert.Equal(height - 100, rect.LCropboxHeight);
        Assert.True(inspector.QInspectorCrop.QInspectorCropbox.LCropboxStateActive);
    }

    [Fact]
    public void MediaChange_SkipSaved_PushesNeutralViewer()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QEditTab tab, _, QViewer viewer, _, QProcessing processing) = TEditBuild();
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

        Assert.Equal(LRotateKind.LRotateNone, viewer.QViewerPreview.LRotateFlip.LRotateKind);
        Assert.Null(viewer.QViewerPreview.LCropbox);
        Assert.True(processing.QProcessingSkipActive);
    }

    [Fact]
    public void CropHandle_RatioAndPersistent_ReachOverlayOwner()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (_, QInspector inspector, _, QCrop crop, QProcessing processing) = TEditBuild();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);

        TInterfaceCrop.TCropboxRatioSet(inspector.QInspectorCrop.QInspectorCropbox, true, false, 16, 9);
        TInterfaceCrop.TCropboxPersistentSet(inspector.QInspectorCrop.QInspectorCropbox, true);
        TInterfaceCrop.TCropboxApplySet(inspector.QInspectorCrop.QInspectorCropbox, true);

        Assert.Equal((16, 9), (crop.QCropRatioWidth, crop.QCropRatioHeight));
        Assert.True(crop.QCropPersistent);
        Assert.True(crop.QCropActive);
        Assert.True(TInterface.TProcessingActiveCheck(processing, "Crop"));

        TInterfaceCrop.TCropboxRatioSet(inspector.QInspectorCrop.QInspectorCropbox, false, false, 16, 9);

        Assert.Equal((0, 0), (crop.QCropRatioWidth, crop.QCropRatioHeight));
    }

    [Fact]
    public void LockHandle_Locked_CancelsNeutralAndDisarmsTool()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QEditTab tab, QInspector inspector, QViewer viewer, QCrop crop, _) = TEditBuild();
        QViewerNeutral neutral = TInterfaceViewer.TViewerNeutralRead(viewer);
        var tools = new List<QViewerTool>();
        TInterfaceCrop.TCropAttach(crop, () => tools.Add(neutral.QViewerTool));
        TInterfaceInspector.TInspectorToolSet(inspector, true);
        TInterfaceViewer.TViewerToolSet(neutral, true, LNeutralTarget.LNeutralTargetGrey);
        tools.Clear();

        TInterface.TEditLockHandle(tab, true);
        bool locked = crop.QCropLocked;
        TInterface.TEditLockHandle(tab, false);

        Assert.True(locked);
        Assert.False(crop.QCropLocked);
        Assert.Equal(QViewerTool.QViewerToolCrop, neutral.QViewerTool);
        Assert.Equal(QViewerTool.QViewerToolNone, tools[0]);
        Assert.Contains(QViewerTool.QViewerToolCrop, tools);
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
