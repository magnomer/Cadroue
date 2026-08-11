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

    private CheckBox pToneGammaBox = null!;
    private CheckBox pInspectorGammaPersistent = null!;
    private Slider pInspectorGammaSlider = null!;
    private TextBox pInspectorGammaValue = null!;
    private Slider pInspectorGammaRedSlider = null!;
    private TextBox pInspectorGammaRedValue = null!;
    private Slider pInspectorGammaGreenSlider = null!;
    private TextBox pInspectorGammaGreenValue = null!;
    private Slider pInspectorGammaBlueSlider = null!;
    private TextBox pInspectorGammaBlueValue = null!;
    private Slider pInspectorGammaHighlightSlider = null!;
    private TextBox pInspectorGammaHighlightValue = null!;
    private StackPanel pInspectorGammaStack = null!;
    private StackPanel pInspectorGammaBody = null!;
    private bool pInspectorGammaCapable;

    private bool pInspectorVideoSuppress;
    private const double PToneBrightnessLeast = -100;
    private const double PToneBrightnessMost = 100;

    public event Action? PInspectorVideoChange;

    public LWorkVideoStep PToneStepRead(LColorKind pStepKind) => pStepKind switch
    {
        LColorKind.LColorKindContrast => LWorkVideoStep.LWorkContrastCreate(
            pToneContrastBox.IsChecked == true,
            PInspectorDecimalRead(pInspectorContrastValue, 100)),
        LColorKind.LColorKindGamma => LWorkVideoStep.LWorkGammaCreate(
            pToneGammaBox.IsChecked == true,
            PInspectorDecimalRead(pInspectorGammaValue, 0),
            PInspectorDecimalRead(pInspectorGammaRedValue, 0),
            PInspectorDecimalRead(pInspectorGammaGreenValue, 0),
            PInspectorDecimalRead(pInspectorGammaBlueValue, 0),
            PInspectorDecimalRead(pInspectorGammaHighlightValue, 0)),
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
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindGamma)
            ?? LWorkVideoStep.LWorkGammaCreate(false, 0));
        PInspectorVideoChange?.Invoke();
    }

    public bool PTonePersistentCheck() =>
        pInspectorBrightnessPersistent.IsChecked == true
        || pInspectorContrastPersistent.IsChecked == true
        || pInspectorGammaPersistent.IsChecked == true;

    public void PTonePersistentApply(LWorkVideo pVideo)
    {
        foreach (LWorkVideoStep pStep in pVideo.LWorkVideoSteps)
        {
            if (pStep.LWorkStepKind == LColorKind.LColorKindContrast)
            {
                pInspectorContrastPersistent.IsChecked = true;
            }
            else if (pStep.LWorkStepKind == LColorKind.LColorKindGamma)
            {
                pInspectorGammaPersistent.IsChecked = true;
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

        if (pInspectorGammaPersistent.IsChecked == true)
        {
            pSteps.Add(PToneStepRead(LColorKind.LColorKindGamma));
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

    private StackPanel PToneGammaBuild()
    {
        pToneGammaBox = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), LLocalization.LLocalizationTextRead("Inspector.Video.ApplyGamma"));
        pInspectorGammaPersistent = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"), LLocalization.LLocalizationTextRead("Inspector.Video.PersistGamma"));
        pInspectorGammaSlider = PToneSliderBuild(-100, 100, 0);
        pInspectorGammaValue = PInspectorDecimalBuild();
        pInspectorGammaValue.Text = "0";
        pInspectorGammaRedSlider = PToneSliderBuild(-100, 100, 0);
        pInspectorGammaRedValue = PInspectorDecimalBuild();
        pInspectorGammaRedValue.Text = "0";
        pInspectorGammaGreenSlider = PToneSliderBuild(-100, 100, 0);
        pInspectorGammaGreenValue = PInspectorDecimalBuild();
        pInspectorGammaGreenValue.Text = "0";
        pInspectorGammaBlueSlider = PToneSliderBuild(-100, 100, 0);
        pInspectorGammaBlueValue = PInspectorDecimalBuild();
        pInspectorGammaBlueValue.Text = "0";
        pInspectorGammaHighlightSlider = PToneSliderBuild(0, 100, 0);
        pInspectorGammaHighlightValue = PInspectorDecimalBuild();
        pInspectorGammaHighlightValue.Text = "0";
        pInspectorGammaStack = new StackPanel();
        PInspectorVideoAttach(
            pToneGammaBox,
            pInspectorGammaStack,
            pInspectorGammaSlider,
            pInspectorGammaValue,
            -100,
            100,
            "0.#");
        PInspectorVideoValueAttach(
            pInspectorGammaRedSlider, pInspectorGammaRedValue, -100, 100, "0.#");
        PInspectorVideoValueAttach(
            pInspectorGammaGreenSlider, pInspectorGammaGreenValue, -100, 100, "0.#");
        PInspectorVideoValueAttach(
            pInspectorGammaBlueSlider, pInspectorGammaBlueValue, -100, 100, "0.#");
        PInspectorVideoValueAttach(
            pInspectorGammaHighlightSlider, pInspectorGammaHighlightValue, 0, 100, "0.#");
        pInspectorGammaStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Video.Midtone"), pInspectorGammaSlider, "", pInspectorGammaValue));
        pInspectorGammaStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Video.RedGamma"), pInspectorGammaRedSlider, "", pInspectorGammaRedValue));
        pInspectorGammaStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Video.GreenGamma"), pInspectorGammaGreenSlider, "", pInspectorGammaGreenValue));
        pInspectorGammaStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Video.BlueGamma"), pInspectorGammaBlueSlider, "", pInspectorGammaBlueValue));
        pInspectorGammaStack.Children.Add(PFilterSliderBuild(LLocalization.LLocalizationTextRead("Inspector.Video.HighlightProtection"), pInspectorGammaHighlightSlider, "%", pInspectorGammaHighlightValue));
        var pGammaReset = new Button
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Video.GammaReset"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Video.GammaResetTooltip"),
            Height = 28,
            MinWidth = 64,
            Padding = new Thickness(8, 0, 8, 0),
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Style = PButton.PButtonPanelCreate(),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pGammaReset.Click += (_, _) => PToneGammaReset();
        pInspectorGammaStack.Children.Add(pGammaReset);
        pInspectorGammaBody = PToneBodyBuild(pToneGammaBox, pInspectorGammaStack);
        PToneApplyUpdate(pToneGammaBox, pInspectorGammaStack);
        return pInspectorGammaBody;
    }

    public void PToneGammaCapabilitySet(bool pGammaCapable)
    {
        pInspectorGammaCapable = pGammaCapable;
        pToneGammaBox.IsEnabled = pGammaCapable;
        pInspectorGammaPersistent.IsEnabled = pGammaCapable;
        pInspectorGammaStack.IsEnabled = pGammaCapable && pToneGammaBox.IsChecked == true;
        pInspectorGammaStack.Opacity = pGammaCapable && pToneGammaBox.IsChecked == true ? 1 : 0.4;
        string? pDisabledTooltip = pGammaCapable
            ? null
            : LLocalization.LLocalizationTextRead("Inspector.Video.GammaRequiresMpv");
        pInspectorGammaBody.ToolTip = pDisabledTooltip;
        pToneGammaBox.ToolTip = pDisabledTooltip ?? LLocalization.LLocalizationTextRead("Inspector.Video.ApplyGamma");
        pInspectorGammaPersistent.ToolTip = pDisabledTooltip ?? LLocalization.LLocalizationTextRead("Inspector.Video.PersistGamma");
        ToolTipService.SetShowOnDisabled(pInspectorGammaBody, true);
        ToolTipService.SetShowOnDisabled(pToneGammaBox, true);
        ToolTipService.SetShowOnDisabled(pInspectorGammaPersistent, true);
    }

    private void PToneGammaReset()
    {
        bool pPrevious = pInspectorVideoSuppress;
        bool pChanged = PInspectorDecimalRead(pInspectorGammaValue, pInspectorGammaSlider.Value) != 0
            || PInspectorDecimalRead(pInspectorGammaRedValue, pInspectorGammaRedSlider.Value) != 0
            || PInspectorDecimalRead(pInspectorGammaGreenValue, pInspectorGammaGreenSlider.Value) != 0
            || PInspectorDecimalRead(pInspectorGammaBlueValue, pInspectorGammaBlueSlider.Value) != 0
            || PInspectorDecimalRead(pInspectorGammaHighlightValue, pInspectorGammaHighlightSlider.Value) != 0;
        pInspectorVideoSuppress = true;
        try
        {
            PToneGammaValueSet(pInspectorGammaSlider, pInspectorGammaValue, 0);
            PToneGammaValueSet(pInspectorGammaRedSlider, pInspectorGammaRedValue, 0);
            PToneGammaValueSet(pInspectorGammaGreenSlider, pInspectorGammaGreenValue, 0);
            PToneGammaValueSet(pInspectorGammaBlueSlider, pInspectorGammaBlueValue, 0);
            PToneGammaValueSet(pInspectorGammaHighlightSlider, pInspectorGammaHighlightValue, 0);
        }
        finally
        {
            pInspectorVideoSuppress = pPrevious;
        }

        if (!pPrevious && pChanged)
        {
            PInspectorVideoChange?.Invoke();
        }
    }

    private static void PToneGammaValueSet(Slider pSlider, TextBox pValue, double pNumber)
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
        PInspectorVideoValueAttach(pSlider, pValue, pMinimum, pMaximum, pFormat);
    }

    private void PInspectorVideoValueAttach(
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

            if (pStep.LWorkStepKind == LColorKind.LColorKindGamma)
            {
                LWorkGammaSettings pGamma = pStep.LWorkGammaRead();
                pToneGammaBox.IsChecked = pStep.LWorkStepActive;
                PToneGammaValueSet(pInspectorGammaSlider, pInspectorGammaValue, pGamma.LWorkGammaGlobal);
                PToneGammaValueSet(pInspectorGammaRedSlider, pInspectorGammaRedValue, pGamma.LWorkGammaRed);
                PToneGammaValueSet(pInspectorGammaGreenSlider, pInspectorGammaGreenValue, pGamma.LWorkGammaGreen);
                PToneGammaValueSet(pInspectorGammaBlueSlider, pInspectorGammaBlueValue, pGamma.LWorkGammaBlue);
                PToneGammaValueSet(
                    pInspectorGammaHighlightSlider,
                    pInspectorGammaHighlightValue,
                    pGamma.LWorkGammaHighlightProtection);
                PToneApplyUpdate(pToneGammaBox, pInspectorGammaStack);
                PToneGammaCapabilitySet(pInspectorGammaCapable);
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
        if (!pInspectorVideoSuppress)
        {
            PInspectorVideoChange?.Invoke();
        }
    }
}
