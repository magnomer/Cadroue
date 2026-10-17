using System.Collections.Concurrent;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed class LRunnerLease
{
    private readonly Guid lRunnerLeaseId;
    private readonly LScheduleLeaseContract lRunnerLeaseSchedule;
    private readonly ConcurrentDictionary<Guid, System.Timers.Timer> lRunnerLeaseTimers = new();

    internal LRunnerLease(LScheduleLeaseContract lSchedule, Guid lRunnerId)
    {
        lRunnerLeaseSchedule = lSchedule;
        lRunnerLeaseId = lRunnerId;
    }

    public static int LRunnerLeaseSeam { get; set; } = 10;

    internal void LRunnerLeaseStart(LWorkItem lWorkItem)
    {
        LRunnerLeaseStop(lWorkItem.LWorkId);
        var lRunnerTimer = new System.Timers.Timer(LRunnerLeaseSeam * 1000) { AutoReset = true };
        lRunnerTimer.Elapsed += (_, _) =>
            lRunnerLeaseSchedule.LScheduleLeaseUpdate(lWorkItem.LWorkId, lRunnerLeaseId);
        lRunnerTimer.Start();
        lRunnerLeaseTimers[lWorkItem.LWorkId] = lRunnerTimer;
    }

    internal void LRunnerLeaseStop(Guid lWorkId)
    {
        if (!lRunnerLeaseTimers.TryRemove(lWorkId, out System.Timers.Timer? lRunnerTimer))
        {
            return;
        }

        lRunnerTimer.Stop();
        lRunnerTimer.Dispose();
    }

    internal void LRunnerLeaseClear()
    {
        foreach (Guid lWorkId in lRunnerLeaseTimers.Keys.ToArray())
        {
            LRunnerLeaseStop(lWorkId);
        }
    }
}
