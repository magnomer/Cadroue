namespace Cadroue.Application;

public sealed partial record LCropbox
{
    public static (double Width, double Height) LCropboxSourceResolve(
        double lCropboxSourceWidth,
        double lCropboxSourceHeight,
        bool lCropboxRotated) =>
        lCropboxRotated
            ? (lCropboxSourceHeight, lCropboxSourceWidth)
            : (lCropboxSourceWidth, lCropboxSourceHeight);

    public static LCropbox LCropboxDisplayResolve(
        double lCropboxSourceWidth,
        double lCropboxSourceHeight,
        double lCropboxAreaWidth,
        double lCropboxAreaHeight)
    {
        if (lCropboxSourceWidth <= 0 || lCropboxSourceHeight <= 0
            || lCropboxAreaWidth <= 0 || lCropboxAreaHeight <= 0)
        {
            return new LCropbox(0, 0, Math.Max(0, lCropboxAreaWidth), Math.Max(0, lCropboxAreaHeight));
        }

        double lCropboxScale = Math.Min(
            lCropboxAreaWidth / lCropboxSourceWidth,
            lCropboxAreaHeight / lCropboxSourceHeight);
        double lCropboxWidth = lCropboxSourceWidth * lCropboxScale;
        double lCropboxHeight = lCropboxSourceHeight * lCropboxScale;
        return new LCropbox(
            (lCropboxAreaWidth - lCropboxWidth) / 2,
            (lCropboxAreaHeight - lCropboxHeight) / 2,
            lCropboxWidth,
            lCropboxHeight);
    }

    public static (double X, double Y) LCropboxPointClamp(
        double lCropboxPointX,
        double lCropboxPointY,
        LCropbox lCropboxVideo)
    {
        double lCropboxX = Math.Max(lCropboxVideo.LCropboxX, Math.Min(lCropboxVideo.LCropboxRight, lCropboxPointX));
        double lCropboxY = Math.Max(lCropboxVideo.LCropboxY, Math.Min(lCropboxVideo.LCropboxBottom, lCropboxPointY));
        return (lCropboxX, lCropboxY);
    }

    public static LCropbox LCropboxRectClamp(LCropbox lCropboxRect, LCropbox lCropboxVideo, bool lCropboxRatioLocked)
    {
        double lCropboxWidth = Math.Min(lCropboxRect.LCropboxWidth, lCropboxVideo.LCropboxWidth);
        double lCropboxHeight = Math.Min(lCropboxRect.LCropboxHeight, lCropboxVideo.LCropboxHeight);

        if (lCropboxRatioLocked)
        {
            double lCropboxScale = Math.Min(
                lCropboxWidth / lCropboxRect.LCropboxWidth,
                lCropboxHeight / lCropboxRect.LCropboxHeight);
            lCropboxWidth = lCropboxRect.LCropboxWidth * lCropboxScale;
            lCropboxHeight = lCropboxRect.LCropboxHeight * lCropboxScale;
        }

        double lCropboxX = Math.Clamp(
            lCropboxRect.LCropboxX,
            lCropboxVideo.LCropboxX,
            Math.Max(lCropboxVideo.LCropboxX, lCropboxVideo.LCropboxRight - lCropboxWidth));
        double lCropboxY = Math.Clamp(
            lCropboxRect.LCropboxY,
            lCropboxVideo.LCropboxY,
            Math.Max(lCropboxVideo.LCropboxY, lCropboxVideo.LCropboxBottom - lCropboxHeight));
        return new LCropbox(lCropboxX, lCropboxY, lCropboxWidth, lCropboxHeight);
    }

    public static LCropbox LCropboxPixelResolve(
        LCropbox lCropboxOverlay,
        LCropbox lCropboxVideo,
        double lCropboxDisplayWidth,
        double lCropboxDisplayHeight)
    {
        double lCropboxX = (lCropboxOverlay.LCropboxX - lCropboxVideo.LCropboxX)
            / lCropboxVideo.LCropboxWidth * lCropboxDisplayWidth;
        double lCropboxY = (lCropboxOverlay.LCropboxY - lCropboxVideo.LCropboxY)
            / lCropboxVideo.LCropboxHeight * lCropboxDisplayHeight;
        double lCropboxWidth = lCropboxOverlay.LCropboxWidth / lCropboxVideo.LCropboxWidth * lCropboxDisplayWidth;
        double lCropboxHeight = lCropboxOverlay.LCropboxHeight / lCropboxVideo.LCropboxHeight * lCropboxDisplayHeight;
        return new LCropbox(lCropboxX, lCropboxY, lCropboxWidth, lCropboxHeight);
    }

    public static LCropbox LCropboxOverlayResolve(
        LCropbox lCropboxSource,
        LCropbox lCropboxVideo,
        double lCropboxDisplayWidth,
        double lCropboxDisplayHeight)
    {
        return new LCropbox(
            lCropboxVideo.LCropboxX + (lCropboxSource.LCropboxX / lCropboxDisplayWidth * lCropboxVideo.LCropboxWidth),
            lCropboxVideo.LCropboxY + (lCropboxSource.LCropboxY / lCropboxDisplayHeight * lCropboxVideo.LCropboxHeight),
            lCropboxSource.LCropboxWidth / lCropboxDisplayWidth * lCropboxVideo.LCropboxWidth,
            lCropboxSource.LCropboxHeight / lCropboxDisplayHeight * lCropboxVideo.LCropboxHeight);
    }

    public static LCropbox LCropboxDrawResolve(
        double lCropboxStartX,
        double lCropboxStartY,
        double lCropboxEndX,
        double lCropboxEndY,
        double lCropboxRatioWidth,
        double lCropboxRatioHeight)
    {
        double lCropboxWidth = Math.Abs(lCropboxStartX - lCropboxEndX);
        double lCropboxHeight = Math.Abs(lCropboxStartY - lCropboxEndY);

        if (lCropboxRatioWidth > 0 && lCropboxRatioHeight > 0)
        {
            if (lCropboxWidth * lCropboxRatioHeight > lCropboxHeight * lCropboxRatioWidth)
            {
                lCropboxWidth = lCropboxHeight * lCropboxRatioWidth / lCropboxRatioHeight;
            }
            else
            {
                lCropboxHeight = lCropboxWidth * lCropboxRatioHeight / lCropboxRatioWidth;
            }
        }

        double lCropboxLeft = lCropboxEndX < lCropboxStartX ? lCropboxStartX - lCropboxWidth : lCropboxStartX;
        double lCropboxTop = lCropboxEndY < lCropboxStartY ? lCropboxStartY - lCropboxHeight : lCropboxStartY;
        return new LCropbox(lCropboxLeft, lCropboxTop, lCropboxWidth, lCropboxHeight);
    }

    public static LCropbox LCropboxMoveResolve(
        LCropbox lCropboxOrigin,
        double lCropboxGrabX,
        double lCropboxGrabY,
        double lCropboxDragX,
        double lCropboxDragY,
        LCropbox lCropboxVideo)
    {
        double lCropboxX = lCropboxOrigin.LCropboxX + (lCropboxDragX - lCropboxGrabX);
        double lCropboxY = lCropboxOrigin.LCropboxY + (lCropboxDragY - lCropboxGrabY);
        lCropboxX = Math.Clamp(
            lCropboxX,
            lCropboxVideo.LCropboxX,
            Math.Max(lCropboxVideo.LCropboxX, lCropboxVideo.LCropboxRight - lCropboxOrigin.LCropboxWidth));
        lCropboxY = Math.Clamp(
            lCropboxY,
            lCropboxVideo.LCropboxY,
            Math.Max(lCropboxVideo.LCropboxY, lCropboxVideo.LCropboxBottom - lCropboxOrigin.LCropboxHeight));
        return new LCropbox(lCropboxX, lCropboxY, lCropboxOrigin.LCropboxWidth, lCropboxOrigin.LCropboxHeight);
    }

    public static LCropbox LCropboxResizeResolve(
        LCropbox lCropboxOrigin,
        double lCropboxDragX,
        double lCropboxDragY,
        int lCropboxEdgeX,
        int lCropboxEdgeY,
        double lCropboxRatioWidth,
        double lCropboxRatioHeight,
        LCropbox lCropboxVideo,
        double lCropboxMinimum)
    {
        double lCropboxLeft = lCropboxEdgeX < 0 ? lCropboxDragX : lCropboxOrigin.LCropboxX;
        double lCropboxRight = lCropboxEdgeX > 0 ? lCropboxDragX : lCropboxOrigin.LCropboxRight;
        double lCropboxTop = lCropboxEdgeY < 0 ? lCropboxDragY : lCropboxOrigin.LCropboxY;
        double lCropboxBottom = lCropboxEdgeY > 0 ? lCropboxDragY : lCropboxOrigin.LCropboxBottom;

        double lCropboxWidth = Math.Max(lCropboxMinimum, Math.Abs(lCropboxRight - lCropboxLeft));
        double lCropboxHeight = Math.Max(lCropboxMinimum, Math.Abs(lCropboxBottom - lCropboxTop));

        bool lCropboxRatioLocked = lCropboxRatioWidth > 0 && lCropboxRatioHeight > 0;
        if (lCropboxRatioLocked)
        {
            if (lCropboxEdgeX != 0 && lCropboxEdgeY != 0)
            {
                if (lCropboxWidth * lCropboxRatioHeight > lCropboxHeight * lCropboxRatioWidth)
                {
                    lCropboxWidth = lCropboxHeight * lCropboxRatioWidth / lCropboxRatioHeight;
                }
                else
                {
                    lCropboxHeight = lCropboxWidth * lCropboxRatioHeight / lCropboxRatioWidth;
                }
            }
            else if (lCropboxEdgeX != 0)
            {
                lCropboxHeight = lCropboxWidth * lCropboxRatioHeight / lCropboxRatioWidth;
            }
            else
            {
                lCropboxWidth = lCropboxHeight * lCropboxRatioWidth / lCropboxRatioHeight;
            }
        }

        double lCropboxX = lCropboxEdgeX < 0 ? lCropboxOrigin.LCropboxRight - lCropboxWidth : lCropboxOrigin.LCropboxX;
        double lCropboxY = lCropboxEdgeY < 0 ? lCropboxOrigin.LCropboxBottom - lCropboxHeight : lCropboxOrigin.LCropboxY;

        if (lCropboxEdgeX == 0)
        {
            lCropboxX = lCropboxOrigin.LCropboxX + ((lCropboxOrigin.LCropboxWidth - lCropboxWidth) / 2);
        }

        if (lCropboxEdgeY == 0)
        {
            lCropboxY = lCropboxOrigin.LCropboxY + ((lCropboxOrigin.LCropboxHeight - lCropboxHeight) / 2);
        }

        return LCropboxRectClamp(new LCropbox(lCropboxX, lCropboxY, lCropboxWidth, lCropboxHeight), lCropboxVideo, lCropboxRatioLocked);
    }
}
