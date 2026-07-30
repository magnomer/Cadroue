using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private const double PInspectorPassMinStages = 1;
    private const double PInspectorPassMaxStages = 8;

    private sealed record PInspectorPassPreset(
        string PInspectorPassPresetToken,
        string PInspectorPassPresetKey,
        double Cutoff,
        int Stages,
        int Poles,
        double Resonance);

    private static readonly PInspectorPassPreset[] pInspectorHighPassPresets =
    {
        new("Rumble", "Inspector.Pass.Preset.Rumble", 30, 2, 2, 0.707),
        new("Voice", "Inspector.Pass.Preset.Voice", 80, 2, 2, 0.707),
        new("Speech (tight)", "Inspector.Pass.Preset.SpeechTight", 100, 4, 2, 0.707),
        new("De-mud", "Inspector.Pass.Preset.Demud", 200, 2, 2, 0.707)
    };

    private static readonly PInspectorPassPreset[] pInspectorLowPassPresets =
    {
        new("De-hiss", "Inspector.Pass.Preset.Dehiss", 16000, 2, 2, 0.707),
        new("Soften", "Inspector.Pass.Preset.Soften", 10000, 2, 2, 0.707),
        new("Warm", "Inspector.Pass.Preset.Warm", 8000, 3, 2, 0.707),
        new("Telephone", "Inspector.Pass.Preset.Telephone", 3400, 4, 2, 0.707)
    };

    private sealed class PInspectorPass
    {
        public required CheckBox PInspectorPassApply { get; init; }
        public required CheckBox PInspectorPassPersistent { get; init; }
        public required ComboBox PInspectorPassPreset { get; init; }
        public required Slider PInspectorPassFrequency { get; init; }
        public required TextBox PInspectorPassValue { get; init; }
        public required Slider PInspectorPassStages { get; init; }
        public required TextBox PInspectorPassStageValue { get; init; }
        public required ComboBox PInspectorPassPoles { get; init; }
        public required TextBox PInspectorPassResonance { get; init; }
        public required StackPanel PInspectorPassStack { get; init; }
        public required StackPanel PInspectorPassBody { get; init; }
        public required IReadOnlyList<PInspectorPassPreset> PInspectorPassPresets { get; init; }
        public double PInspectorPassMin { get; init; }
        public double PInspectorPassMax { get; init; }
        public double PInspectorPassDefault { get; init; }
        public bool PInspectorPassSuppress { get; set; }
        public bool PInspectorPassStageSuppress { get; set; }
    }

    private PInspectorPass pInspectorHighPass = null!;
    private PInspectorPass pInspectorLowPass = null!;

    private StackPanel PInspectorHighPassBodyBuild()
    {
        pInspectorHighPass = PInspectorPassBuild(100, 20, 300, LLocalization.LLocalizationTextRead("Inspector.Pass.HighApply"), pInspectorHighPassPresets);
        return pInspectorHighPass.PInspectorPassBody;
    }

    private StackPanel PInspectorLowPassBodyBuild()
    {
        pInspectorLowPass = PInspectorPassBuild(12000, 3000, 20000, LLocalization.LLocalizationTextRead("Inspector.Pass.LowApply"), pInspectorLowPassPresets);
        return pInspectorLowPass.PInspectorPassBody;
    }

    private double PInspectorPassRead(PInspectorPass pPass) =>
        Math.Clamp(PInspectorDecimalRead(pPass.PInspectorPassValue, pPass.PInspectorPassDefault),
            pPass.PInspectorPassMin, pPass.PInspectorPassMax);

    private int PInspectorPassStagesRead(PInspectorPass pPass) =>
        (int)Math.Clamp(Math.Round(PInspectorDecimalRead(pPass.PInspectorPassStageValue, 1)),
            PInspectorPassMinStages, PInspectorPassMaxStages);

    private static int PInspectorPassPolesRead(PInspectorPass pPass) =>
        pPass.PInspectorPassPoles.SelectedIndex == 0 ? 1 : 2;

    private double PInspectorPassResonanceRead(PInspectorPass pPass) =>
        PInspectorDecimalRead(pPass.PInspectorPassResonance, 0.707);

    private PInspectorPass PInspectorPassBuild(
        double pDefault, double pMin, double pMax, string pApplyTip, IReadOnlyList<PInspectorPassPreset> pPresets)
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
        pPreset.SelectedIndex = pPreset.Items.Count - 1;

        var pFrequency = new Slider { Minimum = pMin, Maximum = pMax, Value = pDefault, VerticalAlignment = VerticalAlignment.Center };
        PSlider.PSliderApply(pFrequency);
        TextBox pValue = PInspectorDecimalBoxBuild();
        pValue.Text = pDefault.ToString("0", CultureInfo.InvariantCulture);

        var pStages = new Slider
        {
            Minimum = PInspectorPassMinStages,
            Maximum = PInspectorPassMaxStages,
            Value = 1,
            IsSnapToTickEnabled = true,
            TickFrequency = 1,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pStages);
        TextBox pStageValue = PInspectorDecimalBoxBuild();
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

        TextBox pResonance = PInspectorDecimalBoxBuild();
        pResonance.Text = "0.707";

        var pStack = new StackPanel();
        var pBody = new StackPanel { Margin = new Thickness(12, 12, 12, 12), Visibility = Visibility.Collapsed };
        var pPass = new PInspectorPass
        {
            PInspectorPassApply = pApply,
            PInspectorPassPersistent = pPersistent,
            PInspectorPassPreset = pPreset,
            PInspectorPassFrequency = pFrequency,
            PInspectorPassValue = pValue,
            PInspectorPassStages = pStages,
            PInspectorPassStageValue = pStageValue,
            PInspectorPassPoles = pPoles,
            PInspectorPassResonance = pResonance,
            PInspectorPassStack = pStack,
            PInspectorPassBody = pBody,
            PInspectorPassPresets = pPresets,
            PInspectorPassMin = pMin,
            PInspectorPassMax = pMax,
            PInspectorPassDefault = pDefault
        };

        pApply.Checked += (_, _) => PInspectorPassApplyUpdate(pPass);
        pApply.Unchecked += (_, _) => PInspectorPassApplyUpdate(pPass);
        pPreset.SelectionChanged += (_, _) => PInspectorPassPresetApply(pPass);

        pFrequency.ValueChanged += (_, _) =>
        {
            if (pPass.PInspectorPassSuppress) { return; }
            pPass.PInspectorPassSuppress = true;
            pValue.Text = pFrequency.Value.ToString("0", CultureInfo.InvariantCulture);
            pPass.PInspectorPassSuppress = false;
        };
        pValue.TextChanged += (_, _) =>
        {
            if (pPass.PInspectorPassSuppress) { return; }
            pPass.PInspectorPassSuppress = true;
            pFrequency.Value = Math.Clamp(PInspectorDecimalRead(pValue, pDefault), pMin, pMax);
            pPass.PInspectorPassSuppress = false;
        };
        pStages.ValueChanged += (_, _) =>
        {
            if (pPass.PInspectorPassStageSuppress) { return; }
            pPass.PInspectorPassStageSuppress = true;
            pStageValue.Text = pStages.Value.ToString("0", CultureInfo.InvariantCulture);
            pPass.PInspectorPassStageSuppress = false;
        };
        pStageValue.TextChanged += (_, _) =>
        {
            if (pPass.PInspectorPassStageSuppress) { return; }
            pPass.PInspectorPassStageSuppress = true;
            pStages.Value = Math.Clamp(Math.Round(PInspectorDecimalRead(pStageValue, 1)), PInspectorPassMinStages, PInspectorPassMaxStages);
            pPass.PInspectorPassStageSuppress = false;
        };

        pStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pPreset));
        pStack.Children.Add(PInspectorPassSliderRowBuild(LLocalization.LLocalizationTextRead("Inspector.Pass.Cutoff"), pFrequency, "Hz", pValue));
        pStack.Children.Add(PInspectorPassSliderRowBuild(LLocalization.LLocalizationTextRead("Inspector.Pass.Steepness"), pStages, "×12dB", pStageValue));
        pStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Pass.Poles"), pPoles));
        pStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Pass.Resonance"), PInspectorNormalizeUnitRowBuild(pResonance, "Q")));

        pBody.Children.Add(pApply);
        pBody.Children.Add(PInspectorSeparatorBuild());
        pBody.Children.Add(pStack);

        PInspectorPassApplyUpdate(pPass);
        PInspectorPassPresetApply(pPass);
        return pPass;
    }

    private void PInspectorPassPresetApply(PInspectorPass pPass)
    {
        string pName = LLocalizationChoice.LLocalizationChoiceRead(pPass.PInspectorPassPreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom")
        {
            PInspectorPassLock(pPass, false);
            return;
        }

        PInspectorPassPreset? pPreset = null;
        foreach (PInspectorPassPreset pEntry in pPass.PInspectorPassPresets)
        {
            if (pEntry.PInspectorPassPresetToken == pName)
            {
                pPreset = pEntry;
                break;
            }
        }

        if (pPreset is null)
        {
            PInspectorPassLock(pPass, false);
            return;
        }

        pPass.PInspectorPassSuppress = true;
        pPass.PInspectorPassStageSuppress = true;
        pPass.PInspectorPassFrequency.Value = Math.Clamp(pPreset.Cutoff, pPass.PInspectorPassMin, pPass.PInspectorPassMax);
        pPass.PInspectorPassValue.Text = pPreset.Cutoff.ToString("0", CultureInfo.InvariantCulture);
        pPass.PInspectorPassStages.Value = Math.Clamp(pPreset.Stages, PInspectorPassMinStages, PInspectorPassMaxStages);
        pPass.PInspectorPassStageValue.Text = pPreset.Stages.ToString(CultureInfo.InvariantCulture);
        pPass.PInspectorPassPoles.SelectedIndex = pPreset.Poles == 1 ? 0 : 1;
        pPass.PInspectorPassResonance.Text = pPreset.Resonance.ToString("0.###", CultureInfo.InvariantCulture);
        pPass.PInspectorPassSuppress = false;
        pPass.PInspectorPassStageSuppress = false;

        PInspectorPassLock(pPass, true);
    }

    private static void PInspectorPassLock(PInspectorPass pPass, bool pLocked)
    {
        bool pEnabled = !pLocked;
        double pOpacity = pLocked ? 0.6 : 1;
        UIElement[] pControls =
        {
            pPass.PInspectorPassFrequency, pPass.PInspectorPassValue,
            pPass.PInspectorPassStages, pPass.PInspectorPassStageValue,
            pPass.PInspectorPassPoles, pPass.PInspectorPassResonance
        };
        foreach (UIElement pControl in pControls)
        {
            pControl.IsEnabled = pEnabled;
            pControl.Opacity = pOpacity;
        }
    }

    private void PInspectorPassApply(PInspectorPass pPass, Cadroue.Core.LWorkAudioStep pStep)
    {
        pPass.PInspectorPassApply.IsChecked = pStep.LWorkAudioStepActive;
        pPass.PInspectorPassPreset.SelectedIndex = pPass.PInspectorPassPreset.Items.Count - 1;
        pPass.PInspectorPassSuppress = true;
        pPass.PInspectorPassStageSuppress = true;
        pPass.PInspectorPassFrequency.Value = Math.Clamp(
            pStep.LWorkAudioStepFrequency,
            pPass.PInspectorPassMin,
            pPass.PInspectorPassMax);
        pPass.PInspectorPassValue.Text = pStep.LWorkAudioStepFrequency.ToString("0", CultureInfo.InvariantCulture);
        pPass.PInspectorPassStages.Value = Math.Clamp(
            pStep.LWorkAudioStepStages,
            PInspectorPassMinStages,
            PInspectorPassMaxStages);
        pPass.PInspectorPassStageValue.Text = pStep.LWorkAudioStepStages.ToString(CultureInfo.InvariantCulture);
        pPass.PInspectorPassPoles.SelectedIndex = pStep.LWorkAudioStepPoles == 1 ? 0 : 1;
        pPass.PInspectorPassResonance.Text = pStep.LWorkAudioStepResonance.ToString("0.###", CultureInfo.InvariantCulture);
        pPass.PInspectorPassSuppress = false;
        pPass.PInspectorPassStageSuppress = false;
        PInspectorPassLock(pPass, false);
        PInspectorPassApplyUpdate(pPass);
    }

    private Grid PInspectorPassSliderRowBuild(string pLabel, Slider pSlider, string pUnit, TextBox pValue)
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

    private void PInspectorPassApplyUpdate(PInspectorPass pPass)
    {
        bool pActive = pPass.PInspectorPassApply.IsChecked == true;
        pPass.PInspectorPassStack.IsEnabled = pActive;
        pPass.PInspectorPassStack.Opacity = pActive ? 1 : 0.4;
        PInspectorAudioActiveRaise();
    }
}
