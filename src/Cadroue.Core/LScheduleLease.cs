using System.Diagnostics;

namespace Cadroue.Core;

public sealed partial class LSchedule
{
    public const int LScheduleLeaseSeconds = 10;

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
            LScheduleRecordWrite(lWorkRecord, LDepotFolder.LDepotFolderRunning);
            return lWorkRecord.LWorkItemCreate();
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
        int lScheduleReclaimedCount = 0;
        foreach (string lDepotFilePath in LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderRunning).ToArray())
        {
            if (LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
                || !LScheduleStaleCheck(lWorkRecord))
            {
                continue;
            }

            if (LScheduleReturn(lWorkRecord))
            {
                lScheduleReclaimedCount++;
            }
        }

        return lScheduleReclaimedCount;
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

    private static bool LScheduleReturn(LWorkRecord lWorkRecord)
    {
        if (!LScheduleMove(lWorkRecord.WorkId, LDepotFolder.LDepotFolderRunning, LDepotFolder.LDepotFolderScheduled))
        {
            return false;
        }

        lWorkRecord.State = nameof(LWorkState.LWorkStatePending);
        lWorkRecord.OwnerProcessId = 0;
        lWorkRecord.OwnerRunnerId = Guid.Empty;
        lWorkRecord.LeaseTime = default;
        lWorkRecord.Progress = 0;
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
