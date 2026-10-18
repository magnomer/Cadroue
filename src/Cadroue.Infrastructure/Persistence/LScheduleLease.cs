using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LScheduleLease : LScheduleLeaseContract
{
    public const int LScheduleLeaseSeconds = 10;

    private readonly LSchedule lScheduleLeaseOwner;

    public LScheduleLease(LSchedule lSchedule)
    {
        lScheduleLeaseOwner = lSchedule;
    }

    public static Func<Guid, DateTimeOffset> LScheduleTimeSeam { get; set; } = _ => DateTimeOffset.MinValue;

    public bool LSchedulePendingExist() =>
        LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderScheduled)
            .Select(LScheduleStore.LScheduleRecordRead)
            .Any(lWorkRecord =>
                lWorkRecord is { } lScheduleRecord && LScheduleScope.LScheduleClaimMatch(lScheduleRecord));

    public LWorkItem? LScheduleClaim(Guid lRunnerId)
    {
        LDepotIndex.LDepotIndexCreate();

        if (LScheduleScope.LScheduleSharedSeam())
        {
            LScheduleStale.LScheduleStaleClaim();
        }

        var lScheduleCandidates = new List<LWorkDocument>();
        foreach (string lDepotFilePath in LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderScheduled))
        {
            if (LScheduleStore.LScheduleRecordRead(lDepotFilePath) is { } lWorkRecord)
            {
                lScheduleCandidates.Add(lWorkRecord);
            }
        }

        IEnumerable<LWorkDocument> lScheduleOrdered = lScheduleCandidates
            .Where(LScheduleScope.LScheduleClaimMatch)
            .OrderByDescending(lWorkRecord => lWorkRecord.LWorkPriorityName == nameof(LWorkPriority.LWorkPriorityHigh))
            .ThenBy(lWorkRecord => LScheduleTimeSeam(lWorkRecord.LWorkBatchId))
            .ThenBy(lWorkRecord => lWorkRecord.LWorkCreateTime);

        foreach (LWorkDocument lWorkRecord in lScheduleOrdered)
        {
            lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStateRunning);
            lWorkRecord.LWorkOwnerProcess = Environment.ProcessId;
            lWorkRecord.LWorkOwnerStamp = LSentinel.LSentinelStampRead();
            lWorkRecord.LWorkOwnerRunner = lRunnerId;
            lWorkRecord.LWorkLeaseTime = DateTimeOffset.Now;
            lWorkRecord.LWorkPhaseName = nameof(LWorkPhase.LWorkPhaseStarted);
            lWorkRecord.LWorkAttemptCount++;
            if (!LScheduleStore.LScheduleRecordMove(
                    lWorkRecord,
                    LDepotFolder.LDepotFolderScheduled,
                    LDepotFolder.LDepotFolderRunning))
            {
                continue;
            }

            LWorkItem lWorkClaimed = lWorkRecord.LWorkItemCreate();
            lWorkClaimed.LWorkRunState.LWorkStateCurrent = LWorkState.LWorkStateRunning;
            lScheduleLeaseOwner.LScheduleLiveSet(lWorkClaimed);
            LTraceLog.LTraceInfoRecord(
                $"Schedule: work '{lWorkRecord.LWorkOutputName}' " +
                $"[{LSchedule.LScheduleIdShorten(lWorkRecord.LWorkId)}] claimed");
            return lWorkClaimed;
        }

        return null;
    }

    public void LScheduleLeaseUpdate(Guid lWorkId, Guid lRunnerId)
    {
        if (LScheduleOwnedRead(lWorkId, lRunnerId) is not { } lWorkRecord)
        {
            return;
        }

        lWorkRecord.LWorkLeaseTime = DateTimeOffset.Now;
        LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderRunning);
    }

    public void LSchedulePhaseSet(Guid lWorkId, Guid lRunnerId, LWorkPhase lWorkPhase)
    {
        if (LScheduleOwnedRead(lWorkId, lRunnerId) is not { } lWorkRecord)
        {
            return;
        }

        lWorkRecord.LWorkPhaseName = lWorkPhase.ToString();
        lWorkRecord.LWorkLeaseTime = DateTimeOffset.Now;
        LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderRunning);
    }

    public void LScheduleOutputCommit(Guid lWorkId, Guid lRunnerId, string lWorkOutputPath, string lWorkOutputName)
    {
        if (LScheduleOwnedRead(lWorkId, lRunnerId) is not { } lWorkRecord)
        {
            return;
        }

        lWorkRecord.LWorkOutputPath = lWorkOutputPath;
        lWorkRecord.LWorkOutputName = lWorkOutputName;
        lWorkRecord.LWorkLeaseTime = DateTimeOffset.Now;
        LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderRunning);
    }

    public int LScheduleRelease(Guid lRunnerId)
    {
        LDepotIndex.LDepotIndexCreate();

        int lScheduleReleasedCount = 0;
        foreach (string lDepotFilePath in LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderRunning).ToArray())
        {
            if (LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
                || lWorkRecord.LWorkOwnerRunner != lRunnerId)
            {
                continue;
            }

            if (LScheduleRecordRelease(lWorkRecord))
            {
                lScheduleReleasedCount++;
            }
        }

        lScheduleLeaseOwner.LScheduleLoad();
        return lScheduleReleasedCount;
    }

    public bool LScheduleItemRelease(Guid lWorkId, Guid lRunnerId, string lScheduleMessage) =>
        LScheduleOwnedRead(lWorkId, lRunnerId) is { } lWorkRecord
        && LScheduleOwnedRelease(lWorkRecord, lScheduleMessage);

    public int LScheduleRetryRelease(Guid lWorkId, Guid lRunnerId, int lScheduleRetryMaximum, string lScheduleReason)
    {
        if (LScheduleOwnedRead(lWorkId, lRunnerId) is not { } lWorkRecord
            || lWorkRecord.LWorkRetryCount >= lScheduleRetryMaximum)
        {
            return 0;
        }

        lWorkRecord.LWorkRetryCount++;
        return LScheduleOwnedRelease(
            lWorkRecord, $"{lScheduleReason} Retry {lWorkRecord.LWorkRetryCount} of {lScheduleRetryMaximum}.")
            ? lWorkRecord.LWorkRetryCount
            : 0;
    }

    public bool LScheduleOwnerCheck(LWorkItem lWorkItem, Guid lRunnerId) =>
        lWorkItem.LWorkRunState.LWorkOwnerRunner != Guid.Empty
        && lWorkItem.LWorkRunState.LWorkOwnerRunner == lRunnerId;

    private static LWorkDocument? LScheduleOwnedRead(Guid lWorkId, Guid lRunnerId)
    {
        string lDepotFilePath = LDepot.LDepotFileRead(LDepotFolder.LDepotFolderRunning, lWorkId);
        return File.Exists(lDepotFilePath)
            && LScheduleStore.LScheduleRecordRead(lDepotFilePath) is { } lWorkRecord
            && lWorkRecord.LWorkOwnerRunner == lRunnerId
            ? lWorkRecord
            : null;
    }

    private bool LScheduleOwnedRelease(LWorkDocument lWorkRecord, string lScheduleMessage)
    {
        LScheduleTransition.LSchedulePartialRemove(lWorkRecord);
        if (!LScheduleRecordRelease(lWorkRecord, lScheduleMessage))
        {
            return false;
        }

        lScheduleLeaseOwner.LScheduleLiveRemove(lWorkRecord.LWorkId);
        lScheduleLeaseOwner.LScheduleLoad();
        return true;
    }

    internal static void LScheduleOwnerClear(LWorkDocument lWorkRecord)
    {
        lWorkRecord.LWorkOwnerProcess = 0;
        lWorkRecord.LWorkOwnerStamp = 0;
        lWorkRecord.LWorkOwnerRunner = Guid.Empty;
        lWorkRecord.LWorkLeaseTime = default;
        lWorkRecord.LWorkPhaseName = nameof(LWorkPhase.LWorkPhaseNone);
    }

    internal static bool LScheduleRecordRelease(LWorkDocument lWorkRecord, string? lScheduleMessage = null)
    {
        LScheduleOwnerClear(lWorkRecord);
        lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStatePending);
        lWorkRecord.LWorkProgress = 0;
        lWorkRecord.LWorkMessage = lScheduleMessage ?? string.Empty;
        return LScheduleStore.LScheduleRecordMove(
            lWorkRecord,
            LDepotFolder.LDepotFolderRunning,
            LDepotFolder.LDepotFolderScheduled);
    }
}
