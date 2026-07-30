using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private const double PInspectorNoiseMinReduction = 0;
    private const double PInspectorNoiseMaxReduction = 30;
    private const double PInspectorNoiseMinSmooth = 0;
    private const double PInspectorNoiseMaxSmooth = 50;

    private CheckBox pInspectorNoiseApply = null!;
    private CheckBox pInspectorNoisePersistent = null!;
    private ComboBox pInspectorNoisePreset = null!;
    private Slider pInspectorNoiseReduction = null!;
    private TextBox pInspectorNoiseReductionValue = null!;
    private TextBox pInspectorNoiseFloor = null!;
    private Slider pInspectorNoiseSmooth = null!;
    private TextBox pInspectorNoiseSmoothValue = null!;
    private TextBox pInspectorNoiseAdaptivity = null!;
    private TextBox pInspectorNoiseResidual = null!;
    private ComboBox pInspectorNoiseType = null!;
    private CheckBox pInspectorNoiseTrack = null!;
    private StackPanel pInspectorNoiseStack = null!;
    private StackPanel pInspectorNoiseBody = null!;
    private bool pInspectorNoiseSuppress;
    private bool pInspectorNoiseSmoothSuppress;

    private LWorkAudioNoiseType PInspectorNoiseTypeRead() => pInspectorNoiseType.SelectedIndex switch
    {
        1 => LWorkAudioNoiseType.LWorkAudioNoiseVinyl,
        2 => LWorkAudioNoiseType.LWorkAudioNoiseShellac,
        _ => LWorkAudioNoiseType.LWorkAudioNoiseWhite
    };

    private StackPanel PInspectorNoiseBodyBuild()
    {
        pInspectorNoiseApply = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), LLocalization.LLocalizationTextRead("Inspector.Noise.ApplyTooltip"));
        pInspectorNoiseApply.Checked += (_, _) => PInspectorNoiseApplyUpdate();
        pInspectorNoiseApply.Unchecked += (_, _) => PInspectorNoiseApplyUpdate();

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
        pInspectorNoisePreset.SelectionChanged += (_, _) => PInspectorNoisePresetApply();

        pInspectorNoiseReduction = new Slider
        {
            Minimum = PInspectorNoiseMinReduction,
            Maximum = PInspectorNoiseMaxReduction,
            Value = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pInspectorNoiseReduction);
        pInspectorNoiseReductionValue = PInspectorDecimalBoxBuild();
        pInspectorNoiseReductionValue.Text = "12";
        pInspectorNoiseReduction.ValueChanged += (_, _) =>
        {
            if (pInspectorNoiseSuppress) { return; }
            pInspectorNoiseSuppress = true;
            pInspectorNoiseReductionValue.Text = pInspectorNoiseReduction.Value.ToString("0.#", CultureInfo.InvariantCulture);
            pInspectorNoiseSuppress = false;
        };
        pInspectorNoiseReductionValue.TextChanged += (_, _) =>
        {
            if (pInspectorNoiseSuppress) { return; }
            pInspectorNoiseSuppress = true;
            pInspectorNoiseReduction.Value = Math.Clamp(
                PInspectorDecimalRead(pInspectorNoiseReductionValue, 12), PInspectorNoiseMinReduction, PInspectorNoiseMaxReduction);
            pInspectorNoiseSuppress = false;
        };

        pInspectorNoiseSmooth = new Slider
        {
            Minimum = PInspectorNoiseMinSmooth,
            Maximum = PInspectorNoiseMaxSmooth,
            Value = 6,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pInspectorNoiseSmooth);
        pInspectorNoiseSmoothValue = PInspectorDecimalBoxBuild();
        pInspectorNoiseSmoothValue.Text = "6";
        pInspectorNoiseSmooth.ValueChanged += (_, _) =>
        {
            if (pInspectorNoiseSmoothSuppress) { return; }
            pInspectorNoiseSmoothSuppress = true;
            pInspectorNoiseSmoothValue.Text = pInspectorNoiseSmooth.Value.ToString("0.#", CultureInfo.InvariantCulture);
            pInspectorNoiseSmoothSuppress = false;
        };
        pInspectorNoiseSmoothValue.TextChanged += (_, _) =>
        {
            if (pInspectorNoiseSmoothSuppress) { return; }
            pInspectorNoiseSmoothSuppress = true;
            pInspectorNoiseSmooth.Value = Math.Clamp(
                PInspectorDecimalRead(pInspectorNoiseSmoothValue, 6), PInspectorNoiseMinSmooth, PInspectorNoiseMaxSmooth);
            pInspectorNoiseSmoothSuppress = false;
        };

        pInspectorNoiseFloor = PInspectorDecimalBoxBuild();
        pInspectorNoiseFloor.Text = "-50";
        pInspectorNoiseAdaptivity = PInspectorDecimalBoxBuild();
        pInspectorNoiseAdaptivity.Text = "0.5";
        pInspectorNoiseResidual = PInspectorDecimalBoxBuild();
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
        pInspectorNoiseStack.Children.Add(PInspectorPassSliderRowBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Amount"), pInspectorNoiseReduction, "dB", pInspectorNoiseReductionValue));
        pInspectorNoiseStack.Children.Add(PInspectorPassSliderRowBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Smoothing"), pInspectorNoiseSmooth, "gs", pInspectorNoiseSmoothValue));
        pInspectorNoiseStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Floor"), PInspectorNormalizeUnitRowBuild(pInspectorNoiseFloor, "dB")));
        pInspectorNoiseStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Residual"), PInspectorNormalizeUnitRowBuild(pInspectorNoiseResidual, "dB")));
        pInspectorNoiseStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Adaptivity"), PInspectorNormalizeUnitRowBuild(pInspectorNoiseAdaptivity, "0-1")));
        pInspectorNoiseStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Noise"), pInspectorNoiseType));
        pInspectorNoiseStack.Children.Add(pInspectorNoiseTrack);

        pInspectorNoiseBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pInspectorNoiseBody.Children.Add(pInspectorNoiseApply);
        pInspectorNoiseBody.Children.Add(PInspectorSeparatorBuild());
        pInspectorNoiseBody.Children.Add(pInspectorNoiseStack);

        PInspectorNoiseApplyUpdate();
        PInspectorNoisePresetApply();
        return pInspectorNoiseBody;
    }

    private void PInspectorNoisePresetApply()
    {
        string pName = LLocalizationChoice.LLocalizationChoiceRead(pInspectorNoisePreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom")
        {
            PInspectorNoiseLock(false);
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
        pInspectorNoiseSmoothSuppress = true;
        pInspectorNoiseReduction.Value = Math.Clamp(pReduction, PInspectorNoiseMinReduction, PInspectorNoiseMaxReduction);
        pInspectorNoiseReductionValue.Text = pReduction.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseSmooth.Value = Math.Clamp(pSmooth, PInspectorNoiseMinSmooth, PInspectorNoiseMaxSmooth);
        pInspectorNoiseSmoothValue.Text = pSmooth.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseSuppress = false;
        pInspectorNoiseSmoothSuppress = false;

        pInspectorNoiseFloor.Text = pFloor.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseResidual.Text = pResidual.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseAdaptivity.Text = pAdaptivity.ToString("0.###", CultureInfo.InvariantCulture);
        pInspectorNoiseType.SelectedIndex = pType;

        PInspectorNoiseLock(true);
    }

    private void PInspectorNoiseLock(bool pLocked)
    {
        bool pEnabled = !pLocked;
        double pOpacity = pLocked ? 0.6 : 1;
        UIElement[] pControls =
        {
            pInspectorNoiseReduction, pInspectorNoiseReductionValue,
            pInspectorNoiseSmooth, pInspectorNoiseSmoothValue,
            pInspectorNoiseFloor, pInspectorNoiseResidual,
            pInspectorNoiseAdaptivity, pInspectorNoiseType
        };
        foreach (UIElement pControl in pControls)
        {
            pControl.IsEnabled = pEnabled;
            pControl.Opacity = pOpacity;
        }
    }

    private void PInspectorNoiseValueSet(LWorkAudioStep pStep)
    {
        pInspectorNoiseSuppress = true;
        pInspectorNoiseSmoothSuppress = true;
        pInspectorNoiseReduction.Value = Math.Clamp(
            pStep.LWorkAudioStepReduction,
            PInspectorNoiseMinReduction,
            PInspectorNoiseMaxReduction);
        pInspectorNoiseReductionValue.Text = pStep.LWorkAudioStepReduction.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseSmooth.Value = Math.Clamp(
            pStep.LWorkAudioStepGainSmooth,
            PInspectorNoiseMinSmooth,
            PInspectorNoiseMaxSmooth);
        pInspectorNoiseSmoothValue.Text = pStep.LWorkAudioStepGainSmooth.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseSuppress = false;
        pInspectorNoiseSmoothSuppress = false;

        pInspectorNoiseFloor.Text = pStep.LWorkAudioStepNoiseFloor.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseResidual.Text = pStep.LWorkAudioStepResidualFloor.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseAdaptivity.Text = pStep.LWorkAudioStepAdaptivity.ToString("0.###", CultureInfo.InvariantCulture);
        PInspectorNoiseLock(false);
    }

    private void PInspectorNoiseApplyUpdate()
    {
        bool pNoiseActive = pInspectorNoiseApply.IsChecked == true;
        pInspectorNoiseStack.IsEnabled = pNoiseActive;
        pInspectorNoiseStack.Opacity = pNoiseActive ? 1 : 0.4;
        PInspectorAudioActiveRaise();
    }
}
