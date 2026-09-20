using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TInspectorPlanApply
{
    [Fact]
    public void PlanApply_SetsEverySection_RaisesOneVideoNotice()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        TInterface.TInspectorSourceSet(inspector, 1920, 1080);
        int notices = 0;
        TInterface.TInspectorVideoAttach(inspector, () => notices++);
        LWorkVideo video = TInterface.TWorkVideoCreate(
        [
            TInterface.TWorkBrightnessCreate(true, 12),
            TInterface.TWorkContrastCreate(true, 110),
            TInterface.TWorkExposureCreate(true, 0.5),
            TInterface.TWorkGammaCreate(true, 20)
        ]);
        LEditPlan plan = TInterface.TEditPlanCreate(
            TInterface.TWorkCropCreate(10, 20, 30, 40, 90, true, false), video, true) with
        {
            LEditSkip = true,
            LEditRatioFixed = true,
            LEditRatioLenient = true,
            LEditRatioWidth = 4,
            LEditRatioHeight = 3
        };

        TInterface.TInspectorPlanApply(inspector, plan);

        Assert.Equal(1, notices);
        Assert.Equal(plan.LEditCrop, inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop);
        Assert.True(inspector.LInspectorCrop.LInspectorActive);
        Assert.True(inspector.LInspectorCrop.LInspectorRatioFixed);
        Assert.True(inspector.LInspectorCrop.LInspectorRatioLenient);
        Assert.Equal(3, inspector.LInspectorCrop.LInspectorPresetIndex);
        Assert.Equal(12, TInterface.TInspectorStepRead(inspector, LColorKind.LColorKindBrightness).LWorkStepValue);
        Assert.Equal(110, TInterface.TInspectorStepRead(inspector, LColorKind.LColorKindContrast).LWorkStepValue);
        Assert.Equal(0.5, TInterface.TInspectorStepRead(inspector, LColorKind.LColorKindExposure).LWorkStepValue);
        Assert.True(TInterface.TInspectorStepRead(inspector, LColorKind.LColorKindGamma).LWorkStepActive);
        Assert.False(TInterface.TInspectorStepRead(inspector, LColorKind.LColorKindSaturation).LWorkStepActive);
        Assert.True(inspector.LInspectorSkip.LSkipActive);
    }

    [Fact]
    public void VideoApply_RaisesOnce_SingleSetRaisesEach()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        int notices = 0;
        TInterface.TInspectorVideoAttach(inspector, () => notices++);

        TInterface.TInspectorVideoApply(inspector, TInterface.TWorkVideoCreate(
        [
            TInterface.TWorkBrightnessCreate(true, 5),
            TInterface.TWorkContrastCreate(true, 120)
        ]));
        Assert.Equal(1, notices);

        TInterface.TToneValueSet(inspector.LInspectorTone, LColorKind.LColorKindBrightness, 7);
        TInterface.TToneValueSet(inspector.LInspectorTone, LColorKind.LColorKindContrast, 90);

        Assert.Equal(3, notices);
    }

    [Fact]
    public void PersistentChange_RaisesOncePerFlip_NotOnValues()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        int notices = 0;
        TInterface.TInspectorPersistentAttach(inspector, () => notices++);

        TInterface.TToneValueSet(inspector.LInspectorTone, LColorKind.LColorKindBrightness, 7);
        Assert.Equal(0, notices);

        TInterface.TInspectorPersistentSet(inspector, LColorKind.LColorKindBrightness, true);
        TInterface.TInspectorPersistentSet(inspector, LColorKind.LColorKindBrightness, true);
        TInterface.TSkipPersistentSet(inspector.LInspectorSkip, true);
        TInterface.TCropboxPersistentSet(inspector.LInspectorCrop.LInspectorCropbox, true);
        TInterface.TVolumePersistentSet(inspector.LInspectorAudio.LInspectorVolume, true);

        Assert.Equal(4, notices);
    }

    [Fact]
    public void AudioApply_RaisesOneAudioNotice()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        int notices = 0;
        inspector.LInspectorAudio.LInspectorAudioChange += () => notices++;

        TInterface.TInspectorAudioApply(
            inspector.LInspectorAudio, TInterface.TWorkAudioCreate([TInterface.TWorkVolumeCreate(true, 3)], false));

        Assert.Equal(1, notices);
        Assert.Equal(3, inspector.LInspectorAudio.LInspectorVolume.LVolumeGain);
    }

    [Fact]
    public void Section_Shown_Title_FollowStep()
    {
        LInspector inspector = TInterface.TInspectorCreate();

        TInterface.TInspectorStepSet(inspector, "Noise Reduction");

        Assert.True(TInterface.TInspectorSectionCheck(inspector, "Noise Reduction"));
        Assert.False(TInterface.TInspectorSectionCheck(inspector, "Crop"));
        Assert.True(inspector.LInspectorPersistentShown);
        Assert.False(inspector.LInspectorEmptyShown);
        Assert.Equal(
            TInterface.TLocalizationTextRead("Inspector.Step.NoiseReduction"),
            TInterface.TInspectorTitleRead(inspector));

        TInterface.TInspectorStepSet(inspector, "Nowhere");

        Assert.False(inspector.LInspectorPersistentShown);
        Assert.True(inspector.LInspectorEmptyShown);
        Assert.Equal(
            TInterface.TLocalizationTextRead("Inspector.Header.Title"), TInterface.TInspectorTitleRead(inspector));
    }

    [Fact]
    public void StepSet_AwayFromCrop_DisarmsTool_RaisesToolNotice()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        var armed = new List<bool>();
        inspector.LInspectorToolChange += armed.Add;
        TInterface.TInspectorStepSet(inspector, "Crop");
        TInterface.TInspectorToolSet(inspector, true);

        TInterface.TInspectorStepSet(inspector, "Gamma");

        Assert.False(inspector.LInspectorToolArmed);
        Assert.Equal(new[] { true, false }, armed);
    }

    [Fact]
    public void TipResolve_DisabledThenPreview_ThenApplyKeys()
    {
        LInspector inspector = TInterface.TInspectorCreate();

        LInspectorTip disabled = TInterface.TInspectorTipResolve(inspector, true, false, true, "Disabled", "Preview");
        LInspectorTip preview = TInterface.TInspectorTipResolve(inspector, true, true, false, "Disabled", "Preview");
        LInspectorTip plain = TInterface.TInspectorTipResolve(inspector, true, true, true, "Disabled", "Preview");

        Assert.False(disabled.LInspectorTipEnabled);
        Assert.Equal(TInterface.TLocalizationTextRead("Disabled"), disabled.LInspectorTipBox);
        Assert.True(preview.LInspectorTipEnabled);
        Assert.Equal(TInterface.TLocalizationTextRead("Preview"), preview.LInspectorTipNotice);
        Assert.Null(plain.LInspectorTipNotice);
        Assert.Equal(TInterface.TLocalizationTextRead("Inspector.Common.Apply"), plain.LInspectorTipBox);
    }
}
