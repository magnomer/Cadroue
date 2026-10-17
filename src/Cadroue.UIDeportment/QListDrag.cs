using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The list's drag wiring: hands each press, move and release to the list gate, keeps the ghost's grab point and
// passes the gate's dragged paths on.
public sealed class QListDrag
{
    private readonly CList qListDragGate;
    private QGhost qListGhost = new(0, 0);

    public QListDrag(CList lGate)
    {
        qListDragGate = lGate;
    }

    public QGhost QListGhost => qListGhost;

    public IReadOnlyList<string> QListDragPaths => qListDragGate.CListDragPaths;

    public bool QListPressHandle(
        string lListPath, bool lShift, bool lControl, double lX, double lY, double lGrabX, double lGrabY)
    {
        if (!qListDragGate.CListPressHandle(lListPath, lShift, lControl, lX, lY))
        {
            return false;
        }

        qListGhost = new QGhost(lGrabX, lGrabY);
        return true;
    }

    public bool QListDragResolve(double lX, double lY, double lMinimumX, double lMinimumY, bool lPressed) =>
        qListDragGate.CListDragResolve(lX, lY, lMinimumX, lMinimumY, lPressed);

    public void QListReleaseHandle() => qListDragGate.CListReleaseHandle();
}
