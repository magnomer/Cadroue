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
        PEditActiveUpdate();
        pEditColorTimer.Stop();
        pEditColorTimer.Start();
        PEditPlanSave();
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
        bool pMpvOnlyCapable = PEditMpvCheck();
        bool pEqCapable = PEditEqCheck();

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

        PEditActiveUpdate();
        PEditColorApply();
    }
}
