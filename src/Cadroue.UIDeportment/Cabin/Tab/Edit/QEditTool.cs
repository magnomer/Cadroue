namespace Cadroue.UIDeportment;

public sealed class QEditTool
{
    private readonly QInspector qEditInspector;
    private readonly QViewer qEditViewer;
    private readonly QCrop qEditCrop;

    public QEditTool(QInspector lInspector, QViewer lViewer)
    {
        qEditInspector = lInspector;
        qEditViewer = lViewer;
        qEditCrop = lViewer.QCrop;
    }

    public void QEditToolHandle(bool lArmed) => qEditCrop.QCropToolSet(lArmed);

    public void QEditLockHandle(bool lLocked)
    {
        if (lLocked)
        {
            qEditViewer.QViewerNeutral.QViewerNeutralCancel();
        }

        qEditCrop.QCropLockSet(lLocked);
        qEditCrop.QCropToolSet(!lLocked && qEditInspector.QInspectorToolArmed);
    }
}
