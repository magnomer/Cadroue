using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private const double PInspectorVolumeMinDb = -24;
    private const double PInspectorVolumeMaxDb = 24;

    private CheckBox pInspectorVolumeApply = null!;
    private Slider pInspectorVolumeSlider = null!;
    private TextBox pInspectorVolumeValue = null!;
    private StackPanel pInspectorVolumeStack = null!;
    private TextBlock pInspectorVolumeWarn = null!;
    private StackPanel pInspectorVolumeBody = null!;
    private bool pInspectorVolumeSuppress;

    public event Action? PInspectorAudioActiveChange;

    private void PInspectorAudioActiveRaise() => PInspectorAudioActiveChange?.Invoke();

    public LWorkAudioStep PInspectorStepRead(LWorkAudioKind pStepKind) => pStepKind switch
    {
        LWorkAudioKind.LWorkAudioKindNormalize => LWorkAudioStep.LWorkAudioNormalizeCreate(
            pInspectorNormalizeApply.IsChecked == true,
            PInspectorNormalizeModeRead(),
            PInspectorDecimalRead(pInspectorNormalizeTarget, -16),
            PInspectorDecimalRead(pInspectorNormalizePeak, -1.5),
            PInspectorDecimalRead(pInspectorNormalizeRange, 11),
            pInspectorNormalizeTwoPass.IsChecked == true),
        LWorkAudioKind.LWorkAudioKindNoiseReduction => LWorkAudioStep.LWorkAudioNoiseCreate(
            pInspectorNoiseApply.IsChecked == true,
            Math.Clamp(PInspectorDecimalRead(pInspectorNoiseReductionValue, 12), PInspectorNoiseMinReduction, PInspectorNoiseMaxReduction),
            PInspectorDecimalRead(pInspectorNoiseFloor, -50),
            pInspectorNoiseTrack.IsChecked == true,
            PInspectorNoiseTypeRead(),
            Math.Clamp(PInspectorDecimalRead(pInspectorNoiseSmoothValue, 6), PInspectorNoiseMinSmooth, PInspectorNoiseMaxSmooth),
            Math.Clamp(PInspectorDecimalRead(pInspectorNoiseAdaptivity, 0.5), 0, 1),
            PInspectorDecimalRead(pInspectorNoiseResidual, -38)),
        LWorkAudioKind.LWorkAudioKindHighPass => LWorkAudioStep.LWorkAudioHighPassCreate(
            pInspectorHighPass.PInspectorPassApply.IsChecked == true,
            PInspectorPassRead(pInspectorHighPass),
            PInspectorPassStagesRead(pInspectorHighPass),
            PInspectorPassPolesRead(pInspectorHighPass),
            PInspectorPassResonanceRead(pInspectorHighPass)),
        LWorkAudioKind.LWorkAudioKindLowPass => LWorkAudioStep.LWorkAudioLowPassCreate(
            pInspectorLowPass.PInspectorPassApply.IsChecked == true,
            PInspectorPassRead(pInspectorLowPass),
            PInspectorPassStagesRead(pInspectorLowPass),
            PInspectorPassPolesRead(pInspectorLowPass),
            PInspectorPassResonanceRead(pInspectorLowPass)),
        _ => LWorkAudioStep.LWorkAudioVolumeCreate(
            pInspectorVolumeApply.IsChecked == true,
            Math.Clamp(PInspectorDecimalRead(pInspectorVolumeValue, 0), PInspectorVolumeMinDb, PInspectorVolumeMaxDb))
    };

    private StackPanel PInspectorVolumeBodyBuild()
    {
        pInspectorVolumeApply = PInspectorSwitchBuild("Apply", "Apply the volume change to queued jobs");
        pInspectorVolumeApply.Checked += (_, _) => PInspectorVolumeApplyUpdate();
        pInspectorVolumeApply.Unchecked += (_, _) => PInspectorVolumeApplyUpdate();

        pInspectorVolumeSlider = new Slider
        {
            Minimum = PInspectorVolumeMinDb,
            Maximum = PInspectorVolumeMaxDb,
            Value = 0,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pInspectorVolumeSlider);

        pInspectorVolumeValue = PInspectorDecimalBoxBuild();
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
            PInspectorVolumeWarnUpdate();
        };
        pInspectorVolumeValue.TextChanged += (_, _) =>
        {
            if (pInspectorVolumeSuppress)
            {
                return;
            }

            pInspectorVolumeSuppress = true;
            pInspectorVolumeSlider.Value = Math.Clamp(
                PInspectorDecimalRead(pInspectorVolumeValue, 0), PInspectorVolumeMinDb, PInspectorVolumeMaxDb);
            pInspectorVolumeSuppress = false;
            PInspectorVolumeWarnUpdate();
        };

        var pGainRow = new Grid();
        pGainRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGainRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGainRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGainRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        TextBlock pGainLabel = PInspectorLabelBuild("Gain");
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
            Text = "Positive gain may clip the audio.",
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
        pInspectorVolumeBody.Children.Add(pInspectorVolumeApply);
        pInspectorVolumeBody.Children.Add(PInspectorSeparatorBuild());
        pInspectorVolumeBody.Children.Add(pInspectorVolumeStack);

        PInspectorVolumeApplyUpdate();
        return pInspectorVolumeBody;
    }

    private void PInspectorVolumeApplyUpdate()
    {
        bool pVolumeActive = pInspectorVolumeApply.IsChecked == true;
        pInspectorVolumeStack.IsEnabled = pVolumeActive;
        pInspectorVolumeStack.Opacity = pVolumeActive ? 1 : 0.4;
        PInspectorAudioActiveRaise();
    }

    private void PInspectorVolumeWarnUpdate()
    {
        pInspectorVolumeWarn.Visibility = PInspectorDecimalRead(pInspectorVolumeValue, 0) > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static TextBox PInspectorDecimalBoxBuild()
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
