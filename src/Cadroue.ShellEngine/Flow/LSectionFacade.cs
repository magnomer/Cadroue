using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// One flow's section store, each edit in its order with its trace, and every change it tells. The source the traces
// name comes from the flow gate. A word chosen by an engine answer is chosen here; whether an edit runs is the gate's.
public sealed class LSectionFacade : LSectionPort
{
    private readonly LSegment _lSectionFacadeSegment = new();
    private readonly Func<string> _lSectionFacadeSource;
    private bool _lSectionFacadeFired;

    internal LSectionFacade(Func<string> lSource)
    {
        ArgumentNullException.ThrowIfNull(lSource);
        _lSectionFacadeSource = lSource;
        _lSectionFacadeSegment.LSegmentNotice += LSectionHandle;
        _lSectionFacadeSegment.LSegmentFaultNotice += LSectionFaultHandle;
    }

    public event Action? LSectionChanged;

    public LSegment LSectionSegment => _lSectionFacadeSegment;

    public int LSectionCountRead() => _lSectionFacadeSegment.LSegmentListRead().Count;

    public int? LSectionSelectionRead() => _lSectionFacadeSegment.LSegmentSelectionRead();

    public IReadOnlyList<int> LSectionSelectedRead() => _lSectionFacadeSegment.LSegmentSelectedRead();

    public int LSectionVersionRead() => _lSectionFacadeSegment.LSegmentVersionRead();

    public IReadOnlyList<(string LSectionName, string LSectionPrefix, string LSectionSuffix, TimeSpan LSectionOrigin,
        TimeSpan LSectionEnd, int LSectionColor, bool LSectionHidden)> LSectionRowsRead() =>
        _lSectionFacadeSegment.LSegmentListRead()
            .Select(lSection => (lSection.LPieceName, lSection.LPiecePrefix, lSection.LPieceSuffix,
                lSection.LPieceOrigin, lSection.LPieceEnd, lSection.LPieceColorIndex, lSection.LPieceHidden))
            .ToArray();

    public void LSectionAttach(string? lSource)
    {
        _lSectionFacadeSegment.LSegmentSourceSet(lSource);
        _lSectionFacadeSegment.LSegmentReset();
    }

    public void LSectionLoad(TimeSpan lDuration) => _lSectionFacadeSegment.LSegmentLoad(lDuration);

    public void LSectionAdd(TimeSpan lCursor, TimeSpan lDuration, int lPalette)
    {
        _lSectionFacadeFired = false;
        _lSectionFacadeSegment.LSegmentAdd(lCursor, lDuration, LSectionColorRead(lPalette), LSectionOverlapRead());
        if (_lSectionFacadeFired)
        {
            LSectionRecord("added", _lSectionFacadeSegment.LSegmentSelectionRead()!.Value);
        }
    }

    public void LSectionStartSet(TimeSpan lCursor, TimeSpan lDuration, int lPalette)
    {
        if (_lSectionFacadeSegment.LSegmentStartSet(
                lCursor, lDuration, LSectionColorRead(lPalette), LSectionOverlapRead()) is not bool lAdded)
        {
            return;
        }

        LSectionRecord(lAdded ? "added" : "start set", _lSectionFacadeSegment.LSegmentSelectionRead()!.Value);
    }

    public void LSectionEndSet(TimeSpan lCursor, int lPalette)
    {
        if (_lSectionFacadeSegment.LSegmentEndSet(lCursor, LSectionColorRead(lPalette), LSectionOverlapRead())
            is not bool lAdded)
        {
            return;
        }

        LSectionRecord(lAdded ? "added" : "end set", _lSectionFacadeSegment.LSegmentSelectionRead()!.Value);
    }

    public void LSectionDivide(TimeSpan lCursor, int lPalette)
    {
        _lSectionFacadeFired = false;
        _lSectionFacadeSegment.LSegmentDivide(lCursor, LSectionColorRead(lPalette));
        if (!_lSectionFacadeFired)
        {
            return;
        }

        int lFirst = _lSectionFacadeSegment.LSegmentSelectionRead()!.Value;
        LSectionRecord($"split at {lCursor:hh\\:mm\\:ss\\.fff}, left half", lFirst);
        LSectionRecord("split, right half", lFirst + 1);
    }

    public void LSectionDelete(IReadOnlyList<int> lSelected, int lApproved)
    {
        if (lApproved != _lSectionFacadeSegment.LSegmentVersionRead())
        {
            LTraceEngine.LTraceWarningSeam("Section delete skipped: sections changed while confirming");
            return;
        }

        foreach (int lIndex in lSelected)
        {
            LSectionRecord("deleted", lIndex);
        }

        _lSectionFacadeSegment.LSegmentDelete(lSelected, lApproved);
    }

    public void LSectionClear(int lCount, int lApproved)
    {
        if (!_lSectionFacadeSegment.LSegmentClear(lApproved))
        {
            LTraceEngine.LTraceWarningSeam("Section clear skipped: sections changed while confirming");
            return;
        }

        LTraceEngine.LTraceInfoSeam($"Sections cleared: {lCount} section(s) removed");
    }

    public void LSectionSelect(int lIndex) => _lSectionFacadeSegment.LSegmentSelect(lIndex);

    public void LSectionSelectToggle(int lIndex) => _lSectionFacadeSegment.LSegmentSelectToggle(lIndex);

    public void LSectionRangeSelect(int lIndex) => _lSectionFacadeSegment.LSegmentRangeSelect(lIndex);

    public TimeSpan LSectionSeek(int lIndex, bool lEnd)
    {
        IReadOnlyList<LPiece> lSections = _lSectionFacadeSegment.LSegmentListRead();
        _lSectionFacadeSegment.LSegmentSelect(lIndex);
        LPiece lTarget = lSections[lIndex];
        return lEnd ? lTarget.LPieceEnd : lTarget.LPieceOrigin;
    }

    public void LSectionToggle(int lIndex)
    {
        _lSectionFacadeFired = false;
        _lSectionFacadeSegment.LSegmentToggle(lIndex);
        if (!_lSectionFacadeFired)
        {
            return;
        }

        bool lHidden = _lSectionFacadeSegment.LSegmentListRead()[lIndex].LPieceHidden;
        LSectionRecord(lHidden ? "turned off" : "turned on", lIndex);
    }

    public bool LSectionMove(int lSource, int lTarget)
    {
        if (_lSectionFacadeSegment.LSegmentMove(lSource, lTarget) is not int lInsert)
        {
            return false;
        }

        LSectionRecord($"moved from #{lSource + 1} to", lInsert);
        return true;
    }

    public bool LSectionSort()
    {
        _lSectionFacadeFired = false;
        _lSectionFacadeSegment.LSegmentSort();
        if (!_lSectionFacadeFired)
        {
            return false;
        }

        LTraceEngine.LTraceInfoSeam(
            $"Sections sorted by name: {_lSectionFacadeSegment.LSegmentListRead().Count} section(s)");
        return true;
    }

    public void LSectionNameSet(int lIndex, string lName, string? lPrefix, string? lSuffix)
    {
        string lWas = _lSectionFacadeSegment.LSegmentListRead()[lIndex].LPieceName;
        _lSectionFacadeFired = false;
        _lSectionFacadeSegment.LSegmentNameSet(lIndex, lName, lPrefix, lSuffix);
        if (_lSectionFacadeFired)
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
        IReadOnlyList<LPiece> lSections = LSweepCombine.LSweepCombineResolve(
            _lSectionFacadeSegment.LSegmentListRead(), lExcluded, lKept, lBoundaries, lDuration, lPalette);
        int? lSelect = lSections.Count > 0 ? 0 : null;
        return _lSectionFacadeSegment.LSegmentBoundSet(lSections, lSelect, lDuration);
    }

    private void LSectionHandle(IReadOnlyList<LPiece> lSections, int? lActive)
    {
        _lSectionFacadeFired = true;
        LSectionChanged?.Invoke();
    }

    private void LSectionFaultHandle(LSegmentFault lFault, int lCount)
    {
        string lSource = _lSectionFacadeSource();
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
        IReadOnlyList<LPiece> lSections = _lSectionFacadeSegment.LSegmentListRead();
        string lSource = _lSectionFacadeSource();
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

    private int LSectionColorRead(int lPalette) => _lSectionFacadeSegment.LSegmentListRead().Count % lPalette;

    private static bool LSectionOverlapRead() => LPreference.LPreferenceStateCurrent.LPreferenceOverlapAllowed;
}
