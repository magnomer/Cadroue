using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TInspectorPlanApplication
{
    [Fact]
    public void PlanApply_SetsEverySection_RaisesOneVideoNotice()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        int notices = 0;
        TInterfaceInspector.TInspectorVideoAttach(inspector, () => notices++);
        LWorkVideo video = Interface.TInterface.TWorkVideoCreate(
        [
            Interface.TInterface.TWorkBrightnessCreate(true, 12),
            Interface.TInterface.TWorkContrastCreate(true, 110),
            Interface.TInterface.TWorkExposureCreate(true, 0.5),
            Interface.TInterface.TWorkGammaCreate(true, 20)
        ]);
        LEditPlan plan = Interface.TInterface.TEditPlanCreate(
            Interface.TInterface.TWorkCropCreate(10, 20, 30, 40, 90, true, false), video, true) with
        {
            LEditSkip = true,
            LEditRatioFixed = true,
            LEditRatioLenient = true,
            LEditRatioWidth = 4,
            LEditRatioHeight = 3
        };

        TInterfaceInspector.TInspectorPlanApply(inspector, plan);

        Assert.Equal(1, notices);
        Assert.Equal(plan.LEditCrop, inspector.QInspectorCrop.QInspectorCropbox.LCropboxStateCrop);
        Assert.True(inspector.QInspectorCrop.QInspectorActive);
        Assert.True(inspector.QInspectorCrop.QInspectorRatioFixed);
        Assert.True(inspector.QInspectorCrop.QInspectorRatioLenient);
        Assert.Equal(3, inspector.QInspectorCrop.QInspectorPresetIndex);
        Assert.Equal(12, TInterfaceInspector.TInspectorStepRead(
            inspector, LColorKind.LColorKindBrightness).LWorkStepValue);
        Assert.Equal(110, TInterfaceInspector.TInspectorStepRead(
            inspector, LColorKind.LColorKindContrast).LWorkStepValue);
        Assert.Equal(0.5, TInterfaceInspector.TInspectorStepRead(
            inspector, LColorKind.LColorKindExposure).LWorkStepValue);
        Assert.True(TInterfaceInspector.TInspectorStepRead(inspector, LColorKind.LColorKindGamma).LWorkStepActive);
        Assert.False(TInterfaceInspector.TInspectorStepRead(
            inspector, LColorKind.LColorKindSaturation).LWorkStepActive);
        Assert.True(inspector.QInspectorSkip.QSkipActive);
    }

    [Fact]
    public void VideoApply_RaisesOnce_SingleSetRaisesEach()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        int notices = 0;
        TInterfaceInspector.TInspectorVideoAttach(inspector, () => notices++);

        TInterfaceInspector.TInspectorVideoApply(inspector, Interface.TInterface.TWorkVideoCreate(
        [
            Interface.TInterface.TWorkBrightnessCreate(true, 5),
            Interface.TInterface.TWorkContrastCreate(true, 120)
        ]));
        Assert.Equal(1, notices);

        TInterface.TToneValueSet(inspector.QInspectorVideo.QInspectorTone, LColorKind.LColorKindBrightness, 7);
        TInterface.TToneValueSet(inspector.QInspectorVideo.QInspectorTone, LColorKind.LColorKindContrast, 90);

        Assert.Equal(3, notices);
    }

    [Fact]
    public void PersistentChange_RaisesOncePerFlip_NotOnValues()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        int notices = 0;
        TInterfaceInspector.TInspectorPersistentAttach(inspector, () => notices++);

        TInterface.TToneValueSet(inspector.QInspectorVideo.QInspectorTone, LColorKind.LColorKindBrightness, 7);
        Assert.Equal(0, notices);

        TInterfaceInspector.TInspectorPersistentSet(inspector, LColorKind.LColorKindBrightness, true);
        TInterfaceInspector.TInspectorPersistentSet(inspector, LColorKind.LColorKindBrightness, true);
        TInterface.TSkipPersistentSet(inspector.QInspectorSkip, true);
        TInterfaceCrop.TCropboxPersistentSet(inspector.QInspectorCrop.QInspectorCropbox, true);
        TInterface.TVolumePersistentSet(inspector.QInspectorAudio.QInspectorVolume, true);

        Assert.Equal(4, notices);
    }

    [Fact]
    public void AudioApply_RaisesOneAudioNotice()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        int notices = 0;
        inspector.QInspectorAudio.QInspectorAudioChanged += () => notices++;

        LWorkAudioStep volume = Interface.TInterface.TWorkVolumeCreate(true, 3);
        TInterfaceInspector.TInspectorAudioApply(
            inspector.QInspectorAudio, Interface.TInterface.TWorkAudioCreate([volume], false));

        Assert.Equal(1, notices);
        Assert.Equal(3, inspector.QInspectorAudio.QInspectorVolume.CVolumeGain);
    }

    [Fact]
    public void Section_Shown_Title_FollowStep()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();

        TInterfaceInspector.TInspectorStepSet(inspector, "Noise Reduction");

        Assert.True(TInterfaceInspector.TInspectorSectionCheck(inspector, "Noise Reduction"));
        Assert.False(TInterfaceInspector.TInspectorSectionCheck(inspector, "Crop"));
        Assert.True(inspector.QInspectorPersistentShown);
        Assert.False(inspector.QInspectorEmptyShown);
        Assert.Equal(
            Interface.TInterface.TLocalizationTextRead("Inspector.Step.NoiseReduction"),
            TInterfaceInspector.TInspectorTitleRead(inspector));

        TInterfaceInspector.TInspectorStepSet(inspector, "Nowhere");

        Assert.False(inspector.QInspectorPersistentShown);
        Assert.True(inspector.QInspectorEmptyShown);
        string title = Interface.TInterface.TLocalizationTextRead("Inspector.Header.Title");
        Assert.Equal(title, TInterfaceInspector.TInspectorTitleRead(inspector));
    }

    [Fact]
    public void StepSet_AwayFromCrop_DisarmsTool_RaisesToolNotice()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        var armed = new List<bool>();
        inspector.QInspectorToolChanged += armed.Add;
        TInterfaceInspector.TInspectorStepSet(inspector, "Crop");
        TInterfaceInspector.TInspectorToolSet(inspector, true);

        TInterfaceInspector.TInspectorStepSet(inspector, "Gamma");

        Assert.False(inspector.QInspectorToolArmed);
        Assert.Equal(new[] { true, false }, armed);
    }

    [Fact]
    public void TipResolve_DisabledThenPreview_ThenApplyKeys()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();

        QInspectorTip disabled = TInterfaceInspector.TInspectorTipResolve(
            inspector, true, false, true, "Disabled", "Preview");
        QInspectorTip preview = TInterfaceInspector.TInspectorTipResolve(
            inspector, true, true, false, "Disabled", "Preview");
        QInspectorTip plain = TInterfaceInspector.TInspectorTipResolve(
            inspector, true, true, true, "Disabled", "Preview");

        Assert.False(disabled.QInspectorTipEnabled);
        Assert.Equal(Interface.TInterface.TLocalizationTextRead("Disabled"), disabled.QInspectorTipBox);
        Assert.True(preview.QInspectorTipEnabled);
        Assert.Equal(Interface.TInterface.TLocalizationTextRead("Preview"), preview.QInspectorTipNotice);
        Assert.Null(plain.QInspectorTipNotice);
        Assert.Equal(Interface.TInterface.TLocalizationTextRead("Inspector.Common.Apply"), plain.QInspectorTipBox);
    }
}
