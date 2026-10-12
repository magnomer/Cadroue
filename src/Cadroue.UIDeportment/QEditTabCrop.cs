using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QEditTabCrop
{
    private readonly QInspector qEditInspector;
    private readonly QProcessing qEditProcessing;
    private readonly QViewer qEditViewer;
    private readonly QCrop qEditCrop;
    private readonly QEditTabPlan qEditStore;
    private readonly QEditTabColor qEditColor;

    public QEditTabCrop(
        QInspector lInspector, QProcessing lProcessing, QViewer lViewer, QEditTabPlan lStore, QEditTabColor lColor)
    {
        qEditInspector = lInspector;
        qEditProcessing = lProcessing;
        qEditViewer = lViewer;
        qEditCrop = lViewer.QCrop;
        qEditStore = lStore;
        qEditColor = lColor;
    }

    public void QEditCropHandle()
    {
        LRotateFlip lRotate = qEditInspector.QInspectorOrientation.QInspectorRotateRead();
        if (qEditViewer.QViewerPreview.LRotateFlip != lRotate)
        {
            qEditViewer.QViewerRotateSet(lRotate);
            QEditSourceSync();
        }

        (bool lRatioFixed, _, int lRatioWidth, int lRatioHeight) =
            qEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateRatio;
        qEditCrop.QCropRatioSet(lRatioFixed ? lRatioWidth : 0, lRatioFixed ? lRatioHeight : 0);
        qEditCrop.QCropPersistentSet(qEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStatePersistent);
        qEditCrop.QCropActiveSet(qEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateActive);
        qEditCrop.QCropRectSet(qEditInspector.QInspectorCrop.QInspectorRectRead());
        QEditCropUpdate();
        qEditStore.QEditStateSave();
    }

    public void QEditCropShow()
    {
        QEditSourceSync();
        LTraceLog.LTraceInfoRecord(
            $"Edit crop from viewer: {QEditTabPlan.QEditRectFormat(qEditViewer.QViewerPreview.LCropbox)}");
        qEditInspector.QInspectorCrop.QInspectorCropSet(
            qEditViewer.QViewerPreview.LCropbox, qEditCrop.QCropDrive, qEditCrop.QCropAnchorX, qEditCrop.QCropAnchorY);
    }

    public void QEditCropRestore(string lName)
    {
        LEditPlan? lApplied = null;
        qEditInspector.QInspectorSaveSuspend();
        try
        {
            QEditSourceSync();
            LEditPlan? lPersistent = qEditStore.QEditCarriedRead();
            LEditPlan? lSaved = qEditStore.QEditSavedRead();
            LTraceLog.LTraceInfoRecord(
                $"Edit media ready '{lName}': display {QEditSourceFormat()}, "
                + $"persistent {(lPersistent is null ? "off" : "on")}, "
                + $"carried {QEditTabPlan.QEditPlanFormat(lPersistent)}, "
                + $"sidecar {QEditTabPlan.QEditPlanFormat(lSaved)}");
            qEditInspector.QInspectorCrop.QInspectorCropReset();

            bool lCarryWins = lPersistent is not null;
            LEditPlan lPlan = LEdit.LEditPlanResolve(
                lSaved,
                lPersistent,
                qEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStatePersistent,
                qEditInspector.QInspectorSkip.QSkipPersistent);
            LTraceLog.LTraceInfoRecord(
                $"Edit applying {(lCarryWins ? "persistent" : "sidecar")} plan to '{lName}': "
                + $"{QEditTabPlan.QEditPlanFormat(lPlan)}");
            qEditViewer.QViewerRotateSet(LRotateFlip.LRotateCropResolve(lPlan.LEditCrop));
            QEditSourceSync();

            lApplied = lCarryWins ? lPlan : null;
            qEditInspector.QInspectorPlanApply(lPlan);
        }
        finally
        {
            qEditInspector.QInspectorSaveResume();
        }

        qEditProcessing.QProcessingSkipSet(qEditInspector.QInspectorSkip.QSkipActive);
        QEditViewerApply();
        if (lApplied is not null)
        {
            qEditStore.QEditStateSave(lApplied);
        }
    }

    public void QEditViewerApply()
    {
        bool lSkip = qEditInspector.QInspectorSkip.QSkipActive;
        LRotateFlip lRotate = lSkip
            ? LRotateFlip.LRotateDefaultCreate()
            : qEditInspector.QInspectorOrientation.QInspectorRotateRead();
        LCropbox? lRect = lSkip ? null : qEditInspector.QInspectorCrop.QInspectorRectRead();
        LTraceLog.LTraceInfoRecord(
            $"Edit viewer push: rotate {lRotate.LRotateKind}, "
            + $"H {lRotate.LRotateFlipHorizontal}, V {lRotate.LRotateFlipVertical}, "
            + $"{QEditTabPlan.QEditRectFormat(lRect)}");
        qEditViewer.QViewerRotateSet(lRotate);
        qEditCrop.QCropRectSet(lRect);
        qEditColor.QEditColorApply();
    }

    public void QEditCropUpdate() =>
        qEditProcessing.QProcessingActiveSet("Crop", qEditInspector.QInspectorCrop.QInspectorActive);

    private void QEditSourceSync()
    {
        LCropboxSize lSize = qEditCrop.QCropDisplayRead();
        qEditInspector.QInspectorSourceSet(lSize.LCropboxSizeWidth, lSize.LCropboxSizeHeight);
    }

    private string QEditSourceFormat()
    {
        if (!qEditViewer.QViewerVideoPresent)
        {
            return "unknown";
        }

        LCropboxSize lSize = qEditCrop.QCropDisplayRead();
        return $"{lSize.LCropboxSizeWidth:0}x{lSize.LCropboxSizeHeight:0}";
    }
}
