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
        LDepotIndex.LDepotIndexEnsure();

        var lScheduleCandidates = new List<LWorkRecord>();
        foreach (string lDepotFilePath in LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderScheduled))
        {
            if (LScheduleRecordRead(lDepotFilePath) is { } lWorkRecord)
            {
                lScheduleCandidates.Add(lWorkRecord);
            }
        }

        IEnumerable<LWorkRecord> lScheduleOrdered = lScheduleCandidates
            .OrderByDescending(lWorkRecord => lWorkRecord.Priority == nameof(LWorkPriority.LWorkPriorityHigh))
            .ThenBy(lWorkRecord => lWorkRecord.CreateTime);

        foreach (LWorkRecord lWorkRecord in lScheduleOrdered)
        {
            if (!LScheduleMove(lWorkRecord.WorkId, LDepotFolder.LDepotFolderScheduled, LDepotFolder.LDepotFolderRunning))
            {
                continue;
            }

            lWorkRecord.State = nameof(LWorkState.LWorkStateRunning);
            lWorkRecord.OwnerProcessId = Environment.ProcessId;
            lWorkRecord.OwnerRunnerId = lRunnerId;
            lWorkRecord.LeaseTime = DateTimeOffset.Now;
            lWorkRecord.Phase = nameof(LWorkPhase.LWorkPhaseStarted);
            lWorkRecord.AttemptCount++;
            LScheduleRecordWrite(lWorkRecord, LDepotFolder.LDepotFolderRunning);

            LWorkItem lWorkClaimed = lWorkRecord.LWorkItemCreate();
            lWorkClaimed.LWorkStateCurrent = LWorkState.LWorkStateRunning;
            lScheduleLiveItems[lWorkRecord.WorkId] = lWorkClaimed;
            return lWorkClaimed;
        }

        return null;
    }

    public void LScheduleLeaseUpdate(Guid lWorkId, Guid lRunnerId)
    {
        string lDepotFilePath = LDepot.LDepotFilePathRead(LDepotFolder.LDepotFolderRunning, lWorkId);
        if (!File.Exists(lDepotFilePath)
            || LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
            || lWorkRecord.OwnerRunnerId != lRunnerId)
        {
            return;
        }

        lWorkRecord.LeaseTime = DateTimeOffset.Now;
        LScheduleRecordWrite(lWorkRecord, LDepotFolder.LDepotFolderRunning);
    }

    public void LSchedulePhaseSet(Guid lWorkId, Guid lRunnerId, LWorkPhase lWorkPhase)
    {
        string lDepotFilePath = LDepot.LDepotFilePathRead(LDepotFolder.LDepotFolderRunning, lWorkId);
        if (!File.Exists(lDepotFilePath)
            || LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
            || lWorkRecord.OwnerRunnerId != lRunnerId)
        {
            return;
        }

        lWorkRecord.Phase = lWorkPhase.ToString();
        lWorkRecord.LeaseTime = DateTimeOffset.Now;
        LScheduleRecordWrite(lWorkRecord, LDepotFolder.LDepotFolderRunning);
    }

    public int LScheduleRelease(Guid lRunnerId)
    {
        LDepotIndex.LDepotIndexEnsure();

        int lScheduleReleasedCount = 0;
        foreach (string lDepotFilePath in LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderRunning).ToArray())
        {
            if (LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
                || lWorkRecord.OwnerRunnerId != lRunnerId)
            {
                continue;
            }

            if (LScheduleReturn(lWorkRecord))
            {
                lScheduleReleasedCount++;
            }
        }

        LScheduleReload();
        return lScheduleReleasedCount;
    }

    public int LScheduleStaleClaim()
    {
        LDepotIndex.LDepotIndexEnsure();

        int lScheduleReclaimedCount = 0;
        foreach (string lDepotFilePath in LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderRunning).ToArray())
        {
            if (LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
                || !LScheduleStaleCheck(lWorkRecord))
            {
                continue;
            }

            LSchedulePartialRemove(lWorkRecord);
            string lSchedulePhase = LSchedulePhaseFormat(lWorkRecord.Phase);

            if (lWorkRecord.AttemptCount >= LScheduleAttemptLimit)
            {
                if (LScheduleFailedSet(
                        lWorkRecord,
                        $"Stopped unexpectedly while {lSchedulePhase} on attempt {lWorkRecord.AttemptCount}. Not retried again."))
                {
                    lScheduleReclaimedCount++;
                    LScheduleRecoverReport?.Invoke(
                        $"Work '{lWorkRecord.OutputName}' failed: stopped unexpectedly while {lSchedulePhase} " +
                        $"after {lWorkRecord.AttemptCount} attempt(s)");
                }

                continue;
            }

            if (LScheduleReturn(
                    lWorkRecord,
                    $"Recovered after an unexpected stop while {lSchedulePhase} (attempt {lWorkRecord.AttemptCount})."))
            {
                lScheduleReclaimedCount++;
                LScheduleRecoverReport?.Invoke(
                    $"Work '{lWorkRecord.OutputName}' returned to the queue: stopped unexpectedly while {lSchedulePhase} " +
                    $"(attempt {lWorkRecord.AttemptCount} of {LScheduleAttemptLimit})");
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
        if (string.IsNullOrWhiteSpace(lWorkRecord.OutputPath))
        {
            return;
        }

        try
        {
            if (File.Exists(lWorkRecord.OutputPath))
            {
                File.Delete(lWorkRecord.OutputPath);
            }
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static bool LScheduleFailedSet(LWorkRecord lWorkRecord, string lScheduleMessage)
    {
        if (!LScheduleMove(lWorkRecord.WorkId, LDepotFolder.LDepotFolderRunning, LDepotFolder.LDepotFolderFailed))
        {
            return false;
        }

        lWorkRecord.State = nameof(LWorkState.LWorkStateFailed);
        lWorkRecord.OwnerProcessId = 0;
        lWorkRecord.OwnerRunnerId = Guid.Empty;
        lWorkRecord.LeaseTime = default;
        lWorkRecord.Phase = nameof(LWorkPhase.LWorkPhaseNone);
        lWorkRecord.Message = lScheduleMessage;
        LScheduleRecordWrite(lWorkRecord, LDepotFolder.LDepotFolderFailed);
        return true;
    }

    public bool LScheduleOwnerCheck(LWorkItem lWorkItem, Guid lRunnerId) =>
        lWorkItem.LWorkOwnerRunner != Guid.Empty && lWorkItem.LWorkOwnerRunner == lRunnerId;

    public bool LScheduleForeignCheck(LWorkItem lWorkItem, Guid lRunnerId) =>
        lWorkItem.LWorkOwnerRunner != Guid.Empty && lWorkItem.LWorkOwnerRunner != lRunnerId;

    private static bool LScheduleStaleCheck(LWorkRecord lWorkRecord)
    {
        bool lScheduleOwnerAlive = lWorkRecord.OwnerProcessId == Environment.ProcessId
            ? LScheduleRunnerCheck(lWorkRecord.OwnerRunnerId)
            : LScheduleProcessCheck(lWorkRecord.OwnerProcessId);

        bool lScheduleLeaseStale = DateTimeOffset.Now - lWorkRecord.LeaseTime > LScheduleLeaseExpiry;
        return !lScheduleOwnerAlive && lScheduleLeaseStale;
    }

    private static bool LScheduleReturn(LWorkRecord lWorkRecord, string? lScheduleMessage = null)
    {
        if (!LScheduleMove(lWorkRecord.WorkId, LDepotFolder.LDepotFolderRunning, LDepotFolder.LDepotFolderScheduled))
        {
            return false;
        }

        lWorkRecord.State = nameof(LWorkState.LWorkStatePending);
        lWorkRecord.OwnerProcessId = 0;
        lWorkRecord.OwnerRunnerId = Guid.Empty;
        lWorkRecord.LeaseTime = default;
        lWorkRecord.Phase = nameof(LWorkPhase.LWorkPhaseNone);
        lWorkRecord.Progress = 0;
        lWorkRecord.Message = lScheduleMessage ?? string.Empty;
        LScheduleRecordWrite(lWorkRecord, LDepotFolder.LDepotFolderScheduled);
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
