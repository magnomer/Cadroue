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

    private sealed record PInspectorPassPreset(string Name, double Cutoff, int Stages, int Poles, double Resonance);

    private static readonly PInspectorPassPreset[] pInspectorHighPassPresets =
    {
        new("Rumble", 30, 2, 2, 0.707),
        new("Voice", 80, 2, 2, 0.707),
        new("Speech (tight)", 100, 4, 2, 0.707),
        new("De-mud", 200, 2, 2, 0.707)
    };

    private static readonly PInspectorPassPreset[] pInspectorLowPassPresets =
    {
        new("De-hiss", 16000, 2, 2, 0.707),
        new("Soften", 10000, 2, 2, 0.707),
        new("Warm", 8000, 3, 2, 0.707),
        new("Telephone", 3400, 4, 2, 0.707)
    };

    private sealed class PInspectorPass
    {
        public required CheckBox PInspectorPassApply { get; init; }
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
        pInspectorHighPass = PInspectorPassBuild(100, 20, 300, "Apply the high-pass cut to queued jobs", pInspectorHighPassPresets);
        return pInspectorHighPass.PInspectorPassBody;
    }

    private StackPanel PInspectorLowPassBodyBuild()
    {
        pInspectorLowPass = PInspectorPassBuild(12000, 3000, 20000, "Apply the low-pass cut to queued jobs", pInspectorLowPassPresets);
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
        CheckBox pApply = PInspectorSwitchBuild("Apply", pApplyTip);

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
            pPreset.Items.Add(pPresetEntry.Name);
        }
        pPreset.Items.Add("Custom");
        pPreset.SelectedItem = "Custom";

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

        pStack.Children.Add(PInspectorFieldBuild("Preset", pPreset));
        pStack.Children.Add(PInspectorPassSliderRowBuild("Cutoff", pFrequency, "Hz", pValue));
        pStack.Children.Add(PInspectorPassSliderRowBuild("Steepness", pStages, "×12dB", pStageValue));
        pStack.Children.Add(PInspectorFieldBuild("Poles", pPoles));
        pStack.Children.Add(PInspectorFieldBuild("Resonance", PInspectorNormalizeUnitRowBuild(pResonance, "Q")));

        pBody.Children.Add(pApply);
        pBody.Children.Add(PInspectorSeparatorBuild());
        pBody.Children.Add(pStack);

        PInspectorPassApplyUpdate(pPass);
        PInspectorPassPresetApply(pPass);
        return pPass;
    }

    private void PInspectorPassPresetApply(PInspectorPass pPass)
    {
        if (pPass.PInspectorPassPreset.SelectedItem is not string pName || pName == "Custom")
        {
            PInspectorPassLock(pPass, false);
            return;
        }

        PInspectorPassPreset? pPreset = null;
        foreach (PInspectorPassPreset pEntry in pPass.PInspectorPassPresets)
        {
            if (pEntry.Name == pName)
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

    private Grid PInspectorPassSliderRowBuild(string pLabel, Slider pSlider, string pUnit, TextBox pValue)
    {
        var pRow = new Grid();
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        TextBlock pLabelBlock = PInspectorLabelBuild(pLabel);
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

    private static void PInspectorPassApplyUpdate(PInspectorPass pPass)
    {
        bool pActive = pPass.PInspectorPassApply.IsChecked == true;
        pPass.PInspectorPassStack.IsEnabled = pActive;
        pPass.PInspectorPassStack.Opacity = pActive ? 1 : 0.4;
    }
}
