namespace Cadroue.Application;

public sealed partial record LCropbox
{
    public static LCropbox? LCropboxRatioResolve(LCropbox lCropboxBounds, int lCropboxRatioWidth, int lCropboxRatioHeight)
    {
        if (lCropboxBounds.LCropboxWidth <= 0 || lCropboxBounds.LCropboxHeight <= 0
            || lCropboxRatioWidth <= 0 || lCropboxRatioHeight <= 0)
        {
            return null;
        }

        int lCropboxDivisor = LCropboxDivisorResolve(lCropboxRatioWidth, lCropboxRatioHeight);
        int lCropboxUnitWidth = lCropboxRatioWidth / lCropboxDivisor;
        int lCropboxUnitHeight = lCropboxRatioHeight / lCropboxDivisor;
        int lCropboxScale = (int)Math.Floor(Math.Min(
            lCropboxBounds.LCropboxWidth / lCropboxUnitWidth,
            lCropboxBounds.LCropboxHeight / lCropboxUnitHeight));

        while (lCropboxScale > 0
            && ((lCropboxScale * lCropboxUnitWidth % 2) != 0 || (lCropboxScale * lCropboxUnitHeight % 2) != 0))
        {
            lCropboxScale--;
        }

        if (lCropboxScale <= 0)
        {
            return null;
        }

        double lCropboxWidth = lCropboxScale * lCropboxUnitWidth;
        double lCropboxHeight = lCropboxScale * lCropboxUnitHeight;
        double lCropboxMinimumX = Math.Ceiling(lCropboxBounds.LCropboxX / 2) * 2;
        double lCropboxMinimumY = Math.Ceiling(lCropboxBounds.LCropboxY / 2) * 2;
        double lCropboxMaximumX = LCropboxEvenNormalize(lCropboxBounds.LCropboxRight - lCropboxWidth);
        double lCropboxMaximumY = LCropboxEvenNormalize(lCropboxBounds.LCropboxBottom - lCropboxHeight);
        if (lCropboxMaximumX < lCropboxMinimumX || lCropboxMaximumY < lCropboxMinimumY)
        {
            return null;
        }

        double lCropboxX = Math.Clamp(
            LCropboxEvenNormalize(lCropboxBounds.LCropboxX + ((lCropboxBounds.LCropboxWidth - lCropboxWidth) / 2)),
            lCropboxMinimumX,
            lCropboxMaximumX);
        double lCropboxY = Math.Clamp(
            LCropboxEvenNormalize(lCropboxBounds.LCropboxY + ((lCropboxBounds.LCropboxHeight - lCropboxHeight) / 2)),
            lCropboxMinimumY,
            lCropboxMaximumY);
        return new LCropbox(lCropboxX, lCropboxY, lCropboxWidth, lCropboxHeight);
    }

    public static LCropbox? LCropboxAnchorResolve(
        LCropbox lCropboxDesired,
        LCropbox lCropboxBounds,
        int lCropboxRatioWidth,
        int lCropboxRatioHeight,
        int lCropboxDriveAxis,
        int lCropboxAnchorX,
        int lCropboxAnchorY)
    {
        if (lCropboxDesired.LCropboxWidth <= 0 || lCropboxDesired.LCropboxHeight <= 0
            || lCropboxBounds.LCropboxWidth <= 0 || lCropboxBounds.LCropboxHeight <= 0
            || lCropboxRatioWidth <= 0 || lCropboxRatioHeight <= 0)
        {
            return null;
        }

        int lCropboxDivisor = LCropboxDivisorResolve(lCropboxRatioWidth, lCropboxRatioHeight);
        int lCropboxUnitWidth = lCropboxRatioWidth / lCropboxDivisor;
        int lCropboxUnitHeight = lCropboxRatioHeight / lCropboxDivisor;

        double lCropboxScaleRaw = lCropboxDriveAxis switch
        {
            0 => lCropboxDesired.LCropboxWidth / lCropboxUnitWidth,
            1 => lCropboxDesired.LCropboxHeight / lCropboxUnitHeight,
            _ => Math.Min(
                lCropboxDesired.LCropboxWidth / lCropboxUnitWidth,
                lCropboxDesired.LCropboxHeight / lCropboxUnitHeight)
        };

        int lCropboxScale = (int)Math.Round(lCropboxScaleRaw);
        while (lCropboxScale > 0)
        {
            double lCropboxWidth = lCropboxScale * lCropboxUnitWidth;
            double lCropboxHeight = lCropboxScale * lCropboxUnitHeight;
            if ((lCropboxWidth % 2) != 0 || (lCropboxHeight % 2) != 0)
            {
                lCropboxScale--;
                continue;
            }

            double lCropboxMaximumX = LCropboxEvenNormalize(lCropboxBounds.LCropboxWidth - lCropboxWidth);
            double lCropboxMaximumY = LCropboxEvenNormalize(lCropboxBounds.LCropboxHeight - lCropboxHeight);
            if (lCropboxMaximumX < 0 || lCropboxMaximumY < 0)
            {
                lCropboxScale--;
                continue;
            }

            double lCropboxX = Math.Clamp(
                LCropboxEvenNormalize(LCropboxAnchorPlace(lCropboxDesired.LCropboxX, lCropboxDesired.LCropboxWidth, lCropboxWidth, lCropboxAnchorX)),
                0,
                lCropboxMaximumX);
            double lCropboxY = Math.Clamp(
                LCropboxEvenNormalize(LCropboxAnchorPlace(lCropboxDesired.LCropboxY, lCropboxDesired.LCropboxHeight, lCropboxHeight, lCropboxAnchorY)),
                0,
                lCropboxMaximumY);
            return new LCropbox(lCropboxX, lCropboxY, lCropboxWidth, lCropboxHeight);
        }

        return null;
    }

    public static (int Width, int Height) LCropboxRatioNormalize(int lCropboxRatioWidth, int lCropboxRatioHeight)
    {
        int lCropboxDivisor = LCropboxDivisorResolve(lCropboxRatioWidth, lCropboxRatioHeight);
        return (lCropboxRatioWidth / lCropboxDivisor, lCropboxRatioHeight / lCropboxDivisor);
    }

    public static (int Excess, bool Wide) LCropboxExcessResolve(
        double lCropboxCropWidth,
        double lCropboxCropHeight,
        double lCropboxRatioWidth,
        double lCropboxRatioHeight)
    {
        double lCropboxWide = lCropboxCropWidth * lCropboxRatioHeight;
        double lCropboxTall = lCropboxCropHeight * lCropboxRatioWidth;
        bool lCropboxIsWide = lCropboxWide > lCropboxTall;
        double lCropboxExcess = lCropboxIsWide
            ? lCropboxCropWidth - (lCropboxCropHeight * lCropboxRatioWidth / lCropboxRatioHeight)
            : lCropboxCropHeight - (lCropboxCropWidth * lCropboxRatioHeight / lCropboxRatioWidth);
        return (LCropboxEvenCeilResolve(lCropboxExcess), lCropboxIsWide);
    }

    public static (double Left, double Top, double Right, double Bottom)? LCropboxLockResolve(
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

            double lCropboxTargetHeight = LCropboxEvenNormalize(lCropboxWidth * lCropboxRatioHeight / lCropboxRatioWidth);
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

            return (lCropboxLeft, lCropboxNewTop, lCropboxRight, lCropboxNewBottom);
        }

        if (lCropboxLeftLock && lCropboxRightLock)
        {
            return null;
        }

        double lCropboxTargetWidth = LCropboxEvenNormalize(lCropboxHeight * lCropboxRatioWidth / lCropboxRatioHeight);
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

        return (lCropboxNewLeft, lCropboxTop, lCropboxNewRight, lCropboxBottom);
    }

    public static double LCropboxEvenNormalize(double lCropboxValue)
    {
        int lCropboxWhole = (int)Math.Floor(lCropboxValue);
        return lCropboxWhole <= 0 ? 0 : lCropboxWhole - (lCropboxWhole % 2);
    }

    public static int LCropboxDivisorResolve(int lCropboxFirst, int lCropboxSecond)
    {
        while (lCropboxSecond != 0)
        {
            (lCropboxFirst, lCropboxSecond) = (lCropboxSecond, lCropboxFirst % lCropboxSecond);
        }

        return lCropboxFirst == 0 ? 1 : lCropboxFirst;
    }

    private static double LCropboxAnchorPlace(double lCropboxOrigin, double lCropboxDesiredSize, double lCropboxSize, int lCropboxAnchor) => lCropboxAnchor switch
    {
        < 0 => lCropboxOrigin,
        > 0 => lCropboxOrigin + lCropboxDesiredSize - lCropboxSize,
        _ => lCropboxOrigin + ((lCropboxDesiredSize - lCropboxSize) / 2)
    };

    private static int LCropboxEvenCeilResolve(double lCropboxExcess)
    {
        if (lCropboxExcess < 1)
        {
            return 0;
        }

        int lCropboxWhole = (int)Math.Ceiling(lCropboxExcess - 0.001);
        return lCropboxWhole + (lCropboxWhole % 2);
    }
}
