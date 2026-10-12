using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record QCropBox(
    double QCropBoxX,
    double QCropBoxY,
    double QCropBoxWidth,
    double QCropBoxHeight,
    bool QCropBoxShown);

public sealed record QCropGrip(int QCropHandleIndex, double QCropHandleX, double QCropHandleY, bool QCropHandleShown);

public sealed record QCropShade(LCropbox QCropShadeVideo, LCropbox QCropShadeBox, bool QCropShadeShown);

public sealed class QCrop
{
    public const double QCropHandleSize = 10;
    public const int QCropHandleCount = 8;

    private static readonly int[] qCropGripX = [-1, 0, 1, 1, 1, 0, -1, -1];
    private static readonly int[] qCropGripY = [-1, -1, -1, 0, 1, 1, 1, 0];

    private readonly QViewer qViewer;
    private bool qCropActive = true;
    private bool qCropPersistent;
    private bool qCropLocked;
    private int qCropEdgeX;
    private int qCropEdgeY;
    private int qCropDrive = -1;
    private int qCropAnchorX = -1;
    private int qCropAnchorY = -1;
    private double qCropRatioWidth;
    private double qCropRatioHeight;
    private double qCropAreaWidth;
    private double qCropAreaHeight;
    private double qCropBoxX;
    private double qCropBoxY;
    private double qCropBoxWidth;
    private double qCropBoxHeight;
    private bool qCropBoxShown;

    public QCrop(QViewer lOwner)
    {
        qViewer = lOwner;
    }

    public event Action? QCropApplied;
    public event Action? QCropVideoChanged;

    public bool QCropActive => qCropActive;

    public bool QCropPersistent => qCropPersistent;

    public bool QCropLocked => qCropLocked;

    public int QCropEdgeX => qCropEdgeX;

    public int QCropEdgeY => qCropEdgeY;

    public int QCropDrive => qCropDrive;

    public int QCropAnchorX => qCropAnchorX;

    public int QCropAnchorY => qCropAnchorY;

    public double QCropRatioWidth => qCropRatioWidth;

    public double QCropRatioHeight => qCropRatioHeight;

    public bool QCropBoxShown => qCropBoxShown;

    public bool QCropEditable =>
        qCropActive && !qCropLocked && qViewer.QViewerNeutral.QViewerTool != QViewerTool.QViewerToolNeutral;

    public bool QCropCrossShown =>
        qViewer.QViewerNeutral.QViewerTool == QViewerTool.QViewerToolNeutral
        || (qViewer.QViewerNeutral.QViewerTool == QViewerTool.QViewerToolCrop && !qCropLocked);

    public bool QCropMoveCheck() => qCropEdgeX == 0 && qCropEdgeY == 0;

    public QCropBox QCropBoxRead() => new(qCropBoxX, qCropBoxY, qCropBoxWidth, qCropBoxHeight, qCropBoxShown);

    public IReadOnlyList<QCropGrip> QCropHandlesRead() =>
        Enumerable.Range(0, QCropHandleCount).Select(QCropHandleResolve).ToList();

    public QCropShade QCropShadeRead() => new(
        QCropVideoRead(),
        new LCropbox(qCropBoxX, qCropBoxY, qCropBoxWidth, qCropBoxHeight),
        qCropBoxShown && qCropBoxWidth > 0 && qCropBoxHeight > 0);

    public LCropboxSize QCropDisplayRead()
    {
        if (qViewer.QViewerMediaInfo is not { LMediaVideoPresent: true } lMediaInfo)
        {
            return new LCropboxSize(0, 0);
        }

        return LCropbox.LCropboxSourceResolve(
            lMediaInfo.LMediaVideoWidth,
            lMediaInfo.LMediaVideoHeight,
            qViewer.QViewerPreview.LRotateFlip.LRotateKind is LRotateKind.LRotate90 or LRotateKind.LRotate270);
    }

    public LCropbox QCropVideoRead()
    {
        if (!qViewer.QViewerVideoPresent || qCropAreaWidth <= 0 || qCropAreaHeight <= 0)
        {
            return new LCropbox(0, 0, qCropAreaWidth, qCropAreaHeight);
        }

        LCropboxSize lDisplay = QCropDisplayRead();
        return LCropbox.LCropboxDisplayResolve(
            lDisplay.LCropboxSizeWidth, lDisplay.LCropboxSizeHeight, qCropAreaWidth, qCropAreaHeight);
    }

    public LCropbox? QCropPixelRead()
    {
        if (!qViewer.QViewerVideoPresent || !qCropBoxShown)
        {
            return null;
        }

        LCropbox lVideo = QCropVideoRead();
        if (lVideo.LCropboxWidth <= 0 || lVideo.LCropboxHeight <= 0 || qCropBoxWidth <= 1 || qCropBoxHeight <= 1)
        {
            return null;
        }

        LCropboxSize lDisplay = QCropDisplayRead();
        return LCropbox.LCropboxPixelResolve(
            new LCropbox(qCropBoxX, qCropBoxY, qCropBoxWidth, qCropBoxHeight),
            lVideo,
            lDisplay.LCropboxSizeWidth,
            lDisplay.LCropboxSizeHeight);
    }

    public void QCropActiveSet(bool lActive)
    {
        if (qCropActive == lActive)
        {
            return;
        }

        qCropActive = lActive;
        if (!lActive)
        {
            QCropToolSet(false);
            qViewer.QCropDrag.QCropDragClear();
        }

        QCropBoxUpdate();
        QCropOverlayUpdate();
        qViewer.QViewerPreviewApply();
    }

    public void QCropPersistentSet(bool lPersistent) => qCropPersistent = lPersistent;

    public void QCropLockSet(bool lLocked)
    {
        if (qCropLocked == lLocked)
        {
            return;
        }

        qCropLocked = lLocked;
        if (lLocked)
        {
            qViewer.QCropDrag.QCropDragClear();
        }

        QCropOverlayUpdate();
    }

    public void QCropToolSet(bool lArmed)
    {
        QViewerNeutral lNeutral = qViewer.QViewerNeutral;
        if (lArmed && lNeutral.QViewerTool == QViewerTool.QViewerToolNeutral)
        {
            lNeutral.QViewerNeutralCancel();
        }

        lNeutral.QViewerToolApply(lArmed
            ? QViewerTool.QViewerToolCrop
            : lNeutral.QViewerTool == QViewerTool.QViewerToolCrop ? QViewerTool.QViewerToolNone : lNeutral.QViewerTool);
        QCropOverlayUpdate();
    }

    public void QCropRatioSet(double lWidth, double lHeight)
    {
        bool lValid = lWidth > 0 && lHeight > 0;
        qCropRatioWidth = lValid ? lWidth : 0;
        qCropRatioHeight = lValid ? lHeight : 0;
    }

    public void QCropGripSet(int lEdgeX, int lEdgeY)
    {
        qCropEdgeX = lEdgeX;
        qCropEdgeY = lEdgeY;
        qCropDrive = lEdgeX != 0 && lEdgeY != 0 ? -1 : lEdgeX != 0 ? 0 : 1;
        qCropAnchorX = -lEdgeX;
        qCropAnchorY = -lEdgeY;
    }

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

    public void QCropRectSet(LCropbox? lCropbox)
    {
        LCropbox? lClamped = QCropSourceClamp(lCropbox);
        if (lClamped is not { LCropboxWidth: > 0, LCropboxHeight: > 0 })
        {
            LTraceLog.LTraceInfoRecord("Viewer crop cleared: overlay hidden");
            QCropHide();
            return;
        }

        LCropboxSize lDisplay = QCropDisplayRead();
        bool lFull = lClamped.LCropboxWidth >= lDisplay.LCropboxSizeWidth
            && lClamped.LCropboxHeight >= lDisplay.LCropboxSizeHeight;
        LTraceLog.LTraceInfoRecord(
            $"Viewer crop set: {lClamped.LCropboxX:0},{lClamped.LCropboxY:0} "
            + $"{lClamped.LCropboxWidth:0}x{lClamped.LCropboxHeight:0} "
            + $"over display {lDisplay.LCropboxSizeWidth:0}x{lDisplay.LCropboxSizeHeight:0}"
            + (lFull ? " (full frame)" : string.Empty));

        qViewer.QViewerPreviewSet(qViewer.QViewerPreview.LCropboxChange(lClamped));
        QCropBoxUpdate();
        QCropBoxRestore();
        qViewer.QViewerFilterUpdate();
    }

    public void QCropHide()
    {
        qCropBoxShown = false;
        qCropBoxWidth = 0;
        qCropBoxHeight = 0;
        qViewer.QViewerPreviewSet(qViewer.QViewerPreview.LCropboxChange(null));
        QCropOverlayUpdate();
        qViewer.QViewerFilterUpdate();
    }

    public void QCropOverlayUpdate() => QCropApplied?.Invoke();

    public void QCropShownSet(bool lShown) => qCropBoxShown = lShown;

    public void QCropSizeHandle(double lWidth, double lHeight)
    {
        qCropAreaWidth = Math.Max(0, lWidth);
        qCropAreaHeight = Math.Max(0, lHeight);
        QCropBoxRestore();
    }

    public void QCropBoxSet(LCropbox lBox)
    {
        qCropBoxX = lBox.LCropboxX;
        qCropBoxY = lBox.LCropboxY;
        qCropBoxWidth = lBox.LCropboxWidth;
        qCropBoxHeight = lBox.LCropboxHeight;
    }

    public void QCropVideoCommit()
    {
        qViewer.QViewerPreviewSet(qViewer.QViewerPreview.LCropboxChange(QCropPixelRead()));
        qViewer.QViewerFilterUpdate();
        QCropVideoChanged?.Invoke();
    }

    private QCropGrip QCropHandleResolve(int lIndex)
    {
        int lEdgeX = qCropGripX[lIndex];
        int lEdgeY = qCropGripY[lIndex];
        double lPointX = lEdgeX == 0
            ? qCropBoxX + (qCropBoxWidth / 2)
            : lEdgeX < 0 ? qCropBoxX : qCropBoxX + qCropBoxWidth;
        double lPointY = lEdgeY == 0
            ? qCropBoxY + (qCropBoxHeight / 2)
            : lEdgeY < 0 ? qCropBoxY : qCropBoxY + qCropBoxHeight;
        bool lShown = QCropEditable && qCropBoxShown && qCropBoxWidth > 0 && qCropBoxHeight > 0;
        return new QCropGrip(lIndex, lPointX - (QCropHandleSize / 2), lPointY - (QCropHandleSize / 2), lShown);
    }

    private LCropbox? QCropSourceClamp(LCropbox? lCropbox)
    {
        if (lCropbox is not { LCropboxWidth: > 0, LCropboxHeight: > 0 } || !qViewer.QViewerVideoPresent)
        {
            return lCropbox;
        }

        LCropboxSize lSource = QCropDisplayRead();
        LCropbox lClamped = LCropbox.LCropboxRectClamp(
            lCropbox, new LCropbox(0, 0, lSource.LCropboxSizeWidth, lSource.LCropboxSizeHeight), false);
        return lClamped is { LCropboxWidth: > 0, LCropboxHeight: > 0 } ? lClamped : null;
    }

    private void QCropBoxUpdate() =>
        qCropBoxShown = qCropActive && qViewer.QViewerPreview.LCropbox is { LCropboxWidth: > 0, LCropboxHeight: > 0 };

    private void QCropBoxRestore()
    {
        if (qViewer.QViewerPreview.LCropbox is not { } lCropbox || !qViewer.QViewerVideoPresent)
        {
            return;
        }

        LCropbox lVideo = QCropVideoRead();
        LCropboxSize lDisplay = QCropDisplayRead();
        if (lDisplay.LCropboxSizeWidth <= 0 || lDisplay.LCropboxSizeHeight <= 0)
        {
            return;
        }

        QCropBoxSet(LCropbox.LCropboxOverlayResolve(
            lCropbox, lVideo, lDisplay.LCropboxSizeWidth, lDisplay.LCropboxSizeHeight));
        QCropOverlayUpdate();
    }
}
