namespace Cadroue.UIDeportment;

public sealed class LSectionDrag
{
    private readonly LSection lSection;
    private readonly LFlow lFlow;
    private bool lSectionDragActive;
    private bool lSectionDragOrigin;
    private double lSectionDragX;
    private double lSectionDragY;
    private double lSectionGrabX;
    private double lSectionGrabY;
    private int? lSectionDragIndex;

    public event Action<int>? LSectionDragStart;
    public event Action<int>? LSectionDragEnd;
    public event Action<int, int>? LSectionRowMove;

    public LSectionDrag(LSection lOwner, LFlow lFlowOwner)
    {
        lSection = lOwner;
        lFlow = lFlowOwner;
    }

    public bool LSectionDragActive => lSectionDragActive;

    public int? LSectionDragIndex => lSectionDragIndex;

    public double LSectionGrabX => lSectionGrabX;

    public double LSectionGrabY => lSectionGrabY;

    public int LSectionDragRead() => lSectionDragIndex ?? -1;

    public bool LSectionDragCheck() =>
        lSection.LSectionEditable && lSectionDragIndex is not null && lSection.LSectionEditIndex is null;

    public bool LSectionPressHandle(int lIndex, int lClickCount, double lX, double lY, double lGrabX, double lGrabY)
    {
        if (lClickCount >= 2)
        {
            return false;
        }

        lSectionDragIndex = lIndex;
        lSectionDragActive = false;
        lSectionDragOrigin = true;
        lSectionDragX = lX;
        lSectionDragY = lY;
        lSectionGrabX = lGrabX;
        lSectionGrabY = lGrabY;
        return true;
    }

    public bool LSectionMoveHandle(
        double lX,
        double lY,
        double lMinimumX,
        double lMinimumY,
        bool lPressed,
        IReadOnlyList<double> lTops,
        IReadOnlyList<double> lHeights)
    {
        if (!LSectionDragCheck() || !lSectionDragOrigin || lSectionDragIndex is not int lDragIndex || !lPressed)
        {
            return false;
        }

        if (!lSectionDragActive
            && Math.Abs(lX - lSectionDragX) < lMinimumX
            && Math.Abs(lY - lSectionDragY) < lMinimumY)
        {
            return false;
        }

        if (!lSectionDragActive)
        {
            lSectionDragActive = true;
            LSectionDragStart?.Invoke(lDragIndex);
        }

        LSectionLiveMove(lDragIndex, LSectionIndexResolve(lY, lTops, lHeights));
        return true;
    }

    public bool LSectionReleaseHandle(bool lShift, bool lControl)
    {
        if (!lSectionDragOrigin || lSectionDragIndex is not int lIndex)
        {
            return false;
        }

        bool lMoved = lSectionDragActive;
        LSectionDragEnd?.Invoke(lIndex);
        LSectionDragClear();
        if (lMoved)
        {
            lSection.LSectionRebuildRaise();
            return true;
        }

        if (lIndex >= 0 && lSection.LSectionEditIndex != lIndex)
        {
            lSection.LSectionEditCommit();
            if (lShift)
            {
                lFlow.LFlowSection.LFlowRangeSelect(lIndex);
            }
            else if (lControl)
            {
                lFlow.LFlowSection.LFlowSelectToggle(lIndex);
            }
            else
            {
                lFlow.LFlowSection.LFlowSectionSelect(lIndex);
            }
        }

        return true;
    }

    public void LSectionLostHandle()
    {
        if (lSectionDragIndex is int lIndex)
        {
            LSectionDragEnd?.Invoke(lIndex);
        }

        LSectionDragClear();
    }

    private void LSectionLiveMove(int lSource, int lTarget)
    {
        lTarget = Math.Clamp(lTarget, 0, lSection.LSectionRowCount);
        int lInsert = lSource < lTarget ? lTarget - 1 : lTarget;
        if (lSource == lInsert || !lFlow.LFlowSection.LFlowSectionMove(lSource, lTarget))
        {
            return;
        }

        lSectionDragIndex = lInsert;
        LSectionRowMove?.Invoke(lSource, lInsert);
    }

    private int LSectionIndexResolve(double lY, IReadOnlyList<double> lTops, IReadOnlyList<double> lHeights)
    {
        int lTarget = 0;
        for (int lIndex = 0; lIndex < lTops.Count; lIndex++)
        {
            if (lY > lTops[lIndex] + lHeights[lIndex] / 2)
            {
                lTarget = lIndex + 1;
            }
        }

        return Math.Clamp(lTarget, 0, lSection.LSectionRowCount);
    }

    public void LSectionDragClear()
    {
        lSectionDragIndex = null;
        lSectionDragActive = false;
        lSectionDragOrigin = false;
    }
}
