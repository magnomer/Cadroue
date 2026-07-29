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

    public LWorkAudioStep PInspectorStepRead(LWorkAudioKind pStepKind) => pStepKind switch
    {
        LWorkAudioKind.LWorkAudioKindNormalize => LWorkAudioStep.LWorkAudioNormalizeCreate(
            pInspectorNormalizeApply.IsChecked == true,
            PInspectorNormalizeModeRead(),
            PInspectorDecimalRead(pInspectorNormalizeTarget, -16),
            PInspectorDecimalRead(pInspectorNormalizePeak, -1.5),
            PInspectorDecimalRead(pInspectorNormalizeRange, 11),
            pInspectorNormalizeTwoPass.IsChecked == true),
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
            Width = 132,
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

        var pGainRow = new StackPanel { Orientation = Orientation.Horizontal };
        pGainRow.Children.Add(PInspectorLabelBuild("Gain"));
        pGainRow.Children.Add(pInspectorVolumeSlider);
        pGainRow.Children.Add(new TextBlock
        {
            Text = "dB",
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 8, 0)
        });
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
