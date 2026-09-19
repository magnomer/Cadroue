using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private PInspectorPass PInspectorPassBuild(
        LFilter pOwner,
        string pApplyTip,
        IReadOnlyList<PInspectorPassChoice> pPresets)
    {
        CheckBox pApply = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            pApplyTip);
        CheckBox pPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Pass.PersistentTooltip"));
        PInspectorSwitchAttach(pApply, pOwner.LFilterActiveSet);
        PInspectorSwitchAttach(pPersistent, pOwner.LFilterPersistentSet);

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
            pPreset.Items.Add(
                new LLocalizationChoice(pPresetEntry.PInspectorPassToken, pPresetEntry.PInspectorPassKey));
        }

        pPreset.Items.Add(new LLocalizationChoice("Custom", "Inspector.Common.Custom"));
        pPreset.SelectionChanged += (_, _) =>
        {
            if (PInspectorPresetRead(pPreset, pOwner.LFilterToken, pOwner.LFilterMatchRead()) is { } pToken)
            {
                pOwner.LFilterPresetSelect(pToken);
            }
        };

        var pFrequency = new Slider
        {
            Minimum = pOwner.LFilterFloor,
            Maximum = pOwner.LFilterCeiling,
            Value = pOwner.LFilterStep.LWorkPassFrequency,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pFrequency);
        PSlider.PSliderResetApply(
            pFrequency,
            () => pOwner.LFilterPresetRead()?.LPassbandCutoff ?? pOwner.LFilterStep.LWorkPassFrequency);
        TextBox pValue = PInspectorDecimalBuild();
        PInspectorValueAttach(
            pFrequency,
            pValue,
            pOwner.LFilterFloor,
            pOwner.LFilterCeiling,
            () => pOwner.LFilterStep.LWorkPassFrequency,
            pOwner.LFilterFrequencySet);

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
        PSlider.PSliderResetApply(pStages, () => pOwner.LFilterPresetRead()?.LPassbandStages ?? 1);
        TextBox pStageValue = PInspectorDecimalBuild();
        PInspectorValueAttach(
            pStages,
            pStageValue,
            LPassband.LPassbandStagesLeast,
            LPassband.LPassbandStagesMost,
            () => pOwner.LFilterStep.LWorkPassStages,
            pNumber => pOwner.LFilterStagesSet((int)Math.Round(pNumber)));

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
        pPoles.SelectionChanged += (_, _) =>
        {
            if (pPoles.SelectedIndex >= 0)
            {
                pOwner.LFilterPolesSet(pPoles.SelectedIndex == 0 ? 1 : 2);
            }
        };

        var pResonanceSlider = new Slider
        {
            Minimum = LPassband.LPassbandResonanceLeast,
            Maximum = LPassband.LPassbandResonanceMost,
            Value = pOwner.LFilterStep.LWorkPassResonance,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pResonanceSlider);
        PSlider.PSliderResetApply(
            pResonanceSlider,
            () => pOwner.LFilterPresetRead()?.LPassbandResonance ?? 0.707);
        TextBox pResonance = PInspectorDecimalBuild();
        PInspectorValueAttach(
            pResonanceSlider,
            pResonance,
            LPassband.LPassbandResonanceLeast,
            LPassband.LPassbandResonanceMost,
            () => pOwner.LFilterStep.LWorkPassResonance,
            pOwner.LFilterResonanceSet);

        var pStack = new StackPanel();
        var pBody = new StackPanel { Margin = new Thickness(12, 12, 12, 12), Visibility = Visibility.Collapsed };
        pStack.Children.Add(
            PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pPreset));
        pStack.Children.Add(
            PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Pass.Cutoff"), pFrequency, "Hz", pValue));
        pStack.Children.Add(
            PFilterSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Pass.Steepness"),
                pStages,
                "×12dB",
                pStageValue));
        Grid pResonanceRow = PFilterSliderBuild(
            LLocalization.LLocalizationTextRead("Inspector.Pass.Resonance"),
            pResonanceSlider,
            "Q",
            pResonance);
        pStack.Children.Add(pResonanceRow);
        pStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Pass.Poles"), pPoles));

        pBody.Children.Add(pApply);
        pBody.Children.Add(PInspectorSeparatorBuild());
        pBody.Children.Add(pStack);

        return new PInspectorPass
        {
            PFilterOwner = pOwner,
            PFilterApplyBox = pApply,
            PInspectorPassPersistent = pPersistent,
            PInspectorPassPreset = pPreset,
            PInspectorPassFrequency = pFrequency,
            PInspectorPassValue = pValue,
            PInspectorPassStages = pStages,
            PFilterStageValue = pStageValue,
            PInspectorPassPoles = pPoles,
            PFilterResonanceSlider = pResonanceSlider,
            PInspectorPassResonance = pResonance,
            PFilterResonanceRow = pResonanceRow,
            PInspectorPassStack = pStack,
            PInspectorPassBody = pBody,
            PInspectorPassPresets = pPresets
        };
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
