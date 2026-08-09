using System.Collections.Concurrent;
using System.Diagnostics;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public sealed partial class LRunner
{
    internal readonly LScheduleContract lRunnerSchedule;
    private readonly Action<Action> lRunnerPost;
    internal readonly ConcurrentDictionary<Guid, LWorkItem> lRunnerItems = new();
    internal readonly ConcurrentDictionary<Guid, Process> lRunnerProcesses = new();
    internal readonly ConcurrentDictionary<Guid, int> lRunnerAttempts = new();
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
        LSentinel.LSentinelRunnerAdd(lRunnerId);
    }

    public string LRunnerProgramPath { get; set; } = "ffmpeg";

    public int LRunnerParallelMaximum { get; set; } = 1;

    public bool LRunnerFailurePaused { get; set; }

    public bool LRunnerRetryAllowed { get; set; }

    public int LRunnerRetryMaximum { get; set; } = 3;

    public static Action<string, Exception?>? LRunnerReport { get; set; }

    public static Action<string, string?>? LRunnerFfmpegReport { get; set; }

    public static Func<bool>? LRunnerVerboseSource { get; set; }

    internal static void LRunnerRecord(string lRunnerMessage, Exception? lRunnerException = null)
        => LRunnerReport?.Invoke(lRunnerMessage, lRunnerException);

    internal static bool LRunnerVerboseCheck() => LRunnerVerboseSource?.Invoke() ?? false;

    internal static void LRunnerFfmpegRecord(string lRunnerSummary, string? lRunnerDetail = null)
        => LRunnerFfmpegReport?.Invoke(lRunnerSummary, lRunnerDetail);

    public bool LRunnerSuspended => lRunnerSuspended;

    internal async Task LRunnerResumeAwait(CancellationToken lRunnerToken)
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

            if (lRunnerSuspended)
            {
                LRunnerProcessResume();
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

            if (!lRunnerSuspended)
            {
                bool lRunnerLiveExisted = false;
                bool lRunnerSuspendedAny = false;
                foreach (KeyValuePair<Guid, Process> lRunnerEntry in lRunnerProcesses)
                {
                    Process lRunnerProcess = lRunnerEntry.Value;
                    if (lRunnerProcess.HasExited)
                    {
                        continue;
                    }

                    lRunnerLiveExisted = true;
                    if (!LRunnerProcessSuspend(lRunnerProcess))
                    {
                        continue;
                    }

                    lRunnerSuspendedAny = true;
                    lRunnerItems.TryGetValue(lRunnerEntry.Key, out LWorkItem? lRunnerItem);
                    LRunnerMessageSet(lRunnerItem, "Suspended");
                }

                if (lRunnerSuspendedAny || !lRunnerLiveExisted)
                {
                    lRunnerSuspended = true;
                }
                else
                {
                    LRunnerRecord("Pause failed: no running process could be suspended; the queue keeps encoding");
                }
            }
        }

        lRunnerSchedule.LScheduleChangeRaise();
    }

    public void LRunnerCancel()
    {
        lock (lRunnerGate)
        {
            LRunnerRunning = false;
            lRunnerBatch?.LRunnerBatchSource.Cancel();
            lRunnerBatch = null;

            if (lRunnerSuspended)
            {
                LRunnerProcessResume();
            }

            foreach (Process lRunnerProcess in lRunnerProcesses.Values)
            {
                LRunnerProcessKill(lRunnerProcess);
            }

            lRunnerSuspended = false;
            LRunnerLeaseClear();
            foreach (LWorkItem lRunnerItem in lRunnerItems.Values)
            {
                LRunnerPartialRemove(lRunnerItem);
            }
        }

        lRunnerSchedule.LScheduleRelease(lRunnerId);
    }

    public void LRunnerJobCancel(Guid lWorkId)
    {
        lRunnerCancelled[lWorkId] = 0;
        if (lRunnerProcesses.TryGetValue(lWorkId, out Process? lRunnerProcess))
        {
            LRunnerProcessKill(lRunnerProcess);
        }
    }

    internal void LRunnerProcessAttach(Guid lWorkId, Process lRunnerProcess, CancellationToken lRunnerToken)
    {
        lock (lRunnerGate)
        {
            lRunnerProcesses[lWorkId] = lRunnerProcess;
            if (lRunnerToken.IsCancellationRequested || lRunnerCancelled.ContainsKey(lWorkId))
            {
                LRunnerProcessKill(lRunnerProcess);
            }
            else if (lRunnerSuspended && !lRunnerProcess.HasExited && LRunnerProcessSuspend(lRunnerProcess))
            {
                lRunnerItems.TryGetValue(lWorkId, out LWorkItem? lRunnerItem);
                LRunnerMessageSet(lRunnerItem, "Suspended");
            }
        }
    }

    private static void LRunnerProcessKill(Process lRunnerProcess)
    {
        try
        {
            if (!lRunnerProcess.HasExited)
            {
                lRunnerProcess.Kill(true);
            }
        }
        catch (Exception lRunnerException)
            when (lRunnerException is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
        }
    }

    private void LRunnerBatchStart()
    {
        lRunnerBatch ??= new LRunnerBatch();
        LRunnerBatch lRunnerActive = lRunnerBatch;
        CancellationToken lRunnerToken = lRunnerActive.LRunnerBatchSource.Token;
        int lRunnerWanted = Math.Max(1, LRunnerParallelMaximum);

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
                    if (pNext is null)
                    {
                        pPending = lRunnerSchedule.LSchedulePendingExist();
                    }
                });

                if (pNext is not null)
                {
                    await new LJob(this, pNext, lRunnerToken).LJobRun().ConfigureAwait(false);
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
            LRunnerRecord("Worker loop stopped on an unexpected error; the queue resumes on the next change", lRunnerException);
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
            }
        }

        LRunnerLeaseClear();
        LRunnerDispatch(() =>
        {
            if (lRunnerCurrent)
            {
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

    private static void LRunnerPartialRemove(LWorkItem? pWorkItem)
    {
        if (pWorkItem is null || string.IsNullOrWhiteSpace(pWorkItem.LWorkOutputPath))
        {
            return;
        }

        try
        {
            if (File.Exists(pWorkItem.LWorkOutputPath))
            {
                File.Delete(pWorkItem.LWorkOutputPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
