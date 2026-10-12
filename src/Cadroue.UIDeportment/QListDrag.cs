namespace Cadroue.UIDeportment;

public sealed class QListDrag
{
    private readonly QList qList;
    private string? qListDragPath;
    private bool qListDragOrigin;
    private double qListDragX;
    private double qListDragY;
    private QGhost qListGhost = new(0, 0);
    private IReadOnlyList<string> qListDragPaths = [];

    public QListDrag(QList lOwner)
    {
        qList = lOwner;
    }

    public string? QListDragPath => qListDragPath;

    public QGhost QListGhost => qListGhost;

    public IReadOnlyList<string> QListDragPaths => qListDragPaths;

    public bool QListPressHandle(
        string lListPath, bool lShift, bool lControl, double lX, double lY, double lGrabX, double lGrabY)
    {
        qList.QListPressSelect(lListPath, lShift, lControl);
        if (qList.QListLockCheck(lListPath))
        {
            return false;
        }

        qListDragOrigin = true;
        qListDragX = lX;
        qListDragY = lY;
        qListGhost = new QGhost(lGrabX, lGrabY);
        qListDragPath = lListPath;
        return true;
    }

    public bool QListDragResolve(double lX, double lY, double lMinimumX, double lMinimumY, bool lPressed)
    {
        if (!qListDragOrigin || qListDragPath is not { } lDragPath || !lPressed)
        {
            return false;
        }

        if (Math.Abs(lX - qListDragX) < lMinimumX && Math.Abs(lY - qListDragY) < lMinimumY)
        {
            return false;
        }

        qListDragPaths = qList.QListSelectionCheck(lDragPath)
            ? qList.QListSelectionRead().Where(lListPath => !qList.QListLockCheck(lListPath)).ToArray()
            : [lDragPath];
        qListDragOrigin = false;
        qListDragPath = null;
        qList.QListPressReset();
        return qListDragPaths.Count > 0;
    }

    public void QListReleaseHandle()
    {
        qListDragOrigin = false;
        qListDragPath = null;
        qList.QListReleaseSelect();
    }
}
