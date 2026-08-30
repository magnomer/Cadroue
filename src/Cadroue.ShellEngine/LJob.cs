using System.Diagnostics;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    private readonly LRunner lJobOwner;
    private readonly LWorkItem lJobItem;
    private readonly CancellationToken lJobToken;

    private double lJobTotalSeconds;
    private long lJobBlockMicroseconds = -1;
    private System.Text.StringBuilder? lJobProgressBlock;

    private readonly List<LEncodeStage> lJobStagesDone = new();
    private readonly List<string> lJobReserved = new();
    private Stopwatch lJobClock = null!;
    private string lJobDirectory = string.Empty;
    private double lJobRunSeconds;
    private string lJobFinalPath = string.Empty;
    private LWorkState? lJobValidateState;
    private string lJobValidateMessage = string.Empty;

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

        string pJobCollision = LJobCollisionApply();
        if (pJobCollision.Length > 0)
        {
            LRunner.LRunnerRecord($"Encode skipped '{lJobItem.LWorkOutputName}': {pJobCollision}");
            lJobOwner.LRunnerDispatch(() =>
            {
                lJobItem.LWorkFinishTime = DateTimeOffset.Now;
                lJobItem.LWorkStateCurrent = LWorkState.LWorkStateFailed;
                lJobItem.LWorkMessage = pJobCollision;
                lJobOwner.lRunnerSchedule.LScheduleCommit(lJobItem, false, pJobCollision);
                lJobOwner.lRunnerSchedule.LScheduleLoad();
            });
            lJobOwner.lRunnerAttempts.TryRemove(lJobItem.LWorkId, out _);
            lJobOwner.lRunnerItems.TryRemove(lJobItem.LWorkId, out _);
            lJobOwner.LRunnerLeaseStop(lJobItem.LWorkId);
            LJobReservedClear();
            lJobOwner.LRunnerFailureApply();
            return;
        }

        // Persist the resolved output path before the encode runs, synchronously so the
        // stored record is durable first. A retry or stale-job recovery then acts on the
        // reserved name, never the original pre-existing file. The record is this job's own
        // running entry (owner-guarded, atomic replace), safe to write off the post thread.
        lJobOwner.lRunnerSchedule.LScheduleOutputCommit(
            lJobItem.LWorkId, lJobOwner.LRunnerIdentity, lJobItem.LWorkOutputPath, lJobItem.LWorkOutputName);

        lJobItem.LWorkSourceMedia ??= LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken);

        var pJobClock = Stopwatch.StartNew();
        lJobClock = pJobClock;
        lJobDirectory = pDirectory;
        lJobItem.LWorkStartTime = DateTimeOffset.Now;
        lJobItem.LWorkFinishTime = null;
        LRunner.LRunnerRecord(
            $"Encode started '{lJobItem.LWorkOutputName}': {lJobItem.LWorkKind} at {lJobItem.LWorkPriority}, " +
            $"{lJobItem.LWorkOrigin:hh\\:mm\\:ss\\.fff}-{lJobItem.LWorkEnd:hh\\:mm\\:ss\\.fff} " +
            $"from '{Path.GetFileName(lJobItem.LWorkSourcePath)}' to '{lJobItem.LWorkOutputPath}'");

        try
        {
            double pTotalSeconds = lJobItem.LWorkKind switch
            {
                LWorkKind.LWorkKindAudio => LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken)?.LWorkMediaDuration.TotalSeconds ?? 0,
                LWorkKind.LWorkKindMerge => LScout.LScoutMergeRead(lJobItem.LWorkMergeSources, lJobToken),
                _ => lJobItem.LWorkDuration.TotalSeconds
            };

            if (pTotalSeconds <= 0 && lJobItem.LWorkKind != LWorkKind.LWorkKindMerge)
            {
                pTotalSeconds = (lJobItem.LWorkSourceMedia
                    ?? LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken))
                    ?.LWorkMediaDuration.TotalSeconds ?? 0;
            }

            lJobRunSeconds = pTotalSeconds;
            (int pExitCode, string pJobError) = await LJobStagesRun().ConfigureAwait(false);

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

            string pFailureMessage = string.Empty;
            if (pExitCode == 0)
            {
                LJobStageCommit();
                LRunner.LRunnerRecord(
                    $"Encode finished '{lJobItem.LWorkOutputName}' in {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff} " +
                    $"[{lJobItem.LWorkOutputPath}]");
            }
            else
            {
                string pTail = LJobTailRead(pJobError);
                LAutopsyResult pAutopsy = LAutopsy.LAutopsyResolve(pExitCode, pTail);
                string pSymbol = pAutopsy.LAutopsyResultSymbol is { Length: > 0 } pAutopsySymbol
                    ? $" {pAutopsySymbol}"
                    : string.Empty;

                LRunner.LRunnerRecord(
                    $"Encode failed '{lJobItem.LWorkOutputName}': {pAutopsy.LAutopsyResultTechnical} " +
                    $"(exit {pAutopsy.LAutopsyResultCode}{pSymbol}) " +
                    $"after {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff}. {pTail}");

                pFailureMessage = pAutopsy.LAutopsyResultVisible
                    ? pAutopsy.LAutopsyResultAction is { Length: > 0 } pAutopsyAction
                        ? $"{pAutopsy.LAutopsyResultSimple} {pAutopsyAction}"
                        : pAutopsy.LAutopsyResultSimple
                    : $"FFmpeg exited with code {pExitCode}.";

                if (LJobRetryStart(pFailureMessage))
                {
                    return;
                }
            }

            bool pExitClean = pExitCode == 0;
            LWorkState pTerminalState = pExitClean
                ? lJobValidateState ?? LWorkState.LWorkStateDone
                : LWorkState.LWorkStateFailed;
            bool pSucceeded = pTerminalState == LWorkState.LWorkStateDone;

            // A Fix that does not end resolved must leave nothing behind: the copied
            // (and any partially repaired) output is discarded so an unrepaired file
            // never persists as if it were a valid result.
            if (lJobItem.LWorkKind == LWorkKind.LWorkKindFix && !pSucceeded)
            {
                LJobOutputClear();
            }

            long? pOutputBytes = LScout.LScoutBytesRead(lJobItem.LWorkOutputPath);
            long? pSourceBytes = LScout.LScoutInputRead(lJobItem, lJobToken);
            IReadOnlyList<long> pMergeBytes = LJobMergeRead();
            LWorkMedia? pSourceMedia = LJobMediaResolve(
                lJobItem.LWorkSourceMedia ?? LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken),
                lJobItem.LWorkSourcePath);
            LWorkMedia? pOutputMedia = LJobMediaResolve(
                LScout.LScoutMediaRead(lJobItem.LWorkOutputPath, lJobToken),
                lJobItem.LWorkOutputPath);
            lJobOwner.LRunnerDispatch(() =>
            {
                lJobItem.LWorkFinishTime = DateTimeOffset.Now;
                lJobItem.LWorkOutputBytes = pOutputBytes;
                lJobItem.LWorkSourceBytes = pSourceBytes;
                lJobItem.LWorkMergeBytes = pMergeBytes;
                lJobItem.LWorkSourceMedia = pSourceMedia;
                lJobItem.LWorkOutputMedia = pOutputMedia;
                lJobItem.LWorkProgress = pSucceeded ? 1 : lJobItem.LWorkProgress;
                lJobItem.LWorkStateCurrent = pTerminalState;
                lJobItem.LWorkMessage = pExitClean
                    ? lJobValidateState is null ? string.Empty : lJobValidateMessage
                    : pFailureMessage;

                lJobOwner.lRunnerSchedule.LScheduleCommit(lJobItem, pSucceeded, lJobItem.LWorkMessage);
                LJobSalvageRecord();
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

            if (lJobItem.LWorkKind == LWorkKind.LWorkKindFix)
            {
                LJobOutputClear();
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
            LJobTempClear(lJobStagesDone);
            LJobReservedClear();
            LEncode.LEncodeBridgeClear(lJobItem.LWorkId);
        }
    }

    // The worklist never re-measures a source or output when a row is selected: every
    // figure it shows is measured here, once, while the job is in hand, and stored on the
    // item so the record survives the file being deleted. This enriches the probed media
    // snapshot with the keyframe interval (video) and integrated loudness (audio) that the
    // base probe does not carry.
    // Per-input byte sizes for a merge, measured once here so the worklist's batch summary
    // can total sources without touching disk when a row is selected.
    private IReadOnlyList<long> LJobMergeRead()
    {
        if (lJobItem.LWorkMergeSources.Count <= 1)
        {
            return Array.Empty<long>();
        }

        var pMergeBytes = new List<long>(lJobItem.LWorkMergeSources.Count);
        foreach (string pMergeSource in lJobItem.LWorkMergeSources)
        {
            pMergeBytes.Add(LScout.LScoutBytesRead(pMergeSource) ?? 0);
        }

        return pMergeBytes;
    }

    private LWorkMedia? LJobMediaResolve(LWorkMedia? pMedia, string pPath)
    {
        if (pMedia is null)
        {
            return null;
        }

        LWorkMedia pMeasured = pMedia;
        if (pMeasured.LWorkMediaVideo
            && pMeasured.LWorkKeyframeInterval is null
            && LScout.LScoutIntervalRead(pPath, pMeasured.LWorkMediaDuration, lJobToken) is { } pInterval)
        {
            pMeasured = pMeasured with { LWorkKeyframeInterval = pInterval };
        }

        if (pMeasured.LWorkMediaSamplerate > 0
            && pMeasured.LWorkMediaLoudness is null
            && LScout.LScoutLoudnessRead(pPath, lJobToken) is { } pLoudness)
        {
            pMeasured = pMeasured with { LWorkMediaLoudness = pLoudness };
        }

        return pMeasured;
    }

    private async Task<(int, string)> LJobStagesRun()
    {
        if (!LEncode.LEncodeSmartCheck(lJobItem))
        {
            return await LJobSmartRun().ConfigureAwait(false);
        }

        if (string.Equals(lJobItem.LWorkOutput.LEncodingVideo.LEncodingMode, "Smart", StringComparison.OrdinalIgnoreCase))
        {
            LRunner.LRunnerRecord(
                lJobItem.LWorkKind != LWorkKind.LWorkKindSplit
                    ? $"Smart encoding fallback for '{lJobItem.LWorkOutputName}': smart encoding applies to split tabs only; encoding the full requested interval"
                    : $"Smart encoding fallback for '{lJobItem.LWorkOutputName}': the item has edits or an fps change; encoding the full requested interval");
        }

        if (lJobItem.LWorkKind == LWorkKind.LWorkKindFix)
        {
            return await LJobFixRun().ConfigureAwait(false);
        }

        IReadOnlyList<LEncodeStage> pStages = LEncode.LEncodeStagesBuild(lJobItem);
        if (pStages.Count == 0)
        {
            throw new InvalidOperationException("the stored job produced no encode steps (incomplete or corrupt)");
        }

        return await LJobBatchRun(pStages, 0, pStages.Count).ConfigureAwait(false);
    }

    private async Task<(int, string)> LJobBatchRun(IReadOnlyList<LEncodeStage> pStages, int pBaseNumber, int pTotalCount)
    {
        int pExitCode = 0;
        string pJobError = string.Empty;
        string? pMeasureStderr = null;
        for (int pStageIndex = 0; pStageIndex < pStages.Count; pStageIndex++)
        {
            await lJobOwner.LRunnerResume(lJobToken).ConfigureAwait(false);

            LEncodeStage pStage = pStages[pStageIndex];
            lJobStagesDone.Add(pStage);

            if (pStage.LEncodeStageKind == LWorkStage.LWorkStageSplice)
            {
                (pExitCode, pJobError) = LJobLeadingRun(pStage);
                if (pExitCode != 0)
                {
                    break;
                }

                continue;
            }

            if (pStage.LEncodeStageKind == LWorkStage.LWorkStageDuplicate)
            {
                (pExitCode, pJobError) = LJobCopyRun(pStage, pBaseNumber + pStageIndex + 1, pTotalCount);
                if (pExitCode != 0)
                {
                    break;
                }

                continue;
            }

            if (pStage.LEncodeStageKind == LWorkStage.LWorkStageVerify)
            {
                (pExitCode, pJobError) = await LJobValidateRun(pStage, pBaseNumber + pStageIndex + 1, pTotalCount).ConfigureAwait(false);
                if (pExitCode != 0)
                {
                    break;
                }

                continue;
            }

            if (pStage.LEncodeStageKind == LWorkStage.LWorkStageRepair)
            {
                (pExitCode, pJobError) = await LJobRepairRun(pStage, pBaseNumber + pStageIndex + 1, pTotalCount).ConfigureAwait(false);
                if (pExitCode != 0)
                {
                    break;
                }

                continue;
            }

            string pStageArguments = pStage.LEncodeStageArguments;
            if (pStageArguments.Contains(LEncode.LEncodeMeasureToken, StringComparison.Ordinal))
            {
                string pMeasured = pMeasureStderr is null
                    ? string.Empty
                    : LEncodeLoudnorm.LEncodeLoudnormRead(pMeasureStderr);
                pStageArguments = pStageArguments.Replace(LEncode.LEncodeMeasureToken, pMeasured, StringComparison.Ordinal);
            }

            (pExitCode, pJobError) = await LJobStageRun(
                pStage, pStageArguments, pBaseNumber + pStageIndex + 1, pTotalCount, lJobRunSeconds,
                lJobClock, lJobDirectory).ConfigureAwait(false);

            if (pStage.LEncodeStageMeasure)
            {
                pMeasureStderr = pJobError;
            }

            if (pExitCode != 0)
            {
                break;
            }
        }

        return (pExitCode, pJobError);
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
        string pExecutableArguments = lJobOwner.LRunnerArgumentTransform?.Invoke(pStageArguments)
            ?? pStageArguments;
        lJobOwner.LRunnerDispatch(() =>
        {
            lJobItem.LWorkProgress = 0;
            lJobItem.LWorkStageCurrent = pStage.LEncodeStageKind;
            lJobItem.LWorkMessage = pStageCount > 1
                ? $"Stage {pStageNumber}/{pStageCount}: {pStage.LEncodeStageLabel}"
                : string.Empty;
            lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeStatus);
        });
        LRunner.LRunnerRecord($"{pStage.LEncodeStageLabel} '{lJobItem.LWorkOutputName}': {lJobOwner.LRunnerProgramPath} {pExecutableArguments}");
        LRunner.LRunnerFfmpegRecord(
            $"{pStage.LEncodeStageLabel} command for '{lJobItem.LWorkOutputName}'",
            $"{lJobOwner.LRunnerProgramPath} {pExecutableArguments}\n"
            + $"working folder {(string.IsNullOrWhiteSpace(pDirectory) ? "(process default)" : pDirectory)}\n"
            + $"source {lJobItem.LWorkSourcePath}\n"
            + $"output {pStage.LEncodeStagePath}");

        lJobTotalSeconds = pTotalSeconds;
        lJobBlockMicroseconds = -1;
        lJobProgressBlock = LRunner.LRunnerVerboseCheck() ? new System.Text.StringBuilder() : null;

        var pJobEmployer = new LEmployer(
            lJobOwner.LRunnerProgramPath,
            lJobOwner.LRunnerArgumentPrefix);
        LEmployerResult pJobResult = await pJobEmployer.LEmployerRun(
            pExecutableArguments,
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
}
