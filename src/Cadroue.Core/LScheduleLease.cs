using System.Diagnostics;

namespace Cadroue.Core;

public sealed partial class LSchedule
{
    public const int LScheduleLeaseSeconds = 10;
    public const int LScheduleAttemptLimit = 3;

    public static Action<string>? LScheduleRecoverReport { get; set; }

    private static readonly TimeSpan LScheduleLeaseExpiry = TimeSpan.FromSeconds(LScheduleLeaseSeconds * 3);
    private static readonly HashSet<Guid> lScheduleLiveRunners = new();
    private static readonly object lScheduleRunnerLock = new();

    public static void LScheduleRunnerAdd(Guid lRunnerId)
    {
        lock (lScheduleRunnerLock)
        {
            lScheduleLiveRunners.Add(lRunnerId);
        }
    }

    public static void LScheduleRunnerRemove(Guid lRunnerId)
    {
        lock (lScheduleRunnerLock)
        {
            lScheduleLiveRunners.Remove(lRunnerId);
        }
    }

    public static bool LScheduleRunnerCheck(Guid lRunnerId)
    {
        lock (lScheduleRunnerLock)
        {
            return lScheduleLiveRunners.Contains(lRunnerId);
        }
    }

    public LWorkItem? LScheduleClaim(Guid lRunnerId)
    {
        LDepotIndex.LDepotIndexCreate();

        var lScheduleCandidates = new List<LWorkRecord>();
        foreach (string lDepotFilePath in LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderScheduled))
        {
            if (LScheduleRecordRead(lDepotFilePath) is { } lWorkRecord)
            {
                lScheduleCandidates.Add(lWorkRecord);
            }
        }

        IEnumerable<LWorkRecord> lScheduleOrdered = lScheduleCandidates
            .OrderByDescending(lWorkRecord => lWorkRecord.LWorkPriorityName == nameof(LWorkPriority.LWorkPriorityHigh))
            .ThenBy(lWorkRecord => lWorkRecord.LWorkCreateTime);

        foreach (LWorkRecord lWorkRecord in lScheduleOrdered)
        {
            if (!LScheduleMove(lWorkRecord.LWorkId, LDepotFolder.LDepotFolderScheduled, LDepotFolder.LDepotFolderRunning))
            {
                continue;
            }

            lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStateRunning);
            lWorkRecord.LWorkOwnerProcess = Environment.ProcessId;
            lWorkRecord.LWorkOwnerRunner = lRunnerId;
            lWorkRecord.LWorkLeaseTime = DateTimeOffset.Now;
            lWorkRecord.LWorkPhaseName = nameof(LWorkPhase.LWorkPhaseStarted);
            lWorkRecord.LWorkAttemptCount++;
            if (!LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderRunning))
            {
                LScheduleRecoverReport?.Invoke(
                    $"Work '{lWorkRecord.LWorkOutputName}' was claimed but its owner/lease could not be written; it may be reclaimed after the lease expires");
            }

            LWorkItem lWorkClaimed = lWorkRecord.LWorkItemCreate();
            lWorkClaimed.LWorkStateCurrent = LWorkState.LWorkStateRunning;
            lScheduleLiveItems[lWorkRecord.LWorkId] = lWorkClaimed;
            return lWorkClaimed;
        }

        return null;
    }

    public void LScheduleLeaseUpdate(Guid lWorkId, Guid lRunnerId)
    {
        string lDepotFilePath = LDepot.LDepotFileRead(LDepotFolder.LDepotFolderRunning, lWorkId);
        if (!File.Exists(lDepotFilePath)
            || LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
            || lWorkRecord.LWorkOwnerRunner != lRunnerId)
        {
            return;
        }

        lWorkRecord.LWorkLeaseTime = DateTimeOffset.Now;
        LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderRunning);
    }

    public void LSchedulePhaseSet(Guid lWorkId, Guid lRunnerId, LWorkPhase lWorkPhase)
    {
        string lDepotFilePath = LDepot.LDepotFileRead(LDepotFolder.LDepotFolderRunning, lWorkId);
        if (!File.Exists(lDepotFilePath)
            || LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
            || lWorkRecord.LWorkOwnerRunner != lRunnerId)
        {
            return;
        }

        lWorkRecord.LWorkPhaseName = lWorkPhase.ToString();
        lWorkRecord.LWorkLeaseTime = DateTimeOffset.Now;
        LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderRunning);
    }

    public int LScheduleRelease(Guid lRunnerId)
    {
        LDepotIndex.LDepotIndexCreate();

        int lScheduleReleasedCount = 0;
        foreach (string lDepotFilePath in LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderRunning).ToArray())
        {
            if (LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
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
            || LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
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
            if (LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
                || !LScheduleStaleCheck(lWorkRecord))
            {
                continue;
            }

            LSchedulePartialRemove(lWorkRecord);
            string lSchedulePhase = LSchedulePhaseFormat(lWorkRecord.LWorkPhaseName);

            if (lWorkRecord.LWorkAttemptCount >= LScheduleAttemptLimit)
            {
                if (LScheduleFailedSet(
                        lWorkRecord,
                        $"Stopped unexpectedly while {lSchedulePhase} on attempt {lWorkRecord.LWorkAttemptCount}. Not retried again."))
                {
                    lScheduleReclaimedCount++;
                    LScheduleRecoverReport?.Invoke(
                        $"Work '{lWorkRecord.LWorkOutputName}' failed: stopped unexpectedly while {lSchedulePhase} " +
                        $"after {lWorkRecord.LWorkAttemptCount} attempt(s)");
                }

                continue;
            }

            if (LScheduleRecordRelease(
                    lWorkRecord,
                    $"Recovered after an unexpected stop while {lSchedulePhase} (attempt {lWorkRecord.LWorkAttemptCount})."))
            {
                lScheduleReclaimedCount++;
                LScheduleRecoverReport?.Invoke(
                    $"Work '{lWorkRecord.LWorkOutputName}' returned to the queue: stopped unexpectedly while {lSchedulePhase} " +
                    $"(attempt {lWorkRecord.LWorkAttemptCount} of {LScheduleAttemptLimit})");
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
        if (!LScheduleMove(lWorkRecord.LWorkId, LDepotFolder.LDepotFolderRunning, LDepotFolder.LDepotFolderFailed))
        {
            return false;
        }

        lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStateFailed);
        lWorkRecord.LWorkOwnerProcess = 0;
        lWorkRecord.LWorkOwnerRunner = Guid.Empty;
        lWorkRecord.LWorkLeaseTime = default;
        lWorkRecord.LWorkPhaseName = nameof(LWorkPhase.LWorkPhaseNone);
        lWorkRecord.LWorkMessage = lScheduleMessage;
        if (!LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderFailed))
        {
            LScheduleRecoverReport?.Invoke(
                $"Work '{lWorkRecord.LWorkOutputName}' was filed as Failed but its details could not be written");
        }

        return true;
    }

    public bool LScheduleOwnerCheck(LWorkItem lWorkItem, Guid lRunnerId) =>
        lWorkItem.LWorkOwnerRunner != Guid.Empty && lWorkItem.LWorkOwnerRunner == lRunnerId;

    public bool LScheduleForeignCheck(LWorkItem lWorkItem, Guid lRunnerId) =>
        lWorkItem.LWorkOwnerRunner != Guid.Empty && lWorkItem.LWorkOwnerRunner != lRunnerId;

    private static bool LScheduleStaleCheck(LWorkRecord lWorkRecord)
    {
        bool lScheduleOwnerAlive = lWorkRecord.LWorkOwnerProcess == Environment.ProcessId
            ? LScheduleRunnerCheck(lWorkRecord.LWorkOwnerRunner)
            : LScheduleProcessCheck(lWorkRecord.LWorkOwnerProcess);

        bool lScheduleLeaseStale = DateTimeOffset.Now - lWorkRecord.LWorkLeaseTime > LScheduleLeaseExpiry;
        return !lScheduleOwnerAlive && lScheduleLeaseStale;
    }

    private static bool LScheduleRecordRelease(LWorkRecord lWorkRecord, string? lScheduleMessage = null)
    {
        if (!LScheduleMove(lWorkRecord.LWorkId, LDepotFolder.LDepotFolderRunning, LDepotFolder.LDepotFolderScheduled))
        {
            return false;
        }

        lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStatePending);
        lWorkRecord.LWorkOwnerProcess = 0;
        lWorkRecord.LWorkOwnerRunner = Guid.Empty;
        lWorkRecord.LWorkLeaseTime = default;
        lWorkRecord.LWorkPhaseName = nameof(LWorkPhase.LWorkPhaseNone);
        lWorkRecord.LWorkProgress = 0;
        lWorkRecord.LWorkMessage = lScheduleMessage ?? string.Empty;
        if (!LScheduleRecordSave(lWorkRecord, LDepotFolder.LDepotFolderScheduled))
        {
            LScheduleRecoverReport?.Invoke(
                $"Work '{lWorkRecord.LWorkOutputName}' was returned to the queue but its details could not be written");
        }

        return true;
    }

    private static bool LScheduleProcessCheck(int lProcessId)
    {
        if (lProcessId <= 0)
        {
            return false;
        }

        try
        {
            using Process lScheduleProcess = Process.GetProcessById(lProcessId);
            return !lScheduleProcess.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
