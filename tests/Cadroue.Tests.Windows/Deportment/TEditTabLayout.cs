using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TEditTabLayout
{
    private static (LEditTab, LInspector) TEditBuild()
    {
        LList list = TInterfaceList.TListCreate();
        LEditTab tab = TInterface.TEditTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"), TInterfaceViewer.TViewerCreate(), list);
        LInspector inspector = TInterface.TEditInspectorRead(tab);
        return (tab, inspector);
    }

    [Fact]
    public void LayoutRead_NothingPersistent_LeavesInspectorAbsent()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector) = TEditBuild();
        LWorkCrop edges = Interface.TInterface.TWorkCropCreate(10, 20, 30, 40, 90, false, false);
        TInterfaceCrop.TCropboxCropSet(inspector.LInspectorCrop.LInspectorCropbox, edges);
        TInterface.TToneActiveSet(inspector.LInspectorTone, LColorKind.LColorKindBrightness, true);

        LSceneTabEntry layout = TInterface.TEditLayoutRead(tab);

        Assert.Null(layout.LSceneInspector);
    }

    [Fact]
    public void LayoutRead_PersistentFlags_RoundTripIntoSecondTab()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab source, LInspector inspector) = TEditBuild();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        LWorkCrop edges = Interface.TInterface.TWorkCropCreate(10, 20, 30, 40, 90, true, false);
        TInterfaceCrop.TCropboxCropSet(inspector.LInspectorCrop.LInspectorCropbox, edges);
        TInterfaceCrop.TCropboxApplySet(inspector.LInspectorCrop.LInspectorCropbox, true);
        TInterfaceCrop.TCropboxPersistentSet(inspector.LInspectorCrop.LInspectorCropbox, true);
        TInterfaceCrop.TCropboxRatioSet(inspector.LInspectorCrop.LInspectorCropbox, true, false, 16, 9);
        TInterface.TToneValueSet(inspector.LInspectorTone, LColorKind.LColorKindBrightness, 20);
        TInterface.TToneActiveSet(inspector.LInspectorTone, LColorKind.LColorKindBrightness, true);
        TInterfaceInspector.TInspectorPersistentSet(inspector, LColorKind.LColorKindBrightness, true);
        TInterface.TSkipActiveSet(inspector.LInspectorSkip, true);
        TInterface.TSkipPersistentSet(inspector.LInspectorSkip, true);

        LSceneTabEntry layout = TInterface.TEditLayoutRead(source);
        (LEditTab target, LInspector restored) = TEditBuild();
        TInterfaceInspector.TInspectorSourceSet(restored, 1920, 1080);
        TInterface.TEditLayoutApply(target, layout);

        Assert.NotNull(layout.LSceneInspector);
        Assert.True(layout.LSceneInspector!.LSceneInspectorCrop);
        Assert.True(layout.LSceneInspector.LSceneInspectorSkip);
        Assert.Equal(TInterfaceInspector.TInspectorCropRead(
            inspector), TInterfaceInspector.TInspectorCropRead(restored));
        Assert.True(restored.LInspectorCrop.LInspectorCropbox.LCropboxStateActive);
        Assert.True(restored.LInspectorCrop.LInspectorCropbox.LCropboxStatePersistent);
        Assert.Equal(
            inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateRatio,
            restored.LInspectorCrop.LInspectorCropbox.LCropboxStateRatio);
        Assert.Equal(
            TInterface.TToneStepRead(inspector.LInspectorTone, LColorKind.LColorKindBrightness),
            TInterface.TToneStepRead(restored.LInspectorTone, LColorKind.LColorKindBrightness));
        Assert.True(TInterfaceInspector.TInspectorPersistentCheck(restored, LColorKind.LColorKindBrightness));
        Assert.False(TInterfaceInspector.TInspectorPersistentCheck(restored, LColorKind.LColorKindContrast));
        Assert.True(restored.LInspectorSkip.LSkipActive);
        Assert.True(restored.LInspectorSkip.LSkipPersistent);
        Assert.False(restored.LInspectorSaveSuspended);
    }

    [Fact]
    public void LayoutApply_CropNotPersistent_LeavesCropAlone()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab source, LInspector inspector) = TEditBuild();
        LWorkCrop edges = Interface.TInterface.TWorkCropCreate(10, 20, 30, 40, 0, false, false);
        TInterfaceCrop.TCropboxCropSet(inspector.LInspectorCrop.LInspectorCropbox, edges);
        TInterface.TSkipPersistentSet(inspector.LInspectorSkip, true);

        LSceneTabEntry layout = TInterface.TEditLayoutRead(source);
        (LEditTab target, LInspector restored) = TEditBuild();
        TInterface.TEditLayoutApply(target, layout);

        Assert.False(layout.LSceneInspector!.LSceneInspectorCrop);
        LWorkCrop cleared = Interface.TInterface.TWorkCropCreate();
        Assert.Equal(cleared, restored.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop);
        Assert.True(restored.LInspectorSkip.LSkipPersistent);
    }

    [Fact]
    public void LayoutApply_Null_MinimizesInspector()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector) = TEditBuild();

        TInterface.TEditLayoutApply(tab, null);

        Assert.True(inspector.LInspectorMinimized);
    }
}
