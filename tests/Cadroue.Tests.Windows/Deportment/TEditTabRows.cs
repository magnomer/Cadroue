using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TEditTabRows
{
    private static (QEditTab, QInspector, QProcessing) TEditBuild()
    {
        QList list = TInterfaceList.TListCreate();
        QEditTab tab = TInterface.TEditTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"), TInterfaceViewer.TViewerCreate(), list);
        QInspector inspector = TInterface.TEditInspectorRead(tab);
        QProcessing processing = TInterface.TEditProcessingRead(tab);
        foreach (QProcessingRow row in QEditProcessing.QEditRows)
        {
            TInterface.TProcessingStepAdd(processing, row.QProcessingRowKey);
        }
        return (tab, inspector, processing);
    }

    [Fact]
    public void Rows_EightSteps_CropFirstCurveLast()
    {
        Assert.Equal(8, QEditProcessing.QEditRows.Count);
        Assert.Equal("Crop", QEditProcessing.QEditRows[0].QProcessingRowKey);
        Assert.Equal("Curve", QEditProcessing.QEditRows[^1].QProcessingRowKey);
        Assert.All(QEditProcessing.QEditRows, row => Assert.StartsWith("/PAsset/PPanel/", row.QProcessingRowIcon));
        Assert.All(QEditProcessing.QEditRows, row => Assert.StartsWith("Processing.Step.", row.QProcessingRowLabel));
    }

    [Fact]
    public void VideoRead_FollowsProcessingOrder_SkipsCrop()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QEditTab tab, _, QProcessing processing) = TEditBuild();

        LWorkVideo video = TInterface.TEditVideoRead(tab, true);

        Assert.Equal(7, video.LWorkVideoSteps.Count);
        Assert.Equal(LColorKind.LColorKindWhitebalance, video.LWorkVideoSteps[0].LWorkStepKind);
        Assert.Equal(LColorKind.LColorKindCurve, video.LWorkVideoSteps[^1].LWorkStepKind);
        Assert.False(processing.QProcessingOrdered);
    }

    [Fact]
    public void VideoRead_MpvIncapable_DropsMpvOnlyKinds()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QEditTab tab, _, _) = TEditBuild();

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
        (QEditTab tab, QInspector inspector, QProcessing processing) = TEditBuild();
        TInterface.TToneActiveSet(inspector.QInspectorVideo.QInspectorTone, LColorKind.LColorKindBrightness, true);
        TInterface.TToneActiveSet(inspector.QInspectorVideo.QInspectorTone, LColorKind.LColorKindSaturation, true);

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
        QProcessing processing = TInterface.TProcessingCreate();
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
