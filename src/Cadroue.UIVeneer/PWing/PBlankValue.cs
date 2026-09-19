using System.Windows;
using Cadroue.Core;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    public LDetectorBlank PBlankRead() => LBlank.LBlankStep;

    public void PBlankApply(LDetectorBlank pBlankStep) => LBlank.LBlankStepSet(pBlankStep);

    public void PBlankSampleApply(int pBlankRed, int pBlankGreen, int pBlankBlue) =>
        LBlank.LBlankSampleSet(pBlankRed, pBlankGreen, pBlankBlue);

    private void PBlankUpdate()
    {
        LDetectorBlank pBlank = LBlank.LBlankStep;
        PSensorSection pSection = pSensorSections[LDetectorKind.LDetectorKindBlank];
        PInspectorSwitchUpdate(pSection.PSensorApplyBox, pBlank.LDetectorBlankEnabled, false);
        bool pColor = pBlank.LDetectorBlankType == LDetectorType.LDetectorTypeColor;
        PSensorRadioUpdate(pBlankColor, pColor);
        PSensorRadioUpdate(pBlankBlack, !pColor);
        pBlankColorArea.Visibility = pColor ? Visibility.Visible : Visibility.Collapsed;
        PInspectorValueUpdate(pBlankBrightnessSlider, pBlankBrightnessValue, pBlank.LDetectorBlankBrightness, "0.00");
        PInspectorValueUpdate(pBlankToleranceSlider, pBlankToleranceValue, pBlank.LDetectorBlankTolerance, "0.00");
        PInspectorValueUpdate(pBlankCoverageSlider, pBlankCoverageValue, pBlank.LDetectorBlankCoverage, "0.00");
        PInspectorValueUpdate(pBlankMinimumSlider, pBlankMinimumValue, pBlank.LDetectorBlankMinimum, "0.0");
        PBlankWheelPlace();
        PInspectorSectionUpdate(pSection.PSensorStack, pBlank.LDetectorBlankEnabled);
        PSensorChange?.Invoke();
    }
}
