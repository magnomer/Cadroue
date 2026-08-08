using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private CheckBox pVolumeApplyBox = null!;
    private CheckBox pInspectorVolumePersistent = null!;
    private Slider pInspectorVolumeSlider = null!;
    private TextBox pInspectorVolumeValue = null!;
    private StackPanel pInspectorVolumeStack = null!;
    private TextBlock pInspectorVolumeWarn = null!;
    private StackPanel pInspectorVolumeBody = null!;
    private bool pInspectorVolumeSuppress;

    public event Action? PInspectorAudioChange;

    private void PInspectorActiveRaise() => PInspectorAudioChange?.Invoke();

    public LWorkAudioStep PInspectorStepRead(LAudioKind pStepKind) => pStepKind switch
    {
        LAudioKind.LAudioKindNormalize => LWorkAudioStep.LWorkNormalizeCreate(
            pLoudnessApplyBox.IsChecked == true,
            PLoudnessModeRead(),
            PInspectorDecimalRead(pLoudnessTarget, -16),
            PInspectorDecimalRead(pLoudnessPeak, -1.5),
            PInspectorDecimalRead(pLoudnessRange, 11),
            pLoudnessTwoPass.IsChecked == true,
            PInspectorDecimalRead(pDynamicFrame, 300),
            PInspectorDecimalRead(pDynamicGauss, 21),
            PInspectorDecimalRead(pDynamicMaxGain, 10),
            PInspectorDecimalRead(pDynamicCompress, 6)),
        LAudioKind.LAudioKindDenoise => LWorkAudioStep.LWorkNoiseCreate(
            pNoiseApplyBox.IsChecked == true,
            Math.Clamp(PInspectorDecimalRead(pNoiseReductionValue, 12), LGrainCatalog.LGrainReductionLeast, LGrainCatalog.LGrainReductionMost),
            PInspectorDecimalRead(pNoiseFloor, -50),
            pNoiseTrack.IsChecked == true,
            PNoiseTypeRead(),
            Math.Clamp(PInspectorDecimalRead(pNoiseSmoothValue, 6), LGrainCatalog.LGrainSmoothLeast, LGrainCatalog.LGrainSmoothMost),
            Math.Clamp(PInspectorDecimalRead(pNoiseAdaptivity, 0.5), 0, 1),
            PInspectorDecimalRead(pNoiseResidual, -38)),
        LAudioKind.LAudioKindHighpass => LWorkAudioStep.LWorkHighCreate(
            pInspectorHighPass.PFilterApplyBox.IsChecked == true,
            PInspectorPassRead(pInspectorHighPass),
            PFilterStagesRead(pInspectorHighPass),
            PFilterPolesRead(pInspectorHighPass),
            PFilterResonanceRead(pInspectorHighPass)),
        LAudioKind.LAudioKindLowpass => LWorkAudioStep.LWorkLowCreate(
            pInspectorLowPass.PFilterApplyBox.IsChecked == true,
            PInspectorPassRead(pInspectorLowPass),
            PFilterStagesRead(pInspectorLowPass),
            PFilterPolesRead(pInspectorLowPass),
            PFilterResonanceRead(pInspectorLowPass)),
        LAudioKind.LAudioKindEqualizer => PEqualizerStepRead(),
        _ => LWorkAudioStep.LWorkVolumeCreate(
            pVolumeApplyBox.IsChecked == true,
            Math.Clamp(PInspectorDecimalRead(pInspectorVolumeValue, 0), LWorkAudio.LWorkVolumeGainLeast, LWorkAudio.LWorkVolumeGainMost))
    };

    public void PInspectorPlanApply(LWorkAudio pInspectorPlan)
    {
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LAudioKind.LAudioKindHighpass)
                ?? LPassband.LPassbandStepCreate(true, false));
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LAudioKind.LAudioKindLowpass)
                ?? LPassband.LPassbandStepCreate(false, false));
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LAudioKind.LAudioKindDenoise)
                ?? LWorkAudioStep.LWorkNoiseCreate(false, 12, -50, false, LGrain.LGrainWhite, 6, 0.5, -38));
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LAudioKind.LAudioKindEqualizer)
                ?? LWorkAudioStep.LWorkEqualizerCreate(false, LWorkEqualizerStep.LWorkBandsCreate()));
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LAudioKind.LAudioKindVolume)
                ?? LWorkAudioStep.LWorkVolumeCreate(false, 0));
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LAudioKind.LAudioKindNormalize)
                ?? LWorkAudioStep.LWorkNormalizeCreate(false, LLeveling.LLevelingLoudness, -21, -2, 6, true));
        PInspectorActiveRaise();
    }

    public void PInspectorMediaReset()
    {
        LWorkAudio pCurrent = PInspectorPersistentRead();
        PInspectorPlanApply(pCurrent);
    }

    public bool PInspectorPersistentCheck() =>
        pInspectorVolumePersistent.IsChecked == true
        || pLoudnessPersistent.IsChecked == true
        || pNoisePersistent.IsChecked == true
        || pInspectorHighPass.PInspectorPassPersistent.IsChecked == true
        || pInspectorLowPass.PInspectorPassPersistent.IsChecked == true
        || pEqualizerPersistent.IsChecked == true
        || pSkipPersistentBox.IsChecked == true;

    public void PInspectorPersistentApply(LWorkAudio pInspectorPlan)
    {
        foreach (LWorkAudioStep pStep in pInspectorPlan.LWorkAudioSteps)
        {
            switch (pStep.LWorkStepKind)
            {
                case LAudioKind.LAudioKindHighpass:
                    pInspectorHighPass.PInspectorPassPersistent.IsChecked = true;
                    break;
                case LAudioKind.LAudioKindLowpass:
                    pInspectorLowPass.PInspectorPassPersistent.IsChecked = true;
                    break;
                case LAudioKind.LAudioKindDenoise:
                    pNoisePersistent.IsChecked = true;
                    break;
                case LAudioKind.LAudioKindVolume:
                    pInspectorVolumePersistent.IsChecked = true;
                    break;
                case LAudioKind.LAudioKindNormalize:
                    pLoudnessPersistent.IsChecked = true;
                    break;
                case LAudioKind.LAudioKindEqualizer:
                    pEqualizerPersistent.IsChecked = true;
                    break;
            }
        }

        pSkipPersistentBox.IsChecked = pInspectorPlan.LWorkAudioSkip;
    }

    public LWorkAudio PInspectorPersistentRead()
    {
        var pSteps = new List<LWorkAudioStep>();
        if (pInspectorHighPass.PInspectorPassPersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LAudioKind.LAudioKindHighpass));
        }

        if (pInspectorLowPass.PInspectorPassPersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LAudioKind.LAudioKindLowpass));
        }

        if (pNoisePersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LAudioKind.LAudioKindDenoise));
        }

        if (pInspectorVolumePersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LAudioKind.LAudioKindVolume));
        }

        if (pLoudnessPersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LAudioKind.LAudioKindNormalize));
        }

        if (pEqualizerPersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LAudioKind.LAudioKindEqualizer));
        }

        return new LWorkAudio(pSteps) { LWorkAudioSkip = pSkipPersistentBox.IsChecked == true };
    }

    private void PInspectorStepApply(LWorkAudioStep pStep)
    {
        switch (pStep)
        {
            case LWorkNormalizeStep pNormalize:
                pLoudnessApplyBox.IsChecked = pNormalize.LWorkStepActive;
                pLoudnessPresetSuppress = true;
                pLoudnessMode.SelectedIndex = pNormalize.LWorkNormalizeMode == LLeveling.LLevelingDynamic ? 1 : 0;
                pLoudnessTarget.Text = pNormalize.LWorkNormalizeTarget.ToString("0.###", CultureInfo.InvariantCulture);
                pLoudnessPeak.Text = pNormalize.LWorkNormalizePeak.ToString("0.###", CultureInfo.InvariantCulture);
                pLoudnessRange.Text = pNormalize.LWorkNormalizeRange.ToString("0.###", CultureInfo.InvariantCulture);
                pLoudnessTwoPass.IsChecked = pNormalize.LWorkTwoPass;
                pDynamicFrame.Text = pNormalize.LWorkNormalizeFrame.ToString("0.###", CultureInfo.InvariantCulture);
                pDynamicGauss.Text = pNormalize.LWorkNormalizeGauss.ToString("0.###", CultureInfo.InvariantCulture);
                pDynamicMaxGain.Text = pNormalize.LWorkNormalizeGain.ToString("0.###", CultureInfo.InvariantCulture);
                pDynamicCompress.Text = pNormalize.LWorkNormalizeCompress.ToString("0.###", CultureInfo.InvariantCulture);
                pLoudnessPresetSuppress = false;
                PLoudnessApplyUpdate();
                PLoudnessModeUpdate();
                break;
            case LWorkNoiseStep pNoise:
                pNoiseApplyBox.IsChecked = pNoise.LWorkStepActive;
                PNoiseValueSet(pNoise);
                pNoiseTrack.IsChecked = pNoise.LWorkNoiseTrack;
                PNoiseApplyUpdate();
                break;
            case LWorkPassStep pPass:
                PFilterActiveSet(pPass.LWorkPassHigh ? pInspectorHighPass : pInspectorLowPass, pPass);
                break;
            case LWorkEqualizerStep pEqualizer:
                PEqualizerActiveSet(pEqualizer);
                break;
            case LWorkVolumeStep pVolume:
                pVolumeApplyBox.IsChecked = pVolume.LWorkStepActive;
                pInspectorVolumeValue.Text = pVolume.LWorkVolumeGain.ToString("0.#", CultureInfo.InvariantCulture);
                pInspectorVolumeSlider.Value = Math.Clamp(pVolume.LWorkVolumeGain, LWorkAudio.LWorkVolumeGainLeast, LWorkAudio.LWorkVolumeGainMost);
                PVolumeWarnUpdate();
                PVolumeApplyUpdate();
                break;
        }
    }

    private StackPanel PVolumeBodyBuild()
    {
        pVolumeApplyBox = PInspectorSwitchBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), LLocalization.LLocalizationTextRead("Inspector.Volume.ApplyTooltip"));
        pVolumeApplyBox.Checked += (_, _) => PVolumeApplyUpdate();
        pVolumeApplyBox.Unchecked += (_, _) => PVolumeApplyUpdate();

        pInspectorVolumePersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Volume.PersistentTooltip"));

        pInspectorVolumeSlider = new Slider
        {
            Minimum = LWorkAudio.LWorkVolumeGainLeast,
            Maximum = LWorkAudio.LWorkVolumeGainMost,
            Value = 0,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pInspectorVolumeSlider);
        PSlider.PSliderResetApply(pInspectorVolumeSlider, static () => 0);

        pInspectorVolumeValue = PInspectorDecimalBuild();
        pInspectorVolumeValue.Text = "0";

        pInspectorVolumeSlider.ValueChanged += (_, _) =>
        {
            if (pInspectorVolumeSuppress)
            {
                return;
            }

            pInspectorVolumeSuppress = true;
            pInspectorVolumeValue.Text = pInspectorVolumeSlider.Value.ToString("0.#", CultureInfo.InvariantCulture);
            pInspectorVolumeSuppress = false;
            PVolumeWarnUpdate();
        };
        pInspectorVolumeValue.TextChanged += (_, _) =>
        {
            if (pInspectorVolumeSuppress)
            {
                return;
            }

            pInspectorVolumeSuppress = true;
            pInspectorVolumeSlider.Value = Math.Clamp(
                PInspectorDecimalRead(pInspectorVolumeValue, 0), LWorkAudio.LWorkVolumeGainLeast, LWorkAudio.LWorkVolumeGainMost);
            pInspectorVolumeSuppress = false;
            PVolumeWarnUpdate();
        };

        var pGainRow = new Grid();
        pGainRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGainRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGainRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGainRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        TextBlock pGainLabel = PInspectorLabelBuild(LLocalization.LLocalizationTextRead("Inspector.Volume.Gain"));
        var pGainUnit = new TextBlock
        {
            Text = "dB",
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 8, 0)
        };

        Grid.SetColumn(pGainLabel, 0);
        Grid.SetColumn(pInspectorVolumeSlider, 1);
        Grid.SetColumn(pGainUnit, 2);
        Grid.SetColumn(pInspectorVolumeValue, 3);
        pGainRow.Children.Add(pGainLabel);
        pGainRow.Children.Add(pInspectorVolumeSlider);
        pGainRow.Children.Add(pGainUnit);
        pGainRow.Children.Add(pInspectorVolumeValue);

        pInspectorVolumeWarn = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Volume.ClipNotice"),
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorWarnBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(PInspectorLabelWidth, 6, 0, 0),
            Visibility = Visibility.Collapsed
        };

        pInspectorVolumeStack = new StackPanel();
        pInspectorVolumeStack.Children.Add(pGainRow);
        pInspectorVolumeStack.Children.Add(pInspectorVolumeWarn);

        pInspectorVolumeBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pInspectorVolumeBody.Children.Add(pVolumeApplyBox);
        pInspectorVolumeBody.Children.Add(PInspectorSeparatorBuild());
        pInspectorVolumeBody.Children.Add(pInspectorVolumeStack);

        PVolumeApplyUpdate();
        return pInspectorVolumeBody;
    }

    private void PVolumeApplyUpdate()
    {
        bool pVolumeActive = pVolumeApplyBox.IsChecked == true;
        pInspectorVolumeStack.IsEnabled = pVolumeActive;
        pInspectorVolumeStack.Opacity = pVolumeActive ? 1 : 0.4;
        PInspectorActiveRaise();
    }

    private void PVolumeWarnUpdate()
    {
        pInspectorVolumeWarn.Visibility = PInspectorDecimalRead(pInspectorVolumeValue, 0) > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static TextBox PInspectorDecimalBuild()
    {
        var pDecimalBox = new TextBox
        {
            Width = PInspectorInsetWidth,
            Height = PInspectorFieldHeight,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PTextbox.PTextboxApply(pDecimalBox);
        pDecimalBox.TextAlignment = TextAlignment.Center;
        pDecimalBox.Padding = new Thickness(4, 0, 4, 0);
        pDecimalBox.PreviewTextInput += (_, pDecimalEvent) =>
            pDecimalEvent.Handled = !pDecimalEvent.Text.All(pChar => char.IsDigit(pChar) || pChar == '.' || pChar == '-');
        return pDecimalBox;
    }

    private static double PInspectorDecimalRead(TextBox pDecimalBox, double pFallback) =>
        double.TryParse(pDecimalBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double pValue)
            ? pValue
            : pFallback;

    private static Slider PInspectorSliderBind(
        TextBox pValueBox,
        double pMin,
        double pMax,
        double pFallback,
        string pFormat,
        Func<double>? pResetRead,
        Action pChanged)
    {
        var pSlider = new Slider
        {
            Minimum = pMin,
            Maximum = pMax,
            Value = Math.Clamp(PInspectorDecimalRead(pValueBox, pFallback), pMin, pMax),
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);
        if (pResetRead is not null)
        {
            PSlider.PSliderResetApply(pSlider, pResetRead);
        }

        bool[] pSuppress = { false };
        pSlider.ValueChanged += (_, _) =>
        {
            if (pSuppress[0]) { return; }
            pSuppress[0] = true;
            pValueBox.Text = pSlider.Value.ToString(pFormat, CultureInfo.InvariantCulture);
            pSuppress[0] = false;
            pChanged();
        };
        pValueBox.TextChanged += (_, _) =>
        {
            if (pSuppress[0]) { return; }
            pSuppress[0] = true;
            pSlider.Value = Math.Clamp(PInspectorDecimalRead(pValueBox, pFallback), pMin, pMax);
            pSuppress[0] = false;
            pChanged();
        };
        return pSlider;
    }
}
