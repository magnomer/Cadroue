using System.Diagnostics;
using System.Globalization;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed class LJob
{
    private readonly LRunner lJobOwner;
    private readonly LWorkItem lJobItem;
    private readonly CancellationToken lJobToken;

    private double lJobTotalSeconds;
    private long lJobBlockMicroseconds = -1;
    private System.Text.StringBuilder? lJobProgressBlock;

    internal LJob(LRunner lJobRunner, LWorkItem lJobWorkItem, CancellationToken lJobCancelToken)
    {
        lJobOwner = lJobRunner;
        lJobItem = lJobWorkItem;
        lJobToken = lJobCancelToken;
    }

    internal async Task LJobRun()
    {
        string pJobInvalid = LJobValidate();
        if (pJobInvalid.Length > 0)
        {
            LRunner.LRunnerRecord($"Encode skipped '{lJobItem.LWorkOutputName}': {pJobInvalid}");
            lJobOwner.LRunnerDispatch(() =>
            {
                lJobItem.LWorkFinishTime = DateTimeOffset.Now;
                lJobItem.LWorkStateCurrent = LWorkState.LWorkStateFailed;
                lJobItem.LWorkMessage = pJobInvalid;
                lJobOwner.lRunnerSchedule.LScheduleCommit(lJobItem, false, pJobInvalid);
                lJobOwner.lRunnerSchedule.LScheduleLoad();
            });
            lJobOwner.lRunnerAttempts.TryRemove(lJobItem.LWorkId, out _);
            lJobOwner.LRunnerFailureApply();
            return;
        }

        lJobOwner.lRunnerItems[lJobItem.LWorkId] = lJobItem;
        lJobOwner.LRunnerLeaseStart(lJobItem);
        lJobOwner.LRunnerDispatch(() =>
        {
            lJobItem.LWorkStateCurrent = LWorkState.LWorkStateRunning;
            lJobItem.LWorkProgress = 0;
            lJobItem.LWorkMessage = string.Empty;
            lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeStatus);
        });

        string pDirectory = Path.GetDirectoryName(lJobItem.LWorkOutputPath) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(pDirectory))
        {
            Directory.CreateDirectory(pDirectory);
        }

        LJobCollisionApply();

        IReadOnlyList<LEncodeStage> pStages = LEncode.LEncodeStagesBuild(lJobItem);
        if (pStages.Count == 0)
        {
            throw new InvalidOperationException("the stored job produced no encode steps (incomplete or corrupt)");
        }

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
                LWorkKind.LWorkKindAudio => LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken)?.LWorkMediaDuration.TotalSeconds ?? 0,
                LWorkKind.LWorkKindMerge => LScout.LScoutMergeRead(lJobItem.LWorkMergeSources, lJobToken),
                _ => lJobItem.LWorkDuration.TotalSeconds
            };

            int pExitCode = 0;
            string pJobError = string.Empty;
            string? pMeasureStderr = null;
            for (int pStageIndex = 0; pStageIndex < pStages.Count; pStageIndex++)
            {
                await lJobOwner.LRunnerResumeAwait(lJobToken).ConfigureAwait(false);

                LEncodeStage pStage = pStages[pStageIndex];
                string pStageArguments = pStage.LEncodeStageArguments;
                if (pStageArguments.Contains(LEncode.LEncodeMeasureToken, StringComparison.Ordinal))
                {
                    string pMeasured = pMeasureStderr is null
                        ? string.Empty
                        : LEncodeLoudnorm.LEncodeLoudnormRead(pMeasureStderr);
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

            long? pOutputBytes = LScout.LScoutBytesRead(lJobItem.LWorkOutputPath);
            long? pSourceBytes = LScout.LScoutInputRead(lJobItem, lJobToken);
            LWorkMedia? pSourceMedia = lJobItem.LWorkSourceMedia ?? LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken);
            LWorkMedia? pOutputMedia = LScout.LScoutMediaRead(lJobItem.LWorkOutputPath, lJobToken);
            if (pOutputMedia is { LWorkMediaVideo: true }
                && LScout.LScoutIntervalRead(lJobItem.LWorkOutputPath, pOutputMedia.LWorkMediaDuration, lJobToken) is { } pOutputKeyframeInterval)
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

    private string LJobValidate()
    {
        if (lJobItem.LWorkKind == LWorkKind.LWorkKindMerge)
        {
            if (lJobItem.LWorkMergeSources.Count == 0)
            {
                return "the merge has no source files (the stored job is incomplete or corrupt)";
            }

            foreach (string pMergeSource in lJobItem.LWorkMergeSources)
            {
                if (string.IsNullOrWhiteSpace(pMergeSource) || !File.Exists(pMergeSource))
                {
                    return $"a merge source is missing: '{pMergeSource}'";
                }
            }
        }
        else if (string.IsNullOrWhiteSpace(lJobItem.LWorkSourcePath) || !File.Exists(lJobItem.LWorkSourcePath))
        {
            return $"the source file is missing: '{lJobItem.LWorkSourcePath}'";
        }

        if (string.IsNullOrWhiteSpace(lJobItem.LWorkOutputPath))
        {
            return "the output path is empty (the stored job is incomplete or corrupt)";
        }

        IEnumerable<string> pJobInputs = lJobItem.LWorkKind == LWorkKind.LWorkKindMerge
            ? lJobItem.LWorkMergeSources
            : new[] { lJobItem.LWorkSourcePath };
        if (LJobCollisionCheck(lJobItem.LWorkOutputPath, pJobInputs))
        {
            return "the output path is the same as an input file; the source will not be overwritten";
        }

        return string.Empty;
    }

    internal static bool LJobCollisionCheck(string pOutputPath, IEnumerable<string> pInputPaths)
    {
        string pOutputFullPath = Path.GetFullPath(pOutputPath);
        return pInputPaths.Any(pInputPath => string.Equals(
            pOutputFullPath,
            Path.GetFullPath(pInputPath),
            StringComparison.OrdinalIgnoreCase));
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
        lJobOwner.LRunnerDispatch(() =>
        {
            lJobItem.LWorkProgress = 0;
            lJobItem.LWorkMessage = pStageCount > 1
                ? $"Stage {pStageNumber}/{pStageCount}: {pStage.LEncodeStageLabel}"
                : string.Empty;
            lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeStatus);
        });
        LRunner.LRunnerRecord($"{pStage.LEncodeStageLabel} '{lJobItem.LWorkOutputName}': {lJobOwner.LRunnerProgramPath} {pStageArguments}");
        LRunner.LRunnerFfmpegRecord(
            $"{pStage.LEncodeStageLabel} command for '{lJobItem.LWorkOutputName}'",
            $"{lJobOwner.LRunnerProgramPath} {pStageArguments}\n"
            + $"working folder {(string.IsNullOrWhiteSpace(pDirectory) ? "(process default)" : pDirectory)}\n"
            + $"source {lJobItem.LWorkSourcePath}\n"
            + $"output {pStage.LEncodeStagePath}");

        lJobTotalSeconds = pTotalSeconds;
        lJobBlockMicroseconds = -1;
        lJobProgressBlock = LRunner.LRunnerVerboseCheck() ? new System.Text.StringBuilder() : null;

        var pJobEmployer = new LEmployer(lJobOwner.LRunnerProgramPath);
        LEmployerResult pJobResult = await pJobEmployer.LEmployerRun(
            pStageArguments,
            lJobToken,
            pProcess => lJobOwner.LRunnerProcessAttach(lJobItem.LWorkId, pProcess, lJobToken),
            LJobOutputRead,
            LJobStderrRead).ConfigureAwait(false);

        LRunner.LRunnerFfmpegRecord(
            $"Exit code {pJobResult.LEmployerExit} for '{lJobItem.LWorkOutputName}' [{pStage.LEncodeStageLabel}]",
            $"ran for {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff}");

        lJobOwner.lRunnerProcesses.TryRemove(lJobItem.LWorkId, out _);
        return (pJobResult.LEmployerExit, pJobResult.LEmployerError);
    }

    private void LJobOutputRead(string pLine)
    {
        int pSeparator = pLine.IndexOf('=');
        if (pSeparator <= 0)
        {
            return;
        }

        string pKey = pLine[..pSeparator];
        string pValue = pLine[(pSeparator + 1)..].Trim();
        lJobProgressBlock?.AppendLine(pLine);

        switch (pKey)
        {
            case "out_time_us":
            case "out_time_ms":
                if (long.TryParse(pValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long pParsed))
                {
                    lJobBlockMicroseconds = pParsed;
                }
                break;

            case "progress":
                lJobOwner.LRunnerDispatch(() => lJobOwner.LRunnerPhaseSet(lJobItem, LWorkPhase.LWorkPhaseEncoding));
                if (lJobBlockMicroseconds >= 0 && lJobTotalSeconds > 0)
                {
                    double pFraction = lJobBlockMicroseconds / 1_000_000d / lJobTotalSeconds;
                    lJobOwner.LRunnerDispatch(() =>
                    {
                        lJobItem.LWorkProgress = pFraction;
                        lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeProgress);
                    });
                }

                if (string.Equals(pValue, "end", StringComparison.Ordinal))
                {
                    lJobOwner.LRunnerDispatch(() =>
                    {
                        lJobItem.LWorkProgress = 1;
                        lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeProgress);
                    });
                }

                if (lJobProgressBlock is not null)
                {
                    LRunner.LRunnerFfmpegRecord(
                        $"stdout progress '{lJobItem.LWorkOutputName}'",
                        lJobProgressBlock.ToString());
                    lJobProgressBlock.Clear();
                }

                lJobBlockMicroseconds = -1;
                break;
        }
    }

    private void LJobStderrRead(string pLine)
    {
        if (pLine.Length > 0 && LRunner.LRunnerVerboseCheck())
        {
            LRunner.LRunnerFfmpegRecord($"stderr '{lJobItem.LWorkOutputName}'", pLine);
        }
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
        LEncoding pOutput = lJobItem.LWorkOutput;
        string pTarget = lJobItem.LWorkOutputPath;
        if (string.IsNullOrWhiteSpace(pTarget) || !File.Exists(pTarget))
        {
            return;
        }

        if (string.Equals(pOutput.LEncodingCollision, "Rename output", StringComparison.Ordinal))
        {
            string pFreePath = LJobPathResolve(pTarget, pOutput.LEncodingCollisionSuffix);
            lJobItem.LWorkOutputSet(pFreePath, Path.GetFileName(pFreePath));
            LRunner.LRunnerRecord($"Output exists; renaming output to '{Path.GetFileName(pFreePath)}'");
            return;
        }

        if (string.Equals(pOutput.LEncodingCollision, "Rename existing", StringComparison.Ordinal))
        {
            string pFreePath = LJobPathResolve(pTarget, pOutput.LEncodingCollisionSuffix);
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
            if (!pStage.LEncodeStageTemporary || string.IsNullOrWhiteSpace(pStage.LEncodeStagePath))
            {
                continue;
            }

            string pPath = pStage.LEncodeStagePath;
            bool pRemoved = false;
            for (int pAttempt = 0; pAttempt < 5 && !pRemoved; pAttempt++)
            {
                try
                {
                    if (!File.Exists(pPath))
                    {
                        pRemoved = true;
                        break;
                    }

                    File.Delete(pPath);
                    pRemoved = true;
                }
                catch (Exception pException)
                    when (pException is IOException or UnauthorizedAccessException)
                {
                    System.Threading.Thread.Sleep(200);
                }
            }

            if (!pRemoved)
            {
                LRunner.LRunnerRecord(
                    $"Could not delete the temporary file '{pPath}'; it may remain on disk.",
                    null);
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
