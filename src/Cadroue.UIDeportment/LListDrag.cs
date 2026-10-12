namespace Cadroue.UIDeportment;

public sealed class LListDrag
{
    private readonly LList lList;
    private string? lListDragPath;
    private bool lListDragOrigin;
    private double lListDragX;
    private double lListDragY;
    private LGhost lListGhost = new(0, 0);
    private IReadOnlyList<string> lListDragPaths = [];

    public LListDrag(LList lOwner)
    {
        lList = lOwner;
    }

    public string? LListDragPath => lListDragPath;

    public LGhost LListGhost => lListGhost;

    public IReadOnlyList<string> LListDragPaths => lListDragPaths;

    public bool LListPressHandle(
        string lListPath, bool lShift, bool lControl, double lX, double lY, double lGrabX, double lGrabY)
    {
        lList.LListPressSelect(lListPath, lShift, lControl);
        if (lList.LListLockCheck(lListPath))
        {
            return false;
        }

        lListDragOrigin = true;
        lListDragX = lX;
        lListDragY = lY;
        lListGhost = new LGhost(lGrabX, lGrabY);
        lListDragPath = lListPath;
        return true;
    }

    public bool LListDragResolve(double lX, double lY, double lMinimumX, double lMinimumY, bool lPressed)
    {
        if (!lListDragOrigin || lListDragPath is not { } lDragPath || !lPressed)
        {
            return false;
        }

        if (Math.Abs(lX - lListDragX) < lMinimumX && Math.Abs(lY - lListDragY) < lMinimumY)
        {
            return false;
        }

        lListDragPaths = lList.LListSelectionCheck(lDragPath)
            ? lList.LListSelectionRead().Where(lListPath => !lList.LListLockCheck(lListPath)).ToArray()
            : [lDragPath];
        lListDragOrigin = false;
        lListDragPath = null;
        lList.LListPressReset();
        return lListDragPaths.Count > 0;
    }

    public void LListReleaseHandle()
    {
        lListDragOrigin = false;
        lListDragPath = null;
        lList.LListReleaseSelect();
    }
}
