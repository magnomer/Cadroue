namespace Cadroue.UIDeportment;

// The step list's drag: a press picks the step and keeps where the pointer went down; once the pointer travels the
// minimum distance in an ordered list, the drag is shown and each move carries the step to the row under the pointer.
public sealed class QProcessingDrag
{
    private readonly QProcessing qProcessing;
    private readonly Action qProcessingChanged;
    private int? qProcessingDragIndex;
    private double qProcessingDragX;
    private double qProcessingDragY;
    private bool qProcessingDragActive;

    internal QProcessingDrag(QProcessing lOwner, Action lChanged)
    {
        qProcessing = lOwner;
        qProcessingChanged = lChanged;
    }

    public int? QProcessingDragIndex => qProcessingDragIndex;

    public bool QProcessingDragActive => qProcessingDragActive;

    public void QProcessingDragStart(string lStep, double lX, double lY)
    {
        qProcessing.QProcessingStepSelect(lStep);
        QProcessingDragPrepare(lStep, lX, lY);
    }

    private void QProcessingDragPrepare(string lStep, double lX, double lY)
    {
        qProcessingDragIndex = qProcessing.QProcessingIndexRead(lStep);
        qProcessingDragX = lX;
        qProcessingDragY = lY;
        qProcessingDragActive = false;
    }

    public void QProcessingDragMove(
        double lX,
        double lY,
        bool lPressed,
        double lMinimumX,
        double lMinimumY,
        IReadOnlyList<double> lTops,
        IReadOnlyList<double> lHeights)
    {
        if (QProcessingDragResolve(lX, lY, lPressed, lMinimumX, lMinimumY) is not int lDragIndex)
        {
            return;
        }

        QProcessingDragApply();
        qProcessing.QProcessingIndexMove(lDragIndex, QProcessing.QProcessingIndexResolve(lY, lTops, lHeights));
    }

    private int? QProcessingDragResolve(double lX, double lY, bool lPressed, double lMinimumX, double lMinimumY)
    {
        if (!qProcessing.QProcessingOrdered || qProcessingDragIndex is not int lDragIndex || !lPressed)
        {
            return null;
        }

        if (!qProcessingDragActive
            && Math.Abs(lX - qProcessingDragX) < lMinimumX
            && Math.Abs(lY - qProcessingDragY) < lMinimumY)
        {
            return null;
        }

        return lDragIndex;
    }

    private void QProcessingDragApply()
    {
        bool lStarted = !qProcessingDragActive;
        qProcessingDragActive = true;
        if (lStarted)
        {
            qProcessingChanged();
        }
    }

    public void QProcessingDragClear()
    {
        bool lShown = qProcessingDragActive;
        qProcessingDragIndex = null;
        qProcessingDragActive = false;
        if (lShown)
        {
            qProcessingChanged();
        }
    }

    // A step moved by the order follows as the dragged step when it was the one dragged.
    internal void QProcessingDragAdjust(int lFrom, int lTo)
    {
        if (qProcessingDragIndex == lFrom)
        {
            qProcessingDragIndex = lTo;
        }
    }
}
