using System.Collections.ObjectModel;

namespace Cadroue.Core;

public sealed partial class LSchedule
{
    private readonly ObservableCollection<LWorkItem> lScheduleItems = new();
    private readonly Dictionary<Guid, LWorkItem> lScheduleLiveItems = new();

    public LSchedule()
    {
        LScheduleRecords = new ReadOnlyObservableCollection<LWorkItem>(lScheduleItems);
    }

    public static LSchedule LScheduleCurrent { get; } = new();

    public ReadOnlyObservableCollection<LWorkItem> LScheduleRecords { get; }

    public event Action<LSchedule>? LScheduleChange;

    public int LScheduleDoneCount =>
        lScheduleItems.Count(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateDone);

    public void LScheduleChangeRaise() => LScheduleChange?.Invoke(this);

    public void LScheduleLoad()
    {
        LDepotIndex.LDepotIndexCreate();
        LScheduleStaleClaim();
        if (LDepotIndex.LDepotIndexDirty)
        {
            LDepotIndex.LDepotIndexRebuild();
        }

        LScheduleItemsBuild(LDepotIndex.LDepotIndexDirty
            ? LScheduleFolderPairsRead()
            : LScheduleIndexPairsRead());
    }

    private void LScheduleItemsBuild(IEnumerable<(LDepotFolder LDepotFolder, LWorkRecord LWorkRecord)> lSchedulePairs)
    {
        var lScheduleLoaded = new List<LWorkItem>();
        var lScheduleRunningIds = new HashSet<Guid>();
        foreach ((LDepotFolder lDepotFolder, LWorkRecord lWorkRecord) in lSchedulePairs)
        {
            if (lDepotFolder == LDepotFolder.LDepotFolderRunning
                && lScheduleLiveItems.TryGetValue(lWorkRecord.LWorkId, out LWorkItem? lWorkLive))
            {
                lScheduleRunningIds.Add(lWorkRecord.LWorkId);
                lScheduleLoaded.Add(lWorkLive);
                continue;
            }

            LWorkItem lWorkItem = lWorkRecord.LWorkItemCreate();
            lWorkItem.LWorkStateCurrent = LScheduleStateRead(lDepotFolder, lWorkItem.LWorkStateCurrent);
            lScheduleLoaded.Add(lWorkItem);
        }

        foreach (Guid lScheduleLiveId in lScheduleLiveItems.Keys.ToArray())
        {
            if (!lScheduleRunningIds.Contains(lScheduleLiveId))
            {
                lScheduleLiveItems.Remove(lScheduleLiveId);
            }
        }

        lScheduleItems.Clear();
        foreach (LWorkItem lWorkItem in lScheduleLoaded.OrderBy(lItem => lItem.LWorkCreateTime))
        {
            lScheduleItems.Add(lWorkItem);
        }

        LScheduleChange?.Invoke(this);
    }

    private IEnumerable<(LDepotFolder, LWorkRecord)> LScheduleIndexPairsRead()
    {
        foreach ((LDepotFolder lDepotFolder, string lDepotRecord) in LDepotIndex.LDepotRecordsRead())
        {
            if (LScheduleRecordParse(lDepotRecord) is { } lWorkRecord)
            {
                yield return (lDepotFolder, lWorkRecord);
            }
        }
    }

    private static IEnumerable<(LDepotFolder, LWorkRecord)> LScheduleFolderPairsRead()
    {
        foreach (LDepotFolder lDepotFolder in Enum.GetValues<LDepotFolder>())
        {
            foreach (string lDepotFilePath in LDepot.LDepotFilesRead(lDepotFolder))
            {
                if (LScheduleRecordRead(lDepotFilePath) is { } lWorkRecord)
                {
                    yield return (lDepotFolder, lWorkRecord);
                }
            }
        }
    }

    private static LWorkRecord? LScheduleRecordParse(string lScheduleRecordJson)
    {
        try
        {
            return LWorkRecord.LWorkRecordParse(lScheduleRecordJson);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void LScheduleDurationSet(Guid lWorkId, TimeSpan lWorkDuration)
    {
        if (lWorkDuration <= TimeSpan.Zero)
        {
            return;
        }

        foreach (LWorkItem lWorkItem in lScheduleItems)
        {
            if (lWorkItem.LWorkId != lWorkId || lWorkItem.LWorkEnd > TimeSpan.Zero)
            {
                continue;
            }

            lWorkItem.LWorkEnd = lWorkDuration;
            break;
        }

        string lDepotFilePath = LDepot.LDepotFileRead(LDepotFolder.LDepotFolderScheduled, lWorkId);
        if (!File.Exists(lDepotFilePath) || LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord)
        {
            return;
        }

        lWorkRecord.LWorkEndTicks = lWorkDuration.Ticks;
        LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderScheduled);
    }

    public int LScheduleAdd(
        IReadOnlyList<LWorkItem> lWorkItems,
        Guid lScheduleRelayTarget = default,
        Guid lScheduleRelaySource = default)
    {
        if (lWorkItems.Count == 0)
        {
            return 0;
        }

        LDepotIndex.LDepotIndexCreate();
        int lScheduleAddedCount = 0;
        foreach (LWorkItem lWorkItem in lWorkItems)
        {
            if (lScheduleRelayTarget != Guid.Empty)
            {
                lWorkItem.LWorkRelayTarget = lScheduleRelayTarget;
            }

            if (lScheduleRelaySource != Guid.Empty)
            {
                lWorkItem.LWorkRelaySource = lScheduleRelaySource;
            }

            if (lWorkItem.LWorkLineage == Guid.Empty)
            {
                lWorkItem.LWorkLineage = LScheduleLineageResolve(lWorkItem);
            }

            var lWorkRecord = LWorkRecord.LWorkRecordCreate(lWorkItem);
            if (!LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderScheduled))
            {
                continue;
            }

            lScheduleAddedCount++;
            lScheduleItems.Add(lWorkItem);
        }

        if (lScheduleAddedCount > 0)
        {
            LScheduleChange?.Invoke(this);
        }

        return lScheduleAddedCount;
    }

    public Guid LScheduleLineageRead(LWorkItem lWorkItem) =>
        lWorkItem.LWorkLineage == Guid.Empty
            ? LScheduleFileRead(lWorkItem.LWorkSourcePath)
            : lWorkItem.LWorkLineage;

    private Guid LScheduleLineageResolve(LWorkItem lWorkItem)
    {
        if (lWorkItem.LWorkKind == LWorkKind.LWorkKindMerge)
        {
            return Guid.NewGuid();
        }

        LWorkItem? lScheduleParent = LScheduleParentFind(lWorkItem.LWorkSourcePath);
        return lScheduleParent is not null && lScheduleParent.LWorkKind != LWorkKind.LWorkKindSplit
            ? LScheduleLineageRead(lScheduleParent)
            : LScheduleFileRead(lWorkItem.LWorkSourcePath);
    }

    private LWorkItem? LScheduleParentFind(string lWorkSourcePath)
    {
        if (string.IsNullOrWhiteSpace(lWorkSourcePath))
        {
            return null;
        }

        LWorkItem? lScheduleParent = null;
        foreach (LWorkItem lScheduleItem in lScheduleItems)
        {
            if (!LSchedulePathMatch(lScheduleItem.LWorkOutputPath, lWorkSourcePath))
            {
                continue;
            }

            if (lScheduleParent is null || lScheduleItem.LWorkCreateTime > lScheduleParent.LWorkCreateTime)
            {
                lScheduleParent = lScheduleItem;
            }
        }

        return lScheduleParent;
    }

    private static bool LSchedulePathMatch(string lScheduleLeft, string lScheduleRight)
    {
        if (string.IsNullOrWhiteSpace(lScheduleLeft) || string.IsNullOrWhiteSpace(lScheduleRight))
        {
            return false;
        }

        try
        {
            return string.Equals(
                Path.GetFullPath(lScheduleLeft),
                Path.GetFullPath(lScheduleRight),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception lScheduleError) when (
            lScheduleError is ArgumentException or IOException or NotSupportedException)
        {
            return string.Equals(lScheduleLeft, lScheduleRight, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static Guid LScheduleFileRead(string lWorkFilePath)
    {
        if (string.IsNullOrWhiteSpace(lWorkFilePath))
        {
            return Guid.NewGuid();
        }

        string lScheduleKey;
        try
        {
            lScheduleKey = Path.GetFullPath(lWorkFilePath).ToLowerInvariant();
        }
        catch (Exception lScheduleError) when (
            lScheduleError is ArgumentException or IOException or NotSupportedException)
        {
            lScheduleKey = lWorkFilePath.ToLowerInvariant();
        }

        byte[] lScheduleHash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(lScheduleKey));
        return new Guid(lScheduleHash.AsSpan(0, 16));
    }

    public void LScheduleCommit(LWorkItem lWorkItem, bool lScheduleSucceeded, string lScheduleMessage)
    {
        lScheduleLiveItems.Remove(lWorkItem.LWorkId);
        LDepotFolder lScheduleTarget = lScheduleSucceeded
            ? LDepotFolder.LDepotFolderDone
            : LDepotFolder.LDepotFolderFailed;

        if (!LScheduleMove(lWorkItem.LWorkId, LDepotFolder.LDepotFolderRunning, lScheduleTarget))
        {
            LScheduleRecoverReport?.Invoke(
                $"Work '{lWorkItem.LWorkOutputName}' could not be filed as {lScheduleTarget}; it stays running and is retried on the next scan");
            return;
        }

        var lWorkRecord = LWorkRecord.LWorkRecordCreate(lWorkItem);
        lWorkRecord.LWorkStateName = lScheduleSucceeded
            ? nameof(LWorkState.LWorkStateDone)
            : nameof(LWorkState.LWorkStateFailed);
        lWorkRecord.LWorkMessage = lScheduleMessage;
        lWorkRecord.LWorkProgress = lScheduleSucceeded ? 1 : lWorkItem.LWorkProgress;
        if (!LScheduleRecordSave(lWorkRecord, lScheduleTarget))
        {
            LScheduleRecoverReport?.Invoke(
                $"Work '{lWorkItem.LWorkOutputName}' was filed as {lScheduleTarget} but its details could not be written");
        }
    }

    public bool LScheduleItemCancel(LWorkItem lWorkItem)
    {
        lScheduleLiveItems.Remove(lWorkItem.LWorkId);
        LSchedulePartialRemove(LWorkRecord.LWorkRecordCreate(lWorkItem));

        if (!LScheduleMove(lWorkItem.LWorkId, LDepotFolder.LDepotFolderRunning, LDepotFolder.LDepotFolderCancelled))
        {
            return false;
        }

        var lWorkRecord = LWorkRecord.LWorkRecordCreate(lWorkItem);
        lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStateCancelled);
        lWorkRecord.LWorkOwnerProcess = 0;
        lWorkRecord.LWorkOwnerRunner = Guid.Empty;
        lWorkRecord.LWorkLeaseTime = default;
        lWorkRecord.LWorkPhaseName = nameof(LWorkPhase.LWorkPhaseNone);
        lWorkRecord.LWorkProgress = 0;
        lWorkRecord.LWorkMessage = string.Empty;
        LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderCancelled);
        LScheduleLoad();
        return true;
    }

    public bool LScheduleItemReset(Guid lWorkId)
    {
        foreach (LDepotFolder lDepotFolder in new[] { LDepotFolder.LDepotFolderCancelled, LDepotFolder.LDepotFolderFailed })
        {
            string lDepotFilePath = LDepot.LDepotFileRead(lDepotFolder, lWorkId);
            if (!File.Exists(lDepotFilePath) || LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord)
            {
                continue;
            }

            if (!LScheduleMove(lWorkId, lDepotFolder, LDepotFolder.LDepotFolderScheduled))
            {
                return false;
            }

            lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStatePending);
            lWorkRecord.LWorkOwnerProcess = 0;
            lWorkRecord.LWorkOwnerRunner = Guid.Empty;
            lWorkRecord.LWorkLeaseTime = default;
            lWorkRecord.LWorkPhaseName = nameof(LWorkPhase.LWorkPhaseNone);
            lWorkRecord.LWorkAttemptCount = 0;
            lWorkRecord.LWorkProgress = 0;
            lWorkRecord.LWorkMessage = string.Empty;
            LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderScheduled);
            LScheduleLoad();
            return true;
        }

        return false;
    }

    public bool LScheduleRemove(Guid lWorkId)
    {
        bool lScheduleRemoved = false;
        foreach (LDepotFolder lDepotFolder in Enum.GetValues<LDepotFolder>())
        {
            string lDepotFilePath = LDepot.LDepotFileRead(lDepotFolder, lWorkId);
            if (!File.Exists(lDepotFilePath))
            {
                continue;
            }

            try
            {
                File.Delete(lDepotFilePath);
                lScheduleRemoved = true;
            }
            catch (IOException)
            {
            }
        }

        if (lScheduleRemoved)
        {
            LDepotIndex.LDepotIndexRemove(lWorkId);
            LScheduleLoad();
        }

        return lScheduleRemoved;
    }

    public int LScheduleDoneClear() =>
        LScheduleFolderClear(new[] { LDepotFolder.LDepotFolderDone });

    public int LScheduleAllClear() =>
        LScheduleFolderClear(new[]
        {
            LDepotFolder.LDepotFolderScheduled,
            LDepotFolder.LDepotFolderDone,
            LDepotFolder.LDepotFolderFailed,
            LDepotFolder.LDepotFolderCancelled
        });

    private int LScheduleFolderClear(IReadOnlyList<LDepotFolder> lDepotFolders)
    {
        int lScheduleClearedCount = 0;
        foreach (LDepotFolder lDepotFolder in lDepotFolders)
        {
            foreach (string lDepotFilePath in LDepot.LDepotFilesRead(lDepotFolder).ToArray())
            {
                try
                {
                    File.Delete(lDepotFilePath);
                    lScheduleClearedCount++;
                }
                catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
                {
                }
            }
        }

        LDepotIndex.LDepotIndexRebuild();
        LScheduleLoad();
        return lScheduleClearedCount;
    }

    public IReadOnlyList<LWorkItem> LSchedulePendingRead() =>
        lScheduleItems
            .Where(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStatePending)
            .ToArray();

    public bool LSchedulePendingExist() =>
        LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderScheduled).Any();

    private static LWorkState LScheduleStateRead(LDepotFolder lDepotFolder, LWorkState lScheduleFileState) =>
        lDepotFolder switch
        {
            LDepotFolder.LDepotFolderRunning => LWorkState.LWorkStateRunning,
            LDepotFolder.LDepotFolderDone => LWorkState.LWorkStateDone,
            LDepotFolder.LDepotFolderFailed => LWorkState.LWorkStateFailed,
            LDepotFolder.LDepotFolderCancelled => LWorkState.LWorkStateCancelled,
            _ => lScheduleFileState == LWorkState.LWorkStateRunning ? LWorkState.LWorkStatePending : lScheduleFileState
        };

    private static LWorkRecord? LScheduleRecordRead(string lDepotFilePath)
    {
        try
        {
            return LWorkRecord.LWorkRecordParse(File.ReadAllText(lDepotFilePath));
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static bool LScheduleRecordSave(LWorkRecord lWorkRecord, LDepotFolder lDepotFolder)
    {
        string lDepotFilePath = LDepot.LDepotFileRead(lDepotFolder, lWorkRecord.LWorkId);
        if (!LScheduleFileWrite(lDepotFilePath, lWorkRecord.LWorkJsonCreate()))
        {
            LDepotIndex.LDepotIndexInvalidate();
            return false;
        }

        LDepotIndex.LDepotIndexSet(lWorkRecord, lDepotFolder);
        return true;
    }

    private static bool LScheduleFileWrite(string lDepotFilePath, string lDepotContent)
    {
        string lDepotTempPath = lDepotFilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(lDepotTempPath, lDepotContent);
            File.Move(lDepotTempPath, lDepotFilePath, overwrite: true);
            return true;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
            try
            {
                if (File.Exists(lDepotTempPath))
                {
                    File.Delete(lDepotTempPath);
                }
            }
            catch (Exception lCleanup) when (lCleanup is IOException or UnauthorizedAccessException)
            {
            }

            return false;
        }
    }

    private static bool LScheduleMove(Guid lWorkId, LDepotFolder lDepotFrom, LDepotFolder lDepotTo)
    {
        string lDepotFromPath = LDepot.LDepotFileRead(lDepotFrom, lWorkId);
        string lDepotToPath = LDepot.LDepotFileRead(lDepotTo, lWorkId);

        try
        {
            File.Move(lDepotFromPath, lDepotToPath, overwrite: false);
            return true;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
