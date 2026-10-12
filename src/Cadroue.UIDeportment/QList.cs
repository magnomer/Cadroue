using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed class QList
{
    private readonly CUsher qListUsher;
    private readonly LDocket qListDocket = new();
    private readonly HashSet<string> qListSelected = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource qListScanSource = new();
    private string? qListPathCurrent;
    private string? qListPathAnchor;
    private string? qListPathSuccessor;
    private string? qListPressPath;
    private bool qListMinimized;

    public event Action? QListChanged;
    public event Action<string?>? QListPathChanged;
    public event Action<bool>? QListMinimizeChanged;
    public event Action<IReadOnlyList<string>>? QListClearChanged;
    public event Action<IReadOnlyList<LDocketEntry>>? QListItemsAdded;
    public event Action<bool>? QListLockChanged;

    public QList(CUsher lUsher)
    {
        qListUsher = lUsher;
        QListDrag = new QListDrag(this);
        QListFace = new QListFace(this, qListUsher);
        qListDocket.LDocketChanged += QListDocketHandle;
        qListDocket.LDocketAdded += QListAddHandle;
        qListDocket.LDocketDelivered += QListDeliveredHandle;
        qListDocket.LDocketRemoved += QListRemoveHandle;
    }

    public LDocket QListDocket => qListDocket;

    public QListDrag QListDrag { get; }

    public QListFace QListFace { get; }

    public string? QListPathCurrent => qListPathCurrent;

    public string? QListPathAnchor => qListPathAnchor;

    public string? QListPressPath => qListPressPath;

    public bool QListMinimized => qListMinimized;

    public bool QListEmpty => qListDocket.LDocketItemsRead().Count == 0;

    public IReadOnlyList<string> QListSelectionRead() =>
        qListDocket.LDocketPathsRead()
            .Where(qListSelected.Contains)
            .ToArray();

    public bool QListSelectionCheck(string lListPath) => qListSelected.Contains(lListPath);

    public LDocketEntry? QListItemRead() =>
        qListPathCurrent is { } lListCurrentPath ? qListDocket.LDocketItemFind(lListCurrentPath) : null;

    public LDocketEntry? QListEditableRead() =>
        QListItemRead() is { LDocketEntryLocked: false } lListItem ? lListItem : null;

    public bool QListLockCheck() => QListItemRead()?.LDocketEntryLocked == true;

    public bool QListLockCheck(string lListPath) => qListDocket.LDocketLockCheck(lListPath);

    public int QListIndexRead(string? lListPath)
    {
        if (lListPath is null)
        {
            return -1;
        }

        IReadOnlyList<string> lPaths = qListDocket.LDocketPathsRead();
        for (int lIndex = 0; lIndex < lPaths.Count; lIndex++)
        {
            if (string.Equals(lPaths[lIndex], lListPath, StringComparison.OrdinalIgnoreCase))
            {
                return lIndex;
            }
        }

        return -1;
    }

    public async Task<int> QListPathsAdd(IEnumerable<string> lAddPaths)
    {
        IReadOnlyList<string> lRequested = lAddPaths as IReadOnlyList<string> ?? lAddPaths.ToArray();
        LTraceLog.LTraceInfoRecord(
            $"List add requested: {lRequested.Count} path(s)",
            string.Join(", ", lRequested.Select(qListUsher.CUsherNameRead)));
        try
        {
            LMediaScanResult lScanResult = await LMedia.LMediaPathScan(lRequested, qListScanSource.Token);
            foreach (LMediaScanNotice lScanNotice in lScanResult.LMediaScanNotices)
            {
                LTraceLog.LTraceWarningRecord(
                    $"List skipped folder '{lScanNotice.LMediaScanFolder}': {lScanNotice.LMediaScanReason}");
            }

            IReadOnlyList<string> lScannedPaths = lScanResult.LMediaScanPaths;
            LTraceLog.LTraceInfoRecord($"List scan resolved {lScannedPaths.Count} media path(s); adding to docket");
            int lAdded = lScannedPaths.Count == 0 ? 0 : qListDocket.LDocketPathsAdd(lScannedPaths);
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

    public void QListDialogAdd(bool? lConfirmed, IReadOnlyList<string> lPaths, string lKind)
    {
        if (lConfirmed != true)
        {
            return;
        }

        LTraceLog.LTraceInfoRecord($"List manual {lKind} dialog confirmed: {lPaths.Count} {lKind}(s)");
        _ = QListPathsAdd(lPaths);
    }

    public void QListRemove()
    {
        IReadOnlyList<string> lRemovedPaths = QListSelectionRead()
            .Where(lListPath => !QListLockCheck(lListPath))
            .ToArray();
        if (lRemovedPaths.Count == 0)
        {
            return;
        }

        QListSuccessorSet(lRemovedPaths);
        qListDocket.LDocketPathsRemove(lRemovedPaths);
        QListSuccessorReset();
    }

    public void QListClear()
    {
        string[] lRemovedPaths = qListDocket.LDocketUnlockedRead()
            .Select(lListItem => lListItem.LDocketEntryPath)
            .ToArray();
        if (lRemovedPaths.Length > 0)
        {
            qListDocket.LDocketPathsRemove(lRemovedPaths);
        }
    }

    public void QListClose() => qListScanSource.Cancel();

    public void QListMinimizedSet(bool lMinimized)
    {
        if (qListMinimized == lMinimized)
        {
            return;
        }

        qListMinimized = lMinimized;
        QListMinimizeChanged?.Invoke(lMinimized);
    }

    public bool QListKeyRun(string lKey, bool lControl)
    {
        if (!string.Equals(lKey, "A", StringComparison.Ordinal) || !lControl)
        {
            return false;
        }

        QListAllSelect();
        return true;
    }

    public void QListSelect(string? lListPath)
    {
        qListSelected.Clear();
        if (lListPath is not null)
        {
            qListSelected.Add(lListPath);
        }

        qListPathAnchor = lListPath;
        QListCurrentApply(lListPath);
    }

    public void QListListedSelect(string lListPath)
    {
        if (qListDocket.LDocketItemFind(lListPath) is not null)
        {
            QListSelect(lListPath);
        }
    }

    public void QListPressSelect(string lListPath, bool lShift, bool lControl)
    {
        qListPressPath = null;
        if (lShift)
        {
            QListRangeSelect(lListPath);
            return;
        }

        if (lControl)
        {
            QListSelectionToggle(lListPath);
            return;
        }

        if (qListSelected.Contains(lListPath) && qListSelected.Count > 1)
        {
            qListPressPath = lListPath;
            qListPathAnchor = lListPath;
            QListCurrentApply(lListPath);
            return;
        }

        QListSelect(lListPath);
    }

    public void QListReleaseSelect()
    {
        if (qListPressPath is { } lCollapsePath)
        {
            QListSelect(lCollapsePath);
        }

        qListPressPath = null;
    }

    public void QListPressReset() => qListPressPath = null;

    public void QListAllSelect()
    {
        IReadOnlyList<string> lPaths = qListDocket.LDocketPathsRead();
        if (lPaths.Count == 0)
        {
            return;
        }

        qListSelected.Clear();
        foreach (string lPath in lPaths)
        {
            qListSelected.Add(lPath);
        }

        QListCurrentApply(QListCurrentResolve());
    }

    public void QListSuccessorSet(IReadOnlyList<string> lRemovedPaths)
    {
        var lRemovedSet = new HashSet<string>(lRemovedPaths, StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<string> lPaths = qListDocket.LDocketPathsRead();
        int lRemovedIndex = QListIndexRead(lRemovedPaths[0]);
        qListPathSuccessor = null;
        for (int lIndex = lRemovedIndex; lIndex < lPaths.Count; lIndex++)
        {
            if (!lRemovedSet.Contains(lPaths[lIndex]))
            {
                qListPathSuccessor = lPaths[lIndex];
                return;
            }
        }

        for (int lIndex = lRemovedIndex - 1; lIndex >= 0; lIndex--)
        {
            if (!lRemovedSet.Contains(lPaths[lIndex]))
            {
                qListPathSuccessor = lPaths[lIndex];
                return;
            }
        }
    }

    public void QListSuccessorReset() => qListPathSuccessor = null;

    public void QListRemovedApply(IReadOnlyList<string> lRemovedPaths)
    {
        qListSelected.ExceptWith(lRemovedPaths);
        if (QListIndexRead(qListPathAnchor) < 0)
        {
            qListPathAnchor = null;
        }
    }

    public void QListSuccessorSelect()
    {
        if (qListPathCurrent is { } lCurrentPath && qListDocket.LDocketItemFind(lCurrentPath) is null)
        {
            QListSelect(qListPathSuccessor);
        }
    }

    private void QListDocketHandle(IReadOnlyList<LDocketEntry> lEntries)
    {
        QListChanged?.Invoke();
        QListLockChanged?.Invoke(QListLockCheck());
    }

    private void QListAddHandle(IReadOnlyList<LDocketEntry> lAdded)
    {
        LTraceLog.LTraceInfoRecord(
            $"List add handled: {lAdded.Count} entry(ies), "
            + $"selecting '{qListUsher.CUsherNameRead(lAdded[0].LDocketEntryPath)}' and notifying subscribers");
        QListSelect(lAdded[0].LDocketEntryPath);
        QListItemsAdded?.Invoke(lAdded);
        LTraceLog.LTraceInfoRecord("List add subscribers notified");
    }

    private void QListDeliveredHandle(IReadOnlyList<LDocketEntry> lAdded) => QListItemsAdded?.Invoke(lAdded);

    private void QListRemoveHandle(IReadOnlyList<string> lRemoved)
    {
        QListRemovedApply(lRemoved);
        QListClearChanged?.Invoke(lRemoved);
        QListSuccessorSelect();
    }

    private void QListSelectionToggle(string lListPath)
    {
        qListPathAnchor = lListPath;
        if (qListSelected.Add(lListPath))
        {
            QListCurrentApply(lListPath);
            return;
        }

        qListSelected.Remove(lListPath);
        QListCurrentApply(QListCurrentResolve());
    }

    private void QListRangeSelect(string lListPath)
    {
        int lRowIndex = QListIndexRead(lListPath);
        if (lRowIndex < 0)
        {
            return;
        }

        int lAnchorIndex = QListIndexRead(qListPathAnchor ?? qListPathCurrent);
        if (lAnchorIndex < 0)
        {
            lAnchorIndex = lRowIndex;
        }

        IReadOnlyList<string> lPaths = qListDocket.LDocketPathsRead();
        qListSelected.Clear();
        for (int lIndex = Math.Min(lAnchorIndex, lRowIndex); lIndex <= Math.Max(lAnchorIndex, lRowIndex); lIndex++)
        {
            qListSelected.Add(lPaths[lIndex]);
        }

        QListCurrentApply(lListPath);
    }

    private string? QListCurrentResolve() =>
        qListPathCurrent is not null && qListSelected.Contains(qListPathCurrent)
            ? qListPathCurrent
            : QListSelectionRead().LastOrDefault();

    private void QListCurrentApply(string? lListPath)
    {
        qListPathCurrent = lListPath;
        QListPathChanged?.Invoke(lListPath);
        QListLockChanged?.Invoke(QListLockCheck());
    }
}
