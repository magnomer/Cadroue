using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private const double PVolumeLeastDb = -24;
    private const double PVolumeMostDb = 24;

    private CheckBox pVolumeApplyBox = null!;
    private CheckBox pInspectorVolumePersistent = null!;
    private Slider pInspectorVolumeSlider = null!;
    private TextBox pInspectorVolumeValue = null!;
    private StackPanel pInspectorVolumeStack = null!;
    private TextBlock pInspectorVolumeWarn = null!;
    private StackPanel pInspectorVolumeBody = null!;
    private bool pInspectorVolumeSuppress;

    public event Action? PInspectorAudioActiveChange;

    private void PInspectorActiveRaise() => PInspectorAudioActiveChange?.Invoke();

    public LWorkAudioStep PInspectorStepRead(LWorkAudioKind pStepKind) => pStepKind switch
    {
        LWorkAudioKind.LWorkAudioKindNormalize => LWorkAudioStep.LWorkNormalizeCreate(
            pLoudnessApplyBox.IsChecked == true,
            PLoudnessModeRead(),
            PInspectorDecimalRead(pInspectorNormalizeTarget, -16),
            PInspectorDecimalRead(pInspectorNormalizePeak, -1.5),
            PInspectorDecimalRead(pInspectorNormalizeRange, 11),
            pLoudnessTwoPass.IsChecked == true),
        LWorkAudioKind.LWorkAudioKindNoiseReduction => LWorkAudioStep.LWorkNoiseCreate(
            pNoiseApplyBox.IsChecked == true,
            Math.Clamp(PInspectorDecimalRead(pNoiseReductionValue, 12), PNoiseReductionLeast, PNoiseReductionMost),
            PInspectorDecimalRead(pInspectorNoiseFloor, -50),
            pInspectorNoiseTrack.IsChecked == true,
            PNoiseTypeRead(),
            Math.Clamp(PInspectorDecimalRead(pNoiseSmoothValue, 6), PNoiseSmoothLeast, PNoiseSmoothMost),
            Math.Clamp(PInspectorDecimalRead(pInspectorNoiseAdaptivity, 0.5), 0, 1),
            PInspectorDecimalRead(pInspectorNoiseResidual, -38)),
        LWorkAudioKind.LWorkAudioKindHighPass => LWorkAudioStep.LWorkHighCreate(
            pInspectorHighPass.PFilterApplyBox.IsChecked == true,
            PInspectorPassRead(pInspectorHighPass),
            PFilterStagesRead(pInspectorHighPass),
            PFilterPolesRead(pInspectorHighPass),
            PFilterResonanceRead(pInspectorHighPass)),
        LWorkAudioKind.LWorkAudioKindLowPass => LWorkAudioStep.LWorkLowCreate(
            pInspectorLowPass.PFilterApplyBox.IsChecked == true,
            PInspectorPassRead(pInspectorLowPass),
            PFilterStagesRead(pInspectorLowPass),
            PFilterPolesRead(pInspectorLowPass),
            PFilterResonanceRead(pInspectorLowPass)),
        _ => LWorkAudioStep.LWorkVolumeCreate(
            pVolumeApplyBox.IsChecked == true,
            Math.Clamp(PInspectorDecimalRead(pInspectorVolumeValue, 0), PVolumeLeastDb, PVolumeMostDb))
    };

    public void PInspectorPlanApply(LWorkAudio pInspectorPlan)
    {
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkAudioStepKind == LWorkAudioKind.LWorkAudioKindHighPass)
                ?? LWorkAudioStep.LWorkHighCreate(false, 100, 1, 2, 0.707));
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkAudioStepKind == LWorkAudioKind.LWorkAudioKindLowPass)
                ?? LWorkAudioStep.LWorkLowCreate(false, 12000, 1, 2, 0.707));
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkAudioStepKind == LWorkAudioKind.LWorkAudioKindNoiseReduction)
                ?? LWorkAudioStep.LWorkNoiseCreate(false, 12, -50, false, LWorkAudioNoiseType.LWorkAudioNoiseWhite, 6, 0.5, -38));
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkAudioStepKind == LWorkAudioKind.LWorkAudioKindVolume)
                ?? LWorkAudioStep.LWorkVolumeCreate(false, 0));
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkAudioStepKind == LWorkAudioKind.LWorkAudioKindNormalize)
                ?? LWorkAudioStep.LWorkNormalizeCreate(false, LWorkAudioNormalizeMode.LWorkAudioNormalizeLoudness, -16, -1.5, 11, true));
        PInspectorActiveRaise();
    }

    public void PInspectorMediaReset()
    {
        LWorkAudio pCurrent = PInspectorPersistentRead();
        PInspectorPlanApply(pCurrent);
    }

    public bool PInspectorPersistentCheck() =>
        pInspectorVolumePersistent.IsChecked == true
        || pInspectorNormalizePersistent.IsChecked == true
        || pInspectorNoisePersistent.IsChecked == true
        || pInspectorHighPass.PInspectorPassPersistent.IsChecked == true
        || pInspectorLowPass.PInspectorPassPersistent.IsChecked == true;

    public LWorkAudio PInspectorPersistentRead()
    {
        var pSteps = new List<LWorkAudioStep>();
        if (pInspectorHighPass.PInspectorPassPersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LWorkAudioKind.LWorkAudioKindHighPass));
        }

        if (pInspectorLowPass.PInspectorPassPersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LWorkAudioKind.LWorkAudioKindLowPass));
        }

        if (pInspectorNoisePersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LWorkAudioKind.LWorkAudioKindNoiseReduction));
        }

        if (pInspectorVolumePersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LWorkAudioKind.LWorkAudioKindVolume));
        }

        if (pInspectorNormalizePersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LWorkAudioKind.LWorkAudioKindNormalize));
        }

        return new LWorkAudio(pSteps);
    }

    private void PInspectorStepApply(LWorkAudioStep pStep)
    {
        switch (pStep.LWorkAudioStepKind)
        {
            case LWorkAudioKind.LWorkAudioKindNormalize:
                pLoudnessApplyBox.IsChecked = pStep.LWorkAudioStepActive;
                pInspectorNormalizeMode.SelectedIndex = pStep.LWorkAudioStepMode == LWorkAudioNormalizeMode.LWorkAudioNormalizeDynamic ? 1 : 0;
                string pNormalizePreset = PLoudnessPresetResolve(
                    pStep.LWorkAudioStepTarget, pStep.LWorkAudioStepPeak, pStep.LWorkAudioStepRange);
                pInspectorNormalizePreset.SelectedItem = pInspectorNormalizePreset.Items
                    .Cast<object>()
                    .FirstOrDefault(pItem => LLocalizationChoice.LLocalizationChoiceRead(pItem) == pNormalizePreset);
                pInspectorNormalizeTarget.Text = pStep.LWorkAudioStepTarget.ToString("0.###", CultureInfo.InvariantCulture);
                pInspectorNormalizePeak.Text = pStep.LWorkAudioStepPeak.ToString("0.###", CultureInfo.InvariantCulture);
                pInspectorNormalizeRange.Text = pStep.LWorkAudioStepRange.ToString("0.###", CultureInfo.InvariantCulture);
                pLoudnessTwoPass.IsChecked = pStep.LWorkAudioStepTwoPass;
                PLoudnessApplyUpdate();
                PLoudnessModeUpdate();
                break;
            case LWorkAudioKind.LWorkAudioKindNoiseReduction:
                pNoiseApplyBox.IsChecked = pStep.LWorkAudioStepActive;
                pInspectorNoisePreset.SelectedIndex = pInspectorNoisePreset.Items.Count - 1;
                PNoiseValueSet(pStep);
                pInspectorNoiseTrack.IsChecked = pStep.LWorkAudioStepTrackNoise;
                pInspectorNoiseType.SelectedIndex = pStep.LWorkAudioStepNoiseType switch
                {
                    LWorkAudioNoiseType.LWorkAudioNoiseVinyl => 1,
                    LWorkAudioNoiseType.LWorkAudioNoiseShellac => 2,
                    _ => 0
                };
                PNoiseApplyUpdate();
                break;
            case LWorkAudioKind.LWorkAudioKindHighPass:
                PFilterActiveSet(pInspectorHighPass, pStep);
                break;
            case LWorkAudioKind.LWorkAudioKindLowPass:
                PFilterActiveSet(pInspectorLowPass, pStep);
                break;
            default:
                pVolumeApplyBox.IsChecked = pStep.LWorkAudioStepActive;
                pInspectorVolumeValue.Text = pStep.LWorkAudioStepGain.ToString("0.#", CultureInfo.InvariantCulture);
                pInspectorVolumeSlider.Value = Math.Clamp(pStep.LWorkAudioStepGain, PVolumeLeastDb, PVolumeMostDb);
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
            Minimum = PVolumeLeastDb,
            Maximum = PVolumeMostDb,
            Value = 0,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pInspectorVolumeSlider);

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
                PInspectorDecimalRead(pInspectorVolumeValue, 0), PVolumeLeastDb, PVolumeMostDb);
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
}
