using System.Diagnostics;
using System.IO;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed partial class LRunner
{
    private readonly LSchedule lRunnerSchedule;
    private readonly Action<Action> lRunnerPost;

    private Process? lRunnerProcess;
    private LWorkItem? lRunnerItem;
    private CancellationTokenSource? lRunnerCancel;
    private bool lRunnerSuspended;
    private bool lRunnerLooping;

    public LRunner(LSchedule lSchedule, Action<Action> lRunnerPostAction)
    {
        lRunnerSchedule = lSchedule;
        lRunnerPost = lRunnerPostAction;
    }

    public string LRunnerProgramPath { get; set; } = "ffmpeg";

    public static Action<string, Exception?>? LRunnerReport { get; set; }

    private static void LRunnerNote(string lRunnerMessage, Exception? lRunnerException = null)
        => LRunnerReport?.Invoke(lRunnerMessage, lRunnerException);

    public bool LRunnerSuspended => lRunnerSuspended;

    public void LRunnerStart()
    {
        lRunnerSchedule.LScheduleStart();

        if (lRunnerSuspended)
        {
            LRunnerProcessResume();
            return;
        }

        LRunnerLoopStart();
    }

    public void LRunnerPause()
    {
        lRunnerSchedule.LSchedulePause();

        Process? pProcess = lRunnerProcess;
        if (pProcess is null || pProcess.HasExited || lRunnerSuspended)
        {
            return;
        }

        if (LRunnerProcessSuspend(pProcess))
        {
            lRunnerSuspended = true;
            LRunnerMessageSet(lRunnerItem, "Suspended");
        }
    }

    public void LRunnerCancel()
    {
        lRunnerCancel?.Cancel();

        Process? pProcess = lRunnerProcess;
        LWorkItem? pItem = lRunnerItem;
        if (pProcess is not null && !pProcess.HasExited)
        {
            if (lRunnerSuspended)
            {
                LRunnerProcessResume();
            }

            try
            {
                pProcess.Kill(true);
            }
            catch (InvalidOperationException)
            {
            }
        }

        lRunnerSuspended = false;
        LRunnerPartialRemove(pItem);
        lRunnerSchedule.LScheduleCancel();
    }

    private void LRunnerLoopStart()
    {
        if (lRunnerLooping)
        {
            return;
        }

        lRunnerLooping = true;
        lRunnerCancel = new CancellationTokenSource();
        _ = Task.Run(() => LRunnerLoopRun(lRunnerCancel.Token));
    }

    private async Task LRunnerLoopRun(CancellationToken lRunnerToken)
    {
        try
        {
            while (!lRunnerToken.IsCancellationRequested && lRunnerSchedule.LScheduleRunning)
            {
                LWorkItem? pNext = null;
                LRunnerInvoke(() => pNext = lRunnerSchedule.LScheduleClaim());
                if (pNext is null)
                {
                    break;
                }

                await LRunnerItemRun(pNext, lRunnerToken).ConfigureAwait(false);
            }
        }
        finally
        {
            lRunnerLooping = false;
            lRunnerItem = null;
            lRunnerProcess = null;
            LRunnerInvoke(() =>
            {
                if (!lRunnerSchedule.LSchedulePendingExist())
                {
                    lRunnerSchedule.LSchedulePause();
                }
            });
        }
    }

    private async Task LRunnerItemRun(LWorkItem pWorkItem, CancellationToken lRunnerToken)
    {
        lRunnerItem = pWorkItem;
        LRunnerInvoke(() =>
        {
            pWorkItem.LWorkStateCurrent = LWorkState.LWorkStateRunning;
            pWorkItem.LWorkProgress = 0;
            pWorkItem.LWorkMessage = string.Empty;
        });

        string pDirectory = Path.GetDirectoryName(pWorkItem.LWorkOutputPath) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(pDirectory))
        {
            Directory.CreateDirectory(pDirectory);
        }

        var pStartInfo = new ProcessStartInfo
        {
            FileName = LRunnerProgramPath,
            Arguments = LEncode.LEncodeArgumentBuild(pWorkItem),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var pRunnerClock = System.Diagnostics.Stopwatch.StartNew();
        LRunnerNote(
            $"Encode started '{pWorkItem.LWorkOutputName}': {pWorkItem.LWorkKind} at {pWorkItem.LWorkPriority}, " +
            $"{pWorkItem.LWorkStart:hh\\:mm\\:ss\\.fff}-{pWorkItem.LWorkEnd:hh\\:mm\\:ss\\.fff} " +
            $"from '{Path.GetFileName(pWorkItem.LWorkSourcePath)}' to '{pWorkItem.LWorkOutputPath}'");
        LRunnerNote($"Encode command '{pWorkItem.LWorkOutputName}': {pStartInfo.FileName} {pStartInfo.Arguments}");

        try
        {
            using var pProcess = new Process { StartInfo = pStartInfo };
            pProcess.Start();
            lRunnerProcess = pProcess;

            Task<string> pErrorTask = pProcess.StandardError.ReadToEndAsync(lRunnerToken);
            await LRunnerProgressRead(pProcess, pWorkItem, lRunnerToken).ConfigureAwait(false);
            await pProcess.WaitForExitAsync(lRunnerToken).ConfigureAwait(false);
            string pRunnerError = await pErrorTask.ConfigureAwait(false);

            int pExitCode = pProcess.ExitCode;
            if (pExitCode == 0)
            {
                LRunnerNote(
                    $"Encode finished '{pWorkItem.LWorkOutputName}' in {pRunnerClock.Elapsed:hh\\:mm\\:ss\\.fff} " +
                    $"[{pWorkItem.LWorkOutputPath}]");
            }
            else
            {
                LRunnerNote(
                    $"Encode failed '{pWorkItem.LWorkOutputName}': FFmpeg exit code {pExitCode} " +
                    $"after {pRunnerClock.Elapsed:hh\\:mm\\:ss\\.fff}. {LRunnerTailRead(pRunnerError)}");
            }

            LRunnerInvoke(() =>
            {
                bool pSucceeded = pExitCode == 0;
                pWorkItem.LWorkProgress = pSucceeded ? 1 : pWorkItem.LWorkProgress;
                pWorkItem.LWorkStateCurrent = pSucceeded ? LWorkState.LWorkStateDone : LWorkState.LWorkStateFailed;
                pWorkItem.LWorkMessage = pSucceeded ? string.Empty : $"FFmpeg exited with code {pExitCode}.";

                lRunnerSchedule.LScheduleComplete(pWorkItem, pSucceeded, pWorkItem.LWorkMessage);
                lRunnerSchedule.LScheduleReload();
            });
        }
        catch (OperationCanceledException)
        {
            LRunnerNote($"Encode cancelled '{pWorkItem.LWorkOutputName}' after {pRunnerClock.Elapsed:hh\\:mm\\:ss\\.fff}; returned to the queue");
        }
        catch (Exception pException)
        {
            LRunnerNote($"Encode failed '{pWorkItem.LWorkOutputName}' after {pRunnerClock.Elapsed:hh\\:mm\\:ss\\.fff}", pException);
            LRunnerInvoke(() =>
            {
                pWorkItem.LWorkStateCurrent = LWorkState.LWorkStateFailed;
                pWorkItem.LWorkMessage = pException.Message;
                lRunnerSchedule.LScheduleComplete(pWorkItem, false, pException.Message);
                lRunnerSchedule.LScheduleReload();
            });
        }
        finally
        {
            lRunnerProcess = null;
        }
    }

    private static string LRunnerTailRead(string lRunnerError)
    {
        if (string.IsNullOrWhiteSpace(lRunnerError))
        {
            return "FFmpeg reported nothing.";
        }

        string[] lRunnerLines = lRunnerError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(" | ", lRunnerLines[^Math.Min(3, lRunnerLines.Length)..]);
    }
}
