using System.Collections.Concurrent;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public sealed partial class LRunner
{
    private readonly Guid lRunnerId = Guid.NewGuid();
    private readonly ConcurrentDictionary<Guid, System.Timers.Timer> lRunnerLeaseTimers = new();

    public Guid LRunnerIdentity => lRunnerId;

    public bool LRunnerOwnerCheck(LWorkItem lWorkItem) =>
        lRunnerSchedule.LScheduleOwnerCheck(lWorkItem, lRunnerId);

    public bool LRunnerForeignCheck(LWorkItem lWorkItem) =>
        lRunnerSchedule.LScheduleForeignCheck(lWorkItem, lRunnerId);

    public void LRunnerDispose()
    {
        if (LRunnerRunning || !lRunnerItems.IsEmpty)
        {
            LRunnerRecord($"Worklist tab closed: releasing work held by runner {lRunnerId:N}");
            LRunnerCancel();
        }

        LRunnerRunning = false;
        LRunnerLeaseClear();
        LSchedule.LScheduleRunnerRemove(lRunnerId);
    }

    public void LRunnerPhaseSet(LWorkItem lWorkItem, LWorkPhase lRunnerPhase)
    {
        if (lWorkItem.LWorkPhaseCurrent == lRunnerPhase)
        {
            return;
        }

        lWorkItem.LWorkPhaseCurrent = lRunnerPhase;
        lRunnerSchedule.LSchedulePhaseSet(lWorkItem.LWorkId, lRunnerId, lRunnerPhase);
    }

    internal void LRunnerLeaseStart(LWorkItem lWorkItem)
    {
        LRunnerLeaseStop(lWorkItem.LWorkId);
        var lRunnerTimer = new System.Timers.Timer(LSchedule.LScheduleLeaseSeconds * 1000) { AutoReset = true };
        lRunnerTimer.Elapsed += (_, _) => lRunnerSchedule.LScheduleLeaseUpdate(lWorkItem.LWorkId, lRunnerId);
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

    private void LRunnerLeaseClear()
    {
        foreach (Guid lWorkId in lRunnerLeaseTimers.Keys.ToArray())
        {
            LRunnerLeaseStop(lWorkId);
        }
    }
}
