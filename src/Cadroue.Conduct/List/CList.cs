using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// One list's selection over its docket: the selected paths, the anchor a range runs from, the current path, the
// pressed path a release collapses to, the path a removal moves on to, and the paths a drag carries. The docket is
// the engine's, reached through the list port; every path compares without case.
public sealed class CList
{
    private readonly LListPort _cListPort;
    private readonly HashSet<string> _cListSelected = new(StringComparer.OrdinalIgnoreCase);
    private string? _cListSuccessor;
    private bool _cListDragOrigin;
    private double _cListDragX;
    private double _cListDragY;

    // The port is built for this list; its traces name a path as the usher does.
    internal CList(Func<Func<string, string>, LListPort> port, CUsher usher)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(usher);
        _cListPort = port(usher.CUsherNameRead);
        _cListPort.LListChanged += CListChangeHandle;
        _cListPort.LListAdded += CListAddHandle;
        _cListPort.LListEntriesAdded += CListEntriesHandle;
        _cListPort.LListRemoved += CListRemoveHandle;
    }

    public event Action? CListChanged;

    public event Action<string?>? CListPathChanged;

    public event Action<IReadOnlyList<string>>? CListRemoved;

    // The engine's entries, passed through unread, only for the readers not yet moved to a gate.
    public event Action<IReadOnlyList<Cadroue.Application.LDocketEntry>>? CListItemsAdded;

    // Whether the current path is locked, told after every change that can move it.
    public event Action<bool>? CListLockChanged;

    // The engine's docket, passed through unread, only for the readers not yet moved to a gate.
    public Cadroue.Application.LDocket CListDocket => _cListPort.LListDocket;

    public string? CListPathCurrent { get; private set; }

    public string? CListPathAnchor { get; private set; }

    // The selected path a plain press landed on while several were selected; the release selects it alone.
    public string? CListPressPath { get; private set; }

    // The path a drag may start from, from an unlocked press until the drag starts or the button is released.
    public string? CListDragPath { get; private set; }

    // The paths the last drag carried.
    public IReadOnlyList<string> CListDragPaths { get; private set; } = [];

    public bool CListEmpty => _cListPort.LListPathsRead().Count == 0;

    // The selected paths in docket order.
    public IReadOnlyList<string> CListSelectionRead() =>
        _cListPort.LListPathsRead()
            .Where(_cListSelected.Contains)
            .ToArray();

    public bool CListSelectionCheck(string path) => _cListSelected.Contains(path);

    public bool CListLockCheck() => CListPathCurrent is { } current && _cListPort.LListLockCheck(current);

    public bool CListLockCheck(string path) => _cListPort.LListLockCheck(path);

    public async Task<int> CListPathsAdd(IEnumerable<string> paths)
    {
        IReadOnlyList<string> requested = paths as IReadOnlyList<string> ?? paths.ToArray();
        return await _cListPort.LListPathsAdd(requested);
    }

    // Removes the selected unlocked paths; the current path moves to the next one left, else the one before.
    public void CListRemove()
    {
        IReadOnlyList<string> removed = CListSelectionRead()
            .Where(path => !_cListPort.LListLockCheck(path))
            .ToArray();
        if (removed.Count == 0)
        {
            return;
        }

        _cListSuccessor = CListSuccessorResolve(removed, _cListPort.LListPathsRead(), CListIndexRead(removed[0]));
        _cListPort.LListPathsRemove(removed);
        _cListSuccessor = null;
    }

    // Removes every unlocked path.
    public void CListClear()
    {
        IReadOnlyList<string> removed = _cListPort.LListUnlockedRead();
        if (removed.Count > 0)
        {
            _cListPort.LListPathsRemove(removed);
        }
    }

    public void CListClose() => _cListPort.LListClose();

    // Selects this path alone (none for null) and makes it the anchor and the current path.
    public void CListSelect(string? path)
    {
        _cListSelected.Clear();
        if (path is not null)
        {
            _cListSelected.Add(path);
        }

        CListPathAnchor = path;
        CListCurrentApply(path);
    }

    // Selects the path only while the docket holds it.
    public void CListListedSelect(string path)
    {
        if (_cListPort.LListPathCheck(path))
        {
            CListSelect(path);
        }
    }

    // Shift selects the range from the anchor, Control toggles the path, and a plain press on one of several selected
    // paths waits for the release; any other press selects the path alone.
    public void CListPressSelect(string path, bool shift, bool control)
    {
        CListPressPath = null;
        if (shift)
        {
            CListRangeSelect(path);
            return;
        }

        if (control)
        {
            CListSelectionToggle(path);
            return;
        }

        if (CListPluralCheck(path))
        {
            CListPressSet(path);
            return;
        }

        CListSelect(path);
    }

    public void CListReleaseSelect()
    {
        if (CListPressPath is { } collapse)
        {
            CListSelect(collapse);
        }

        CListPressPath = null;
    }

    public void CListAllSelect()
    {
        IReadOnlyList<string> paths = _cListPort.LListPathsRead();
        if (paths.Count == 0)
        {
            return;
        }

        CListSelectedSet(paths);
        CListCurrentApply(CListCurrentResolve());
    }

    // A press at (x, y) selects as CListPressSelect does; a drag may start from it unless the pressed path is locked.
    public bool CListPressHandle(string path, bool shift, bool control, double x, double y)
    {
        CListPressSelect(path, shift, control);
        if (_cListPort.LListLockCheck(path))
        {
            return false;
        }

        _cListDragOrigin = true;
        _cListDragX = x;
        _cListDragY = y;
        CListDragPath = path;
        return true;
    }

    // A move to (x, y), at least the minimum away from the press on one axis with the button held, starts the drag:
    // it carries the selection's unlocked paths when the pressed path is selected, else the pressed path alone.
    // Answers whether it carries any.
    public bool CListDragResolve(double x, double y, double minimumX, double minimumY, bool pressed)
    {
        if (!_cListDragOrigin || CListDragPath is not { } dragPath || !pressed)
        {
            return false;
        }

        if (Math.Abs(x - _cListDragX) < minimumX && Math.Abs(y - _cListDragY) < minimumY)
        {
            return false;
        }

        IReadOnlyList<string> dragged = CListSelectionCheck(dragPath)
            ? CListSelectionRead().Where(path => !_cListPort.LListLockCheck(path)).ToArray()
            : [dragPath];
        CListDragPaths = dragged;
        CListDragClear();
        CListPressPath = null;
        return dragged.Count > 0;
    }

    public void CListReleaseHandle()
    {
        CListDragClear();
        CListReleaseSelect();
    }

    private int CListIndexRead(string? path)
    {
        if (path is null)
        {
            return -1;
        }

        IReadOnlyList<string> paths = _cListPort.LListPathsRead();
        for (int index = 0; index < paths.Count; index++)
        {
            if (string.Equals(paths[index], path, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private void CListChangeHandle()
    {
        CListChanged?.Invoke();
        CListLockChanged?.Invoke(CListLockCheck());
    }

    // The first added path is selected before the entries go on.
    private void CListAddHandle(IReadOnlyList<string> added) => CListSelect(added[0]);

    private void CListEntriesHandle(IReadOnlyList<Cadroue.Application.LDocketEntry> added) =>
        CListItemsAdded?.Invoke(added);

    private void CListRemoveHandle(IReadOnlyList<string> removed)
    {
        CListRemovedApply(removed);
        CListRemoved?.Invoke(removed);
        CListSuccessorSelect();
    }

    private void CListRemovedApply(IReadOnlyList<string> removed)
    {
        _cListSelected.ExceptWith(removed);
        if (CListIndexRead(CListPathAnchor) < 0)
        {
            CListPathAnchor = null;
        }
    }

    private static string? CListSuccessorResolve(IReadOnlyList<string> removed, IReadOnlyList<string> paths, int start)
    {
        var removedSet = new HashSet<string>(removed, StringComparer.OrdinalIgnoreCase);
        for (int index = start; index < paths.Count; index++)
        {
            if (!removedSet.Contains(paths[index]))
            {
                return paths[index];
            }
        }

        for (int index = start - 1; index >= 0; index--)
        {
            if (!removedSet.Contains(paths[index]))
            {
                return paths[index];
            }
        }

        return null;
    }

    private void CListSuccessorSelect()
    {
        if (CListPathCurrent is { } current && !_cListPort.LListPathCheck(current))
        {
            CListSelect(_cListSuccessor);
        }
    }

    private void CListSelectionToggle(string path)
    {
        CListPathAnchor = path;
        if (_cListSelected.Add(path))
        {
            CListCurrentApply(path);
            return;
        }

        _cListSelected.Remove(path);
        CListCurrentApply(CListCurrentResolve());
    }

    private void CListRangeSelect(string path)
    {
        if (CListRangeRead(path) is not { } range)
        {
            return;
        }

        CListSelectedSet(range);
        CListCurrentApply(path);
    }

    private IEnumerable<string>? CListRangeRead(string path)
    {
        int rowIndex = CListIndexRead(path);
        if (rowIndex < 0)
        {
            return null;
        }

        int anchorIndex = CListIndexRead(CListPathAnchor ?? CListPathCurrent);
        if (anchorIndex < 0)
        {
            anchorIndex = rowIndex;
        }

        IReadOnlyList<string> paths = _cListPort.LListPathsRead();
        int first = Math.Min(anchorIndex, rowIndex);
        return Enumerable.Range(first, Math.Max(anchorIndex, rowIndex) - first + 1)
            .Select(index => paths[index]);
    }

    private void CListSelectedSet(IEnumerable<string> paths)
    {
        _cListSelected.Clear();
        foreach (string path in paths)
        {
            _cListSelected.Add(path);
        }
    }

    private bool CListPluralCheck(string path) => _cListSelected.Contains(path) && _cListSelected.Count > 1;

    private void CListPressSet(string path)
    {
        CListPressPath = path;
        CListPathAnchor = path;
        CListCurrentApply(path);
    }

    private string? CListCurrentResolve() =>
        CListPathCurrent is not null && _cListSelected.Contains(CListPathCurrent)
            ? CListPathCurrent
            : CListSelectionRead().LastOrDefault();

    private void CListCurrentApply(string? path)
    {
        CListPathCurrent = path;
        CListPathChanged?.Invoke(path);
        CListLockChanged?.Invoke(CListLockCheck());
    }

    private void CListDragClear()
    {
        _cListDragOrigin = false;
        CListDragPath = null;
    }
}
