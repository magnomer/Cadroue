using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The flow's sections gate: when each section edit may run, from the flow's media, cursor and switches and the
// editable switch its owner reports. CFlow makes one per flow; the engine keeps the sections and orders each edit.
// It also holds the rows its driver paints, with their selection and count, and makes the drag and the name edit.
public sealed class CSection
{
    private readonly LSectionPort _cSectionPort;
    private readonly CFlow _cSectionFlow;
    private IReadOnlyList<(string LSectionName, string LSectionPrefix, string LSectionSuffix, TimeSpan LSectionOrigin,
        TimeSpan LSectionEnd, int LSectionColor, bool LSectionHidden)> _cSectionHeld = [];
    private HashSet<int> _cSectionSelected = [];
    private int _cSectionRowCount;
    private bool _cSectionEditable = true;

    internal CSection(LSectionPort port, CFlow flow)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(flow);
        _cSectionPort = port;
        _cSectionFlow = flow;
        _cSectionPort.LSectionChanged += CSectionChangeRaise;
        CSectionDrag = new CSectionDrag(this);
        CSectionEdit = new CSectionEdit(this);
    }

    // Told when the sections or the selection change, whoever changed them.
    public event Action? CSectionChanged;

    // Told with the editable switch each time it turns.
    public event Action<bool>? CSectionEditableChanged;

    // Told when the painted rows must be built again.
    public event Action? CSectionRowsChanged;

    // Told when a change left the held rows as they were, so only the selection repaints.
    public event Action? CSectionSelectApplied;

    public CSectionDrag CSectionDrag { get; }

    public CSectionEdit CSectionEdit { get; }

    // The held rows' count for the header; null while no row is held.
    public int? CSectionHeldCount => _cSectionHeld.Count == 0 ? null : _cSectionHeld.Count;

    // The rows as last painted, which bound the drag's slots.
    internal int CSectionRowCount => _cSectionRowCount;

    // The editable switch the owner turns; editable until it is turned off.
    public bool CSectionEditable => _cSectionEditable;

    // The engine's store, passed through unread, only for the readers not yet moved to a gate.
    public Cadroue.Application.LSegment CSectionSegment => _cSectionPort.LSectionSegment;

    public int CSectionCount => _cSectionPort.LSectionCountRead();

    public int? CSectionSelection => _cSectionPort.LSectionSelectionRead();

    public IReadOnlyList<int> CSectionSelected => _cSectionPort.LSectionSelectedRead();

    public int CSectionVersion => _cSectionPort.LSectionVersionRead();

    // No section is held and none is selected.
    public bool CSectionEmpty => CSectionCount == 0 && CSectionSelection is null;

    // A new media, told by the flow's attach: the store takes the flow's source and drops what it held.
    internal void CSectionAttach()
    {
        _cSectionPort.LSectionAttach(_cSectionFlow.CFlowSource);
        CSectionChanged?.Invoke();
    }

    public void CSectionLoad() => _cSectionPort.LSectionLoad(_cSectionFlow.CFlowDuration);

    // The store drops what it held without a tell of its own, so the gate tells.
    public void CSectionReset()
    {
        _cSectionPort.LSectionAttach(null);
        CSectionChanged?.Invoke();
    }

    // The owner turns the switch: turned off, the flow's name editor hides; a turn is told. True when it turned.
    public bool CSectionEditableSet(bool editable)
    {
        if (!editable)
        {
            _cSectionFlow.CFlowName.CFlowNameHide();
        }

        if (_cSectionEditable == editable)
        {
            return false;
        }

        _cSectionEditable = editable;
        CSectionEditableChanged?.Invoke(editable);
        return true;
    }

    public void CSectionAdd(bool editable)
    {
        if (!editable || !_cSectionFlow.CFlowSourceCheck())
        {
            return;
        }

        _cSectionPort.LSectionAdd(
            _cSectionFlow.CFlowCursor, _cSectionFlow.CFlowDuration, _cSectionFlow.CFlowPaletteCount);
    }

    public void CSectionStartSet(bool editable)
    {
        if (!editable || !_cSectionFlow.CFlowSourceCheck())
        {
            return;
        }

        _cSectionPort.LSectionStartSet(
            _cSectionFlow.CFlowCursor, _cSectionFlow.CFlowDuration, _cSectionFlow.CFlowPaletteCount);
    }

    public void CSectionEndSet(bool editable)
    {
        if (!editable || !_cSectionFlow.CFlowSourceCheck())
        {
            return;
        }

        _cSectionPort.LSectionEndSet(_cSectionFlow.CFlowCursor, _cSectionFlow.CFlowPaletteCount);
    }

    public void CSectionDivide(bool editable)
    {
        if (!editable)
        {
            return;
        }

        _cSectionPort.LSectionDivide(_cSectionFlow.CFlowCursor, _cSectionFlow.CFlowPaletteCount);
    }

    // The confirmed delete; the engine skips it when the sections changed since the selection was read.
    public void CSectionDelete(IReadOnlyList<int> selected, int approved) =>
        _cSectionPort.LSectionDelete(selected, approved);

    // The confirmed clear; the engine skips it when the sections changed since the count was read.
    public void CSectionClear(int count, int approved) => _cSectionPort.LSectionClear(count, approved);

    public void CSectionSelect(int index) => _cSectionPort.LSectionSelect(index);

    public void CSectionSelectToggle(int index) => _cSectionPort.LSectionSelectToggle(index);

    public void CSectionRangeSelect(int index) => _cSectionPort.LSectionRangeSelect(index);

    // A section's start or end, selected, while commands run on a held range; null when the seek may not run.
    public TimeSpan? CSectionSeek(int index, bool end)
    {
        if (!_cSectionFlow.CFlowCommandActive || !_cSectionFlow.CFlowLoaded || index < 0 || index >= CSectionCount)
        {
            return null;
        }

        return _cSectionPort.LSectionSeek(index, end);
    }

    public void CSectionToggle(bool editable, int index)
    {
        if (!editable)
        {
            return;
        }

        _cSectionPort.LSectionToggle(index);
    }

    public bool CSectionMove(bool editable, int source, int target) =>
        editable && _cSectionPort.LSectionMove(source, target);

    public bool CSectionSort(bool editable) => editable && _cSectionPort.LSectionSort();

    public void CSectionNameSet(bool editable, int index, string name, string? prefix, string? suffix)
    {
        if (!editable || index < 0 || index >= CSectionCount)
        {
            return;
        }

        _cSectionPort.LSectionNameSet(index, name, prefix, suffix);
    }

    // A sweep's result replaces the sections while a range is held and the flow is not unloaded.
    public bool CSectionCombineApply(
        IReadOnlyList<LSweepSpan> excluded,
        IReadOnlyList<LSweepSpan> kept,
        IReadOnlyList<LSweepBoundary> boundaries)
    {
        if (_cSectionFlow.CFlowUnloaded || !_cSectionFlow.CFlowLoaded)
        {
            return false;
        }

        return _cSectionPort.LSectionCombineApply(
            excluded, kept, boundaries, _cSectionFlow.CFlowDuration, _cSectionFlow.CFlowPaletteCount);
    }

    // A sweep: the editable switch turns off while the scan runs, the scan's result replaces the sections, and the
    // switch turns back on unless the item is locked by then, whatever the scan threw. False when the result may not
    // apply.
    public async Task<bool> CSectionSweepRun(
        Func<Task> scan,
        IReadOnlyList<LSweepSpan> excluded,
        IReadOnlyList<LSweepSpan> kept,
        IReadOnlyList<LSweepBoundary> boundaries,
        Func<bool> locked)
    {
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentNullException.ThrowIfNull(locked);
        CSectionEditableSet(false);
        try
        {
            await scan();
            return CSectionCombineApply(excluded, kept, boundaries);
        }
        finally
        {
            CSectionEditableSet(!locked());
        }
    }

    // The rows each painted row is built from, in store order, with the selection and the editor marked.
    public IReadOnlyList<CSectionRow> CSectionRowsRead() =>
        Enumerable.Range(0, _cSectionHeld.Count).Select(CSectionRowCreate).ToArray();

    public bool CSectionSelectedCheck(int index) => _cSectionSelected.Contains(index);

    // A driver starts on the store as it stands: its rows, selection and count are held without a tell.
    public void CSectionRowsSync()
    {
        CSectionHeldSet();
        _cSectionRowCount = _cSectionHeld.Count;
    }

    // A change the flow tells: the held rows and selection follow the store. A running drag keeps the painted rows;
    // rows left as they were repaint only the selection; other rows are built again.
    public void CSectionRowsUpdate()
    {
        var was = _cSectionHeld;
        CSectionHeldSet();
        bool same = was.SequenceEqual(_cSectionHeld) && _cSectionRowCount == _cSectionHeld.Count;
        if (CSectionDrag.CSectionDragActive)
        {
            return;
        }

        if (same)
        {
            CSectionSelectApplied?.Invoke();
            return;
        }

        CSectionRowsRaise();
    }

    // The editable switch turned: turned off, a drag ends and the pending edit is dropped.
    public void CSectionEditableHandle(bool editable)
    {
        if (editable)
        {
            return;
        }

        CSectionDrag.CSectionDragClear();
        if (CSectionEdit.CSectionEditIndex is not null)
        {
            CSectionEdit.CSectionEditCancel();
        }
    }

    // A row's switch: a drag ends and the pending edit is committed before the section turns on or off.
    public void CSectionToggleHandle(int index)
    {
        CSectionDrag.CSectionDragClear();
        CSectionEdit.CSectionEditCommit();
        CSectionToggle(CSectionEditable, index);
    }

    // A double press on a row's time: a drag ends, the pending edit is committed and the section's start or end,
    // selected, takes the cursor. Handled for any row, whether the seek may run or not.
    public bool CSectionSeekHandle(int index, int clickCount, bool end)
    {
        if (clickCount < 2)
        {
            return false;
        }

        CSectionDrag.CSectionDragClear();
        if (index < 0)
        {
            return false;
        }

        CSectionEdit.CSectionEditCommit();
        if (CSectionSeek(index, end) is { } target)
        {
            _cSectionFlow.CFlowCursorPropagate(target, true, true);
        }

        return true;
    }

    // A double press on a row's name while editable: a drag ends, the row is selected and its editor opens.
    public bool CSectionRenameHandle(int index, int clickCount)
    {
        if (clickCount < 2 || !CSectionEditable)
        {
            return false;
        }

        CSectionDrag.CSectionDragClear();
        if (index < 0)
        {
            return false;
        }

        CSectionSelect(index);
        var held = _cSectionHeld[index];
        CSectionEdit.CSectionEditStart(index, held.LSectionName, held.LSectionPrefix, held.LSectionSuffix);
        return true;
    }

    // The menu's sort: the pending edit is committed first.
    public void CSectionSortRun()
    {
        CSectionEdit.CSectionEditCommit();
        CSectionSort(CSectionEditable);
    }

    // The painted rows are built again at the held count.
    internal void CSectionRowsRaise()
    {
        _cSectionRowCount = _cSectionHeld.Count;
        CSectionRowsChanged?.Invoke();
    }

    private void CSectionHeldSet()
    {
        _cSectionHeld = _cSectionPort.LSectionRowsRead();
        _cSectionSelected = new HashSet<int>(_cSectionPort.LSectionSelectedRead());
    }

    // An unnamed row says so; set affixes are listed; a span below zero is held at zero.
    private CSectionRow CSectionRowCreate(int index)
    {
        var held = _cSectionHeld[index];
        TimeSpan span = held.LSectionEnd - held.LSectionOrigin;
        string[] affixes = new[] { held.LSectionPrefix, held.LSectionSuffix }
            .Where(affix => !string.IsNullOrEmpty(affix))
            .ToArray();
        return new CSectionRow(
            index,
            held.LSectionName,
            string.IsNullOrEmpty(held.LSectionName),
            held.LSectionPrefix,
            held.LSectionSuffix,
            !string.IsNullOrEmpty(held.LSectionPrefix),
            !string.IsNullOrEmpty(held.LSectionSuffix),
            affixes,
            held.LSectionOrigin,
            held.LSectionEnd,
            span < TimeSpan.Zero ? TimeSpan.Zero : span,
            held.LSectionColor,
            held.LSectionHidden,
            _cSectionSelected.Contains(index),
            CSectionEdit.CSectionEditIndex == index);
    }

    private void CSectionChangeRaise() => CSectionChanged?.Invoke();
}
