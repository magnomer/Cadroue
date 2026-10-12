namespace Cadroue.UIDeportment;

public readonly record struct QSashBounds(double QSashLeft, double QSashTop, double QSashWidth, double QSashHeight)
{
    public double QSashRight => QSashLeft + QSashWidth;

    public double QSashBottom => QSashTop + QSashHeight;

    public static QSashBounds QSashCornersCreate(double lLeft, double lTop, double lRight, double lBottom) =>
        new(lLeft, lTop, lRight - lLeft, lBottom - lTop);
}

public readonly record struct QSashLimits(double QSashX, double QSashY, double QSashWidth, double QSashHeight);

public sealed class QSash
{
    public const int QSashNone = 0;
    public const int QSashLeft = 1;
    public const int QSashRight = 2;
    public const int QSashTop = 4;
    public const int QSashBottom = 8;

    private const double QSashOffsetX = 120;
    private const double QSashOffsetY = 16;
    private const double QSashMarginX = 200;
    private const double QSashMarginY = 120;

    private readonly double qSashBorder;
    private bool qSashActive;
    private int qSashDirection;
    private double qSashStartX;
    private double qSashStartY;
    private QSashBounds qSashOrigin;

    public QSash(double lBorder)
    {
        qSashBorder = lBorder;
    }

    public event Action? QSashCaptureStarted;
    public event Action? QSashCaptureStopped;
    public event Action<bool>? QSashActiveChanged;
    public event Action<QSashBounds>? QSashBoundsChanged;

    public bool QSashActive => qSashActive;

    public int QSashDirection => qSashDirection;

    public int QSashDirectionResolve(double lX, double lY, double lWidth, double lHeight) =>
        QSashDirectionResolve(lX, lY, lWidth, lHeight, qSashBorder);

    public static int QSashDirectionResolve(double lX, double lY, double lWidth, double lHeight, double lBorder)
    {
        int lDirection = QSashNone;
        if (lX >= 0 && lX < lBorder)
        {
            lDirection |= QSashLeft;
        }

        if (lX <= lWidth && lX > lWidth - lBorder)
        {
            lDirection |= QSashRight;
        }

        if (lY >= 0 && lY < lBorder)
        {
            lDirection |= QSashTop;
        }

        if (lY <= lHeight && lY > lHeight - lBorder)
        {
            lDirection |= QSashBottom;
        }

        return lDirection;
    }

    public bool QSashPressHandle(
        bool lNormal,
        bool lInteractive,
        double lX,
        double lY,
        double lPointerX,
        double lPointerY,
        QSashBounds lBounds)
    {
        int lDirection = QSashDirectionResolve(lX, lY, lBounds.QSashWidth, lBounds.QSashHeight);
        if (!lNormal || lDirection == QSashNone || lInteractive)
        {
            return false;
        }

        qSashActive = true;
        qSashDirection = lDirection;
        qSashStartX = lPointerX;
        qSashStartY = lPointerY;
        qSashOrigin = lBounds;
        QSashActiveChanged?.Invoke(true);
        QSashCaptureStarted?.Invoke();
        return true;
    }

    public bool QSashMoveHandle(
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
        if (qSashActive)
        {
            QSashBoundsChanged?.Invoke(QSashMoveResolve(lPointerX, lPointerY, lMinimumWidth, lMinimumHeight));
            return true;
        }

        qSashDirection = lNormal && !lInteractive
            ? QSashDirectionResolve(lX, lY, lWidth, lHeight)
            : QSashNone;
        return false;
    }

    public int QSashLeaveResolve() => qSashActive ? qSashDirection : QSashNone;

    public bool QSashReleaseHandle()
    {
        if (!qSashActive)
        {
            return false;
        }

        qSashActive = false;
        QSashActiveChanged?.Invoke(false);
        QSashCaptureStopped?.Invoke();
        return true;
    }

    public void QSashCaptureHandle()
    {
        if (!qSashActive)
        {
            return;
        }

        qSashActive = false;
        QSashActiveChanged?.Invoke(false);
    }

    public QSashBounds QSashMoveResolve(double lPointerX, double lPointerY, double lMinimumWidth, double lMinimumHeight)
    {
        double lDx = lPointerX - qSashStartX;
        double lDy = lPointerY - qSashStartY;
        double lLeft = qSashOrigin.QSashLeft;
        double lTop = qSashOrigin.QSashTop;
        double lWidth = qSashOrigin.QSashWidth;
        double lHeight = qSashOrigin.QSashHeight;
        if ((qSashDirection & QSashLeft) != 0)
        {
            lWidth = Math.Max(lMinimumWidth, qSashOrigin.QSashWidth - lDx);
            lLeft = qSashOrigin.QSashRight - lWidth;
        }

        if ((qSashDirection & QSashRight) != 0)
        {
            lWidth = Math.Max(lMinimumWidth, qSashOrigin.QSashWidth + lDx);
        }

        if ((qSashDirection & QSashTop) != 0)
        {
            lHeight = Math.Max(lMinimumHeight, qSashOrigin.QSashHeight - lDy);
            lTop = qSashOrigin.QSashBottom - lHeight;
        }

        if ((qSashDirection & QSashBottom) != 0)
        {
            lHeight = Math.Max(lMinimumHeight, qSashOrigin.QSashHeight + lDy);
        }

        return new QSashBounds(lLeft, lTop, lWidth, lHeight);
    }

    public static QSashBounds QSashBoundsClamp(
        QSashBounds lBounds,
        QSashBounds lWorkArea,
        double lMinimumWidth,
        double lMinimumHeight,
        double lMaximumWidth,
        double lMaximumHeight)
    {
        double lWidthLimit = double.IsFinite(lMaximumWidth)
            ? Math.Min(lMaximumWidth, lWorkArea.QSashWidth)
            : lWorkArea.QSashWidth;
        double lHeightLimit = double.IsFinite(lMaximumHeight)
            ? Math.Min(lMaximumHeight, lWorkArea.QSashHeight)
            : lWorkArea.QSashHeight;
        double lWidth = Math.Min(Math.Max(lBounds.QSashWidth, lMinimumWidth), lWidthLimit);
        double lHeight = Math.Min(Math.Max(lBounds.QSashHeight, lMinimumHeight), lHeightLimit);
        double lLeft = Math.Clamp(lBounds.QSashLeft, lWorkArea.QSashLeft, lWorkArea.QSashRight - lWidth);
        double lTop = Math.Clamp(lBounds.QSashTop, lWorkArea.QSashTop, lWorkArea.QSashBottom - lHeight);
        return new QSashBounds(lLeft, lTop, lWidth, lHeight);
    }

    public static QSashBounds QSashPlacementResolve(
        double? lSavedLeft,
        double? lSavedTop,
        double lSavedWidth,
        double lSavedHeight,
        QSashBounds lCurrent,
        double lMinimumWidth,
        double lMinimumHeight,
        double lWorkLeft,
        double lWorkTop)
    {
        double lLeft = lSavedLeft is double lFiniteLeft && double.IsFinite(lFiniteLeft)
            ? lFiniteLeft
            : double.IsFinite(lCurrent.QSashLeft) ? lCurrent.QSashLeft : lWorkLeft;
        double lTop = lSavedTop is double lFiniteTop && double.IsFinite(lFiniteTop)
            ? lFiniteTop
            : double.IsFinite(lCurrent.QSashTop) ? lCurrent.QSashTop : lWorkTop;
        double lWidth = double.IsFinite(lSavedWidth) && lSavedWidth > 0
            ? lSavedWidth
            : Math.Max(lCurrent.QSashWidth, lMinimumWidth);
        double lHeight = double.IsFinite(lSavedHeight) && lSavedHeight > 0
            ? lSavedHeight
            : Math.Max(lCurrent.QSashHeight, lMinimumHeight);
        return new QSashBounds(lLeft, lTop, lWidth, lHeight);
    }

    public static (double QSashLeft, double QSashTop) QSashDragResolve(
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

    public static QSashLimits QSashLimitsResolve(
        double lBoundsLeft,
        double lBoundsTop,
        double lWorkLeft,
        double lWorkTop,
        double lWorkRight,
        double lWorkBottom) =>
        new(lWorkLeft - lBoundsLeft, lWorkTop - lBoundsTop, lWorkRight - lWorkLeft, lWorkBottom - lWorkTop);

    public static (double QSashLeft, double QSashTop) QSashCenterResolve(
        bool lManual,
        QSashBounds lWindow,
        QSashBounds lOwner) =>
        lManual
            ? (lWindow.QSashLeft, lWindow.QSashTop)
            : (
                lOwner.QSashLeft + ((lOwner.QSashWidth - lWindow.QSashWidth) / 2),
                lOwner.QSashTop + ((lOwner.QSashHeight - lWindow.QSashHeight) / 2));

    public static (double QSashLeft, double QSashTop) QSashRelayResolve(
        double lDropLeft,
        double lDropTop,
        double lScreenLeft,
        double lScreenTop,
        double lScreenWidth,
        double lScreenHeight) =>
        (
            Math.Clamp(lDropLeft - QSashOffsetX, lScreenLeft, lScreenLeft + lScreenWidth - QSashMarginX),
            Math.Clamp(lDropTop - QSashOffsetY, lScreenTop, lScreenTop + lScreenHeight - QSashMarginY));
}
