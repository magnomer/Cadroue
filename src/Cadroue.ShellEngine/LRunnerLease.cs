using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed partial class LRunner
{
    private readonly Guid lRunnerId = Guid.NewGuid();
    private System.Timers.Timer? lRunnerLeaseTimer;

    public Guid LRunnerIdentity => lRunnerId;

    public bool LRunnerOwnerCheck(LWorkItem lWorkItem) =>
        lRunnerSchedule.LScheduleOwnerCheck(lWorkItem, lRunnerId);

    public bool LRunnerForeignCheck(LWorkItem lWorkItem) =>
        lRunnerSchedule.LScheduleForeignCheck(lWorkItem, lRunnerId);

    public void LRunnerDispose()
    {
        if (LRunnerRunning || lRunnerItem is not null)
        {
            LRunnerNote($"Worklist tab closed: releasing work held by runner {lRunnerId:N}");
            LRunnerCancel();
        }

        LRunnerRunning = false;
        LRunnerLeaseStop();
        LSchedule.LScheduleRunnerRemove(lRunnerId);
    }

    private void LRunnerLeaseStart(LWorkItem lWorkItem)
    {
        LRunnerLeaseStop();
        var lRunnerTimer = new System.Timers.Timer(LSchedule.LScheduleLeaseSeconds * 1000) { AutoReset = true };
        lRunnerTimer.Elapsed += (_, _) => lRunnerSchedule.LScheduleLeaseUpdate(lWorkItem.LWorkId, lRunnerId);
        lRunnerTimer.Start();
        lRunnerLeaseTimer = lRunnerTimer;
    }

    private void LRunnerLeaseStop()
    {
        System.Timers.Timer? lRunnerTimer = lRunnerLeaseTimer;
        lRunnerLeaseTimer = null;
        if (lRunnerTimer is null)
        {
            return;
        }

        lRunnerTimer.Stop();
        lRunnerTimer.Dispose();
    }
}
