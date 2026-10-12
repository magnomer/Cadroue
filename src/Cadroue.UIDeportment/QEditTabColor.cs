using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed class QEditTabColor
{
    private readonly QInspector qEditInspector;
    private readonly QProcessing qEditProcessing;
    private readonly QViewer qEditViewer;

    public QEditTabColor(QInspector lInspector, QProcessing lProcessing, QViewer lViewer)
    {
        qEditInspector = lInspector;
        qEditProcessing = lProcessing;
        qEditViewer = lViewer;
    }

    public void QEditColorUpdate()
    {
        QEditActiveSet("Brightness", LColorKind.LColorKindBrightness);
        QEditActiveSet("Contrast", LColorKind.LColorKindContrast);
        QEditActiveSet("Saturation", LColorKind.LColorKindSaturation);
        QEditActiveSet("Gamma", LColorKind.LColorKindGamma);
        QEditActiveSet("Exposure", LColorKind.LColorKindExposure);
        QEditActiveSet("Curve", LColorKind.LColorKindCurve);
        QEditActiveSet("Whitebalance", LColorKind.LColorKindWhitebalance);
    }

    public void QEditColorApply()
    {
        LWorkVideo lVideo = qEditInspector.QInspectorSkip.QSkipActive
            ? LWorkVideo.LWorkVideoCreate()
            : QEditVideoRead(QEditMpvCheck());
        qEditViewer.QViewerColorSet(LPreview.LPreviewColorResolve(lVideo));
    }

    public LWorkVideo QEditVideoRead(bool lMpvOnlyCapable = true)
    {
        var lSteps = new List<LWorkVideoStep>();
        foreach (string lStep in qEditProcessing.QProcessingSteps)
        {
            if (LColor.LColorKindParse(lStep) is { } lKind)
            {
                lSteps.Add(qEditInspector.QInspectorVideo.QInspectorStepRead(lKind));
            }
        }

        return LEdit.LEditVideoCreate(lSteps, lMpvOnlyCapable);
    }

    public bool QEditEqCheck() => QEditFilterCheck("eq");

    public void QEditCapableHandle()
    {
        bool lPreviewMpv = QEditMpvCheck();
        bool lEqCapable = QEditEqCheck();
        bool lExposureCapable = QEditFilterCheck("exposure");
        bool lCurveCapable = QEditFilterCheck("curves");
        bool lWhitebalanceCapable = QEditFilterCheck("colorcorrect", "colorchannelmixer");
        bool lCropCapable = QEditFilterCheck("crop");
        bool lOrientationCapable = QEditFilterCheck("transpose", "hflip", "vflip");

        string lEqNotice = LLocalization.LLocalizationTextRead("Processing.Step.RequiresEq");
        qEditProcessing.QProcessingEnabledSet("Brightness", lEqCapable, lEqNotice);
        qEditProcessing.QProcessingEnabledSet("Contrast", lEqCapable, lEqNotice);
        qEditProcessing.QProcessingEnabledSet("Saturation", lEqCapable, lEqNotice);
        qEditInspector.QInspectorVideo.QInspectorTone.QToneCapableSet(lEqCapable);

        qEditProcessing.QProcessingEnabledSet(
            "Crop", lCropCapable, LLocalization.LLocalizationTextRead("Processing.Step.RequiresCrop"));
        qEditInspector.QInspectorCapableSet(lCropCapable, lOrientationCapable);

        qEditProcessing.QProcessingEnabledSet(
            "Exposure", lExposureCapable, LLocalization.LLocalizationTextRead("Inspector.Video.ExposureRequiresEq"));
        qEditInspector.QInspectorVideo.QInspectorExposure.QExposureCapableSet(lExposureCapable, lPreviewMpv);

        qEditProcessing.QProcessingEnabledSet(
            "Curve", lCurveCapable, LLocalization.LLocalizationTextRead("Inspector.Video.CurveRequiresEq"));
        qEditInspector.QInspectorVideo.QInspectorCurve.QCurveCapableSet(lCurveCapable, lPreviewMpv);

        qEditProcessing.QProcessingEnabledSet(
            "Whitebalance",
            lWhitebalanceCapable,
            LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceRequiresEq"));
        qEditInspector.QInspectorVideo.QInspectorWhitebalance.QWhitebalanceCapableSet(
            lWhitebalanceCapable, lPreviewMpv);

        qEditProcessing.QProcessingEnabledSet(
            "Gamma", lEqCapable, LLocalization.LLocalizationTextRead("Processing.Step.GammaRequiresEq"));
        qEditInspector.QInspectorVideo.QInspectorGamma.QGammaCapableSet(lEqCapable, lEqCapable && lPreviewMpv);

        QEditColorUpdate();
        QEditColorApply();
    }

    public void QEditNeutralHandle(LNeutralSample lSample)
    {
        switch (LNeutral.LNeutralStatusResolve(lSample.LNeutralOutcome))
        {
            case LNeutralStatus.LNeutralStatusValid:
                qEditInspector.QInspectorVideo.QInspectorWhitebalance.QWhitebalanceSampleSet(lSample);
                return;
            case LNeutralStatus.LNeutralStatusDecode:
                qEditInspector.QInspectorVideo.QInspectorWhitebalance.QWhitebalanceStatusSet(
                    LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceDecode"));
                LTraceLog.LTraceInfoRecord("Whitebalance pick: frame decode failed");
                return;
            default:
                qEditInspector.QInspectorVideo.QInspectorWhitebalance.QWhitebalanceStatusSet(
                    LLocalization.LLocalizationTextRead("Inspector.Video.WhitebalanceInvalid"));
                LTraceLog.LTraceInfoRecord($"Whitebalance pick: invalid sample ({lSample.LNeutralOutcome})");
                return;
        }
    }

    public void QEditEstimateHandle(LWhitebalanceMethod lMethod)
    {
        if (lMethod == LWhitebalanceMethod.LWhitebalanceMethodManual)
        {
            return;
        }

        qEditViewer.QViewerNeutral.QViewerEstimateStart(lMethod);
    }

    public void QEditHistogramRun() => qEditViewer.QViewerFrameRead(QEditHistogramApply);

    public void QEditEstimateApply(LNeutralWheel lEstimate) =>
        qEditInspector.QInspectorVideo.QInspectorWhitebalance.QWhitebalanceEstimateSet(lEstimate);

    public void QEditHistogramApply(LMediaFrame? lFrame) =>
        qEditInspector.QInspectorVideo.QInspectorCurve.QCurveHistogramSet(lFrame is null
            ? null
            : LHistogram.LHistogramCreate(lFrame.LMediaFramePixels, lFrame.LMediaFrameWidth, lFrame.LMediaFrameHeight));

    private void QEditActiveSet(string lStep, LColorKind lKind) =>
        qEditProcessing.QProcessingActiveSet(
            lStep, qEditInspector.QInspectorVideo.QInspectorStepRead(lKind).LWorkStepActive);

    private static bool QEditMpvCheck() => LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv;

    private static bool QEditFilterCheck(params string[] lFilters) => lFilters.All(LInventory.LInventoryFilterExist);
}
