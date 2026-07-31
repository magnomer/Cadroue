using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private const double PNoiseReductionLeast = 0;
    private const double PNoiseReductionMost = 30;
    private const double PNoiseSmoothLeast = 0;
    private const double PNoiseSmoothMost = 50;

    private CheckBox pNoiseApplyBox = null!;
    private CheckBox pInspectorNoisePersistent = null!;
    private ComboBox pInspectorNoisePreset = null!;
    private Slider pInspectorNoiseReduction = null!;
    private TextBox pNoiseReductionValue = null!;
    private TextBox pInspectorNoiseFloor = null!;
    private Slider pInspectorNoiseSmooth = null!;
    private TextBox pNoiseSmoothValue = null!;
    private TextBox pInspectorNoiseAdaptivity = null!;
    private TextBox pInspectorNoiseResidual = null!;
    private ComboBox pInspectorNoiseType = null!;
    private CheckBox pInspectorNoiseTrack = null!;
    private StackPanel pInspectorNoiseStack = null!;
    private StackPanel pInspectorNoiseBody = null!;
    private bool pInspectorNoiseSuppress;
    private bool pNoiseSmoothSuppress;

    private LWorkAudioNoiseType PNoiseTypeRead() => pInspectorNoiseType.SelectedIndex switch
    {
        1 => LWorkAudioNoiseType.LWorkAudioNoiseVinyl,
        2 => LWorkAudioNoiseType.LWorkAudioNoiseShellac,
        _ => LWorkAudioNoiseType.LWorkAudioNoiseWhite
    };

    private StackPanel PNoiseBodyBuild()
    {
        pNoiseApplyBox = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), LLocalization.LLocalizationTextRead("Inspector.Noise.ApplyTooltip"));
        pNoiseApplyBox.Checked += (_, _) => PNoiseApplyUpdate();
        pNoiseApplyBox.Unchecked += (_, _) => PNoiseApplyUpdate();

        pInspectorNoisePersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Noise.PersistentTooltip"));

        pInspectorNoisePreset = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pInspectorNoisePreset);
        pInspectorNoisePreset.Items.Add(new LLocalizationChoice("Light", "Inspector.Noise.Light"));
        pInspectorNoisePreset.Items.Add(new LLocalizationChoice("Medium", "Inspector.Noise.Medium"));
        pInspectorNoisePreset.Items.Add(new LLocalizationChoice("Strong", "Inspector.Noise.Strong"));
        pInspectorNoisePreset.Items.Add(new LLocalizationChoice("Vinyl", "Inspector.Noise.Vinyl"));
        pInspectorNoisePreset.Items.Add(new LLocalizationChoice("Shellac", "Inspector.Noise.Shellac"));
        pInspectorNoisePreset.Items.Add(new LLocalizationChoice("Custom", "Inspector.Common.Custom"));
        pInspectorNoisePreset.SelectedIndex = 1;
        pInspectorNoisePreset.SelectionChanged += (_, _) => PNoisePresetApply();

        pInspectorNoiseReduction = new Slider
        {
            Minimum = PNoiseReductionLeast,
            Maximum = PNoiseReductionMost,
            Value = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pInspectorNoiseReduction);
        pNoiseReductionValue = PInspectorDecimalBuild();
        pNoiseReductionValue.Text = "12";
        pInspectorNoiseReduction.ValueChanged += (_, _) =>
        {
            if (pInspectorNoiseSuppress) { return; }
            pInspectorNoiseSuppress = true;
            pNoiseReductionValue.Text = pInspectorNoiseReduction.Value.ToString("0.#", CultureInfo.InvariantCulture);
            pInspectorNoiseSuppress = false;
        };
        pNoiseReductionValue.TextChanged += (_, _) =>
        {
            if (pInspectorNoiseSuppress) { return; }
            pInspectorNoiseSuppress = true;
            pInspectorNoiseReduction.Value = Math.Clamp(
                PInspectorDecimalRead(pNoiseReductionValue, 12), PNoiseReductionLeast, PNoiseReductionMost);
            pInspectorNoiseSuppress = false;
        };

        pInspectorNoiseSmooth = new Slider
        {
            Minimum = PNoiseSmoothLeast,
            Maximum = PNoiseSmoothMost,
            Value = 6,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pInspectorNoiseSmooth);
        pNoiseSmoothValue = PInspectorDecimalBuild();
        pNoiseSmoothValue.Text = "6";
        pInspectorNoiseSmooth.ValueChanged += (_, _) =>
        {
            if (pNoiseSmoothSuppress) { return; }
            pNoiseSmoothSuppress = true;
            pNoiseSmoothValue.Text = pInspectorNoiseSmooth.Value.ToString("0.#", CultureInfo.InvariantCulture);
            pNoiseSmoothSuppress = false;
        };
        pNoiseSmoothValue.TextChanged += (_, _) =>
        {
            if (pNoiseSmoothSuppress) { return; }
            pNoiseSmoothSuppress = true;
            pInspectorNoiseSmooth.Value = Math.Clamp(
                PInspectorDecimalRead(pNoiseSmoothValue, 6), PNoiseSmoothLeast, PNoiseSmoothMost);
            pNoiseSmoothSuppress = false;
        };

        pInspectorNoiseFloor = PInspectorDecimalBuild();
        pInspectorNoiseFloor.Text = "-50";
        pInspectorNoiseAdaptivity = PInspectorDecimalBuild();
        pInspectorNoiseAdaptivity.Text = "0.5";
        pInspectorNoiseResidual = PInspectorDecimalBuild();
        pInspectorNoiseResidual.Text = "-38";

        pInspectorNoiseType = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pInspectorNoiseType);
        pInspectorNoiseType.Items.Add(new LLocalizationChoice("White", "Inspector.Noise.White"));
        pInspectorNoiseType.Items.Add(new LLocalizationChoice("Vinyl", "Inspector.Noise.Vinyl"));
        pInspectorNoiseType.Items.Add(new LLocalizationChoice("Shellac", "Inspector.Noise.Shellac"));
        pInspectorNoiseType.SelectedIndex = 0;

        pInspectorNoiseTrack = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Noise.Track"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Noise.TrackTooltip"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        PMainWindow.PCheckbox.PCheckboxApply(pInspectorNoiseTrack);

        pInspectorNoiseStack = new StackPanel();
        pInspectorNoiseStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pInspectorNoisePreset));
        pInspectorNoiseStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Amount"), pInspectorNoiseReduction, "dB", pNoiseReductionValue));
        pInspectorNoiseStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Smoothing"), pInspectorNoiseSmooth, "gs", pNoiseSmoothValue));
        pInspectorNoiseStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Floor"), PLoudnessRowBuild(pInspectorNoiseFloor, "dB")));
        pInspectorNoiseStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Residual"), PLoudnessRowBuild(pInspectorNoiseResidual, "dB")));
        pInspectorNoiseStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Adaptivity"), PLoudnessRowBuild(pInspectorNoiseAdaptivity, "0-1")));
        pInspectorNoiseStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Noise"), pInspectorNoiseType));
        pInspectorNoiseStack.Children.Add(pInspectorNoiseTrack);

        pInspectorNoiseBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pInspectorNoiseBody.Children.Add(pNoiseApplyBox);
        pInspectorNoiseBody.Children.Add(PInspectorSeparatorBuild());
        pInspectorNoiseBody.Children.Add(pInspectorNoiseStack);

        PNoiseApplyUpdate();
        PNoisePresetApply();
        return pInspectorNoiseBody;
    }

    private void PNoisePresetApply()
    {
        string pName = LLocalizationChoice.LLocalizationChoiceRead(pInspectorNoisePreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom")
        {
            PNoiseSet(false);
            return;
        }

        (double pReduction, double pFloor, double pSmooth, double pAdaptivity, double pResidual, int pType) = pName switch
        {
            "Light" => (8d, -50d, 4d, 0.5d, -38d, 0),
            "Strong" => (24d, -45d, 10d, 0.4d, -30d, 0),
            "Vinyl" => (12d, -50d, 6d, 0.5d, -38d, 1),
            "Shellac" => (12d, -50d, 8d, 0.5d, -35d, 2),
            _ => (12d, -50d, 6d, 0.5d, -38d, 0)
        };

        pInspectorNoiseSuppress = true;
        pNoiseSmoothSuppress = true;
        pInspectorNoiseReduction.Value = Math.Clamp(pReduction, PNoiseReductionLeast, PNoiseReductionMost);
        pNoiseReductionValue.Text = pReduction.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseSmooth.Value = Math.Clamp(pSmooth, PNoiseSmoothLeast, PNoiseSmoothMost);
        pNoiseSmoothValue.Text = pSmooth.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseSuppress = false;
        pNoiseSmoothSuppress = false;

        pInspectorNoiseFloor.Text = pFloor.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseResidual.Text = pResidual.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseAdaptivity.Text = pAdaptivity.ToString("0.###", CultureInfo.InvariantCulture);
        pInspectorNoiseType.SelectedIndex = pType;

        PNoiseSet(true);
    }

    private void PNoiseSet(bool pLocked)
    {
        bool pEnabled = !pLocked;
        double pOpacity = pLocked ? 0.6 : 1;
        UIElement[] pControls =
        {
            pInspectorNoiseReduction, pNoiseReductionValue,
            pInspectorNoiseSmooth, pNoiseSmoothValue,
            pInspectorNoiseFloor, pInspectorNoiseResidual,
            pInspectorNoiseAdaptivity, pInspectorNoiseType
        };
        foreach (UIElement pControl in pControls)
        {
            pControl.IsEnabled = pEnabled;
            pControl.Opacity = pOpacity;
        }
    }

    private void PNoiseValueSet(LWorkAudioStep pStep)
    {
        pInspectorNoiseSuppress = true;
        pNoiseSmoothSuppress = true;
        pInspectorNoiseReduction.Value = Math.Clamp(
            pStep.LWorkAudioStepReduction,
            PNoiseReductionLeast,
            PNoiseReductionMost);
        pNoiseReductionValue.Text = pStep.LWorkAudioStepReduction.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseSmooth.Value = Math.Clamp(
            pStep.LWorkAudioStepGainSmooth,
            PNoiseSmoothLeast,
            PNoiseSmoothMost);
        pNoiseSmoothValue.Text = pStep.LWorkAudioStepGainSmooth.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseSuppress = false;
        pNoiseSmoothSuppress = false;

        pInspectorNoiseFloor.Text = pStep.LWorkAudioStepNoiseFloor.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseResidual.Text = pStep.LWorkAudioStepResidualFloor.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseAdaptivity.Text = pStep.LWorkAudioStepAdaptivity.ToString("0.###", CultureInfo.InvariantCulture);
        PNoiseSet(false);
    }

    private void PNoiseApplyUpdate()
    {
        bool pNoiseActive = pNoiseApplyBox.IsChecked == true;
        pInspectorNoiseStack.IsEnabled = pNoiseActive;
        pInspectorNoiseStack.Opacity = pNoiseActive ? 1 : 0.4;
        PInspectorActiveRaise();
    }
}
