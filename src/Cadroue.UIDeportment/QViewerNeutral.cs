using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public enum QViewerTool
{
    QViewerToolNone,
    QViewerToolCrop,
    QViewerToolNeutral
}

public sealed class QViewerNeutral
{
    private readonly QViewer qViewer;
    private QViewerTool qViewerTool;
    private LNeutralTarget qViewerTarget;
    private int qViewerSerial;
    private bool qViewerPlayingHeld;

    public QViewerNeutral(QViewer lOwner)
    {
        qViewer = lOwner;
    }

    public event Action<bool, LNeutralTarget>? QViewerToolChanged;
    public event Action<LNeutralSample>? QViewerNeutralChanged;
    public event Action<LNeutralWheel>? QViewerEstimateChanged;
    public event Action? QViewerFocusApplied;

    public QViewerTool QViewerTool => qViewerTool;

    public LNeutralTarget QViewerNeutralTarget => qViewerTarget;

    public int QViewerNeutralSerial => qViewerSerial;

    public bool QViewerNeutralArmed => qViewerTool == QViewerTool.QViewerToolNeutral;

    public void QViewerToolApply(QViewerTool lTool) => qViewerTool = lTool;

    public void QViewerToolSet(bool lArmed, LNeutralTarget lTarget)
    {
        if (lArmed)
        {
            QViewerNeutralStart(lTarget);
            return;
        }

        if (!QViewerNeutralArmed)
        {
            return;
        }

        qViewerSerial++;
        QViewerNeutralReset();
    }

    public void QViewerNeutralCancel() => QViewerToolSet(false, qViewerTarget);

    public bool QViewerKeyHandle(string lKey)
    {
        if (!QViewerNeutralArmed || lKey != "Escape")
        {
            return false;
        }

        QViewerNeutralCancel();
        return true;
    }

    public void QViewerPressHandle(double lX, double lY)
    {
        if (qViewer.QViewerMediaInfo is not { LMediaVideoPresent: true } lMediaInfo)
        {
            return;
        }

        int lSourceWidth = lMediaInfo.LMediaVideoWidth;
        int lSourceHeight = lMediaInfo.LMediaVideoHeight;
        if (lSourceWidth <= 0 || lSourceHeight <= 0)
        {
            return;
        }

        LCropbox lDisplay = qViewer.QCrop.QCropVideoRead();
        LCropboxSize lShown = qViewer.QCrop.QCropDisplayRead();
        LRotateFlip lRotate = qViewer.QViewerPreview.LRotateFlip;
        LNeutralPoint lPoint = LNeutral.LNeutralPointResolve(
            lX, lY,
            lDisplay.LCropboxX, lDisplay.LCropboxY, lDisplay.LCropboxWidth, lDisplay.LCropboxHeight,
            0, 0, lShown.LCropboxSizeWidth, lShown.LCropboxSizeHeight,
            lRotate.LRotateKind,
            lRotate.LRotateFlipHorizontal,
            lRotate.LRotateFlipVertical,
            lSourceWidth, lSourceHeight);
        if (!lPoint.LNeutralPointInside)
        {
            return;
        }

        string? lPath = qViewer.QViewerSourcePath;
        if (string.IsNullOrWhiteSpace(lPath))
        {
            return;
        }

        TimeSpan lTime = qViewer.QViewerTimeRead();
        int lLoadClaim = qViewer.QViewerLoadSerial;
        int lNeutralClaim = qViewerSerial;
        LNeutralTarget lTarget = qViewerTarget;
        QViewerNeutralReset();
        QViewerSampleStart(
            lPath, lTime, lSourceWidth, lSourceHeight,
            lPoint.LNeutralPointX, lPoint.LNeutralPointY, lLoadClaim, lNeutralClaim, lTarget);
    }

    public void QViewerEstimateStart(LWhitebalanceMethod lMethod) =>
        qViewer.QViewerFrameRead(lFrame => QViewerEstimateChanged?.Invoke(lFrame is null
            ? new LNeutralWheel(0, 0, false)
            : LNeutral.LNeutralAnalyzeResolve(
                lFrame.LMediaFramePixels, lFrame.LMediaFrameWidth, lFrame.LMediaFrameHeight, lMethod)));

    private void QViewerNeutralStart(LNeutralTarget lTarget)
    {
        if (QViewerNeutralArmed)
        {
            qViewerTarget = lTarget;
            return;
        }

        qViewerSerial++;
        qViewerTarget = lTarget;
        qViewerPlayingHeld = qViewer.QViewerPlaying;
        qViewerTool = QViewerTool.QViewerToolNeutral;
        if (qViewerPlayingHeld)
        {
            qViewer.QViewerPlayback.QViewerPause();
        }

        QViewerFocusApplied?.Invoke();
        QViewerToolRaise(true);
    }

    private void QViewerNeutralReset()
    {
        qViewer.QCropDrag.QCropCaptureRelease();
        qViewerTool = QViewerTool.QViewerToolNone;
        bool lResume = qViewerPlayingHeld;
        qViewerPlayingHeld = false;
        if (lResume)
        {
            qViewer.QViewerPlayback.QViewerPlay();
        }

        QViewerToolRaise(false);
    }

    private void QViewerToolRaise(bool lArmed)
    {
        qViewer.QCrop.QCropOverlayUpdate();
        QViewerToolChanged?.Invoke(lArmed, qViewerTarget);
    }

    private async void QViewerSampleStart(
        string lPath,
        TimeSpan lPosition,
        int lWidth,
        int lHeight,
        int lPixelX,
        int lPixelY,
        int lLoadClaim,
        int lNeutralClaim,
        LNeutralTarget lTarget)
    {
        LMediaFrame? lFrame = await LMedia.LMediaFrameStart(lPath, lPosition, lWidth, lHeight);
        if (qViewer.QViewerUnloaded
            || lLoadClaim != qViewer.QViewerLoadSerial
            || lNeutralClaim != qViewerSerial)
        {
            return;
        }

        QViewerNeutralChanged?.Invoke(lFrame is null
            ? new LNeutralSample(LNeutralOutcome.LNeutralOutcomeDecode, 0, 0, 0, 1, 1, 1)
            : LNeutral.LNeutralResolve(
                lFrame.LMediaFramePixels, lFrame.LMediaFrameWidth, lFrame.LMediaFrameHeight,
                lPixelX, lPixelY, lTarget));
    }
}
