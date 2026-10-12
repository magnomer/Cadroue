namespace Cadroue.UIDeportment;

public sealed class QSectionDrag
{
    private readonly QSection qSection;
    private readonly QFlow qFlow;
    private bool qSectionDragActive;
    private bool qSectionDragOrigin;
    private double qSectionDragX;
    private double qSectionDragY;
    private QGhost qSectionGhost = new(0, 0);
    private int? qSectionDragIndex;

    public event Action<int>? QSectionDragStarted;
    public event Action<int>? QSectionDragEnd;
    public event Action<int, int>? QSectionRowMoved;

    public QSectionDrag(QSection lOwner, QFlow lFlowOwner)
    {
        qSection = lOwner;
        qFlow = lFlowOwner;
    }

    public bool QSectionDragActive => qSectionDragActive;

    public int? QSectionDragIndex => qSectionDragIndex;

    public QGhost QSectionGhost => qSectionGhost;

    public int QSectionDragRead() => qSectionDragIndex ?? -1;

    public bool QSectionDragCheck() =>
        qSection.QSectionEditable && qSectionDragIndex is not null && qSection.QSectionEditIndex is null;

    public bool QSectionPressHandle(int lIndex, int lClickCount, double lX, double lY, double lGrabX, double lGrabY)
    {
        if (lClickCount >= 2)
        {
            return false;
        }

        qSectionDragIndex = lIndex;
        qSectionDragActive = false;
        qSectionDragOrigin = true;
        qSectionDragX = lX;
        qSectionDragY = lY;
        qSectionGhost = new QGhost(lGrabX, lGrabY);
        return true;
    }

    public bool QSectionMoveHandle(
        double lX,
        double lY,
        double lMinimumX,
        double lMinimumY,
        bool lPressed,
        IReadOnlyList<double> lTops,
        IReadOnlyList<double> lHeights)
    {
        if (!QSectionDragCheck() || !qSectionDragOrigin || qSectionDragIndex is not int lDragIndex || !lPressed)
        {
            return false;
        }

        if (!qSectionDragActive
            && Math.Abs(lX - qSectionDragX) < lMinimumX
            && Math.Abs(lY - qSectionDragY) < lMinimumY)
        {
            return false;
        }

        if (!qSectionDragActive)
        {
            qSectionDragActive = true;
            QSectionDragStarted?.Invoke(lDragIndex);
        }

        QSectionLiveMove(lDragIndex, QSectionIndexResolve(lY, lTops, lHeights));
        return true;
    }

    public bool QSectionReleaseHandle(bool lShift, bool lControl)
    {
        if (!qSectionDragOrigin || qSectionDragIndex is not int lIndex)
        {
            return false;
        }

        bool lMoved = qSectionDragActive;
        QSectionDragEnd?.Invoke(lIndex);
        QSectionDragClear();
        if (lMoved)
        {
            qSection.QSectionRebuildRaise();
            return true;
        }

        if (lIndex >= 0 && qSection.QSectionEditIndex != lIndex)
        {
            qSection.QSectionEditCommit();
            if (lShift)
            {
                qFlow.QFlowSection.QFlowRangeSelect(lIndex);
            }
            else if (lControl)
            {
                qFlow.QFlowSection.QFlowSelectToggle(lIndex);
            }
            else
            {
                qFlow.QFlowSection.QFlowSectionSelect(lIndex);
            }
        }

        return true;
    }

    public void QSectionLostHandle()
    {
        if (qSectionDragIndex is int lIndex)
        {
            QSectionDragEnd?.Invoke(lIndex);
        }

        QSectionDragClear();
    }

    private void QSectionLiveMove(int lSource, int lTarget)
    {
        lTarget = Math.Clamp(lTarget, 0, qSection.QSectionRowCount);
        int lInsert = lSource < lTarget ? lTarget - 1 : lTarget;
        if (lSource == lInsert || !qFlow.QFlowSection.QFlowSectionMove(lSource, lTarget))
        {
            return;
        }

        qSectionDragIndex = lInsert;
        QSectionRowMoved?.Invoke(lSource, lInsert);
    }

    private int QSectionIndexResolve(double lY, IReadOnlyList<double> lTops, IReadOnlyList<double> lHeights)
    {
        int lTarget = 0;
        for (int lIndex = 0; lIndex < lTops.Count; lIndex++)
        {
            if (lY > lTops[lIndex] + lHeights[lIndex] / 2)
            {
                lTarget = lIndex + 1;
            }
        }

        return Math.Clamp(lTarget, 0, qSection.QSectionRowCount);
    }

    public void QSectionDragClear()
    {
        qSectionDragIndex = null;
        qSectionDragActive = false;
        qSectionDragOrigin = false;
    }
}
