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
        pNoiseApplyBox = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), LLocalization.LLocalizationTextRead("Inspector.Noise.ApplyTooltip"));
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
            Minimum = PNoiseReductionLeast,
            Maximum = PNoiseReductionMost,
            Value = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pNoiseReduction);
        PSlider.PSliderResetApply(pNoiseReduction, () => PNoisePresetCurrent()?.LGrainReduction ?? 12);
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
                PInspectorDecimalRead(pNoiseReductionValue, 12), PNoiseReductionLeast, PNoiseReductionMost);
            pNoiseSuppress = false;
            PNoiseDeviationCheck();
        };

        pNoiseSmooth = new Slider
        {
            Minimum = PNoiseSmoothLeast,
            Maximum = PNoiseSmoothMost,
            Value = 6,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pNoiseSmooth);
        PSlider.PSliderResetApply(pNoiseSmooth, () => PNoisePresetCurrent()?.LGrainSmooth ?? 6);
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
                PInspectorDecimalRead(pNoiseSmoothValue, 6), PNoiseSmoothLeast, PNoiseSmoothMost);
            pNoiseSmoothSuppress = false;
            PNoiseDeviationCheck();
        };

        pNoiseFloor = PInspectorDecimalBuild();
        pNoiseFloor.Text = "-50";
        Slider pNoiseFloorSlider = PInspectorSliderBind(
            pNoiseFloor, PNoiseFloorLeast, PNoiseFloorMost, -50, "0.#",
            () => PNoisePresetCurrent()?.LGrainFloor ?? -50, PNoiseDeviationCheck);
        pNoiseAdaptivity = PInspectorDecimalBuild();
        pNoiseAdaptivity.Text = "0.5";
        Slider pNoiseAdaptivitySlider = PInspectorSliderBind(
            pNoiseAdaptivity, PNoiseAdaptivityLeast, PNoiseAdaptivityMost, 0.5, "0.###",
            () => PNoisePresetCurrent()?.LGrainAdaptivity ?? 0.5, PNoiseDeviationCheck);
        pNoiseResidual = PInspectorDecimalBuild();
        pNoiseResidual.Text = "-38";
        Slider pNoiseResidualSlider = PInspectorSliderBind(
            pNoiseResidual, PNoiseFloorLeast, PNoiseFloorMost, -38, "0.#",
            () => PNoisePresetCurrent()?.LGrainResidual ?? -38, PNoiseDeviationCheck);

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
        PMainWindow.PCheckbox.PCheckboxApply(pNoiseTrack);

        pNoiseStack = new StackPanel();
        pNoiseStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pNoisePreset));
        pNoiseStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Amount"), pNoiseReduction, "dB", pNoiseReductionValue));
        pNoiseStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Smoothing"), pNoiseSmooth, "gs", pNoiseSmoothValue));
        pNoiseStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Floor"), pNoiseFloorSlider, "dB", pNoiseFloor));
        pNoiseStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Residual"), pNoiseResidualSlider, "dB", pNoiseResidual));
        pNoiseStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Adaptivity"), pNoiseAdaptivitySlider, "0-1", pNoiseAdaptivity));
        pNoiseStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Noise"), pNoiseType));
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

    private LGrainPreset? PNoisePresetCurrent() =>
        pNoiseBaseToken is { } pBase ? LGrainCatalog.LGrainRead(pBase) : null;

    private string? PNoiseMatchRead() => LGrainCatalog.LGrainMatch(
        PInspectorDecimalRead(pNoiseReductionValue, 12),
        PInspectorDecimalRead(pNoiseFloor, -50),
        PInspectorDecimalRead(pNoiseSmoothValue, 6),
        PInspectorDecimalRead(pNoiseAdaptivity, 0.5),
        PInspectorDecimalRead(pNoiseResidual, -38),
        PNoiseTypeRead());

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

    private void PNoiseValuesApply(LGrainPreset pPreset)
    {
        pNoiseSuppress = true;
        pNoiseSmoothSuppress = true;
        pNoisePresetSuppress = true;
        pNoiseReduction.Value = Math.Clamp(pPreset.LGrainReduction, PNoiseReductionLeast, PNoiseReductionMost);
        pNoiseReductionValue.Text = pPreset.LGrainReduction.ToString("0.#", CultureInfo.InvariantCulture);
        pNoiseSmooth.Value = Math.Clamp(pPreset.LGrainSmooth, PNoiseSmoothLeast, PNoiseSmoothMost);
        pNoiseSmoothValue.Text = pPreset.LGrainSmooth.ToString("0.#", CultureInfo.InvariantCulture);
        pNoiseFloor.Text = pPreset.LGrainFloor.ToString("0.#", CultureInfo.InvariantCulture);
        pNoiseResidual.Text = pPreset.LGrainResidual.ToString("0.#", CultureInfo.InvariantCulture);
        pNoiseAdaptivity.Text = pPreset.LGrainAdaptivity.ToString("0.###", CultureInfo.InvariantCulture);
        pNoiseType.SelectedIndex = pPreset.LGrainType switch
        {
            LGrain.LGrainVinyl => 1,
            LGrain.LGrainShellac => 2,
            _ => 0
        };
        pNoiseSuppress = false;
        pNoiseSmoothSuppress = false;
        pNoisePresetSuppress = false;
    }

    private void PNoisePresetApply()
    {
        if (pNoisePresetSuppress)
        {
            return;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pNoisePreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom" || LGrainCatalog.LGrainRead(pName) is not { } pPreset)
        {
            pNoiseBaseToken = null;
            return;
        }

        pNoiseBaseToken = pName;
        PNoiseValuesApply(pPreset);
        PNoiseCustomReset();
        PInspectorActiveRaise();
    }

    private void PNoiseDeviationCheck()
    {
        if (pNoisePresetSuppress || pNoiseBaseToken is not { } pBase
            || LGrainCatalog.LGrainRead(pBase) is null)
        {
            return;
        }

        pNoisePresetSuppress = true;
        if (PNoiseMatchRead() == pBase)
        {
            PNoiseCustomReset();
            PNoisePresetSelect(pBase);
        }
        else
        {
            PNoiseCustomSet(pBase);
        }

        pNoisePresetSuppress = false;
    }

    private void PNoiseCustomSet(string pBase)
    {
        int pLast = pNoisePreset.Items.Count - 1;
        string pText = LLocalization.LLocalizationFormat(
            "Inspector.Common.PresetCustom",
            LLocalization.LLocalizationTextRead(PNoiseKeyRead(pBase)));
        pNoisePreset.Items[pLast] = new LLocalizationChoice("Custom", string.Empty, pText);
        pNoisePreset.SelectedIndex = pLast;
    }

    private void PNoiseCustomReset()
    {
        int pLast = pNoisePreset.Items.Count - 1;
        pNoisePreset.Items[pLast] = new LLocalizationChoice("Custom", "Inspector.Common.Custom");
    }

    private void PNoisePresetSelect(string pToken)
    {
        for (int pIndex = 0; pIndex < pNoisePreset.Items.Count; pIndex++)
        {
            if (LLocalizationChoice.LLocalizationChoiceRead(pNoisePreset.Items[pIndex]) == pToken)
            {
                pNoisePreset.SelectedIndex = pIndex;
                return;
            }
        }
    }

    private void PNoiseValueSet(LWorkNoiseStep pStep)
    {
        pNoiseSuppress = true;
        pNoiseSmoothSuppress = true;
        pNoisePresetSuppress = true;
        pNoiseReduction.Value = Math.Clamp(
            pStep.LWorkNoiseReduction,
            PNoiseReductionLeast,
            PNoiseReductionMost);
        pNoiseReductionValue.Text = pStep.LWorkNoiseReduction.ToString("0.#", CultureInfo.InvariantCulture);
        pNoiseSmooth.Value = Math.Clamp(
            pStep.LWorkNoiseSmooth,
            PNoiseSmoothLeast,
            PNoiseSmoothMost);
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

    private void PNoisePresetUpdate()
    {
        pNoisePresetSuppress = true;
        string? pMatch = PNoiseMatchRead();

        if (pMatch is not null)
        {
            pNoiseBaseToken = pMatch;
            PNoiseCustomReset();
            PNoisePresetSelect(pMatch);
        }
        else
        {
            pNoiseBaseToken = null;
            PNoiseCustomReset();
            pNoisePreset.SelectedIndex = pNoisePreset.Items.Count - 1;
        }

        pNoisePresetSuppress = false;
    }

    private void PNoiseApplyUpdate()
    {
        bool pNoiseActive = pNoiseApplyBox.IsChecked == true;
        pNoiseStack.IsEnabled = pNoiseActive;
        pNoiseStack.Opacity = pNoiseActive ? 1 : 0.4;
        PInspectorActiveRaise();
    }
}
