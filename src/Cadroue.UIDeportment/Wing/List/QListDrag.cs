using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The list's drag wiring: hands each press, move and release to the list gate, keeps the ghost's grab point and
// passes the gate's dragged paths on.
public sealed class QListDrag
{
    private readonly CListDrag qListDragGate;
    private QGhost qListGhost = new(0, 0);

    public QListDrag(CList lGate)
    {
        qListDragGate = lGate.CListDrag;
    }

    public QGhost QListGhost => qListGhost;

    public IReadOnlyList<string> QListDragPaths => qListDragGate.CListDragPaths;

    // The grab point is kept on every press: only a press the gate lets a drag start from is ever dragged.
    public bool QListPressHandle(
        string lListPath, bool lShift, bool lControl, double lX, double lY, double lGrabX, double lGrabY)
    {
        qListGhost = new QGhost(lGrabX, lGrabY);
        return qListDragGate.CListPressHandle(lListPath, lShift, lControl, lX, lY);
    }

    public bool QListDragResolve(double lX, double lY, double lMinimumX, double lMinimumY, bool lPressed) =>
        qListDragGate.CListDragResolve(lX, lY, lMinimumX, lMinimumY, lPressed);

    public void QListReleaseHandle() => qListDragGate.CListReleaseHandle();
}
