using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class LGroupRecord
{
    public string LGroupRecordName { get; set; } = string.Empty;

    public List<string> LGroupRecordPaths { get; } = [];
}

public sealed class LGroup
{
    private readonly LGroupSelection lGroupOwner;
    private readonly List<LGroupRecord> lGroupRecords = [];
    private bool lGroupMinimized;
    private int? lGroupSourceIndex;
    private string? lGroupDragPath;

    public event Action? LGroupChange;
    public event Action<bool>? LGroupMinimizeChange;

    public LGroup(LGroupSelection lGroupSelection)
    {
        lGroupOwner = lGroupSelection;
    }

    public IReadOnlyList<LGroupRecord> LGroupRecords => lGroupRecords;

    public bool LGroupMinimized => lGroupMinimized;

    public int? LGroupSourceIndex => lGroupSourceIndex;

    public string? LGroupDragPath => lGroupDragPath;

    public void LGroupMinimizedSet(bool lMinimized)
    {
        if (lGroupMinimized == lMinimized)
        {
            return;
        }

        lGroupMinimized = lMinimized;
        LGroupMinimizeChange?.Invoke(lMinimized);
    }

    public void LGroupDragSet(int? lSourceIndex, string? lDragPath)
    {
        lGroupSourceIndex = lSourceIndex;
        lGroupDragPath = lDragPath;
    }

    public void LGroupSort()
    {
        if (lGroupRecords.Count == 0)
        {
            return;
        }

        foreach (LGroupRecord lRecord in lGroupRecords)
        {
            IReadOnlyList<string> lSorted = LSeries.LSeriesPathsSort(lRecord.LGroupRecordPaths);
            lRecord.LGroupRecordPaths.Clear();
            lRecord.LGroupRecordPaths.AddRange(lSorted);
        }

        LGroupChange?.Invoke();
    }

    public void LGroupPathsRemove(IReadOnlyList<string> lPaths)
    {
        var lTargetSet = new HashSet<string>(lPaths, StringComparer.OrdinalIgnoreCase);
        int lRemovedCount = 0;
        foreach (LGroupRecord lRecord in lGroupRecords)
        {
            lRemovedCount += lRecord.LGroupRecordPaths.RemoveAll(lTargetSet.Contains);
        }

        if (lRemovedCount == 0)
        {
            return;
        }

        int lEmptied = lGroupRecords.RemoveAll(lRecord => lRecord.LGroupRecordPaths.Count == 0);
        LTraceLog.LTraceInfoRecord(
            $"Group: removed {lRemovedCount} unloaded file(s) from groups, {lEmptied} group(s) emptied");
        LGroupChange?.Invoke();
    }

    public void LGroupAutoApply(IReadOnlyList<string> lFiles, bool? lStrict = null)
    {
        IReadOnlyList<LSeriesGroup> lGroups = lGroupOwner.LGroupResolve(lFiles, lStrict);
        lGroupRecords.Clear();
        foreach (LSeriesGroup lSeries in lGroups)
        {
            var lRecord = new LGroupRecord { LGroupRecordName = lSeries.LSeriesName };
            lRecord.LGroupRecordPaths.AddRange(lSeries.LSeriesPaths);
            lGroupRecords.Add(lRecord);
        }

        LGroupChange?.Invoke();
    }

    public bool LGroupItemMove(int lSourceIndex, string lPath, int lTargetIndex, int lInsertAt)
    {
        if (!LGroupIndexCheck(lSourceIndex) || !LGroupIndexCheck(lTargetIndex))
        {
            return false;
        }

        List<string> lSourcePaths = lGroupRecords[lSourceIndex].LGroupRecordPaths;
        int lRemovedIndex = lSourcePaths.FindIndex(lExisting =>
            string.Equals(lExisting, lPath, StringComparison.OrdinalIgnoreCase));
        if (lRemovedIndex >= 0)
        {
            lSourcePaths.RemoveAt(lRemovedIndex);
            if (lSourceIndex == lTargetIndex && lRemovedIndex < lInsertAt)
            {
                lInsertAt--;
            }
        }

        LGroupPathInsert(lGroupRecords[lTargetIndex].LGroupRecordPaths, lPath, lInsertAt);
        LTraceLog.LTraceInfoRecord(lSourceIndex == lTargetIndex
            ? $"Group {lTargetIndex + 1}: reordered '{Path.GetFileName(lPath)}'"
            : $"Group {lTargetIndex + 1}: moved in '{Path.GetFileName(lPath)}' from group {lSourceIndex + 1}");
        LGroupChange?.Invoke();
        return true;
    }

    public bool LGroupPathsInsert(int lTargetIndex, IReadOnlyList<string> lPaths, int lInsertAt)
    {
        if (!LGroupIndexCheck(lTargetIndex) || lPaths.Count == 0)
        {
            return false;
        }

        List<string> lTargetPaths = lGroupRecords[lTargetIndex].LGroupRecordPaths;
        foreach (string lPath in lPaths)
        {
            LGroupPathInsert(lTargetPaths, lPath, lInsertAt);
            lInsertAt++;
        }

        LTraceLog.LTraceInfoRecord($"Group {lTargetIndex + 1}: added {lPaths.Count} file(s)");
        LGroupChange?.Invoke();
        return true;
    }

    public bool LGroupAdd(IReadOnlyList<string> lPaths, string lName, int? lSourceIndex = null)
    {
        var lNewPaths = new List<string>();
        if (lSourceIndex is { } lSource && LGroupIndexCheck(lSource) && lPaths.Count == 1)
        {
            if (lGroupRecords[lSource].LGroupRecordPaths.RemoveAll(lExisting =>
                    string.Equals(lExisting, lPaths[0], StringComparison.OrdinalIgnoreCase)) > 0)
            {
                lNewPaths.Add(lPaths[0]);
            }
        }
        else
        {
            lNewPaths.AddRange(lPaths);
        }

        if (lNewPaths.Count == 0)
        {
            return false;
        }

        var lRecord = new LGroupRecord { LGroupRecordName = lName };
        foreach (string lPath in lNewPaths)
        {
            LGroupPathInsert(lRecord.LGroupRecordPaths, lPath, lRecord.LGroupRecordPaths.Count);
        }

        lGroupRecords.Add(lRecord);
        LTraceLog.LTraceInfoRecord($"Group {lGroupRecords.Count}: created with {lNewPaths.Count} file(s)");
        LGroupChange?.Invoke();
        return true;
    }

    public void LGroupItemRemove(int lIndex, string lPath)
    {
        if (!LGroupIndexCheck(lIndex)
            || lGroupRecords[lIndex].LGroupRecordPaths.RemoveAll(lExisting =>
                string.Equals(lExisting, lPath, StringComparison.OrdinalIgnoreCase)) == 0)
        {
            return;
        }

        LGroupDragSet(null, null);
        LGroupChange?.Invoke();
    }

    public void LGroupRemove(int lIndex)
    {
        if (!LGroupIndexCheck(lIndex))
        {
            return;
        }

        lGroupRecords.RemoveAt(lIndex);
        LGroupChange?.Invoke();
    }

    public void LGroupNameSet(int lIndex, string lName)
    {
        string lTrimmed = lName.Trim();
        if (LGroupIndexCheck(lIndex) && lTrimmed.Length > 0)
        {
            lGroupRecords[lIndex].LGroupRecordName = lTrimmed;
        }

        LGroupChange?.Invoke();
    }

    private bool LGroupIndexCheck(int lIndex) => lIndex >= 0 && lIndex < lGroupRecords.Count;

    private static void LGroupPathInsert(List<string> lPaths, string lPath, int lInsertAt)
    {
        if (lPaths.Any(lExisting => string.Equals(lExisting, lPath, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        lPaths.Insert(Math.Clamp(lInsertAt, 0, lPaths.Count), lPath);
    }
}
