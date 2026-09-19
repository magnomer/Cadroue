using Cadroue.Core;
using Cadroue.UIVeneer.PWing;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.Media;
using Cadroue.Infrastructure;

namespace Cadroue.UIVeneer.PCabin;

public sealed partial class PEditTab
{
    private void PEditSkipHandle()
    {
        pProcessing.PProcessingSkipSet(pInspector.PSkipActiveCheck());
        PEditViewerApply();
        PEditPlanSave();
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
        LWorkVideo pEditVideo = pInspector.PSkipActiveCheck()
            ? LWorkVideo.LWorkVideoCreate()
            : PEditVideoRead(PEditMpvCheck());
        pViewer.PViewerColorSet(LPreview.LPreviewColorResolve(pEditVideo));
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

        return LEdit.LEditVideoCreate(pSteps, pMpvOnlyCapable);
    }

    private bool PEditMpvCheck() =>
        Cadroue.Infrastructure.LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;

    private static bool PEditEqCheck() => PEditFilterCheck("eq");

    private static bool PEditFilterCheck(params string[] pEditFilters) =>
        pEditFilters.All(Cadroue.Infrastructure.LInventory.LInventoryFilterExist);

    private void PEditCapabilityHandle()
    {
        bool pPreviewMpv = PEditMpvCheck();
        bool pEqCapable = PEditEqCheck();
        bool pExposureCapable = PEditFilterCheck("exposure");
        bool pCurveCapable = PEditFilterCheck("curves");
        bool pWhitebalanceCapable = PEditFilterCheck("colorcorrect", "colorchannelmixer");
        bool pCropCapable = PEditFilterCheck("crop");
        bool pOrientationCapable = PEditFilterCheck("transpose", "hflip", "vflip");

        string pEqTooltip = LLocalization.LLocalizationTextRead("Processing.Step.RequiresEq");
        pProcessing.PProcessingEnabledSet("Brightness", pEqCapable, pEqTooltip);
        pProcessing.PProcessingEnabledSet("Contrast", pEqCapable, pEqTooltip);
        pProcessing.PProcessingEnabledSet("Saturation", pEqCapable, pEqTooltip);
        pInspector.PToneCapabilitySet(pEqCapable);

        pProcessing.PProcessingEnabledSet(
            "Crop", pCropCapable, LLocalization.LLocalizationTextRead("Processing.Step.RequiresCrop"));
        pInspector.PCropCapabilitySet(pCropCapable, pOrientationCapable);

        pProcessing.PProcessingEnabledSet(
            "Exposure",
            pExposureCapable,
            LLocalization.LLocalizationTextRead("Inspector.Video.ExposureRequiresEq"));
        pInspector.PExposureCapabilitySet(pExposureCapable, pPreviewMpv);

        pProcessing.PProcessingEnabledSet(
            "Curve", pCurveCapable, LLocalization.LLocalizationTextRead("Inspector.Video.CurveRequiresEq"));
        pInspector.PCurveCapabilitySet(pCurveCapable, pPreviewMpv);

        pProcessing.PProcessingEnabledSet(
            "Whitebalance",
            pWhitebalanceCapable,
            LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceRequiresEq"));
        pInspector.PWhitebalanceCapabilitySet(pWhitebalanceCapable, pPreviewMpv);

        string pGammaTooltip = LLocalization.LLocalizationTextRead("Processing.Step.GammaRequiresEq");
        pProcessing.PProcessingEnabledSet("Gamma", pEqCapable, pGammaTooltip);
        pInspector.PGammaCapabilitySet(pEqCapable, pEqCapable && pPreviewMpv);

        PEditColorUpdate();
        PEditColorApply();
    }
}
