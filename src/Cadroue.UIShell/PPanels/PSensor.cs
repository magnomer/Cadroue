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
        public required CheckBox PSensorApplyBox { get; init; }
        public required StackPanel PSensorStack { get; init; }
        public required StackPanel PSensorBody { get; init; }
        public TextBox? PSensorThreshold { get; init; }
        public TextBox? PSensorMinimum { get; init; }
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
        LDetectorKind.LDetectorKindScene => ("Inspector.Detector.Sensitivity", string.Empty, "0"),
        LDetectorKind.LDetectorKindStill => ("Inspector.Detector.Noise", "dB", "0"),
        LDetectorKind.LDetectorKindSilence => ("Inspector.Detector.Threshold", "dB", "0"),
        LDetectorKind.LDetectorKindVolume => ("Inspector.Detector.Threshold", "dB", "0"),
        _ => ("Inspector.Detector.Threshold", string.Empty, "0")
    };

    private StackPanel PSensorBuild(LDetectorKind pDetectorKind)
    {
        if (pDetectorKind == LDetectorKind.LDetectorKindBlank)
        {
            return PBlankBuild();
        }

        PSensorSection pSection = null!;
        void pSensorRaise()
        {
            if (!pSection.PSensorSuppress)
            {
                PSensorRaise();
            }
        }

        CheckBox pApply = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Detector.ApplyTooltip"));
        var pStack = new StackPanel();
        var pBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };

        TextBox? pThresholdValue = null;
        TextBox? pMinimumValue = null;
        if (pDetectorKind != LDetectorKind.LDetectorKindLuminance)
        {
            LDetectorBound pThresholdBound = LDetector.LDetectorThresholdRead(pDetectorKind);
            (string pLabelKey, string pUnit, string pFormat) = PSensorShapeRead(pDetectorKind);
            pThresholdValue = PSensorDecimalBuild(pThresholdBound.LDetectorBoundDefault, pFormat);

            LDetectorBound pMinimumBound = LDetector.LDetectorMinimumRead(pDetectorKind);
            pMinimumValue = PSensorDecimalBuild(pMinimumBound.LDetectorBoundDefault, "0.0");

            Slider pThresholdSlider = PInspectorSliderBuild(
                pThresholdValue, pThresholdBound.LDetectorBoundLeast, pThresholdBound.LDetectorBoundMost,
                pThresholdBound.LDetectorBoundDefault, pFormat,
                () => pThresholdBound.LDetectorBoundDefault, pSensorRaise);
            Slider pMinimumSlider = PInspectorSliderBuild(
                pMinimumValue, pMinimumBound.LDetectorBoundLeast, pMinimumBound.LDetectorBoundMost,
                pMinimumBound.LDetectorBoundDefault, "0.0",
                () => pMinimumBound.LDetectorBoundDefault, pSensorRaise);

            string pMinimumKey = pDetectorKind == LDetectorKind.LDetectorKindScene
                ? "Inspector.Detector.Minimal"
                : "Inspector.Detector.Minimum";
            pStack.Children.Add(PFilterSliderBuild(
                LLocalization.LLocalizationTextRead(pLabelKey), pThresholdSlider, pUnit, pThresholdValue));
            pStack.Children.Add(PFilterSliderBuild(
                LLocalization.LLocalizationTextRead(pMinimumKey), pMinimumSlider, "s", pMinimumValue));
        }

        pSection = new PSensorSection
        {
            PSensorKind = pDetectorKind,
            PSensorApplyBox = pApply,
            PSensorStack = pStack,
            PSensorBody = pBody,
            PSensorThreshold = pThresholdValue,
            PSensorMinimum = pMinimumValue
        };

        pApply.Checked += (_, _) => PSensorApplyHandle(pSection);
        pApply.Unchecked += (_, _) => PSensorApplyHandle(pSection);

        pBody.Children.Add(pApply);
        pBody.Children.Add(PInspectorSeparatorBuild());
        pBody.Children.Add(pStack);
        PSensorStackUpdate(pSection);

        pSensorSections[pDetectorKind] = pSection;
        return pBody;
    }

    private void PSensorApplyHandle(PSensorSection pSection)
    {
        PSensorStackUpdate(pSection);
        if (!pSection.PSensorSuppress)
        {
            PSensorRaise();
        }
    }

    private static void PSensorStackUpdate(PSensorSection pSection)
    {
        bool pActive = pSection.PSensorApplyBox.IsChecked == true;
        pSection.PSensorStack.IsEnabled = pActive;
        pSection.PSensorStack.Opacity = pActive ? 1 : 0.4;
    }

    private static double PSensorThresholdClamp(LDetectorKind pDetectorKind, double pValue) =>
        pDetectorKind == LDetectorKind.LDetectorKindScene
            ? LDetector.LDetectorSensitivityClamp(pValue)
            : LDetector.LDetectorThresholdClamp(pDetectorKind, pValue);

    private static TextBox PSensorDecimalBuild(double pDefault, string pFormat)
    {
        TextBox pDecimalBox = PInspectorDecimalBuild();
        pDecimalBox.Text = pDefault.ToString(pFormat, CultureInfo.InvariantCulture);
        return pDecimalBox;
    }

    public LDetectorStep PSensorStepRead(LDetectorKind pDetectorKind)
    {
        if (!pSensorSections.TryGetValue(pDetectorKind, out PSensorSection? pSection))
        {
            return LDetector.LDetectorCreate(pDetectorKind);
        }

        LDetectorStep pDefault = LDetector.LDetectorCreate(pDetectorKind);
        return new LDetectorStep(
            pDetectorKind,
            pSection.PSensorApplyBox.IsChecked == true,
            pSection.PSensorThreshold is { } pThreshold
                ? PSensorThresholdClamp(pDetectorKind, PInspectorDecimalRead(pThreshold, pDefault.LDetectorStepThreshold))
                : pDefault.LDetectorStepThreshold,
            pSection.PSensorMinimum is { } pMinimum
                ? LDetector.LDetectorMinimumClamp(pDetectorKind, PInspectorDecimalRead(pMinimum, pDefault.LDetectorStepMinimum))
                : pDefault.LDetectorStepMinimum);
    }

    public void PSensorApply(LDetectorStep pDetectorStep)
    {
        if (!pSensorSections.TryGetValue(pDetectorStep.LDetectorStepKind, out PSensorSection? pSection))
        {
            return;
        }

        pSection.PSensorSuppress = true;
        pSection.PSensorApplyBox.IsChecked = pDetectorStep.LDetectorStepEnabled;
        if (pSection.PSensorThreshold is { } pThreshold)
        {
            pThreshold.Text = PSensorThresholdClamp(
                    pDetectorStep.LDetectorStepKind, pDetectorStep.LDetectorStepThreshold)
                .ToString(PSensorShapeRead(pDetectorStep.LDetectorStepKind).Format, CultureInfo.InvariantCulture);
        }

        if (pSection.PSensorMinimum is { } pMinimum)
        {
            pMinimum.Text = LDetector
                .LDetectorMinimumClamp(pDetectorStep.LDetectorStepKind, pDetectorStep.LDetectorStepMinimum)
                .ToString("0.0", CultureInfo.InvariantCulture);
        }

        pSection.PSensorSuppress = false;
        PSensorStackUpdate(pSection);
    }
}
