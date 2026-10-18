namespace Cadroue.UIDeportment;

public sealed class QFunnelDrag
{
    private readonly QFunnel qFunnel;
    private QFunnelRule? qFunnelDragRule;
    private double qFunnelDragX;
    private double qFunnelDragY;
    private QGhost qFunnelGhost = new(0, 0);
    private bool qFunnelDragActive;

    public QFunnelDrag(QFunnel lOwner)
    {
        qFunnel = lOwner;
    }

    public bool QFunnelDragActive => qFunnelDragActive;

    public QGhost QFunnelGhost => qFunnelGhost;

    public void QFunnelPressHandle(QFunnelRule lRule, double lX, double lY, double lOffsetX, double lOffsetY)
    {
        qFunnelDragRule = lRule;
        qFunnelDragX = lX;
        qFunnelDragY = lY;
        qFunnelGhost = new QGhost(lOffsetX, lOffsetY);
        qFunnelDragActive = false;
    }

    public bool QFunnelMoveCheck(QFunnelRule lRule, bool lLeftPressed) =>
        ReferenceEquals(qFunnelDragRule, lRule) && lLeftPressed;

    public bool QFunnelDragResolve(double lX, double lY, double lMinimumX, double lMinimumY)
    {
        if (qFunnelDragRule is null || qFunnelDragActive)
        {
            return false;
        }

        if (!QFunnelThresholdCheck(lX, lY, lMinimumX, lMinimumY))
        {
            return false;
        }

        QFunnelDragStart();
        return true;
    }

    private bool QFunnelThresholdCheck(double lX, double lY, double lMinimumX, double lMinimumY) =>
        !(Math.Abs(lX - qFunnelDragX) < lMinimumX && Math.Abs(lY - qFunnelDragY) < lMinimumY);

    private void QFunnelDragStart() => qFunnelDragActive = true;

    public void QFunnelDragMove(double lPointer, IReadOnlyList<double> lCenters)
    {
        if (qFunnelDragRule is not { } lRule || !qFunnelDragActive)
        {
            return;
        }

        qFunnel.QFunnelRuleMove(lRule, QFunnelIndexResolve(lPointer, lCenters));
    }

    public bool QFunnelReleaseCheck(QFunnelRule lRule) => ReferenceEquals(qFunnelDragRule, lRule);

    public void QFunnelDragClear()
    {
        qFunnelDragRule = null;
        qFunnelDragActive = false;
    }

    public static double QFunnelCenterResolve(double lTop, double lHeight) => lTop + lHeight / 2;

    public static int QFunnelIndexResolve(double lPointer, IReadOnlyList<double> lCenters)
    {
        int lTarget = 0;
        for (int lIndex = 0; lIndex < lCenters.Count; lIndex++)
        {
            if (lPointer > lCenters[lIndex])
            {
                lTarget = lIndex + 1;
            }
        }

        return Math.Clamp(lTarget, 0, lCenters.Count);
    }
}
