using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.UIShell.PHouse;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    private PInspectorPass PInspectorPassBuild(
        double pDefault, double pMin, double pMax, string pApplyTip, IReadOnlyList<PInspectorPassChoice> pPresets, bool pHigh, string pDefaultToken)
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
        foreach (PInspectorPassChoice pPresetEntry in pPresets)
        {
            pPreset.Items.Add(new LLocalizationChoice(pPresetEntry.PInspectorPassToken, pPresetEntry.PInspectorPassKey));
        }
        pPreset.Items.Add(new LLocalizationChoice("Custom", "Inspector.Common.Custom"));
        int pDefaultIndex = pPreset.Items.Count - 1;
        for (int pIndex = 0; pIndex < pPresets.Count; pIndex++)
        {
            if (pPresets[pIndex].PInspectorPassToken == pDefaultToken)
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
            Minimum = LPassband.LPassbandStagesLeast,
            Maximum = LPassband.LPassbandStagesMost,
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
            PInspectorPassHigh = pHigh,
            PInspectorPassMin = pMin,
            PInspectorPassMax = pMax,
            PInspectorPassDefault = pDefault
        };

        PSlider.PSliderResetApply(pFrequency, () => PFilterPresetRead(pPass) is { } pEntry ? pEntry.LPassbandCutoff : pDefault);
        PSlider.PSliderResetApply(pStages, () => PFilterPresetRead(pPass) is { } pEntry ? pEntry.LPassbandStages : 1);

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
            pStages.Value = Math.Clamp(Math.Round(PInspectorDecimalRead(pStageValue, 1)), LPassband.LPassbandStagesLeast, LPassband.LPassbandStagesMost);
            pPass.PFilterStageSuppress = false;
            PFilterDeviationCheck(pPass);
        };
        pPoles.SelectionChanged += (_, _) => PFilterDeviationCheck(pPass);
        Slider pResonanceSlider = PInspectorSliderBuild(
            pResonance, LPassband.LPassbandResonanceLeast, LPassband.LPassbandResonanceMost, 0.707, "0.###",
            () => PFilterPresetRead(pPass) is { } pEntry ? pEntry.LPassbandResonance : 0.707,
            () => PFilterDeviationCheck(pPass));

        pStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pPreset));
        pStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Pass.Cutoff"), pFrequency, "Hz", pValue));
        pStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Pass.Steepness"), pStages, "×12dB", pStageValue));
        Grid pResonanceRow = PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Pass.Resonance"), pResonanceSlider, "Q", pResonance);
        pStack.Children.Add(pResonanceRow);
        pStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Pass.Poles"), pPoles));

        void PFilterResonanceUpdate()
        {
            bool pResonanceOn = PFilterPolesRead(pPass) == 2;
            pResonanceRow.IsEnabled = pResonanceOn;
            pResonanceRow.Opacity = pResonanceOn ? 1 : 0.4;
        }

        pPoles.SelectionChanged += (_, _) => PFilterResonanceUpdate();
        PFilterResonanceUpdate();

        pBody.Children.Add(pApply);
        pBody.Children.Add(PInspectorSeparatorBuild());
        pBody.Children.Add(pStack);

        PFilterApplyUpdate(pPass);
        PFilterPresetApply(pPass);
        return pPass;
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
}
