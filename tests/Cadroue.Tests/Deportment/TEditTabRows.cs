using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TEditTabRows
{
    private static (LEditTab, LInspector, LProcessing) TEditBuild()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        LDocket docket = TInterface.TDocketCreate();
        LProcessing processing = TInterface.TProcessingCreate();
        foreach (LProcessingRow row in LEditTab.LEditRows)
        {
            TInterface.TProcessingStepAdd(processing, row.LProcessingRowKey);
        }

        LEditTab tab = TInterface.TEditTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"),
            inspector,
            TInterface.TViewerCreate(),
            TInterface.TListCreate(docket),
            docket,
            processing);
        return (tab, inspector, processing);
    }

    [Fact]
    public void Rows_EightSteps_CropFirstCurveLast()
    {
        Assert.Equal(8, LEditTab.LEditRows.Count);
        Assert.Equal("Crop", LEditTab.LEditRows[0].LProcessingRowKey);
        Assert.Equal("Curve", LEditTab.LEditRows[^1].LProcessingRowKey);
        Assert.All(LEditTab.LEditRows, row => Assert.StartsWith("/PAsset/PPanel/", row.LProcessingRowIcon));
        Assert.All(LEditTab.LEditRows, row => Assert.StartsWith("Processing.Step.", row.LProcessingRowLabel));
    }

    [Fact]
    public void VideoRead_FollowsProcessingOrder_SkipsCrop()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, _, LProcessing processing) = TEditBuild();

        LWorkVideo video = TInterface.TEditVideoRead(tab, true);

        Assert.Equal(7, video.LWorkVideoSteps.Count);
        Assert.Equal(LColorKind.LColorKindWhitebalance, video.LWorkVideoSteps[0].LWorkStepKind);
        Assert.Equal(LColorKind.LColorKindCurve, video.LWorkVideoSteps[^1].LWorkStepKind);
        Assert.False(processing.LProcessingOrdered);
    }

    [Fact]
    public void VideoRead_MpvIncapable_DropsMpvOnlyKinds()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, _, _) = TEditBuild();

        LWorkVideo video = TInterface.TEditVideoRead(tab, false);

        Assert.DoesNotContain(video.LWorkVideoSteps, step => step.LWorkStepKind == LColorKind.LColorKindGamma);
        Assert.DoesNotContain(video.LWorkVideoSteps, step => step.LWorkStepKind == LColorKind.LColorKindExposure);
        Assert.DoesNotContain(video.LWorkVideoSteps, step => step.LWorkStepKind == LColorKind.LColorKindWhitebalance);
        Assert.Contains(video.LWorkVideoSteps, step => step.LWorkStepKind == LColorKind.LColorKindBrightness);
    }

    [Fact]
    public void ColorUpdate_ActiveSteps_MarkTheirRows()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector, LProcessing processing) = TEditBuild();
        TInterface.TToneActiveSet(inspector.LInspectorTone, LColorKind.LColorKindBrightness, true);
        TInterface.TToneActiveSet(inspector.LInspectorTone, LColorKind.LColorKindSaturation, true);

        TInterface.TEditColorUpdate(tab);

        Assert.True(TInterface.TProcessingActiveCheck(processing, "Brightness"));
        Assert.True(TInterface.TProcessingActiveCheck(processing, "Saturation"));
        Assert.False(TInterface.TProcessingActiveCheck(processing, "Contrast"));
        Assert.False(TInterface.TProcessingActiveCheck(processing, "Gamma"));
        Assert.False(TInterface.TProcessingActiveCheck(processing, "Crop"));
    }

    [Fact]
    public void EnabledSet_Notice_KeptWhileDisabled_ClearedWhenEnabled()
    {
        LProcessing processing = TInterface.TProcessingCreate();
        TInterface.TProcessingStepAdd(processing, "Gamma");
        int notices = 0;
        TInterface.TProcessingAttach(processing, () => notices++);

        TInterface.TProcessingEnabledSet(processing, "Gamma", false, "Requires eq");
        TInterface.TProcessingEnabledSet(processing, "Gamma", false, "Requires eq");

        Assert.False(TInterface.TProcessingEnabledCheck(processing, "Gamma"));
        Assert.Equal("Requires eq", TInterface.TProcessingNoticeRead(processing, "Gamma"));
        Assert.Equal(1, notices);

        TInterface.TProcessingEnabledSet(processing, "Gamma", true);

        Assert.True(TInterface.TProcessingEnabledCheck(processing, "Gamma"));
        Assert.Null(TInterface.TProcessingNoticeRead(processing, "Gamma"));
        Assert.Equal(2, notices);
    }
}
