using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private CheckBox pLoudnessApplyBox = null!;
    private CheckBox pLoudnessPersistent = null!;
    private ComboBox pLoudnessPreset = null!;
    private ComboBox pLoudnessMode = null!;
    private CheckBox pLoudnessTwoPass = null!;
    private StackPanel pLoudnessPanel = null!;
    private StackPanel pLoudnessStack = null!;
    private StackPanel pDynamicStack = null!;
    private StackPanel pLoudnessBody = null!;
    private readonly Slider[] pLoudnessSliders = new Slider[3];
    private readonly TextBox[] pLoudnessValues = new TextBox[3];
    private readonly Slider[] pDynamicSliders = new Slider[4];
    private readonly TextBox[] pDynamicValues = new TextBox[4];

    private StackPanel PLoudnessBodyBuild()
    {
        pLoudnessApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Normalize.ApplyTooltip"));
        PInspectorSwitchAttach(pLoudnessApplyBox, LLoudness.LLoudnessActiveSet);

        pLoudnessPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Normalize.PersistentTooltip"));
        PInspectorSwitchAttach(pLoudnessPersistent, LLoudness.LLoudnessPersistentSet);

        pLoudnessPreset = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pLoudnessPreset);
        PLoudnessComboBuild(true);
        pLoudnessPreset.SelectionChanged += (_, _) =>
        {
            if (PInspectorPresetRead(pLoudnessPreset, LLoudness.LLoudnessToken, LLoudness.LLoudnessMatchRead())
                is { } pToken)
            {
                LLoudness.LLoudnessPresetSelect(pToken);
            }
        };

        pLoudnessMode = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pLoudnessMode);
        pLoudnessMode.Items.Add(new LLocalizationChoice("Loudness", "Inspector.Normalize.Loudness"));
        pLoudnessMode.Items.Add(new LLocalizationChoice("Dynamic", "Inspector.Normalize.Dynamic"));
        pLoudnessMode.SelectedIndex = 0;
        pLoudnessMode.SelectionChanged += (_, _) =>
        {
            if (pLoudnessMode.SelectedIndex >= 0)
            {
                LLoudness.LLoudnessModeSet(
                    pLoudnessMode.SelectedIndex == 1 ? LLeveling.LLevelingDynamic : LLeveling.LLevelingLoudness);
            }
        };

        for (int pIndex = 0; pIndex < pLoudnessSliders.Length; pIndex++)
        {
            int pSlot = pIndex;
            pLoudnessSliders[pSlot] = PLoudnessSliderBuild(
                PLoudnessLeast[pSlot], PLoudnessMost[pSlot], () => PLoudnessDefaultRead(pSlot));
            pLoudnessValues[pSlot] = PInspectorDecimalBuild();
            PInspectorValueAttach(
                pLoudnessSliders[pSlot],
                pLoudnessValues[pSlot],
                PLoudnessLeast[pSlot],
                PLoudnessMost[pSlot],
                () => PLoudnessValueRead(pSlot),
                pNumber => LLoudness.LLoudnessValueSet(pSlot, pNumber));
        }

        for (int pIndex = 0; pIndex < pDynamicSliders.Length; pIndex++)
        {
            int pSlot = pIndex;
            pDynamicSliders[pSlot] = PLoudnessSliderBuild(
                PDynamicLeast[pSlot], PDynamicMost[pSlot], () => PDynamicDefaultRead(pSlot));
            pDynamicValues[pSlot] = PInspectorDecimalBuild();
            PInspectorValueAttach(
                pDynamicSliders[pSlot],
                pDynamicValues[pSlot],
                PDynamicLeast[pSlot],
                PDynamicMost[pSlot],
                () => PDynamicValueRead(pSlot),
                pNumber => LLoudness.LLoudnessDynamicSet(pSlot, pNumber));
        }

        pLoudnessTwoPass = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Normalize.TwoPass"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Normalize.TwoPassTooltip"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        PHouse.PCheckbox.PCheckboxApply(pLoudnessTwoPass);
        PInspectorSwitchAttach(pLoudnessTwoPass, LLoudness.LLoudnessTwopassSet);

        pLoudnessStack = new StackPanel();
        for (int pSlot = 0; pSlot < pLoudnessSliders.Length; pSlot++)
        {
            pLoudnessStack.Children.Add(
                PFilterSliderBuild(
                    LLocalization.LLocalizationTextRead(PLoudnessLabelKeys[pSlot]),
                    pLoudnessSliders[pSlot],
                    PLoudnessUnits[pSlot],
                    pLoudnessValues[pSlot]));
        }

        pLoudnessStack.Children.Add(pLoudnessTwoPass);

        pDynamicStack = new StackPanel { Visibility = Visibility.Collapsed };
        for (int pSlot = 0; pSlot < pDynamicSliders.Length; pSlot++)
        {
            pDynamicStack.Children.Add(
                PFilterSliderBuild(
                    LLocalization.LLocalizationTextRead(PDynamicLabelKeys[pSlot]),
                    pDynamicSliders[pSlot],
                    PDynamicUnits[pSlot],
                    pDynamicValues[pSlot]));
        }

        var pNotice = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Normalize.Notice"),
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };

        pLoudnessPanel = new StackPanel();
        pLoudnessPanel.Children.Add(
            PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Normalize.Mode"), pLoudnessMode));
        pLoudnessPanel.Children.Add(
            PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pLoudnessPreset));
        pLoudnessPanel.Children.Add(pLoudnessStack);
        pLoudnessPanel.Children.Add(pDynamicStack);
        pLoudnessPanel.Children.Add(pNotice);

        pLoudnessBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pLoudnessBody.Children.Add(pLoudnessApplyBox);
        pLoudnessBody.Children.Add(PInspectorSeparatorBuild());
        pLoudnessBody.Children.Add(pLoudnessPanel);
        return pLoudnessBody;
    }

    private static Slider PLoudnessSliderBuild(double pMin, double pMax, Func<double> pResetRead)
    {
        var pSlider = new Slider
        {
            Minimum = pMin,
            Maximum = pMax,
            Value = pMin,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);
        PSlider.PSliderResetApply(pSlider, pResetRead);
        return pSlider;
    }

    private void PLoudnessComboBuild(bool pLoudness)
    {
        pLoudnessPreset.Items.Clear();
        if (pLoudness)
        {
            foreach (string pToken in LLevelingCatalog.LLevelingLoudnessTokens)
            {
                pLoudnessPreset.Items.Add(new LLocalizationChoice(pToken, PLoudnessKeyRead(pToken)));
            }
        }
        else
        {
            foreach (string pToken in LLevelingCatalog.LLevelingDynamicTokens)
            {
                pLoudnessPreset.Items.Add(new LLocalizationChoice(pToken, PDynamicKeyRead(pToken)));
            }
        }

        pLoudnessPreset.Items.Add(new LLocalizationChoice("Custom", "Inspector.Common.Custom"));
    }

    private void PLoudnessUpdate()
    {
        LWorkNormalizeStep pStep = LLoudness.LLoudnessStep;
        bool pDynamic = LLoudness.LLoudnessDynamic;
        PInspectorSwitchUpdate(pLoudnessApplyBox, pStep.LWorkStepActive, false);
        PInspectorSwitchUpdate(pLoudnessPersistent, LLoudness.LLoudnessPersistent, true);
        PInspectorSwitchUpdate(pLoudnessTwoPass, pStep.LWorkTwoPass, false);
        if (pLoudnessMode.SelectedIndex != (pDynamic ? 1 : 0))
        {
            pLoudnessMode.SelectedIndex = pDynamic ? 1 : 0;
        }

        pLoudnessStack.Visibility = pDynamic ? Visibility.Collapsed : Visibility.Visible;
        pDynamicStack.Visibility = pDynamic ? Visibility.Visible : Visibility.Collapsed;
        string pFirst = LLocalizationChoice.LLocalizationChoiceRead(pLoudnessPreset.Items[0]);
        bool pComboDynamic = LLevelingCatalog.LLevelingDynamicTokens.Contains(pFirst);
        if (pComboDynamic != pDynamic)
        {
            PLoudnessComboBuild(!pDynamic);
        }

        for (int pSlot = 0; pSlot < pLoudnessSliders.Length; pSlot++)
        {
            PInspectorValueUpdate(pLoudnessSliders[pSlot], pLoudnessValues[pSlot], PLoudnessValueRead(pSlot), "0.###");
        }

        for (int pSlot = 0; pSlot < pDynamicSliders.Length; pSlot++)
        {
            PInspectorValueUpdate(pDynamicSliders[pSlot], pDynamicValues[pSlot], PDynamicValueRead(pSlot), "0.###");
        }

        PInspectorPresetUpdate(
            pLoudnessPreset,
            LLoudness.LLoudnessMatchRead(),
            LLoudness.LLoudnessToken,
            pDynamic ? PDynamicKeyRead : PLoudnessKeyRead);
        PInspectorSectionUpdate(pLoudnessPanel, pStep.LWorkStepActive);
    }
}
