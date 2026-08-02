using System.Collections.ObjectModel;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LSchedule : LScheduleContract
{
    private readonly ObservableCollection<LWorkItem> lScheduleItems = new();
    private readonly Dictionary<Guid, LWorkItem> lScheduleLiveItems = new();

    public LSchedule()
    {
        LScheduleRecords = new ReadOnlyObservableCollection<LWorkItem>(lScheduleItems);
    }

    public ReadOnlyObservableCollection<LWorkItem> LScheduleRecords { get; }

    public event Action<LScheduleContract>? LScheduleChange;

    public event Action<LWorkItem, LScheduleNotice>? LScheduleItemChange;

    public int LScheduleDoneCount =>
        lScheduleItems.Count(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateDone);

    public void LScheduleChangeRaise() => LScheduleChange?.Invoke(this);

    public void LScheduleItemRaise(LWorkItem lWorkItem, LScheduleNotice lScheduleNotice) =>
        LScheduleItemChange?.Invoke(lWorkItem, lScheduleNotice);

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
            if (LScheduleStore.LScheduleRecordParse(lDepotRecord) is { } lWorkRecord)
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
                if (LScheduleStore.LScheduleRecordRead(lDepotFilePath) is { } lWorkRecord)
                {
                    yield return (lDepotFolder, lWorkRecord);
                }
            }
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
            LScheduleItemRaise(lWorkItem, LScheduleNotice.LScheduleNoticeStatus);
            break;
        }

        string lDepotFilePath = LDepot.LDepotFileRead(LDepotFolder.LDepotFolderScheduled, lWorkId);
        if (!File.Exists(lDepotFilePath) || LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord)
        {
            return;
        }

        lWorkRecord.LWorkEndTicks = lWorkDuration.Ticks;
        LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderScheduled);
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
                lWorkItem.LWorkLineage = LScheduleLineage.LScheduleLineageResolve(lWorkItem, lScheduleItems);
            }

            var lWorkRecord = LWorkRecord.LWorkRecordCreate(lWorkItem);
            if (!LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderScheduled))
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
        LScheduleLineage.LScheduleLineageRead(lWorkItem);

    public void LScheduleCommit(LWorkItem lWorkItem, bool lScheduleSucceeded, string lScheduleMessage)
    {
        lScheduleLiveItems.Remove(lWorkItem.LWorkId);
        LDepotFolder lScheduleTarget = lScheduleSucceeded
            ? LDepotFolder.LDepotFolderDone
            : LDepotFolder.LDepotFolderFailed;

        if (!LScheduleStore.LScheduleMove(lWorkItem.LWorkId, LDepotFolder.LDepotFolderRunning, lScheduleTarget))
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
        if (!LScheduleStore.LScheduleRecordSave(lWorkRecord, lScheduleTarget))
        {
            LScheduleRecoverReport?.Invoke(
                $"Work '{lWorkItem.LWorkOutputName}' was filed as {lScheduleTarget} but its details could not be written");
        }
    }

    public bool LScheduleItemCancel(LWorkItem lWorkItem)
    {
        lScheduleLiveItems.Remove(lWorkItem.LWorkId);
        LSchedulePartialRemove(LWorkRecord.LWorkRecordCreate(lWorkItem));

        if (!LScheduleStore.LScheduleMove(lWorkItem.LWorkId, LDepotFolder.LDepotFolderRunning, LDepotFolder.LDepotFolderCancelled))
        {
            return false;
        }

        var lWorkRecord = LWorkRecord.LWorkRecordCreate(lWorkItem);
        lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStateCancelled);
        lWorkRecord.LWorkOwnerProcess = 0;
        lWorkRecord.LWorkOwnerStamp = 0;
        lWorkRecord.LWorkOwnerRunner = Guid.Empty;
        lWorkRecord.LWorkLeaseTime = default;
        lWorkRecord.LWorkPhaseName = nameof(LWorkPhase.LWorkPhaseNone);
        lWorkRecord.LWorkProgress = 0;
        lWorkRecord.LWorkMessage = string.Empty;
        LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderCancelled);
        LScheduleLoad();
        return true;
    }

    public bool LScheduleItemReset(Guid lWorkId)
    {
        foreach (LDepotFolder lDepotFolder in new[] { LDepotFolder.LDepotFolderCancelled, LDepotFolder.LDepotFolderFailed })
        {
            string lDepotFilePath = LDepot.LDepotFileRead(lDepotFolder, lWorkId);
            if (!File.Exists(lDepotFilePath) || LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord)
            {
                continue;
            }

            if (!LScheduleStore.LScheduleMove(lWorkId, lDepotFolder, LDepotFolder.LDepotFolderScheduled))
            {
                return false;
            }

            lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStatePending);
            lWorkRecord.LWorkOwnerProcess = 0;
            lWorkRecord.LWorkOwnerStamp = 0;
            lWorkRecord.LWorkOwnerRunner = Guid.Empty;
            lWorkRecord.LWorkLeaseTime = default;
            lWorkRecord.LWorkPhaseName = nameof(LWorkPhase.LWorkPhaseNone);
            lWorkRecord.LWorkAttemptCount = 0;
            lWorkRecord.LWorkRecoverCount = 0;
            lWorkRecord.LWorkProgress = 0;
            lWorkRecord.LWorkMessage = string.Empty;
            LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderScheduled);
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
}
