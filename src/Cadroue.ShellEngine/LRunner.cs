using System.Collections.Concurrent;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed class LRunner
{
    internal readonly LScheduleContract lRunnerSchedule;
    internal readonly LScheduleIntakeContract lRunnerIntake;
    internal readonly LScheduleLeaseContract lRunnerScheduleLease;
    internal readonly LScheduleTransitionContract lRunnerTransition;
    private readonly Action<Action> lRunnerPost;
    internal readonly ConcurrentDictionary<Guid, LJob> lRunnerJobs = new();
    private readonly ConcurrentDictionary<Guid, byte> lRunnerCancelled = new();

    private readonly Guid lRunnerId = Guid.NewGuid();
    private readonly object lRunnerGate = new();
    private LRunnerBatch? lRunnerBatch;
    private volatile bool lRunnerRunning;

    private static readonly TimeSpan lRunnerPollInterval = TimeSpan.FromMilliseconds(500);

    public LRunner(
        LScheduleContract lSchedule,
        LScheduleIntakeContract lIntake,
        LScheduleLeaseContract lLease,
        LScheduleTransitionContract lTransition,
        Action<Action> lRunnerPostAction)
    {
        lRunnerSchedule = lSchedule;
        lRunnerIntake = lIntake;
        lRunnerScheduleLease = lLease;
        lRunnerTransition = lTransition;
        lRunnerPost = lRunnerPostAction;
        LRunnerLease = new LRunnerLease(lLease, lRunnerId);
        LRunnerProcess = new LRunnerProcess(lRunnerGate, lSchedule, lRunnerPostAction);
        LRunnerAddSeam(lRunnerId);
    }

    public static Action<Guid> LRunnerAddSeam { get; set; } = _ => { };

    public static Action<Guid> LRunnerRemoveSeam { get; set; } = _ => { };

    internal LRunnerLease LRunnerLease { get; }

    public LRunnerProcess LRunnerProcess { get; }

    public string LRunnerProgramPath { get; set; } = "ffmpeg";

    public int? LRunnerLevel { get; set; }

    public string LRunnerArgumentPrefix { get; set; } = string.Empty;

    public Func<string, string>? LRunnerArgumentTransform { get; set; }

    public bool LRunnerFailurePaused { get; set; }

    public bool LRunnerRetryAllowed { get; set; }

    public int LRunnerRetryMaximum { get; set; } = 3;

    internal static void LRunnerRecord(string lRunnerMessage, Exception? lRunnerException = null)
    {
        if (lRunnerException is null)
        {
            LTraceEngine.LTraceInfoSeam(lRunnerMessage);
            return;
        }

        LTraceEngine.LTraceErrorSeam(lRunnerMessage, lRunnerException);
    }

    internal static bool LRunnerVerboseCheck() => LTraceEngine.LTraceVerboseSeam();

    internal static void LRunnerFfmpegRecord(string lRunnerSummary, string? lRunnerDetail = null)
        => LTraceEngine.LTraceFfmpegSeam(lRunnerSummary, lRunnerDetail);

    public Guid LRunnerIdentity => lRunnerId;

    public bool LRunnerOwnerCheck(LWorkItem lWorkItem) =>
        lRunnerScheduleLease.LScheduleOwnerCheck(lWorkItem, lRunnerId);

    public bool LRunnerForeignCheck(LWorkItem lWorkItem) =>
        lRunnerScheduleLease.LScheduleForeignCheck(lWorkItem, lRunnerId);

    public bool LRunnerPaused { get; private set; }

    internal async Task LRunnerResume(CancellationToken lRunnerToken)
    {
        while (LRunnerProcess.LRunnerProcessSuspended)
        {
            lRunnerToken.ThrowIfCancellationRequested();
            await Task.Delay(lRunnerPollInterval, lRunnerToken).ConfigureAwait(false);
        }
    }

    public bool LRunnerRunning
    {
        get => lRunnerRunning;
        private set => lRunnerRunning = value;
    }

    public void LRunnerStart()
    {
        lock (lRunnerGate)
        {
            LRunnerRunning = true;
            LRunnerPaused = false;

            if (LRunnerProcess.LRunnerProcessSuspended)
            {
                LRunnerProcess.LRunnerProcessResume();
                if (lRunnerBatch is null)
                {
                    LRunnerBatchStart();
                }
            }
            else
            {
                LRunnerBatchStart();
            }
        }

        lRunnerSchedule.LScheduleChangeRaise();
    }

    public void LRunnerPause()
    {
        lock (lRunnerGate)
        {
            LRunnerRunning = false;
            LRunnerPaused = true;
            LRunnerProcess.LRunnerProcessSuspend();
        }

        lRunnerSchedule.LScheduleChangeRaise();
    }

    public Task LRunnerCancel()
    {
        Task[] lRunnerDraining;
        lock (lRunnerGate)
        {
            LRunnerRunning = false;
            LRunnerPaused = false;
            lRunnerBatch?.LRunnerBatchSource.Cancel();
            lRunnerBatch = null;

            if (LRunnerProcess.LRunnerProcessSuspended)
            {
                LRunnerProcess.LRunnerProcessResume();
            }

            LRunnerProcess.LRunnerProcessReset();
            lRunnerDraining = lRunnerJobs.Values.Select(lRunnerJob => lRunnerJob.LJobCompletion).ToArray();
        }

        lRunnerSchedule.LScheduleChangeRaise();
        return Task.WhenAll(lRunnerDraining);
    }

    public void LRunnerJobCancel(Guid lWorkId)
    {
        if (lRunnerJobs.TryGetValue(lWorkId, out LJob? lRunnerJob))
        {
            lRunnerJob.LJobCancel();
            return;
        }

        lRunnerCancelled[lWorkId] = 0;
    }

    internal bool LRunnerPendingRemove(Guid lWorkId) => lRunnerCancelled.TryRemove(lWorkId, out _);

    public async Task LRunnerDispose()
    {
        if (LRunnerRunning || !lRunnerJobs.IsEmpty)
        {
            LRunnerRecord($"Worklist tab closed: releasing work held by runner {lRunnerId:N}");
            await LRunnerCancel().ConfigureAwait(false);
        }

        LRunnerRunning = false;
        LRunnerLease.LRunnerLeaseClear();
        LRunnerRemoveSeam(lRunnerId);
    }

    public void LRunnerPhaseSet(LWorkItem lWorkItem, LWorkPhase lRunnerPhase)
    {
        if (lWorkItem.LWorkRunState.LWorkPhaseCurrent == lRunnerPhase)
        {
            return;
        }

        lWorkItem.LWorkRunState.LWorkPhaseCurrent = lRunnerPhase;
        lRunnerScheduleLease.LSchedulePhaseSet(lWorkItem.LWorkId, lRunnerId, lRunnerPhase);
    }

    private void LRunnerBatchStart()
    {
        lRunnerBatch ??= new LRunnerBatch();
        LRunnerBatch lRunnerActive = lRunnerBatch;
        CancellationToken lRunnerToken = lRunnerActive.LRunnerBatchSource.Token;
        const int lRunnerWanted = 1;

        while (lRunnerActive.LRunnerBatchActive < lRunnerWanted)
        {
            lRunnerActive.LRunnerBatchActive++;
            _ = Task.Run(() => LRunnerLoopRun(lRunnerActive, lRunnerToken));
        }
    }

    private async Task LRunnerLoopRun(LRunnerBatch lRunnerActive, CancellationToken lRunnerToken)
    {
        try
        {
            while (!lRunnerToken.IsCancellationRequested && LRunnerRunning)
            {
                LWorkItem? pNext = null;
                bool pPending = false;
                LRunnerDispatch(() =>
                {
                    pNext = lRunnerScheduleLease.LScheduleClaim(lRunnerId);
                    if (pNext is not null)
                    {
                        lRunnerSchedule.LScheduleLoad();
                    }
                    else
                    {
                        pPending = lRunnerScheduleLease.LSchedulePendingExist();
                    }
                });

                if (pNext is not null)
                {
                    var pJob = new LJob(this, pNext, lRunnerToken);
                    lRunnerJobs[pNext.LWorkId] = pJob;
                    try
                    {
                        await pJob.LJobStart().ConfigureAwait(false);
                    }
                    finally
                    {
                        lRunnerJobs.TryRemove(pNext.LWorkId, out _);
                    }

                    continue;
                }

                if (!pPending || !LRunnerRunning || lRunnerToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await Task.Delay(lRunnerPollInterval, lRunnerToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception lRunnerException)
        {
            LRunnerRecord(
                "Worker loop stopped on an unexpected error; the queue resumes on the next change",
                lRunnerException);
        }
        finally
        {
            LRunnerBatchStop(lRunnerActive);
        }
    }

    private void LRunnerBatchStop(LRunnerBatch lRunnerActive)
    {
        bool lRunnerCurrent;
        lock (lRunnerGate)
        {
            if (--lRunnerActive.LRunnerBatchActive != 0)
            {
                return;
            }

            lRunnerActive.LRunnerBatchSource.Dispose();
            lRunnerCurrent = ReferenceEquals(lRunnerBatch, lRunnerActive);
            if (lRunnerCurrent)
            {
                lRunnerBatch = null;
                LRunnerProcess.LRunnerProcessReset();
            }
        }

        LRunnerDispatch(() =>
        {
            if (lRunnerCurrent)
            {
                lRunnerScheduleLease.LScheduleRelease(lRunnerId);
                LRunnerRunning = false;
            }

            lRunnerSchedule.LScheduleChangeRaise();
        });
    }

    internal void LRunnerFailureApply()
    {
        if (!LRunnerFailurePaused)
        {
            return;
        }

        LRunnerRunning = false;
        LRunnerPaused = true;
        LRunnerRecord("Queue paused: a job failed and 'Pause queue on failure' is on");
    }

    internal void LRunnerDispatch(Action pAction) => lRunnerPost(pAction);
}
