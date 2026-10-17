using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QLog
{
    private const int QLogRowMaximum = 5000;
    private readonly CUsher qLogUsher;
    private readonly QLogPending qLogPending = new();
    private readonly List<QLogRow> qLogRowsAll = [];
    private readonly List<QLogRow> qLogRowsShown = [];
    private readonly List<QLogFile> qLogFiles = [];
    private readonly HashSet<LTraceKind> qLogCategories = [];
    private string qLogFilePath = string.Empty;
    private bool qLogFileLive = true;
    private bool qLogFollowTail = true;
    private long qLogSnapshot;
    private int qLogFileIndex;

    public QLog(CUsher lUsher)
    {
        qLogUsher = lUsher;
    }

    public event Action? QLogFilesChanged;
    public event Action? QLogRowsRebuilt;
    public event Action<QLogRow>? QLogRowAppended;
    public event Action<int>? QLogRowsRemoved;
    public event Action? QLogScrolled;
    public event Action<string>? QLogErrorShown;
    public event Action<string>? QLogCopied;

    public string QLogFilePath => qLogFilePath;

    public bool QLogFileLive => qLogFileLive;

    public bool QLogFollowTail => qLogFollowTail;

    public long QLogSnapshot => qLogSnapshot;

    public IReadOnlyList<QLogFile> QLogFiles => qLogFiles;

    public int QLogFileIndex => qLogFileIndex;

    public IReadOnlyList<QLogRow> QLogRowsShown => qLogRowsShown;

    public IReadOnlyList<LLocalizationChoice> QLogCategoriesRead() =>
        Enum.GetValues<LTraceKind>()
            .Select(lKind => new LLocalizationChoice(LTraceEntry.LTraceKindRead(lKind), QLogRow.QLogKeyRead(lKind)))
            .ToArray();

    public void QLogAttach()
    {
        qLogPending.QLogPendingAttach();
        QLogFilesUpdate();
    }

    public void QLogDetach() => qLogPending.QLogPendingDetach();

    public bool QLogFileCheck(string lPath) =>
        string.Equals(qLogFilePath, lPath, StringComparison.OrdinalIgnoreCase);

    public void QLogFileSet(string lPath, string lLivePath)
    {
        qLogFilePath = lPath;
        QLogLiveSet(lLivePath);
    }

    public bool QLogLiveSet(string lLivePath)
    {
        qLogFileLive = QLogFileCheck(lLivePath);
        return qLogFileLive;
    }

    public void QLogFollowSet(bool lFollowTail) => qLogFollowTail = lFollowTail;

    public void QLogScrollHandle(
        bool lFeedViewer,
        double lExtentChange,
        double? lScrollable,
        double? lOffset,
        double lRow)
    {
        if (!lFeedViewer || lExtentChange != 0 || lScrollable is null || lOffset is null)
        {
            return;
        }

        QLogFollowSet(lScrollable.Value - lOffset.Value <= lRow);
    }

    public void QLogSnapshotSet(long lSequence) => qLogSnapshot = lSequence;

    public bool QLogSnapshotCheck(long lSequence) => lSequence <= qLogSnapshot;

    public bool QLogExpandToggle(QLogRow? lRow)
    {
        if (lRow is null)
        {
            return false;
        }

        lRow.QLogRowExpanded = !lRow.QLogRowExpanded;
        return true;
    }

    public void QLogCategorySet(IReadOnlyList<string> lTokens)
    {
        QLogFilterSet(QLogKindsParse(lTokens));
        QLogRowsApply();
    }

    public void QLogVerboseSet(bool lVerbose)
    {
        if (LTrace.LTraceVerbose == lVerbose)
        {
            return;
        }

        LTrace.LTraceVerbose = lVerbose;
        LPreferenceState lNext = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        lNext.LPreferenceLogVerbose = lVerbose;
        LPreference.LPreferenceStateSet(lNext);
    }

    public void QLogFilesUpdate(bool lFollowCurrent = false)
    {
        (string lCurrentPath, List<string> lFiles) = QLogFilesRead();
        QLogCurrentInsert(lFiles, lCurrentPath);
        string lSelectedPath = QLogSelectedResolve(lFiles, lCurrentPath, lFollowCurrent);
        QLogFilesApply(lFiles, lCurrentPath, lSelectedPath);
    }

    public void QLogFileSelect(int lIndex)
    {
        if (!QLogIndexCheck(lIndex))
        {
            return;
        }

        QLogIndexSet(lIndex);
        QLogFileSet(QLogFiles[lIndex].QLogFilePath, LTraceWriter.LTracePathRead());
        LTraceReadResult<string> lRead = QLogTextRead(out long lCommitted);
        if (!lRead.LTraceReadSuccess)
        {
            QLogErrorRaise("Log.Error.Read", lRead.LTraceReadError);
            return;
        }

        QLogSnapshotApply(lCommitted);
        QLogRowsReset();
        QLogRowsAdd(QLogRowsParse(lRead.LTraceReadValue));
        QLogExcessRemove();
        QLogRowsApply();
    }

    public void QLogTick()
    {
        QLogLiveSync();
        if (qLogPending.QLogPendingRemove() is not { } lBatch)
        {
            return;
        }

        if (!QLogLiveSet(LTraceWriter.LTracePathRead()))
        {
            return;
        }

        QLogBatchAppend(lBatch);
        QLogScrollRaise();
    }

    public void QLogTextCopy()
    {
        LTraceReadResult<string> lRead = LTraceWriter.LTraceFileRead(qLogFilePath);
        if (!lRead.LTraceReadSuccess)
        {
            QLogErrorRaise("Log.Error.Read", lRead.LTraceReadError);
            return;
        }

        QLogCopied?.Invoke(lRead.LTraceReadValue);
    }

    public void QLogFolderOpen()
    {
        if (qLogUsher.CUsherPathOpen(qLogFilePath, LTraceWriter.LTraceFolderRead()) is { } lError)
        {
            QLogErrorRaise("Log.Error.Open", lError);
        }
    }

    public void QLogErrorRaise(string lMessageKey, string lDetail) =>
        QLogErrorShown?.Invoke(QLocalizationCatalog.QLocalizationFormat(lMessageKey, lDetail));

    private (string, List<string>) QLogFilesRead()
    {
        string lCurrentPath = LTraceWriter.LTracePathRead();
        LTraceReadResult<List<string>> lFilesResult = LTraceWriter.LTraceFilesRead();
        List<string> lFiles = lFilesResult.LTraceReadValue;
        if (!lFilesResult.LTraceReadSuccess)
        {
            QLogErrorRaise("Log.Error.List", lFilesResult.LTraceReadError);
        }

        return (lCurrentPath, lFiles);
    }

    private static void QLogCurrentInsert(List<string> lFiles, string lCurrentPath)
    {
        if (!lFiles.Contains(lCurrentPath, StringComparer.OrdinalIgnoreCase))
        {
            lFiles.Insert(0, lCurrentPath);
        }
    }

    private string QLogSelectedResolve(List<string> lFiles, string lCurrentPath, bool lFollowCurrent)
    {
        string lSelectedPath = !lFollowCurrent && qLogFiles.Count > 0
            ? qLogFiles[Math.Clamp(qLogFileIndex, 0, qLogFiles.Count - 1)].QLogFilePath
            : lCurrentPath;
        if (!lFiles.Contains(lSelectedPath, StringComparer.OrdinalIgnoreCase))
        {
            lSelectedPath = lCurrentPath;
        }

        return lSelectedPath;
    }

    private void QLogFilesApply(List<string> lFiles, string lCurrentPath, string lSelectedPath)
    {
        if (qLogFiles.Select(lFile => lFile.QLogFilePath).SequenceEqual(lFiles, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        qLogFiles.Clear();
        qLogFileIndex = 0;
        foreach (string lFile in lFiles)
        {
            bool lCurrent = string.Equals(lFile, lCurrentPath, StringComparison.OrdinalIgnoreCase);
            if (string.Equals(lFile, lSelectedPath, StringComparison.OrdinalIgnoreCase))
            {
                qLogFileIndex = qLogFiles.Count;
            }

            qLogFiles.Add(new QLogFile(lFile, QLogLabelRead(lFile, lCurrent)));
        }

        QLogFilesChanged?.Invoke();
    }

    private string QLogLabelRead(string lFile, bool lCurrent) =>
        lCurrent
            ? QLocalizationCatalog.QLocalizationTextRead("Log.File.Current")
            : qLogUsher.CUsherStemRead(lFile);

    private bool QLogIndexCheck(int lIndex) => lIndex >= 0 && lIndex < qLogFiles.Count;

    private void QLogIndexSet(int lIndex) => qLogFileIndex = lIndex;

    private LTraceReadResult<string> QLogTextRead(out long lCommitted)
    {
        lCommitted = qLogSnapshot;
        if (qLogFileLive)
        {
            return LTraceWriter.LTraceWriterRead(out lCommitted);
        }

        return LTraceWriter.LTraceFileRead(qLogFilePath);
    }

    private void QLogSnapshotApply(long lCommitted)
    {
        if (!qLogFileLive)
        {
            return;
        }

        QLogSnapshotSet(lCommitted);
        qLogPending.QLogPendingClear(QLogSnapshotCheck);
    }

    private void QLogRowsReset() => qLogRowsAll.Clear();

    private static IEnumerable<QLogRow> QLogRowsParse(string lText) =>
        LTraceEntry.LTraceEntryParse(lText).Select(lEntry => new QLogRow(lEntry));

    private void QLogRowsAdd(IEnumerable<QLogRow> lRows) => qLogRowsAll.AddRange(lRows);

    private void QLogLiveSync()
    {
        string lCurrentPath = LTraceWriter.LTracePathRead();
        if (qLogFileLive && !QLogFileCheck(lCurrentPath))
        {
            QLogFilesUpdate(true);
            if (!QLogFileCheck(lCurrentPath))
            {
                QLogFileSelect(qLogFileIndex);
            }
        }
    }

    private void QLogBatchAppend(List<(long QLogSequence, LTraceEntry QLogEntry)> lBatch)
    {
        foreach ((long lSequence, LTraceEntry lEntry) in lBatch)
        {
            if (QLogSnapshotCheck(lSequence))
            {
                continue;
            }

            var lRow = new QLogRow(lEntry);
            qLogRowsAll.Add(lRow);
            if (QLogCategoryCheck(lRow))
            {
                qLogRowsShown.Add(lRow);
                QLogRowAppended?.Invoke(lRow);
            }
        }

        if (qLogRowsAll.Count > QLogRowMaximum)
        {
            QLogExcessRemove();
        }
    }

    private static LTraceKind[] QLogKindsParse(IReadOnlyList<string> lTokens) =>
        lTokens.Select(LTraceEntry.LTraceKindFind).ToArray();

    private void QLogFilterSet(LTraceKind[] lKinds)
    {
        qLogCategories.Clear();
        foreach (LTraceKind lKind in lKinds)
        {
            qLogCategories.Add(lKind);
        }
    }

    private bool QLogCategoryCheck(QLogRow lRow) =>
        qLogCategories.Count == 0 || qLogCategories.Contains(lRow.QLogRowCategory);

    private void QLogRowsApply()
    {
        qLogRowsShown.Clear();
        qLogRowsShown.AddRange(qLogRowsAll.Where(QLogCategoryCheck));
        QLogRowsRebuilt?.Invoke();
        QLogScrollRaise();
    }

    private void QLogExcessRemove()
    {
        int lExcess = qLogRowsAll.Count - QLogRowMaximum;
        if (lExcess <= 0)
        {
            return;
        }

        var lRemoved = new HashSet<QLogRow>(qLogRowsAll.Take(lExcess));
        qLogRowsAll.RemoveRange(0, lExcess);
        int lShownExcess = qLogRowsShown.TakeWhile(lRemoved.Contains).Count();
        qLogRowsShown.RemoveRange(0, lShownExcess);
        QLogRowsRemoved?.Invoke(lShownExcess);
    }

    private void QLogScrollRaise()
    {
        if (qLogFollowTail && qLogRowsShown.Count > 0)
        {
            QLogScrolled?.Invoke();
        }
    }
}
