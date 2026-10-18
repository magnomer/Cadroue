using Cadroue.Core;

namespace Cadroue.Application;

public static class LCropboxAspect
{
    public const double LCropboxTolerance = 0.01;

    public static LCropbox? LCropboxFitResolve(
        LCropbox lCropboxDesired,
        LCropbox lCropboxBounds,
        LCropboxRatio lCropboxRatio,
        int lCropboxDriveAxis,
        int lCropboxAnchorX,
        int lCropboxAnchorY)
    {
        if (!lCropboxRatio.LCropboxRatioFixed
            || lCropboxDesired.LCropboxWidth <= 0 || lCropboxDesired.LCropboxHeight <= 0
            || lCropboxBounds.LCropboxWidth <= 0 || lCropboxBounds.LCropboxHeight <= 0
            || lCropboxRatio.LCropboxRatioWidth <= 0 || lCropboxRatio.LCropboxRatioHeight <= 0)
        {
            return null;
        }

        return lCropboxRatio.LCropboxRatioLenient
            ? LCropboxLenientResolve(
                lCropboxDesired,
                lCropboxBounds,
                lCropboxRatio.LCropboxRatioWidth,
                lCropboxRatio.LCropboxRatioHeight,
                lCropboxDriveAxis,
                lCropboxAnchorX,
                lCropboxAnchorY,
                LCropboxTolerance)
            : LCropboxAnchorResolve(
                lCropboxDesired,
                lCropboxBounds,
                lCropboxRatio.LCropboxRatioWidth,
                lCropboxRatio.LCropboxRatioHeight,
                lCropboxDriveAxis,
                lCropboxAnchorX,
                lCropboxAnchorY);
    }

    public static bool LCropboxToleranceCheck(
        double lCropboxCropWidth,
        double lCropboxCropHeight,
        double lCropboxRatioWidth,
        double lCropboxRatioHeight) =>
        LCropboxErrorResolve(lCropboxCropWidth, lCropboxCropHeight, lCropboxRatioWidth, lCropboxRatioHeight)
            <= LCropboxTolerance;

    public static LCropbox? LCropboxRatioResolve(
        LCropbox lCropboxBounds,
        int lCropboxRatioWidth,
        int lCropboxRatioHeight)
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
        double lCropboxMaximumX = LCropboxFloorNormalize(lCropboxBounds.LCropboxRight - lCropboxWidth);
        double lCropboxMaximumY = LCropboxFloorNormalize(lCropboxBounds.LCropboxBottom - lCropboxHeight);
        if (lCropboxMaximumX < lCropboxMinimumX || lCropboxMaximumY < lCropboxMinimumY)
        {
            return null;
        }

        double lCropboxX = Math.Clamp(
            LCropboxFloorNormalize(lCropboxBounds.LCropboxX + ((lCropboxBounds.LCropboxWidth - lCropboxWidth) / 2)),
            lCropboxMinimumX,
            lCropboxMaximumX);
        double lCropboxY = Math.Clamp(
            LCropboxFloorNormalize(lCropboxBounds.LCropboxY + ((lCropboxBounds.LCropboxHeight - lCropboxHeight) / 2)),
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

            double lCropboxMaximumX = LCropboxFloorNormalize(lCropboxBounds.LCropboxWidth - lCropboxWidth);
            double lCropboxMaximumY = LCropboxFloorNormalize(lCropboxBounds.LCropboxHeight - lCropboxHeight);
            if (lCropboxMaximumX < 0 || lCropboxMaximumY < 0)
            {
                lCropboxScale--;
                continue;
            }

            double lCropboxX = Math.Clamp(
                LCropboxFloorNormalize(
                    LCropboxAnchorPlace(
                        lCropboxDesired.LCropboxX,
                        lCropboxDesired.LCropboxWidth,
                        lCropboxWidth,
                        lCropboxAnchorX)),
                0,
                lCropboxMaximumX);
            double lCropboxY = Math.Clamp(
                LCropboxFloorNormalize(
                    LCropboxAnchorPlace(
                        lCropboxDesired.LCropboxY,
                        lCropboxDesired.LCropboxHeight,
                        lCropboxHeight,
                        lCropboxAnchorY)),
                0,
                lCropboxMaximumY);
            return new LCropbox(lCropboxX, lCropboxY, lCropboxWidth, lCropboxHeight);
        }

        return null;
    }

    public static LCropbox? LCropboxLenientResolve(
        LCropbox lCropboxDesired,
        LCropbox lCropboxBounds,
        int lCropboxRatioWidth,
        int lCropboxRatioHeight,
        int lCropboxDriveAxis,
        int lCropboxAnchorX,
        int lCropboxAnchorY,
        double lCropboxTolerance)
    {
        if (lCropboxDesired.LCropboxWidth <= 0 || lCropboxDesired.LCropboxHeight <= 0
            || lCropboxBounds.LCropboxWidth <= 0 || lCropboxBounds.LCropboxHeight <= 0
            || lCropboxRatioWidth <= 0 || lCropboxRatioHeight <= 0)
        {
            return null;
        }

        int lCropboxDivisor = LCropboxDivisorResolve(lCropboxRatioWidth, lCropboxRatioHeight);
        double lCropboxUnitWidth = lCropboxRatioWidth / lCropboxDivisor;
        double lCropboxUnitHeight = lCropboxRatioHeight / lCropboxDivisor;
        double lCropboxRatio = lCropboxUnitWidth / lCropboxUnitHeight;

        double lCropboxMaximumWidth = LCropboxFloorNormalize(lCropboxBounds.LCropboxWidth);
        double lCropboxMaximumHeight = LCropboxFloorNormalize(lCropboxBounds.LCropboxHeight);
        if (lCropboxMaximumWidth < 2 || lCropboxMaximumHeight < 2)
        {
            return null;
        }

        bool lCropboxWidthDrive = lCropboxDriveAxis switch
        {
            0 => true,
            1 => false,
            _ => lCropboxDesired.LCropboxWidth / lCropboxUnitWidth
                <= lCropboxDesired.LCropboxHeight / lCropboxUnitHeight
        };

        double lCropboxWidth;
        double lCropboxHeight;
        if (lCropboxWidthDrive)
        {
            lCropboxWidth = Math.Clamp(LCropboxNearNormalize(lCropboxDesired.LCropboxWidth), 2, lCropboxMaximumWidth);
            lCropboxHeight = Math.Clamp(LCropboxNearNormalize(lCropboxWidth / lCropboxRatio), 2, lCropboxMaximumHeight);
        }
        else
        {
            lCropboxHeight = Math.Clamp(
                LCropboxNearNormalize(lCropboxDesired.LCropboxHeight),
                2,
                lCropboxMaximumHeight);
            lCropboxWidth = Math.Clamp(LCropboxNearNormalize(lCropboxHeight * lCropboxRatio), 2, lCropboxMaximumWidth);
        }

        if (LCropboxErrorResolve(lCropboxWidth, lCropboxHeight, lCropboxUnitWidth, lCropboxUnitHeight)
            > lCropboxTolerance)
        {
            return null;
        }

        double lCropboxMaximumX = LCropboxFloorNormalize(lCropboxBounds.LCropboxWidth - lCropboxWidth);
        double lCropboxMaximumY = LCropboxFloorNormalize(lCropboxBounds.LCropboxHeight - lCropboxHeight);
        if (lCropboxMaximumX < 0 || lCropboxMaximumY < 0)
        {
            return null;
        }

        double lCropboxX = Math.Clamp(
            LCropboxFloorNormalize(
                LCropboxAnchorPlace(
                    lCropboxDesired.LCropboxX,
                    lCropboxDesired.LCropboxWidth,
                    lCropboxWidth,
                    lCropboxAnchorX)),
            0,
            lCropboxMaximumX);
        double lCropboxY = Math.Clamp(
            LCropboxFloorNormalize(
                LCropboxAnchorPlace(
                    lCropboxDesired.LCropboxY,
                    lCropboxDesired.LCropboxHeight,
                    lCropboxHeight,
                    lCropboxAnchorY)),
            0,
            lCropboxMaximumY);
        return new LCropbox(lCropboxX, lCropboxY, lCropboxWidth, lCropboxHeight);
    }

    // A drawn box snapped to the ratio inside a source of this size, as insets from the source's sides; a box with no
    // area leaves no insets. The ratio is read only for a box with area.
    public static LCropboxEdges LCropboxDrawnResolve(
        LCropbox? lCropboxDrawn,
        double lCropboxWidth,
        double lCropboxHeight,
        Func<LCropboxRatio> lCropboxRatio,
        int lCropboxDriveAxis,
        int lCropboxAnchorX,
        int lCropboxAnchorY)
    {
        LCropbox? lCropboxSnapped = lCropboxDrawn is { LCropboxWidth: > 0, LCropboxHeight: > 0 } lCropboxDesired
            ? LCropboxFitResolve(
                lCropboxDesired,
                new LCropbox(0, 0, lCropboxWidth, lCropboxHeight),
                lCropboxRatio(),
                lCropboxDriveAxis,
                lCropboxAnchorX,
                lCropboxAnchorY) ?? lCropboxDesired
            : lCropboxDrawn;
        if (lCropboxSnapped is not { LCropboxWidth: > 0, LCropboxHeight: > 0 } lCropboxRect)
        {
            return new LCropboxEdges(0, 0, 0, 0);
        }

        return new LCropboxEdges(
            lCropboxRect.LCropboxX,
            lCropboxRect.LCropboxY,
            lCropboxWidth - lCropboxRect.LCropboxX - lCropboxRect.LCropboxWidth,
            lCropboxHeight - lCropboxRect.LCropboxY - lCropboxRect.LCropboxHeight);
    }

    // The ratio's largest box inside the crop's present extent in a source of this size, from its top left; no extent
    // leaves the crop as it is.
    public static LWorkCrop LCropboxPresetResolve(
        LWorkCrop lCropboxCrop,
        double lCropboxWidth,
        double lCropboxHeight,
        int lCropboxRatioWidth,
        int lCropboxRatioHeight)
    {
        double lCropboxBoundsWidth = lCropboxWidth - lCropboxCrop.LWorkCropLeft - lCropboxCrop.LWorkCropRight;
        double lCropboxBoundsHeight = lCropboxHeight - lCropboxCrop.LWorkCropTop - lCropboxCrop.LWorkCropBottom;
        LCropbox? lCropboxPreset = lCropboxBoundsWidth > 0 && lCropboxBoundsHeight > 0
            ? LCropboxRatioResolve(
                new LCropbox(
                    lCropboxCrop.LWorkCropLeft, lCropboxCrop.LWorkCropTop, lCropboxBoundsWidth, lCropboxBoundsHeight),
                lCropboxRatioWidth,
                lCropboxRatioHeight)
            : null;
        return lCropboxPreset is { } lCropbox
            ? LCropboxInset.LCropboxInsetResolve(lCropboxCrop, new LCropboxEdges(
                lCropbox.LCropboxX,
                lCropbox.LCropboxY,
                lCropboxWidth - lCropbox.LCropboxRight,
                lCropboxHeight - lCropbox.LCropboxBottom))
            : lCropboxCrop;
    }

    public static LCropboxExtent LCropboxRatioNormalize(int lCropboxRatioWidth, int lCropboxRatioHeight)
    {
        int lCropboxDivisor = LCropboxDivisorResolve(lCropboxRatioWidth, lCropboxRatioHeight);
        return new LCropboxExtent(lCropboxRatioWidth / lCropboxDivisor, lCropboxRatioHeight / lCropboxDivisor);
    }

    public static (int, bool) LCropboxExcessResolve(
        double lCropboxCropWidth,
        double lCropboxCropHeight,
        double lCropboxRatioWidth,
        double lCropboxRatioHeight)
    {
        double lCropboxWide = lCropboxCropWidth * lCropboxRatioHeight;
        double lCropboxTall = lCropboxCropHeight * lCropboxRatioWidth;
        bool lCropboxIsWide = lCropboxWide > lCropboxTall;
        double lCropboxCurrent = lCropboxIsWide ? lCropboxCropWidth : lCropboxCropHeight;
        double lCropboxTargetExact = lCropboxIsWide
            ? lCropboxCropHeight * lCropboxRatioWidth / lCropboxRatioHeight
            : lCropboxCropWidth * lCropboxRatioHeight / lCropboxRatioWidth;
        double lCropboxTargetEven = Math.Round(lCropboxTargetExact / 2, MidpointRounding.AwayFromZero) * 2;
        if (lCropboxTargetEven > lCropboxCurrent)
        {
            lCropboxTargetEven = LCropboxFloorNormalize(lCropboxCurrent);
        }

        int lCropboxExcess = (int)Math.Round(lCropboxCurrent - lCropboxTargetEven, MidpointRounding.AwayFromZero);
        return (lCropboxExcess < 0 ? 0 : lCropboxExcess, lCropboxIsWide);
    }

    public static double LCropboxErrorResolve(
        double lCropboxCropWidth,
        double lCropboxCropHeight,
        double lCropboxRatioWidth,
        double lCropboxRatioHeight)
    {
        if (lCropboxCropWidth <= 0 || lCropboxCropHeight <= 0
            || lCropboxRatioWidth <= 0 || lCropboxRatioHeight <= 0)
        {
            return 0;
        }

        double lCropboxTarget = lCropboxRatioWidth / lCropboxRatioHeight;
        double lCropboxActual = lCropboxCropWidth / lCropboxCropHeight;
        return Math.Abs(lCropboxActual - lCropboxTarget) / lCropboxTarget;
    }

    public static double LCropboxFloorNormalize(double lCropboxValue)
    {
        int lCropboxWhole = (int)Math.Floor(lCropboxValue);
        return lCropboxWhole <= 0 ? 0 : lCropboxWhole - (lCropboxWhole % 2);
    }

    public static double LCropboxNearNormalize(double lCropboxValue) =>
        lCropboxValue <= 0 ? 0 : Math.Round(lCropboxValue / 2, MidpointRounding.AwayFromZero) * 2;

    public static int LCropboxDivisorResolve(int lCropboxFirst, int lCropboxSecond)
    {
        while (lCropboxSecond != 0)
        {
            (lCropboxFirst, lCropboxSecond) = (lCropboxSecond, lCropboxFirst % lCropboxSecond);
        }

        return lCropboxFirst == 0 ? 1 : lCropboxFirst;
    }

    private static double LCropboxAnchorPlace(
        double lCropboxOrigin,
        double lCropboxDesiredSize,
        double lCropboxSize,
        int lCropboxAnchor) => lCropboxAnchor switch
    {
        < 0 => lCropboxOrigin,
        > 0 => lCropboxOrigin + lCropboxDesiredSize - lCropboxSize,
        _ => lCropboxOrigin + ((lCropboxDesiredSize - lCropboxSize) / 2)
    };
}
