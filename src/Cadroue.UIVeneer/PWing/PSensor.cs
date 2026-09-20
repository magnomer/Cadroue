using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed record PSensorShape(string PSensorLabelKey, string PSensorUnit, string PSensorPattern);

public sealed partial class PInspector
{
    public event Action? PSensorChange;

    private readonly Dictionary<LDetectorKind, PSensorSection> pSensorSections = new();

    public LSensor LSensor => LInspector.LInspectorSensor;

    public LBlank LBlank => LInspector.LInspectorBlank;

    private sealed class PSensorSection
    {
        public required LDetectorKind PSensorKind { get; init; }
        public required CheckBox PSensorApplyBox { get; init; }
        public required StackPanel PSensorStack { get; init; }
        public required StackPanel PSensorBody { get; init; }
        public Slider? PSensorThresholdSlider { get; init; }
        public TextBox? PSensorThreshold { get; init; }
        public TextBlock? PSensorUnit { get; init; }
        public Slider? PSensorMinimumSlider { get; init; }
        public TextBox? PSensorMinimum { get; init; }
        public Slider? PSensorWindowSlider { get; init; }
        public TextBox? PSensorWindow { get; init; }
        public RadioButton? PSensorDiscard { get; init; }
        public RadioButton? PSensorTreat { get; init; }
        public RadioButton? PSensorFast { get; init; }
        public RadioButton? PSensorNormal { get; init; }
        public RadioButton? PSensorFull { get; init; }
        public RadioButton? PSensorLufs { get; init; }
        public RadioButton? PSensorRms { get; init; }
        public ComboBox? PSensorPreset { get; init; }
    }

    public static string PSensorNameRead(LDetectorKind pDetectorKind) => LSensor.LSensorNameRead(pDetectorKind);

    private static LDetectorKind? PSensorKindRead(string? pStepName) => LSensor.LSensorKindRead(pStepName);

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

    private static PSensorShape PSensorShapeRead(LDetectorKind pDetectorKind) => pDetectorKind switch
    {
        LDetectorKind.LDetectorKindBlank => new PSensorShape("Inspector.Detector.BlackRatio", string.Empty, "0.00"),
        LDetectorKind.LDetectorKindScene => new PSensorShape("Inspector.Detector.Sensitivity", string.Empty, "0"),
        LDetectorKind.LDetectorKindStill => new PSensorShape("Inspector.Detector.Tolerance", "%", "0.00"),
        LDetectorKind.LDetectorKindLuminance => new PSensorShape("Inspector.Detector.LuminanceChange", "%", "0"),
        LDetectorKind.LDetectorKindSilence => new PSensorShape("Inspector.Detector.Threshold", "dB", "0"),
        LDetectorKind.LDetectorKindVolume => new PSensorShape("Inspector.Detector.Threshold", "LU", "0"),
        _ => new PSensorShape("Inspector.Detector.Threshold", string.Empty, "0")
    };

    private StackPanel PSensorBuild(LDetectorKind pDetectorKind)
    {
        if (pDetectorKind == LDetectorKind.LDetectorKindBlank)
        {
            PSensorSection pBlank = PBlankBuild();
            pSensorSections[pDetectorKind] = pBlank;
            return pBlank.PSensorBody;
        }

        CheckBox pApply = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Detector.ApplyTooltip"));
        PInspectorSwitchAttach(pApply, pEnabled => LSensor.LSensorEnabledSet(pDetectorKind, pEnabled));
        var pStack = new StackPanel();
        var pBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };

        (string pLabelKey, string pUnit, string pFormat) = PSensorShapeRead(pDetectorKind);
        (Slider pThresholdSlider, TextBox pThresholdValue) = PSensorValueBuild(
            LDetector.LDetectorThresholdRead(pDetectorKind),
            () => LSensor.LSensorStepRead(pDetectorKind).LDetectorStepThreshold,
            pNumber => LSensor.LSensorThresholdSet(pDetectorKind, pNumber));
        (Slider pMinimumSlider, TextBox pMinimumValue) = PSensorValueBuild(
            LDetector.LDetectorMinimumRead(pDetectorKind),
            () => LSensor.LSensorStepRead(pDetectorKind).LDetectorStepMinimum,
            pNumber => LSensor.LSensorMinimumSet(pDetectorKind, pNumber));

        string pMinimumKey = pDetectorKind switch
        {
            LDetectorKind.LDetectorKindScene or LDetectorKind.LDetectorKindStill
                or LDetectorKind.LDetectorKindLuminance or LDetectorKind.LDetectorKindSilence
                or LDetectorKind.LDetectorKindVolume => "Inspector.Detector.Minimal",
            _ => "Inspector.Detector.Minimum"
        };
        Grid pThresholdRow = PFilterSliderBuild(
            LLocalization.LLocalizationTextRead(pLabelKey), pThresholdSlider, pUnit, pThresholdValue);
        pStack.Children.Add(pThresholdRow);
        TextBlock? pThresholdUnit = pThresholdRow.Children.Count > 2
            ? pThresholdRow.Children[2] as TextBlock
            : null;

        Slider? pWindowSlider = null;
        TextBox? pWindowValue = null;
        if (pDetectorKind is LDetectorKind.LDetectorKindLuminance or LDetectorKind.LDetectorKindVolume)
        {
            (pWindowSlider, pWindowValue) = PSensorValueBuild(
                LDetector.LDetectorWindowRead(pDetectorKind),
                () => LSensor.LSensorStepRead(pDetectorKind).LDetectorStepWindow,
                pNumber => LSensor.LSensorWindowSet(pDetectorKind, pNumber));
            pStack.Children.Add(PFilterSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Detector.Window"), pWindowSlider, "s", pWindowValue));
        }

        pStack.Children.Add(PFilterSliderBuild(
            LLocalization.LLocalizationTextRead(pMinimumKey), pMinimumSlider, "s", pMinimumValue));

        (RadioButton? pDiscard, RadioButton? pTreat) = pDetectorKind == LDetectorKind.LDetectorKindStill
            ? PSensorModeBuild(pStack)
            : (null, null);
        (RadioButton? pFast, RadioButton? pNormal, RadioButton? pFull) =
            pDetectorKind == LDetectorKind.LDetectorKindLuminance
                ? PSensorSpeedBuild(pStack)
                : (null, null, null);
        (RadioButton? pLufs, RadioButton? pRms) = pDetectorKind == LDetectorKind.LDetectorKindVolume
            ? PSensorMetricBuild(pStack)
            : (null, null);
        ComboBox? pPreset = LSensor.LSensorPresetCheck(pDetectorKind)
            ? PSensorPresetBuild(pStack, pDetectorKind, pDetectorKind == LDetectorKind.LDetectorKindVolume ? 1 : 0)
            : null;

        var pSection = new PSensorSection
        {
            PSensorKind = pDetectorKind,
            PSensorApplyBox = pApply,
            PSensorStack = pStack,
            PSensorBody = pBody,
            PSensorThresholdSlider = pThresholdSlider,
            PSensorThreshold = pThresholdValue,
            PSensorUnit = pThresholdUnit,
            PSensorMinimumSlider = pMinimumSlider,
            PSensorMinimum = pMinimumValue,
            PSensorWindowSlider = pWindowSlider,
            PSensorWindow = pWindowValue,
            PSensorDiscard = pDiscard,
            PSensorTreat = pTreat,
            PSensorFast = pFast,
            PSensorNormal = pNormal,
            PSensorFull = pFull,
            PSensorLufs = pLufs,
            PSensorRms = pRms,
            PSensorPreset = pPreset
        };

        pBody.Children.Add(pApply);
        pBody.Children.Add(PInspectorSeparatorBuild());
        pBody.Children.Add(pStack);
        pSensorSections[pDetectorKind] = pSection;
        PSensorSectionUpdate(pSection);
        return pBody;
    }

    private (Slider, TextBox) PSensorValueBuild(LDetectorBound pBound, Func<double> pRead, Action<double> pSet)
    {
        var pSlider = new Slider
        {
            Minimum = pBound.LDetectorBoundLeast,
            Maximum = pBound.LDetectorBoundMost,
            Value = Math.Clamp(pRead(), pBound.LDetectorBoundLeast, pBound.LDetectorBoundMost),
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);
        PSlider.PSliderResetApply(pSlider, () => pBound.LDetectorBoundDefault);
        TextBox pValue = PInspectorDecimalBuild();
        PInspectorValueAttach(pSlider, pValue, pBound.LDetectorBoundLeast, pBound.LDetectorBoundMost, pRead, pSet);
        return (pSlider, pValue);
    }

    private void PSensorUpdate()
    {
        foreach (PSensorSection pSection in pSensorSections.Values)
        {
            if (pSection.PSensorKind != LDetectorKind.LDetectorKindBlank)
            {
                PSensorSectionUpdate(pSection);
            }
        }

        PSensorChange?.Invoke();
    }

    private void PSensorSectionUpdate(PSensorSection pSection)
    {
        LDetectorKind pKind = pSection.PSensorKind;
        LDetectorStep pStep = LSensor.LSensorStepRead(pKind);
        string pFormat = PSensorShapeRead(pKind).PSensorPattern;
        PInspectorSwitchUpdate(pSection.PSensorApplyBox, pStep.LDetectorStepEnabled);
        if (pSection.PSensorThresholdSlider is { } pThresholdSlider && pSection.PSensorThreshold is { } pThreshold)
        {
            PInspectorValueUpdate(pThresholdSlider, pThreshold, pStep.LDetectorStepThreshold, pFormat);
        }

        if (pSection.PSensorMinimumSlider is { } pMinimumSlider && pSection.PSensorMinimum is { } pMinimum)
        {
            PInspectorValueUpdate(pMinimumSlider, pMinimum, pStep.LDetectorStepMinimum, "0.0");
        }

        if (pSection.PSensorWindowSlider is { } pWindowSlider && pSection.PSensorWindow is { } pWindow)
        {
            PInspectorValueUpdate(pWindowSlider, pWindow, pStep.LDetectorStepWindow, "0.0");
        }

        PSensorRadioUpdate(pSection.PSensorTreat, LSensor.LSensorMode == LDetectorStillMode.LDetectorStillTreat);
        PSensorRadioUpdate(pSection.PSensorDiscard, LSensor.LSensorMode == LDetectorStillMode.LDetectorStillDiscard);
        PSensorRadioUpdate(pSection.PSensorFast, LSensor.LSensorSpeed == LDetectorLuminanceMode.LDetectorLuminanceFast);
        PSensorRadioUpdate(pSection.PSensorFull, LSensor.LSensorSpeed == LDetectorLuminanceMode.LDetectorLuminanceFull);
        PSensorRadioUpdate(
            pSection.PSensorNormal, LSensor.LSensorSpeed == LDetectorLuminanceMode.LDetectorLuminanceNormal);
        PSensorRadioUpdate(pSection.PSensorRms, LSensor.LSensorMetric == LDetectorMetricMode.LDetectorMetricRms);
        PSensorRadioUpdate(pSection.PSensorLufs, LSensor.LSensorMetric == LDetectorMetricMode.LDetectorMetricLufs);
        if (pSection.PSensorUnit is { } pUnit && pKind == LDetectorKind.LDetectorKindVolume)
        {
            pUnit.Text = LSensor.LSensorMetric == LDetectorMetricMode.LDetectorMetricRms ? "dB" : "LU";
        }

        if (pSection.PSensorPreset is { } pPreset)
        {
            PInspectorPresetUpdate(
                pPreset,
                LSensor.LSensorMatchRead(pKind),
                LSensor.LSensorTokenRead(pKind),
                pToken => PSensorKeyRead(pKind, pToken));
        }

        PInspectorSectionUpdate(pSection.PSensorStack, pStep.LDetectorStepEnabled);
    }

    private static void PSensorRadioUpdate(RadioButton? pRadio, bool pChecked)
    {
        if (pRadio is not null && pChecked && pRadio.IsChecked != true)
        {
            pRadio.IsChecked = true;
        }
    }

    private RadioButton PSensorRadioBuild(string pTextKey, string pGroup, Action pSelect)
    {
        var pRadio = new RadioButton
        {
            Content = LLocalization.LLocalizationTextRead(pTextKey),
            GroupName = pGroup,
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        pRadio.Checked += (_, _) => pSelect();
        return pRadio;
    }

    private (RadioButton, RadioButton) PSensorModeBuild(StackPanel pStack)
    {
        string pGroup = "PSensorStillMode_" + Guid.NewGuid().ToString("N");
        RadioButton pDiscard = PSensorRadioBuild(
            "Inspector.Detector.StillMode.Discard",
            pGroup,
            () => LSensor.LSensorModeSet(LDetectorStillMode.LDetectorStillDiscard));
        RadioButton pTreat = PSensorRadioBuild(
            "Inspector.Detector.StillMode.Treat",
            pGroup,
            () => LSensor.LSensorModeSet(LDetectorStillMode.LDetectorStillTreat));
        Border pModeRow = PRadio.PRadioSegmentBuild(pDiscard, pTreat);
        pStack.Children.Add(PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Detector.StillMode"), pModeRow, true));
        return (pDiscard, pTreat);
    }

    private (RadioButton, RadioButton, RadioButton) PSensorSpeedBuild(StackPanel pStack)
    {
        string pGroup = "PSensorLuminanceMode_" + Guid.NewGuid().ToString("N");
        RadioButton pFast = PSensorRadioBuild(
            "Inspector.Detector.LuminanceMode.Fast",
            pGroup,
            () => LSensor.LSensorSpeedSet(LDetectorLuminanceMode.LDetectorLuminanceFast));
        RadioButton pNormal = PSensorRadioBuild(
            "Inspector.Detector.LuminanceMode.Normal",
            pGroup,
            () => LSensor.LSensorSpeedSet(LDetectorLuminanceMode.LDetectorLuminanceNormal));
        RadioButton pFull = PSensorRadioBuild(
            "Inspector.Detector.LuminanceMode.Full",
            pGroup,
            () => LSensor.LSensorSpeedSet(LDetectorLuminanceMode.LDetectorLuminanceFull));
        Border pModeRow = PRadio.PRadioSegmentBuild(pFast, pNormal, pFull);
        pStack.Children.Insert(0, PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Detector.LuminanceMode"), pModeRow, true));
        return (pFast, pNormal, pFull);
    }
}
