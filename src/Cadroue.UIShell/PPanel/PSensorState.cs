using System.Globalization;
using System.Windows.Controls;
using Cadroue.Core;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    private static double PSensorThresholdClamp(LDetectorKind pDetectorKind, double pValue) =>
        pDetectorKind == LDetectorKind.LDetectorKindScene
            ? LDetector.LDetectorSensitivityClamp(pValue)
            : LDetector.LDetectorThresholdClamp(pDetectorKind, pValue);


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
                ? PSensorThresholdClamp(
                    pDetectorKind,
                    PInspectorDecimalRead(pThreshold, pDefault.LDetectorStepThreshold))
                : pDefault.LDetectorStepThreshold,
            pSection.PSensorMinimum is { } pMinimum
                ? LDetector.LDetectorMinimumClamp(
                    pDetectorKind,
                    PInspectorDecimalRead(pMinimum, pDefault.LDetectorStepMinimum))
                : pDefault.LDetectorStepMinimum,
            pSection.PSensorWindow is { } pWindow
                ? LDetector.LDetectorWindowClamp(
                    pDetectorKind,
                    PInspectorDecimalRead(pWindow, pDefault.LDetectorStepWindow))
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
                .ToString(
                    PSensorShapeRead(pDetectorStep.LDetectorStepKind).PSensorPattern,
                    CultureInfo.InvariantCulture);
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
