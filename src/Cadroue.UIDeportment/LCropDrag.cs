using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed class LCropDrag
{
    private const double LCropSizeMinimum = 8;

    private static readonly int[] lCropGripX = [-1, 0, 1, 1, 1, 0, -1, -1];
    private static readonly int[] lCropGripY = [-1, -1, -1, 0, 1, 1, 1, 0];

    private readonly LViewer lViewer;
    private double lCropOriginX;
    private double lCropOriginY;
    private double lCropOriginWidth;
    private double lCropOriginHeight;
    private double lCropGrabX;
    private double lCropGrabY;
    private bool lCropPress;
    private double lCropPointX;
    private double lCropPointY;
    private bool lCropPointActive;

    public LCropDrag(LViewer lOwner)
    {
        lViewer = lOwner;
    }

    public event Action<bool>? LCropCaptureApply;

    public bool LCropPress => lCropPress;

    public bool LCropPointActive => lCropPointActive;

    private LCrop LCrop => lViewer.LCrop;

    public void LCropCaptureRelease() => LCropCaptureApply?.Invoke(false);

    public void LCropDragClear()
    {
        lCropPress = false;
        lCropPointActive = false;
        LCropCaptureApply?.Invoke(false);
    }

    public bool LCropGripHandle(int lIndex, double lX, double lY)
    {
        if (!LCrop.LCropEditable)
        {
            return false;
        }

        LCrop.LCropGripSet(lCropGripX[lIndex], lCropGripY[lIndex]);
        LCropDragStart(lX, lY);
        return true;
    }

    public bool LCropBodyHandle(double lX, double lY)
    {
        if (!LCrop.LCropEditable || !LCrop.LCropBoxShown)
        {
            return false;
        }

        LCrop.LCropBodySet();
        LCropDragStart(lX, lY);
        return true;
    }

    public bool LCropPressHandle(double lX, double lY)
    {
        LViewerNeutral lNeutral = lViewer.LViewerNeutral;
        if (lNeutral.LViewerTool == LViewerTool.LViewerToolNeutral)
        {
            lNeutral.LViewerPressHandle(lX, lY);
            return true;
        }

        if (lNeutral.LViewerTool != LViewerTool.LViewerToolCrop
            || !LCrop.LCropActive
            || LCrop.LCropLocked
            || !lViewer.LViewerVideoPresent)
        {
            return false;
        }

        lCropPointX = lX;
        lCropPointY = lY;
        lCropPointActive = true;
        LCrop.LCropDrawSet();
        LCrop.LCropShownSet(true);
        LCropCaptureApply?.Invoke(true);
        LCropBoxPlace(lX, lY, lX, lY);
        return true;
    }

    public bool LCropMoveHandle(bool lPressed, double lX, double lY)
    {
        if (!lPressed)
        {
            return false;
        }

        if (lCropPress)
        {
            LCropDragApply(lX, lY);
            return true;
        }

        if (!lCropPointActive)
        {
            return false;
        }

        LCropBoxPlace(lCropPointX, lCropPointY, lX, lY);
        return true;
    }

    public bool LCropReleaseHandle(double lX, double lY)
    {
        if (lCropPress)
        {
            LCropDragApply(lX, lY);
            lCropPress = false;
            LCropCaptureApply?.Invoke(false);
            LCrop.LCropVideoCommit();
            return true;
        }

        if (!lCropPointActive)
        {
            return false;
        }

        LCropBoxPlace(lCropPointX, lCropPointY, lX, lY);
        lCropPointActive = false;
        LCropCaptureApply?.Invoke(false);
        LCrop.LCropOverlayUpdate();
        LCrop.LCropVideoCommit();
        return true;
    }

    private void LCropBoxPlace(double lStartX, double lStartY, double lEndX, double lEndY)
    {
        LCropbox lVideo = LCrop.LCropVideoRead();
        LCropboxPoint lStart = LCropbox.LCropboxPointClamp(lStartX, lStartY, lVideo);
        LCropboxPoint lEnd = LCropbox.LCropboxPointClamp(lEndX, lEndY, lVideo);
        LCrop.LCropBoxSet(LCropbox.LCropboxDrawResolve(
            lStart.LCropboxPointX, lStart.LCropboxPointY, lEnd.LCropboxPointX, lEnd.LCropboxPointY,
            LCrop.LCropRatioWidth, LCrop.LCropRatioHeight));
        LCrop.LCropOverlayUpdate();
    }

    private void LCropDragStart(double lGrabX, double lGrabY)
    {
        LCropBox lBox = LCrop.LCropBoxRead();
        lCropOriginX = lBox.LCropBoxX;
        lCropOriginY = lBox.LCropBoxY;
        lCropOriginWidth = lBox.LCropBoxWidth;
        lCropOriginHeight = lBox.LCropBoxHeight;
        lCropGrabX = lGrabX;
        lCropGrabY = lGrabY;
        lCropPress = true;
        LCropCaptureApply?.Invoke(true);
    }

    private void LCropDragApply(double lDragX, double lDragY)
    {
        LCropbox lVideo = LCrop.LCropVideoRead();
        var lOrigin = new LCropbox(lCropOriginX, lCropOriginY, lCropOriginWidth, lCropOriginHeight);
        LCropbox lResult;
        if (LCrop.LCropMoveCheck())
        {
            lResult = LCropbox.LCropboxMoveResolve(lOrigin, lCropGrabX, lCropGrabY, lDragX, lDragY, lVideo);
        }
        else
        {
            LCropboxPoint lClamped = LCropbox.LCropboxPointClamp(lDragX, lDragY, lVideo);
            lResult = LCropbox.LCropboxResizeResolve(
                lOrigin, lClamped.LCropboxPointX, lClamped.LCropboxPointY, LCrop.LCropEdgeX, LCrop.LCropEdgeY,
                LCrop.LCropRatioWidth, LCrop.LCropRatioHeight, lVideo, LCropSizeMinimum);
        }

        LCrop.LCropBoxSet(lResult);
        LCrop.LCropOverlayUpdate();
    }
}
