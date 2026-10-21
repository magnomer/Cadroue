using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

// The white balance picker's relay: the viewer's neutral tool and the inspector's white balance gate follow each other.
public sealed class QEditTabWhitebalance
{
    private readonly QInspector qEditInspector;
    private readonly QViewer qEditViewer;

    public QEditTabWhitebalance(QInspector lInspector, QViewer lViewer)
    {
        qEditInspector = lInspector;
        qEditViewer = lViewer;
    }

    // The relay listens to the gate and the viewer it joins, in the edit tab's subscription order.
    public void QEditWhitebalanceAttach()
    {
        qEditInspector.QInspectorVideo.QInspectorWhitebalance.CWhitebalanceEstimateChanged += QEditEstimateHandle;
        qEditViewer.QViewerMediaChanged += QEditEstimateStart;
        qEditViewer.QViewerNeutral.QViewerToolChanged += QEditTargetHandle;
        qEditViewer.QViewerNeutral.QViewerNeutralChanged += QEditNeutralHandle;
        qEditViewer.QViewerNeutral.QViewerEstimateChanged += QEditEstimateApply;
        qEditInspector.QInspectorVideo.QInspectorWhitebalance.CWhitebalanceToolChanged += QEditTargetApply;
    }

    public void QEditWhitebalanceClose()
    {
        qEditInspector.QInspectorVideo.QInspectorWhitebalance.CWhitebalanceEstimateChanged -= QEditEstimateHandle;
        qEditViewer.QViewerMediaChanged -= QEditEstimateStart;
        qEditViewer.QViewerNeutral.QViewerToolChanged -= QEditTargetHandle;
        qEditViewer.QViewerNeutral.QViewerNeutralChanged -= QEditNeutralHandle;
        qEditViewer.QViewerNeutral.QViewerEstimateChanged -= QEditEstimateApply;
        qEditInspector.QInspectorVideo.QInspectorWhitebalance.CWhitebalanceToolChanged -= QEditTargetApply;
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

    // The viewer's picker and the gate's picker follow each other; the gate owns the viewer-target mapping.
    public void QEditTargetHandle(bool lArmed, LNeutralTarget lTarget) =>
        qEditInspector.QInspectorVideo.QInspectorWhitebalance.CWhitebalanceToolSet(lArmed, lTarget);

    public void QEditTargetApply(bool lArmed, bool lWhite) =>
        qEditViewer.QViewerNeutral.QViewerToolSet(
            lArmed, lWhite ? LNeutralTarget.LNeutralTargetWhite : LNeutralTarget.LNeutralTargetGrey);

    public void QEditEstimateApply(LNeutralWheel lEstimate) =>
        qEditInspector.QInspectorVideo.QInspectorWhitebalance.CWhitebalanceEstimateSet(
            lEstimate.LNeutralWheelX, lEstimate.LNeutralWheelY, lEstimate.LNeutralWheelPresent);
}
