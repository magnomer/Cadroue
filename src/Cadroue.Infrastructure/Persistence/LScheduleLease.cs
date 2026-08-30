using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LSchedule
{
    public const int LScheduleLeaseSeconds = 10;
    public const int LScheduleAttemptLimit = 3;

    internal static string LScheduleIdShorten(Guid lWorkId) => lWorkId.ToString("N")[..8];

    internal static bool LScheduleSignetMatch(LWorkRecord lWorkRecord) =>
        lWorkRecord.LWorkSignet == Guid.Empty
        || lWorkRecord.LWorkSignet == LSignet.LSignetCurrent;

    public LWorkItem? LScheduleClaim(Guid lRunnerId)
    {
        LDepotIndex.LDepotIndexCreate();

        var lScheduleCandidates = new List<LWorkRecord>();
        foreach (string lDepotFilePath in LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderScheduled))
        {
            if (LScheduleStore.LScheduleRecordRead(lDepotFilePath) is { } lWorkRecord)
            {
                lScheduleCandidates.Add(lWorkRecord);
            }
        }

        IEnumerable<LWorkRecord> lScheduleOrdered = lScheduleCandidates
            .Where(LScheduleSignetMatch)
            .OrderByDescending(lWorkRecord => lWorkRecord.LWorkPriorityName == nameof(LWorkPriority.LWorkPriorityHigh))
            .ThenBy(lWorkRecord => Cadroue.Application.LGate.LGateTimeRead(lWorkRecord.LWorkBatchId))
            .ThenBy(lWorkRecord => lWorkRecord.LWorkCreateTime);

        foreach (LWorkRecord lWorkRecord in lScheduleOrdered)
        {
            if (!LScheduleStore.LScheduleMove(lWorkRecord.LWorkId, LDepotFolder.LDepotFolderScheduled, LDepotFolder.LDepotFolderRunning))
            {
                continue;
            }

            lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStateRunning);
            lWorkRecord.LWorkOwnerProcess = Environment.ProcessId;
            lWorkRecord.LWorkOwnerStamp = LSentinel.LSentinelStampRead();
            lWorkRecord.LWorkOwnerRunner = lRunnerId;
            lWorkRecord.LWorkLeaseTime = DateTimeOffset.Now;
            lWorkRecord.LWorkPhaseName = nameof(LWorkPhase.LWorkPhaseStarted);
            lWorkRecord.LWorkAttemptCount++;
            if (!LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderRunning))
            {
                LTraceLog.LTraceWarningRecord(
                    $"Schedule: work '{lWorkRecord.LWorkOutputName}' [{LScheduleIdShorten(lWorkRecord.LWorkId)}] was claimed but its owner/lease could not be written; it may be reclaimed after the lease expires");
            }

            LWorkItem lWorkClaimed = lWorkRecord.LWorkItemCreate();
            lWorkClaimed.LWorkStateCurrent = LWorkState.LWorkStateRunning;
            lScheduleLiveItems[lWorkRecord.LWorkId] = lWorkClaimed;
            LTraceLog.LTraceInfoRecord(
                $"Schedule: work '{lWorkRecord.LWorkOutputName}' [{LScheduleIdShorten(lWorkRecord.LWorkId)}] claimed");
            return lWorkClaimed;
        }

        return null;
    }

    public void LScheduleLeaseUpdate(Guid lWorkId, Guid lRunnerId)
    {
        string lDepotFilePath = LDepot.LDepotFileRead(LDepotFolder.LDepotFolderRunning, lWorkId);
        if (!File.Exists(lDepotFilePath)
            || LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
            || lWorkRecord.LWorkOwnerRunner != lRunnerId)
        {
            return;
        }

        lWorkRecord.LWorkLeaseTime = DateTimeOffset.Now;
        LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderRunning);
    }

    public void LSchedulePhaseSet(Guid lWorkId, Guid lRunnerId, LWorkPhase lWorkPhase)
    {
        string lDepotFilePath = LDepot.LDepotFileRead(LDepotFolder.LDepotFolderRunning, lWorkId);
        if (!File.Exists(lDepotFilePath)
            || LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
            || lWorkRecord.LWorkOwnerRunner != lRunnerId)
        {
            return;
        }

        lWorkRecord.LWorkPhaseName = lWorkPhase.ToString();
        lWorkRecord.LWorkLeaseTime = DateTimeOffset.Now;
        LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderRunning);
    }

    public void LScheduleOutputCommit(Guid lWorkId, Guid lRunnerId, string lWorkOutputPath, string lWorkOutputName)
    {
        string lDepotFilePath = LDepot.LDepotFileRead(LDepotFolder.LDepotFolderRunning, lWorkId);
        if (!File.Exists(lDepotFilePath)
            || LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
            || lWorkRecord.LWorkOwnerRunner != lRunnerId)
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

        LScheduleLoad();
        return lScheduleReleasedCount;
    }

    public bool LScheduleItemRelease(Guid lWorkId, Guid lRunnerId, string lScheduleMessage)
    {
        string lDepotFilePath = LDepot.LDepotFileRead(LDepotFolder.LDepotFolderRunning, lWorkId);
        if (!File.Exists(lDepotFilePath)
            || LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
            || lWorkRecord.LWorkOwnerRunner != lRunnerId)
        {
            return false;
        }

        LSchedulePartialRemove(lWorkRecord);
        if (!LScheduleRecordRelease(lWorkRecord, lScheduleMessage))
        {
            return false;
        }

        lScheduleLiveItems.Remove(lWorkId);
        LScheduleLoad();
        return true;
    }

    public int LScheduleStaleClaim()
    {
        LDepotIndex.LDepotIndexCreate();

        int lScheduleReclaimedCount = 0;
        foreach (string lDepotFilePath in LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderRunning).ToArray())
        {
            if (LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
                || !LSentinel.LSentinelStaleCheck(lWorkRecord))
            {
                continue;
            }

            LSchedulePartialRemove(lWorkRecord);
            string lSchedulePhase = LSchedulePhaseFormat(lWorkRecord.LWorkPhaseName);

            if (lWorkRecord.LWorkRecoverCount >= LScheduleAttemptLimit)
            {
                if (LScheduleFailedSet(
                        lWorkRecord,
                        $"Stopped unexpectedly while {lSchedulePhase} on attempt {lWorkRecord.LWorkRecoverCount}. Not retried again."))
                {
                    lScheduleReclaimedCount++;
                    LTraceLog.LTraceWarningRecord(
                        $"Schedule: work '{lWorkRecord.LWorkOutputName}' [{LScheduleIdShorten(lWorkRecord.LWorkId)}] failed: stopped unexpectedly while {lSchedulePhase} " +
                        $"after {lWorkRecord.LWorkRecoverCount} recovery attempt(s)");
                }

                continue;
            }

            lWorkRecord.LWorkRecoverCount++;
            if (LScheduleRecordRelease(
                    lWorkRecord,
                    $"Recovered after an unexpected stop while {lSchedulePhase} (attempt {lWorkRecord.LWorkRecoverCount})."))
            {
                lScheduleReclaimedCount++;
                LTraceLog.LTraceWarningRecord(
                    $"Schedule: work '{lWorkRecord.LWorkOutputName}' [{LScheduleIdShorten(lWorkRecord.LWorkId)}] returned to the queue: stopped unexpectedly while {lSchedulePhase} " +
                    $"(attempt {lWorkRecord.LWorkRecoverCount} of {LScheduleAttemptLimit})");
            }
        }

        return lScheduleReclaimedCount;
    }

    private static string LSchedulePhaseFormat(string lWorkPhase) =>
        Enum.TryParse(lWorkPhase, out LWorkPhase lSchedulePhase) && lSchedulePhase == LWorkPhase.LWorkPhaseEncoding
            ? "being processed"
            : "starting";

    private static void LSchedulePartialRemove(LWorkRecord lWorkRecord)
    {
        if (string.IsNullOrWhiteSpace(lWorkRecord.LWorkOutputPath))
        {
            return;
        }

        try
        {
            if (File.Exists(lWorkRecord.LWorkOutputPath))
            {
                File.Delete(lWorkRecord.LWorkOutputPath);
            }
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static bool LScheduleFailedSet(LWorkRecord lWorkRecord, string lScheduleMessage)
    {
        if (!LScheduleStore.LScheduleMove(lWorkRecord.LWorkId, LDepotFolder.LDepotFolderRunning, LDepotFolder.LDepotFolderFailed))
        {
            return false;
        }

        lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStateFailed);
        lWorkRecord.LWorkOwnerProcess = 0;
        lWorkRecord.LWorkOwnerStamp = 0;
        lWorkRecord.LWorkOwnerRunner = Guid.Empty;
        lWorkRecord.LWorkLeaseTime = default;
        lWorkRecord.LWorkPhaseName = nameof(LWorkPhase.LWorkPhaseNone);
        lWorkRecord.LWorkMessage = lScheduleMessage;
        if (!LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderFailed))
        {
            LTraceLog.LTraceWarningRecord(
                $"Schedule: work '{lWorkRecord.LWorkOutputName}' [{LScheduleIdShorten(lWorkRecord.LWorkId)}] was filed as Failed but its details could not be written");
        }

        return true;
    }

    public bool LScheduleOwnerCheck(LWorkItem lWorkItem, Guid lRunnerId) =>
        lWorkItem.LWorkOwnerRunner != Guid.Empty && lWorkItem.LWorkOwnerRunner == lRunnerId;

    public bool LScheduleForeignCheck(LWorkItem lWorkItem, Guid lRunnerId) =>
        lWorkItem.LWorkOwnerRunner != Guid.Empty && lWorkItem.LWorkOwnerRunner != lRunnerId;

    private static bool LScheduleRecordRelease(LWorkRecord lWorkRecord, string? lScheduleMessage = null)
    {
        if (!LScheduleStore.LScheduleMove(lWorkRecord.LWorkId, LDepotFolder.LDepotFolderRunning, LDepotFolder.LDepotFolderScheduled))
        {
            return false;
        }

        lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStatePending);
        lWorkRecord.LWorkOwnerProcess = 0;
        lWorkRecord.LWorkOwnerStamp = 0;
        lWorkRecord.LWorkOwnerRunner = Guid.Empty;
        lWorkRecord.LWorkLeaseTime = default;
        lWorkRecord.LWorkPhaseName = nameof(LWorkPhase.LWorkPhaseNone);
        lWorkRecord.LWorkProgress = 0;
        lWorkRecord.LWorkMessage = lScheduleMessage ?? string.Empty;
        if (!LScheduleStore.LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderScheduled))
        {
            LTraceLog.LTraceWarningRecord(
                $"Schedule: work '{lWorkRecord.LWorkOutputName}' [{LScheduleIdShorten(lWorkRecord.LWorkId)}] was returned to the queue but its details could not be written");
        }

        return true;
    }

}
