using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QLog
{
    private readonly CUsher qLogUsher;
    private readonly QLogPending qLogPending = new();
    private string qLogFilePath = string.Empty;
    private bool qLogFileLive = true;
    private bool qLogFollowTail = true;
    private long qLogSnapshot;

    public QLog(CUsher lUsher)
    {
        qLogUsher = lUsher;
        QLogFolder = new QLogFolder(lUsher, QLogErrorRaise);
    }

    public event Action? QLogScrolled;
    public event Action<string>? QLogErrorShown;
    public event Action<string>? QLogCopied;

    public string QLogFilePath => qLogFilePath;

    public bool QLogFileLive => qLogFileLive;

    public bool QLogFollowTail => qLogFollowTail;

    public long QLogSnapshot => qLogSnapshot;

    public QLogFolder QLogFolder { get; }

    public QLogFeed QLogFeed { get; } = new();

    public IReadOnlyList<LLocalizationChoice> QLogCategoriesRead() =>
        Enum.GetValues<LTraceKind>()
            .Select(lKind => new LLocalizationChoice(LTraceText.LTraceKindRead(lKind), QLogRow.QLogKeyRead(lKind)))
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
        QLogFeed.QLogFilterSet(QLogFeed.QLogKindsParse(lTokens));
        QLogFeed.QLogRowsApply();
        QLogScrollRaise();
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
        (string lCurrentPath, List<string> lFiles) = QLogFolder.QLogFilesRead();
        QLogFolder.QLogCurrentInsert(lFiles, lCurrentPath);
        string lSelectedPath = QLogFolder.QLogSelectedResolve(lFiles, lCurrentPath, lFollowCurrent);
        QLogFolder.QLogFilesApply(lFiles, lCurrentPath, lSelectedPath);
    }

    public void QLogFileSelect(int lIndex)
    {
        if (!QLogFolder.QLogIndexCheck(lIndex))
        {
            return;
        }

        QLogFolder.QLogIndexSet(lIndex);
        QLogFileSet(QLogFolder.QLogFiles[lIndex].QLogFilePath, LTraceFolder.LTracePathRead());
        LTraceReadResult<string> lRead = QLogTextRead(out long lCommitted);
        if (!lRead.LTraceReadSuccess)
        {
            QLogErrorRaise("Log.Error.Read", lRead.LTraceReadError);
            return;
        }

        QLogSnapshotApply(lCommitted);
        QLogFeed.QLogRowsReset();
        QLogFeed.QLogRowsAdd(QLogFeed.QLogRowsParse(lRead.LTraceReadValue));
        QLogFeed.QLogExcessRemove();
        QLogFeed.QLogRowsApply();
        QLogScrollRaise();
    }

    public void QLogTick()
    {
        QLogLiveSync();
        if (qLogPending.QLogPendingRemove() is not { } lBatch)
        {
            return;
        }

        if (!QLogLiveSet(LTraceFolder.LTracePathRead()))
        {
            return;
        }

        QLogFeed.QLogBatchAppend(lBatch, QLogSnapshotCheck);
        QLogScrollRaise();
    }

    public void QLogTextCopy()
    {
        LTraceReadResult<string> lRead = LTraceReader.LTraceFileRead(qLogFilePath);
        if (!lRead.LTraceReadSuccess)
        {
            QLogErrorRaise("Log.Error.Read", lRead.LTraceReadError);
            return;
        }

        QLogCopied?.Invoke(lRead.LTraceReadValue);
    }

    public void QLogFolderOpen()
    {
        if (qLogUsher.CUsherPathOpen(qLogFilePath, LTraceFolder.LTraceFolderRead()) is { } lError)
        {
            QLogErrorRaise("Log.Error.Open", lError);
        }
    }

    public void QLogErrorRaise(string lMessageKey, string lDetail) =>
        QLogErrorShown?.Invoke(QLocalizationCatalog.QLocalizationFormat(lMessageKey, lDetail));

    private LTraceReadResult<string> QLogTextRead(out long lCommitted)
    {
        lCommitted = qLogSnapshot;
        if (qLogFileLive)
        {
            return LTraceReader.LTraceTextRead(out lCommitted);
        }

        return LTraceReader.LTraceFileRead(qLogFilePath);
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

    private void QLogLiveSync()
    {
        string lCurrentPath = LTraceFolder.LTracePathRead();
        if (qLogFileLive && !QLogFileCheck(lCurrentPath))
        {
            QLogFilesUpdate(true);
            if (!QLogFileCheck(lCurrentPath))
            {
                QLogFileSelect(QLogFolder.QLogFileIndex);
            }
        }
    }

    private void QLogScrollRaise()
    {
        if (qLogFollowTail && QLogFeed.QLogRowsShown.Count > 0)
        {
            QLogScrolled?.Invoke();
        }
    }
}
