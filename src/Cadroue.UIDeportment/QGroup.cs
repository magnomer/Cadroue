using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QGroupEntry
{
    public string QGroupRecordName { get; set; } = string.Empty;

    public List<string> QGroupRecordPaths { get; } = [];
}

public sealed class QGroup
{
    private readonly CUsher qGroupUsher;
    private readonly LGroupSelection qGroupOwner;
    private readonly List<QGroupEntry> qGroupRecords = [];
    private Func<IReadOnlyList<string>> qGroupSource = () => [];
    private bool qGroupMinimized;
    private int? qGroupSourceIndex;
    private string? qGroupDragPath;

    public event Action? QGroupChanged;
    public event Action? QGroupFaceChanged;
    public event Action<bool>? QGroupMinimizeChanged;

    public QGroup(CUsher lUsher, LGroupSelection lGroupSelection)
    {
        qGroupUsher = lUsher;
        qGroupOwner = lGroupSelection;
        QGroupDrag = new QGroupDrag(this);
        QGroupFace = new QGroupFace(this, lUsher);
        QGroupName = new QGroupName(this);
        QGroupAttach();
    }

    public LGroupSelection QGroupSelection => qGroupOwner;

    public QGroupDrag QGroupDrag { get; }

    public QGroupFace QGroupFace { get; }

    public QGroupName QGroupName { get; }

    public IReadOnlyList<QGroupEntry> QGroupRecords => qGroupRecords;

    public bool QGroupAuto => qGroupOwner.LGroupAuto;

    public bool QGroupEmpty => qGroupRecords.Count == 0;

    public bool QGroupMinimized => qGroupMinimized;

    public int? QGroupSourceIndex => qGroupSourceIndex;

    public string? QGroupDragPath => qGroupDragPath;

    public void QGroupMinimizedSet(bool lMinimized)
    {
        if (qGroupMinimized == lMinimized)
        {
            return;
        }

        qGroupMinimized = lMinimized;
        QGroupMinimizeChanged?.Invoke(lMinimized);
    }

    public void QGroupDragSet(int? lSourceIndex, string? lDragPath)
    {
        qGroupSourceIndex = lSourceIndex;
        qGroupDragPath = lDragPath;
    }

    public void QGroupSort()
    {
        if (qGroupRecords.Count == 0)
        {
            return;
        }

        foreach (QGroupEntry lRecord in qGroupRecords)
        {
            IReadOnlyList<string> lSorted = LSeries.LSeriesPathsSort(lRecord.QGroupRecordPaths);
            lRecord.QGroupRecordPaths.Clear();
            lRecord.QGroupRecordPaths.AddRange(lSorted);
        }

        QGroupRecordsRaise();
    }

    public void QGroupPathsRemove(IReadOnlyList<string> lPaths)
    {
        (int lRemovedCount, int lEmptied) = QGroupEntriesRemove(lPaths);
        if (lRemovedCount == 0)
        {
            return;
        }

        QGroupRemovalRecord(lRemovedCount, lEmptied);
        QGroupRecordsRaise();
    }

    public void QGroupAutoApply(IReadOnlyList<string> lFiles, bool? lStrict = null)
    {
        IReadOnlyList<LSeriesGroup> lGroups = qGroupOwner.LGroupResolve(lFiles, lStrict);
        QGroupRecordsSet(lGroups);
        QGroupRecordsRaise();
    }

    public bool QGroupItemMove(int lSourceIndex, string lPath, int lTargetIndex, int lInsertAt)
    {
        if (!QGroupIndexCheck(lSourceIndex) || !QGroupIndexCheck(lTargetIndex))
        {
            return false;
        }

        int lRemovedIndex = QGroupPathRemove(lSourceIndex, lPath);
        int lTargetAt = QGroupIndexAdjust(lSourceIndex, lTargetIndex, lRemovedIndex, lInsertAt);
        bool lInserted = QGroupPathInsert(qGroupRecords[lTargetIndex].QGroupRecordPaths, lPath, lTargetAt);
        QGroupMoveRecord(lSourceIndex, lPath, lTargetIndex, lInserted);
        QGroupRecordsRaise();
        return true;
    }

    public bool QGroupPathsInsert(int lTargetIndex, IReadOnlyList<string> lPaths, int lInsertAt)
    {
        if (!QGroupIndexCheck(lTargetIndex) || lPaths.Count == 0)
        {
            return false;
        }

        int lInsertedCount = QGroupTargetInsert(lTargetIndex, lPaths, lInsertAt);
        if (lInsertedCount == 0)
        {
            return true;
        }

        QGroupInsertionRecord(lTargetIndex, lInsertedCount);
        QGroupRecordsRaise();
        return true;
    }

    public bool QGroupAdd(IReadOnlyList<string> lPaths, string lName, int? lSourceIndex = null)
    {
        List<string> lNewPaths = lSourceIndex is { } lSource && QGroupIndexCheck(lSource) && lPaths.Count == 1
            ? QGroupSourceRemove(lSource, lPaths[0])
            : [.. lPaths];
        if (lNewPaths.Count == 0)
        {
            return false;
        }

        QGroupEntry lRecord = QGroupEntryAdd(lNewPaths, lName);
        QGroupCreationRecord(lRecord);
        QGroupRecordsRaise();
        return true;
    }

    private (int, int) QGroupEntriesRemove(IReadOnlyList<string> lPaths)
    {
        var lTargetSet = new HashSet<string>(lPaths, StringComparer.OrdinalIgnoreCase);
        int lRemovedCount = 0;
        foreach (QGroupEntry lRecord in qGroupRecords)
        {
            lRemovedCount += lRecord.QGroupRecordPaths.RemoveAll(lTargetSet.Contains);
        }

        if (lRemovedCount == 0)
        {
            return (0, 0);
        }

        int lEmptied = qGroupRecords.RemoveAll(lRecord => lRecord.QGroupRecordPaths.Count == 0);
        return (lRemovedCount, lEmptied);
    }

    private static void QGroupRemovalRecord(int lRemovedCount, int lEmptied) =>
        LTraceLog.LTraceInfoRecord(
            $"Group: removed {lRemovedCount} unloaded file(s) from groups, {lEmptied} group(s) emptied");

    private void QGroupRecordsSet(IReadOnlyList<LSeriesGroup> lGroups)
    {
        qGroupRecords.Clear();
        foreach (LSeriesGroup lSeries in lGroups)
        {
            var lRecord = new QGroupEntry { QGroupRecordName = lSeries.LSeriesName };
            lRecord.QGroupRecordPaths.AddRange(lSeries.LSeriesPaths);
            qGroupRecords.Add(lRecord);
        }
    }

    private int QGroupPathRemove(int lSourceIndex, string lPath)
    {
        List<string> lSourcePaths = qGroupRecords[lSourceIndex].QGroupRecordPaths;
        int lRemovedIndex = lSourcePaths.FindIndex(lExisting =>
            string.Equals(lExisting, lPath, StringComparison.OrdinalIgnoreCase));
        if (lRemovedIndex >= 0)
        {
            lSourcePaths.RemoveAt(lRemovedIndex);
        }

        return lRemovedIndex;
    }

    private static int QGroupIndexAdjust(int lSourceIndex, int lTargetIndex, int lRemovedIndex, int lInsertAt) =>
        lRemovedIndex >= 0 && lSourceIndex == lTargetIndex && lRemovedIndex < lInsertAt ? lInsertAt - 1 : lInsertAt;

    private void QGroupMoveRecord(int lSourceIndex, string lPath, int lTargetIndex, bool lInserted)
    {
        string lGroupName = qGroupUsher.CUsherNameRead(lPath);
        LTraceLog.LTraceInfoRecord(lSourceIndex == lTargetIndex
            ? $"Group {lTargetIndex + 1}: reordered '{lGroupName}'"
            : lInserted
                ? $"Group {lTargetIndex + 1}: moved in '{lGroupName}' from group {lSourceIndex + 1}"
                : $"Group {lTargetIndex + 1}: already holds '{lGroupName}', removed from group {lSourceIndex + 1}");
    }

    private int QGroupTargetInsert(int lTargetIndex, IReadOnlyList<string> lPaths, int lInsertAt)
    {
        List<string> lTargetPaths = qGroupRecords[lTargetIndex].QGroupRecordPaths;
        int lInsertedCount = 0;
        foreach (string lPath in lPaths)
        {
            if (QGroupPathInsert(lTargetPaths, lPath, lInsertAt + lInsertedCount))
            {
                lInsertedCount++;
            }
        }

        return lInsertedCount;
    }

    private static void QGroupInsertionRecord(int lTargetIndex, int lInsertedCount) =>
        LTraceLog.LTraceInfoRecord($"Group {lTargetIndex + 1}: added {lInsertedCount} file(s)");

    private List<string> QGroupSourceRemove(int lSourceIndex, string lPath) =>
        qGroupRecords[lSourceIndex].QGroupRecordPaths.RemoveAll(lExisting =>
            string.Equals(lExisting, lPath, StringComparison.OrdinalIgnoreCase)) > 0
            ? [lPath]
            : [];

    private QGroupEntry QGroupEntryAdd(List<string> lNewPaths, string lName)
    {
        var lRecord = new QGroupEntry { QGroupRecordName = lName };
        foreach (string lPath in lNewPaths)
        {
            QGroupPathInsert(lRecord.QGroupRecordPaths, lPath, lRecord.QGroupRecordPaths.Count);
        }

        qGroupRecords.Add(lRecord);
        return lRecord;
    }

    private void QGroupCreationRecord(QGroupEntry lRecord) =>
        LTraceLog.LTraceInfoRecord(
            $"Group {qGroupRecords.Count}: created with {lRecord.QGroupRecordPaths.Count} file(s)");

    public void QGroupItemRemove(int lIndex, string lPath)
    {
        if (!QGroupIndexCheck(lIndex)
            || qGroupRecords[lIndex].QGroupRecordPaths.RemoveAll(lExisting =>
                string.Equals(lExisting, lPath, StringComparison.OrdinalIgnoreCase)) == 0)
        {
            return;
        }

        QGroupDragSet(null, null);
        QGroupRecordsRaise();
    }

    public void QGroupRemove(int lIndex)
    {
        if (!QGroupIndexCheck(lIndex))
        {
            return;
        }

        qGroupRecords.RemoveAt(lIndex);
        QGroupRecordsRaise();
    }

    public void QGroupSourceAttach(Func<IReadOnlyList<string>> lSource) => qGroupSource = lSource;

    public void QGroupAutoUpdate()
    {
        if (qGroupOwner.LGroupAuto)
        {
            QGroupSourceApply(null);
        }
    }

    public void QGroupStrictApply() => QGroupSourceApply(true);

    public void QGroupLooseApply() => QGroupSourceApply(false);

    private bool QGroupIndexCheck(int lIndex) => lIndex >= 0 && lIndex < qGroupRecords.Count;

    private void QGroupSourceApply(bool? lStrict) => QGroupAutoApply(qGroupSource(), lStrict);

    private void QGroupAttach()
    {
        QGroupName.QGroupNameChanged += QGroupNameHandle;
        qGroupOwner.LGroupSelectionChanged += QGroupSelectionHandle;
    }

    private void QGroupNameHandle() => QGroupChanged?.Invoke();

    private void QGroupSelectionHandle()
    {
        QGroupFaceChanged?.Invoke();
        QGroupAutoUpdate();
    }

    private void QGroupRecordsRaise()
    {
        QGroupName.QGroupEditClear();
        QGroupChanged?.Invoke();
    }

    private static bool QGroupPathInsert(List<string> lPaths, string lPath, int lInsertAt)
    {
        if (QGroupPositionResolve(lPaths, lPath, lInsertAt) is not { } lPosition)
        {
            return false;
        }

        lPaths.Insert(lPosition, lPath);
        return true;
    }

    private static int? QGroupPositionResolve(List<string> lPaths, string lPath, int lInsertAt) =>
        lPaths.Any(lExisting => string.Equals(lExisting, lPath, StringComparison.OrdinalIgnoreCase))
            ? null
            : Math.Clamp(lInsertAt, 0, lPaths.Count);
}
