using System.Diagnostics;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LSentinel
{
    private static readonly TimeSpan LSentinelLeaseExpiry =
        TimeSpan.FromSeconds(LScheduleLease.LScheduleLeaseSeconds * 3);

    private static readonly long lSentinelStamp = LSentinelStampResolve();

    public static long LSentinelStampRead() => lSentinelStamp;

    public static bool LSentinelOwnerCheck(int lProcessId, long lOwnerStamp) =>
        lProcessId == Environment.ProcessId
            ? lOwnerStamp == 0 || lOwnerStamp == lSentinelStamp
            : LSentinelProcessCheck(lProcessId, lOwnerStamp);

    public static bool LSentinelOwnerCheck(int lProcessId, long lOwnerStamp, Guid lRunnerId) =>
        lProcessId == Environment.ProcessId
            ? LSentinelRunner.LSentinelRunnerCheck(lRunnerId)
            : LSentinelProcessCheck(lProcessId, lOwnerStamp);

    private static long LSentinelStampResolve()
    {
        try
        {
            using Process lSentinelProcess = Process.GetCurrentProcess();
            return lSentinelProcess.StartTime.ToUniversalTime().Ticks;
        }
        catch (Exception lException) when (
            lException is InvalidOperationException
                or System.ComponentModel.Win32Exception)
        {
            return 0;
        }
    }

    internal static bool LSentinelStaleCheck(LWorkDocument lWorkRecord)
    {
        bool lSentinelOwnerAlive = LSentinelOwnerCheck(
            lWorkRecord.LWorkOwnerProcess, lWorkRecord.LWorkOwnerStamp, lWorkRecord.LWorkOwnerRunner);

        bool lSentinelLeaseStale = DateTimeOffset.Now - lWorkRecord.LWorkLeaseTime > LSentinelLeaseExpiry;
        return !lSentinelOwnerAlive && lSentinelLeaseStale;
    }

    private static bool LSentinelProcessCheck(int lProcessId, long lOwnerStamp)
    {
        if (lProcessId <= 0)
        {
            return false;
        }

        try
        {
            using Process lSentinelProcess = Process.GetProcessById(lProcessId);
            if (lSentinelProcess.HasExited)
            {
                return false;
            }

            if (lOwnerStamp == 0)
            {
                return true;
            }

            return lSentinelProcess.StartTime.ToUniversalTime().Ticks == lOwnerStamp;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
