using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The section rows' drag wiring: hands each press, move, release and lost capture to the drag gate, keeps the ghost's
// grab point and relays the gate's tells.
public sealed class QSectionDrag
{
    private readonly CSectionDrag qSectionDragGate;
    private QGhost qSectionGhost = new(0, 0);

    public event Action<int>? QSectionDragStarted;
    public event Action<int>? QSectionDragEnd;
    public event Action<int, int>? QSectionRowMoved;

    public QSectionDrag(CSectionDrag lGate)
    {
        qSectionDragGate = lGate;
        qSectionDragGate.CSectionDragStarted += QSectionStartRaise;
        qSectionDragGate.CSectionDragEnd += QSectionEndRaise;
        qSectionDragGate.CSectionDragMoved += QSectionMoveRaise;
    }

    public QGhost QSectionGhost => qSectionGhost;

    // Forwarded for the panel surface's dragged row.
    public int QSectionDragRead() => qSectionDragGate.CSectionDragRead();

    // The grab point is kept on every press: only a press the gate readies a drag from is ever dragged.
    public bool QSectionPressHandle(int lIndex, int lClickCount, double lX, double lY, double lGrabX, double lGrabY)
    {
        qSectionGhost = new QGhost(lGrabX, lGrabY);
        return qSectionDragGate.CSectionDragPrepare(lIndex, lClickCount, lX, lY);
    }

    public bool QSectionMoveHandle(
        double lX,
        double lY,
        double lMinimumX,
        double lMinimumY,
        bool lPressed,
        IReadOnlyList<double> lTops,
        IReadOnlyList<double> lHeights) =>
        qSectionDragGate.CSectionDragMove(lX, lY, lMinimumX, lMinimumY, lPressed, lTops, lHeights);

    public bool QSectionReleaseHandle(bool lShift, bool lControl) =>
        qSectionDragGate.CSectionDragCommit(lShift, lControl);

    public void QSectionLostHandle() => qSectionDragGate.CSectionDragReset();

    private void QSectionStartRaise(int lIndex) => QSectionDragStarted?.Invoke(lIndex);

    private void QSectionEndRaise(int lIndex) => QSectionDragEnd?.Invoke(lIndex);

    private void QSectionMoveRaise(int lSource, int lInsert) => QSectionRowMoved?.Invoke(lSource, lInsert);
}
