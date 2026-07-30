using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed partial class LRunner
{
    private readonly LSchedule lRunnerSchedule;
    private readonly Action<Action> lRunnerPost;
    private readonly ConcurrentDictionary<Guid, LWorkItem> lRunnerItems = new();
    private readonly ConcurrentDictionary<Guid, Process> lRunnerProcesses = new();
    private readonly ConcurrentDictionary<Guid, int> lRunnerAttempts = new();
    private readonly ConcurrentDictionary<Guid, byte> lRunnerCancelled = new();

    private CancellationTokenSource? lRunnerCancel;
    private bool lRunnerSuspended;
    private int lRunnerLoopCount;

    public LRunner(LSchedule lSchedule, Action<Action> lRunnerPostAction)
    {
        lRunnerSchedule = lSchedule;
        lRunnerPost = lRunnerPostAction;
        LSchedule.LScheduleRunnerAdd(lRunnerId);
    }

    public string LRunnerProgramPath { get; set; } = "ffmpeg";

    public int LRunnerParallelMaximum { get; set; } = 1;

    public bool LRunnerFailurePaused { get; set; }

    public bool LRunnerRetryAllowed { get; set; }

    public int LRunnerRetryMaximum { get; set; } = 3;

    public static Action<string, Exception?>? LRunnerReport { get; set; }

    public static Action<string, string?>? LRunnerFfmpegReport { get; set; }

    public static Func<bool>? LRunnerVerboseSource { get; set; }

    private static void LRunnerNote(string lRunnerMessage, Exception? lRunnerException = null)
        => LRunnerReport?.Invoke(lRunnerMessage, lRunnerException);

    private static bool LRunnerVerboseCheck() => LRunnerVerboseSource?.Invoke() ?? false;

    private static void LRunnerFfmpegNote(string lRunnerSummary, string? lRunnerDetail = null)
        => LRunnerFfmpegReport?.Invoke(lRunnerSummary, lRunnerDetail);

    public bool LRunnerSuspended => lRunnerSuspended;

    public bool LRunnerRunning { get; private set; }

    public void LRunnerStart()
    {
        LRunnerRunning = true;

        if (lRunnerSuspended)
        {
            LRunnerProcessResume();
            lRunnerSchedule.LScheduleChangeRaise();
            return;
        }

        LRunnerLoopStart();
        lRunnerSchedule.LScheduleChangeRaise();
    }

    public void LRunnerPause()
    {
        LRunnerRunning = false;

        if (!lRunnerSuspended)
        {
            bool lRunnerAnySuspended = false;
            foreach (KeyValuePair<Guid, Process> lRunnerEntry in lRunnerProcesses)
            {
                Process lRunnerProcess = lRunnerEntry.Value;
                if (lRunnerProcess.HasExited || !LRunnerProcessSuspend(lRunnerProcess))
                {
                    continue;
                }

                lRunnerAnySuspended = true;
                lRunnerItems.TryGetValue(lRunnerEntry.Key, out LWorkItem? lRunnerItem);
                LRunnerMessageSet(lRunnerItem, "Suspended");
            }

            if (lRunnerAnySuspended)
            {
                lRunnerSuspended = true;
            }
        }

        lRunnerSchedule.LScheduleChangeRaise();
    }

    public void LRunnerCancel()
    {
        LRunnerRunning = false;
        lRunnerCancel?.Cancel();

        if (lRunnerSuspended)
        {
            LRunnerProcessResume();
        }

        foreach (Process lRunnerProcess in lRunnerProcesses.Values)
        {
            if (lRunnerProcess.HasExited)
            {
                continue;
            }

            try
            {
                lRunnerProcess.Kill(true);
            }
            catch (InvalidOperationException)
            {
            }
        }

        lRunnerSuspended = false;
        LRunnerLeaseClear();
        foreach (LWorkItem lRunnerItem in lRunnerItems.Values)
        {
            LRunnerPartialRemove(lRunnerItem);
        }

        lRunnerSchedule.LScheduleRelease(lRunnerId);
    }

    public void LRunnerJobCancel(Guid lWorkId)
    {
        lRunnerCancelled[lWorkId] = 0;
        if (lRunnerProcesses.TryGetValue(lWorkId, out Process? lRunnerProcess) && !lRunnerProcess.HasExited)
        {
            try
            {
                lRunnerProcess.Kill(true);
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    private void LRunnerLoopStart()
    {
        int lRunnerWanted = Math.Max(1, LRunnerParallelMaximum);
        lRunnerCancel ??= new CancellationTokenSource();
        CancellationToken lRunnerToken = lRunnerCancel.Token;

        while (Volatile.Read(ref lRunnerLoopCount) < lRunnerWanted)
        {
            Interlocked.Increment(ref lRunnerLoopCount);
            _ = Task.Run(() => LRunnerLoopRun(lRunnerToken));
        }
    }

    private async Task LRunnerLoopRun(CancellationToken lRunnerToken)
    {
        try
        {
            while (!lRunnerToken.IsCancellationRequested && LRunnerRunning)
            {
                LWorkItem? pNext = null;
                LRunnerInvoke(() => pNext = lRunnerSchedule.LScheduleClaim(lRunnerId));
                if (pNext is null)
                {
                    break;
                }

                await LRunnerItemRun(pNext, lRunnerToken).ConfigureAwait(false);
            }
        }
        finally
        {
            if (Interlocked.Decrement(ref lRunnerLoopCount) == 0)
            {
                lRunnerCancel = null;
                LRunnerLeaseClear();
                LRunnerInvoke(() =>
                {
                    if (!lRunnerSchedule.LSchedulePendingExist())
                    {
                        LRunnerRunning = false;
                    }

                    lRunnerSchedule.LScheduleChangeRaise();
                });
            }
        }
    }

    private async Task LRunnerItemRun(LWorkItem pWorkItem, CancellationToken lRunnerToken)
    {
        lRunnerItems[pWorkItem.LWorkId] = pWorkItem;
        LRunnerLeaseStart(pWorkItem);
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

        IReadOnlyList<LEncodeStage> pStages = LEncode.LEncodeStagesBuild(pWorkItem);

        var pRunnerClock = System.Diagnostics.Stopwatch.StartNew();
        pWorkItem.LWorkStartTime = DateTimeOffset.Now;
        pWorkItem.LWorkFinishTime = null;
        LRunnerNote(
            $"Encode started '{pWorkItem.LWorkOutputName}': {pWorkItem.LWorkKind} at {pWorkItem.LWorkPriority}, " +
            $"{pWorkItem.LWorkStart:hh\\:mm\\:ss\\.fff}-{pWorkItem.LWorkEnd:hh\\:mm\\:ss\\.fff} " +
            $"from '{Path.GetFileName(pWorkItem.LWorkSourcePath)}' to '{pWorkItem.LWorkOutputPath}' in {pStages.Count} stage(s)");

        try
        {
            double pTotalSeconds = pWorkItem.LWorkKind switch
            {
                LWorkKind.LWorkKindAudio => LRunnerMediaRead(pWorkItem.LWorkSourcePath)?.LWorkMediaDuration.TotalSeconds ?? 0,
                LWorkKind.LWorkKindMerge => LRunnerMergeDurationRead(pWorkItem.LWorkMergeSources),
                _ => pWorkItem.LWorkDuration.TotalSeconds
            };

            int pExitCode = 0;
            string pRunnerError = string.Empty;
            string? pMeasureStderr = null;
            for (int pStageIndex = 0; pStageIndex < pStages.Count; pStageIndex++)
            {
                LEncodeStage pStage = pStages[pStageIndex];
                string pStageArguments = pStage.LEncodeStageArguments;
                if (pStageArguments.Contains(LEncode.LEncodeMeasureToken, StringComparison.Ordinal))
                {
                    string pMeasured = pMeasureStderr is null
                        ? string.Empty
                        : LEncode.LEncodeLoudnormMeasureRead(pMeasureStderr);
                    pStageArguments = pStageArguments.Replace(LEncode.LEncodeMeasureToken, pMeasured, StringComparison.Ordinal);
                }

                (pExitCode, pRunnerError) = await LRunnerStageRun(
                    pWorkItem, pStage, pStageArguments, pStageIndex + 1, pStages.Count, pTotalSeconds,
                    pRunnerClock, pDirectory, lRunnerToken).ConfigureAwait(false);

                if (pStage.LEncodeStageMeasure)
                {
                    pMeasureStderr = pRunnerError;
                }

                if (pExitCode != 0)
                {
                    break;
                }
            }

            if (lRunnerCancelled.TryRemove(pWorkItem.LWorkId, out _))
            {
                LRunnerNote($"Encode cancelled '{pWorkItem.LWorkOutputName}' after {pRunnerClock.Elapsed:hh\\:mm\\:ss\\.fff}; job removed, continuing with the queue");
                LRunnerInvoke(() =>
                {
                    pWorkItem.LWorkFinishTime = DateTimeOffset.Now;
                    lRunnerSchedule.LScheduleItemCancel(pWorkItem);
                });
                lRunnerAttempts.TryRemove(pWorkItem.LWorkId, out _);
                return;
            }

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

                if (LRunnerRetryStart(pWorkItem, $"FFmpeg exited with code {pExitCode}."))
                {
                    return;
                }
            }

            long? pOutputBytes = LRunnerBytesRead(pWorkItem.LWorkOutputPath);
            long? pSourceBytes = LRunnerSourceBytesRead(pWorkItem);
            LWorkMedia? pSourceMedia = pWorkItem.LWorkSourceMedia ?? LRunnerMediaRead(pWorkItem.LWorkSourcePath);
            LWorkMedia? pOutputMedia = LRunnerMediaRead(pWorkItem.LWorkOutputPath);
            if (pOutputMedia is { LWorkMediaVideoPresent: true }
                && LRunnerKeyframeIntervalRead(pWorkItem.LWorkOutputPath, pOutputMedia.LWorkMediaDuration) is { } pOutputKeyframeInterval)
            {
                pOutputMedia = pOutputMedia with { LWorkMediaKeyframeIntervalMilliseconds = pOutputKeyframeInterval };
            }
            LRunnerInvoke(() =>
            {
                bool pSucceeded = pExitCode == 0;
                pWorkItem.LWorkFinishTime = DateTimeOffset.Now;
                pWorkItem.LWorkOutputBytes = pOutputBytes;
                pWorkItem.LWorkSourceBytes = pSourceBytes;
                pWorkItem.LWorkSourceMedia = pSourceMedia;
                pWorkItem.LWorkOutputMedia = pOutputMedia;
                pWorkItem.LWorkProgress = pSucceeded ? 1 : pWorkItem.LWorkProgress;
                pWorkItem.LWorkStateCurrent = pSucceeded ? LWorkState.LWorkStateDone : LWorkState.LWorkStateFailed;
                pWorkItem.LWorkMessage = pSucceeded ? string.Empty : $"FFmpeg exited with code {pExitCode}.";

                lRunnerSchedule.LScheduleComplete(pWorkItem, pSucceeded, pWorkItem.LWorkMessage);
                lRunnerSchedule.LScheduleReload();
            });

            lRunnerAttempts.TryRemove(pWorkItem.LWorkId, out _);
            if (pExitCode != 0)
            {
                LRunnerFailureApply();
            }
        }
        catch (OperationCanceledException)
        {
            LRunnerNote($"Encode cancelled '{pWorkItem.LWorkOutputName}' after {pRunnerClock.Elapsed:hh\\:mm\\:ss\\.fff}; returned to the queue");
        }
        catch (Exception pException)
        {
            LRunnerNote($"Encode failed '{pWorkItem.LWorkOutputName}' after {pRunnerClock.Elapsed:hh\\:mm\\:ss\\.fff}", pException);
            if (LRunnerRetryStart(pWorkItem, pException.Message))
            {
                return;
            }

            LRunnerInvoke(() =>
            {
                pWorkItem.LWorkFinishTime = DateTimeOffset.Now;
                pWorkItem.LWorkStateCurrent = LWorkState.LWorkStateFailed;
                pWorkItem.LWorkMessage = pException.Message;
                lRunnerSchedule.LScheduleComplete(pWorkItem, false, pException.Message);
                lRunnerSchedule.LScheduleReload();
            });

            lRunnerAttempts.TryRemove(pWorkItem.LWorkId, out _);
            LRunnerFailureApply();
        }
        finally
        {
            lRunnerProcesses.TryRemove(pWorkItem.LWorkId, out _);
            lRunnerItems.TryRemove(pWorkItem.LWorkId, out _);
            LRunnerLeaseStop(pWorkItem.LWorkId);
            LRunnerTempClear(pStages);
        }
    }

    private async Task<(int, string)> LRunnerStageRun(
        LWorkItem pWorkItem,
        LEncodeStage pStage,
        string pStageArguments,
        int pStageNumber,
        int pStageCount,
        double pTotalSeconds,
        System.Diagnostics.Stopwatch pRunnerClock,
        string pDirectory,
        CancellationToken lRunnerToken)
    {
        var pStartInfo = new ProcessStartInfo
        {
            FileName = LRunnerProgramPath,
            Arguments = pStageArguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        LRunnerInvoke(() =>
        {
            pWorkItem.LWorkProgress = 0;
            pWorkItem.LWorkMessage = pStageCount > 1
                ? $"Stage {pStageNumber}/{pStageCount}: {pStage.LEncodeStageLabel}"
                : string.Empty;
        });
        LRunnerNote($"{pStage.LEncodeStageLabel} '{pWorkItem.LWorkOutputName}': {pStartInfo.FileName} {pStartInfo.Arguments}");
        LRunnerFfmpegNote(
            $"{pStage.LEncodeStageLabel} command for '{pWorkItem.LWorkOutputName}'",
            $"{pStartInfo.FileName} {pStartInfo.Arguments}\n"
            + $"working folder {(string.IsNullOrWhiteSpace(pDirectory) ? "(process default)" : pDirectory)}\n"
            + $"source {pWorkItem.LWorkSourcePath}\n"
            + $"output {pStage.LEncodeStageOutputPath}");

        using var pProcess = new Process { StartInfo = pStartInfo };
        pProcess.Start();
        lRunnerProcesses[pWorkItem.LWorkId] = pProcess;

        Task<string> pErrorTask = LRunnerErrorRead(pProcess, pWorkItem, lRunnerToken);
        await LRunnerProgressRead(pProcess, pWorkItem, pTotalSeconds, lRunnerToken).ConfigureAwait(false);
        await pProcess.WaitForExitAsync(lRunnerToken).ConfigureAwait(false);
        string pRunnerError = await pErrorTask.ConfigureAwait(false);
        LRunnerFfmpegNote(
            $"Exit code {pProcess.ExitCode} for '{pWorkItem.LWorkOutputName}' [{pStage.LEncodeStageLabel}]",
            $"ran for {pRunnerClock.Elapsed:hh\\:mm\\:ss\\.fff}");

        int pStageExit = pProcess.ExitCode;
        lRunnerProcesses.TryRemove(pWorkItem.LWorkId, out _);
        return (pStageExit, pRunnerError);
    }

    private static void LRunnerTempClear(IReadOnlyList<LEncodeStage> pStages)
    {
        foreach (LEncodeStage pStage in pStages)
        {
            if (!pStage.LEncodeStageTemporary || string.IsNullOrWhiteSpace(pStage.LEncodeStageOutputPath))
            {
                continue;
            }

            try
            {
                if (File.Exists(pStage.LEncodeStageOutputPath))
                {
                    File.Delete(pStage.LEncodeStageOutputPath);
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

    private bool LRunnerRetryStart(LWorkItem pWorkItem, string pRunnerReason)
    {
        if (!LRunnerRetryAllowed || LRunnerRetryMaximum <= 0)
        {
            return false;
        }

        int pAttempt = lRunnerAttempts.AddOrUpdate(pWorkItem.LWorkId, 1, (_, pPrevious) => pPrevious + 1);
        if (pAttempt > LRunnerRetryMaximum)
        {
            return false;
        }

        string pRunnerMessage = $"{pRunnerReason} Retry {pAttempt} of {LRunnerRetryMaximum}.";
        bool pReleased = false;
        LRunnerInvoke(() => pReleased = lRunnerSchedule.LScheduleItemRelease(pWorkItem.LWorkId, lRunnerId, pRunnerMessage));
        if (!pReleased)
        {
            return false;
        }

        LRunnerNote($"Encode requeued '{pWorkItem.LWorkOutputName}': {pRunnerMessage}");
        return true;
    }

    private void LRunnerFailureApply()
    {
        if (!LRunnerFailurePaused)
        {
            return;
        }

        LRunnerRunning = false;
        LRunnerNote("Queue paused: a job failed and 'Pause queue on failure' is on");
    }

    private static double LRunnerMergeDurationRead(IReadOnlyList<string> lRunnerMergeSources)
    {
        double lRunnerTotalSeconds = 0;
        foreach (string lRunnerMergeSource in lRunnerMergeSources)
        {
            lRunnerTotalSeconds += LRunnerMediaRead(lRunnerMergeSource)?.LWorkMediaDuration.TotalSeconds ?? 0;
        }

        return lRunnerTotalSeconds;
    }

    private static LWorkMedia? LRunnerMediaRead(string lRunnerMediaPath)
    {
        if (string.IsNullOrWhiteSpace(lRunnerMediaPath) || !File.Exists(lRunnerMediaPath))
        {
            return null;
        }

        try
        {
            Cadroue.Media.LMediaInfo lRunnerMedia = Cadroue.Media.LMediaInfo.LMediaFfprobeRead(lRunnerMediaPath);
            return new LWorkMedia(
                lRunnerMedia.LMediaInfoVideoWidth,
                lRunnerMedia.LMediaInfoVideoHeight,
                lRunnerMedia.LMediaInfoVideoFrameRate,
                (long)Math.Round(lRunnerMedia.LMediaInfoDuration.TotalMilliseconds),
                lRunnerMedia.LMediaInfoVideoPresent);
        }
        catch (Exception lRunnerException)
        {
            LRunnerNote($"Media could not be read '{Path.GetFileName(lRunnerMediaPath)}'", lRunnerException);
            return null;
        }
    }

    private static double? LRunnerKeyframeIntervalRead(string lRunnerMediaPath, TimeSpan lRunnerMediaDuration)
    {
        if (string.IsNullOrWhiteSpace(lRunnerMediaPath) || !File.Exists(lRunnerMediaPath) || lRunnerMediaDuration <= TimeSpan.Zero)
        {
            return null;
        }

        try
        {
            IReadOnlyList<Cadroue.Media.LKeyframeEntry> lRunnerKeyframes = Cadroue.Media.LKeyframeSeeker.LKeyframeRangeScan(
                lRunnerMediaPath, TimeSpan.Zero, lRunnerMediaDuration);
            if (lRunnerKeyframes.Count < 2)
            {
                return null;
            }

            double lRunnerSpanMilliseconds =
                (lRunnerKeyframes[^1].LKeyframePresentationTime - lRunnerKeyframes[0].LKeyframePresentationTime).TotalMilliseconds;
            return lRunnerSpanMilliseconds / (lRunnerKeyframes.Count - 1);
        }
        catch (Exception lRunnerException)
        {
            LRunnerNote($"Keyframe interval could not be read '{Path.GetFileName(lRunnerMediaPath)}'", lRunnerException);
            return null;
        }
    }

    private static long? LRunnerSourceBytesRead(LWorkItem pWorkItem)
    {
        if (pWorkItem.LWorkMergeSources.Count > 1)
        {
            long lRunnerMergeTotal = 0;
            foreach (string lRunnerMergeSource in pWorkItem.LWorkMergeSources)
            {
                if (LRunnerBytesRead(lRunnerMergeSource) is not { } lRunnerMergeBytes)
                {
                    lRunnerMergeTotal = 0;
                    break;
                }

                lRunnerMergeTotal += lRunnerMergeBytes;
            }

            if (lRunnerMergeTotal > 0)
            {
                return lRunnerMergeTotal;
            }
        }

        return pWorkItem.LWorkSourceBytes ?? LRunnerBytesRead(pWorkItem.LWorkSourcePath);
    }

    private static long? LRunnerBytesRead(string lRunnerOutputPath)
    {
        if (string.IsNullOrWhiteSpace(lRunnerOutputPath))
        {
            return null;
        }

        try
        {
            var lRunnerOutputFile = new FileInfo(lRunnerOutputPath);
            return lRunnerOutputFile.Exists ? lRunnerOutputFile.Length : null;
        }
        catch (Exception lRunnerException) when (lRunnerException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    private static async Task<string> LRunnerErrorRead(
        Process pProcess,
        LWorkItem pWorkItem,
        CancellationToken lRunnerToken)
    {
        if (!LRunnerVerboseCheck())
        {
            return await pProcess.StandardError.ReadToEndAsync(lRunnerToken).ConfigureAwait(false);
        }

        var pRunnerBuilder = new System.Text.StringBuilder();
        while (await pProcess.StandardError.ReadLineAsync(lRunnerToken).ConfigureAwait(false) is string pRunnerLine)
        {
            pRunnerBuilder.AppendLine(pRunnerLine);
            if (pRunnerLine.Length > 0)
            {
                LRunnerFfmpegNote($"stderr '{pWorkItem.LWorkOutputName}'", pRunnerLine);
            }
        }

        return pRunnerBuilder.ToString();
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
