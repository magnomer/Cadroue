using System;
using System.Collections.Generic;
using Cadroue.Application;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TSectionPort : LSectionPort
{
    public event Action? LSectionChanged;

    public LSegment LSectionSegment { get; } = new();

    public Func<string>? TSectionSource { get; set; }

    public int TSectionCount { get; set; }

    public int? TSectionSelection { get; set; }

    public IReadOnlyList<int> TSectionSelected { get; set; } = [];

    public int TSectionVersion { get; set; }

    public TimeSpan TSectionBound { get; set; }

    public bool TSectionAnswer { get; set; } = true;

    public List<string> TSectionCalls { get; } = [];

    public void TSectionRaise() => LSectionChanged?.Invoke();

    public int LSectionCountRead() => TSectionCount;

    public int? LSectionSelectionRead() => TSectionSelection;

    public IReadOnlyList<int> LSectionSelectedRead() => TSectionSelected;

    public int LSectionVersionRead() => TSectionVersion;

    public void LSectionAttach(string? lSource) => TSectionCalls.Add($"attach:{lSource ?? "null"}");

    public void LSectionLoad(TimeSpan lDuration) => TSectionCalls.Add($"load:{lDuration.TotalSeconds}");

    public void LSectionAdd(TimeSpan lCursor, TimeSpan lDuration, int lPalette) =>
        TSectionCalls.Add($"add:{lCursor.TotalSeconds}:{lDuration.TotalSeconds}:{lPalette}");

    public void LSectionStartSet(TimeSpan lCursor, TimeSpan lDuration, int lPalette) =>
        TSectionCalls.Add($"start:{lCursor.TotalSeconds}:{lDuration.TotalSeconds}:{lPalette}");

    public void LSectionEndSet(TimeSpan lCursor, int lPalette) =>
        TSectionCalls.Add($"end:{lCursor.TotalSeconds}:{lPalette}");

    public void LSectionDivide(TimeSpan lCursor, int lPalette) =>
        TSectionCalls.Add($"divide:{lCursor.TotalSeconds}:{lPalette}");

    public void LSectionDelete(IReadOnlyList<int> lSelected, int lApproved) =>
        TSectionCalls.Add($"delete:{string.Join(",", lSelected)}:{lApproved}");

    public void LSectionClear(int lCount, int lApproved) => TSectionCalls.Add($"clear:{lCount}:{lApproved}");

    public void LSectionSelect(int lIndex) => TSectionCalls.Add($"select:{lIndex}");

    public void LSectionSelectToggle(int lIndex) => TSectionCalls.Add($"selectToggle:{lIndex}");

    public void LSectionRangeSelect(int lIndex) => TSectionCalls.Add($"rangeSelect:{lIndex}");

    public TimeSpan LSectionSeek(int lIndex, bool lEnd)
    {
        TSectionCalls.Add($"seek:{lIndex}:{lEnd}");
        return TSectionBound;
    }

    public void LSectionToggle(int lIndex) => TSectionCalls.Add($"toggle:{lIndex}");

    public bool LSectionMove(int lSource, int lTarget)
    {
        TSectionCalls.Add($"move:{lSource}:{lTarget}");
        return TSectionAnswer;
    }

    public bool LSectionSort()
    {
        TSectionCalls.Add("sort");
        return TSectionAnswer;
    }

    public void LSectionNameSet(int lIndex, string lName, string? lPrefix, string? lSuffix) =>
        TSectionCalls.Add($"name:{lIndex}:{lName}:{lPrefix}:{lSuffix}");

    public bool LSectionCombineApply(
        IReadOnlyList<LSweepSpan> lExcluded,
        IReadOnlyList<LSweepSpan> lKept,
        IReadOnlyList<LSweepBoundary> lBoundaries,
        TimeSpan lDuration,
        int lPalette)
    {
        TSectionCalls.Add($"combine:{lDuration.TotalSeconds}:{lPalette}");
        return TSectionAnswer;
    }
}
