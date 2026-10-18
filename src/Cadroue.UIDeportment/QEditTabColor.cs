using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

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
        qEditViewer.QViewerColorSet(LColor.LColorVideoResolve(lVideo));
    }

    public LWorkVideo QEditVideoRead(bool lMpvOnlyCapable = true) =>
        qEditInspector.QInspectorVideo.QInspectorVideoRead(qEditProcessing.QProcessingSteps, lMpvOnlyCapable);

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

        string lEqNotice = QLocalizationCatalog.QLocalizationTextRead("Processing.Step.RequiresEq");
        qEditProcessing.QProcessingEnabledSet("Brightness", lEqCapable, lEqNotice);
        qEditProcessing.QProcessingEnabledSet("Contrast", lEqCapable, lEqNotice);
        qEditProcessing.QProcessingEnabledSet("Saturation", lEqCapable, lEqNotice);

        qEditProcessing.QProcessingEnabledSet(
            "Crop", lCropCapable, QLocalizationCatalog.QLocalizationTextRead("Processing.Step.RequiresCrop"));
        qEditInspector.QInspectorCapableSet(lCropCapable, lOrientationCapable);

        qEditProcessing.QProcessingEnabledSet(
            "Exposure", lExposureCapable, QLocalizationCatalog.QLocalizationTextRead(
                "Inspector.Video.ExposureRequiresEq"));

        qEditProcessing.QProcessingEnabledSet(
            "Curve", lCurveCapable, QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.CurveRequiresEq"));
        qEditInspector.QInspectorVideo.QInspectorCurve.QCurveCapableSet(lCurveCapable, lPreviewMpv);

        qEditProcessing.QProcessingEnabledSet(
            "Whitebalance",
            lWhitebalanceCapable,
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.WhitebalanceRequiresEq"));

        qEditProcessing.QProcessingEnabledSet(
            "Gamma", lEqCapable, QLocalizationCatalog.QLocalizationTextRead("Processing.Step.GammaRequiresEq"));
        qEditInspector.QInspectorGate.CInspectorCapableSet(
            lEqCapable, lExposureCapable, lWhitebalanceCapable, QEditMpvCheck());

        QEditColorUpdate();
        QEditColorApply();
    }

    // The viewer's pick goes to the gate whole: the gate applies it or keeps its failure status; a failure is traced.
    public void QEditNeutralHandle(LNeutralSample lSample) =>
        qEditInspector.QInspectorVideo.QInspectorWhitebalance.CWhitebalanceSampleAccept(
            new LWhitebalanceSampleOutlet(lSample, LTraceLog.LTraceInfoRecord));

    // New media asks the gate for a fresh estimate.
    public void QEditEstimateStart(LCargo lCargo) =>
        qEditInspector.QInspectorVideo.QInspectorWhitebalance.CWhitebalanceEstimateStart();

    // The gate asks for an estimate by method token; the viewer starts it.
    public void QEditEstimateHandle(string lToken) =>
        qEditViewer.QViewerNeutral.QViewerEstimateStart(LWhitebalancePort.LWhitebalanceMethodRead(lToken));

    // The viewer's picker and the gate's picker follow each other; the gate holds the target as whether it is white.
    public void QEditTargetHandle(bool lArmed, LNeutralTarget lTarget) =>
        qEditInspector.QInspectorVideo.QInspectorWhitebalance.CWhitebalanceToolSet(
            lArmed, lTarget == LNeutralTarget.LNeutralTargetWhite);

    public void QEditTargetApply(bool lArmed, bool lWhite) =>
        qEditViewer.QViewerNeutral.QViewerToolSet(
            lArmed, lWhite ? LNeutralTarget.LNeutralTargetWhite : LNeutralTarget.LNeutralTargetGrey);

    public void QEditHistogramRun() => qEditViewer.QViewerFrame.QViewerFrameRead(QEditHistogramApply);

    public void QEditEstimateApply(LNeutralWheel lEstimate) =>
        qEditInspector.QInspectorVideo.QInspectorWhitebalance.CWhitebalanceEstimateSet(
            lEstimate.LNeutralWheelX, lEstimate.LNeutralWheelY, lEstimate.LNeutralWheelPresent);

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
