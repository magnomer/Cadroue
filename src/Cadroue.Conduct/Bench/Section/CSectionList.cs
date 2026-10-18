using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The rows the sections panel paints: the held rows with their selection and painted count, the drag and the name
// edit it makes, and each row input, which ends a drag or commits the pending edit before the section gate runs it.
// CSection makes one and answers the editable switch and the edits; the rows are read from the engine's store.
public sealed class CSectionList
{
    private readonly LSectionPort _cSectionListPort;
    private readonly CSection _cSectionListSection;
    private readonly CFlow _cSectionListFlow;
    private IReadOnlyList<(string LSectionName, string LSectionPrefix, string LSectionSuffix, TimeSpan LSectionOrigin,
        TimeSpan LSectionEnd, int LSectionColor, bool LSectionHidden)> _cSectionHeld = [];
    private HashSet<int> _cSectionSelected = [];
    private int _cSectionRowCount;

    internal CSectionList(LSectionPort port, CSection section, CFlow flow)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(flow);
        _cSectionListPort = port;
        _cSectionListSection = section;
        _cSectionListFlow = flow;
        CSectionDrag = new CSectionDrag(section, this);
        CSectionEdit = new CSectionEdit(section, this);
    }

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
        _cSectionListSection.CSectionToggle(_cSectionListSection.CSectionEditable, index);
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
        if (_cSectionListSection.CSectionSeek(index, end) is { } target)
        {
            _cSectionListFlow.CFlowCursorPropagate(target, true, true);
        }

        return true;
    }

    // A double press on a row's name while editable: a drag ends, the row is selected and its editor opens.
    public bool CSectionRenameHandle(int index, int clickCount)
    {
        if (clickCount < 2 || !_cSectionListSection.CSectionEditable)
        {
            return false;
        }

        CSectionDrag.CSectionDragClear();
        if (index < 0)
        {
            return false;
        }

        _cSectionListSection.CSectionSelect(index);
        var held = _cSectionHeld[index];
        CSectionEdit.CSectionEditStart(index, held.LSectionName, held.LSectionPrefix, held.LSectionSuffix);
        return true;
    }

    // The menu's sort: the pending edit is committed first.
    public void CSectionSortRun()
    {
        CSectionEdit.CSectionEditCommit();
        _cSectionListSection.CSectionSort(_cSectionListSection.CSectionEditable);
    }

    // The painted rows are built again at the held count.
    internal void CSectionRowsRaise()
    {
        _cSectionRowCount = _cSectionHeld.Count;
        CSectionRowsChanged?.Invoke();
    }

    private void CSectionHeldSet()
    {
        _cSectionHeld = _cSectionListPort.LSectionRowsRead();
        _cSectionSelected = new HashSet<int>(_cSectionListPort.LSectionSelectedRead());
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
}
