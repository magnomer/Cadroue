using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

/// <summary>
/// Runs the schedule one job at a time, highest priority first.
///
/// Pause genuinely suspends the running FFmpeg process rather than waiting for it to
/// finish or killing it: every thread in the child is frozen and resumed later, so the
/// encode continues from exactly where it stopped and no partial file is discarded.
///
/// State is written back through <see cref="LRunnerPost"/> so the single-threaded
/// <see cref="LSchedule"/> is only ever touched on the owning (UI) thread.
/// </summary>
public sealed class LRunner
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

    /// <summary>Executable used to encode. Overridable for a custom FFmpeg build.</summary>
    public string LRunnerProgramPath { get; set; } = "ffmpeg";

    public bool LRunnerSuspended => lRunnerSuspended;

    /// <summary>Begin, or resume a suspended job.</summary>
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

    /// <summary>
    /// Freeze the running encode in place. The process keeps its handles and memory, so
    /// resuming continues the same encode rather than restarting it.
    /// </summary>
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

    /// <summary>Stop the queue, kill the current encode and drop its partial file.</summary>
    public void LRunnerCancel()
    {
        lRunnerCancel?.Cancel();

        Process? pProcess = lRunnerProcess;
        LWorkItem? pItem = lRunnerItem;
        if (pProcess is not null && !pProcess.HasExited)
        {
            // A suspended process cannot die until it is running again.
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
                // Already gone between the check and the kill.
            }
        }

        lRunnerSuspended = false;
        LRunnerPartialRemove(pItem);
        lRunnerSchedule.LScheduleCancel();
    }

    private void LRunnerProcessResume()
    {
        Process? pProcess = lRunnerProcess;
        if (pProcess is null || pProcess.HasExited)
        {
            lRunnerSuspended = false;
            return;
        }

        if (LRunnerProcessResume(pProcess))
        {
            lRunnerSuspended = false;
            LRunnerMessageSet(lRunnerItem, string.Empty);
        }
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
                LRunnerInvoke(() => pNext = lRunnerSchedule.LScheduleNextRead());
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
                if (lRunnerSchedule.LScheduleNextRead() is null)
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

        try
        {
            using var pProcess = new Process { StartInfo = pStartInfo };
            pProcess.Start();
            lRunnerProcess = pProcess;

            Task pErrorTask = pProcess.StandardError.ReadToEndAsync(lRunnerToken);
            await LRunnerProgressRead(pProcess, pWorkItem, lRunnerToken).ConfigureAwait(false);
            await pProcess.WaitForExitAsync(lRunnerToken).ConfigureAwait(false);
            await pErrorTask.ConfigureAwait(false);

            int pExitCode = pProcess.ExitCode;
            LRunnerInvoke(() =>
            {
                if (pExitCode == 0)
                {
                    pWorkItem.LWorkProgress = 1;
                    pWorkItem.LWorkStateCurrent = LWorkState.LWorkStateDone;
                    return;
                }

                pWorkItem.LWorkStateCurrent = LWorkState.LWorkStateFailed;
                pWorkItem.LWorkMessage = $"FFmpeg exited with code {pExitCode}.";
            });
        }
        catch (OperationCanceledException)
        {
            // LRunnerCancel already moved the item to Cancelled.
        }
        catch (Exception pException)
        {
            LRunnerInvoke(() =>
            {
                pWorkItem.LWorkStateCurrent = LWorkState.LWorkStateFailed;
                pWorkItem.LWorkMessage = pException.Message;
            });
        }
        finally
        {
            lRunnerProcess = null;
        }
    }

    /// <summary>
    /// Follow "-progress pipe:1". FFmpeg writes one block of key=value lines per
    /// -stats_period (forced to <see cref="LEncode.LEncodeStatsPeriod"/> seconds) and
    /// closes each block with a "progress=" line. The position is collected while the
    /// block streams in and published once the block closes, so the UI gets exactly one
    /// coherent update per report instead of a partial one per line.
    /// </summary>
    private async Task LRunnerProgressRead(Process pProcess, LWorkItem pWorkItem, CancellationToken lRunnerToken)
    {
        double pTotalSeconds = pWorkItem.LWorkDuration.TotalSeconds;
        long pBlockMicroseconds = -1;

        while (await pProcess.StandardOutput.ReadLineAsync(lRunnerToken).ConfigureAwait(false) is string pLine)
        {
            int pSeparator = pLine.IndexOf('=');
            if (pSeparator <= 0)
            {
                continue;
            }

            string pKey = pLine[..pSeparator];
            string pValue = pLine[(pSeparator + 1)..].Trim();

            switch (pKey)
            {
                // Both keys carry microseconds; older builds only emit out_time_ms and
                // still write a microsecond value into it. "N/A" appears before the
                // first frame lands and simply fails to parse.
                case "out_time_us":
                case "out_time_ms":
                    if (long.TryParse(pValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long pParsed))
                    {
                        pBlockMicroseconds = pParsed;
                    }

                    break;

                case "progress":
                    if (pBlockMicroseconds >= 0 && pTotalSeconds > 0)
                    {
                        double pFraction = pBlockMicroseconds / 1_000_000d / pTotalSeconds;
                        LRunnerInvoke(() => pWorkItem.LWorkProgress = pFraction);
                    }

                    if (string.Equals(pValue, "end", StringComparison.Ordinal))
                    {
                        LRunnerInvoke(() => pWorkItem.LWorkProgress = 1);
                    }

                    pBlockMicroseconds = -1;
                    break;
            }
        }
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
            // The file is still locked by the dying process; leaving it is better than throwing.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void LRunnerMessageSet(LWorkItem? pWorkItem, string pMessage)
    {
        if (pWorkItem is null)
        {
            return;
        }

        LRunnerInvoke(() => pWorkItem.LWorkMessage = pMessage);
    }

    private void LRunnerInvoke(Action pAction) => lRunnerPost(pAction);

    // ---- Win32 process suspend/resume -------------------------------------
    // NtSuspendProcess/NtResumeProcess freeze every thread in the child. This is how a
    // real pause is achieved: SIGSTOP has no Windows equivalent, and killing the encoder
    // would throw away the partial output.

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtSuspendProcess(IntPtr lRunnerProcessHandle);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtResumeProcess(IntPtr lRunnerProcessHandle);

    private static bool LRunnerProcessSuspend(Process pProcess)
    {
        try
        {
            return NtSuspendProcess(pProcess.Handle) == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool LRunnerProcessResume(Process pProcess)
    {
        try
        {
            return NtResumeProcess(pProcess.Handle) == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
