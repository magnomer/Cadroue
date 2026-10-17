using Cadroue.Application;

namespace Cadroue.ShellEngine;

// One flow's sections: each member is one edit the engine orders with the trace it leaves.
public interface LSectionPort
{
    event Action? LSectionChanged;

    // The store itself, only for the readers not yet moved to a gate.
    LSegment LSectionSegment { get; }

    int LSectionCountRead();

    int? LSectionSelectionRead();

    IReadOnlyList<int> LSectionSelectedRead();

    int LSectionVersionRead();

    void LSectionAttach(string? lSource);

    void LSectionLoad(TimeSpan lDuration);

    void LSectionAdd(TimeSpan lCursor, TimeSpan lDuration, int lPalette);

    void LSectionStartSet(TimeSpan lCursor, TimeSpan lDuration, int lPalette);

    void LSectionEndSet(TimeSpan lCursor, int lPalette);

    void LSectionDivide(TimeSpan lCursor, int lPalette);

    void LSectionDelete(IReadOnlyList<int> lSelected, int lApproved);

    void LSectionClear(int lCount, int lApproved);

    void LSectionSelect(int lIndex);

    void LSectionSelectToggle(int lIndex);

    void LSectionRangeSelect(int lIndex);

    TimeSpan LSectionSeek(int lIndex, bool lEnd);

    void LSectionToggle(int lIndex);

    bool LSectionMove(int lSource, int lTarget);

    bool LSectionSort();

    void LSectionNameSet(int lIndex, string lName, string? lPrefix, string? lSuffix);

    bool LSectionCombineApply(
        IReadOnlyList<LSweepSpan> lExcluded,
        IReadOnlyList<LSweepSpan> lKept,
        IReadOnlyList<LSweepBoundary> lBoundaries,
        TimeSpan lDuration,
        int lPalette);
}
