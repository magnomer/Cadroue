using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed class LList
{
    private readonly LDocket lListDocket;
    private readonly HashSet<string> lListSelected = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource lListScanSource = new();
    private string? lListPathCurrent;
    private string? lListPathAnchor;
    private string? lListPathSuccessor;
    private string? lListPressPath;
    private bool lListMinimized;

    public event Action? LListChange;
    public event Action<string?>? LListPathChange;
    public event Action<bool>? LListMinimizeChange;
    public event Action<IReadOnlyList<string>>? LListClearChange;
    public event Action<IReadOnlyList<LDocketEntry>>? LListItemsAdd;
    public event Action<bool>? LListLockChange;

    public LList(LDocket lListOwner)
    {
        lListDocket = lListOwner;
        LListDrag = new LListDrag(this);
        LListFace = new LListFace(this);
        lListDocket.LDocketChange += LListDocketHandle;
        lListDocket.LDocketAdded += LListAddHandle;
        lListDocket.LDocketRemoved += LListRemoveHandle;
    }

    public LDocket LListDocket => lListDocket;

    public LListDrag LListDrag { get; }

    public LListFace LListFace { get; }

    public string? LListPathCurrent => lListPathCurrent;

    public string? LListPathAnchor => lListPathAnchor;

    public string? LListPressPath => lListPressPath;

    public bool LListMinimized => lListMinimized;

    public bool LListEmpty => lListDocket.LDocketItemsRead().Count == 0;

    public IReadOnlyList<string> LListSelectionRead() =>
        lListDocket.LDocketPathsRead()
            .Where(lListSelected.Contains)
            .ToArray();

    public bool LListSelectionCheck(string lListPath) => lListSelected.Contains(lListPath);

    public LDocketEntry? LListItemRead() =>
        lListPathCurrent is { } lListCurrentPath ? lListDocket.LDocketItemFind(lListCurrentPath) : null;

    public LDocketEntry? LListEditableRead() =>
        LListItemRead() is { LDocketEntryLocked: false } lListItem ? lListItem : null;

    public bool LListLockCheck() => LListItemRead()?.LDocketEntryLocked == true;

    public bool LListLockCheck(string lListPath) => lListDocket.LDocketLockCheck(lListPath);

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

    public async Task<int> LListPathsAdd(IEnumerable<string> lAddPaths)
    {
        IReadOnlyList<string> lRequested = lAddPaths as IReadOnlyList<string> ?? lAddPaths.ToArray();
        LTraceLog.LTraceInfoRecord(
            $"List add requested: {lRequested.Count} path(s)",
            string.Join(", ", lRequested.Select(LUsher.LUsherNameRead)));
        try
        {
            LMediaScanResult lScanResult = await LMedia.LMediaPathScan(lRequested, lListScanSource.Token);
            foreach (LMediaScanNotice lScanNotice in lScanResult.LMediaScanNotices)
            {
                LTraceLog.LTraceWarningRecord(
                    $"List skipped folder '{lScanNotice.LMediaScanFolder}': {lScanNotice.LMediaScanReason}");
            }

            IReadOnlyList<string> lScannedPaths = lScanResult.LMediaScanPaths;
            LTraceLog.LTraceInfoRecord($"List scan resolved {lScannedPaths.Count} media path(s); adding to docket");
            int lAdded = lScannedPaths.Count == 0 ? 0 : lListDocket.LDocketPathsAdd(lScannedPaths);
            LTraceLog.LTraceInfoRecord($"List add committed: {lAdded} entry(ies)");
            return lAdded;
        }
        catch (OperationCanceledException)
        {
            LTraceLog.LTraceInfoRecord("List add cancelled: the tab closed during the folder scan");
            return 0;
        }
        catch (Exception lAddException)
        {
            LTraceLog.LTraceErrorRecord("List add failed", lAddException);
            return 0;
        }
    }

    public void LListDialogAdd(bool? lConfirmed, IReadOnlyList<string> lPaths, string lKind)
    {
        if (lConfirmed != true)
        {
            return;
        }

        LTraceLog.LTraceInfoRecord($"List manual {lKind} dialog confirmed: {lPaths.Count} {lKind}(s)");
        _ = LListPathsAdd(lPaths);
    }

    public void LListRemove()
    {
        IReadOnlyList<string> lRemovedPaths = LListSelectionRead()
            .Where(lListPath => !LListLockCheck(lListPath))
            .ToArray();
        if (lRemovedPaths.Count == 0)
        {
            return;
        }

        LListSuccessorSet(lRemovedPaths);
        lListDocket.LDocketPathsRemove(lRemovedPaths);
        LListSuccessorReset();
    }

    public void LListClear()
    {
        string[] lRemovedPaths = lListDocket.LDocketUnlockedRead()
            .Select(lListItem => lListItem.LDocketEntryPath)
            .ToArray();
        if (lRemovedPaths.Length > 0)
        {
            lListDocket.LDocketPathsRemove(lRemovedPaths);
        }
    }

    public void LListClose() => lListScanSource.Cancel();

    public void LListMinimizedSet(bool lMinimized)
    {
        if (lListMinimized == lMinimized)
        {
            return;
        }

        lListMinimized = lMinimized;
        LListMinimizeChange?.Invoke(lMinimized);
    }

    public bool LListKeyRun(string lKey, bool lControl)
    {
        if (!string.Equals(lKey, "A", StringComparison.Ordinal) || !lControl)
        {
            return false;
        }

        LListAllSelect();
        return true;
    }

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

    public void LListListedSelect(string lListPath)
    {
        if (lListDocket.LDocketItemFind(lListPath) is not null)
        {
            LListSelect(lListPath);
        }
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

    private void LListDocketHandle(IReadOnlyList<LDocketEntry> lEntries)
    {
        LListChange?.Invoke();
        LListLockChange?.Invoke(LListLockCheck());
    }

    private void LListAddHandle(IReadOnlyList<LDocketEntry> lAdded)
    {
        LTraceLog.LTraceInfoRecord(
            $"List add handled: {lAdded.Count} entry(ies), "
            + $"selecting '{LUsher.LUsherNameRead(lAdded[0].LDocketEntryPath)}' and notifying subscribers");
        LListSelect(lAdded[0].LDocketEntryPath);
        LListItemsAdd?.Invoke(lAdded);
        LTraceLog.LTraceInfoRecord("List add subscribers notified");
    }

    private void LListRemoveHandle(IReadOnlyList<string> lRemoved)
    {
        LListRemovedApply(lRemoved);
        LListClearChange?.Invoke(lRemoved);
        LListSuccessorSelect();
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
        LListLockChange?.Invoke(LListLockCheck());
    }
}
