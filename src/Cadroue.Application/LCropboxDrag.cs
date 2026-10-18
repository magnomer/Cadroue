namespace Cadroue.Application;

public static class LCropboxDrag
{
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
        double lCropboxY = lCropboxEdgeY < 0
            ? lCropboxOrigin.LCropboxBottom - lCropboxHeight
            : lCropboxOrigin.LCropboxY;

        if (lCropboxEdgeX == 0)
        {
            lCropboxX = lCropboxOrigin.LCropboxX + ((lCropboxOrigin.LCropboxWidth - lCropboxWidth) / 2);
        }

        if (lCropboxEdgeY == 0)
        {
            lCropboxY = lCropboxOrigin.LCropboxY + ((lCropboxOrigin.LCropboxHeight - lCropboxHeight) / 2);
        }

        return LCropboxDisplay.LCropboxRectClamp(
            new LCropbox(lCropboxX, lCropboxY, lCropboxWidth, lCropboxHeight),
            lCropboxVideo,
            lCropboxRatioLocked);
    }
}
