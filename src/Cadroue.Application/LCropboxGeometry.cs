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
}
