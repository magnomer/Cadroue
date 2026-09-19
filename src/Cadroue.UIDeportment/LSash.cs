namespace Cadroue.UIDeportment;

public readonly record struct LSashBounds(double LSashLeft, double LSashTop, double LSashWidth, double LSashHeight)
{
    public double LSashRight => LSashLeft + LSashWidth;

    public double LSashBottom => LSashTop + LSashHeight;

    public static LSashBounds LSashCornersCreate(double lLeft, double lTop, double lRight, double lBottom) =>
        new(lLeft, lTop, lRight - lLeft, lBottom - lTop);
}

public readonly record struct LSashLimits(double LSashX, double LSashY, double LSashWidth, double LSashHeight);

public sealed class LSash
{
    public const int LSashNone = 0;
    public const int LSashLeft = 1;
    public const int LSashRight = 2;
    public const int LSashTop = 4;
    public const int LSashBottom = 8;

    private const double LSashOffsetX = 120;
    private const double LSashOffsetY = 16;
    private const double LSashMarginX = 200;
    private const double LSashMarginY = 120;

    private readonly double lSashBorder;
    private bool lSashActive;
    private int lSashDirection;
    private double lSashStartX;
    private double lSashStartY;
    private LSashBounds lSashOrigin;

    public LSash(double lBorder)
    {
        lSashBorder = lBorder;
    }

    public event Action? LSashCaptureStart;
    public event Action? LSashCaptureStop;
    public event Action<bool>? LSashActiveChange;
    public event Action<LSashBounds>? LSashBoundsChange;

    public bool LSashActive => lSashActive;

    public int LSashDirection => lSashDirection;

    public int LSashDirectionResolve(double lX, double lY, double lWidth, double lHeight) =>
        LSashDirectionResolve(lX, lY, lWidth, lHeight, lSashBorder);

    public static int LSashDirectionResolve(double lX, double lY, double lWidth, double lHeight, double lBorder)
    {
        int lDirection = LSashNone;
        if (lX >= 0 && lX < lBorder)
        {
            lDirection |= LSashLeft;
        }

        if (lX <= lWidth && lX > lWidth - lBorder)
        {
            lDirection |= LSashRight;
        }

        if (lY >= 0 && lY < lBorder)
        {
            lDirection |= LSashTop;
        }

        if (lY <= lHeight && lY > lHeight - lBorder)
        {
            lDirection |= LSashBottom;
        }

        return lDirection;
    }

    public bool LSashPressHandle(
        bool lNormal,
        bool lInteractive,
        double lX,
        double lY,
        double lPointerX,
        double lPointerY,
        LSashBounds lBounds)
    {
        int lDirection = LSashDirectionResolve(lX, lY, lBounds.LSashWidth, lBounds.LSashHeight);
        if (!lNormal || lDirection == LSashNone || lInteractive)
        {
            return false;
        }

        lSashActive = true;
        lSashDirection = lDirection;
        lSashStartX = lPointerX;
        lSashStartY = lPointerY;
        lSashOrigin = lBounds;
        LSashActiveChange?.Invoke(true);
        LSashCaptureStart?.Invoke();
        return true;
    }

    public bool LSashMoveHandle(
        bool lNormal,
        bool lInteractive,
        double lX,
        double lY,
        double lWidth,
        double lHeight,
        double lPointerX,
        double lPointerY,
        double lMinimumWidth,
        double lMinimumHeight)
    {
        if (lSashActive)
        {
            LSashBoundsChange?.Invoke(LSashMoveResolve(lPointerX, lPointerY, lMinimumWidth, lMinimumHeight));
            return true;
        }

        lSashDirection = lNormal && !lInteractive
            ? LSashDirectionResolve(lX, lY, lWidth, lHeight)
            : LSashNone;
        return false;
    }

    public int LSashLeaveResolve() => lSashActive ? lSashDirection : LSashNone;

    public bool LSashReleaseHandle()
    {
        if (!lSashActive)
        {
            return false;
        }

        lSashActive = false;
        LSashActiveChange?.Invoke(false);
        LSashCaptureStop?.Invoke();
        return true;
    }

    public void LSashCaptureHandle()
    {
        if (!lSashActive)
        {
            return;
        }

        lSashActive = false;
        LSashActiveChange?.Invoke(false);
    }

    public LSashBounds LSashMoveResolve(double lPointerX, double lPointerY, double lMinimumWidth, double lMinimumHeight)
    {
        double lDx = lPointerX - lSashStartX;
        double lDy = lPointerY - lSashStartY;
        double lLeft = lSashOrigin.LSashLeft;
        double lTop = lSashOrigin.LSashTop;
        double lWidth = lSashOrigin.LSashWidth;
        double lHeight = lSashOrigin.LSashHeight;
        if ((lSashDirection & LSashLeft) != 0)
        {
            lWidth = Math.Max(lMinimumWidth, lSashOrigin.LSashWidth - lDx);
            lLeft = lSashOrigin.LSashRight - lWidth;
        }

        if ((lSashDirection & LSashRight) != 0)
        {
            lWidth = Math.Max(lMinimumWidth, lSashOrigin.LSashWidth + lDx);
        }

        if ((lSashDirection & LSashTop) != 0)
        {
            lHeight = Math.Max(lMinimumHeight, lSashOrigin.LSashHeight - lDy);
            lTop = lSashOrigin.LSashBottom - lHeight;
        }

        if ((lSashDirection & LSashBottom) != 0)
        {
            lHeight = Math.Max(lMinimumHeight, lSashOrigin.LSashHeight + lDy);
        }

        return new LSashBounds(lLeft, lTop, lWidth, lHeight);
    }

    public static LSashBounds LSashBoundsClamp(
        LSashBounds lBounds,
        LSashBounds lWorkArea,
        double lMinimumWidth,
        double lMinimumHeight,
        double lMaximumWidth,
        double lMaximumHeight)
    {
        double lWidthLimit = double.IsFinite(lMaximumWidth)
            ? Math.Min(lMaximumWidth, lWorkArea.LSashWidth)
            : lWorkArea.LSashWidth;
        double lHeightLimit = double.IsFinite(lMaximumHeight)
            ? Math.Min(lMaximumHeight, lWorkArea.LSashHeight)
            : lWorkArea.LSashHeight;
        double lWidth = Math.Min(Math.Max(lBounds.LSashWidth, lMinimumWidth), lWidthLimit);
        double lHeight = Math.Min(Math.Max(lBounds.LSashHeight, lMinimumHeight), lHeightLimit);
        double lLeft = Math.Clamp(lBounds.LSashLeft, lWorkArea.LSashLeft, lWorkArea.LSashRight - lWidth);
        double lTop = Math.Clamp(lBounds.LSashTop, lWorkArea.LSashTop, lWorkArea.LSashBottom - lHeight);
        return new LSashBounds(lLeft, lTop, lWidth, lHeight);
    }

    public static LSashBounds LSashPlacementResolve(
        double? lSavedLeft,
        double? lSavedTop,
        double lSavedWidth,
        double lSavedHeight,
        LSashBounds lCurrent,
        double lMinimumWidth,
        double lMinimumHeight,
        double lWorkLeft,
        double lWorkTop)
    {
        double lLeft = lSavedLeft is double lFiniteLeft && double.IsFinite(lFiniteLeft)
            ? lFiniteLeft
            : double.IsFinite(lCurrent.LSashLeft) ? lCurrent.LSashLeft : lWorkLeft;
        double lTop = lSavedTop is double lFiniteTop && double.IsFinite(lFiniteTop)
            ? lFiniteTop
            : double.IsFinite(lCurrent.LSashTop) ? lCurrent.LSashTop : lWorkTop;
        double lWidth = double.IsFinite(lSavedWidth) && lSavedWidth > 0
            ? lSavedWidth
            : Math.Max(lCurrent.LSashWidth, lMinimumWidth);
        double lHeight = double.IsFinite(lSavedHeight) && lSavedHeight > 0
            ? lSavedHeight
            : Math.Max(lCurrent.LSashHeight, lMinimumHeight);
        return new LSashBounds(lLeft, lTop, lWidth, lHeight);
    }

    public static (double LSashLeft, double LSashTop) LSashDragResolve(
        bool lMaximized,
        double lLeft,
        double lTop,
        double lPointerX,
        double lPointerY,
        double lPointerDipX,
        double lPointerDipY,
        double lActualWidth,
        double lRestoreWidth,
        double lWindowWidth,
        double lBandHeight)
    {
        if (!lMaximized)
        {
            return (lLeft, lTop);
        }

        double lRatio = lActualWidth > 0 ? Math.Clamp(lPointerX / lActualWidth, 0, 1) : 0.5;
        double lWidth = lRestoreWidth > 0 ? lRestoreWidth : lWindowWidth;
        return (lPointerDipX - (lWidth * lRatio), lPointerDipY - Math.Min(lPointerY, lBandHeight / 2));
    }

    public static LSashLimits LSashLimitsResolve(
        double lBoundsLeft,
        double lBoundsTop,
        double lWorkLeft,
        double lWorkTop,
        double lWorkRight,
        double lWorkBottom) =>
        new(lWorkLeft - lBoundsLeft, lWorkTop - lBoundsTop, lWorkRight - lWorkLeft, lWorkBottom - lWorkTop);

    public static (double LSashLeft, double LSashTop) LSashCenterResolve(
        bool lManual,
        LSashBounds lWindow,
        LSashBounds lOwner) =>
        lManual
            ? (lWindow.LSashLeft, lWindow.LSashTop)
            : (
                lOwner.LSashLeft + ((lOwner.LSashWidth - lWindow.LSashWidth) / 2),
                lOwner.LSashTop + ((lOwner.LSashHeight - lWindow.LSashHeight) / 2));

    public static (double LSashLeft, double LSashTop) LSashRelayResolve(
        double lDropLeft,
        double lDropTop,
        double lScreenLeft,
        double lScreenTop,
        double lScreenWidth,
        double lScreenHeight) =>
        (
            Math.Clamp(lDropLeft - LSashOffsetX, lScreenLeft, lScreenLeft + lScreenWidth - LSashMarginX),
            Math.Clamp(lDropTop - LSashOffsetY, lScreenTop, lScreenTop + lScreenHeight - LSashMarginY));
}
