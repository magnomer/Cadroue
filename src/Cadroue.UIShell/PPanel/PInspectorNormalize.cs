using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PHouse;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    private CheckBox pLoudnessApplyBox = null!;
    private CheckBox pLoudnessPersistent = null!;
    private ComboBox pLoudnessPreset = null!;
    private ComboBox pLoudnessMode = null!;
    private TextBox pLoudnessTarget = null!;
    private TextBox pLoudnessPeak = null!;
    private TextBox pLoudnessRange = null!;
    private TextBox pDynamicFrame = null!;
    private TextBox pDynamicGauss = null!;
    private TextBox pDynamicMaxGain = null!;
    private TextBox pDynamicCompress = null!;
    private CheckBox pLoudnessTwoPass = null!;
    private StackPanel pLoudnessPanel = null!;
    private StackPanel pLoudnessStack = null!;
    private StackPanel pDynamicStack = null!;
    private StackPanel pLoudnessBody = null!;
    private string? pLoudnessBaseToken;
    private bool pLoudnessPresetSuppress;

    private LLeveling PLoudnessModeRead() =>
        pLoudnessMode.SelectedIndex == 1
            ? LLeveling.LLevelingDynamic
            : LLeveling.LLevelingLoudness;

    private StackPanel PLoudnessBodyBuild()
    {
        LLevelingDefault pDefault = LLevelingCatalog.LLevelingDefaultRead();
        pLoudnessApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Normalize.ApplyTooltip"));
        pLoudnessApplyBox.Checked += (_, _) => PLoudnessApplyUpdate();
        pLoudnessApplyBox.Unchecked += (_, _) => PLoudnessApplyUpdate();

        pLoudnessPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Normalize.PersistentTooltip"));

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
        pLoudnessPreset.SelectionChanged += (_, _) => PLoudnessComboApply();

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
        pLoudnessMode.SelectionChanged += (_, _) => PLoudnessModeUpdate();

        pLoudnessTarget = PInspectorDecimalBuild();
        pLoudnessTarget.Text = pDefault.LLevelingTarget.ToString("0.###", CultureInfo.InvariantCulture);
        Slider pTargetSlider = PInspectorSliderBuild(
            pLoudnessTarget,
            LLevelingCatalog.LLevelingTargetLeast,
            LLevelingCatalog.LLevelingTargetMost,
            pDefault.LLevelingTarget,
            "0.#",
            () => PLoudnessPresetRead()?.LLevelingTarget ?? pDefault.LLevelingTarget,
            PLoudnessValueUpdate);
        pLoudnessPeak = PInspectorDecimalBuild();
        pLoudnessPeak.Text = pDefault.LLevelingPeak.ToString("0.###", CultureInfo.InvariantCulture);
        Slider pPeakSlider = PInspectorSliderBuild(
            pLoudnessPeak,
            LLevelingCatalog.LLevelingPeakLeast,
            LLevelingCatalog.LLevelingPeakMost,
            pDefault.LLevelingPeak,
            "0.#",
            () => PLoudnessPresetRead()?.LLevelingPeak ?? pDefault.LLevelingPeak,
            PLoudnessValueUpdate);
        pLoudnessRange = PInspectorDecimalBuild();
        pLoudnessRange.Text = pDefault.LLevelingRange.ToString("0.###", CultureInfo.InvariantCulture);
        Slider pRangeSlider = PInspectorSliderBuild(
            pLoudnessRange,
            LLevelingCatalog.LLevelingRangeLeast,
            LLevelingCatalog.LLevelingRangeMost,
            pDefault.LLevelingRange,
            "0.#",
            () => PLoudnessPresetRead()?.LLevelingRange ?? pDefault.LLevelingRange,
            PLoudnessValueUpdate);

        pDynamicFrame = PInspectorDecimalBuild();
        pDynamicFrame.Text = pDefault.LLevelingFrame.ToString("0.###", CultureInfo.InvariantCulture);
        Slider pFrameSlider = PInspectorSliderBuild(
            pDynamicFrame,
            LLevelingCatalog.LLevelingFrameLeast,
            LLevelingCatalog.LLevelingFrameMost,
            pDefault.LLevelingFrame,
            "0",
            () => PDynamicPresetRead()?.LLevelingFrame ?? pDefault.LLevelingFrame,
            PDynamicValueUpdate);
        pDynamicGauss = PInspectorDecimalBuild();
        pDynamicGauss.Text = pDefault.LLevelingGauss.ToString("0.###", CultureInfo.InvariantCulture);
        Slider pGaussSlider = PInspectorSliderBuild(
            pDynamicGauss,
            LLevelingCatalog.LLevelingGaussLeast,
            LLevelingCatalog.LLevelingGaussMost,
            pDefault.LLevelingGauss,
            "0",
            () => PDynamicPresetRead()?.LLevelingGauss ?? pDefault.LLevelingGauss,
            PDynamicValueUpdate);
        pDynamicMaxGain = PInspectorDecimalBuild();
        pDynamicMaxGain.Text = pDefault.LLevelingMaxGain.ToString("0.###", CultureInfo.InvariantCulture);
        Slider pMaxGainSlider = PInspectorSliderBuild(
            pDynamicMaxGain,
            LLevelingCatalog.LLevelingGainLeast,
            LLevelingCatalog.LLevelingGainMost,
            pDefault.LLevelingMaxGain,
            "0.#",
            () => PDynamicPresetRead()?.LLevelingMaxGain ?? pDefault.LLevelingMaxGain,
            PDynamicValueUpdate);
        pDynamicCompress = PInspectorDecimalBuild();
        pDynamicCompress.Text = pDefault.LLevelingCompress.ToString("0.###", CultureInfo.InvariantCulture);
        Slider pCompressSlider = PInspectorSliderBuild(
            pDynamicCompress,
            LLevelingCatalog.LLevelingCompressLeast,
            LLevelingCatalog.LLevelingCompressMost,
            pDefault.LLevelingCompress,
            "0.#",
            () => PDynamicPresetRead()?.LLevelingCompress ?? pDefault.LLevelingCompress,
            PDynamicValueUpdate);

        pLoudnessTwoPass = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Normalize.TwoPass"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Normalize.TwoPassTooltip"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            IsChecked = pDefault.LLevelingTwoPass,
            Margin = new Thickness(0, 8, 0, 0)
        };
        PHouse.PCheckbox.PCheckboxApply(pLoudnessTwoPass);
        pLoudnessTwoPass.Checked += (_, _) => PInspectorActiveRaise();
        pLoudnessTwoPass.Unchecked += (_, _) => PInspectorActiveRaise();

        pLoudnessStack = new StackPanel();
        pLoudnessStack.Children.Add(
            PFilterSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Normalize.Target"),
                pTargetSlider,
                "LUFS",
                pLoudnessTarget));
        pLoudnessStack.Children.Add(
            PFilterSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Normalize.Peak"),
                pPeakSlider,
                "dBTP",
                pLoudnessPeak));
        pLoudnessStack.Children.Add(
            PFilterSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Normalize.Range"),
                pRangeSlider,
                "LU",
                pLoudnessRange));
        pLoudnessStack.Children.Add(pLoudnessTwoPass);

        pDynamicStack = new StackPanel { Visibility = Visibility.Collapsed };
        pDynamicStack.Children.Add(
            PFilterSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Dynamic.Frame"),
                pFrameSlider,
                "ms",
                pDynamicFrame));
        pDynamicStack.Children.Add(
            PFilterSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Dynamic.Smoothness"),
                pGaussSlider,
                "g",
                pDynamicGauss));
        pDynamicStack.Children.Add(
            PFilterSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Dynamic.MaxGain"),
                pMaxGainSlider,
                "×",
                pDynamicMaxGain));
        pDynamicStack.Children.Add(
            PFilterSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Dynamic.Compress"),
                pCompressSlider,
                "s",
                pDynamicCompress));

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

        PLoudnessApplyUpdate();
        PLoudnessModeUpdate();
        PLoudnessPresetApply();
        return pLoudnessBody;
    }

    private void PLoudnessCustomReset()
    {
        int pLast = pLoudnessPreset.Items.Count - 1;
        pLoudnessPreset.Items[pLast] = new LLocalizationChoice("Custom", "Inspector.Common.Custom");
    }

    private void PLoudnessPresetSelect(string pToken)
    {
        for (int pIndex = 0; pIndex < pLoudnessPreset.Items.Count; pIndex++)
        {
            if (LLocalizationChoice.LLocalizationChoiceRead(pLoudnessPreset.Items[pIndex]) == pToken)
            {
                pLoudnessPreset.SelectedIndex = pIndex;
                return;
            }
        }
    }

    private void PLoudnessComboUpdate(string? pMatch)
    {
        pLoudnessPresetSuppress = true;
        if (pMatch is not null)
        {
            pLoudnessBaseToken = pMatch;
            PLoudnessCustomReset();
            PLoudnessPresetSelect(pMatch);
        }
        else
        {
            pLoudnessBaseToken = null;
            PLoudnessCustomReset();
            pLoudnessPreset.SelectedIndex = pLoudnessPreset.Items.Count - 1;
        }

        pLoudnessPresetSuppress = false;
    }

    private void PLoudnessApplyUpdate()
    {
        bool pNormalizeActive = pLoudnessApplyBox.IsChecked == true;
        pLoudnessPanel.IsEnabled = pNormalizeActive;
        pLoudnessPanel.Opacity = pNormalizeActive ? 1 : 0.4;
        PInspectorActiveRaise();
    }

    private void PLoudnessModeUpdate()
    {
        bool pLoudness = PLoudnessModeRead() == LLeveling.LLevelingLoudness;
        pLoudnessStack.Visibility = pLoudness ? Visibility.Visible : Visibility.Collapsed;
        pDynamicStack.Visibility = pLoudness ? Visibility.Collapsed : Visibility.Visible;

        pLoudnessPresetSuppress = true;
        PLoudnessComboBuild(pLoudness);
        pLoudnessPresetSuppress = false;

        PLoudnessComboUpdate(pLoudness ? PLoudnessValuesMatch() : PDynamicValuesMatch());

        PInspectorActiveRaise();
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

    private void PLoudnessComboApply()
    {
        if (PLoudnessModeRead() == LLeveling.LLevelingLoudness)
        {
            PLoudnessPresetApply();
        }
        else
        {
            PDynamicPresetApply();
        }
    }
}
