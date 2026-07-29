using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private CheckBox pInspectorBrightnessApply = null!;
    private CheckBox pInspectorBrightnessPersistent = null!;
    private Slider pInspectorBrightnessSlider = null!;
    private TextBox pInspectorBrightnessValue = null!;
    private StackPanel pInspectorBrightnessStack = null!;
    private StackPanel pInspectorBrightnessBody = null!;

    private CheckBox pInspectorContrastApply = null!;
    private CheckBox pInspectorContrastPersistent = null!;
    private Slider pInspectorContrastSlider = null!;
    private TextBox pInspectorContrastValue = null!;
    private StackPanel pInspectorContrastStack = null!;
    private StackPanel pInspectorContrastBody = null!;

    private bool pInspectorVideoSuppress;
    private const double PInspectorBrightnessDefaultMinimum = -100;
    private const double PInspectorBrightnessDefaultMaximum = 100;

    public event Action? PInspectorVideoChange;

    public LWorkVideoStep PInspectorVideoStepRead(LWorkVideoKind pStepKind) => pStepKind switch
    {
        LWorkVideoKind.LWorkVideoKindContrast => LWorkVideoStep.LWorkVideoContrastCreate(
            pInspectorContrastApply.IsChecked == true,
            Math.Clamp(PInspectorDecimalRead(pInspectorContrastValue, 100), 0, 200)),
        _ => LWorkVideoStep.LWorkVideoBrightnessCreate(
            pInspectorBrightnessApply.IsChecked == true,
            PInspectorDecimalRead(pInspectorBrightnessValue, 0))
    };

    public void PInspectorVideoPlanApply(LWorkVideo pVideo)
    {
        PInspectorVideoStepApply(
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkVideoStepKind == LWorkVideoKind.LWorkVideoKindBrightness)
            ?? LWorkVideoStep.LWorkVideoBrightnessCreate(false, 0));
        PInspectorVideoStepApply(
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkVideoStepKind == LWorkVideoKind.LWorkVideoKindContrast)
            ?? LWorkVideoStep.LWorkVideoContrastCreate(false, 100));
        PInspectorVideoChange?.Invoke();
    }

    public bool PInspectorVideoPersistentAnyCheck() =>
        pInspectorBrightnessPersistent.IsChecked == true
        || pInspectorContrastPersistent.IsChecked == true;

    public LWorkVideo PInspectorVideoPersistentRead()
    {
        var pSteps = new List<LWorkVideoStep>();
        if (pInspectorBrightnessPersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorVideoStepRead(LWorkVideoKind.LWorkVideoKindBrightness));
        }

        if (pInspectorContrastPersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorVideoStepRead(LWorkVideoKind.LWorkVideoKindContrast));
        }

        return new LWorkVideo(pSteps);
    }

    private StackPanel PInspectorBrightnessBodyBuild()
    {
        pInspectorBrightnessApply = PInspectorSwitchBuild("Apply", "Apply the brightness adjustment to queued jobs");
        pInspectorBrightnessPersistent = PInspectorSwitchBuild("Persistent", "Apply this brightness setup to every loaded file");
        pInspectorBrightnessSlider = PInspectorVideoSliderBuild(
            PInspectorBrightnessDefaultMinimum,
            PInspectorBrightnessDefaultMaximum,
            0);
        pInspectorBrightnessValue = PInspectorDecimalBoxBuild();
        pInspectorBrightnessValue.Text = "0";
        pInspectorBrightnessStack = new StackPanel();
        PInspectorVideoWire(
            pInspectorBrightnessApply,
            pInspectorBrightnessStack,
            pInspectorBrightnessSlider,
            pInspectorBrightnessValue,
            null,
            null,
            "0.#");
        pInspectorBrightnessStack.Children.Add(PInspectorPassSliderRowBuild("Amount", pInspectorBrightnessSlider, string.Empty, pInspectorBrightnessValue));
        pInspectorBrightnessBody = PInspectorVideoBodyBuild(pInspectorBrightnessApply, pInspectorBrightnessStack);
        PInspectorVideoApplyUpdate(pInspectorBrightnessApply, pInspectorBrightnessStack);
        return pInspectorBrightnessBody;
    }

    private StackPanel PInspectorContrastBodyBuild()
    {
        pInspectorContrastApply = PInspectorSwitchBuild("Apply", "Apply the contrast adjustment to queued jobs");
        pInspectorContrastPersistent = PInspectorSwitchBuild("Persistent", "Apply this contrast setup to every loaded file");
        pInspectorContrastSlider = PInspectorVideoSliderBuild(0, 200, 100);
        pInspectorContrastValue = PInspectorDecimalBoxBuild();
        pInspectorContrastValue.Text = "100";
        pInspectorContrastStack = new StackPanel();
        PInspectorVideoWire(
            pInspectorContrastApply,
            pInspectorContrastStack,
            pInspectorContrastSlider,
            pInspectorContrastValue,
            0,
            200,
            "0.#");
        pInspectorContrastStack.Children.Add(PInspectorPassSliderRowBuild("Amount", pInspectorContrastSlider, "%", pInspectorContrastValue));
        if (!LFlyleafLocal.LFlyleafLocalActive)
        {
            pInspectorContrastStack.Children.Add(new TextBlock
            {
                Text = "Preview requires local Flyleaf. Export still applies contrast.",
                Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x64, 0x70, 0x82)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        pInspectorContrastBody = PInspectorVideoBodyBuild(pInspectorContrastApply, pInspectorContrastStack);
        PInspectorVideoApplyUpdate(pInspectorContrastApply, pInspectorContrastStack);
        return pInspectorContrastBody;
    }

    private static Slider PInspectorVideoSliderBuild(double pMinimum, double pMaximum, double pValue)
    {
        var pSlider = new Slider
        {
            Minimum = pMinimum,
            Maximum = pMaximum,
            Value = pValue,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);
        return pSlider;
    }

    private static StackPanel PInspectorVideoBodyBuild(CheckBox pApply, StackPanel pStack)
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

    private void PInspectorVideoWire(
        CheckBox pApply,
        StackPanel pStack,
        Slider pSlider,
        TextBox pValue,
        double? pMinimum,
        double? pMaximum,
        string pFormat)
    {
        pApply.Checked += (_, _) => PInspectorVideoApplyUpdate(pApply, pStack);
        pApply.Unchecked += (_, _) => PInspectorVideoApplyUpdate(pApply, pStack);
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

    private void PInspectorVideoStepApply(LWorkVideoStep pStep)
    {
        bool pPrevious = pInspectorVideoSuppress;
        pInspectorVideoSuppress = true;
        try
        {
            if (pStep.LWorkVideoStepKind == LWorkVideoKind.LWorkVideoKindContrast)
            {
                pInspectorContrastApply.IsChecked = pStep.LWorkVideoStepActive;
                pInspectorContrastValue.Text = pStep.LWorkVideoStepValue.ToString("0.#", CultureInfo.InvariantCulture);
                pInspectorContrastSlider.Value = Math.Clamp(pStep.LWorkVideoStepValue, 0, 200);
                PInspectorVideoApplyUpdate(pInspectorContrastApply, pInspectorContrastStack);
                return;
            }

            pInspectorBrightnessApply.IsChecked = pStep.LWorkVideoStepActive;
            pInspectorBrightnessValue.Text = pStep.LWorkVideoStepValue.ToString("0.#", CultureInfo.InvariantCulture);
            pInspectorBrightnessSlider.Value = Math.Clamp(
                pStep.LWorkVideoStepValue,
                pInspectorBrightnessSlider.Minimum,
                pInspectorBrightnessSlider.Maximum);
            PInspectorVideoApplyUpdate(pInspectorBrightnessApply, pInspectorBrightnessStack);
        }
        finally
        {
            pInspectorVideoSuppress = pPrevious;
        }
    }

    private void PInspectorVideoApplyUpdate(CheckBox pApply, StackPanel pStack)
    {
        bool pActive = pApply.IsChecked == true;
        pStack.IsEnabled = pActive;
        pStack.Opacity = pActive ? 1 : 0.4;
        PInspectorVideoChange?.Invoke();
    }
}
