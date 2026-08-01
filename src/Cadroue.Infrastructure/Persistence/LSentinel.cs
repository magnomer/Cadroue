using System.Diagnostics;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LSentinel
{
    private static readonly TimeSpan LSentinelLeaseExpiry =
        TimeSpan.FromSeconds(LSchedule.LScheduleLeaseSeconds * 3);

    private static readonly HashSet<Guid> lSentinelLiveRunners = new();
    private static readonly object lSentinelRunnerLock = new();

    public static void LSentinelRunnerAdd(Guid lRunnerId)
    {
        lock (lSentinelRunnerLock)
        {
            lSentinelLiveRunners.Add(lRunnerId);
        }
    }

    public static void LSentinelRunnerRemove(Guid lRunnerId)
    {
        lock (lSentinelRunnerLock)
        {
            lSentinelLiveRunners.Remove(lRunnerId);
        }
    }

    public static bool LSentinelRunnerCheck(Guid lRunnerId)
    {
        lock (lSentinelRunnerLock)
        {
            return lSentinelLiveRunners.Contains(lRunnerId);
        }
    }

    internal static bool LSentinelStaleCheck(LWorkRecord lWorkRecord)
    {
        bool lSentinelOwnerAlive = lWorkRecord.LWorkOwnerProcess == Environment.ProcessId
            ? LSentinelRunnerCheck(lWorkRecord.LWorkOwnerRunner)
            : LSentinelProcessCheck(lWorkRecord.LWorkOwnerProcess);

        bool lSentinelLeaseStale = DateTimeOffset.Now - lWorkRecord.LWorkLeaseTime > LSentinelLeaseExpiry;
        return !lSentinelOwnerAlive && lSentinelLeaseStale;
    }

    private static bool LSentinelProcessCheck(int lProcessId)
    {
        if (lProcessId <= 0)
        {
            return false;
        }

        try
        {
            using Process lSentinelProcess = Process.GetProcessById(lProcessId);
            return !lSentinelProcess.HasExited;
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
