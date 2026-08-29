using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.Media;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PEditTab
{
    private void PEditSkipHandle()
    {
        pProcessing.PProcessingSkipSet(pInspector.PSkipActiveCheck());
        PEditPlanSave();
    }

    private void PEditStepHandle(string? pStepName)
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

    private void PEditChangeHandle()
    {
        PEditColorUpdate();
        pEditColorTimer.Stop();
        pEditColorTimer.Start();
        PEditPlanSave();
    }

    private void PEditColorUpdate()
    {
        pProcessing.PProcessingActiveSet("Brightness",
            pInspector.PToneStepRead(LColorKind.LColorKindBrightness).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Contrast",
            pInspector.PToneStepRead(LColorKind.LColorKindContrast).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Saturation",
            pInspector.PToneStepRead(LColorKind.LColorKindSaturation).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Gamma",
            pInspector.PToneStepRead(LColorKind.LColorKindGamma).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Exposure",
            pInspector.PToneStepRead(LColorKind.LColorKindExposure).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Curve",
            pInspector.PToneStepRead(LColorKind.LColorKindCurve).LWorkStepActive);
        pProcessing.PProcessingActiveSet("Whitebalance",
            pInspector.PToneStepRead(LColorKind.LColorKindWhitebalance).LWorkStepActive);
    }

    private void PEditNeutralHandle(LNeutralSample pNeutralSample)
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

    private void PEditEstimateHandle(LWhitebalanceMethod pMethod)
    {
        if (pMethod == LWhitebalanceMethod.LWhitebalanceMethodManual)
        {
            return;
        }

        pViewer.PViewerEstimateRead(pMethod, pInspector.PWhitebalanceEstimateApply);
    }

    private void PEditHistogramDefer()
    {
        pEditHistogramTimer.Stop();
        pEditHistogramTimer.Start();
    }

    private void PEditHistogramHandle()
    {
        pViewer.PViewerFrameRead(pFrame => pInspector.PCurveHistogramApply(
            pFrame is null
                ? null
                : LHistogram.LHistogramCreate(
                    pFrame.LMediaFramePixels, pFrame.LMediaFrameWidth, pFrame.LMediaFrameHeight)));
    }

    private void PEditColorApply()
    {
        pViewer.PViewerColorSet(LPreview.LPreviewColorResolve(PEditVideoRead(PEditMpvCheck())));
    }

    private LWorkVideo PEditVideoRead(bool pMpvOnlyCapable = true)
    {
        var pSteps = new List<LWorkVideoStep>();
        foreach (string pStepName in pProcessing.PProcessingStepsRead())
        {
            if (LColor.LColorKindParse(pStepName) is { } pKind)
            {
                pSteps.Add(pInspector.PToneStepRead(pKind));
            }
        }

        return LEdit.LEditVideoCreate(pSteps, pMpvOnlyCapable, PEditEqCheck());
    }

    private bool PEditMpvCheck() =>
        Cadroue.Infrastructure.LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;

    private static bool PEditEqCheck() =>
        Cadroue.Infrastructure.LInventory.LInventoryFilterExist("eq");

    private void PEditCapabilityHandle()
    {
        bool pPreviewMpv = PEditMpvCheck();
        bool pEqCapable = PEditEqCheck();

        string pEqTooltip = LLocalization.LLocalizationTextRead("Processing.Step.RequiresEq");
        pProcessing.PProcessingEnabledSet("Brightness", pEqCapable, pEqTooltip);
        pProcessing.PProcessingEnabledSet("Contrast", pEqCapable, pEqTooltip);
        pProcessing.PProcessingEnabledSet("Saturation", pEqCapable, pEqTooltip);
        pInspector.PToneCapabilitySet(pEqCapable);

        pProcessing.PProcessingEnabledSet("Exposure", true, string.Empty);
        pInspector.PExposureCapabilitySet(true, pPreviewMpv);

        pProcessing.PProcessingEnabledSet("Curve", true, string.Empty);
        pInspector.PCurveCapabilitySet(true, pPreviewMpv, "Inspector.Video.CurvePreviewMpv");

        pProcessing.PProcessingEnabledSet("Whitebalance", true, string.Empty);
        pInspector.PWhitebalanceCapabilitySet(true, pPreviewMpv);

        string pGammaTooltip = LLocalization.LLocalizationTextRead("Processing.Step.GammaRequiresEq");
        pProcessing.PProcessingEnabledSet("Gamma", pEqCapable, pGammaTooltip);
        pInspector.PGammaCapabilitySet(pEqCapable, pEqCapable && pPreviewMpv, "Inspector.Video.GammaRequiresEq");

        PEditColorUpdate();
        PEditColorApply();
    }
}
