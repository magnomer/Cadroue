using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LList
{
    private readonly LDocket lListDocket;
    private readonly HashSet<string> lListSelected = new(StringComparer.OrdinalIgnoreCase);
    private string? lListPathCurrent;
    private string? lListPathAnchor;
    private string? lListPathSuccessor;
    private string? lListPressPath;
    private string? lListDragPath;
    private bool lListMinimized;

    public event Action<string?>? LListPathChange;
    public event Action<bool>? LListMinimizeChange;

    public LList(LDocket lListOwner)
    {
        lListDocket = lListOwner;
    }

    public string? LListPathCurrent => lListPathCurrent;

    public string? LListPathAnchor => lListPathAnchor;

    public string? LListPressPath => lListPressPath;

    public string? LListDragPath => lListDragPath;

    public bool LListMinimized => lListMinimized;

    public IReadOnlyList<string> LListSelectionRead() =>
        lListDocket.LDocketPathsRead()
            .Where(lListSelected.Contains)
            .ToArray();

    public bool LListSelectionCheck(string lListPath) => lListSelected.Contains(lListPath);

    public int LListIndexRead(string? lListPath)
    {
        if (lListPath is null)
        {
            return -1;
        }

        IReadOnlyList<string> lPaths = lListDocket.LDocketPathsRead();
        for (int lIndex = 0; lIndex < lPaths.Count; lIndex++)
        {
            if (string.Equals(lPaths[lIndex], lListPath, StringComparison.OrdinalIgnoreCase))
            {
                return lIndex;
            }
        }

        return -1;
    }

    public void LListMinimizedSet(bool lMinimized)
    {
        if (lListMinimized == lMinimized)
        {
            return;
        }

        lListMinimized = lMinimized;
        LListMinimizeChange?.Invoke(lMinimized);
    }

    public void LListDragSet(string? lListPath) => lListDragPath = lListPath;

    public void LListSelect(string? lListPath)
    {
        lListSelected.Clear();
        if (lListPath is not null)
        {
            lListSelected.Add(lListPath);
        }

        lListPathAnchor = lListPath;
        LListCurrentApply(lListPath);
    }

    public void LListPressSelect(string lListPath, bool lShift, bool lControl)
    {
        lListPressPath = null;
        if (lShift)
        {
            LListRangeSelect(lListPath);
            return;
        }

        if (lControl)
        {
            LListSelectionToggle(lListPath);
            return;
        }

        if (lListSelected.Contains(lListPath) && lListSelected.Count > 1)
        {
            lListPressPath = lListPath;
            lListPathAnchor = lListPath;
            LListCurrentApply(lListPath);
            return;
        }

        LListSelect(lListPath);
    }

    public void LListReleaseSelect()
    {
        if (lListPressPath is { } lCollapsePath)
        {
            LListSelect(lCollapsePath);
        }

        lListPressPath = null;
    }

    public void LListPressReset() => lListPressPath = null;

    public void LListAllSelect()
    {
        IReadOnlyList<string> lPaths = lListDocket.LDocketPathsRead();
        if (lPaths.Count == 0)
        {
            return;
        }

        lListSelected.Clear();
        foreach (string lPath in lPaths)
        {
            lListSelected.Add(lPath);
        }

        LListCurrentApply(LListCurrentResolve());
    }

    public void LListSuccessorSet(IReadOnlyList<string> lRemovedPaths)
    {
        var lRemovedSet = new HashSet<string>(lRemovedPaths, StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<string> lPaths = lListDocket.LDocketPathsRead();
        int lRemovedIndex = LListIndexRead(lRemovedPaths[0]);
        lListPathSuccessor = null;
        for (int lIndex = lRemovedIndex; lIndex < lPaths.Count; lIndex++)
        {
            if (!lRemovedSet.Contains(lPaths[lIndex]))
            {
                lListPathSuccessor = lPaths[lIndex];
                return;
            }
        }

        for (int lIndex = lRemovedIndex - 1; lIndex >= 0; lIndex--)
        {
            if (!lRemovedSet.Contains(lPaths[lIndex]))
            {
                lListPathSuccessor = lPaths[lIndex];
                return;
            }
        }
    }

    public void LListSuccessorReset() => lListPathSuccessor = null;

    public void LListRemovedApply(IReadOnlyList<string> lRemovedPaths)
    {
        lListSelected.ExceptWith(lRemovedPaths);
        if (LListIndexRead(lListPathAnchor) < 0)
        {
            lListPathAnchor = null;
        }
    }

    public void LListSuccessorSelect()
    {
        if (lListPathCurrent is { } lCurrentPath && lListDocket.LDocketItemFind(lCurrentPath) is null)
        {
            LListSelect(lListPathSuccessor);
        }
    }

    private void LListSelectionToggle(string lListPath)
    {
        lListPathAnchor = lListPath;
        if (lListSelected.Add(lListPath))
        {
            LListCurrentApply(lListPath);
            return;
        }

        lListSelected.Remove(lListPath);
        LListCurrentApply(LListCurrentResolve());
    }

    private void LListRangeSelect(string lListPath)
    {
        int lRowIndex = LListIndexRead(lListPath);
        if (lRowIndex < 0)
        {
            return;
        }

        int lAnchorIndex = LListIndexRead(lListPathAnchor ?? lListPathCurrent);
        if (lAnchorIndex < 0)
        {
            lAnchorIndex = lRowIndex;
        }

        IReadOnlyList<string> lPaths = lListDocket.LDocketPathsRead();
        lListSelected.Clear();
        for (int lIndex = Math.Min(lAnchorIndex, lRowIndex); lIndex <= Math.Max(lAnchorIndex, lRowIndex); lIndex++)
        {
            lListSelected.Add(lPaths[lIndex]);
        }

        LListCurrentApply(lListPath);
    }

    private string? LListCurrentResolve() =>
        lListPathCurrent is not null && lListSelected.Contains(lListPathCurrent)
            ? lListPathCurrent
            : LListSelectionRead().LastOrDefault();

    private void LListCurrentApply(string? lListPath)
    {
        lListPathCurrent = lListPath;
        LListPathChange?.Invoke(lListPath);
    }

    public static void LListRelayAttach(
        Func<Guid, string, Guid, bool> lDeliveredAdd,
        Func<Guid, string, Guid, bool> lDeliveredCommit,
        Action<LWorkItem, bool> lDeliveredRemove,
        Action<Guid, string, Guid> lAccept,
        Action<IReadOnlyList<Guid>> lBatchRemove,
        Action<IReadOnlyList<(string PListPath, Guid PListBatch, LWorkItem PListOwner)>> lSourceRelease,
        Func<IReadOnlyList<LWorkItem>, bool> lSourceClaim)
    {
        LCartographer.LCartographerLockSeam = lSourceClaim;
        LCartographer.LCartographerDeliverySeam = new LCartographerDelivery(
            lDeliveredAdd,
            lDeliveredCommit,
            lDeliveredRemove,
            lAccept,
            lBatchRemove,
            lSourceRelease);
        LMessenger.LMessengerDeliverSource = (lTarget, lPath, lCohort) =>
        {
            if (!lDeliveredAdd(lTarget, lPath, lCohort))
            {
                return false;
            }

            lAccept(lTarget, lPath, lCohort);
            return true;
        };
    }
}
