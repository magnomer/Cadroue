using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed class LEditTabColor
{
    private readonly LInspector lEditInspector;
    private readonly LProcessing lEditProcessing;

    public LEditTabColor(LInspector lInspector, LProcessing lProcessing)
    {
        lEditInspector = lInspector;
        lEditProcessing = lProcessing;
    }

    public event Action<LColor>? LEditPreviewApply;
    public event Action<LWhitebalanceMethod>? LEditEstimateRead;
    public event Action? LEditHistogramRead;

    public void LEditColorUpdate()
    {
        LEditActiveSet("Brightness", LColorKind.LColorKindBrightness);
        LEditActiveSet("Contrast", LColorKind.LColorKindContrast);
        LEditActiveSet("Saturation", LColorKind.LColorKindSaturation);
        LEditActiveSet("Gamma", LColorKind.LColorKindGamma);
        LEditActiveSet("Exposure", LColorKind.LColorKindExposure);
        LEditActiveSet("Curve", LColorKind.LColorKindCurve);
        LEditActiveSet("Whitebalance", LColorKind.LColorKindWhitebalance);
    }

    public void LEditColorApply()
    {
        LWorkVideo lVideo = lEditInspector.LInspectorSkip.LSkipActive
            ? LWorkVideo.LWorkVideoCreate()
            : LEditVideoRead(LEditMpvCheck());
        LEditPreviewApply?.Invoke(LPreview.LPreviewColorResolve(lVideo));
    }

    public LWorkVideo LEditVideoRead(bool lMpvOnlyCapable = true)
    {
        var lSteps = new List<LWorkVideoStep>();
        foreach (string lStep in lEditProcessing.LProcessingSteps)
        {
            if (LColor.LColorKindParse(lStep) is { } lKind)
            {
                lSteps.Add(lEditInspector.LInspectorStepRead(lKind));
            }
        }

        return LEdit.LEditVideoCreate(lSteps, lMpvOnlyCapable);
    }

    public bool LEditEqCheck() => LEditFilterCheck("eq");

    public void LEditCapableHandle()
    {
        bool lPreviewMpv = LEditMpvCheck();
        bool lEqCapable = LEditEqCheck();
        bool lExposureCapable = LEditFilterCheck("exposure");
        bool lCurveCapable = LEditFilterCheck("curves");
        bool lWhitebalanceCapable = LEditFilterCheck("colorcorrect", "colorchannelmixer");
        bool lCropCapable = LEditFilterCheck("crop");
        bool lOrientationCapable = LEditFilterCheck("transpose", "hflip", "vflip");

        string lEqNotice = LLocalization.LLocalizationTextRead("Processing.Step.RequiresEq");
        lEditProcessing.LProcessingEnabledSet("Brightness", lEqCapable, lEqNotice);
        lEditProcessing.LProcessingEnabledSet("Contrast", lEqCapable, lEqNotice);
        lEditProcessing.LProcessingEnabledSet("Saturation", lEqCapable, lEqNotice);
        lEditInspector.LInspectorTone.LToneCapableSet(lEqCapable);

        lEditProcessing.LProcessingEnabledSet(
            "Crop", lCropCapable, LLocalization.LLocalizationTextRead("Processing.Step.RequiresCrop"));
        lEditInspector.LInspectorCapableSet(lCropCapable, lOrientationCapable);

        lEditProcessing.LProcessingEnabledSet(
            "Exposure", lExposureCapable, LLocalization.LLocalizationTextRead("Inspector.Video.ExposureRequiresEq"));
        lEditInspector.LInspectorExposure.LExposureCapableSet(lExposureCapable, lPreviewMpv);

        lEditProcessing.LProcessingEnabledSet(
            "Curve", lCurveCapable, LLocalization.LLocalizationTextRead("Inspector.Video.CurveRequiresEq"));
        lEditInspector.LInspectorCurve.LCurveCapableSet(lCurveCapable, lPreviewMpv);

        lEditProcessing.LProcessingEnabledSet(
            "Whitebalance",
            lWhitebalanceCapable,
            LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceRequiresEq"));
        lEditInspector.LInspectorWhitebalance.LWhitebalanceCapableSet(lWhitebalanceCapable, lPreviewMpv);

        lEditProcessing.LProcessingEnabledSet(
            "Gamma", lEqCapable, LLocalization.LLocalizationTextRead("Processing.Step.GammaRequiresEq"));
        lEditInspector.LInspectorGamma.LGammaCapableSet(lEqCapable, lEqCapable && lPreviewMpv);

        LEditColorUpdate();
        LEditColorApply();
    }

    public void LEditNeutralHandle(LNeutralSample lSample)
    {
        switch (LNeutral.LNeutralStatusResolve(lSample.LNeutralOutcome))
        {
            case LNeutralStatus.LNeutralStatusValid:
                lEditInspector.LInspectorWhitebalance.LWhitebalanceSampleSet(lSample);
                return;
            case LNeutralStatus.LNeutralStatusDecode:
                lEditInspector.LInspectorWhitebalance.LWhitebalanceStatusSet(
                    LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceDecode"));
                LTraceLog.LTraceInfoRecord("Whitebalance pick: frame decode failed");
                return;
            default:
                lEditInspector.LInspectorWhitebalance.LWhitebalanceStatusSet(
                    LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceInvalid"));
                LTraceLog.LTraceInfoRecord($"Whitebalance pick: invalid sample ({lSample.LNeutralOutcome})");
                return;
        }
    }

    public void LEditEstimateHandle(LWhitebalanceMethod lMethod)
    {
        if (lMethod == LWhitebalanceMethod.LWhitebalanceMethodManual)
        {
            return;
        }

        LEditEstimateRead?.Invoke(lMethod);
    }

    public void LEditHistogramRun() => LEditHistogramRead?.Invoke();

    public void LEditEstimateApply(LNeutralWheel lEstimate) =>
        lEditInspector.LInspectorWhitebalance.LWhitebalanceEstimateSet(lEstimate);

    public void LEditHistogramApply(LMediaFrame? lFrame) =>
        lEditInspector.LInspectorCurve.LCurveHistogramSet(lFrame is null
            ? null
            : LHistogram.LHistogramCreate(lFrame.LMediaFramePixels, lFrame.LMediaFrameWidth, lFrame.LMediaFrameHeight));

    private void LEditActiveSet(string lStep, LColorKind lKind) =>
        lEditProcessing.LProcessingActiveSet(lStep, lEditInspector.LInspectorStepRead(lKind).LWorkStepActive);

    private static bool LEditMpvCheck() => LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;

    private static bool LEditFilterCheck(params string[] lFilters) => lFilters.All(LInventory.LInventoryFilterExist);
}
