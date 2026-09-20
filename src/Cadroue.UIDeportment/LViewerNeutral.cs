using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public enum LViewerTool
{
    LViewerToolNone,
    LViewerToolCrop,
    LViewerToolNeutral
}

public sealed class LViewerNeutral
{
    private readonly LViewer lViewer;
    private LViewerTool lViewerTool;
    private LNeutralTarget lViewerTarget;
    private int lViewerSerial;
    private bool lViewerPlayingHeld;

    public LViewerNeutral(LViewer lOwner)
    {
        lViewer = lOwner;
    }

    public event Action<bool, LNeutralTarget>? LViewerToolChange;
    public event Action<LNeutralSample>? LViewerNeutralChange;
    public event Action<LNeutralWheel>? LViewerEstimateChange;
    public event Action? LViewerFocusApply;

    public LViewerTool LViewerTool => lViewerTool;

    public LNeutralTarget LViewerNeutralTarget => lViewerTarget;

    public int LViewerNeutralSerial => lViewerSerial;

    public bool LViewerNeutralArmed => lViewerTool == LViewerTool.LViewerToolNeutral;

    public void LViewerToolApply(LViewerTool lTool) => lViewerTool = lTool;

    public void LViewerToolSet(bool lArmed, LNeutralTarget lTarget)
    {
        if (lArmed)
        {
            LViewerNeutralStart(lTarget);
            return;
        }

        if (!LViewerNeutralArmed)
        {
            return;
        }

        lViewerSerial++;
        LViewerNeutralReset();
    }

    public void LViewerNeutralCancel() => LViewerToolSet(false, lViewerTarget);

    public bool LViewerKeyHandle(string lKey)
    {
        if (!LViewerNeutralArmed || lKey != "Escape")
        {
            return false;
        }

        LViewerNeutralCancel();
        return true;
    }

    public void LViewerPressHandle(double lX, double lY)
    {
        if (lViewer.LViewerMediaInfo is not { LMediaVideoPresent: true } lMediaInfo)
        {
            return;
        }

        int lSourceWidth = lMediaInfo.LMediaVideoWidth;
        int lSourceHeight = lMediaInfo.LMediaVideoHeight;
        if (lSourceWidth <= 0 || lSourceHeight <= 0)
        {
            return;
        }

        LCropbox lDisplay = lViewer.LCrop.LCropVideoRead();
        LCropboxSize lShown = lViewer.LCrop.LCropDisplayRead();
        LRotateFlip lRotate = lViewer.LViewerPreview.LRotateFlip;
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

        string? lPath = lViewer.LViewerSourcePath;
        if (string.IsNullOrWhiteSpace(lPath))
        {
            return;
        }

        TimeSpan lTime = lViewer.LViewerTimeRead();
        int lLoadClaim = lViewer.LViewerLoadSerial;
        int lNeutralClaim = lViewerSerial;
        LNeutralTarget lTarget = lViewerTarget;
        LViewerNeutralReset();
        LViewerSampleStart(
            lPath, lTime, lSourceWidth, lSourceHeight,
            lPoint.LNeutralPointX, lPoint.LNeutralPointY, lLoadClaim, lNeutralClaim, lTarget);
    }

    public void LViewerEstimateStart(LWhitebalanceMethod lMethod) =>
        lViewer.LViewerFrameRead(lFrame => LViewerEstimateChange?.Invoke(lFrame is null
            ? new LNeutralWheel(0, 0, false)
            : LNeutral.LNeutralAnalyzeResolve(
                lFrame.LMediaFramePixels, lFrame.LMediaFrameWidth, lFrame.LMediaFrameHeight, lMethod)));

    private void LViewerNeutralStart(LNeutralTarget lTarget)
    {
        if (LViewerNeutralArmed)
        {
            lViewerTarget = lTarget;
            return;
        }

        lViewerSerial++;
        lViewerTarget = lTarget;
        lViewerPlayingHeld = lViewer.LViewerPlaying;
        lViewerTool = LViewerTool.LViewerToolNeutral;
        if (lViewerPlayingHeld)
        {
            lViewer.LViewerPlayback.LViewerPause();
        }

        LViewerFocusApply?.Invoke();
        LViewerToolRaise(true);
    }

    private void LViewerNeutralReset()
    {
        lViewer.LCropDrag.LCropCaptureRelease();
        lViewerTool = LViewerTool.LViewerToolNone;
        bool lResume = lViewerPlayingHeld;
        lViewerPlayingHeld = false;
        if (lResume)
        {
            lViewer.LViewerPlayback.LViewerPlay();
        }

        LViewerToolRaise(false);
    }

    private void LViewerToolRaise(bool lArmed)
    {
        lViewer.LCrop.LCropOverlayUpdate();
        LViewerToolChange?.Invoke(lArmed, lViewerTarget);
    }

    private async void LViewerSampleStart(
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
        if (lViewer.LViewerUnloaded
            || lLoadClaim != lViewer.LViewerLoadSerial
            || lNeutralClaim != lViewerSerial)
        {
            return;
        }

        LViewerNeutralChange?.Invoke(lFrame is null
            ? new LNeutralSample(LNeutralOutcome.LNeutralOutcomeDecode, 0, 0, 0, 1, 1, 1)
            : LNeutral.LNeutralResolve(
                lFrame.LMediaFramePixels, lFrame.LMediaFrameWidth, lFrame.LMediaFrameHeight,
                lPixelX, lPixelY, lTarget));
    }
}
