namespace Cadroue.UIDeportment;

public sealed class QEditTool
{
    private readonly LInspector lEditInspector;
    private readonly LViewer lEditViewer;
    private readonly LCrop lEditCrop;

    public QEditTool(LInspector lInspector, LViewer lViewer)
    {
        lEditInspector = lInspector;
        lEditViewer = lViewer;
        lEditCrop = lViewer.LCrop;
    }

    public void LEditToolHandle(bool lArmed) => lEditCrop.LCropToolSet(lArmed);

    public void LEditLockHandle(bool lLocked)
    {
        if (lLocked)
        {
            lEditViewer.LViewerNeutral.LViewerNeutralCancel();
        }

        lEditCrop.LCropLockSet(lLocked);
        lEditCrop.LCropToolSet(!lLocked && lEditInspector.LInspectorToolArmed);
    }
}
