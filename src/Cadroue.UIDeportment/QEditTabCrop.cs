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

    public QEditTabCrop(QInspector lInspector, QProcessing lProcessing, QViewer lViewer, QEditTabPlan lStore)
    {
        qEditInspector = lInspector;
        qEditProcessing = lProcessing;
        qEditViewer = lViewer;
        qEditCrop = lViewer.QCrop;
        qEditStore = lStore;
    }

    public void QEditCropHandle()
    {
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

    public void QEditCropStart()
    {
        QEditCropUpdate();
        qEditCrop.QCropActiveSet(qEditInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateActive);
    }

    public void QEditCropUpdate() =>
        qEditProcessing.QProcessingActiveSet("Crop", qEditInspector.QInspectorCrop.QInspectorActive);

    public void QEditSourceSync()
    {
        LCropboxSize lSize = qEditCrop.QCropDisplayRead();
        qEditInspector.QInspectorSourceSet(lSize.LCropboxSizeWidth, lSize.LCropboxSizeHeight);
    }

    public string QEditSourceFormat()
    {
        if (!qEditViewer.QViewerVideoPresent)
        {
            return "unknown";
        }

        LCropboxSize lSize = qEditCrop.QCropDisplayRead();
        return $"{lSize.LCropboxSizeWidth:0}x{lSize.LCropboxSizeHeight:0}";
    }
}
