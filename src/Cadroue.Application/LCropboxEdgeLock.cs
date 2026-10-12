namespace Cadroue.Application;

public sealed class LCropboxEdgeLock
{
    private readonly bool[] lCropboxEdgeLocked = new bool[4];

    public void LCropboxEdgeSet(int lEdge, bool lLocked) => lCropboxEdgeLocked[lEdge] = lLocked;

    public void LCropboxEdgeClear() => Array.Clear(lCropboxEdgeLocked);

    public LCropboxEdges? LCropboxEdgeResolve(
        double lSourceWidth,
        double lSourceHeight,
        LCropboxEdges lInsets,
        double lRatioWidth,
        double lRatioHeight,
        bool lHorizontal) =>
        LCropboxLockResolve(
            lSourceWidth,
            lSourceHeight,
            lInsets.LCropboxLeft,
            lInsets.LCropboxTop,
            lInsets.LCropboxRight,
            lInsets.LCropboxBottom,
            lCropboxEdgeLocked[0],
            lCropboxEdgeLocked[1],
            lCropboxEdgeLocked[2],
            lCropboxEdgeLocked[3],
            lRatioWidth,
            lRatioHeight,
            lHorizontal);

    private static LCropboxEdges? LCropboxLockResolve(
        double lCropboxSourceWidth,
        double lCropboxSourceHeight,
        double lCropboxLeft,
        double lCropboxTop,
        double lCropboxRight,
        double lCropboxBottom,
        bool lCropboxLeftLock,
        bool lCropboxTopLock,
        bool lCropboxRightLock,
        bool lCropboxBottomLock,
        double lCropboxRatioWidth,
        double lCropboxRatioHeight,
        bool lCropboxHorizontal)
    {
        double lCropboxWidth = lCropboxSourceWidth - lCropboxLeft - lCropboxRight;
        double lCropboxHeight = lCropboxSourceHeight - lCropboxTop - lCropboxBottom;
        if (lCropboxWidth <= 0 || lCropboxHeight <= 0
            || lCropboxRatioWidth <= 0 || lCropboxRatioHeight <= 0)
        {
            return null;
        }

        if (lCropboxHorizontal)
        {
            if (lCropboxTopLock && lCropboxBottomLock)
            {
                return null;
            }

            double lCropboxTargetHeight = LCropbox.LCropboxFloorNormalize(
                lCropboxWidth * lCropboxRatioHeight / lCropboxRatioWidth);
            if (lCropboxTargetHeight <= 0 || lCropboxTargetHeight > lCropboxSourceHeight)
            {
                return null;
            }

            double lCropboxNewTop = lCropboxBottomLock
                ? lCropboxSourceHeight - lCropboxBottom - lCropboxTargetHeight
                : lCropboxTop;
            double lCropboxNewBottom = lCropboxBottomLock
                ? lCropboxBottom
                : lCropboxSourceHeight - lCropboxTop - lCropboxTargetHeight;
            if (lCropboxNewTop < 0 || lCropboxNewBottom < 0)
            {
                return null;
            }

            return new LCropboxEdges(lCropboxLeft, lCropboxNewTop, lCropboxRight, lCropboxNewBottom);
        }

        if (lCropboxLeftLock && lCropboxRightLock)
        {
            return null;
        }

        double lCropboxTargetWidth = LCropbox.LCropboxFloorNormalize(
            lCropboxHeight * lCropboxRatioWidth / lCropboxRatioHeight);
        if (lCropboxTargetWidth <= 0 || lCropboxTargetWidth > lCropboxSourceWidth)
        {
            return null;
        }

        double lCropboxNewLeft = lCropboxRightLock
            ? lCropboxSourceWidth - lCropboxRight - lCropboxTargetWidth
            : lCropboxLeft;
        double lCropboxNewRight = lCropboxRightLock
            ? lCropboxRight
            : lCropboxSourceWidth - lCropboxLeft - lCropboxTargetWidth;
        if (lCropboxNewLeft < 0 || lCropboxNewRight < 0)
        {
            return null;
        }

        return new LCropboxEdges(lCropboxNewLeft, lCropboxTop, lCropboxNewRight, lCropboxBottom);
    }
}
