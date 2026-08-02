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
    private const double PNoiseFloorLeast = -80;
    private const double PNoiseFloorMost = -20;
    private const double PNoiseAdaptivityLeast = 0;
    private const double PNoiseAdaptivityMost = 1;

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
    private bool pInspectorNoisePresetSuppress;
    private string? pInspectorNoiseBaseToken;

    private LGrain PNoiseTypeRead() => pInspectorNoiseType.SelectedIndex switch
    {
        1 => LGrain.LGrainVinyl,
        2 => LGrain.LGrainShellac,
        _ => LGrain.LGrainWhite
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
        pInspectorNoisePreset.Items.Add(new LLocalizationChoice("Dialogue", "Inspector.Noise.Dialogue"));
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
        PSlider.PSliderResetApply(pInspectorNoiseReduction, () => PNoisePresetCurrent()?.Reduction ?? 12);
        pNoiseReductionValue = PInspectorDecimalBuild();
        pNoiseReductionValue.Text = "12";
        pInspectorNoiseReduction.ValueChanged += (_, _) =>
        {
            if (pInspectorNoiseSuppress) { return; }
            pInspectorNoiseSuppress = true;
            pNoiseReductionValue.Text = pInspectorNoiseReduction.Value.ToString("0.#", CultureInfo.InvariantCulture);
            pInspectorNoiseSuppress = false;
            PNoiseDeviationCheck();
        };
        pNoiseReductionValue.TextChanged += (_, _) =>
        {
            if (pInspectorNoiseSuppress) { return; }
            pInspectorNoiseSuppress = true;
            pInspectorNoiseReduction.Value = Math.Clamp(
                PInspectorDecimalRead(pNoiseReductionValue, 12), PNoiseReductionLeast, PNoiseReductionMost);
            pInspectorNoiseSuppress = false;
            PNoiseDeviationCheck();
        };

        pInspectorNoiseSmooth = new Slider
        {
            Minimum = PNoiseSmoothLeast,
            Maximum = PNoiseSmoothMost,
            Value = 6,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pInspectorNoiseSmooth);
        PSlider.PSliderResetApply(pInspectorNoiseSmooth, () => PNoisePresetCurrent()?.Smooth ?? 6);
        pNoiseSmoothValue = PInspectorDecimalBuild();
        pNoiseSmoothValue.Text = "6";
        pInspectorNoiseSmooth.ValueChanged += (_, _) =>
        {
            if (pNoiseSmoothSuppress) { return; }
            pNoiseSmoothSuppress = true;
            pNoiseSmoothValue.Text = pInspectorNoiseSmooth.Value.ToString("0.#", CultureInfo.InvariantCulture);
            pNoiseSmoothSuppress = false;
            PNoiseDeviationCheck();
        };
        pNoiseSmoothValue.TextChanged += (_, _) =>
        {
            if (pNoiseSmoothSuppress) { return; }
            pNoiseSmoothSuppress = true;
            pInspectorNoiseSmooth.Value = Math.Clamp(
                PInspectorDecimalRead(pNoiseSmoothValue, 6), PNoiseSmoothLeast, PNoiseSmoothMost);
            pNoiseSmoothSuppress = false;
            PNoiseDeviationCheck();
        };

        pInspectorNoiseFloor = PInspectorDecimalBuild();
        pInspectorNoiseFloor.Text = "-50";
        Slider pNoiseFloorSlider = PInspectorSliderBind(
            pInspectorNoiseFloor, PNoiseFloorLeast, PNoiseFloorMost, -50, "0.#",
            () => PNoisePresetCurrent()?.Floor ?? -50, PNoiseDeviationCheck);
        pInspectorNoiseAdaptivity = PInspectorDecimalBuild();
        pInspectorNoiseAdaptivity.Text = "0.5";
        Slider pNoiseAdaptivitySlider = PInspectorSliderBind(
            pInspectorNoiseAdaptivity, PNoiseAdaptivityLeast, PNoiseAdaptivityMost, 0.5, "0.###",
            () => PNoisePresetCurrent()?.Adaptivity ?? 0.5, PNoiseDeviationCheck);
        pInspectorNoiseResidual = PInspectorDecimalBuild();
        pInspectorNoiseResidual.Text = "-38";
        Slider pNoiseResidualSlider = PInspectorSliderBind(
            pInspectorNoiseResidual, PNoiseFloorLeast, PNoiseFloorMost, -38, "0.#",
            () => PNoisePresetCurrent()?.Residual ?? -38, PNoiseDeviationCheck);

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
        pInspectorNoiseType.SelectionChanged += (_, _) => PNoiseDeviationCheck();

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
        pInspectorNoiseStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Floor"), pNoiseFloorSlider, "dB", pInspectorNoiseFloor));
        pInspectorNoiseStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Residual"), pNoiseResidualSlider, "dB", pInspectorNoiseResidual));
        pInspectorNoiseStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Adaptivity"), pNoiseAdaptivitySlider, "0-1", pInspectorNoiseAdaptivity));
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

    private static (double Reduction, double Floor, double Smooth, double Adaptivity, double Residual, int Type)? PNoiseValuesRead(string pToken) =>
        pToken switch
        {
            "Light" => (8d, -50d, 4d, 0.5d, -38d, 0),
            "Medium" => (12d, -50d, 6d, 0.5d, -38d, 0),
            "Strong" => (24d, -45d, 10d, 0.4d, -30d, 0),
            "Dialogue" => (10d, -50d, 5d, 0.8d, -40d, 0),
            "Vinyl" => (12d, -50d, 6d, 0.5d, -38d, 1),
            "Shellac" => (12d, -50d, 8d, 0.5d, -35d, 2),
            _ => null
        };

    private (double Reduction, double Floor, double Smooth, double Adaptivity, double Residual, int Type)? PNoisePresetCurrent() =>
        pInspectorNoiseBaseToken is { } pBase ? PNoiseValuesRead(pBase) : null;

    private static string PNoiseKeyRead(string pToken) => pToken switch
    {
        "Light" => "Inspector.Noise.Light",
        "Medium" => "Inspector.Noise.Medium",
        "Strong" => "Inspector.Noise.Strong",
        "Dialogue" => "Inspector.Noise.Dialogue",
        "Vinyl" => "Inspector.Noise.Vinyl",
        "Shellac" => "Inspector.Noise.Shellac",
        _ => "Inspector.Common.Custom"
    };

    private bool PNoiseValuesMatch((double Reduction, double Floor, double Smooth, double Adaptivity, double Residual, int Type) pPreset) =>
        Math.Abs(PInspectorDecimalRead(pNoiseReductionValue, 12) - pPreset.Reduction) < 0.05
        && Math.Abs(PInspectorDecimalRead(pInspectorNoiseFloor, -50) - pPreset.Floor) < 0.05
        && Math.Abs(PInspectorDecimalRead(pNoiseSmoothValue, 6) - pPreset.Smooth) < 0.05
        && Math.Abs(PInspectorDecimalRead(pInspectorNoiseAdaptivity, 0.5) - pPreset.Adaptivity) < 0.005
        && Math.Abs(PInspectorDecimalRead(pInspectorNoiseResidual, -38) - pPreset.Residual) < 0.05
        && pInspectorNoiseType.SelectedIndex == pPreset.Type;

    private void PNoiseValuesApply((double Reduction, double Floor, double Smooth, double Adaptivity, double Residual, int Type) pPreset)
    {
        pInspectorNoiseSuppress = true;
        pNoiseSmoothSuppress = true;
        pInspectorNoisePresetSuppress = true;
        pInspectorNoiseReduction.Value = Math.Clamp(pPreset.Reduction, PNoiseReductionLeast, PNoiseReductionMost);
        pNoiseReductionValue.Text = pPreset.Reduction.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseSmooth.Value = Math.Clamp(pPreset.Smooth, PNoiseSmoothLeast, PNoiseSmoothMost);
        pNoiseSmoothValue.Text = pPreset.Smooth.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseFloor.Text = pPreset.Floor.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseResidual.Text = pPreset.Residual.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseAdaptivity.Text = pPreset.Adaptivity.ToString("0.###", CultureInfo.InvariantCulture);
        pInspectorNoiseType.SelectedIndex = pPreset.Type;
        pInspectorNoiseSuppress = false;
        pNoiseSmoothSuppress = false;
        pInspectorNoisePresetSuppress = false;
    }

    private void PNoisePresetApply()
    {
        if (pInspectorNoisePresetSuppress)
        {
            return;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pInspectorNoisePreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom" || PNoiseValuesRead(pName) is not { } pPreset)
        {
            pInspectorNoiseBaseToken = null;
            return;
        }

        pInspectorNoiseBaseToken = pName;
        PNoiseValuesApply(pPreset);
        PNoiseCustomReset();
        PInspectorActiveRaise();
    }

    private void PNoiseDeviationCheck()
    {
        if (pInspectorNoisePresetSuppress || pInspectorNoiseBaseToken is not { } pBase
            || PNoiseValuesRead(pBase) is not { } pPreset)
        {
            return;
        }

        pInspectorNoisePresetSuppress = true;
        if (PNoiseValuesMatch(pPreset))
        {
            PNoiseCustomReset();
            PNoisePresetSelect(pBase);
        }
        else
        {
            PNoiseCustomSet(pBase);
        }

        pInspectorNoisePresetSuppress = false;
    }

    private void PNoiseCustomSet(string pBase)
    {
        int pLast = pInspectorNoisePreset.Items.Count - 1;
        string pText = LLocalization.LLocalizationFormat(
            "Inspector.Common.PresetCustom",
            LLocalization.LLocalizationTextRead(PNoiseKeyRead(pBase)));
        pInspectorNoisePreset.Items[pLast] = new LLocalizationChoice("Custom", string.Empty, pText);
        pInspectorNoisePreset.SelectedIndex = pLast;
    }

    private void PNoiseCustomReset()
    {
        int pLast = pInspectorNoisePreset.Items.Count - 1;
        pInspectorNoisePreset.Items[pLast] = new LLocalizationChoice("Custom", "Inspector.Common.Custom");
    }

    private void PNoisePresetSelect(string pToken)
    {
        for (int pIndex = 0; pIndex < pInspectorNoisePreset.Items.Count; pIndex++)
        {
            if (LLocalizationChoice.LLocalizationChoiceRead(pInspectorNoisePreset.Items[pIndex]) == pToken)
            {
                pInspectorNoisePreset.SelectedIndex = pIndex;
                return;
            }
        }
    }

    private void PNoiseValueSet(LWorkNoiseStep pStep)
    {
        pInspectorNoiseSuppress = true;
        pNoiseSmoothSuppress = true;
        pInspectorNoisePresetSuppress = true;
        pInspectorNoiseReduction.Value = Math.Clamp(
            pStep.LWorkNoiseReduction,
            PNoiseReductionLeast,
            PNoiseReductionMost);
        pNoiseReductionValue.Text = pStep.LWorkNoiseReduction.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseSmooth.Value = Math.Clamp(
            pStep.LWorkNoiseSmooth,
            PNoiseSmoothLeast,
            PNoiseSmoothMost);
        pNoiseSmoothValue.Text = pStep.LWorkNoiseSmooth.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseFloor.Text = pStep.LWorkNoiseFloor.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseResidual.Text = pStep.LWorkNoiseResidual.ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorNoiseAdaptivity.Text = pStep.LWorkNoiseAdaptivity.ToString("0.###", CultureInfo.InvariantCulture);
        pInspectorNoiseType.SelectedIndex = pStep.LWorkNoiseType switch
        {
            LGrain.LGrainVinyl => 1,
            LGrain.LGrainShellac => 2,
            _ => 0
        };
        pInspectorNoiseSuppress = false;
        pNoiseSmoothSuppress = false;
        pInspectorNoisePresetSuppress = false;
        PNoisePresetUpdate();
    }

    private void PNoisePresetUpdate()
    {
        pInspectorNoisePresetSuppress = true;
        string? pMatch = null;
        foreach (string pToken in new[] { "Light", "Medium", "Strong", "Dialogue", "Vinyl", "Shellac" })
        {
            if (PNoiseValuesRead(pToken) is { } pPreset && PNoiseValuesMatch(pPreset))
            {
                pMatch = pToken;
                break;
            }
        }

        if (pMatch is not null)
        {
            pInspectorNoiseBaseToken = pMatch;
            PNoiseCustomReset();
            PNoisePresetSelect(pMatch);
        }
        else
        {
            pInspectorNoiseBaseToken = null;
            PNoiseCustomReset();
            pInspectorNoisePreset.SelectedIndex = pInspectorNoisePreset.Items.Count - 1;
        }

        pInspectorNoisePresetSuppress = false;
    }

    private void PNoiseApplyUpdate()
    {
        bool pNoiseActive = pNoiseApplyBox.IsChecked == true;
        pInspectorNoiseStack.IsEnabled = pNoiseActive;
        pInspectorNoiseStack.Opacity = pNoiseActive ? 1 : 0.4;
        PInspectorActiveRaise();
    }
}
