namespace Cadroue.UIDeportment;

// The edge-resize gesture: which edges the pointer is on, the press that starts a resize, the bounds each move
// asks for, and the release or capture loss that ends it.
public sealed class QSash
{
    public const int QSashNone = 0;
    public const int QSashLeft = 1;
    public const int QSashRight = 2;
    public const int QSashTop = 4;
    public const int QSashBottom = 8;

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

        QSashStartSet(lDirection, lPointerX, lPointerY, lBounds);
        QSashActiveChanged?.Invoke(true);
        QSashCaptureStarted?.Invoke();
        return true;
    }

    private void QSashStartSet(int lDirection, double lPointerX, double lPointerY, QSashBounds lBounds)
    {
        qSashActive = true;
        qSashDirection = lDirection;
        qSashStartX = lPointerX;
        qSashStartY = lPointerY;
        qSashOrigin = lBounds;
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
        if (QSashActive)
        {
            QSashBoundsChanged?.Invoke(QSashMoveResolve(lPointerX, lPointerY, lMinimumWidth, lMinimumHeight));
            return true;
        }

        QSashDirectionSet(QSashHoverResolve(lNormal, lInteractive, lX, lY, lWidth, lHeight));
        return false;
    }

    private int QSashHoverResolve(
        bool lNormal,
        bool lInteractive,
        double lX,
        double lY,
        double lWidth,
        double lHeight) =>
        lNormal && !lInteractive
            ? QSashDirectionResolve(lX, lY, lWidth, lHeight)
            : QSashNone;

    private void QSashDirectionSet(int lDirection) => qSashDirection = lDirection;

    public int QSashLeaveResolve() => qSashActive ? qSashDirection : QSashNone;

    public bool QSashReleaseHandle()
    {
        if (!QSashActive)
        {
            return false;
        }

        QSashActiveSet(false);
        QSashActiveChanged?.Invoke(false);
        QSashCaptureStopped?.Invoke();
        return true;
    }

    public void QSashCaptureHandle()
    {
        if (!QSashActive)
        {
            return;
        }

        QSashActiveSet(false);
        QSashActiveChanged?.Invoke(false);
    }

    private void QSashActiveSet(bool lActive) => qSashActive = lActive;

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
}
