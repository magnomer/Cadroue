using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

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
        var pDefault = LLevelingCatalog.LLevelingDefaultRead();
        pLoudnessApplyBox = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), LLocalization.LLocalizationTextRead("Inspector.Normalize.ApplyTooltip"));
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
        PNormalizePresetBuild(true);
        pLoudnessPreset.SelectionChanged += (_, _) => PNormalizePresetApply();

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
        pLoudnessTarget.Text = pDefault.Target.ToString("0.###", CultureInfo.InvariantCulture);
        Slider pTargetSlider = PInspectorSliderBind(
            pLoudnessTarget, LLevelingCatalog.LLevelingTargetLeast, LLevelingCatalog.LLevelingTargetMost, pDefault.Target, "0.#",
            () => PLoudnessPresetCurrent()?.Target ?? pDefault.Target, PLoudnessValueUpdate);
        pLoudnessPeak = PInspectorDecimalBuild();
        pLoudnessPeak.Text = pDefault.Peak.ToString("0.###", CultureInfo.InvariantCulture);
        Slider pPeakSlider = PInspectorSliderBind(
            pLoudnessPeak, LLevelingCatalog.LLevelingPeakLeast, LLevelingCatalog.LLevelingPeakMost, pDefault.Peak, "0.#",
            () => PLoudnessPresetCurrent()?.Peak ?? pDefault.Peak, PLoudnessValueUpdate);
        pLoudnessRange = PInspectorDecimalBuild();
        pLoudnessRange.Text = pDefault.Range.ToString("0.###", CultureInfo.InvariantCulture);
        Slider pRangeSlider = PInspectorSliderBind(
            pLoudnessRange, LLevelingCatalog.LLevelingRangeLeast, LLevelingCatalog.LLevelingRangeMost, pDefault.Range, "0.#",
            () => PLoudnessPresetCurrent()?.Range ?? pDefault.Range, PLoudnessValueUpdate);

        pDynamicFrame = PInspectorDecimalBuild();
        pDynamicFrame.Text = pDefault.Frame.ToString("0.###", CultureInfo.InvariantCulture);
        Slider pFrameSlider = PInspectorSliderBind(
            pDynamicFrame, LLevelingCatalog.LLevelingFrameLeast, LLevelingCatalog.LLevelingFrameMost, pDefault.Frame, "0",
            () => PDynamicPresetCurrent()?.Frame ?? pDefault.Frame, PDynamicValueUpdate);
        pDynamicGauss = PInspectorDecimalBuild();
        pDynamicGauss.Text = pDefault.Gauss.ToString("0.###", CultureInfo.InvariantCulture);
        Slider pGaussSlider = PInspectorSliderBind(
            pDynamicGauss, LLevelingCatalog.LLevelingGaussLeast, LLevelingCatalog.LLevelingGaussMost, pDefault.Gauss, "0",
            () => PDynamicPresetCurrent()?.Gauss ?? pDefault.Gauss, PDynamicValueUpdate);
        pDynamicMaxGain = PInspectorDecimalBuild();
        pDynamicMaxGain.Text = pDefault.MaxGain.ToString("0.###", CultureInfo.InvariantCulture);
        Slider pMaxGainSlider = PInspectorSliderBind(
            pDynamicMaxGain, LLevelingCatalog.LLevelingGainLeast, LLevelingCatalog.LLevelingGainMost, pDefault.MaxGain, "0.#",
            () => PDynamicPresetCurrent()?.MaxGain ?? pDefault.MaxGain, PDynamicValueUpdate);
        pDynamicCompress = PInspectorDecimalBuild();
        pDynamicCompress.Text = pDefault.Compress.ToString("0.###", CultureInfo.InvariantCulture);
        Slider pCompressSlider = PInspectorSliderBind(
            pDynamicCompress, LLevelingCatalog.LLevelingCompressLeast, LLevelingCatalog.LLevelingCompressMost, pDefault.Compress, "0.#",
            () => PDynamicPresetCurrent()?.Compress ?? pDefault.Compress, PDynamicValueUpdate);

        pLoudnessTwoPass = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Normalize.TwoPass"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Normalize.TwoPassTooltip"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            IsChecked = pDefault.TwoPass,
            Margin = new Thickness(0, 8, 0, 0)
        };
        PMainWindow.PCheckbox.PCheckboxApply(pLoudnessTwoPass);
        pLoudnessTwoPass.Checked += (_, _) => PInspectorActiveRaise();
        pLoudnessTwoPass.Unchecked += (_, _) => PInspectorActiveRaise();

        pLoudnessStack = new StackPanel();
        pLoudnessStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Normalize.Target"), pTargetSlider, "LUFS", pLoudnessTarget));
        pLoudnessStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Normalize.Peak"), pPeakSlider, "dBTP", pLoudnessPeak));
        pLoudnessStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Normalize.Range"), pRangeSlider, "LU", pLoudnessRange));
        pLoudnessStack.Children.Add(pLoudnessTwoPass);

        pDynamicStack = new StackPanel { Visibility = Visibility.Collapsed };
        pDynamicStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Dynamic.Frame"), pFrameSlider, "ms", pDynamicFrame));
        pDynamicStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Dynamic.Smoothness"), pGaussSlider, "g", pDynamicGauss));
        pDynamicStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Dynamic.MaxGain"), pMaxGainSlider, "×", pDynamicMaxGain));
        pDynamicStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Dynamic.Compress"), pCompressSlider, "s", pDynamicCompress));

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
        pLoudnessPanel.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Normalize.Mode"), pLoudnessMode));
        pLoudnessPanel.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pLoudnessPreset));
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

    private (double Target, double Peak, double Range)? PLoudnessPresetCurrent() =>
        pLoudnessBaseToken is { } pBase ? LLevelingCatalog.LLevelingLoudnessRead(pBase) : null;

    private static string PLoudnessKeyRead(string pToken) => pToken switch
    {
        "Loud" => "Inspector.Normalize.Loud",
        "Streaming" => "Inspector.Normalize.Streaming",
        "Podcast" => "Inspector.Normalize.Podcast",
        "Dialogue" => "Inspector.Normalize.Dialogue",
        "Audiobook" => "Inspector.Normalize.Audiobook",
        "Broadcast" => "Inspector.Normalize.Broadcast",
        "TV" => "Inspector.Normalize.TV",
        "Film" => "Inspector.Normalize.Film",
        _ => "Inspector.Common.Custom"
    };

    private string? PLoudnessValuesMatch() =>
        LLevelingCatalog.LLevelingLoudnessMatch(
            PInspectorDecimalRead(pLoudnessTarget, -16),
            PInspectorDecimalRead(pLoudnessPeak, -1.5),
            PInspectorDecimalRead(pLoudnessRange, 11));

    private void PLoudnessValuesApply((double Target, double Peak, double Range) pPreset)
    {
        pLoudnessPresetSuppress = true;
        pLoudnessTarget.Text = pPreset.Target.ToString("0.###", CultureInfo.InvariantCulture);
        pLoudnessPeak.Text = pPreset.Peak.ToString("0.###", CultureInfo.InvariantCulture);
        pLoudnessRange.Text = pPreset.Range.ToString("0.###", CultureInfo.InvariantCulture);
        pLoudnessPresetSuppress = false;
    }

    private void PLoudnessPresetApply()
    {
        if (pLoudnessPresetSuppress)
        {
            return;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pLoudnessPreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom" || LLevelingCatalog.LLevelingLoudnessRead(pName) is not { } pPreset)
        {
            pLoudnessBaseToken = null;
            return;
        }

        pLoudnessBaseToken = pName;
        PLoudnessValuesApply(pPreset);
        PLoudnessCustomReset();
        PInspectorActiveRaise();
    }

    private void PLoudnessDeviationCheck()
    {
        if (pLoudnessPresetSuppress || pLoudnessBaseToken is not { } pBase
            || LLevelingCatalog.LLevelingLoudnessRead(pBase) is null)
        {
            return;
        }

        pLoudnessPresetSuppress = true;
        if (PLoudnessValuesMatch() == pBase)
        {
            PLoudnessCustomReset();
            PLoudnessPresetSelect(pBase);
        }
        else
        {
            PLoudnessCustomSet(pBase);
        }

        pLoudnessPresetSuppress = false;
    }

    private void PLoudnessValueUpdate()
    {
        PLoudnessDeviationCheck();
        PInspectorActiveRaise();
    }

    private void PLoudnessCustomSet(string pBase)
    {
        int pLast = pLoudnessPreset.Items.Count - 1;
        string pText = LLocalization.LLocalizationFormat(
            "Inspector.Common.PresetCustom",
            LLocalization.LLocalizationTextRead(PLoudnessKeyRead(pBase)));
        pLoudnessPreset.Items[pLast] = new LLocalizationChoice("Custom", string.Empty, pText);
        pLoudnessPreset.SelectedIndex = pLast;
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

    private void PNormalizePresetUpdate(string? pMatch)
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
        PNormalizePresetBuild(pLoudness);
        pLoudnessPresetSuppress = false;

        PNormalizePresetUpdate(pLoudness ? PLoudnessValuesMatch() : PDynamicValuesMatch());

        PInspectorActiveRaise();
    }

    private void PNormalizePresetBuild(bool pLoudness)
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

    private void PNormalizePresetApply()
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

    private (double Frame, double Gauss, double MaxGain, double Compress)? PDynamicPresetCurrent() =>
        pLoudnessBaseToken is { } pBase ? LLevelingCatalog.LLevelingDynamicRead(pBase) : null;

    private static string PDynamicKeyRead(string pToken) => pToken switch
    {
        "Gentle" => "Inspector.Dynamic.Gentle",
        "Leveler" => "Inspector.Dynamic.Leveler",
        "Voice" => "Inspector.Dynamic.Voice",
        "Aggressive" => "Inspector.Dynamic.Aggressive",
        "Music" => "Inspector.Dynamic.Music",
        _ => "Inspector.Common.Custom"
    };

    private string? PDynamicValuesMatch() =>
        LLevelingCatalog.LLevelingDynamicMatch(
            PInspectorDecimalRead(pDynamicFrame, 300),
            PInspectorDecimalRead(pDynamicGauss, 21),
            PInspectorDecimalRead(pDynamicMaxGain, 10),
            PInspectorDecimalRead(pDynamicCompress, 6));

    private void PDynamicValuesApply((double Frame, double Gauss, double MaxGain, double Compress) pPreset)
    {
        pLoudnessPresetSuppress = true;
        pDynamicFrame.Text = pPreset.Frame.ToString("0.###", CultureInfo.InvariantCulture);
        pDynamicGauss.Text = pPreset.Gauss.ToString("0.###", CultureInfo.InvariantCulture);
        pDynamicMaxGain.Text = pPreset.MaxGain.ToString("0.###", CultureInfo.InvariantCulture);
        pDynamicCompress.Text = pPreset.Compress.ToString("0.###", CultureInfo.InvariantCulture);
        pLoudnessPresetSuppress = false;
    }

    private void PDynamicPresetApply()
    {
        if (pLoudnessPresetSuppress)
        {
            return;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pLoudnessPreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom" || LLevelingCatalog.LLevelingDynamicRead(pName) is not { } pPreset)
        {
            pLoudnessBaseToken = null;
            return;
        }

        pLoudnessBaseToken = pName;
        PDynamicValuesApply(pPreset);
        PLoudnessCustomReset();
        PInspectorActiveRaise();
    }

    private void PDynamicDeviationCheck()
    {
        if (pLoudnessPresetSuppress || pLoudnessBaseToken is not { } pBase
            || LLevelingCatalog.LLevelingDynamicRead(pBase) is null)
        {
            return;
        }

        pLoudnessPresetSuppress = true;
        if (PDynamicValuesMatch() == pBase)
        {
            PLoudnessCustomReset();
            PLoudnessPresetSelect(pBase);
        }
        else
        {
            PDynamicCustomSet(pBase);
        }

        pLoudnessPresetSuppress = false;
    }

    private void PDynamicValueUpdate()
    {
        PDynamicDeviationCheck();
        PInspectorActiveRaise();
    }

    private void PDynamicCustomSet(string pBase)
    {
        int pLast = pLoudnessPreset.Items.Count - 1;
        string pText = LLocalization.LLocalizationFormat(
            "Inspector.Common.PresetCustom",
            LLocalization.LLocalizationTextRead(PDynamicKeyRead(pBase)));
        pLoudnessPreset.Items[pLast] = new LLocalizationChoice("Custom", string.Empty, pText);
        pLoudnessPreset.SelectedIndex = pLast;
    }
}
