using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    public event Action? PSensorChange;

    private const string PSensorBlank = "Blank";
    private const string PSensorScene = "Scene";
    private const string PSensorStill = "Still Image";
    private const string PSensorLuminance = "Luminance";
    private const string PSensorSilence = "Silence";
    private const string PSensorVolume = "Volume";

    private readonly Dictionary<LDetectorKind, PSensorSection> pSensorSections = new();

    private sealed class PSensorSection
    {
        public required LDetectorKind PSensorKind { get; init; }
        public required TextBox PSensorThreshold { get; init; }
        public required TextBox PSensorMinimum { get; init; }
        public required StackPanel PSensorBody { get; init; }
        public bool PSensorSuppress { get; set; }
    }

    private void PSensorRaise() => PSensorChange?.Invoke();

    public static string PSensorNameRead(LDetectorKind pDetectorKind) => pDetectorKind switch
    {
        LDetectorKind.LDetectorKindBlank => PSensorBlank,
        LDetectorKind.LDetectorKindScene => PSensorScene,
        LDetectorKind.LDetectorKindStill => PSensorStill,
        LDetectorKind.LDetectorKindLuminance => PSensorLuminance,
        LDetectorKind.LDetectorKindSilence => PSensorSilence,
        LDetectorKind.LDetectorKindVolume => PSensorVolume,
        _ => string.Empty
    };

    private static LDetectorKind? PSensorKindRead(string? pStepName) => pStepName switch
    {
        PSensorBlank => LDetectorKind.LDetectorKindBlank,
        PSensorScene => LDetectorKind.LDetectorKindScene,
        PSensorStill => LDetectorKind.LDetectorKindStill,
        PSensorLuminance => LDetectorKind.LDetectorKindLuminance,
        PSensorSilence => LDetectorKind.LDetectorKindSilence,
        PSensorVolume => LDetectorKind.LDetectorKindVolume,
        _ => null
    };

    private static string PSensorTitleRead(LDetectorKind pDetectorKind) => pDetectorKind switch
    {
        LDetectorKind.LDetectorKindBlank => "Inspector.Step.Blank",
        LDetectorKind.LDetectorKindScene => "Inspector.Step.Scene",
        LDetectorKind.LDetectorKindStill => "Inspector.Step.Still",
        LDetectorKind.LDetectorKindLuminance => "Inspector.Step.Luminance",
        LDetectorKind.LDetectorKindSilence => "Inspector.Step.Silence",
        LDetectorKind.LDetectorKindVolume => "Inspector.Step.Volume",
        _ => "Inspector.Header.Title"
    };

    private static (string LabelKey, string Unit, string Format) PSensorShapeRead(LDetectorKind pDetectorKind) => pDetectorKind switch
    {
        LDetectorKind.LDetectorKindBlank => ("Inspector.Detector.BlackRatio", string.Empty, "0.00"),
        LDetectorKind.LDetectorKindScene => ("Inspector.Detector.Sensitivity", string.Empty, "0.00"),
        LDetectorKind.LDetectorKindStill => ("Inspector.Detector.Noise", "dB", "0"),
        LDetectorKind.LDetectorKindSilence => ("Inspector.Detector.Threshold", "dB", "0"),
        LDetectorKind.LDetectorKindVolume => ("Inspector.Detector.Threshold", "dB", "0"),
        _ => ("Inspector.Detector.Threshold", string.Empty, "0")
    };

    private StackPanel PSensorBuild(LDetectorKind pDetectorKind)
    {
        LDetectorBound pThresholdBound = LDetector.LDetectorThresholdRead(pDetectorKind);
        LDetectorBound pMinimumBound = LDetector.LDetectorMinimumRead(pDetectorKind);
        (string pLabelKey, string pUnit, string pFormat) = PSensorShapeRead(pDetectorKind);

        TextBox pThresholdValue = PSensorDecimalBuild(pThresholdBound.LDetectorBoundDefault, pFormat);
        TextBox pMinimumValue = PSensorDecimalBuild(pMinimumBound.LDetectorBoundDefault, "0.0");

        var pSection = new PSensorSection
        {
            PSensorKind = pDetectorKind,
            PSensorThreshold = pThresholdValue,
            PSensorMinimum = pMinimumValue,
            PSensorBody = new StackPanel
            {
                Margin = new Thickness(12, 12, 12, 12),
                Visibility = Visibility.Collapsed
            }
        };

        Slider pThresholdSlider = PInspectorSliderBuild(
            pThresholdValue,
            pThresholdBound.LDetectorBoundLeast,
            pThresholdBound.LDetectorBoundMost,
            pThresholdBound.LDetectorBoundDefault,
            pFormat,
            () => pThresholdBound.LDetectorBoundDefault,
            () => PSensorChangeHandle(pSection));
        Slider pMinimumSlider = PInspectorSliderBuild(
            pMinimumValue,
            pMinimumBound.LDetectorBoundLeast,
            pMinimumBound.LDetectorBoundMost,
            pMinimumBound.LDetectorBoundDefault,
            "0.0",
            () => pMinimumBound.LDetectorBoundDefault,
            () => PSensorChangeHandle(pSection));

        pSection.PSensorBody.Children.Add(PFilterSliderBuild(
            LLocalization.LLocalizationTextRead(pLabelKey), pThresholdSlider, pUnit, pThresholdValue));
        pSection.PSensorBody.Children.Add(PFilterSliderBuild(
            LLocalization.LLocalizationTextRead("Inspector.Detector.Minimum"), pMinimumSlider, "s", pMinimumValue));

        pSensorSections[pDetectorKind] = pSection;
        return pSection.PSensorBody;
    }

    private void PSensorChangeHandle(PSensorSection pSection)
    {
        if (pSection.PSensorSuppress)
        {
            return;
        }

        PSensorRaise();
    }

    private static TextBox PSensorDecimalBuild(double pDefault, string pFormat)
    {
        TextBox pDecimalBox = PInspectorDecimalBuild();
        pDecimalBox.Text = pDefault.ToString(pFormat, CultureInfo.InvariantCulture);
        return pDecimalBox;
    }

    public LDetectorStep PSensorStepRead(LDetectorKind pDetectorKind, bool pEnabled)
    {
        if (!pSensorSections.TryGetValue(pDetectorKind, out PSensorSection? pSection))
        {
            LDetectorStep pDefault = LDetector.LDetectorCreate(pDetectorKind);
            return pDefault with { LDetectorStepEnabled = pEnabled };
        }

        return new LDetectorStep(
            pDetectorKind,
            pEnabled,
            LDetector.LDetectorThresholdClamp(pDetectorKind, PInspectorDecimalRead(pSection.PSensorThreshold, 0)),
            LDetector.LDetectorMinimumClamp(pDetectorKind, PInspectorDecimalRead(pSection.PSensorMinimum, 0)));
    }

    public void PSensorApply(LDetectorStep pDetectorStep)
    {
        if (!pSensorSections.TryGetValue(pDetectorStep.LDetectorStepKind, out PSensorSection? pSection))
        {
            return;
        }

        pSection.PSensorSuppress = true;
        pSection.PSensorThreshold.Text = LDetector
            .LDetectorThresholdClamp(pDetectorStep.LDetectorStepKind, pDetectorStep.LDetectorStepThreshold)
            .ToString(PSensorShapeRead(pDetectorStep.LDetectorStepKind).Format, CultureInfo.InvariantCulture);
        pSection.PSensorMinimum.Text = LDetector
            .LDetectorMinimumClamp(pDetectorStep.LDetectorStepKind, pDetectorStep.LDetectorStepMinimum)
            .ToString("0.0", CultureInfo.InvariantCulture);
        pSection.PSensorSuppress = false;
    }
}
