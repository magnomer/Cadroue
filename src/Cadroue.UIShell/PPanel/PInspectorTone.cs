using System.Windows;
using System.Windows.Controls;
using Cadroue.Infrastructure;
using Cadroue.Core;

namespace Cadroue.UIShell.PPanel;

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

    private bool pToneCapable = true;

    private const double PToneBrightnessLeast = -100;
    private const double PToneBrightnessMost = 100;

    private StackPanel PToneBrightnessBuild()
    {
        pToneBrightnessBox = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), LLocalization.LLocalizationTextRead("Inspector.Video.ApplyBrightness"));
        pInspectorBrightnessPersistent = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"), LLocalization.LLocalizationTextRead("Inspector.Video.PersistBrightness"));
        pInspectorBrightnessSlider = PToneSliderBuild(
            PToneBrightnessLeast,
            PToneBrightnessMost,
            0);
        pInspectorBrightnessValue = PInspectorDecimalBuild();
        pInspectorBrightnessValue.Text = "0";
        pInspectorBrightnessStack = new StackPanel();
        PInspectorVideoAttach(
            pToneBrightnessBox,
            pInspectorBrightnessStack,
            pInspectorBrightnessSlider,
            pInspectorBrightnessValue,
            null,
            null,
            "0.#");
        pInspectorBrightnessStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Amount"), pInspectorBrightnessSlider, string.Empty, pInspectorBrightnessValue));
        pInspectorBrightnessBody = PToneBodyBuild(pToneBrightnessBox, pInspectorBrightnessStack);
        PToneApplyUpdate(pToneBrightnessBox, pInspectorBrightnessStack);
        return pInspectorBrightnessBody;
    }

    private StackPanel PToneContrastBuild()
    {
        pToneContrastBox = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), LLocalization.LLocalizationTextRead("Inspector.Video.ApplyContrast"));
        pInspectorContrastPersistent = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"), LLocalization.LLocalizationTextRead("Inspector.Video.PersistContrast"));
        pInspectorContrastSlider = PToneSliderBuild(0, 200, 100);
        pInspectorContrastValue = PInspectorDecimalBuild();
        pInspectorContrastValue.Text = "100";
        pInspectorContrastStack = new StackPanel();
        PInspectorVideoAttach(
            pToneContrastBox,
            pInspectorContrastStack,
            pInspectorContrastSlider,
            pInspectorContrastValue,
            0,
            200,
            "0.#");
        pInspectorContrastStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Amount"), pInspectorContrastSlider, "%", pInspectorContrastValue));
        bool pContrastPreview = LFlyleaf.LFlyleafActive
            || LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;
        if (!pContrastPreview)
        {
            pInspectorContrastStack.Children.Add(new TextBlock
            {
                Text = LLocalization.LLocalizationTextRead("Inspector.Video.ContrastPreview"),
                Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x64, 0x70, 0x82)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        pInspectorContrastBody = PToneBodyBuild(pToneContrastBox, pInspectorContrastStack);
        PToneApplyUpdate(pToneContrastBox, pInspectorContrastStack);
        return pInspectorContrastBody;
    }

    private StackPanel PToneSaturationBuild()
    {
        pToneSaturationBox = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), LLocalization.LLocalizationTextRead("Inspector.Video.ApplySaturation"));
        pInspectorSaturationPersistent = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"), LLocalization.LLocalizationTextRead("Inspector.Video.PersistSaturation"));
        pInspectorSaturationSlider = PToneSliderBuild(0, 200, 100);
        pInspectorSaturationValue = PInspectorDecimalBuild();
        pInspectorSaturationValue.Text = "100";
        pInspectorSaturationStack = new StackPanel();
        PInspectorVideoAttach(
            pToneSaturationBox,
            pInspectorSaturationStack,
            pInspectorSaturationSlider,
            pInspectorSaturationValue,
            0,
            200,
            "0.#");
        pInspectorSaturationStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Amount"), pInspectorSaturationSlider, "%", pInspectorSaturationValue));
        pInspectorSaturationBody = PToneBodyBuild(pToneSaturationBox, pInspectorSaturationStack);
        PToneApplyUpdate(pToneSaturationBox, pInspectorSaturationStack);
        return pInspectorSaturationBody;
    }

    public void PToneCapabilitySet(bool pCapable)
    {
        this.pToneCapable = pCapable;
        PInspectorSectionApply(
            pToneBrightnessBox, pInspectorBrightnessPersistent, pInspectorBrightnessStack, pInspectorBrightnessBody,
            pToneCapable, true, "Inspector.Video.BrightnessRequiresEq", string.Empty,
            "Inspector.Video.ApplyBrightness", "Inspector.Video.PersistBrightness");
        PInspectorSectionApply(
            pToneContrastBox, pInspectorContrastPersistent, pInspectorContrastStack, pInspectorContrastBody,
            pToneCapable, true, "Inspector.Video.ContrastRequiresEq", string.Empty,
            "Inspector.Video.ApplyContrast", "Inspector.Video.PersistContrast");
        PInspectorSectionApply(
            pToneSaturationBox, pInspectorSaturationPersistent, pInspectorSaturationStack, pInspectorSaturationBody,
            pToneCapable, true, "Inspector.Video.SaturationRequiresEq", string.Empty,
            "Inspector.Video.ApplySaturation", "Inspector.Video.PersistSaturation");
    }
}
