using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The list's wiring: hands each add, removal, key and press to the list gate and passes its answers and changes on.
// It keeps only whether the panel is minimized. The members that merely pass the gate on serve callers not yet on
// the gate (QListGate).
public sealed class QList
{
    private bool qListMinimized;

    // The tab's own list gate comes from the atelier.
    public QList(CAtelier lAtelier)
    {
        QListGate = lAtelier.CAtelierListCreate();
        QListDrag = new QListDrag(QListGate);
        QListFace = new QListFace(QListGate);
    }

    public event Action? QListChanged
    {
        add => QListGate.CListChanged += value;
        remove => QListGate.CListChanged -= value;
    }

    public event Action<string?>? QListPathChanged
    {
        add => QListGate.CListPathChanged += value;
        remove => QListGate.CListPathChanged -= value;
    }

    public event Action<bool>? QListMinimizeChanged;

    public event Action<IReadOnlyList<string>>? QListClearChanged
    {
        add => QListGate.CListRemoved += value;
        remove => QListGate.CListRemoved -= value;
    }

    public event Action<IReadOnlyList<LDocketEntry>>? QListItemsAdded
    {
        add => QListGate.CListItemsAdded += value;
        remove => QListGate.CListItemsAdded -= value;
    }

    public event Action<bool>? QListLockChanged
    {
        add => QListGate.CListLockChanged += value;
        remove => QListGate.CListLockChanged -= value;
    }

    public CList QListGate { get; }

    public LDocket QListDocket => QListGate.CListDocket;

    public QListDrag QListDrag { get; }

    public QListFace QListFace { get; }

    public string? QListPathCurrent => QListGate.CListPathCurrent;

    public bool QListMinimized => qListMinimized;

    public bool QListEmpty => QListGate.CListEmpty;

    public IReadOnlyList<string> QListSelectionRead() => QListGate.CListSelectionRead();

    public bool QListSelectionCheck(string lListPath) => QListGate.CListSelectionCheck(lListPath);

    public bool QListLockCheck() => QListGate.CListLockCheck();

    public Task<int> QListPathsAdd(IEnumerable<string> lAddPaths) => QListGate.CListPathsAdd(lAddPaths);

    public void QListDialogAdd(bool? lConfirmed, IReadOnlyList<string> lPaths, string lKind)
    {
        if (lConfirmed != true)
        {
            return;
        }

        LTraceLog.LTraceInfoRecord($"List manual {lKind} dialog confirmed: {lPaths.Count} {lKind}(s)");
        _ = QListGate.CListPathsAdd(lPaths);
    }

    public void QListRemove() => QListGate.CListRemove();

    public void QListClear() => QListGate.CListClear();

    public void QListClose() => QListGate.CListClose();

    public void QListMinimizedSet(bool lMinimized)
    {
        if (qListMinimized == lMinimized)
        {
            return;
        }

        qListMinimized = lMinimized;
        QListMinimizeChanged?.Invoke(lMinimized);
    }

    // Control+A selects every path.
    public bool QListKeyRun(string lKey, bool lControl)
    {
        if (!QListKeyCheck(lKey, lControl))
        {
            return false;
        }

        QListGate.CListAllSelect();
        return true;
    }

    public void QListListedSelect(string lListPath) => QListGate.CListListedSelect(lListPath);

    private static bool QListKeyCheck(string lKey, bool lControl) =>
        string.Equals(lKey, "A", StringComparison.Ordinal) && lControl;
}
