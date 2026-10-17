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

        QListDragStart(lListPath, lX, lY, lGrabX, lGrabY);
        return true;
    }

    public bool QListDragResolve(double lX, double lY, double lMinimumX, double lMinimumY, bool lPressed)
    {
        if (QListThresholdResolve(lX, lY, lMinimumX, lMinimumY, lPressed) is not { } lDragPath)
        {
            return false;
        }

        IReadOnlyList<string> lDragged = QListDraggedRead(lDragPath);
        QListDragApply(lDragged);
        qList.QListPressReset();
        return lDragged.Count > 0;
    }

    public void QListReleaseHandle()
    {
        QListDragClear();
        qList.QListReleaseSelect();
    }

    private void QListDragStart(string lListPath, double lX, double lY, double lGrabX, double lGrabY)
    {
        qListDragOrigin = true;
        qListDragX = lX;
        qListDragY = lY;
        qListGhost = new QGhost(lGrabX, lGrabY);
        qListDragPath = lListPath;
    }

    private string? QListThresholdResolve(double lX, double lY, double lMinimumX, double lMinimumY, bool lPressed)
    {
        if (!qListDragOrigin || qListDragPath is not { } lDragPath || !lPressed)
        {
            return null;
        }

        if (Math.Abs(lX - qListDragX) < lMinimumX && Math.Abs(lY - qListDragY) < lMinimumY)
        {
            return null;
        }

        return lDragPath;
    }

    private IReadOnlyList<string> QListDraggedRead(string lDragPath) =>
        qList.QListSelectionCheck(lDragPath)
            ? qList.QListSelectionRead().Where(lListPath => !qList.QListLockCheck(lListPath)).ToArray()
            : [lDragPath];

    private void QListDragApply(IReadOnlyList<string> lDragged)
    {
        qListDragPaths = lDragged;
        QListDragClear();
    }

    private void QListDragClear()
    {
        qListDragOrigin = false;
        qListDragPath = null;
    }
}
