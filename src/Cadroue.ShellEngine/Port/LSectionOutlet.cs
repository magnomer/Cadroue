using Cadroue.Application;

namespace Cadroue.ShellEngine;

// The engine's face for one flow's sections: every member forwards to the same-named member of that flow's section
// facade, which the flow outlet has the engine make. It rejects a null facade and keeps it for every forward.
public sealed class LSectionOutlet : LSectionPort
{
    private readonly LSectionFacade _lSectionOutletEngine;

    internal LSectionOutlet(LSectionFacade lFacade)
    {
        ArgumentNullException.ThrowIfNull(lFacade);
        _lSectionOutletEngine = lFacade;
    }

    public event Action? LSectionChanged
    {
        add => _lSectionOutletEngine.LSectionChanged += value;
        remove => _lSectionOutletEngine.LSectionChanged -= value;
    }

    public LSegment LSectionSegment => _lSectionOutletEngine.LSectionSegment;

    public int LSectionCountRead() => _lSectionOutletEngine.LSectionCountRead();

    public int? LSectionSelectionRead() => _lSectionOutletEngine.LSectionSelectionRead();

    public IReadOnlyList<int> LSectionSelectedRead() => _lSectionOutletEngine.LSectionSelectedRead();

    public int LSectionVersionRead() => _lSectionOutletEngine.LSectionVersionRead();

    public IReadOnlyList<(string LSectionName, string LSectionPrefix, string LSectionSuffix, TimeSpan LSectionOrigin,
        TimeSpan LSectionEnd, int LSectionColor, bool LSectionHidden)> LSectionRowsRead() =>
        _lSectionOutletEngine.LSectionRowsRead();

    public void LSectionAttach(string? lSource) => _lSectionOutletEngine.LSectionAttach(lSource);

    public void LSectionLoad(TimeSpan lDuration) => _lSectionOutletEngine.LSectionLoad(lDuration);

    public void LSectionAdd(TimeSpan lCursor, TimeSpan lDuration, int lPalette) =>
        _lSectionOutletEngine.LSectionAdd(lCursor, lDuration, lPalette);

    public void LSectionStartSet(TimeSpan lCursor, TimeSpan lDuration, int lPalette) =>
        _lSectionOutletEngine.LSectionStartSet(lCursor, lDuration, lPalette);

    public void LSectionEndSet(TimeSpan lCursor, int lPalette) =>
        _lSectionOutletEngine.LSectionEndSet(lCursor, lPalette);

    public void LSectionDivide(TimeSpan lCursor, int lPalette) =>
        _lSectionOutletEngine.LSectionDivide(lCursor, lPalette);

    public void LSectionDelete(IReadOnlyList<int> lSelected, int lApproved) =>
        _lSectionOutletEngine.LSectionDelete(lSelected, lApproved);

    public void LSectionClear(int lCount, int lApproved) => _lSectionOutletEngine.LSectionClear(lCount, lApproved);

    public void LSectionSelect(int lIndex) => _lSectionOutletEngine.LSectionSelect(lIndex);

    public void LSectionSelectToggle(int lIndex) => _lSectionOutletEngine.LSectionSelectToggle(lIndex);

    public void LSectionRangeSelect(int lIndex) => _lSectionOutletEngine.LSectionRangeSelect(lIndex);

    public TimeSpan LSectionSeek(int lIndex, bool lEnd) => _lSectionOutletEngine.LSectionSeek(lIndex, lEnd);

    public void LSectionToggle(int lIndex) => _lSectionOutletEngine.LSectionToggle(lIndex);

    public bool LSectionMove(int lSource, int lTarget) => _lSectionOutletEngine.LSectionMove(lSource, lTarget);

    public bool LSectionSort() => _lSectionOutletEngine.LSectionSort();

    public void LSectionNameSet(int lIndex, string lName, string? lPrefix, string? lSuffix) =>
        _lSectionOutletEngine.LSectionNameSet(lIndex, lName, lPrefix, lSuffix);

    public bool LSectionCombineApply(
        IReadOnlyList<LSweepSpan> lExcluded,
        IReadOnlyList<LSweepSpan> lKept,
        IReadOnlyList<LSweepBoundary> lBoundaries,
        TimeSpan lDuration,
        int lPalette) =>
        _lSectionOutletEngine.LSectionCombineApply(lExcluded, lKept, lBoundaries, lDuration, lPalette);
}
