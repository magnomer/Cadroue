using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TEditTabLayout
{
    private static (QEditTab, QInspector) TEditBuild()
    {
        QList list = TInterfaceList.TListCreate();
        QEditTab tab = TInterface.TEditTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"), TInterfaceViewer.TViewerCreate(), list);
        QInspector inspector = TInterface.TEditInspectorRead(tab);
        return (tab, inspector);
    }

    [Fact]
    public void LayoutRead_NothingPersistent_LeavesInspectorAbsent()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QEditTab tab, QInspector inspector) = TEditBuild();
        LWorkCrop edges = Interface.TInterface.TWorkCropCreate(10, 20, 30, 40, 90, false, false);
        TInterfaceCrop.TCropboxCropSet(inspector.QInspectorCrop.QInspectorCropbox, edges);
        TInterface.TToneActiveSet(inspector.QInspectorTone, LColorKind.LColorKindBrightness, true);

        LSceneTabEntry layout = TInterface.TEditLayoutRead(tab);

        Assert.Null(layout.LSceneInspector);
    }

    [Fact]
    public void LayoutRead_PersistentFlags_RoundTripIntoSecondTab()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QEditTab source, QInspector inspector) = TEditBuild();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        LWorkCrop edges = Interface.TInterface.TWorkCropCreate(10, 20, 30, 40, 90, true, false);
        TInterfaceCrop.TCropboxCropSet(inspector.QInspectorCrop.QInspectorCropbox, edges);
        TInterfaceCrop.TCropboxApplySet(inspector.QInspectorCrop.QInspectorCropbox, true);
        TInterfaceCrop.TCropboxPersistentSet(inspector.QInspectorCrop.QInspectorCropbox, true);
        TInterfaceCrop.TCropboxRatioSet(inspector.QInspectorCrop.QInspectorCropbox, true, false, 16, 9);
        TInterface.TToneValueSet(inspector.QInspectorTone, LColorKind.LColorKindBrightness, 20);
        TInterface.TToneActiveSet(inspector.QInspectorTone, LColorKind.LColorKindBrightness, true);
        TInterfaceInspector.TInspectorPersistentSet(inspector, LColorKind.LColorKindBrightness, true);
        TInterface.TSkipActiveSet(inspector.QInspectorSkip, true);
        TInterface.TSkipPersistentSet(inspector.QInspectorSkip, true);

        LSceneTabEntry layout = TInterface.TEditLayoutRead(source);
        (QEditTab target, QInspector restored) = TEditBuild();
        TInterfaceInspector.TInspectorSourceSet(restored, 1920, 1080);
        TInterface.TEditLayoutApply(target, layout);

        Assert.NotNull(layout.LSceneInspector);
        Assert.True(layout.LSceneInspector!.LSceneInspectorCrop);
        Assert.True(layout.LSceneInspector.LSceneInspectorSkip);
        Assert.Equal(TInterfaceInspector.TInspectorCropRead(
            inspector), TInterfaceInspector.TInspectorCropRead(restored));
        Assert.True(restored.QInspectorCrop.QInspectorCropbox.LCropboxStateActive);
        Assert.True(restored.QInspectorCrop.QInspectorCropbox.LCropboxStatePersistent);
        Assert.Equal(
            inspector.QInspectorCrop.QInspectorCropbox.LCropboxStateRatio,
            restored.QInspectorCrop.QInspectorCropbox.LCropboxStateRatio);
        Assert.Equal(
            TInterface.TToneStepRead(inspector.QInspectorTone, LColorKind.LColorKindBrightness),
            TInterface.TToneStepRead(restored.QInspectorTone, LColorKind.LColorKindBrightness));
        Assert.True(TInterfaceInspector.TInspectorPersistentCheck(restored, LColorKind.LColorKindBrightness));
        Assert.False(TInterfaceInspector.TInspectorPersistentCheck(restored, LColorKind.LColorKindContrast));
        Assert.True(restored.QInspectorSkip.QSkipActive);
        Assert.True(restored.QInspectorSkip.QSkipPersistent);
        Assert.False(restored.QInspectorSaveSuspended);
    }

    [Fact]
    public void LayoutApply_CropNotPersistent_LeavesCropAlone()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QEditTab source, QInspector inspector) = TEditBuild();
        LWorkCrop edges = Interface.TInterface.TWorkCropCreate(10, 20, 30, 40, 0, false, false);
        TInterfaceCrop.TCropboxCropSet(inspector.QInspectorCrop.QInspectorCropbox, edges);
        TInterface.TSkipPersistentSet(inspector.QInspectorSkip, true);

        LSceneTabEntry layout = TInterface.TEditLayoutRead(source);
        (QEditTab target, QInspector restored) = TEditBuild();
        TInterface.TEditLayoutApply(target, layout);

        Assert.False(layout.LSceneInspector!.LSceneInspectorCrop);
        LWorkCrop cleared = Interface.TInterface.TWorkCropCreate();
        Assert.Equal(cleared, restored.QInspectorCrop.QInspectorCropbox.LCropboxStateCrop);
        Assert.True(restored.QInspectorSkip.QSkipPersistent);
    }

    [Fact]
    public void LayoutApply_Null_MinimizesInspector()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QEditTab tab, QInspector inspector) = TEditBuild();

        TInterface.TEditLayoutApply(tab, null);

        Assert.True(inspector.QInspectorMinimized);
    }
}
