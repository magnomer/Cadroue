using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private CheckBox pNoiseApplyBox = null!;
    private CheckBox pNoisePersistent = null!;
    private ComboBox pNoisePreset = null!;
    private Slider pNoiseReduction = null!;
    private TextBox pNoiseReductionValue = null!;
    private TextBox pNoiseFloor = null!;
    private Slider pNoiseSmooth = null!;
    private TextBox pNoiseSmoothValue = null!;
    private TextBox pNoiseAdaptivity = null!;
    private TextBox pNoiseResidual = null!;
    private ComboBox pNoiseType = null!;
    private CheckBox pNoiseTrack = null!;
    private StackPanel pNoiseStack = null!;
    private StackPanel pNoiseBody = null!;
    private bool pNoiseSuppress;
    private bool pNoiseSmoothSuppress;
    private bool pNoisePresetSuppress;
    private string? pNoiseBaseToken;

    private LGrain PNoiseTypeRead() => LGrainCatalog.LGrainParse(pNoiseType.SelectedIndex switch
    {
        1 => "Vinyl",
        2 => "Shellac",
        _ => "White"
    });

    private StackPanel PNoiseBodyBuild()
    {
        pNoiseApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Noise.ApplyTooltip"));
        pNoiseApplyBox.Checked += (_, _) => PNoiseApplyUpdate();
        pNoiseApplyBox.Unchecked += (_, _) => PNoiseApplyUpdate();

        pNoisePersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Noise.PersistentTooltip"));

        pNoisePreset = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pNoisePreset);
        pNoisePreset.Items.Add(new LLocalizationChoice("Light", "Inspector.Noise.Light"));
        pNoisePreset.Items.Add(new LLocalizationChoice("Medium", "Inspector.Noise.Medium"));
        pNoisePreset.Items.Add(new LLocalizationChoice("Strong", "Inspector.Noise.Strong"));
        pNoisePreset.Items.Add(new LLocalizationChoice("Dialogue", "Inspector.Noise.Dialogue"));
        pNoisePreset.Items.Add(new LLocalizationChoice("Vinyl", "Inspector.Noise.Vinyl"));
        pNoisePreset.Items.Add(new LLocalizationChoice("Shellac", "Inspector.Noise.Shellac"));
        pNoisePreset.Items.Add(new LLocalizationChoice("Custom", "Inspector.Common.Custom"));
        pNoisePreset.SelectedIndex = 1;
        pNoisePreset.SelectionChanged += (_, _) => PNoisePresetApply();

        pNoiseReduction = new Slider
        {
            Minimum = LGrainCatalog.LGrainReductionLeast,
            Maximum = LGrainCatalog.LGrainReductionMost,
            Value = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pNoiseReduction);
        PSlider.PSliderResetApply(pNoiseReduction, () => PNoisePresetRead()?.LGrainReduction ?? 12);
        pNoiseReductionValue = PInspectorDecimalBuild();
        pNoiseReductionValue.Text = "12";
        pNoiseReduction.ValueChanged += (_, _) =>
        {
            if (pNoiseSuppress) { return; }
            pNoiseSuppress = true;
            pNoiseReductionValue.Text = pNoiseReduction.Value.ToString("0.#", CultureInfo.InvariantCulture);
            pNoiseSuppress = false;
            PNoiseDeviationCheck();
        };
        pNoiseReductionValue.TextChanged += (_, _) =>
        {
            if (pNoiseSuppress) { return; }
            pNoiseSuppress = true;
            pNoiseReduction.Value = Math.Clamp(
                PInspectorDecimalRead(pNoiseReductionValue, 12),
                LGrainCatalog.LGrainReductionLeast,
                LGrainCatalog.LGrainReductionMost);
            pNoiseSuppress = false;
            PNoiseDeviationCheck();
        };

        pNoiseSmooth = new Slider
        {
            Minimum = LGrainCatalog.LGrainSmoothLeast,
            Maximum = LGrainCatalog.LGrainSmoothMost,
            Value = 6,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pNoiseSmooth);
        PSlider.PSliderResetApply(pNoiseSmooth, () => PNoisePresetRead()?.LGrainSmooth ?? 6);
        pNoiseSmoothValue = PInspectorDecimalBuild();
        pNoiseSmoothValue.Text = "6";
        pNoiseSmooth.ValueChanged += (_, _) =>
        {
            if (pNoiseSmoothSuppress) { return; }
            pNoiseSmoothSuppress = true;
            pNoiseSmoothValue.Text = pNoiseSmooth.Value.ToString("0.#", CultureInfo.InvariantCulture);
            pNoiseSmoothSuppress = false;
            PNoiseDeviationCheck();
        };
        pNoiseSmoothValue.TextChanged += (_, _) =>
        {
            if (pNoiseSmoothSuppress) { return; }
            pNoiseSmoothSuppress = true;
            pNoiseSmooth.Value = Math.Clamp(
                PInspectorDecimalRead(pNoiseSmoothValue, 6),
                LGrainCatalog.LGrainSmoothLeast,
                LGrainCatalog.LGrainSmoothMost);
            pNoiseSmoothSuppress = false;
            PNoiseDeviationCheck();
        };

        pNoiseFloor = PInspectorDecimalBuild();
        pNoiseFloor.Text = "-50";
        Slider pNoiseFloorSlider = PInspectorSliderBuild(
            pNoiseFloor, LGrainCatalog.LGrainFloorLeast, LGrainCatalog.LGrainFloorMost, -50, "0.#",
            () => PNoisePresetRead()?.LGrainFloor ?? -50, PNoiseDeviationCheck);
        pNoiseAdaptivity = PInspectorDecimalBuild();
        pNoiseAdaptivity.Text = "0.5";
        Slider pNoiseAdaptivitySlider = PInspectorSliderBuild(
            pNoiseAdaptivity, LGrainCatalog.LGrainAdaptivityLeast, LGrainCatalog.LGrainAdaptivityMost, 0.5, "0.###",
            () => PNoisePresetRead()?.LGrainAdaptivity ?? 0.5, PNoiseDeviationCheck);
        pNoiseResidual = PInspectorDecimalBuild();
        pNoiseResidual.Text = "-38";
        Slider pNoiseResidualSlider = PInspectorSliderBuild(
            pNoiseResidual, LGrainCatalog.LGrainFloorLeast, LGrainCatalog.LGrainFloorMost, -38, "0.#",
            () => PNoisePresetRead()?.LGrainResidual ?? -38, PNoiseDeviationCheck);

        pNoiseType = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pNoiseType);
        pNoiseType.Items.Add(new LLocalizationChoice("White", "Inspector.Noise.White"));
        pNoiseType.Items.Add(new LLocalizationChoice("Vinyl", "Inspector.Noise.Vinyl"));
        pNoiseType.Items.Add(new LLocalizationChoice("Shellac", "Inspector.Noise.Shellac"));
        pNoiseType.SelectedIndex = 0;
        pNoiseType.SelectionChanged += (_, _) => PNoiseDeviationCheck();

        pNoiseTrack = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Noise.Track"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Noise.TrackTooltip"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        PHouse.PCheckbox.PCheckboxApply(pNoiseTrack);

        pNoiseStack = new StackPanel();
        pNoiseStack.Children.Add(
            PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pNoisePreset));
        pNoiseStack.Children.Add(
            PFilterSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Common.Amount"),
                pNoiseReduction,
                "dB",
                pNoiseReductionValue));
        pNoiseStack.Children.Add(
            PFilterSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Noise.Smoothing"),
                pNoiseSmooth,
                "gs",
                pNoiseSmoothValue));
        pNoiseStack.Children.Add(
            PFilterSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Noise.Floor"),
                pNoiseFloorSlider,
                "dB",
                pNoiseFloor));
        pNoiseStack.Children.Add(
            PFilterSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Noise.Residual"),
                pNoiseResidualSlider,
                "dB",
                pNoiseResidual));
        pNoiseStack.Children.Add(
            PFilterSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Noise.Adaptivity"),
                pNoiseAdaptivitySlider,
                "0-1",
                pNoiseAdaptivity));
        pNoiseStack.Children.Add(
            PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Noise"), pNoiseType));
        pNoiseStack.Children.Add(pNoiseTrack);

        pNoiseBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pNoiseBody.Children.Add(pNoiseApplyBox);
        pNoiseBody.Children.Add(PInspectorSeparatorBuild());
        pNoiseBody.Children.Add(pNoiseStack);

        PNoiseApplyUpdate();
        PNoisePresetApply();
        return pNoiseBody;
    }

    private void PNoiseValueSet(LWorkNoiseStep pStep)
    {
        pNoiseSuppress = true;
        pNoiseSmoothSuppress = true;
        pNoisePresetSuppress = true;
        pNoiseReduction.Value = Math.Clamp(
            pStep.LWorkNoiseReduction,
            LGrainCatalog.LGrainReductionLeast,
            LGrainCatalog.LGrainReductionMost);
        pNoiseReductionValue.Text = pStep.LWorkNoiseReduction.ToString("0.#", CultureInfo.InvariantCulture);
        pNoiseSmooth.Value = Math.Clamp(
            pStep.LWorkNoiseSmooth,
            LGrainCatalog.LGrainSmoothLeast,
            LGrainCatalog.LGrainSmoothMost);
        pNoiseSmoothValue.Text = pStep.LWorkNoiseSmooth.ToString("0.#", CultureInfo.InvariantCulture);
        pNoiseFloor.Text = pStep.LWorkNoiseFloor.ToString("0.#", CultureInfo.InvariantCulture);
        pNoiseResidual.Text = pStep.LWorkNoiseResidual.ToString("0.#", CultureInfo.InvariantCulture);
        pNoiseAdaptivity.Text = pStep.LWorkNoiseAdaptivity.ToString("0.###", CultureInfo.InvariantCulture);
        pNoiseType.SelectedIndex = LGrainCatalog.LGrainFormat(pStep.LWorkNoiseType) switch
        {
            "Vinyl" => 1,
            "Shellac" => 2,
            _ => 0
        };
        pNoiseSuppress = false;
        pNoiseSmoothSuppress = false;
        pNoisePresetSuppress = false;
        PNoisePresetUpdate();
    }

    private void PNoiseApplyUpdate()
    {
        bool pNoiseActive = pNoiseApplyBox.IsChecked == true;
        pNoiseStack.IsEnabled = pNoiseActive;
        pNoiseStack.Opacity = pNoiseActive ? 1 : 0.4;
        PInspectorActiveRaise();
    }
}
