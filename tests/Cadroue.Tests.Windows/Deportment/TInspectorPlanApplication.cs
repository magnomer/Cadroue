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
        LInspector inspector = TInterfaceInspector.TInspectorCreate();
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
        Assert.Equal(plan.LEditCrop, inspector.LInspectorCrop.LInspectorCropbox.LCropboxStateCrop);
        Assert.True(inspector.LInspectorCrop.LInspectorActive);
        Assert.True(inspector.LInspectorCrop.LInspectorRatioFixed);
        Assert.True(inspector.LInspectorCrop.LInspectorRatioLenient);
        Assert.Equal(3, inspector.LInspectorCrop.LInspectorPresetIndex);
        Assert.Equal(12, TInterfaceInspector.TInspectorStepRead(
            inspector, LColorKind.LColorKindBrightness).LWorkStepValue);
        Assert.Equal(110, TInterfaceInspector.TInspectorStepRead(
            inspector, LColorKind.LColorKindContrast).LWorkStepValue);
        Assert.Equal(0.5, TInterfaceInspector.TInspectorStepRead(
            inspector, LColorKind.LColorKindExposure).LWorkStepValue);
        Assert.True(TInterfaceInspector.TInspectorStepRead(inspector, LColorKind.LColorKindGamma).LWorkStepActive);
        Assert.False(TInterfaceInspector.TInspectorStepRead(
            inspector, LColorKind.LColorKindSaturation).LWorkStepActive);
        Assert.True(inspector.LInspectorSkip.LSkipActive);
    }

    [Fact]
    public void VideoApply_RaisesOnce_SingleSetRaisesEach()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();
        int notices = 0;
        TInterfaceInspector.TInspectorVideoAttach(inspector, () => notices++);

        TInterfaceInspector.TInspectorVideoApply(inspector, Interface.TInterface.TWorkVideoCreate(
        [
            Interface.TInterface.TWorkBrightnessCreate(true, 5),
            Interface.TInterface.TWorkContrastCreate(true, 120)
        ]));
        Assert.Equal(1, notices);

        TInterface.TToneValueSet(inspector.LInspectorTone, LColorKind.LColorKindBrightness, 7);
        TInterface.TToneValueSet(inspector.LInspectorTone, LColorKind.LColorKindContrast, 90);

        Assert.Equal(3, notices);
    }

    [Fact]
    public void PersistentChange_RaisesOncePerFlip_NotOnValues()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();
        int notices = 0;
        TInterfaceInspector.TInspectorPersistentAttach(inspector, () => notices++);

        TInterface.TToneValueSet(inspector.LInspectorTone, LColorKind.LColorKindBrightness, 7);
        Assert.Equal(0, notices);

        TInterfaceInspector.TInspectorPersistentSet(inspector, LColorKind.LColorKindBrightness, true);
        TInterfaceInspector.TInspectorPersistentSet(inspector, LColorKind.LColorKindBrightness, true);
        TInterface.TSkipPersistentSet(inspector.LInspectorSkip, true);
        TInterfaceCrop.TCropboxPersistentSet(inspector.LInspectorCrop.LInspectorCropbox, true);
        TInterface.TVolumePersistentSet(inspector.LInspectorAudio.LInspectorVolume, true);

        Assert.Equal(4, notices);
    }

    [Fact]
    public void AudioApply_RaisesOneAudioNotice()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();
        int notices = 0;
        inspector.LInspectorAudio.LInspectorAudioChanged += () => notices++;

        LWorkAudioStep volume = Interface.TInterface.TWorkVolumeCreate(true, 3);
        TInterfaceInspector.TInspectorAudioApply(
            inspector.LInspectorAudio, Interface.TInterface.TWorkAudioCreate([volume], false));

        Assert.Equal(1, notices);
        Assert.Equal(3, inspector.LInspectorAudio.LInspectorVolume.LVolumeGain);
    }

    [Fact]
    public void Section_Shown_Title_FollowStep()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();

        TInterfaceInspector.TInspectorStepSet(inspector, "Noise Reduction");

        Assert.True(TInterfaceInspector.TInspectorSectionCheck(inspector, "Noise Reduction"));
        Assert.False(TInterfaceInspector.TInspectorSectionCheck(inspector, "Crop"));
        Assert.True(inspector.LInspectorPersistentShown);
        Assert.False(inspector.LInspectorEmptyShown);
        Assert.Equal(
            Interface.TInterface.TLocalizationTextRead("Inspector.Step.NoiseReduction"),
            TInterfaceInspector.TInspectorTitleRead(inspector));

        TInterfaceInspector.TInspectorStepSet(inspector, "Nowhere");

        Assert.False(inspector.LInspectorPersistentShown);
        Assert.True(inspector.LInspectorEmptyShown);
        string title = Interface.TInterface.TLocalizationTextRead("Inspector.Header.Title");
        Assert.Equal(title, TInterfaceInspector.TInspectorTitleRead(inspector));
    }

    [Fact]
    public void StepSet_AwayFromCrop_DisarmsTool_RaisesToolNotice()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();
        var armed = new List<bool>();
        inspector.LInspectorToolChanged += armed.Add;
        TInterfaceInspector.TInspectorStepSet(inspector, "Crop");
        TInterfaceInspector.TInspectorToolSet(inspector, true);

        TInterfaceInspector.TInspectorStepSet(inspector, "Gamma");

        Assert.False(inspector.LInspectorToolArmed);
        Assert.Equal(new[] { true, false }, armed);
    }

    [Fact]
    public void TipResolve_DisabledThenPreview_ThenApplyKeys()
    {
        LInspector inspector = TInterfaceInspector.TInspectorCreate();

        LInspectorTip disabled = TInterfaceInspector.TInspectorTipResolve(
            inspector, true, false, true, "Disabled", "Preview");
        LInspectorTip preview = TInterfaceInspector.TInspectorTipResolve(
            inspector, true, true, false, "Disabled", "Preview");
        LInspectorTip plain = TInterfaceInspector.TInspectorTipResolve(
            inspector, true, true, true, "Disabled", "Preview");

        Assert.False(disabled.LInspectorTipEnabled);
        Assert.Equal(Interface.TInterface.TLocalizationTextRead("Disabled"), disabled.LInspectorTipBox);
        Assert.True(preview.LInspectorTipEnabled);
        Assert.Equal(Interface.TInterface.TLocalizationTextRead("Preview"), preview.LInspectorTipNotice);
        Assert.Null(plain.LInspectorTipNotice);
        Assert.Equal(Interface.TInterface.TLocalizationTextRead("Inspector.Common.Apply"), plain.LInspectorTipBox);
    }
}
