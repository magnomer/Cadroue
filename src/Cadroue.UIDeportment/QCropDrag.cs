using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed class QCropDrag
{
    private const double QCropSizeMinimum = 8;

    private readonly QViewer qViewer;
    private int qCropEdgeX;
    private int qCropEdgeY;
    private int qCropDrive = -1;
    private int qCropAnchorX = -1;
    private int qCropAnchorY = -1;
    private double qCropOriginX;
    private double qCropOriginY;
    private double qCropOriginWidth;
    private double qCropOriginHeight;
    private double qCropGrabX;
    private double qCropGrabY;
    private bool qCropPress;
    private double qCropPointX;
    private double qCropPointY;
    private bool qCropPointActive;

    public QCropDrag(QViewer lOwner)
    {
        qViewer = lOwner;
    }

    public event Action<bool>? QCropCaptureApplied;

    public bool QCropPress => qCropPress;

    public bool QCropPointActive => qCropPointActive;

    public int QCropEdgeX => qCropEdgeX;

    public int QCropEdgeY => qCropEdgeY;

    public int QCropDrive => qCropDrive;

    public int QCropAnchorX => qCropAnchorX;

    public int QCropAnchorY => qCropAnchorY;

    private QCrop QCrop => qViewer.QCrop;

    public bool QCropMoveCheck() => qCropEdgeX == 0 && qCropEdgeY == 0;

    public void QCropGripSet(int lEdgeX, int lEdgeY)
    {
        qCropEdgeX = lEdgeX;
        qCropEdgeY = lEdgeY;
        qCropDrive = QCropDriveResolve(lEdgeX, lEdgeY);
        qCropAnchorX = -lEdgeX;
        qCropAnchorY = -lEdgeY;
    }

    private static int QCropDriveResolve(int lEdgeX, int lEdgeY) =>
        lEdgeX != 0 && lEdgeY != 0 ? -1 : lEdgeX != 0 ? 0 : 1;

    public void QCropBodySet()
    {
        qCropEdgeX = 0;
        qCropEdgeY = 0;
        QCropDrawSet();
    }

    public void QCropDrawSet()
    {
        qCropDrive = -1;
        qCropAnchorX = -1;
        qCropAnchorY = -1;
    }

    public void QCropCaptureRelease() => QCropCaptureApplied?.Invoke(false);

    public void QCropDragClear()
    {
        QCropPressSet(false);
        QCropPointClear();
        QCropCaptureRelease();
    }

    private void QCropPressSet(bool lPress) => qCropPress = lPress;

    private void QCropPointSet(double lX, double lY)
    {
        qCropPointX = lX;
        qCropPointY = lY;
        qCropPointActive = true;
    }

    private void QCropPointClear() => qCropPointActive = false;

    public bool QCropGripHandle(int lIndex, double lX, double lY)
    {
        if (!QCrop.QCropEditable)
        {
            return false;
        }

        QCropGripSet(QCrop.QCropGripX[lIndex], QCrop.QCropGripY[lIndex]);
        QCropDragStart(lX, lY);
        return true;
    }

    public bool QCropBodyHandle(double lX, double lY)
    {
        if (!QCrop.QCropEditable || !QCrop.QCropBoxShown)
        {
            return false;
        }

        QCropBodySet();
        QCropDragStart(lX, lY);
        return true;
    }

    public bool QCropPressHandle(double lX, double lY)
    {
        QViewerNeutral lNeutral = qViewer.QViewerNeutral;
        if (lNeutral.QViewerTool == QViewerTool.QViewerToolNeutral)
        {
            lNeutral.QViewerPressHandle(lX, lY);
            return true;
        }

        if (lNeutral.QViewerTool != QViewerTool.QViewerToolCrop
            || !QCrop.QCropActive
            || QCrop.QCropLocked
            || !qViewer.QViewerVideoPresent)
        {
            return false;
        }

        QCropPointSet(lX, lY);
        QCropDrawSet();
        QCrop.QCropShownSet(true);
        QCropCaptureApplied?.Invoke(true);
        QCropBoxPlace(lX, lY, lX, lY);
        return true;
    }

    public bool QCropMoveHandle(bool lPressed, double lX, double lY)
    {
        if (!lPressed)
        {
            return false;
        }

        if (qCropPress)
        {
            QCropDragApply(lX, lY);
            return true;
        }

        if (!qCropPointActive)
        {
            return false;
        }

        QCropBoxPlace(qCropPointX, qCropPointY, lX, lY);
        return true;
    }

    public bool QCropReleaseHandle(double lX, double lY)
    {
        if (qCropPress)
        {
            QCropDragApply(lX, lY);
            QCropPressSet(false);
            QCropCaptureRelease();
            QCrop.QCropVideoCommit();
            return true;
        }

        if (!qCropPointActive)
        {
            return false;
        }

        QCropBoxPlace(qCropPointX, qCropPointY, lX, lY);
        QCropPointClear();
        QCropCaptureRelease();
        QCrop.QCropOverlayUpdate();
        QCrop.QCropVideoCommit();
        return true;
    }

    private void QCropBoxPlace(double lStartX, double lStartY, double lEndX, double lEndY)
    {
        LCropbox lVideo = QCrop.QCropVideoRead();
        LCropboxPoint lStart = LCropboxDisplay.LCropboxPointClamp(lStartX, lStartY, lVideo);
        LCropboxPoint lEnd = LCropboxDisplay.LCropboxPointClamp(lEndX, lEndY, lVideo);
        QCrop.QCropBoxSet(LCropboxDrag.LCropboxDrawResolve(
            lStart.LCropboxPointX, lStart.LCropboxPointY, lEnd.LCropboxPointX, lEnd.LCropboxPointY,
            QCrop.QCropRatioWidth, QCrop.QCropRatioHeight));
        QCrop.QCropOverlayUpdate();
    }

    private void QCropDragStart(double lGrabX, double lGrabY)
    {
        QCropOriginSet(QCrop.QCropBoxRead(), lGrabX, lGrabY);
        QCropPressSet(true);
        QCropCaptureApplied?.Invoke(true);
    }

    private void QCropOriginSet(QCropBox lBox, double lGrabX, double lGrabY)
    {
        qCropOriginX = lBox.QCropBoxX;
        qCropOriginY = lBox.QCropBoxY;
        qCropOriginWidth = lBox.QCropBoxWidth;
        qCropOriginHeight = lBox.QCropBoxHeight;
        qCropGrabX = lGrabX;
        qCropGrabY = lGrabY;
    }

    private void QCropDragApply(double lDragX, double lDragY)
    {
        QCrop.QCropBoxSet(QCropDragResolve(lDragX, lDragY));
        QCrop.QCropOverlayUpdate();
    }

    private LCropbox QCropDragResolve(double lDragX, double lDragY)
    {
        LCropbox lVideo = QCrop.QCropVideoRead();
        var lOrigin = new LCropbox(qCropOriginX, qCropOriginY, qCropOriginWidth, qCropOriginHeight);
        LCropbox lResult;
        if (QCropMoveCheck())
        {
            lResult = LCropboxDrag.LCropboxMoveResolve(lOrigin, qCropGrabX, qCropGrabY, lDragX, lDragY, lVideo);
        }
        else
        {
            LCropboxPoint lClamped = LCropboxDisplay.LCropboxPointClamp(lDragX, lDragY, lVideo);
            lResult = LCropboxDrag.LCropboxResizeResolve(
                lOrigin, lClamped.LCropboxPointX, lClamped.LCropboxPointY, qCropEdgeX, qCropEdgeY,
                QCrop.QCropRatioWidth, QCrop.QCropRatioHeight, lVideo, QCropSizeMinimum);
        }

        return lResult;
    }
}
