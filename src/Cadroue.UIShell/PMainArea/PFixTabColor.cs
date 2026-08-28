using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.Media;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PFixTab
{
    private void PFixSkipHandle()
    {
        pProcessing.PProcessingSkipSet(pInspector.PSkipActiveCheck());
        PFixPlanSave();
    }

    private void PFixStepHandle(string? pStepName)
    {
        if (string.IsNullOrEmpty(pStepName) || pStepName == "No Processing")
        {
            return;
        }

        if (pInspector.PSkipPersistentCheck())
        {
            pInspector.PSkipPersistentApply(false);
        }

        if (pInspector.PSkipActiveCheck())
        {
            pInspector.PSkipApply(false);
        }
    }

    private void PFixChangeHandle()
    {
        PFixColorUpdate();
        pFixColorTimer.Stop();
        pFixColorTimer.Start();
        PFixPlanSave();
    }

    private void PFixColorUpdate()
    {
        pProcessing.PProcessingActiveSet("Brightness",
            pInspector.PToneStepRead(LColorKind.LColorKindBrightness).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Contrast",
            pInspector.PToneStepRead(LColorKind.LColorKindContrast).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Saturation",
            pInspector.PToneStepRead(LColorKind.LColorKindSaturation).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Gamma",
            PFixMpvCheck()
            && pInspector.PToneStepRead(LColorKind.LColorKindGamma).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Exposure",
            PFixMpvCheck()
            && pInspector.PToneStepRead(LColorKind.LColorKindExposure).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Curve",
            PFixMpvCheck()
            && pInspector.PToneStepRead(LColorKind.LColorKindCurve).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Whitebalance",
            PFixMpvCheck()
            && pInspector.PToneStepRead(LColorKind.LColorKindWhitebalance).LWorkStepActive);
    }

    private void PFixNeutralHandle(LNeutralSample pNeutralSample)
    {
        switch (LNeutral.LNeutralStatusResolve(pNeutralSample.LNeutralOutcome))
        {
            case LNeutralStatus.LNeutralStatusValid:
                pInspector.PToneNeutralApply(pNeutralSample);
                return;
            case LNeutralStatus.LNeutralStatusDecode:
                pInspector.PInspectorNeutralShow(
                    LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceDecode"));
                LTraceLog.LTraceInfoRecord("Whitebalance pick: frame decode failed");
                return;
            default:
                pInspector.PInspectorNeutralShow(
                    LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceInvalid"));
                LTraceLog.LTraceInfoRecord($"Whitebalance pick: invalid sample ({pNeutralSample.LNeutralOutcome})");
                return;
        }
    }

    private void PFixEstimateHandle(LWhitebalanceMethod pMethod)
    {
        if (pMethod == LWhitebalanceMethod.LWhitebalanceMethodManual)
        {
            return;
        }

        pViewer.PViewerEstimateRead(pMethod, pInspector.PWhitebalanceEstimateApply);
    }

    private void PFixHistogramDefer()
    {
        pFixHistogramTimer.Stop();
        pFixHistogramTimer.Start();
    }

    private void PFixHistogramHandle()
    {
        pViewer.PViewerFrameRead(pFrame => pInspector.PCurveHistogramApply(
            pFrame is null
                ? null
                : LHistogram.LHistogramCreate(
                    pFrame.LMediaFramePixels, pFrame.LMediaFrameWidth, pFrame.LMediaFrameHeight)));
    }

    private void PFixColorApply()
    {
        pViewer.PViewerColorSet(LPreview.LPreviewColorResolve(PFixVideoRead(PFixMpvCheck())));
    }

    private LWorkVideo PFixVideoRead(bool pMpvOnlyCapable = true)
    {
        var pSteps = new List<LWorkVideoStep>();
        foreach (string pStepName in pProcessing.PProcessingStepsRead())
        {
            if (LColor.LColorKindParse(pStepName) is { } pKind)
            {
                pSteps.Add(pInspector.PToneStepRead(pKind));
            }
        }

        return LEdit.LEditVideoCreate(pSteps, pMpvOnlyCapable, PFixEqCheck());
    }

    private bool PFixMpvCheck() =>
        Cadroue.Infrastructure.LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;

    private static bool PFixEqCheck() =>
        Cadroue.Infrastructure.LInventory.LInventoryFilterExist("eq");

    private void PFixCapabilityHandle()
    {
        bool pMpvOnlyCapable = PFixMpvCheck();
        bool pEqCapable = PFixEqCheck();

        string pEqTooltip = LLocalization.LLocalizationTextRead("Processing.Step.RequiresEq");
        pProcessing.PProcessingEnabledSet("Brightness", pEqCapable, pEqTooltip);
        pProcessing.PProcessingEnabledSet("Contrast", pEqCapable, pEqTooltip);
        pProcessing.PProcessingEnabledSet("Saturation", pEqCapable, pEqTooltip);
        pInspector.PToneCapabilitySet(pEqCapable);

        string pExposureTooltip = LLocalization.LLocalizationTextRead("Processing.Step.ExposureRequiresMpv");
        pProcessing.PProcessingEnabledSet("Exposure", pMpvOnlyCapable, pExposureTooltip);
        pInspector.PExposureCapabilitySet(pMpvOnlyCapable);

        string pCurveTooltip = LLocalization.LLocalizationTextRead("Processing.Step.CurveRequiresMpv");
        pProcessing.PProcessingEnabledSet("Curve", pMpvOnlyCapable, pCurveTooltip);
        pInspector.PCurveCapabilitySet(pMpvOnlyCapable, "Inspector.Video.CurveRequiresMpv");
        string pWhitebalanceTooltip = LLocalization.LLocalizationTextRead(
            "Processing.Step.WhitebalanceRequiresMpv");
        pProcessing.PProcessingEnabledSet("Whitebalance", pMpvOnlyCapable, pWhitebalanceTooltip);
        pInspector.PWhitebalanceCapabilitySet(pMpvOnlyCapable);

        bool pGammaCapable = pMpvOnlyCapable && pEqCapable;
        string pGammaTooltip = LLocalization.LLocalizationTextRead(
            !pEqCapable ? "Processing.Step.GammaRequiresEq" : "Processing.Step.GammaRequiresMpv");
        pProcessing.PProcessingEnabledSet("Gamma", pGammaCapable, pGammaTooltip);
        pInspector.PGammaCapabilitySet(
            pGammaCapable,
            !pEqCapable ? "Inspector.Video.GammaRequiresEq" : "Inspector.Video.GammaRequiresMpv");

        PFixColorUpdate();
        PFixColorApply();
    }
}
