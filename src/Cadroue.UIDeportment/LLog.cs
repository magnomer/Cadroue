using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record LLogFile(string LLogFilePath, string LLogFileLabel);

public sealed class LLogRow
{
    private static readonly IReadOnlyDictionary<LTraceKind, string> LLogRowKeys = new Dictionary<LTraceKind, string>
    {
        [LTraceKind.LTraceLoading] = "Log.Category.Loading",
        [LTraceKind.LTraceWarning] = "Log.Category.Warning",
        [LTraceKind.LTraceError] = "Log.Category.Error",
        [LTraceKind.LTraceInteraction] = "Log.Category.Interaction",
        [LTraceKind.LTraceUi] = "Log.Category.Interface",
        [LTraceKind.LTraceWork] = "Log.Category.Work",
        [LTraceKind.LTraceFfmpeg] = "Log.Category.Ffmpeg",
    };

    private const string LLogInfoKey = "Log.Category.Info";

    public LLogRow(LTraceEntry lEntry)
    {
        LLogRowCategory = lEntry.LTraceEntryKind;
        LLogRowKey = LLogKeyRead(lEntry.LTraceEntryKind);
        LLogRowTime = lEntry.LTraceEntryTime;
        LLogRowKind = LLocalization.LLocalizationTextRead(LLogRowKey);
        LLogRowSummary = lEntry.LTraceEntrySummary;
        LLogRowDetail = lEntry.LTraceEntryDetail ?? string.Empty;
        LLogRowDetailed = lEntry.LTraceEntryDetailed;
        LLogRowSpan = lEntry.LTraceEntrySpan is double lSpan ? LTraceEntry.LTraceSpanFormat(lSpan) : string.Empty;
    }

    public LTraceKind LLogRowCategory { get; }

    public string LLogRowKey { get; }

    public string LLogRowTime { get; }

    public string LLogRowKind { get; }

    public string LLogRowSummary { get; }

    public string LLogRowDetail { get; }

    public bool LLogRowDetailed { get; }

    public string LLogRowSpan { get; }

    public bool LLogRowExpanded { get; internal set; }

    public string LLogRowChip =>
        LLocalization.LLocalizationTextRead("Log.Button.Details") + (LLogRowExpanded ? "  ▴" : "  ▾");

    public static string LLogKeyRead(LTraceKind lKind) => LLogRowKeys.GetValueOrDefault(lKind, LLogInfoKey);
}

public sealed class LLog
{
    private const int LLogRowMaximum = 5000;
    private readonly CUsher lLogUsher;
    private readonly List<(long LLogSequence, LTraceEntry LLogEntry)> lLogPending = [];
    private readonly object lLogPendingLock = new();
    private readonly List<LLogRow> lLogRowsAll = [];
    private readonly List<LLogRow> lLogRowsShown = [];
    private readonly List<LLogFile> lLogFiles = [];
    private readonly HashSet<LTraceKind> lLogCategories = [];
    private string lLogFilePath = string.Empty;
    private bool lLogFileLive = true;
    private bool lLogFollowTail = true;
    private long lLogSnapshot;
    private int lLogFileIndex;

    public LLog(LStrip lStrip)
    {
        lLogUsher = lStrip.LStripUsher;
    }

    public event Action? LLogFilesChanged;
    public event Action? LLogRowsRebuilt;
    public event Action<LLogRow>? LLogRowAppended;
    public event Action<int>? LLogRowsRemoved;
    public event Action? LLogScrolled;
    public event Action<string>? LLogErrorShown;
    public event Action<string>? LLogCopied;

    public string LLogFilePath => lLogFilePath;

    public bool LLogFileLive => lLogFileLive;

    public bool LLogFollowTail => lLogFollowTail;

    public long LLogSnapshot => lLogSnapshot;

    public IReadOnlyList<LLogFile> LLogFiles => lLogFiles;

    public int LLogFileIndex => lLogFileIndex;

    public IReadOnlyList<LLogRow> LLogRowsShown => lLogRowsShown;

    public IReadOnlyList<LLocalizationChoice> LLogCategoriesRead() =>
        Enum.GetValues<LTraceKind>()
            .Select(lKind => new LLocalizationChoice(LTraceEntry.LTraceKindRead(lKind), LLogRow.LLogKeyRead(lKind)))
            .ToArray();

    public void LLogAttach()
    {
        LTrace.LTraceCommittedAppended += LLogAppendHandle;
        LLogFilesUpdate();
    }

    public void LLogDetach() => LTrace.LTraceCommittedAppended -= LLogAppendHandle;

    public bool LLogFileCheck(string lPath) =>
        string.Equals(lLogFilePath, lPath, StringComparison.OrdinalIgnoreCase);

    public void LLogFileSet(string lPath, string lLivePath)
    {
        lLogFilePath = lPath;
        LLogLiveSet(lLivePath);
    }

    public bool LLogLiveSet(string lLivePath)
    {
        lLogFileLive = LLogFileCheck(lLivePath);
        return lLogFileLive;
    }

    public void LLogFollowSet(bool lFollowTail) => lLogFollowTail = lFollowTail;

    public void LLogScrollHandle(
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

        LLogFollowSet(lScrollable.Value - lOffset.Value <= lRow);
    }

    public void LLogSnapshotSet(long lSequence) => lLogSnapshot = lSequence;

    public bool LLogSnapshotCheck(long lSequence) => lSequence <= lLogSnapshot;

    public bool LLogExpandToggle(LLogRow? lRow)
    {
        if (lRow is null)
        {
            return false;
        }

        lRow.LLogRowExpanded = !lRow.LLogRowExpanded;
        return true;
    }

    public void LLogCategorySet(IReadOnlyList<string> lTokens)
    {
        lLogCategories.Clear();
        foreach (string lToken in lTokens)
        {
            lLogCategories.Add(LTraceEntry.LTraceKindFind(lToken));
        }

        LLogRowsApply();
    }

    public void LLogVerboseSet(bool lVerbose)
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

    public void LLogFilesUpdate(bool lFollowCurrent = false)
    {
        string lCurrentPath = LTraceWriter.LTracePathRead();
        LTraceReadResult<List<string>> lFilesResult = LTraceWriter.LTraceFilesRead();
        List<string> lFiles = lFilesResult.LTraceReadValue;
        if (!lFilesResult.LTraceReadSuccess)
        {
            LLogNoticeRaise("Log.Error.List", lFilesResult.LTraceReadError);
        }

        if (!lFiles.Contains(lCurrentPath, StringComparer.OrdinalIgnoreCase))
        {
            lFiles.Insert(0, lCurrentPath);
        }

        string lSelectedPath = !lFollowCurrent && lLogFiles.Count > 0
            ? lLogFiles[Math.Clamp(lLogFileIndex, 0, lLogFiles.Count - 1)].LLogFilePath
            : lCurrentPath;
        if (!lFiles.Contains(lSelectedPath, StringComparer.OrdinalIgnoreCase))
        {
            lSelectedPath = lCurrentPath;
        }

        if (lLogFiles.Select(lFile => lFile.LLogFilePath).SequenceEqual(lFiles, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        lLogFiles.Clear();
        lLogFileIndex = 0;
        foreach (string lFile in lFiles)
        {
            bool lCurrent = string.Equals(lFile, lCurrentPath, StringComparison.OrdinalIgnoreCase);
            if (string.Equals(lFile, lSelectedPath, StringComparison.OrdinalIgnoreCase))
            {
                lLogFileIndex = lLogFiles.Count;
            }

            lLogFiles.Add(new LLogFile(
                lFile,
                lCurrent
                    ? LLocalization.LLocalizationTextRead("Log.File.Current")
                    : lLogUsher.CUsherStemRead(lFile)));
        }

        LLogFilesChanged?.Invoke();
    }

    public void LLogFileSelect(int lIndex)
    {
        if (lIndex < 0 || lIndex >= lLogFiles.Count)
        {
            return;
        }

        lLogFileIndex = lIndex;
        LLogFileSet(lLogFiles[lIndex].LLogFilePath, LTraceWriter.LTracePathRead());
        LTraceReadResult<string> lRead;
        long lCommitted = lLogSnapshot;
        if (lLogFileLive)
        {
            lRead = LTraceWriter.LTraceWriterRead(out lCommitted);
        }
        else
        {
            lRead = LTraceWriter.LTraceFileRead(lLogFilePath);
        }

        if (!lRead.LTraceReadSuccess)
        {
            LLogNoticeRaise("Log.Error.Read", lRead.LTraceReadError);
            return;
        }

        if (lLogFileLive)
        {
            LLogSnapshotSet(lCommitted);
            lock (lLogPendingLock)
            {
                lLogPending.RemoveAll(lItem => LLogSnapshotCheck(lItem.LLogSequence));
            }
        }

        lLogRowsAll.Clear();
        lLogRowsAll.AddRange(LTraceEntry.LTraceEntryParse(lRead.LTraceReadValue).Select(lEntry => new LLogRow(lEntry)));
        LLogExcessRemove();
        LLogRowsApply();
    }

    public void LLogTick()
    {
        string lCurrentPath = LTraceWriter.LTracePathRead();
        if (lLogFileLive && !LLogFileCheck(lCurrentPath))
        {
            LLogFilesUpdate(true);
            if (!LLogFileCheck(lCurrentPath))
            {
                LLogFileSelect(lLogFileIndex);
            }
        }

        List<(long LLogSequence, LTraceEntry LLogEntry)> lBatch;
        lock (lLogPendingLock)
        {
            if (lLogPending.Count == 0)
            {
                return;
            }

            lBatch = new List<(long, LTraceEntry)>(lLogPending);
            lLogPending.Clear();
        }

        if (!LLogLiveSet(LTraceWriter.LTracePathRead()))
        {
            return;
        }

        foreach ((long lSequence, LTraceEntry lEntry) in lBatch)
        {
            if (LLogSnapshotCheck(lSequence))
            {
                continue;
            }

            var lRow = new LLogRow(lEntry);
            lLogRowsAll.Add(lRow);
            if (LLogCategoryCheck(lRow))
            {
                lLogRowsShown.Add(lRow);
                LLogRowAppended?.Invoke(lRow);
            }
        }

        if (lLogRowsAll.Count > LLogRowMaximum)
        {
            LLogExcessRemove();
        }

        LLogScrollRaise();
    }

    public void LLogTextCopy()
    {
        LTraceReadResult<string> lRead = LTraceWriter.LTraceFileRead(lLogFilePath);
        if (!lRead.LTraceReadSuccess)
        {
            LLogNoticeRaise("Log.Error.Read", lRead.LTraceReadError);
            return;
        }

        LLogCopied?.Invoke(lRead.LTraceReadValue);
    }

    public void LLogFolderOpen()
    {
        if (lLogUsher.CUsherPathOpen(lLogFilePath, "Log file", LTraceWriter.LTraceFolderRead()) is { } lError)
        {
            LLogNoticeRaise("Log.Error.Open", lError);
        }
    }

    public void LLogErrorRaise(string lMessageKey, string lDetail)
    {
        LTraceLog.LTraceErrorRecord($"Log could not be copied: {lDetail}");
        LLogNoticeRaise(lMessageKey, lDetail);
    }

    private void LLogNoticeRaise(string lMessageKey, string lDetail) =>
        LLogErrorShown?.Invoke(LLocalization.LLocalizationFormat(lMessageKey, lDetail));

    private void LLogAppendHandle(long lSequence, LTraceEntry lEntry)
    {
        lock (lLogPendingLock)
        {
            lLogPending.Add((lSequence, lEntry));
        }
    }

    private bool LLogCategoryCheck(LLogRow lRow) =>
        lLogCategories.Count == 0 || lLogCategories.Contains(lRow.LLogRowCategory);

    private void LLogRowsApply()
    {
        lLogRowsShown.Clear();
        lLogRowsShown.AddRange(lLogRowsAll.Where(LLogCategoryCheck));
        LLogRowsRebuilt?.Invoke();
        LLogScrollRaise();
    }

    private void LLogExcessRemove()
    {
        int lExcess = lLogRowsAll.Count - LLogRowMaximum;
        if (lExcess <= 0)
        {
            return;
        }

        var lRemoved = new HashSet<LLogRow>(lLogRowsAll.Take(lExcess));
        lLogRowsAll.RemoveRange(0, lExcess);
        int lShownExcess = lLogRowsShown.TakeWhile(lRemoved.Contains).Count();
        lLogRowsShown.RemoveRange(0, lShownExcess);
        LLogRowsRemoved?.Invoke(lShownExcess);
    }

    private void LLogScrollRaise()
    {
        if (lLogFollowTail && lLogRowsShown.Count > 0)
        {
            LLogScrolled?.Invoke();
        }
    }
}
