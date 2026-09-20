using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private CheckBox pToneBrightnessBox = null!;
    private CheckBox pInspectorBrightnessPersistent = null!;
    private Slider pInspectorBrightnessSlider = null!;
    private TextBox pInspectorBrightnessValue = null!;
    private StackPanel pInspectorBrightnessStack = null!;
    private StackPanel pInspectorBrightnessBody = null!;

    private CheckBox pToneContrastBox = null!;
    private CheckBox pInspectorContrastPersistent = null!;
    private Slider pInspectorContrastSlider = null!;
    private TextBox pInspectorContrastValue = null!;
    private StackPanel pInspectorContrastStack = null!;
    private StackPanel pInspectorContrastBody = null!;

    private CheckBox pToneSaturationBox = null!;
    private CheckBox pInspectorSaturationPersistent = null!;
    private Slider pInspectorSaturationSlider = null!;
    private TextBox pInspectorSaturationValue = null!;
    private StackPanel pInspectorSaturationStack = null!;
    private StackPanel pInspectorSaturationBody = null!;

    private const double PToneBrightnessLeast = -100;
    private const double PToneBrightnessMost = 100;

    private StackPanel PToneBrightnessBuild()
    {
        pToneBrightnessBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Video.ApplyBrightness"));
        pInspectorBrightnessPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Video.PersistBrightness"));
        pInspectorBrightnessSlider = PToneSliderBuild(
            PToneBrightnessLeast,
            PToneBrightnessMost,
            0);
        pInspectorBrightnessValue = PInspectorDecimalBuild();
        pInspectorBrightnessValue.Text = "0";
        pInspectorBrightnessStack = new StackPanel();
        PToneAttach(
            LColorKind.LColorKindBrightness,
            pToneBrightnessBox,
            pInspectorBrightnessPersistent,
            pInspectorBrightnessSlider,
            pInspectorBrightnessValue,
            null,
            null);
        pInspectorBrightnessStack.Children.Add(
            PInspectorSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Common.Amount"),
                pInspectorBrightnessSlider,
                string.Empty,
                pInspectorBrightnessValue));
        pInspectorBrightnessBody = PToneBodyBuild(pToneBrightnessBox, pInspectorBrightnessStack);
        return pInspectorBrightnessBody;
    }

    private StackPanel PToneContrastBuild()
    {
        pToneContrastBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Video.ApplyContrast"));
        pInspectorContrastPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Video.PersistContrast"));
        pInspectorContrastSlider = PToneSliderBuild(0, 200, 100);
        pInspectorContrastValue = PInspectorDecimalBuild();
        pInspectorContrastValue.Text = "100";
        pInspectorContrastStack = new StackPanel();
        PToneAttach(
            LColorKind.LColorKindContrast,
            pToneContrastBox,
            pInspectorContrastPersistent,
            pInspectorContrastSlider,
            pInspectorContrastValue,
            0,
            200);
        pInspectorContrastStack.Children.Add(
            PInspectorSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Common.Amount"),
                pInspectorContrastSlider,
                "%",
                pInspectorContrastValue));
        pInspectorContrastStack.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Video.ContrastPreview"),
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x64, 0x70, 0x82)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0),
            Visibility = PLook.PLookVisible[LTone.LToneNoticeShown]
        });

        pInspectorContrastBody = PToneBodyBuild(pToneContrastBox, pInspectorContrastStack);
        return pInspectorContrastBody;
    }

    private StackPanel PToneSaturationBuild()
    {
        pToneSaturationBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Video.ApplySaturation"));
        pInspectorSaturationPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Video.PersistSaturation"));
        pInspectorSaturationSlider = PToneSliderBuild(0, 200, 100);
        pInspectorSaturationValue = PInspectorDecimalBuild();
        pInspectorSaturationValue.Text = "100";
        pInspectorSaturationStack = new StackPanel();
        PToneAttach(
            LColorKind.LColorKindSaturation,
            pToneSaturationBox,
            pInspectorSaturationPersistent,
            pInspectorSaturationSlider,
            pInspectorSaturationValue,
            0,
            200);
        pInspectorSaturationStack.Children.Add(
            PInspectorSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Common.Amount"),
                pInspectorSaturationSlider,
                "%",
                pInspectorSaturationValue));
        pInspectorSaturationBody = PToneBodyBuild(pToneSaturationBox, pInspectorSaturationStack);
        return pInspectorSaturationBody;
    }

    private void PToneAttach(
        LColorKind pKind,
        CheckBox pApply,
        CheckBox pPersistent,
        Slider pSlider,
        TextBox pValue,
        double? pMinimum,
        double? pMaximum)
    {
        PInspectorSwitchAttach(pApply, pActive => LTone.LToneActiveSet(pKind, pActive));
        PInspectorSwitchAttach(pPersistent, pFlag => LTone.LTonePersistentSet(pKind, pFlag));
        PInspectorValueAttach(
            pSlider,
            pValue,
            pMinimum,
            pMaximum,
            () => LTone.LToneStepRead(pKind).LWorkStepValue,
            pNumber => LTone.LToneValueSet(pKind, pNumber));
    }

    private void PToneUpdate()
    {
        PToneSectionUpdate(
            LColorKind.LColorKindBrightness,
            pToneBrightnessBox, pInspectorBrightnessPersistent, pInspectorBrightnessSlider,
            pInspectorBrightnessValue, pInspectorBrightnessStack, pInspectorBrightnessBody,
            "Inspector.Video.BrightnessRequiresEq",
            "Inspector.Video.ApplyBrightness", "Inspector.Video.PersistBrightness");
        PToneSectionUpdate(
            LColorKind.LColorKindContrast,
            pToneContrastBox, pInspectorContrastPersistent, pInspectorContrastSlider,
            pInspectorContrastValue, pInspectorContrastStack, pInspectorContrastBody,
            "Inspector.Video.ContrastRequiresEq",
            "Inspector.Video.ApplyContrast", "Inspector.Video.PersistContrast");
        PToneSectionUpdate(
            LColorKind.LColorKindSaturation,
            pToneSaturationBox, pInspectorSaturationPersistent, pInspectorSaturationSlider,
            pInspectorSaturationValue, pInspectorSaturationStack, pInspectorSaturationBody,
            "Inspector.Video.SaturationRequiresEq",
            "Inspector.Video.ApplySaturation", "Inspector.Video.PersistSaturation");
    }

    private void PToneSectionUpdate(
        LColorKind pKind,
        CheckBox pApply,
        CheckBox pPersistent,
        Slider pSlider,
        TextBox pValue,
        StackPanel pStack,
        StackPanel pBody,
        string pDisabledKey,
        string pApplyKey,
        string pPersistKey)
    {
        LWorkVideoStep pStep = LTone.LToneStepRead(pKind);
        PInspectorSwitchUpdate(pApply, pStep.LWorkStepActive);
        PInspectorSwitchUpdate(pPersistent, LTone.LTonePersistentRead(pKind));
        PInspectorValueUpdate(pSlider, pValue, pStep.LWorkStepValue, "0.#");
        PInspectorSectionApply(pApply, pPersistent, pStack, pBody, LInspector.LInspectorTipResolve(
            pStep.LWorkStepActive, LTone.LToneCapable, true, pDisabledKey, string.Empty, pApplyKey, pPersistKey));
    }
}
