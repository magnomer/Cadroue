using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// One flow's section store, each edit in its order with its trace, and every change it tells. The source the traces
// name comes from the flow gate. A word chosen by an engine answer is chosen here; whether an edit runs is the gate's.
public sealed class LSectionOutlet : LSectionPort
{
    private readonly LSegment _lSectionOutletSegment = new();
    private readonly Func<string> _lSectionOutletSource;
    private bool _lSectionOutletFired;

    public LSectionOutlet(Func<string> lSource)
    {
        ArgumentNullException.ThrowIfNull(lSource);
        _lSectionOutletSource = lSource;
        _lSectionOutletSegment.LSegmentNotice += LSectionHandle;
        _lSectionOutletSegment.LSegmentFaultNotice += LSectionFaultHandle;
    }

    public event Action? LSectionChanged;

    public LSegment LSectionSegment => _lSectionOutletSegment;

    public int LSectionCountRead() => _lSectionOutletSegment.LSegmentListRead().Count;

    public int? LSectionSelectionRead() => _lSectionOutletSegment.LSegmentSelectionRead();

    public IReadOnlyList<int> LSectionSelectedRead() => _lSectionOutletSegment.LSegmentSelectedRead();

    public int LSectionVersionRead() => _lSectionOutletSegment.LSegmentVersionRead();

    public void LSectionAttach(string? lSource)
    {
        _lSectionOutletSegment.LSegmentSourceSet(lSource);
        _lSectionOutletSegment.LSegmentReset();
    }

    public void LSectionLoad(TimeSpan lDuration) => _lSectionOutletSegment.LSegmentLoad(lDuration);

    public void LSectionAdd(TimeSpan lCursor, TimeSpan lDuration, int lPalette)
    {
        _lSectionOutletFired = false;
        _lSectionOutletSegment.LSegmentAdd(lCursor, lDuration, LSectionColorRead(lPalette), LSectionOverlapRead());
        if (_lSectionOutletFired)
        {
            LSectionRecord("added", _lSectionOutletSegment.LSegmentSelectionRead()!.Value);
        }
    }

    public void LSectionStartSet(TimeSpan lCursor, TimeSpan lDuration, int lPalette)
    {
        if (_lSectionOutletSegment.LSegmentStartSet(
                lCursor, lDuration, LSectionColorRead(lPalette), LSectionOverlapRead()) is not bool lAdded)
        {
            return;
        }

        LSectionRecord(lAdded ? "added" : "start set", _lSectionOutletSegment.LSegmentSelectionRead()!.Value);
    }

    public void LSectionEndSet(TimeSpan lCursor, int lPalette)
    {
        if (_lSectionOutletSegment.LSegmentEndSet(lCursor, LSectionColorRead(lPalette), LSectionOverlapRead())
            is not bool lAdded)
        {
            return;
        }

        LSectionRecord(lAdded ? "added" : "end set", _lSectionOutletSegment.LSegmentSelectionRead()!.Value);
    }

    public void LSectionDivide(TimeSpan lCursor, int lPalette)
    {
        _lSectionOutletFired = false;
        _lSectionOutletSegment.LSegmentDivide(lCursor, LSectionColorRead(lPalette));
        if (!_lSectionOutletFired)
        {
            return;
        }

        int lFirst = _lSectionOutletSegment.LSegmentSelectionRead()!.Value;
        LSectionRecord($"split at {lCursor:hh\\:mm\\:ss\\.fff}, left half", lFirst);
        LSectionRecord("split, right half", lFirst + 1);
    }

    public void LSectionDelete(IReadOnlyList<int> lSelected, int lApproved)
    {
        if (lApproved != _lSectionOutletSegment.LSegmentVersionRead())
        {
            LTraceEngine.LTraceWarningSeam("Section delete skipped: sections changed while confirming");
            return;
        }

        foreach (int lIndex in lSelected)
        {
            LSectionRecord("deleted", lIndex);
        }

        _lSectionOutletSegment.LSegmentDelete(lSelected, lApproved);
    }

    public void LSectionClear(int lCount, int lApproved)
    {
        if (!_lSectionOutletSegment.LSegmentClear(lApproved))
        {
            LTraceEngine.LTraceWarningSeam("Section clear skipped: sections changed while confirming");
            return;
        }

        LTraceEngine.LTraceInfoSeam($"Sections cleared: {lCount} section(s) removed");
    }

    public void LSectionSelect(int lIndex) => _lSectionOutletSegment.LSegmentSelect(lIndex);

    public void LSectionSelectToggle(int lIndex) => _lSectionOutletSegment.LSegmentSelectToggle(lIndex);

    public void LSectionRangeSelect(int lIndex) => _lSectionOutletSegment.LSegmentRangeSelect(lIndex);

    public TimeSpan LSectionSeek(int lIndex, bool lEnd)
    {
        IReadOnlyList<LPiece> lSections = _lSectionOutletSegment.LSegmentListRead();
        _lSectionOutletSegment.LSegmentSelect(lIndex);
        LPiece lTarget = lSections[lIndex];
        return lEnd ? lTarget.LPieceEnd : lTarget.LPieceOrigin;
    }

    public void LSectionToggle(int lIndex)
    {
        _lSectionOutletFired = false;
        _lSectionOutletSegment.LSegmentToggle(lIndex);
        if (!_lSectionOutletFired)
        {
            return;
        }

        bool lHidden = _lSectionOutletSegment.LSegmentListRead()[lIndex].LPieceHidden;
        LSectionRecord(lHidden ? "turned off" : "turned on", lIndex);
    }

    public bool LSectionMove(int lSource, int lTarget)
    {
        if (_lSectionOutletSegment.LSegmentMove(lSource, lTarget) is not int lInsert)
        {
            return false;
        }

        LSectionRecord($"moved from #{lSource + 1} to", lInsert);
        return true;
    }

    public bool LSectionSort()
    {
        _lSectionOutletFired = false;
        _lSectionOutletSegment.LSegmentSort();
        if (!_lSectionOutletFired)
        {
            return false;
        }

        LTraceEngine.LTraceInfoSeam(
            $"Sections sorted by name: {_lSectionOutletSegment.LSegmentListRead().Count} section(s)");
        return true;
    }

    public void LSectionNameSet(int lIndex, string lName, string? lPrefix, string? lSuffix)
    {
        string lWas = _lSectionOutletSegment.LSegmentListRead()[lIndex].LPieceName;
        _lSectionOutletFired = false;
        _lSectionOutletSegment.LSegmentNameSet(lIndex, lName, lPrefix, lSuffix);
        if (_lSectionOutletFired)
        {
            LSectionRecord(string.IsNullOrEmpty(lWas) ? "named" : $"renamed from '{lWas}' to", lIndex);
        }
    }

    // The combined sections replace the held ones; the first is selected when any is left.
    public bool LSectionCombineApply(
        IReadOnlyList<LSweepSpan> lExcluded,
        IReadOnlyList<LSweepSpan> lKept,
        IReadOnlyList<LSweepBoundary> lBoundaries,
        TimeSpan lDuration,
        int lPalette)
    {
        IReadOnlyList<LPiece> lSections = LSweep.LSweepCombineResolve(
            _lSectionOutletSegment.LSegmentListRead(), lExcluded, lKept, lBoundaries, lDuration, lPalette);
        int? lSelect = lSections.Count > 0 ? 0 : null;
        return _lSectionOutletSegment.LSegmentBoundSet(lSections, lSelect, lDuration);
    }

    private void LSectionHandle(IReadOnlyList<LPiece> lSections, int? lActive)
    {
        _lSectionOutletFired = true;
        LSectionChanged?.Invoke();
    }

    private void LSectionFaultHandle(LSegmentFault lFault, int lCount)
    {
        string lSource = _lSectionOutletSource();
        LTraceEngine.LTraceWarningSeam(lFault switch
        {
            LSegmentFault.LSegmentFaultCeiling =>
                $"Section change refused in '{lSource}': {lCount} section(s) exceed the {LPiece.LPieceCeiling} limit",
            LSegmentFault.LSegmentFaultInvalid =>
                $"Section restore in '{lSource}' dropped {lCount} invalid saved section(s)",
            _ => $"Section save failed in '{lSource}': {lCount} section(s) kept in memory only"
        });
    }

    private void LSectionRecord(string lAction, int lIndex)
    {
        IReadOnlyList<LPiece> lSections = _lSectionOutletSegment.LSegmentListRead();
        string lSource = _lSectionOutletSource();
        if (lIndex < 0 || lIndex >= lSections.Count)
        {
            LTraceEngine.LTraceInfoSeam($"Section {lAction} in '{lSource}': {lSections.Count} section(s) remain");
            return;
        }

        LPiece lSection = lSections[lIndex];
        string lName = string.IsNullOrEmpty(lSection.LPieceName) ? "unnamed" : $"'{lSection.LPieceName}'";
        LTraceEngine.LTraceInfoSeam(
            $"Section {lAction} #{lIndex + 1} of {lSections.Count} in '{lSource}': {lName} "
            + $"{lSection.LPieceOrigin:hh\\:mm\\:ss\\.fff}-{lSection.LPieceEnd:hh\\:mm\\:ss\\.fff}");
    }

    private int LSectionColorRead(int lPalette) => _lSectionOutletSegment.LSegmentListRead().Count % lPalette;

    private static bool LSectionOverlapRead() => LPreference.LPreferenceStateCurrent.LPreferenceOverlapAllowed;
}
