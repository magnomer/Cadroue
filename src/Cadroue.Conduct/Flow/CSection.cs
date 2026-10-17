using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The flow's sections gate: when each section edit may run, from the flow's media, cursor and switches and the
// editable switch its owner reports. CFlow makes one per flow; the engine keeps the sections and orders each edit.
public sealed class CSection
{
    private readonly LSectionPort _cSectionPort;
    private readonly CFlow _cSectionFlow;

    internal CSection(LSectionPort port, CFlow flow)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(flow);
        _cSectionPort = port;
        _cSectionFlow = flow;
        _cSectionPort.LSectionChanged += CSectionChangeRaise;
    }

    // Told when the sections or the selection change, whoever changed them.
    public event Action? CSectionChanged;

    // The engine's store, passed through unread, only for the readers not yet moved to a gate.
    public Cadroue.Application.LSegment CSectionSegment => _cSectionPort.LSectionSegment;

    public int CSectionCount => _cSectionPort.LSectionCountRead();

    public int? CSectionSelection => _cSectionPort.LSectionSelectionRead();

    public IReadOnlyList<int> CSectionSelected => _cSectionPort.LSectionSelectedRead();

    public int CSectionVersion => _cSectionPort.LSectionVersionRead();

    // No section is held and none is selected.
    public bool CSectionEmpty => CSectionCount == 0 && CSectionSelection is null;

    // A new media, told by the flow's attach: the store takes the flow's source and drops what it held.
    internal void CSectionAttach() => _cSectionPort.LSectionAttach(_cSectionFlow.CFlowSource);

    public void CSectionLoad() => _cSectionPort.LSectionLoad(_cSectionFlow.CFlowDuration);

    public void CSectionReset() => _cSectionPort.LSectionAttach(null);

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

    private void CSectionChangeRaise() => CSectionChanged?.Invoke();
}
