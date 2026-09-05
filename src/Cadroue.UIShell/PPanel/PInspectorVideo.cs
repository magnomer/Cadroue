using System.Globalization;
using Cadroue.Core;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    private bool pInspectorVideoSuppress;

    public event Action? PInspectorVideoChange;

    public LWorkVideoStep PToneStepRead(LColorKind pStepKind) => pStepKind switch
    {
        LColorKind.LColorKindContrast => LWorkVideoStep.LWorkContrastCreate(
            pToneContrastBox.IsChecked == true,
            PInspectorDecimalRead(pInspectorContrastValue, 100)),
        LColorKind.LColorKindSaturation => LWorkVideoStep.LWorkSaturationCreate(
            pToneSaturationBox.IsChecked == true,
            PInspectorDecimalRead(pInspectorSaturationValue, 100)),
        LColorKind.LColorKindGamma => LWorkVideoStep.LWorkGammaCreate(
            pGammaBox.IsChecked == true,
            PInspectorDecimalRead(pGammaValue, 0),
            PInspectorDecimalRead(pGammaRedValue, 0),
            PInspectorDecimalRead(pGammaGreenValue, 0),
            PInspectorDecimalRead(pGammaBlueValue, 0),
            PInspectorDecimalRead(pGammaHighlightValue, 0)),
        LColorKind.LColorKindWhitebalance => LWorkVideoStep.LWorkWhitebalanceCreate(
            pWhitebalanceBox.IsChecked == true,
            PWhitebalanceMethodRead(),
            PInspectorDecimalRead(pWhitebalanceSaturationValue, 100),
            pWhitebalanceRedGain,
            pWhitebalanceGreenGain,
            pWhitebalanceBlueGain,
            pWhitebalanceSampleRed,
            pWhitebalanceSampleGreen,
            pWhitebalanceSampleBlue),
        LColorKind.LColorKindExposure => LWorkVideoStep.LWorkExposureCreate(
            pExposureBox.IsChecked == true,
            PInspectorDecimalRead(pExposureValue, 0)),
        LColorKind.LColorKindCurve => LWorkVideoStep.LWorkCurveCreate(
            pCurveBox.IsChecked == true,
            pCurveChannels[0],
            pCurveChannels[1],
            pCurveChannels[2],
            pCurveChannels[3]),
        _ => LWorkVideoStep.LWorkBrightnessCreate(
            pToneBrightnessBox.IsChecked == true,
            PInspectorDecimalRead(pInspectorBrightnessValue, 0))
    };

    public void PTonePlanApply(LWorkVideo pVideo)
    {
        PToneStepApply(
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindBrightness)
            ?? LWorkVideoStep.LWorkBrightnessCreate(false, 0));
        PToneStepApply(
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindContrast)
            ?? LWorkVideoStep.LWorkContrastCreate(false, 100));
        PToneStepApply(
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindSaturation)
            ?? LWorkVideoStep.LWorkSaturationCreate(false, 100));
        PToneStepApply(
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindGamma)
            ?? LWorkVideoStep.LWorkGammaCreate(false, 0));
        PToneStepApply(
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindWhitebalance)
            ?? LWorkVideoStep.LWorkWhitebalanceCreate(false));
        PToneStepApply(
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindExposure)
            ?? LWorkVideoStep.LWorkExposureCreate(false, 0));
        PToneStepApply(
            pVideo.LWorkVideoSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LColorKind.LColorKindCurve)
            ?? LWorkVideoStep.LWorkCurveCreate(false));
        PInspectorVideoChange?.Invoke();
    }

    private void PToneStepApply(LWorkVideoStep pStep)
    {
        bool pPrevious = pInspectorVideoSuppress;
        pInspectorVideoSuppress = true;
        try
        {
            if (pStep.LWorkStepKind == LColorKind.LColorKindContrast)
            {
                pToneContrastBox.IsChecked = pStep.LWorkStepActive;
                pInspectorContrastValue.Text = pStep.LWorkStepValue.ToString("0.#", CultureInfo.InvariantCulture);
                pInspectorContrastSlider.Value = Math.Clamp(pStep.LWorkStepValue, 0, 200);
                PToneApplyUpdate(pToneContrastBox, pInspectorContrastStack);
                return;
            }

            if (pStep.LWorkStepKind == LColorKind.LColorKindSaturation)
            {
                pToneSaturationBox.IsChecked = pStep.LWorkStepActive;
                pInspectorSaturationValue.Text = pStep.LWorkStepValue.ToString("0.#", CultureInfo.InvariantCulture);
                pInspectorSaturationSlider.Value = Math.Clamp(pStep.LWorkStepValue, 0, 200);
                PToneApplyUpdate(pToneSaturationBox, pInspectorSaturationStack);
                return;
            }

            if (pStep.LWorkStepKind == LColorKind.LColorKindGamma)
            {
                LWorkGammaSettings pGamma = pStep.LWorkGammaRead();
                pGammaBox.IsChecked = pStep.LWorkStepActive;
                PInspectorValueSet(pGammaSlider, pGammaValue, pGamma.LWorkGammaGlobal);
                PInspectorValueSet(pGammaRedSlider, pGammaRedValue, pGamma.LWorkGammaRed);
                PInspectorValueSet(pGammaGreenSlider, pGammaGreenValue, pGamma.LWorkGammaGreen);
                PInspectorValueSet(pGammaBlueSlider, pGammaBlueValue, pGamma.LWorkGammaBlue);
                PInspectorValueSet(
                    pGammaHighlightSlider,
                    pGammaHighlightValue,
                    pGamma.LWorkGammaHighlight);
                PToneApplyUpdate(pGammaBox, pGammaStack);
                PGammaCapabilitySet(pGammaCapable, pGammaPreview, pGammaDisabledKey);
                return;
            }

            if (pStep.LWorkStepKind == LColorKind.LColorKindWhitebalance)
            {
                LWorkWhitebalanceSettings pWhitebalance = pStep.LWorkWhitebalanceRead();
                pWhitebalanceBox.IsChecked = pStep.LWorkStepActive;
                pWhitebalanceManual =
                    pWhitebalance.LWorkWhitebalanceMethod == LWhitebalanceMethod.LWhitebalanceMethodManual;
                PToneNeutralRestore(pWhitebalance);
                pWhitebalanceMethod.SelectedIndex = PWhitebalanceIndexRead(
                    pWhitebalance.LWorkWhitebalanceMethod);
                PWhitebalanceManualUpdate();
                PInspectorValueSet(
                    pWhitebalanceSaturationSlider,
                    pWhitebalanceSaturationValue,
                    pWhitebalance.LWorkWhitebalanceSaturation);
                PToneApplyUpdate(pWhitebalanceBox, pWhitebalanceStack);
                PWhitebalanceCapabilitySet(pWhitebalanceCapable, pWhitebalancePreview);
                return;
            }

            if (pStep.LWorkStepKind == LColorKind.LColorKindExposure)
            {
                pExposureBox.IsChecked = pStep.LWorkStepActive;
                pExposureValue.Text = pStep.LWorkStepValue.ToString("0.#", CultureInfo.InvariantCulture);
                pExposureSlider.Value = Math.Clamp(pStep.LWorkStepValue, -3, 3);
                PToneApplyUpdate(pExposureBox, pExposureStack);
                PExposureCapabilitySet(pExposureCapable, pExposurePreview);
                return;
            }

            if (pStep.LWorkStepKind == LColorKind.LColorKindCurve)
            {
                LWorkCurveSettings pCurve = pStep.LWorkCurveRead();
                pCurveBox.IsChecked = pStep.LWorkStepActive;
                pCurveChannels[0] = pCurve.LWorkCurveMaster.ToList();
                pCurveChannels[1] = pCurve.LWorkCurveRed.ToList();
                pCurveChannels[2] = pCurve.LWorkCurveGreen.ToList();
                pCurveChannels[3] = pCurve.LWorkCurveBlue.ToList();
                pCurveSelected = PCurveActiveRead().Count - 1;
                PCurveBoxesUpdate();
                PToneApplyUpdate(pCurveBox, pCurveStack);
                PCurveCapabilitySet(pCurveCapable, pCurvePreview, pCurvePreviewKey);
                return;
            }

            pToneBrightnessBox.IsChecked = pStep.LWorkStepActive;
            pInspectorBrightnessValue.Text = pStep.LWorkStepValue.ToString("0.#", CultureInfo.InvariantCulture);
            pInspectorBrightnessSlider.Value = Math.Clamp(
                pStep.LWorkStepValue,
                pInspectorBrightnessSlider.Minimum,
                pInspectorBrightnessSlider.Maximum);
            PToneApplyUpdate(pToneBrightnessBox, pInspectorBrightnessStack);
        }
        finally
        {
            pInspectorVideoSuppress = pPrevious;
        }
    }
}
