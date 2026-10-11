using System.Collections.Concurrent;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed partial class LRunner
{
    private readonly Guid lRunnerId = Guid.NewGuid();
    private readonly ConcurrentDictionary<Guid, System.Timers.Timer> lRunnerLeaseTimers = new();

    public static Action<Guid> LRunnerAddSeam { get; set; } = _ => { };

    public static Action<Guid> LRunnerRemoveSeam { get; set; } = _ => { };

    public static int LRunnerLeaseSeam { get; set; } = 10;

    public Guid LRunnerIdentity => lRunnerId;

    public bool LRunnerOwnerCheck(LWorkItem lWorkItem) =>
        lRunnerSchedule.LScheduleOwnerCheck(lWorkItem, lRunnerId);

    public bool LRunnerForeignCheck(LWorkItem lWorkItem) =>
        lRunnerSchedule.LScheduleForeignCheck(lWorkItem, lRunnerId);

    public async Task LRunnerDispose()
    {
        if (LRunnerRunning || !lRunnerJobs.IsEmpty)
        {
            LRunnerRecord($"Worklist tab closed: releasing work held by runner {lRunnerId:N}");
            await LRunnerCancel().ConfigureAwait(false);
        }

        LRunnerRunning = false;
        LRunnerLeaseClear();
        LRunnerRemoveSeam(lRunnerId);
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
        var lRunnerTimer = new System.Timers.Timer(LRunnerLeaseSeam * 1000) { AutoReset = true };
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
