using System.Collections.Concurrent;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed partial class LRunner
{
    internal readonly LScheduleContract lRunnerSchedule;
    private readonly Action<Action> lRunnerPost;
    internal readonly ConcurrentDictionary<Guid, LJob> lRunnerJobs = new();
    internal readonly ConcurrentDictionary<Guid, byte> lRunnerProcesses = new();
    internal readonly ConcurrentDictionary<Guid, byte> lRunnerCancelled = new();

    private readonly object lRunnerGate = new();
    private LRunnerBatch? lRunnerBatch;
    private bool lRunnerSuspended;
    private volatile bool lRunnerRunning;

    private static readonly TimeSpan lRunnerPollInterval = TimeSpan.FromMilliseconds(500);

    private sealed class LRunnerBatch
    {
        public readonly CancellationTokenSource LRunnerBatchSource = new();
        public int LRunnerBatchActive;
    }

    public LRunner(LScheduleContract lSchedule, Action<Action> lRunnerPostAction)
    {
        lRunnerSchedule = lSchedule;
        lRunnerPost = lRunnerPostAction;
        LRunnerAddSeam(lRunnerId);
    }

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

    public bool LRunnerSuspended => lRunnerSuspended;

    public bool LRunnerPaused { get; private set; }

    internal async Task LRunnerResume(CancellationToken lRunnerToken)
    {
        while (lRunnerSuspended)
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

            if (lRunnerSuspended)
            {
                LRunnerProcessResume();
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

            if (!lRunnerSuspended)
            {
                bool lRunnerLiveExisted = false;
                bool lRunnerSuspendedAny = false;
                var lRunnerHeld = new List<Guid>();
                foreach (KeyValuePair<Guid, byte> lRunnerEntry in lRunnerProcesses)
                {
                    Guid lWorkId = lRunnerEntry.Key;
                    bool lRunnerLive = LEmployerEngine.LEmployerLiveSeam(lWorkId);
                    lRunnerLiveExisted |= lRunnerLive;
                    if (!LEmployerEngine.LEmployerSuspendSeam(lWorkId))
                    {
                        continue;
                    }

                    lRunnerSuspendedAny |= lRunnerLive;
                    lRunnerHeld.Add(lWorkId);
                }

                if (lRunnerSuspendedAny || !lRunnerLiveExisted)
                {
                    lRunnerSuspended = true;
                    foreach (Guid lWorkId in lRunnerHeld)
                    {
                        LRunnerMessageSet(LRunnerItemRead(lWorkId), "Suspended");
                    }
                }
                else
                {
                    foreach (Guid lWorkId in lRunnerHeld)
                    {
                        LEmployerEngine.LEmployerResumeSeam(lWorkId);
                    }

                    LRunnerRecord("Pause failed: no running process could be suspended; the queue keeps encoding");
                }
            }
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

            if (lRunnerSuspended)
            {
                LRunnerProcessResume();
            }

            lRunnerSuspended = false;
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

    internal void LRunnerProcessAttach(Guid lWorkId)
    {
        lock (lRunnerGate)
        {
            lRunnerProcesses[lWorkId] = 0;
            if (lRunnerSuspended && LEmployerEngine.LEmployerSuspendSeam(lWorkId))
            {
                LRunnerMessageSet(LRunnerItemRead(lWorkId), "Suspended");
            }
        }
    }

    internal void LRunnerProcessDetach(Guid lWorkId)
    {
        lock (lRunnerGate)
        {
            if (lRunnerProcesses.TryRemove(lWorkId, out _))
            {
                LEmployerEngine.LEmployerResumeSeam(lWorkId);
            }
        }
    }

    private void LRunnerProcessResume()
    {
        bool lRunnerAllResumed = true;
        foreach (KeyValuePair<Guid, byte> lRunnerEntry in lRunnerProcesses)
        {
            Guid lWorkId = lRunnerEntry.Key;
            if (!LEmployerEngine.LEmployerResumeSeam(lWorkId))
            {
                lRunnerAllResumed = false;
                continue;
            }

            LRunnerMessageSet(LRunnerItemRead(lWorkId), string.Empty);
        }

        if (lRunnerAllResumed)
        {
            lRunnerSuspended = false;
        }
        else
        {
            LRunnerRecord(
                "Resume failed: a live process stayed suspended; " +
                "the paused flag is kept so the next Play retries the resume");
        }
    }

    private LWorkItem? LRunnerItemRead(Guid lWorkId) =>
        lRunnerJobs.TryGetValue(lWorkId, out LJob? lRunnerJob) ? lRunnerJob.LJobItem : null;

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
                    pNext = lRunnerSchedule.LScheduleClaim(lRunnerId);
                    if (pNext is not null)
                    {
                        lRunnerSchedule.LScheduleLoad();
                    }
                    else
                    {
                        pPending = lRunnerSchedule.LSchedulePendingExist();
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
                lRunnerSuspended = false;
            }
        }

        LRunnerDispatch(() =>
        {
            if (lRunnerCurrent)
            {
                lRunnerSchedule.LScheduleRelease(lRunnerId);
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

    private void LRunnerMessageSet(LWorkItem? pWorkItem, string pMessage)
    {
        if (pWorkItem is null)
        {
            return;
        }

        LRunnerDispatch(() =>
        {
            pWorkItem.LWorkMessage = pMessage;
            lRunnerSchedule.LScheduleItemRaise(pWorkItem, LScheduleNotice.LScheduleNoticeStatus);
        });
    }
}
