using Cadroue.Core;

namespace Cadroue.UIDeportment;

// The viewfinder's press and drag: a press selects the topmost section under it and seeks there, a drag keeps
// seeking, and the flow's wheel hears when a drag starts and ends.
public sealed class QViewfinderDrag
{
    private readonly QFlow qFlow;
    private bool qViewfinderDragActive;

    public QViewfinderDrag(QFlow lOwner)
    {
        qFlow = lOwner;
    }

    public bool QViewfinderDragActive => qViewfinderDragActive;

    public bool QViewfinderPressHandle(double lX, double lWidth)
    {
        if (qFlow.QFlowRange is not { } lSpool || lWidth <= 0)
        {
            return false;
        }

        TimeSpan lTime = QViewfinderTimeResolve(lSpool, lX, lWidth);
        int lHit = QViewfinderHitFind(qFlow.QFlowSection.QFlowSectionsRead(), lTime);
        if (lHit >= 0)
        {
            qFlow.QFlowSection.QFlowSectionSelect(lHit);
        }

        QViewfinderDragSet(true);
        qFlow.QFlowWheel.QFlowDragHandle(true);
        qFlow.QFlowCursorSeek(lTime);
        return true;
    }

    // The topmost section holding the time, or -1.
    private static int QViewfinderHitFind(IReadOnlyList<LPiece> lSections, TimeSpan lTime)
    {
        for (int lIndex = lSections.Count - 1; lIndex >= 0; lIndex--)
        {
            if (lTime >= lSections[lIndex].LPieceOrigin && lTime <= lSections[lIndex].LPieceEnd)
            {
                return lIndex;
            }
        }

        return -1;
    }

    private void QViewfinderDragSet(bool lDragActive) => qViewfinderDragActive = lDragActive;

    public bool QViewfinderMoveHandle(double lX, double lWidth)
    {
        if (!qViewfinderDragActive || qFlow.QFlowRange is not { } lSpool || lWidth <= 0)
        {
            return false;
        }

        qFlow.QFlowCursorSeek(QViewfinderTimeResolve(lSpool, lX, lWidth));
        return true;
    }

    public void QViewfinderDragClear()
    {
        bool lDragging = qViewfinderDragActive;
        QViewfinderDragSet(false);
        if (lDragging)
        {
            qFlow.QFlowWheel.QFlowDragHandle(false);
        }
    }

    private static TimeSpan QViewfinderTimeResolve(LSpool lSpool, double lX, double lWidth)
    {
        double lRatio = Math.Clamp(Math.Clamp(lX, 0, lWidth) / lWidth, 0, 1);
        TimeSpan lRange = lSpool.LSpoolRangeLimit - lSpool.LSpoolRangeOrigin;
        if (lRange <= TimeSpan.Zero)
        {
            return lSpool.LSpoolRangeOrigin;
        }

        return lSpool.LSpoolRangeOrigin + TimeSpan.FromSeconds(lRatio * lRange.TotalSeconds);
    }
}
