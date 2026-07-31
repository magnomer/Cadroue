using System.Diagnostics;
using System.Globalization;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed class LJob
{
    private readonly LRunner lJobOwner;
    private readonly LWorkItem lJobItem;
    private readonly CancellationToken lJobToken;

    internal LJob(LRunner lJobRunner, LWorkItem lJobWorkItem, CancellationToken lJobCancelToken)
    {
        lJobOwner = lJobRunner;
        lJobItem = lJobWorkItem;
        lJobToken = lJobCancelToken;
    }

    internal async Task LJobRun()
    {
        lJobOwner.lRunnerItems[lJobItem.LWorkId] = lJobItem;
        lJobOwner.LRunnerLeaseStart(lJobItem);
        lJobOwner.LRunnerDispatch(() =>
        {
            lJobItem.LWorkStateCurrent = LWorkState.LWorkStateRunning;
            lJobItem.LWorkProgress = 0;
            lJobItem.LWorkMessage = string.Empty;
        });

        string pDirectory = Path.GetDirectoryName(lJobItem.LWorkOutputPath) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(pDirectory))
        {
            Directory.CreateDirectory(pDirectory);
        }

        LJobCollisionApply();

        IReadOnlyList<LEncodeStage> pStages = LEncode.LEncodeStagesBuild(lJobItem);

        var pJobClock = Stopwatch.StartNew();
        lJobItem.LWorkStartTime = DateTimeOffset.Now;
        lJobItem.LWorkFinishTime = null;
        LRunner.LRunnerRecord(
            $"Encode started '{lJobItem.LWorkOutputName}': {lJobItem.LWorkKind} at {lJobItem.LWorkPriority}, " +
            $"{lJobItem.LWorkOrigin:hh\\:mm\\:ss\\.fff}-{lJobItem.LWorkEnd:hh\\:mm\\:ss\\.fff} " +
            $"from '{Path.GetFileName(lJobItem.LWorkSourcePath)}' to '{lJobItem.LWorkOutputPath}' in {pStages.Count} stage(s)");

        try
        {
            double pTotalSeconds = lJobItem.LWorkKind switch
            {
                LWorkKind.LWorkKindAudio => LProbe.LProbeMediaRead(lJobItem.LWorkSourcePath)?.LWorkMediaDuration.TotalSeconds ?? 0,
                LWorkKind.LWorkKindMerge => LProbe.LProbeMergeRead(lJobItem.LWorkMergeSources),
                _ => lJobItem.LWorkDuration.TotalSeconds
            };

            int pExitCode = 0;
            string pJobError = string.Empty;
            string? pMeasureStderr = null;
            for (int pStageIndex = 0; pStageIndex < pStages.Count; pStageIndex++)
            {
                LEncodeStage pStage = pStages[pStageIndex];
                string pStageArguments = pStage.LEncodeStageArguments;
                if (pStageArguments.Contains(LEncode.LEncodeMeasureToken, StringComparison.Ordinal))
                {
                    string pMeasured = pMeasureStderr is null
                        ? string.Empty
                        : LEncode.LEncodeLoudnormRead(pMeasureStderr);
                    pStageArguments = pStageArguments.Replace(LEncode.LEncodeMeasureToken, pMeasured, StringComparison.Ordinal);
                }

                (pExitCode, pJobError) = await LJobStageRun(
                    pStage, pStageArguments, pStageIndex + 1, pStages.Count, pTotalSeconds,
                    pJobClock, pDirectory).ConfigureAwait(false);

                if (pStage.LEncodeStageMeasure)
                {
                    pMeasureStderr = pJobError;
                }

                if (pExitCode != 0)
                {
                    break;
                }
            }

            bool pJobCancelled = lJobOwner.lRunnerCancelled.TryRemove(lJobItem.LWorkId, out _);
            if (pJobCancelled && pExitCode != 0)
            {
                LRunner.LRunnerRecord($"Encode cancelled '{lJobItem.LWorkOutputName}' after {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff}; job kept as cancelled (restartable), continuing with the queue");
                lJobOwner.LRunnerDispatch(() =>
                {
                    lJobItem.LWorkFinishTime = DateTimeOffset.Now;
                    lJobOwner.lRunnerSchedule.LScheduleItemCancel(lJobItem);
                });
                lJobOwner.lRunnerAttempts.TryRemove(lJobItem.LWorkId, out _);
                return;
            }

            if (pExitCode == 0)
            {
                LRunner.LRunnerRecord(
                    $"Encode finished '{lJobItem.LWorkOutputName}' in {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff} " +
                    $"[{lJobItem.LWorkOutputPath}]");
            }
            else
            {
                LRunner.LRunnerRecord(
                    $"Encode failed '{lJobItem.LWorkOutputName}': FFmpeg exit code {pExitCode} " +
                    $"after {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff}. {LJobTailRead(pJobError)}");

                if (LJobRetryStart($"FFmpeg exited with code {pExitCode}."))
                {
                    return;
                }
            }

            long? pOutputBytes = LProbe.LProbeBytesRead(lJobItem.LWorkOutputPath);
            long? pSourceBytes = LProbe.LProbeInputRead(lJobItem);
            LWorkMedia? pSourceMedia = lJobItem.LWorkSourceMedia ?? LProbe.LProbeMediaRead(lJobItem.LWorkSourcePath);
            LWorkMedia? pOutputMedia = LProbe.LProbeMediaRead(lJobItem.LWorkOutputPath);
            if (pOutputMedia is { LWorkMediaVideoPresent: true }
                && LProbe.LProbeIntervalRead(lJobItem.LWorkOutputPath, pOutputMedia.LWorkMediaDuration) is { } pOutputKeyframeInterval)
            {
                pOutputMedia = pOutputMedia with { LWorkKeyframeInterval = pOutputKeyframeInterval };
            }
            lJobOwner.LRunnerDispatch(() =>
            {
                bool pSucceeded = pExitCode == 0;
                lJobItem.LWorkFinishTime = DateTimeOffset.Now;
                lJobItem.LWorkOutputBytes = pOutputBytes;
                lJobItem.LWorkSourceBytes = pSourceBytes;
                lJobItem.LWorkSourceMedia = pSourceMedia;
                lJobItem.LWorkOutputMedia = pOutputMedia;
                lJobItem.LWorkProgress = pSucceeded ? 1 : lJobItem.LWorkProgress;
                lJobItem.LWorkStateCurrent = pSucceeded ? LWorkState.LWorkStateDone : LWorkState.LWorkStateFailed;
                lJobItem.LWorkMessage = pSucceeded ? string.Empty : $"FFmpeg exited with code {pExitCode}.";

                lJobOwner.lRunnerSchedule.LScheduleCommit(lJobItem, pSucceeded, lJobItem.LWorkMessage);
                lJobOwner.lRunnerSchedule.LScheduleLoad();
            });

            lJobOwner.lRunnerAttempts.TryRemove(lJobItem.LWorkId, out _);
            if (pExitCode != 0)
            {
                lJobOwner.LRunnerFailureApply();
            }
        }
        catch (OperationCanceledException)
        {
            LRunner.LRunnerRecord($"Encode cancelled '{lJobItem.LWorkOutputName}' after {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff}; returned to the queue");
        }
        catch (Exception pException)
        {
            LRunner.LRunnerRecord($"Encode failed '{lJobItem.LWorkOutputName}' after {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff}", pException);
            if (LJobRetryStart(pException.Message))
            {
                return;
            }

            lJobOwner.LRunnerDispatch(() =>
            {
                lJobItem.LWorkFinishTime = DateTimeOffset.Now;
                lJobItem.LWorkStateCurrent = LWorkState.LWorkStateFailed;
                lJobItem.LWorkMessage = pException.Message;
                lJobOwner.lRunnerSchedule.LScheduleCommit(lJobItem, false, pException.Message);
                lJobOwner.lRunnerSchedule.LScheduleLoad();
            });

            lJobOwner.lRunnerAttempts.TryRemove(lJobItem.LWorkId, out _);
            lJobOwner.LRunnerFailureApply();
        }
        finally
        {
            lJobOwner.lRunnerProcesses.TryRemove(lJobItem.LWorkId, out _);
            lJobOwner.lRunnerItems.TryRemove(lJobItem.LWorkId, out _);
            lJobOwner.LRunnerLeaseStop(lJobItem.LWorkId);
            LJobTempClear(pStages);
        }
    }

    private async Task<(int, string)> LJobStageRun(
        LEncodeStage pStage,
        string pStageArguments,
        int pStageNumber,
        int pStageCount,
        double pTotalSeconds,
        Stopwatch pJobClock,
        string pDirectory)
    {
        var pStartInfo = new ProcessStartInfo
        {
            FileName = lJobOwner.LRunnerProgramPath,
            Arguments = pStageArguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        lJobOwner.LRunnerDispatch(() =>
        {
            lJobItem.LWorkProgress = 0;
            lJobItem.LWorkMessage = pStageCount > 1
                ? $"Stage {pStageNumber}/{pStageCount}: {pStage.LEncodeStageLabel}"
                : string.Empty;
        });
        LRunner.LRunnerRecord($"{pStage.LEncodeStageLabel} '{lJobItem.LWorkOutputName}': {pStartInfo.FileName} {pStartInfo.Arguments}");
        LRunner.LRunnerFfmpegRecord(
            $"{pStage.LEncodeStageLabel} command for '{lJobItem.LWorkOutputName}'",
            $"{pStartInfo.FileName} {pStartInfo.Arguments}\n"
            + $"working folder {(string.IsNullOrWhiteSpace(pDirectory) ? "(process default)" : pDirectory)}\n"
            + $"source {lJobItem.LWorkSourcePath}\n"
            + $"output {pStage.LEncodeStageOutputPath}");

        using var pProcess = new Process { StartInfo = pStartInfo };
        lJobToken.ThrowIfCancellationRequested();
        pProcess.Start();
        lJobOwner.LRunnerProcessAttach(lJobItem.LWorkId, pProcess, lJobToken);

        Task<string> pErrorTask = LJobErrorRead(pProcess);
        await LJobProgressRead(pProcess, pTotalSeconds).ConfigureAwait(false);
        await pProcess.WaitForExitAsync(lJobToken).ConfigureAwait(false);
        string pJobError = await pErrorTask.ConfigureAwait(false);
        LRunner.LRunnerFfmpegRecord(
            $"Exit code {pProcess.ExitCode} for '{lJobItem.LWorkOutputName}' [{pStage.LEncodeStageLabel}]",
            $"ran for {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff}");

        int pStageExit = pProcess.ExitCode;
        lJobOwner.lRunnerProcesses.TryRemove(lJobItem.LWorkId, out _);
        return (pStageExit, pJobError);
    }

    private async Task LJobProgressRead(Process pProcess, double pTotalSeconds)
    {
        long pBlockMicroseconds = -1;
        bool pJobVerbose = LRunner.LRunnerVerboseCheck();
        var pJobBlock = pJobVerbose ? new System.Text.StringBuilder() : null;

        while (await pProcess.StandardOutput.ReadLineAsync(lJobToken).ConfigureAwait(false) is string pLine)
        {
            int pSeparator = pLine.IndexOf('=');
            if (pSeparator <= 0)
            {
                continue;
            }

            string pKey = pLine[..pSeparator];
            string pValue = pLine[(pSeparator + 1)..].Trim();
            pJobBlock?.AppendLine(pLine);

            switch (pKey)
            {
                case "out_time_us":
                case "out_time_ms":
                    if (long.TryParse(pValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long pParsed))
                    {
                        pBlockMicroseconds = pParsed;
                    }
                    break;

                case "progress":
                    lJobOwner.LRunnerDispatch(() => lJobOwner.LRunnerPhaseSet(lJobItem, LWorkPhase.LWorkPhaseEncoding));
                    if (pBlockMicroseconds >= 0 && pTotalSeconds > 0)
                    {
                        double pFraction = pBlockMicroseconds / 1_000_000d / pTotalSeconds;
                        lJobOwner.LRunnerDispatch(() => lJobItem.LWorkProgress = pFraction);
                    }

                    if (string.Equals(pValue, "end", StringComparison.Ordinal))
                    {
                        lJobOwner.LRunnerDispatch(() => lJobItem.LWorkProgress = 1);
                    }

                    if (pJobBlock is not null)
                    {
                        LRunner.LRunnerFfmpegRecord(
                            $"stdout progress '{lJobItem.LWorkOutputName}'",
                            pJobBlock.ToString());
                        pJobBlock.Clear();
                    }

                    pBlockMicroseconds = -1;
                    break;
            }
        }
    }

    private async Task<string> LJobErrorRead(Process pProcess)
    {
        if (!LRunner.LRunnerVerboseCheck())
        {
            return await pProcess.StandardError.ReadToEndAsync(lJobToken).ConfigureAwait(false);
        }

        var pJobBuilder = new System.Text.StringBuilder();
        while (await pProcess.StandardError.ReadLineAsync(lJobToken).ConfigureAwait(false) is string pJobLine)
        {
            pJobBuilder.AppendLine(pJobLine);
            if (pJobLine.Length > 0)
            {
                LRunner.LRunnerFfmpegRecord($"stderr '{lJobItem.LWorkOutputName}'", pJobLine);
            }
        }

        return pJobBuilder.ToString();
    }

    private bool LJobRetryStart(string pJobReason)
    {
        if (!lJobOwner.LRunnerRetryAllowed || lJobOwner.LRunnerRetryMaximum <= 0)
        {
            return false;
        }

        int pAttempt = lJobOwner.lRunnerAttempts.AddOrUpdate(lJobItem.LWorkId, 1, (_, pPrevious) => pPrevious + 1);
        if (pAttempt > lJobOwner.LRunnerRetryMaximum)
        {
            return false;
        }

        string pJobMessage = $"{pJobReason} Retry {pAttempt} of {lJobOwner.LRunnerRetryMaximum}.";
        bool pReleased = false;
        lJobOwner.LRunnerDispatch(() => pReleased = lJobOwner.lRunnerSchedule.LScheduleItemRelease(lJobItem.LWorkId, lJobOwner.LRunnerIdentity, pJobMessage));
        if (!pReleased)
        {
            return false;
        }

        LRunner.LRunnerRecord($"Encode requeued '{lJobItem.LWorkOutputName}': {pJobMessage}");
        return true;
    }

    private void LJobCollisionApply()
    {
        LWorkOutput pOutput = lJobItem.LWorkOutput;
        string pTarget = lJobItem.LWorkOutputPath;
        if (string.IsNullOrWhiteSpace(pTarget) || !File.Exists(pTarget))
        {
            return;
        }

        if (string.Equals(pOutput.LWorkOutputCollision, "Rename output", StringComparison.Ordinal))
        {
            string pFreePath = LJobPathResolve(pTarget, pOutput.LWorkOutputCollisionSuffix);
            lJobItem.LWorkOutputSet(pFreePath, Path.GetFileName(pFreePath));
            LRunner.LRunnerRecord($"Output exists; renaming output to '{Path.GetFileName(pFreePath)}'");
            return;
        }

        if (string.Equals(pOutput.LWorkOutputCollision, "Rename existing", StringComparison.Ordinal))
        {
            string pFreePath = LJobPathResolve(pTarget, pOutput.LWorkOutputCollisionSuffix);
            try
            {
                File.Move(pTarget, pFreePath);
                LRunner.LRunnerRecord($"Output exists; renaming existing file to '{Path.GetFileName(pFreePath)}'");
            }
            catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
            {
                LRunner.LRunnerRecord($"Could not rename existing file '{Path.GetFileName(pTarget)}'; it will be overwritten", pException);
            }
        }
    }

    private static string LJobPathResolve(string pPath, string pSuffix)
    {
        string pFolder = Path.GetDirectoryName(pPath) ?? string.Empty;
        string pStem = Path.GetFileNameWithoutExtension(pPath);
        string pExtension = Path.GetExtension(pPath);
        string pSuffixText = string.IsNullOrEmpty(pSuffix) ? "_1" : pSuffix;

        for (int pIndex = 0; ; pIndex++)
        {
            string pName = pIndex == 0
                ? $"{pStem}{pSuffixText}{pExtension}"
                : $"{pStem}{pSuffixText} ({pIndex + 1}){pExtension}";
            string pCandidate = Path.Combine(pFolder, pName);
            if (!File.Exists(pCandidate))
            {
                return pCandidate;
            }
        }
    }

    private static void LJobTempClear(IReadOnlyList<LEncodeStage> pStages)
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

    private static string LJobTailRead(string pJobError)
    {
        if (string.IsNullOrWhiteSpace(pJobError))
        {
            return "FFmpeg reported nothing.";
        }

        string[] pJobLines = pJobError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(" | ", pJobLines[^Math.Min(3, pJobLines.Length)..]);
    }
}
