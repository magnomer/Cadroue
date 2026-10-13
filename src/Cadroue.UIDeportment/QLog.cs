using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QLog
{
    private const int QLogRowMaximum = 5000;
    private readonly CUsher qLogUsher;
    private readonly List<(long QLogSequence, LTraceEntry QLogEntry)> qLogPending = [];
    private readonly object qLogPendingLock = new();
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
        LTrace.LTraceCommittedAppended += QLogAppendHandle;
        QLogFilesUpdate();
    }

    public void QLogDetach() => LTrace.LTraceCommittedAppended -= QLogAppendHandle;

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
        qLogCategories.Clear();
        foreach (string lToken in lTokens)
        {
            qLogCategories.Add(LTraceEntry.LTraceKindFind(lToken));
        }

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
        string lCurrentPath = LTraceWriter.LTracePathRead();
        LTraceReadResult<List<string>> lFilesResult = LTraceWriter.LTraceFilesRead();
        List<string> lFiles = lFilesResult.LTraceReadValue;
        if (!lFilesResult.LTraceReadSuccess)
        {
            QLogErrorRaise("Log.Error.List", lFilesResult.LTraceReadError);
        }

        if (!lFiles.Contains(lCurrentPath, StringComparer.OrdinalIgnoreCase))
        {
            lFiles.Insert(0, lCurrentPath);
        }

        string lSelectedPath = !lFollowCurrent && qLogFiles.Count > 0
            ? qLogFiles[Math.Clamp(qLogFileIndex, 0, qLogFiles.Count - 1)].QLogFilePath
            : lCurrentPath;
        if (!lFiles.Contains(lSelectedPath, StringComparer.OrdinalIgnoreCase))
        {
            lSelectedPath = lCurrentPath;
        }

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

            qLogFiles.Add(new QLogFile(
                lFile,
                lCurrent
                    ? LLocalization.LLocalizationTextRead("Log.File.Current")
                    : qLogUsher.CUsherStemRead(lFile)));
        }

        QLogFilesChanged?.Invoke();
    }

    public void QLogFileSelect(int lIndex)
    {
        if (lIndex < 0 || lIndex >= qLogFiles.Count)
        {
            return;
        }

        qLogFileIndex = lIndex;
        QLogFileSet(qLogFiles[lIndex].QLogFilePath, LTraceWriter.LTracePathRead());
        LTraceReadResult<string> lRead;
        long lCommitted = qLogSnapshot;
        if (qLogFileLive)
        {
            lRead = LTraceWriter.LTraceWriterRead(out lCommitted);
        }
        else
        {
            lRead = LTraceWriter.LTraceFileRead(qLogFilePath);
        }

        if (!lRead.LTraceReadSuccess)
        {
            QLogErrorRaise("Log.Error.Read", lRead.LTraceReadError);
            return;
        }

        if (qLogFileLive)
        {
            QLogSnapshotSet(lCommitted);
            lock (qLogPendingLock)
            {
                qLogPending.RemoveAll(lItem => QLogSnapshotCheck(lItem.QLogSequence));
            }
        }

        qLogRowsAll.Clear();
        qLogRowsAll.AddRange(LTraceEntry.LTraceEntryParse(lRead.LTraceReadValue).Select(lEntry => new QLogRow(lEntry)));
        QLogExcessRemove();
        QLogRowsApply();
    }

    public void QLogTick()
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

        List<(long QLogSequence, LTraceEntry QLogEntry)> lBatch;
        lock (qLogPendingLock)
        {
            if (qLogPending.Count == 0)
            {
                return;
            }

            lBatch = new List<(long, LTraceEntry)>(qLogPending);
            qLogPending.Clear();
        }

        if (!QLogLiveSet(LTraceWriter.LTracePathRead()))
        {
            return;
        }

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
        QLogErrorShown?.Invoke(LLocalization.LLocalizationFormat(lMessageKey, lDetail));

    private void QLogAppendHandle(long lSequence, LTraceEntry lEntry)
    {
        lock (qLogPendingLock)
        {
            qLogPending.Add((lSequence, lEntry));
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
