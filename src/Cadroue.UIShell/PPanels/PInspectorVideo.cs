using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private CheckBox pToneBrightnessBox = null!;
    private CheckBox pInspectorBrightnessPersistent = null!;
    private Slider pInspectorBrightnessSlider = null!;
    private TextBox pInspectorBrightnessValue = null!;
    private StackPanel pInspectorBrightnessStack = null!;
    private StackPanel pInspectorBrightnessBody = null!;

    private CheckBox pToneContrastBox = null!;
    private CheckBox pInspectorContrastPersistent = null!;
    private Slider pInspectorContrastSlider = null!;
    private TextBox pInspectorContrastValue = null!;
    private StackPanel pInspectorContrastStack = null!;
    private StackPanel pInspectorContrastBody = null!;

    private CheckBox pToneSaturationBox = null!;
    private CheckBox pInspectorSaturationPersistent = null!;
    private Slider pInspectorSaturationSlider = null!;
    private TextBox pInspectorSaturationValue = null!;
    private StackPanel pInspectorSaturationStack = null!;
    private StackPanel pInspectorSaturationBody = null!;

    private bool pToneCapable = true;

    private bool pInspectorVideoSuppress;
    private const double PToneBrightnessLeast = -100;
    private const double PToneBrightnessMost = 100;

    public event Action? PInspectorVideoChange;

    public LWorkVideoStep PToneStepRead(LColorKind pStepKind) => pStepKind switch
    {
        LColorKind.LColorKindContrast => LWorkVideoStep.LWorkContrastCreate(
            pToneContrastBox.IsChecked == true,
            PInspectorDecimalRead(pInspectorContrastValue, 100)),
        LColorKind.LColorKindSaturation => LWorkVideoStep.LWorkSaturationCreate(
            pToneSaturationBox.IsChecked == true,
            PInspectorDecimalRead(pInspectorSaturationValue, 100)),
        LColorKind.LColorKindGamma => LWorkVideoStep.LWorkGammaCreate(
            pGammaBox.IsChecked == true,
            PInspectorDecimalRead(pGammaValue, 0),
            PInspectorDecimalRead(pGammaRedValue, 0),
            PInspectorDecimalRead(pGammaGreenValue, 0),
            PInspectorDecimalRead(pGammaBlueValue, 0),
            PInspectorDecimalRead(pGammaHighlightValue, 0)),
        LColorKind.LColorKindWhitebalance => LWorkVideoStep.LWorkWhitebalanceCreate(
            pWhitebalanceBox.IsChecked == true,
            PWhitebalanceMethodRead(),
            PInspectorDecimalRead(pWhitebalanceSaturationValue, 100),
            pWhitebalanceRedGain,
            pWhitebalanceGreenGain,
            pWhitebalanceBlueGain,
            pWhitebalanceSampleRed,
            pWhitebalanceSampleGreen,
            pWhitebalanceSampleBlue),
        LColorKind.LColorKindExposure => LWorkVideoStep.LWorkExposureCreate(
            pExposureBox.IsChecked == true,
            PInspectorDecimalRead(pExposureValue, 0)),
        _ => LWorkVideoStep.LWorkBrightnessCreate(
            pToneBrightnessBox.IsChecked == true,
            PInspectorDecimalRead(pInspectorBrightnessValue, 0))
    };

    public void PTonePlanApply(LWorkVideo pVideo)
    {
        PToneStepApply(
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindBrightness)
            ?? LWorkVideoStep.LWorkBrightnessCreate(false, 0));
        PToneStepApply(
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindContrast)
            ?? LWorkVideoStep.LWorkContrastCreate(false, 100));
        PToneStepApply(
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindSaturation)
            ?? LWorkVideoStep.LWorkSaturationCreate(false, 100));
        PToneStepApply(
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindGamma)
            ?? LWorkVideoStep.LWorkGammaCreate(false, 0));
        PToneStepApply(
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindWhitebalance)
            ?? LWorkVideoStep.LWorkWhitebalanceCreate(false));
        PToneStepApply(
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindExposure)
            ?? LWorkVideoStep.LWorkExposureCreate(false, 0));
        PInspectorVideoChange?.Invoke();
    }

    public bool PTonePersistentCheck() =>
        pInspectorBrightnessPersistent.IsChecked == true
        || pInspectorContrastPersistent.IsChecked == true
        || pInspectorSaturationPersistent.IsChecked == true
        || pGammaPersistent.IsChecked == true
        || pWhitebalancePersistent.IsChecked == true
        || pExposurePersistent.IsChecked == true;

    public void PTonePersistentApply(LWorkVideo pVideo)
    {
        foreach (LWorkVideoStep pStep in pVideo.LWorkVideoSteps)
        {
            if (pStep.LWorkStepKind == LColorKind.LColorKindContrast)
            {
                pInspectorContrastPersistent.IsChecked = true;
            }
            else if (pStep.LWorkStepKind == LColorKind.LColorKindSaturation)
            {
                pInspectorSaturationPersistent.IsChecked = true;
            }
            else if (pStep.LWorkStepKind == LColorKind.LColorKindGamma)
            {
                pGammaPersistent.IsChecked = true;
            }
            else if (pStep.LWorkStepKind == LColorKind.LColorKindWhitebalance)
            {
                pWhitebalancePersistent.IsChecked = true;
            }
            else if (pStep.LWorkStepKind == LColorKind.LColorKindExposure)
            {
                pExposurePersistent.IsChecked = true;
            }
            else
            {
                pInspectorBrightnessPersistent.IsChecked = true;
            }
        }
    }

    public LWorkVideo PTonePersistentRead()
    {
        var pSteps = new List<LWorkVideoStep>();
        if (pInspectorBrightnessPersistent.IsChecked == true)
        {
            pSteps.Add(PToneStepRead(LColorKind.LColorKindBrightness));
        }

        if (pInspectorContrastPersistent.IsChecked == true)
        {
            pSteps.Add(PToneStepRead(LColorKind.LColorKindContrast));
        }

        if (pInspectorSaturationPersistent.IsChecked == true)
        {
            pSteps.Add(PToneStepRead(LColorKind.LColorKindSaturation));
        }

        if (pGammaPersistent.IsChecked == true)
        {
            pSteps.Add(PToneStepRead(LColorKind.LColorKindGamma));
        }

        if (pWhitebalancePersistent.IsChecked == true)
        {
            pSteps.Add(PToneStepRead(LColorKind.LColorKindWhitebalance));
        }

        if (pExposurePersistent.IsChecked == true)
        {
            pSteps.Add(PToneStepRead(LColorKind.LColorKindExposure));
        }

        return new LWorkVideo(pSteps);
    }

    private StackPanel PToneBrightnessBuild()
    {
        pToneBrightnessBox = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), LLocalization.LLocalizationTextRead("Inspector.Video.ApplyBrightness"));
        pInspectorBrightnessPersistent = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"), LLocalization.LLocalizationTextRead("Inspector.Video.PersistBrightness"));
        pInspectorBrightnessSlider = PToneSliderBuild(
            PToneBrightnessLeast,
            PToneBrightnessMost,
            0);
        pInspectorBrightnessValue = PInspectorDecimalBuild();
        pInspectorBrightnessValue.Text = "0";
        pInspectorBrightnessStack = new StackPanel();
        PInspectorVideoAttach(
            pToneBrightnessBox,
            pInspectorBrightnessStack,
            pInspectorBrightnessSlider,
            pInspectorBrightnessValue,
            null,
            null,
            "0.#");
        pInspectorBrightnessStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Amount"), pInspectorBrightnessSlider, string.Empty, pInspectorBrightnessValue));
        pInspectorBrightnessBody = PToneBodyBuild(pToneBrightnessBox, pInspectorBrightnessStack);
        PToneApplyUpdate(pToneBrightnessBox, pInspectorBrightnessStack);
        return pInspectorBrightnessBody;
    }

    private StackPanel PToneContrastBuild()
    {
        pToneContrastBox = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), LLocalization.LLocalizationTextRead("Inspector.Video.ApplyContrast"));
        pInspectorContrastPersistent = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"), LLocalization.LLocalizationTextRead("Inspector.Video.PersistContrast"));
        pInspectorContrastSlider = PToneSliderBuild(0, 200, 100);
        pInspectorContrastValue = PInspectorDecimalBuild();
        pInspectorContrastValue.Text = "100";
        pInspectorContrastStack = new StackPanel();
        PInspectorVideoAttach(
            pToneContrastBox,
            pInspectorContrastStack,
            pInspectorContrastSlider,
            pInspectorContrastValue,
            0,
            200,
            "0.#");
        pInspectorContrastStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Amount"), pInspectorContrastSlider, "%", pInspectorContrastValue));
        bool pContrastPreview = LFlyleaf.LFlyleafActive
            || LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;
        if (!pContrastPreview)
        {
            pInspectorContrastStack.Children.Add(new TextBlock
            {
                Text = LLocalization.LLocalizationTextRead("Inspector.Video.ContrastPreview"),
                Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x64, 0x70, 0x82)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        pInspectorContrastBody = PToneBodyBuild(pToneContrastBox, pInspectorContrastStack);
        PToneApplyUpdate(pToneContrastBox, pInspectorContrastStack);
        return pInspectorContrastBody;
    }

    private StackPanel PToneSaturationBuild()
    {
        pToneSaturationBox = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), LLocalization.LLocalizationTextRead("Inspector.Video.ApplySaturation"));
        pInspectorSaturationPersistent = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"), LLocalization.LLocalizationTextRead("Inspector.Video.PersistSaturation"));
        pInspectorSaturationSlider = PToneSliderBuild(0, 200, 100);
        pInspectorSaturationValue = PInspectorDecimalBuild();
        pInspectorSaturationValue.Text = "100";
        pInspectorSaturationStack = new StackPanel();
        PInspectorVideoAttach(
            pToneSaturationBox,
            pInspectorSaturationStack,
            pInspectorSaturationSlider,
            pInspectorSaturationValue,
            0,
            200,
            "0.#");
        pInspectorSaturationStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Amount"), pInspectorSaturationSlider, "%", pInspectorSaturationValue));
        pInspectorSaturationBody = PToneBodyBuild(pToneSaturationBox, pInspectorSaturationStack);
        PToneApplyUpdate(pToneSaturationBox, pInspectorSaturationStack);
        return pInspectorSaturationBody;
    }

    private static void PInspectorSectionApply(
        CheckBox pBox,
        CheckBox pPersistent,
        StackPanel pStack,
        StackPanel pBody,
        bool pCapable,
        string pDisabledKey,
        string pApplyKey,
        string pPersistKey)
    {
        pBox.IsEnabled = pCapable;
        pPersistent.IsEnabled = pCapable;
        pStack.IsEnabled = pCapable && pBox.IsChecked == true;
        pStack.Opacity = pCapable && pBox.IsChecked == true ? 1 : 0.4;
        string? pDisabledTooltip = pCapable
            ? null
            : LLocalization.LLocalizationTextRead(pDisabledKey);
        pBody.ToolTip = pDisabledTooltip;
        pBox.ToolTip = pDisabledTooltip ?? LLocalization.LLocalizationTextRead(pApplyKey);
        pPersistent.ToolTip = pDisabledTooltip ?? LLocalization.LLocalizationTextRead(pPersistKey);
        ToolTipService.SetShowOnDisabled(pBody, true);
        ToolTipService.SetShowOnDisabled(pBox, true);
        ToolTipService.SetShowOnDisabled(pPersistent, true);
    }

    public void PToneCapabilitySet(bool pCapable)
    {
        this.pToneCapable = pCapable;
        PInspectorSectionApply(
            pToneBrightnessBox, pInspectorBrightnessPersistent, pInspectorBrightnessStack, pInspectorBrightnessBody,
            pToneCapable, "Inspector.Video.BrightnessRequiresEq",
            "Inspector.Video.ApplyBrightness", "Inspector.Video.PersistBrightness");
        PInspectorSectionApply(
            pToneContrastBox, pInspectorContrastPersistent, pInspectorContrastStack, pInspectorContrastBody,
            pToneCapable, "Inspector.Video.ContrastRequiresEq",
            "Inspector.Video.ApplyContrast", "Inspector.Video.PersistContrast");
        PInspectorSectionApply(
            pToneSaturationBox, pInspectorSaturationPersistent, pInspectorSaturationStack, pInspectorSaturationBody,
            pToneCapable, "Inspector.Video.SaturationRequiresEq",
            "Inspector.Video.ApplySaturation", "Inspector.Video.PersistSaturation");
    }

    private static void PInspectorValueSet(Slider pSlider, TextBox pValue, double pNumber)
    {
        pSlider.Value = pNumber;
        pValue.Text = pNumber.ToString("0.#", CultureInfo.InvariantCulture);
    }

    private static Slider PToneSliderBuild(double pMinimum, double pMaximum, double pValue)
    {
        var pSlider = new Slider
        {
            Minimum = pMinimum,
            Maximum = pMaximum,
            Value = pValue,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);
        PSlider.PSliderResetApply(pSlider, () => pValue);
        return pSlider;
    }

    private static StackPanel PToneBodyBuild(CheckBox pApply, StackPanel pStack)
    {
        var pBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pBody.Children.Add(pApply);
        pBody.Children.Add(PInspectorSeparatorBuild());
        pBody.Children.Add(pStack);
        return pBody;
    }

    private void PInspectorVideoAttach(
        CheckBox pApply,
        StackPanel pStack,
        Slider pSlider,
        TextBox pValue,
        double? pMinimum,
        double? pMaximum,
        string pFormat)
    {
        pApply.Checked += (_, _) => PToneApplyUpdate(pApply, pStack);
        pApply.Unchecked += (_, _) => PToneApplyUpdate(pApply, pStack);
        PInspectorValueAttach(pSlider, pValue, pMinimum, pMaximum, pFormat);
    }

    private void PInspectorValueAttach(
        Slider pSlider,
        TextBox pValue,
        double? pMinimum,
        double? pMaximum,
        string pFormat)
    {
        pSlider.ValueChanged += (_, _) =>
        {
            if (pInspectorVideoSuppress)
            {
                return;
            }

            pInspectorVideoSuppress = true;
            pValue.Text = pSlider.Value.ToString(pFormat, CultureInfo.InvariantCulture);
            pInspectorVideoSuppress = false;
            PInspectorVideoChange?.Invoke();
        };
        pValue.TextChanged += (_, _) =>
        {
            if (pInspectorVideoSuppress)
            {
                return;
            }

            pInspectorVideoSuppress = true;
            double pParsed = PInspectorDecimalRead(pValue, pSlider.Value);
            if (pMinimum is double pMin && pMaximum is double pMax)
            {
                pParsed = Math.Clamp(pParsed, pMin, pMax);
            }

            pSlider.Value = Math.Clamp(pParsed, pSlider.Minimum, pSlider.Maximum);
            pInspectorVideoSuppress = false;
            PInspectorVideoChange?.Invoke();
        };
    }

    private void PToneStepApply(LWorkVideoStep pStep)
    {
        bool pPrevious = pInspectorVideoSuppress;
        pInspectorVideoSuppress = true;
        try
        {
            if (pStep.LWorkStepKind == LColorKind.LColorKindContrast)
            {
                pToneContrastBox.IsChecked = pStep.LWorkStepActive;
                pInspectorContrastValue.Text = pStep.LWorkStepValue.ToString("0.#", CultureInfo.InvariantCulture);
                pInspectorContrastSlider.Value = Math.Clamp(pStep.LWorkStepValue, 0, 200);
                PToneApplyUpdate(pToneContrastBox, pInspectorContrastStack);
                return;
            }

            if (pStep.LWorkStepKind == LColorKind.LColorKindSaturation)
            {
                pToneSaturationBox.IsChecked = pStep.LWorkStepActive;
                pInspectorSaturationValue.Text = pStep.LWorkStepValue.ToString("0.#", CultureInfo.InvariantCulture);
                pInspectorSaturationSlider.Value = Math.Clamp(pStep.LWorkStepValue, 0, 200);
                PToneApplyUpdate(pToneSaturationBox, pInspectorSaturationStack);
                return;
            }

            if (pStep.LWorkStepKind == LColorKind.LColorKindGamma)
            {
                LWorkGammaSettings pGamma = pStep.LWorkGammaRead();
                pGammaBox.IsChecked = pStep.LWorkStepActive;
                PInspectorValueSet(pGammaSlider, pGammaValue, pGamma.LWorkGammaGlobal);
                PInspectorValueSet(pGammaRedSlider, pGammaRedValue, pGamma.LWorkGammaRed);
                PInspectorValueSet(pGammaGreenSlider, pGammaGreenValue, pGamma.LWorkGammaGreen);
                PInspectorValueSet(pGammaBlueSlider, pGammaBlueValue, pGamma.LWorkGammaBlue);
                PInspectorValueSet(
                    pGammaHighlightSlider,
                    pGammaHighlightValue,
                    pGamma.LWorkGammaHighlight);
                PToneApplyUpdate(pGammaBox, pGammaStack);
                PGammaCapabilitySet(pGammaCapable, pGammaDisabledKey);
                return;
            }

            if (pStep.LWorkStepKind == LColorKind.LColorKindWhitebalance)
            {
                LWorkWhitebalanceSettings pWhitebalance = pStep.LWorkWhitebalanceRead();
                pWhitebalanceBox.IsChecked = pStep.LWorkStepActive;
                pWhitebalanceManual =
                    pWhitebalance.LWorkWhitebalanceMethod == LWhitebalanceMethod.LWhitebalanceMethodManual;
                PToneNeutralRestore(pWhitebalance);
                pWhitebalanceMethod.SelectedIndex = PWhitebalanceIndexRead(
                    pWhitebalance.LWorkWhitebalanceMethod);
                PWhitebalanceManualUpdate();
                PInspectorValueSet(
                    pWhitebalanceSaturationSlider,
                    pWhitebalanceSaturationValue,
                    pWhitebalance.LWorkWhitebalanceSaturation);
                PToneApplyUpdate(pWhitebalanceBox, pWhitebalanceStack);
                PWhitebalanceCapabilitySet(pWhitebalanceCapable);
                return;
            }

            if (pStep.LWorkStepKind == LColorKind.LColorKindExposure)
            {
                pExposureBox.IsChecked = pStep.LWorkStepActive;
                pExposureValue.Text = pStep.LWorkStepValue.ToString("0.#", CultureInfo.InvariantCulture);
                pExposureSlider.Value = Math.Clamp(pStep.LWorkStepValue, -3, 3);
                PToneApplyUpdate(pExposureBox, pExposureStack);
                PExposureCapabilitySet(pExposureCapable);
                return;
            }

            pToneBrightnessBox.IsChecked = pStep.LWorkStepActive;
            pInspectorBrightnessValue.Text = pStep.LWorkStepValue.ToString("0.#", CultureInfo.InvariantCulture);
            pInspectorBrightnessSlider.Value = Math.Clamp(
                pStep.LWorkStepValue,
                pInspectorBrightnessSlider.Minimum,
                pInspectorBrightnessSlider.Maximum);
            PToneApplyUpdate(pToneBrightnessBox, pInspectorBrightnessStack);
        }
        finally
        {
            pInspectorVideoSuppress = pPrevious;
        }
    }

    private void PToneApplyUpdate(CheckBox pApply, StackPanel pStack)
    {
        bool pActive = pApply.IsChecked == true;
        pStack.IsEnabled = pActive;
        pStack.Opacity = pActive ? 1 : 0.4;
        if (!pActive && ReferenceEquals(pApply, pWhitebalanceBox))
        {
            PWhitebalanceToolReset();
        }

        if (!pInspectorVideoSuppress)
        {
            PInspectorVideoChange?.Invoke();
        }
    }
}
