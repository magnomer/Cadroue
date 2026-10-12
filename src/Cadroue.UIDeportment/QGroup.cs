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
    private const string QGroupReturnKey = "Return";
    private const string QGroupEscapeKey = "Escape";

    private readonly CUsher qGroupUsher;
    private readonly LGroupSelection qGroupOwner;
    private readonly List<QGroupEntry> qGroupRecords = [];
    private Func<IReadOnlyList<string>> qGroupSource = () => [];
    private bool qGroupMinimized;
    private int? qGroupSourceIndex;
    private string? qGroupDragPath;
    private int? qGroupEditingIndex;

    public event Action? QGroupChanged;
    public event Action? QGroupFaceChanged;
    public event Action<bool>? QGroupMinimizeChanged;

    public QGroup(CUsher lUsher, LGroupSelection lGroupSelection)
    {
        qGroupUsher = lUsher;
        qGroupOwner = lGroupSelection;
        QGroupDrag = new QGroupDrag(this);
        QGroupFace = new QGroupFace(this, lUsher);
        qGroupOwner.LGroupSelectionChanged += QGroupSelectionHandle;
    }

    public LGroupSelection QGroupSelection => qGroupOwner;

    public QGroupDrag QGroupDrag { get; }

    public QGroupFace QGroupFace { get; }

    public IReadOnlyList<QGroupEntry> QGroupRecords => qGroupRecords;

    public bool QGroupAuto => qGroupOwner.LGroupAuto;

    public int? QGroupEditingIndex => qGroupEditingIndex;

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
        var lTargetSet = new HashSet<string>(lPaths, StringComparer.OrdinalIgnoreCase);
        int lRemovedCount = 0;
        foreach (QGroupEntry lRecord in qGroupRecords)
        {
            lRemovedCount += lRecord.QGroupRecordPaths.RemoveAll(lTargetSet.Contains);
        }

        if (lRemovedCount == 0)
        {
            return;
        }

        int lEmptied = qGroupRecords.RemoveAll(lRecord => lRecord.QGroupRecordPaths.Count == 0);
        LTraceLog.LTraceInfoRecord(
            $"Group: removed {lRemovedCount} unloaded file(s) from groups, {lEmptied} group(s) emptied");
        QGroupRecordsRaise();
    }

    public void QGroupAutoApply(IReadOnlyList<string> lFiles, bool? lStrict = null)
    {
        IReadOnlyList<LSeriesGroup> lGroups = qGroupOwner.LGroupResolve(lFiles, lStrict);
        qGroupRecords.Clear();
        foreach (LSeriesGroup lSeries in lGroups)
        {
            var lRecord = new QGroupEntry { QGroupRecordName = lSeries.LSeriesName };
            lRecord.QGroupRecordPaths.AddRange(lSeries.LSeriesPaths);
            qGroupRecords.Add(lRecord);
        }

        QGroupRecordsRaise();
    }

    public bool QGroupItemMove(int lSourceIndex, string lPath, int lTargetIndex, int lInsertAt)
    {
        if (!QGroupIndexCheck(lSourceIndex) || !QGroupIndexCheck(lTargetIndex))
        {
            return false;
        }

        List<string> lSourcePaths = qGroupRecords[lSourceIndex].QGroupRecordPaths;
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

        bool lInserted = QGroupPathInsert(qGroupRecords[lTargetIndex].QGroupRecordPaths, lPath, lInsertAt);
        string lGroupName = qGroupUsher.CUsherNameRead(lPath);
        LTraceLog.LTraceInfoRecord(lSourceIndex == lTargetIndex
            ? $"Group {lTargetIndex + 1}: reordered '{lGroupName}'"
            : lInserted
                ? $"Group {lTargetIndex + 1}: moved in '{lGroupName}' from group {lSourceIndex + 1}"
                : $"Group {lTargetIndex + 1}: already holds '{lGroupName}', removed from group {lSourceIndex + 1}");
        QGroupRecordsRaise();
        return true;
    }

    public bool QGroupPathsInsert(int lTargetIndex, IReadOnlyList<string> lPaths, int lInsertAt)
    {
        if (!QGroupIndexCheck(lTargetIndex) || lPaths.Count == 0)
        {
            return false;
        }

        List<string> lTargetPaths = qGroupRecords[lTargetIndex].QGroupRecordPaths;
        int lInsertedCount = 0;
        foreach (string lPath in lPaths)
        {
            if (QGroupPathInsert(lTargetPaths, lPath, lInsertAt + lInsertedCount))
            {
                lInsertedCount++;
            }
        }

        if (lInsertedCount == 0)
        {
            return true;
        }

        LTraceLog.LTraceInfoRecord($"Group {lTargetIndex + 1}: added {lInsertedCount} file(s)");
        QGroupRecordsRaise();
        return true;
    }

    public bool QGroupAdd(IReadOnlyList<string> lPaths, string lName, int? lSourceIndex = null)
    {
        var lNewPaths = new List<string>();
        if (lSourceIndex is { } lSource && QGroupIndexCheck(lSource) && lPaths.Count == 1)
        {
            if (qGroupRecords[lSource].QGroupRecordPaths.RemoveAll(lExisting =>
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

        var lRecord = new QGroupEntry { QGroupRecordName = lName };
        foreach (string lPath in lNewPaths)
        {
            QGroupPathInsert(lRecord.QGroupRecordPaths, lPath, lRecord.QGroupRecordPaths.Count);
        }

        qGroupRecords.Add(lRecord);
        LTraceLog.LTraceInfoRecord(
            $"Group {qGroupRecords.Count}: created with {lRecord.QGroupRecordPaths.Count} file(s)");
        QGroupRecordsRaise();
        return true;
    }

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

    public void QGroupEditStart(int lIndex)
    {
        if (!QGroupIndexCheck(lIndex) || qGroupEditingIndex == lIndex)
        {
            return;
        }

        qGroupEditingIndex = lIndex;
        QGroupChanged?.Invoke();
    }

    public void QGroupEditCancel()
    {
        if (qGroupEditingIndex is null)
        {
            return;
        }

        qGroupEditingIndex = null;
        QGroupChanged?.Invoke();
    }

    public bool QGroupNameCommit(string lName)
    {
        if (qGroupEditingIndex is not { } lIndex)
        {
            return false;
        }

        qGroupEditingIndex = null;
        bool lApplied = QGroupNameApply(lIndex, lName);
        QGroupChanged?.Invoke();
        return lApplied;
    }

    public bool QGroupNameSet(int lIndex, string lName)
    {
        if (!QGroupNameApply(lIndex, lName))
        {
            return false;
        }

        QGroupChanged?.Invoke();
        return true;
    }

    private bool QGroupNameApply(int lIndex, string lName)
    {
        string lTrimmed = lName.Trim();
        if (!QGroupIndexCheck(lIndex)
            || lTrimmed.Length == 0
            || string.Equals(lTrimmed, qGroupRecords[lIndex].QGroupRecordName, StringComparison.Ordinal))
        {
            return false;
        }

        qGroupRecords[lIndex].QGroupRecordName = lTrimmed;
        return true;
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

    public bool QGroupLabelHandle(int lIndex, int lClickCount)
    {
        if (lClickCount != 2)
        {
            return false;
        }

        QGroupEditStart(lIndex);
        return true;
    }

    public bool QGroupKeyRun(string lKey, string lText)
    {
        if (string.Equals(lKey, QGroupReturnKey, StringComparison.Ordinal))
        {
            QGroupNameCommit(lText);
            return true;
        }

        if (string.Equals(lKey, QGroupEscapeKey, StringComparison.Ordinal))
        {
            QGroupEditCancel();
            return true;
        }

        return false;
    }

    private void QGroupSourceApply(bool? lStrict) => QGroupAutoApply(qGroupSource(), lStrict);

    private void QGroupSelectionHandle()
    {
        QGroupFaceChanged?.Invoke();
        QGroupAutoUpdate();
    }

    private void QGroupRecordsRaise()
    {
        qGroupEditingIndex = null;
        QGroupChanged?.Invoke();
    }

    private bool QGroupIndexCheck(int lIndex) => lIndex >= 0 && lIndex < qGroupRecords.Count;

    private static bool QGroupPathInsert(List<string> lPaths, string lPath, int lInsertAt)
    {
        if (lPaths.Any(lExisting => string.Equals(lExisting, lPath, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        lPaths.Insert(Math.Clamp(lInsertAt, 0, lPaths.Count), lPath);
        return true;
    }
}
