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
        public TextBox? PSensorWindow { get; init; }
        public RadioButton? PSensorMode { get; init; }
        public RadioButton? PSensorFast { get; init; }
        public RadioButton? PSensorNormal { get; init; }
        public RadioButton? PSensorFull { get; init; }
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
        LDetectorKind.LDetectorKindStill => ("Inspector.Detector.Tolerance", "%", "0.00"),
        LDetectorKind.LDetectorKindLuminance => ("Inspector.Detector.LuminanceChange", "%", "0"),
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
        TextBox? pWindowValue = null;
        RadioButton? pModeTreat = null;
        RadioButton? pLuminanceFast = null;
        RadioButton? pLuminanceNormal = null;
        RadioButton? pLuminanceFull = null;
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

        string pMinimumKey = pDetectorKind switch
        {
            LDetectorKind.LDetectorKindScene or LDetectorKind.LDetectorKindStill
                or LDetectorKind.LDetectorKindLuminance or LDetectorKind.LDetectorKindSilence => "Inspector.Detector.Minimal",
            _ => "Inspector.Detector.Minimum"
        };
        pStack.Children.Add(PFilterSliderBuild(
            LLocalization.LLocalizationTextRead(pLabelKey), pThresholdSlider, pUnit, pThresholdValue));

        if (pDetectorKind == LDetectorKind.LDetectorKindLuminance)
        {
            LDetectorBound pWindowBound = LDetector.LDetectorWindowRead(pDetectorKind);
            pWindowValue = PSensorDecimalBuild(pWindowBound.LDetectorBoundDefault, "0.0");
            Slider pWindowSlider = PInspectorSliderBuild(
                pWindowValue, pWindowBound.LDetectorBoundLeast, pWindowBound.LDetectorBoundMost,
                pWindowBound.LDetectorBoundDefault, "0.0",
                () => pWindowBound.LDetectorBoundDefault, pSensorRaise);
            pStack.Children.Add(PFilterSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Detector.ComparisonWindow"), pWindowSlider, "s", pWindowValue));
        }

        pStack.Children.Add(PFilterSliderBuild(
            LLocalization.LLocalizationTextRead(pMinimumKey), pMinimumSlider, "s", pMinimumValue));

        if (pDetectorKind == LDetectorKind.LDetectorKindStill)
        {
            pModeTreat = PSensorModeBuild(pStack, pSensorRaise);
        }

        if (pDetectorKind == LDetectorKind.LDetectorKindLuminance)
        {
            (pLuminanceFast, pLuminanceNormal, pLuminanceFull) =
                PSensorSpeedBuild(pStack, pSensorRaise);
        }

        pSection = new PSensorSection
        {
            PSensorKind = pDetectorKind,
            PSensorApplyBox = pApply,
            PSensorStack = pStack,
            PSensorBody = pBody,
            PSensorThreshold = pThresholdValue,
            PSensorMinimum = pMinimumValue,
            PSensorWindow = pWindowValue,
            PSensorMode = pModeTreat,
            PSensorFast = pLuminanceFast,
            PSensorNormal = pLuminanceNormal,
            PSensorFull = pLuminanceFull
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

    private RadioButton PSensorModeBuild(StackPanel pStack, Action pSensorRaise)
    {
        string pModeGroup = "PSensorStillMode_" + System.Guid.NewGuid().ToString("N");
        var pModeDiscard = new RadioButton
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Detector.StillMode.Discard"),
            GroupName = pModeGroup,
            IsChecked = true,
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        var pModeTreat = new RadioButton
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Detector.StillMode.Treat"),
            GroupName = pModeGroup,
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        pModeDiscard.Checked += (_, _) => pSensorRaise();
        pModeTreat.Checked += (_, _) => pSensorRaise();

        Border pModeRow = PRadio.PRadioSegmentBuild(pModeDiscard, pModeTreat);
        pStack.Children.Add(PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Detector.StillMode"), pModeRow, true));
        return pModeTreat;
    }

    public LDetectorStillMode PSensorModeRead(LDetectorKind pDetectorKind)
    {
        if (pSensorSections.TryGetValue(pDetectorKind, out PSensorSection? pSection)
            && pSection.PSensorMode is { IsChecked: true })
        {
            return LDetectorStillMode.LDetectorStillTreat;
        }

        return LDetectorStillMode.LDetectorStillDiscard;
    }

    public void PSensorModeApply(LDetectorKind pDetectorKind, LDetectorStillMode pDetectorMode)
    {
        if (!pSensorSections.TryGetValue(pDetectorKind, out PSensorSection? pSection)
            || pSection.PSensorMode is not { } pModeTreat)
        {
            return;
        }

        pSection.PSensorSuppress = true;
        if (pDetectorMode == LDetectorStillMode.LDetectorStillTreat)
        {
            pModeTreat.IsChecked = true;
        }
        else if (pModeTreat.Parent is Panel pModeRow && pModeRow.Children[0] is RadioButton pModeDiscard)
        {
            pModeDiscard.IsChecked = true;
        }

        pSection.PSensorSuppress = false;
    }

    private (RadioButton Fast, RadioButton Normal, RadioButton Full) PSensorSpeedBuild(
        StackPanel pStack, Action pSensorRaise)
    {
        string pModeGroup = "PSensorLuminanceMode_" + System.Guid.NewGuid().ToString("N");
        RadioButton pModeButtonBuild(string pTextKey, bool pChecked) => new()
        {
            Content = LLocalization.LLocalizationTextRead(pTextKey),
            GroupName = pModeGroup,
            IsChecked = pChecked,
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        RadioButton pFast = pModeButtonBuild("Inspector.Detector.LuminanceMode.Fast", false);
        RadioButton pNormal = pModeButtonBuild("Inspector.Detector.LuminanceMode.Normal", true);
        RadioButton pFull = pModeButtonBuild("Inspector.Detector.LuminanceMode.Full", false);
        pFast.Checked += (_, _) => pSensorRaise();
        pNormal.Checked += (_, _) => pSensorRaise();
        pFull.Checked += (_, _) => pSensorRaise();

        Border pModeRow = PRadio.PRadioSegmentBuild(pFast, pNormal, pFull);
        pStack.Children.Insert(0, PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Detector.LuminanceMode"), pModeRow, true));
        return (pFast, pNormal, pFull);
    }

    public LDetectorLuminanceMode PSensorSpeedRead(LDetectorKind pDetectorKind)
    {
        if (!pSensorSections.TryGetValue(pDetectorKind, out PSensorSection? pSection))
        {
            return LDetectorLuminanceMode.LDetectorLuminanceNormal;
        }

        if (pSection.PSensorFast is { IsChecked: true })
        {
            return LDetectorLuminanceMode.LDetectorLuminanceFast;
        }

        return pSection.PSensorFull is { IsChecked: true }
            ? LDetectorLuminanceMode.LDetectorLuminanceFull
            : LDetectorLuminanceMode.LDetectorLuminanceNormal;
    }

    public void PSensorSpeedApply(LDetectorKind pDetectorKind, LDetectorLuminanceMode pDetectorMode)
    {
        if (!pSensorSections.TryGetValue(pDetectorKind, out PSensorSection? pSection)
            || pSection.PSensorNormal is not { } pNormal
            || pSection.PSensorFast is not { } pFast
            || pSection.PSensorFull is not { } pFull)
        {
            return;
        }

        pSection.PSensorSuppress = true;
        switch (pDetectorMode)
        {
            case LDetectorLuminanceMode.LDetectorLuminanceFast:
                pFast.IsChecked = true;
                break;
            case LDetectorLuminanceMode.LDetectorLuminanceFull:
                pFull.IsChecked = true;
                break;
            default:
                pNormal.IsChecked = true;
                break;
        }

        pSection.PSensorSuppress = false;
    }

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
                : pDefault.LDetectorStepMinimum,
            pSection.PSensorWindow is { } pWindow
                ? LDetector.LDetectorWindowClamp(pDetectorKind, PInspectorDecimalRead(pWindow, pDefault.LDetectorStepWindow))
                : pDefault.LDetectorStepWindow);
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

        if (pSection.PSensorWindow is { } pWindow)
        {
            pWindow.Text = LDetector
                .LDetectorWindowClamp(pDetectorStep.LDetectorStepKind, pDetectorStep.LDetectorStepWindow)
                .ToString("0.0", CultureInfo.InvariantCulture);
        }

        pSection.PSensorSuppress = false;
        PSensorStackUpdate(pSection);
    }
}
