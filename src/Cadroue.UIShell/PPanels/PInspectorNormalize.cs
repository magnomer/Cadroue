using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private const double PLoudnessTargetLeast = -36;
    private const double PLoudnessTargetMost = -5;
    private const double PLoudnessPeakLeast = -9;
    private const double PLoudnessPeakMost = 0;
    private const double PLoudnessRangeLeast = 1;
    private const double PLoudnessRangeMost = 20;
    private const double PDynamicFrameLeast = 50;
    private const double PDynamicFrameMost = 1000;
    private const double PDynamicGaussLeast = 3;
    private const double PDynamicGaussMost = 101;
    private const double PDynamicGainLeast = 1;
    private const double PDynamicGainMost = 40;
    private const double PDynamicCompressLeast = 0;
    private const double PDynamicCompressMost = 30;

    private CheckBox pLoudnessApplyBox = null!;
    private CheckBox pInspectorNormalizePersistent = null!;
    private ComboBox pInspectorNormalizePreset = null!;
    private ComboBox pInspectorNormalizeMode = null!;
    private TextBox pInspectorNormalizeTarget = null!;
    private TextBox pInspectorNormalizePeak = null!;
    private TextBox pInspectorNormalizeRange = null!;
    private TextBox pInspectorNormalizeFrame = null!;
    private TextBox pInspectorNormalizeGauss = null!;
    private TextBox pInspectorNormalizeMaxGain = null!;
    private TextBox pInspectorNormalizeCompress = null!;
    private CheckBox pLoudnessTwoPass = null!;
    private StackPanel pInspectorNormalizeStack = null!;
    private StackPanel pLoudnessStack = null!;
    private StackPanel pDynamicStack = null!;
    private StackPanel pInspectorNormalizeBody = null!;
    private string? pInspectorNormalizeBaseToken;
    private bool pInspectorNormalizePresetSuppress;

    private LLeveling PLoudnessModeRead() =>
        pInspectorNormalizeMode.SelectedIndex == 1
            ? LLeveling.LLevelingDynamic
            : LLeveling.LLevelingLoudness;

    private StackPanel PLoudnessBodyBuild()
    {
        pLoudnessApplyBox = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), LLocalization.LLocalizationTextRead("Inspector.Normalize.ApplyTooltip"));
        pLoudnessApplyBox.Checked += (_, _) => PLoudnessApplyUpdate();
        pLoudnessApplyBox.Unchecked += (_, _) => PLoudnessApplyUpdate();

        pInspectorNormalizePersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Normalize.PersistentTooltip"));

        pInspectorNormalizePreset = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pInspectorNormalizePreset);
        PNormalizePresetBuild(true);
        pInspectorNormalizePreset.SelectionChanged += (_, _) => PNormalizePresetApply();

        pInspectorNormalizeMode = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pInspectorNormalizeMode);
        pInspectorNormalizeMode.Items.Add(new LLocalizationChoice("Loudness", "Inspector.Normalize.Loudness"));
        pInspectorNormalizeMode.Items.Add(new LLocalizationChoice("Dynamic", "Inspector.Normalize.Dynamic"));
        pInspectorNormalizeMode.SelectedIndex = 0;
        pInspectorNormalizeMode.SelectionChanged += (_, _) => PLoudnessModeUpdate();

        pInspectorNormalizeTarget = PInspectorDecimalBuild();
        pInspectorNormalizeTarget.Text = "-21";
        Slider pTargetSlider = PInspectorSliderBind(
            pInspectorNormalizeTarget, PLoudnessTargetLeast, PLoudnessTargetMost, -21, "0.#",
            () => PLoudnessPresetCurrent()?.Target ?? -21, PLoudnessDeviationCheck);
        pInspectorNormalizePeak = PInspectorDecimalBuild();
        pInspectorNormalizePeak.Text = "-2";
        Slider pPeakSlider = PInspectorSliderBind(
            pInspectorNormalizePeak, PLoudnessPeakLeast, PLoudnessPeakMost, -2, "0.#",
            () => PLoudnessPresetCurrent()?.Peak ?? -2, PLoudnessDeviationCheck);
        pInspectorNormalizeRange = PInspectorDecimalBuild();
        pInspectorNormalizeRange.Text = "6";
        Slider pRangeSlider = PInspectorSliderBind(
            pInspectorNormalizeRange, PLoudnessRangeLeast, PLoudnessRangeMost, 6, "0.#",
            () => PLoudnessPresetCurrent()?.Range ?? 6, PLoudnessDeviationCheck);

        pInspectorNormalizeFrame = PInspectorDecimalBuild();
        pInspectorNormalizeFrame.Text = "300";
        Slider pFrameSlider = PInspectorSliderBind(
            pInspectorNormalizeFrame, PDynamicFrameLeast, PDynamicFrameMost, 300, "0",
            () => PDynamicPresetCurrent()?.Frame ?? 300, PDynamicDeviationCheck);
        pInspectorNormalizeGauss = PInspectorDecimalBuild();
        pInspectorNormalizeGauss.Text = "21";
        Slider pGaussSlider = PInspectorSliderBind(
            pInspectorNormalizeGauss, PDynamicGaussLeast, PDynamicGaussMost, 21, "0",
            () => PDynamicPresetCurrent()?.Gauss ?? 21, PDynamicDeviationCheck);
        pInspectorNormalizeMaxGain = PInspectorDecimalBuild();
        pInspectorNormalizeMaxGain.Text = "10";
        Slider pMaxGainSlider = PInspectorSliderBind(
            pInspectorNormalizeMaxGain, PDynamicGainLeast, PDynamicGainMost, 10, "0.#",
            () => PDynamicPresetCurrent()?.MaxGain ?? 10, PDynamicDeviationCheck);
        pInspectorNormalizeCompress = PInspectorDecimalBuild();
        pInspectorNormalizeCompress.Text = "6";
        Slider pCompressSlider = PInspectorSliderBind(
            pInspectorNormalizeCompress, PDynamicCompressLeast, PDynamicCompressMost, 6, "0.#",
            () => PDynamicPresetCurrent()?.Compress ?? 6, PDynamicDeviationCheck);

        pLoudnessTwoPass = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Normalize.TwoPass"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Normalize.TwoPassTooltip"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            IsChecked = true,
            Margin = new Thickness(0, 8, 0, 0)
        };
        PMainWindow.PCheckbox.PCheckboxApply(pLoudnessTwoPass);

        pLoudnessStack = new StackPanel();
        pLoudnessStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Normalize.Target"), pTargetSlider, "LUFS", pInspectorNormalizeTarget));
        pLoudnessStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Normalize.Peak"), pPeakSlider, "dBTP", pInspectorNormalizePeak));
        pLoudnessStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Normalize.Range"), pRangeSlider, "LU", pInspectorNormalizeRange));
        pLoudnessStack.Children.Add(pLoudnessTwoPass);

        pDynamicStack = new StackPanel { Visibility = Visibility.Collapsed };
        pDynamicStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Dynamic.Frame"), pFrameSlider, "ms", pInspectorNormalizeFrame));
        pDynamicStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Dynamic.Smoothness"), pGaussSlider, "g", pInspectorNormalizeGauss));
        pDynamicStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Dynamic.MaxGain"), pMaxGainSlider, "×", pInspectorNormalizeMaxGain));
        pDynamicStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Dynamic.Compress"), pCompressSlider, "s", pInspectorNormalizeCompress));

        var pNotice = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Normalize.Notice"),
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        };

        pInspectorNormalizeStack = new StackPanel();
        pInspectorNormalizeStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Normalize.Mode"), pInspectorNormalizeMode));
        pInspectorNormalizeStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pInspectorNormalizePreset));
        pInspectorNormalizeStack.Children.Add(pLoudnessStack);
        pInspectorNormalizeStack.Children.Add(pDynamicStack);
        pInspectorNormalizeStack.Children.Add(pNotice);

        pInspectorNormalizeBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pInspectorNormalizeBody.Children.Add(pLoudnessApplyBox);
        pInspectorNormalizeBody.Children.Add(PInspectorSeparatorBuild());
        pInspectorNormalizeBody.Children.Add(pInspectorNormalizeStack);

        PLoudnessApplyUpdate();
        PLoudnessModeUpdate();
        PLoudnessPresetApply();
        return pInspectorNormalizeBody;
    }

    private static (double Target, double Peak, double Range)? PLoudnessValuesRead(string pToken) =>
        pToken switch
        {
            "Loud" => (-9d, -1d, 6d),
            "Streaming" => (-14d, -1d, 9d),
            "Podcast" => (-16d, -1.5d, 8d),
            "Dialogue" => (-18d, -1.5d, 7d),
            "Audiobook" => (-21d, -2d, 6d),
            "Broadcast" => (-23d, -1d, 15d),
            "TV" => (-24d, -2d, 20d),
            "Film" => (-27d, -2d, 18d),
            _ => null
        };

    private (double Target, double Peak, double Range)? PLoudnessPresetCurrent() =>
        pInspectorNormalizeBaseToken is { } pBase ? PLoudnessValuesRead(pBase) : null;

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

    private bool PLoudnessValuesMatch((double Target, double Peak, double Range) pPreset) =>
        Math.Abs(PInspectorDecimalRead(pInspectorNormalizeTarget, -16) - pPreset.Target) < 0.05
        && Math.Abs(PInspectorDecimalRead(pInspectorNormalizePeak, -1.5) - pPreset.Peak) < 0.05
        && Math.Abs(PInspectorDecimalRead(pInspectorNormalizeRange, 11) - pPreset.Range) < 0.05;

    private void PLoudnessValuesApply((double Target, double Peak, double Range) pPreset)
    {
        pInspectorNormalizePresetSuppress = true;
        pInspectorNormalizeTarget.Text = pPreset.Target.ToString("0.###", CultureInfo.InvariantCulture);
        pInspectorNormalizePeak.Text = pPreset.Peak.ToString("0.###", CultureInfo.InvariantCulture);
        pInspectorNormalizeRange.Text = pPreset.Range.ToString("0.###", CultureInfo.InvariantCulture);
        pInspectorNormalizePresetSuppress = false;
    }

    private void PLoudnessPresetApply()
    {
        if (pInspectorNormalizePresetSuppress)
        {
            return;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pInspectorNormalizePreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom" || PLoudnessValuesRead(pName) is not { } pPreset)
        {
            pInspectorNormalizeBaseToken = null;
            return;
        }

        pInspectorNormalizeBaseToken = pName;
        PLoudnessValuesApply(pPreset);
        PLoudnessCustomReset();
        PInspectorActiveRaise();
    }

    private void PLoudnessDeviationCheck()
    {
        if (pInspectorNormalizePresetSuppress || pInspectorNormalizeBaseToken is not { } pBase
            || PLoudnessValuesRead(pBase) is not { } pPreset)
        {
            return;
        }

        pInspectorNormalizePresetSuppress = true;
        if (PLoudnessValuesMatch(pPreset))
        {
            PLoudnessCustomReset();
            PLoudnessPresetSelect(pBase);
        }
        else
        {
            PLoudnessCustomSet(pBase);
        }

        pInspectorNormalizePresetSuppress = false;
    }

    private void PLoudnessCustomSet(string pBase)
    {
        int pLast = pInspectorNormalizePreset.Items.Count - 1;
        string pText = LLocalization.LLocalizationFormat(
            "Inspector.Common.PresetCustom",
            LLocalization.LLocalizationTextRead(PLoudnessKeyRead(pBase)));
        pInspectorNormalizePreset.Items[pLast] = new LLocalizationChoice("Custom", string.Empty, pText);
        pInspectorNormalizePreset.SelectedIndex = pLast;
    }

    private void PLoudnessCustomReset()
    {
        int pLast = pInspectorNormalizePreset.Items.Count - 1;
        pInspectorNormalizePreset.Items[pLast] = new LLocalizationChoice("Custom", "Inspector.Common.Custom");
    }

    private void PLoudnessPresetSelect(string pToken)
    {
        for (int pIndex = 0; pIndex < pInspectorNormalizePreset.Items.Count; pIndex++)
        {
            if (LLocalizationChoice.LLocalizationChoiceRead(pInspectorNormalizePreset.Items[pIndex]) == pToken)
            {
                pInspectorNormalizePreset.SelectedIndex = pIndex;
                return;
            }
        }
    }

    private void PLoudnessPresetUpdate()
    {
        pInspectorNormalizePresetSuppress = true;
        string? pMatch = null;
        foreach (string pToken in new[] { "Loud", "Streaming", "Podcast", "Dialogue", "Audiobook", "Broadcast", "TV", "Film" })
        {
            if (PLoudnessValuesRead(pToken) is { } pPreset && PLoudnessValuesMatch(pPreset))
            {
                pMatch = pToken;
                break;
            }
        }

        if (pMatch is not null)
        {
            pInspectorNormalizeBaseToken = pMatch;
            PLoudnessCustomReset();
            PLoudnessPresetSelect(pMatch);
        }
        else
        {
            pInspectorNormalizeBaseToken = null;
            PLoudnessCustomReset();
            pInspectorNormalizePreset.SelectedIndex = pInspectorNormalizePreset.Items.Count - 1;
        }

        pInspectorNormalizePresetSuppress = false;
    }

    private void PLoudnessApplyUpdate()
    {
        bool pNormalizeActive = pLoudnessApplyBox.IsChecked == true;
        pInspectorNormalizeStack.IsEnabled = pNormalizeActive;
        pInspectorNormalizeStack.Opacity = pNormalizeActive ? 1 : 0.4;
        PInspectorActiveRaise();
    }

    private void PLoudnessModeUpdate()
    {
        bool pLoudness = PLoudnessModeRead() == LLeveling.LLevelingLoudness;
        pLoudnessStack.Visibility = pLoudness ? Visibility.Visible : Visibility.Collapsed;
        pDynamicStack.Visibility = pLoudness ? Visibility.Collapsed : Visibility.Visible;

        pInspectorNormalizePresetSuppress = true;
        PNormalizePresetBuild(pLoudness);
        pInspectorNormalizePresetSuppress = false;

        if (pLoudness)
        {
            PLoudnessPresetUpdate();
        }
        else
        {
            PDynamicPresetUpdate();
        }
    }

    private void PNormalizePresetBuild(bool pLoudness)
    {
        pInspectorNormalizePreset.Items.Clear();
        if (pLoudness)
        {
            foreach (string pToken in new[] { "Loud", "Streaming", "Podcast", "Dialogue", "Audiobook", "Broadcast", "TV", "Film" })
            {
                pInspectorNormalizePreset.Items.Add(new LLocalizationChoice(pToken, PLoudnessKeyRead(pToken)));
            }
        }
        else
        {
            foreach (string pToken in new[] { "Gentle", "Leveler", "Voice", "Aggressive", "Music" })
            {
                pInspectorNormalizePreset.Items.Add(new LLocalizationChoice(pToken, PDynamicKeyRead(pToken)));
            }
        }

        pInspectorNormalizePreset.Items.Add(new LLocalizationChoice("Custom", "Inspector.Common.Custom"));
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

    private static (double Frame, double Gauss, double MaxGain, double Compress)? PDynamicValuesRead(string pToken) =>
        pToken switch
        {
            "Gentle" => (500d, 31d, 7d, 0d),
            "Leveler" => (300d, 21d, 10d, 6d),
            "Voice" => (200d, 15d, 12d, 8d),
            "Aggressive" => (150d, 11d, 15d, 12d),
            "Music" => (400d, 31d, 8d, 0d),
            _ => null
        };

    private (double Frame, double Gauss, double MaxGain, double Compress)? PDynamicPresetCurrent() =>
        pInspectorNormalizeBaseToken is { } pBase ? PDynamicValuesRead(pBase) : null;

    private static string PDynamicKeyRead(string pToken) => pToken switch
    {
        "Gentle" => "Inspector.Dynamic.Gentle",
        "Leveler" => "Inspector.Dynamic.Leveler",
        "Voice" => "Inspector.Dynamic.Voice",
        "Aggressive" => "Inspector.Dynamic.Aggressive",
        "Music" => "Inspector.Dynamic.Music",
        _ => "Inspector.Common.Custom"
    };

    private bool PDynamicValuesMatch((double Frame, double Gauss, double MaxGain, double Compress) pPreset) =>
        Math.Abs(PInspectorDecimalRead(pInspectorNormalizeFrame, 300) - pPreset.Frame) < 0.5
        && Math.Abs(PInspectorDecimalRead(pInspectorNormalizeGauss, 21) - pPreset.Gauss) < 0.5
        && Math.Abs(PInspectorDecimalRead(pInspectorNormalizeMaxGain, 10) - pPreset.MaxGain) < 0.05
        && Math.Abs(PInspectorDecimalRead(pInspectorNormalizeCompress, 6) - pPreset.Compress) < 0.05;

    private void PDynamicValuesApply((double Frame, double Gauss, double MaxGain, double Compress) pPreset)
    {
        pInspectorNormalizePresetSuppress = true;
        pInspectorNormalizeFrame.Text = pPreset.Frame.ToString("0.###", CultureInfo.InvariantCulture);
        pInspectorNormalizeGauss.Text = pPreset.Gauss.ToString("0.###", CultureInfo.InvariantCulture);
        pInspectorNormalizeMaxGain.Text = pPreset.MaxGain.ToString("0.###", CultureInfo.InvariantCulture);
        pInspectorNormalizeCompress.Text = pPreset.Compress.ToString("0.###", CultureInfo.InvariantCulture);
        pInspectorNormalizePresetSuppress = false;
    }

    private void PDynamicPresetApply()
    {
        if (pInspectorNormalizePresetSuppress)
        {
            return;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pInspectorNormalizePreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom" || PDynamicValuesRead(pName) is not { } pPreset)
        {
            pInspectorNormalizeBaseToken = null;
            return;
        }

        pInspectorNormalizeBaseToken = pName;
        PDynamicValuesApply(pPreset);
        PLoudnessCustomReset();
        PInspectorActiveRaise();
    }

    private void PDynamicDeviationCheck()
    {
        if (pInspectorNormalizePresetSuppress || pInspectorNormalizeBaseToken is not { } pBase
            || PDynamicValuesRead(pBase) is not { } pPreset)
        {
            return;
        }

        pInspectorNormalizePresetSuppress = true;
        if (PDynamicValuesMatch(pPreset))
        {
            PLoudnessCustomReset();
            PLoudnessPresetSelect(pBase);
        }
        else
        {
            PDynamicCustomSet(pBase);
        }

        pInspectorNormalizePresetSuppress = false;
    }

    private void PDynamicCustomSet(string pBase)
    {
        int pLast = pInspectorNormalizePreset.Items.Count - 1;
        string pText = LLocalization.LLocalizationFormat(
            "Inspector.Common.PresetCustom",
            LLocalization.LLocalizationTextRead(PDynamicKeyRead(pBase)));
        pInspectorNormalizePreset.Items[pLast] = new LLocalizationChoice("Custom", string.Empty, pText);
        pInspectorNormalizePreset.SelectedIndex = pLast;
    }

    private void PDynamicPresetUpdate()
    {
        pInspectorNormalizePresetSuppress = true;
        string? pMatch = null;
        foreach (string pToken in new[] { "Gentle", "Leveler", "Voice", "Aggressive", "Music" })
        {
            if (PDynamicValuesRead(pToken) is { } pPreset && PDynamicValuesMatch(pPreset))
            {
                pMatch = pToken;
                break;
            }
        }

        if (pMatch is not null)
        {
            pInspectorNormalizeBaseToken = pMatch;
            PLoudnessCustomReset();
            PLoudnessPresetSelect(pMatch);
        }
        else
        {
            pInspectorNormalizeBaseToken = null;
            PLoudnessCustomReset();
            pInspectorNormalizePreset.SelectedIndex = pInspectorNormalizePreset.Items.Count - 1;
        }

        pInspectorNormalizePresetSuppress = false;
    }
}
