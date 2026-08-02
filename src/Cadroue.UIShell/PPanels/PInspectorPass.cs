using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private const double PFilterStagesLeast = 1;
    private const double PFilterStagesMost = 8;
    private const double PFilterResonanceLeast = 0.1;
    private const double PFilterResonanceMost = 2;

    private sealed record PInspectorPassPreset(
        string PInspectorPassPresetToken,
        string PInspectorPassPresetKey,
        double Cutoff,
        int Stages,
        int Poles,
        double Resonance);

    private static readonly PInspectorPassPreset[] pFilterHighPresets =
    {
        new("Rumble", "Inspector.Pass.Preset.Rumble", 30, 2, 2, 0.707),
        new("Voice", "Inspector.Pass.Preset.Voice", 80, 2, 2, 0.707),
        new("Speech (tight)", "Inspector.Pass.Preset.SpeechTight", 100, 4, 2, 0.707),
        new("De-mud", "Inspector.Pass.Preset.Demud", 200, 2, 2, 0.707)
    };

    private static readonly PInspectorPassPreset[] pFilterLowPresets =
    {
        new("De-hiss", "Inspector.Pass.Preset.Dehiss", 16000, 2, 2, 0.707),
        new("Soften", "Inspector.Pass.Preset.Soften", 10000, 2, 2, 0.707),
        new("Warm", "Inspector.Pass.Preset.Warm", 8000, 3, 2, 0.707),
        new("Telephone", "Inspector.Pass.Preset.Telephone", 3400, 4, 2, 0.707)
    };

    private sealed class PInspectorPass
    {
        public required CheckBox PFilterApplyBox { get; init; }
        public required CheckBox PInspectorPassPersistent { get; init; }
        public required ComboBox PInspectorPassPreset { get; init; }
        public required Slider PInspectorPassFrequency { get; init; }
        public required TextBox PInspectorPassValue { get; init; }
        public required Slider PInspectorPassStages { get; init; }
        public required TextBox PFilterStageValue { get; init; }
        public required ComboBox PInspectorPassPoles { get; init; }
        public required TextBox PInspectorPassResonance { get; init; }
        public required StackPanel PInspectorPassStack { get; init; }
        public required StackPanel PInspectorPassBody { get; init; }
        public required IReadOnlyList<PInspectorPassPreset> PInspectorPassPresets { get; init; }
        public double PInspectorPassMin { get; init; }
        public double PInspectorPassMax { get; init; }
        public double PInspectorPassDefault { get; init; }
        public bool PInspectorPassSuppress { get; set; }
        public bool PFilterStageSuppress { get; set; }
        public bool PInspectorPassPresetSuppress { get; set; }
        public string? PInspectorPassBaseToken { get; set; }
    }

    private PInspectorPass pInspectorHighPass = null!;
    private PInspectorPass pInspectorLowPass = null!;

    private StackPanel PFilterHighBuild()
    {
        pInspectorHighPass = PInspectorPassBuild(80, 20, 300, LLocalization.LLocalizationTextRead("Inspector.Pass.HighApply"), pFilterHighPresets, "Voice");
        return pInspectorHighPass.PInspectorPassBody;
    }

    private StackPanel PFilterLowBuild()
    {
        pInspectorLowPass = PInspectorPassBuild(16000, 3000, 20000, LLocalization.LLocalizationTextRead("Inspector.Pass.LowApply"), pFilterLowPresets, "De-hiss");
        return pInspectorLowPass.PInspectorPassBody;
    }

    private double PInspectorPassRead(PInspectorPass pPass) =>
        Math.Clamp(PInspectorDecimalRead(pPass.PInspectorPassValue, pPass.PInspectorPassDefault),
            pPass.PInspectorPassMin, pPass.PInspectorPassMax);

    private int PFilterStagesRead(PInspectorPass pPass) =>
        (int)Math.Clamp(Math.Round(PInspectorDecimalRead(pPass.PFilterStageValue, 1)),
            PFilterStagesLeast, PFilterStagesMost);

    private static int PFilterPolesRead(PInspectorPass pPass) =>
        pPass.PInspectorPassPoles.SelectedIndex == 0 ? 1 : 2;

    private double PFilterResonanceRead(PInspectorPass pPass) =>
        PInspectorDecimalRead(pPass.PInspectorPassResonance, 0.707);

    private PInspectorPass PInspectorPassBuild(
        double pDefault, double pMin, double pMax, string pApplyTip, IReadOnlyList<PInspectorPassPreset> pPresets, string pDefaultToken)
    {
        CheckBox pApply = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), pApplyTip);
        CheckBox pPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Pass.PersistentTooltip"));

        var pPreset = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pPreset);
        foreach (PInspectorPassPreset pPresetEntry in pPresets)
        {
            pPreset.Items.Add(new LLocalizationChoice(pPresetEntry.PInspectorPassPresetToken, pPresetEntry.PInspectorPassPresetKey));
        }
        pPreset.Items.Add(new LLocalizationChoice("Custom", "Inspector.Common.Custom"));
        int pDefaultIndex = pPreset.Items.Count - 1;
        for (int pIndex = 0; pIndex < pPresets.Count; pIndex++)
        {
            if (pPresets[pIndex].PInspectorPassPresetToken == pDefaultToken)
            {
                pDefaultIndex = pIndex;
                break;
            }
        }

        pPreset.SelectedIndex = pDefaultIndex;

        var pFrequency = new Slider { Minimum = pMin, Maximum = pMax, Value = pDefault, VerticalAlignment = VerticalAlignment.Center };
        PSlider.PSliderApply(pFrequency);
        TextBox pValue = PInspectorDecimalBuild();
        pValue.Text = pDefault.ToString("0", CultureInfo.InvariantCulture);

        var pStages = new Slider
        {
            Minimum = PFilterStagesLeast,
            Maximum = PFilterStagesMost,
            Value = 1,
            IsSnapToTickEnabled = true,
            TickFrequency = 1,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pStages);
        TextBox pStageValue = PInspectorDecimalBuild();
        pStageValue.Text = "1";

        var pPoles = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pPoles);
        pPoles.Items.Add("1 (6 dB)");
        pPoles.Items.Add("2 (12 dB)");
        pPoles.SelectedIndex = 1;

        TextBox pResonance = PInspectorDecimalBuild();
        pResonance.Text = "0.707";

        var pStack = new StackPanel();
        var pBody = new StackPanel { Margin = new Thickness(12, 12, 12, 12), Visibility = Visibility.Collapsed };
        var pPass = new PInspectorPass
        {
            PFilterApplyBox = pApply,
            PInspectorPassPersistent = pPersistent,
            PInspectorPassPreset = pPreset,
            PInspectorPassFrequency = pFrequency,
            PInspectorPassValue = pValue,
            PInspectorPassStages = pStages,
            PFilterStageValue = pStageValue,
            PInspectorPassPoles = pPoles,
            PInspectorPassResonance = pResonance,
            PInspectorPassStack = pStack,
            PInspectorPassBody = pBody,
            PInspectorPassPresets = pPresets,
            PInspectorPassMin = pMin,
            PInspectorPassMax = pMax,
            PInspectorPassDefault = pDefault
        };

        PSlider.PSliderResetApply(pFrequency, () => PFilterPresetCurrent(pPass) is { } pEntry ? pEntry.Cutoff : pDefault);
        PSlider.PSliderResetApply(pStages, () => PFilterPresetCurrent(pPass) is { } pEntry ? pEntry.Stages : 1);

        pApply.Checked += (_, _) => PFilterApplyUpdate(pPass);
        pApply.Unchecked += (_, _) => PFilterApplyUpdate(pPass);
        pPreset.SelectionChanged += (_, _) => PFilterPresetApply(pPass);

        pFrequency.ValueChanged += (_, _) =>
        {
            if (pPass.PInspectorPassSuppress) { return; }
            pPass.PInspectorPassSuppress = true;
            pValue.Text = pFrequency.Value.ToString("0", CultureInfo.InvariantCulture);
            pPass.PInspectorPassSuppress = false;
            PFilterDeviationCheck(pPass);
        };
        pValue.TextChanged += (_, _) =>
        {
            if (pPass.PInspectorPassSuppress) { return; }
            pPass.PInspectorPassSuppress = true;
            pFrequency.Value = Math.Clamp(PInspectorDecimalRead(pValue, pDefault), pMin, pMax);
            pPass.PInspectorPassSuppress = false;
            PFilterDeviationCheck(pPass);
        };
        pStages.ValueChanged += (_, _) =>
        {
            if (pPass.PFilterStageSuppress) { return; }
            pPass.PFilterStageSuppress = true;
            pStageValue.Text = pStages.Value.ToString("0", CultureInfo.InvariantCulture);
            pPass.PFilterStageSuppress = false;
            PFilterDeviationCheck(pPass);
        };
        pStageValue.TextChanged += (_, _) =>
        {
            if (pPass.PFilterStageSuppress) { return; }
            pPass.PFilterStageSuppress = true;
            pStages.Value = Math.Clamp(Math.Round(PInspectorDecimalRead(pStageValue, 1)), PFilterStagesLeast, PFilterStagesMost);
            pPass.PFilterStageSuppress = false;
            PFilterDeviationCheck(pPass);
        };
        pPoles.SelectionChanged += (_, _) => PFilterDeviationCheck(pPass);
        Slider pResonanceSlider = PInspectorSliderBind(
            pResonance, PFilterResonanceLeast, PFilterResonanceMost, 0.707, "0.###",
            () => PFilterPresetCurrent(pPass) is { } pEntry ? pEntry.Resonance : 0.707,
            () => PFilterDeviationCheck(pPass));

        pStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pPreset));
        pStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Pass.Cutoff"), pFrequency, "Hz", pValue));
        pStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Pass.Steepness"), pStages, "×12dB", pStageValue));
        pStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Pass.Poles"), pPoles));
        pStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Pass.Resonance"), pResonanceSlider, "Q", pResonance));

        pBody.Children.Add(pApply);
        pBody.Children.Add(PInspectorSeparatorBuild());
        pBody.Children.Add(pStack);

        PFilterApplyUpdate(pPass);
        PFilterPresetApply(pPass);
        return pPass;
    }

    private static PInspectorPassPreset? PFilterPresetByToken(PInspectorPass pPass, string pToken)
    {
        foreach (PInspectorPassPreset pEntry in pPass.PInspectorPassPresets)
        {
            if (pEntry.PInspectorPassPresetToken == pToken)
            {
                return pEntry;
            }
        }

        return null;
    }

    private static PInspectorPassPreset? PFilterPresetCurrent(PInspectorPass pPass) =>
        pPass.PInspectorPassBaseToken is { } pBase ? PFilterPresetByToken(pPass, pBase) : null;

    private bool PFilterValuesMatch(PInspectorPass pPass, PInspectorPassPreset pPreset) =>
        Math.Abs(PInspectorPassRead(pPass) - pPreset.Cutoff) < 0.5
        && PFilterStagesRead(pPass) == pPreset.Stages
        && PFilterPolesRead(pPass) == pPreset.Poles
        && Math.Abs(PFilterResonanceRead(pPass) - pPreset.Resonance) < 0.001;

    private static void PFilterValuesApply(PInspectorPass pPass, PInspectorPassPreset pPreset)
    {
        pPass.PInspectorPassSuppress = true;
        pPass.PFilterStageSuppress = true;
        pPass.PInspectorPassPresetSuppress = true;
        pPass.PInspectorPassFrequency.Value = Math.Clamp(pPreset.Cutoff, pPass.PInspectorPassMin, pPass.PInspectorPassMax);
        pPass.PInspectorPassValue.Text = pPreset.Cutoff.ToString("0", CultureInfo.InvariantCulture);
        pPass.PInspectorPassStages.Value = Math.Clamp(pPreset.Stages, PFilterStagesLeast, PFilterStagesMost);
        pPass.PFilterStageValue.Text = pPreset.Stages.ToString(CultureInfo.InvariantCulture);
        pPass.PInspectorPassPoles.SelectedIndex = pPreset.Poles == 1 ? 0 : 1;
        pPass.PInspectorPassResonance.Text = pPreset.Resonance.ToString("0.###", CultureInfo.InvariantCulture);
        pPass.PInspectorPassSuppress = false;
        pPass.PFilterStageSuppress = false;
        pPass.PInspectorPassPresetSuppress = false;
    }

    private void PFilterPresetApply(PInspectorPass pPass)
    {
        if (pPass.PInspectorPassPresetSuppress)
        {
            return;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pPass.PInspectorPassPreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom" || PFilterPresetByToken(pPass, pName) is not { } pPreset)
        {
            pPass.PInspectorPassBaseToken = null;
            return;
        }

        pPass.PInspectorPassBaseToken = pName;
        PFilterValuesApply(pPass, pPreset);
        PFilterCustomLabelReset(pPass);
        PInspectorActiveRaise();
    }

    private void PFilterDeviationCheck(PInspectorPass pPass)
    {
        if (pPass.PInspectorPassPresetSuppress || pPass.PInspectorPassBaseToken is not { } pBase
            || PFilterPresetByToken(pPass, pBase) is not { } pPreset)
        {
            return;
        }

        pPass.PInspectorPassPresetSuppress = true;
        if (PFilterValuesMatch(pPass, pPreset))
        {
            PFilterCustomLabelReset(pPass);
            PFilterPresetItemSelect(pPass, pBase);
        }
        else
        {
            PFilterCustomLabelSet(pPass, pPreset);
        }

        pPass.PInspectorPassPresetSuppress = false;
    }

    private static void PFilterCustomLabelSet(PInspectorPass pPass, PInspectorPassPreset pPreset)
    {
        int pLast = pPass.PInspectorPassPreset.Items.Count - 1;
        string pText = LLocalization.LLocalizationFormat(
            "Inspector.Common.PresetCustom",
            LLocalization.LLocalizationTextRead(pPreset.PInspectorPassPresetKey));
        pPass.PInspectorPassPreset.Items[pLast] = new LLocalizationChoice("Custom", string.Empty, pText);
        pPass.PInspectorPassPreset.SelectedIndex = pLast;
    }

    private static void PFilterCustomLabelReset(PInspectorPass pPass)
    {
        int pLast = pPass.PInspectorPassPreset.Items.Count - 1;
        pPass.PInspectorPassPreset.Items[pLast] = new LLocalizationChoice("Custom", "Inspector.Common.Custom");
    }

    private static void PFilterPresetItemSelect(PInspectorPass pPass, string pToken)
    {
        for (int pIndex = 0; pIndex < pPass.PInspectorPassPreset.Items.Count; pIndex++)
        {
            if (LLocalizationChoice.LLocalizationChoiceRead(pPass.PInspectorPassPreset.Items[pIndex]) == pToken)
            {
                pPass.PInspectorPassPreset.SelectedIndex = pIndex;
                return;
            }
        }
    }

    private void PFilterActiveSet(PInspectorPass pPass, Cadroue.Core.LWorkPassStep pStep)
    {
        pPass.PFilterApplyBox.IsChecked = pStep.LWorkAudioStepActive;
        pPass.PInspectorPassSuppress = true;
        pPass.PFilterStageSuppress = true;
        pPass.PInspectorPassPresetSuppress = true;
        pPass.PInspectorPassFrequency.Value = Math.Clamp(
            pStep.LWorkPassFrequency,
            pPass.PInspectorPassMin,
            pPass.PInspectorPassMax);
        pPass.PInspectorPassValue.Text = pStep.LWorkPassFrequency.ToString("0", CultureInfo.InvariantCulture);
        pPass.PInspectorPassStages.Value = Math.Clamp(
            pStep.LWorkPassStages,
            PFilterStagesLeast,
            PFilterStagesMost);
        pPass.PFilterStageValue.Text = pStep.LWorkPassStages.ToString(CultureInfo.InvariantCulture);
        pPass.PInspectorPassPoles.SelectedIndex = pStep.LWorkPassPoles == 1 ? 0 : 1;
        pPass.PInspectorPassResonance.Text = pStep.LWorkPassResonance.ToString("0.###", CultureInfo.InvariantCulture);
        pPass.PInspectorPassSuppress = false;
        pPass.PFilterStageSuppress = false;
        pPass.PInspectorPassPresetSuppress = false;
        PFilterRestoreReflect(pPass);
        PFilterApplyUpdate(pPass);
    }

    private void PFilterRestoreReflect(PInspectorPass pPass)
    {
        pPass.PInspectorPassPresetSuppress = true;
        PInspectorPassPreset? pMatch = null;
        foreach (PInspectorPassPreset pEntry in pPass.PInspectorPassPresets)
        {
            if (PFilterValuesMatch(pPass, pEntry))
            {
                pMatch = pEntry;
                break;
            }
        }

        if (pMatch is not null)
        {
            pPass.PInspectorPassBaseToken = pMatch.PInspectorPassPresetToken;
            PFilterCustomLabelReset(pPass);
            PFilterPresetItemSelect(pPass, pMatch.PInspectorPassPresetToken);
        }
        else
        {
            pPass.PInspectorPassBaseToken = null;
            PFilterCustomLabelReset(pPass);
            pPass.PInspectorPassPreset.SelectedIndex = pPass.PInspectorPassPreset.Items.Count - 1;
        }

        pPass.PInspectorPassPresetSuppress = false;
    }

    private Grid PFilterSliderBuild(string pLabel, Slider pSlider, string pUnit, TextBox pValue)
    {
        var pRow = new Grid
        {
            Height = PInspectorRowHeight,
            Margin = new Thickness(0, 0, 0, 8)
        };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        TextBlock pLabelBlock = PInspectorLabelBuild(pLabel);
        pSlider.VerticalAlignment = VerticalAlignment.Center;
        pValue.VerticalAlignment = VerticalAlignment.Center;
        var pUnitBlock = new TextBlock
        {
            Text = pUnit,
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 8, 0)
        };

        Grid.SetColumn(pLabelBlock, 0);
        Grid.SetColumn(pSlider, 1);
        Grid.SetColumn(pUnitBlock, 2);
        Grid.SetColumn(pValue, 3);
        pRow.Children.Add(pLabelBlock);
        pRow.Children.Add(pSlider);
        pRow.Children.Add(pUnitBlock);
        pRow.Children.Add(pValue);
        return pRow;
    }

    private void PFilterApplyUpdate(PInspectorPass pPass)
    {
        bool pActive = pPass.PFilterApplyBox.IsChecked == true;
        pPass.PInspectorPassStack.IsEnabled = pActive;
        pPass.PInspectorPassStack.Opacity = pActive ? 1 : 0.4;
        PInspectorActiveRaise();
    }
}
