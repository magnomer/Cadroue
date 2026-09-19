using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TEditTabLayout
{
    private static (LEditTab, LInspector) TEditBuild()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        LDocket docket = TInterface.TDocketCreate();
        LEditTab tab = TInterface.TEditTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"),
            inspector,
            TInterface.TViewerCreate(),
            TInterface.TCropCreate(),
            TInterface.TListCreate(docket),
            docket,
            TInterface.TProcessingCreate());
        return (tab, inspector);
    }

    [Fact]
    public void LayoutRead_NothingPersistent_LeavesInspectorAbsent()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector) = TEditBuild();
        TInterface.TCropboxCropSet(
            inspector.LInspectorCropbox, TInterface.TWorkCropCreate(10, 20, 30, 40, 90, false, false));
        TInterface.TToneActiveSet(inspector.LInspectorTone, LColorKind.LColorKindBrightness, true);

        LSceneTabRecord layout = TInterface.TEditLayoutRead(tab);

        Assert.Null(layout.LSceneInspector);
    }

    [Fact]
    public void LayoutRead_PersistentFlags_RoundTripIntoSecondTab()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab source, LInspector inspector) = TEditBuild();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);
        TInterface.TCropboxCropSet(
            inspector.LInspectorCropbox, TInterface.TWorkCropCreate(10, 20, 30, 40, 90, true, false));
        TInterface.TCropboxApplySet(inspector.LInspectorCropbox, true);
        TInterface.TCropboxPersistentSet(inspector.LInspectorCropbox, true);
        TInterface.TCropboxRatioSet(inspector.LInspectorCropbox, true, false, 16, 9);
        TInterface.TToneValueSet(inspector.LInspectorTone, LColorKind.LColorKindBrightness, 20);
        TInterface.TToneActiveSet(inspector.LInspectorTone, LColorKind.LColorKindBrightness, true);
        TInterface.TInspectorPersistentSet(inspector, LColorKind.LColorKindBrightness, true);
        TInterface.TSkipActiveSet(inspector.LInspectorSkip, true);
        TInterface.TSkipPersistentSet(inspector.LInspectorSkip, true);

        LSceneTabRecord layout = TInterface.TEditLayoutRead(source);
        (LEditTab target, LInspector restored) = TEditBuild();
        TInterface.TInspectorSourceSet(restored, 1920, 1080);
        TInterface.TEditLayoutApply(target, layout);

        Assert.NotNull(layout.LSceneInspector);
        Assert.True(layout.LSceneInspector!.LSceneInspectorCrop);
        Assert.True(layout.LSceneInspector.LSceneInspectorSkip);
        Assert.Equal(TInterface.TInspectorCropRead(inspector), TInterface.TInspectorCropRead(restored));
        Assert.True(restored.LInspectorCropbox.LCropboxStateActive);
        Assert.True(restored.LInspectorCropbox.LCropboxStatePersistent);
        Assert.Equal(inspector.LInspectorCropbox.LCropboxStateRatio, restored.LInspectorCropbox.LCropboxStateRatio);
        Assert.Equal(
            TInterface.TToneStepRead(inspector.LInspectorTone, LColorKind.LColorKindBrightness),
            TInterface.TToneStepRead(restored.LInspectorTone, LColorKind.LColorKindBrightness));
        Assert.True(TInterface.TInspectorPersistentCheck(restored, LColorKind.LColorKindBrightness));
        Assert.False(TInterface.TInspectorPersistentCheck(restored, LColorKind.LColorKindContrast));
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
        TInterface.TCropboxCropSet(
            inspector.LInspectorCropbox, TInterface.TWorkCropCreate(10, 20, 30, 40, 0, false, false));
        TInterface.TSkipPersistentSet(inspector.LInspectorSkip, true);

        LSceneTabRecord layout = TInterface.TEditLayoutRead(source);
        (LEditTab target, LInspector restored) = TEditBuild();
        TInterface.TEditLayoutApply(target, layout);

        Assert.False(layout.LSceneInspector!.LSceneInspectorCrop);
        Assert.Equal(TInterface.TWorkCropCreate(), restored.LInspectorCropbox.LCropboxStateCrop);
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
