using System.Diagnostics;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed class LJob
{
    private readonly LRunner lJobOwner;
    private readonly LWorkItem lJobItem;
    private readonly CancellationTokenSource lJobSource;
    private readonly CancellationToken lJobToken;
    private readonly LJobReservation lJobReservation = new();
    private readonly LJobCollision lJobCollision;
    private readonly LJobFile lJobFile;
    private readonly LJobVerdict lJobVerdict;
    private readonly LJobSalvage lJobSalvage;
    private readonly LJobMeasure lJobMeasure;
    private LJobStage? lJobStage;
    private volatile bool lJobCancelled;

    internal LJob(LRunner lJobRunner, LWorkItem lJobWorkItem, CancellationToken lJobStopToken)
    {
        lJobOwner = lJobRunner;
        lJobItem = lJobWorkItem;
        lJobSource = CancellationTokenSource.CreateLinkedTokenSource(lJobStopToken);
        lJobToken = lJobSource.Token;
        lJobCollision = new LJobCollision(lJobItem, lJobReservation);
        lJobFile = new LJobFile(lJobItem, lJobCollision);
        lJobVerdict = new LJobVerdict(lJobOwner, lJobItem, lJobToken);
        lJobSalvage = new LJobSalvage(lJobOwner, lJobItem, lJobToken);
        lJobMeasure = new LJobMeasure(lJobOwner, lJobItem, lJobToken);
    }

    internal Task LJobCompletion { get; private set; } = Task.CompletedTask;

    internal Task LJobStart() => LJobCompletion = LJobRun();

    internal void LJobCancel()
    {
        lJobCancelled = true;
        try
        {
            lJobSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private async Task LJobRun()
    {
        var pJobClock = Stopwatch.StartNew();
        if (lJobOwner.LRunnerPendingRemove(lJobItem.LWorkId))
        {
            LJobCancel();
        }

        try
        {
            string pJobInvalid = await new LJobPreflight(lJobOwner, lJobItem, lJobToken, lJobCollision)
                .LJobValidate().ConfigureAwait(false);
            if (pJobInvalid.Length > 0)
            {
                LRunnerTrace.LRunnerRecord($"Encode skipped '{lJobItem.LWorkOutputName}': {pJobInvalid}");
                LJobFailureCommit(pJobInvalid);
                return;
            }

            lJobOwner.LRunnerLease.LRunnerLeaseStart(lJobItem);
            lJobOwner.LRunnerDispatch(() =>
            {
                lJobItem.LWorkRunState.LWorkStateCurrent = LWorkState.LWorkStateRunning;
                lJobItem.LWorkRunState.LWorkProgress = 0;
                lJobItem.LWorkRunState.LWorkMessage = string.Empty;
                lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeStatus);
            });

            string pDirectory = Path.GetDirectoryName(lJobItem.LWorkOutputPath) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(pDirectory))
            {
                Directory.CreateDirectory(pDirectory);
            }

            string pJobCollision = lJobCollision.LJobCollisionApply();
            if (pJobCollision.Length > 0)
            {
                LRunnerTrace.LRunnerRecord($"Encode skipped '{lJobItem.LWorkOutputName}': {pJobCollision}");
                LJobFailureCommit(pJobCollision);
                return;
            }

            lJobOwner.lRunnerScheduleLease.LScheduleOutputCommit(
                lJobItem.LWorkId, lJobOwner.LRunnerIdentity, lJobItem.LWorkOutputPath, lJobItem.LWorkOutputName);

            lJobItem.LWorkSourceMedia ??=
                await LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken, lJobOwner.LRunnerLevel)
                    .ConfigureAwait(false);

            lJobItem.LWorkRunState.LWorkStartTime = DateTimeOffset.Now;
            lJobItem.LWorkRunState.LWorkFinishTime = null;
            LRunnerTrace.LRunnerRecord(
                $"Encode started '{lJobItem.LWorkOutputName}': {lJobItem.LWorkKind} at {lJobItem.LWorkPriority}, " +
                $"{lJobItem.LWorkOrigin:hh\\:mm\\:ss\\.fff}-{lJobItem.LWorkEnd:hh\\:mm\\:ss\\.fff} " +
                $"from '{Path.GetFileName(lJobItem.LWorkSourcePath)}' to '{lJobItem.LWorkOutputPath}'");

            double pTotalSeconds = await lJobMeasure.LJobSecondsRead().ConfigureAwait(false);
            lJobStage = new LJobStage(
                lJobOwner, lJobItem, lJobToken, lJobReservation, lJobVerdict, pJobClock, pDirectory, pTotalSeconds);
            (int pExitCode, string pJobError) = await LJobStagesRun(lJobStage).ConfigureAwait(false);
            lJobToken.ThrowIfCancellationRequested();

            string pFailureMessage = string.Empty;
            if (pExitCode == 0)
            {
                lJobCollision.LJobStageCommit();
                LRunnerTrace.LRunnerRecord(
                    $"Encode finished '{lJobItem.LWorkOutputName}' in {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff} " +
                    $"[{lJobItem.LWorkOutputPath}]");
            }
            else
            {
                string pTail = LJobFailure.LJobTailRead(pJobError);
                LAutopsyResult pAutopsy = LAutopsy.LAutopsyResolve(pExitCode);
                LJobFailure.LJobFailureRecord(lJobItem, pAutopsy, pTail, pJobClock.Elapsed);
                pFailureMessage = LJobFailure.LJobFailureDescribe(pAutopsy, pExitCode, pTail);

                if (await LJobRetryStart(pFailureMessage).ConfigureAwait(false))
                {
                    return;
                }
            }

            bool pExitClean = pExitCode == 0;
            LWorkState pTerminalState = pExitClean
                ? lJobVerdict.LJobValidateState ?? LWorkState.LWorkStateDone
                : LWorkState.LWorkStateFailed;
            long? pOutputBytes = LScoutBytes.LScoutBytesRead(lJobItem.LWorkOutputPath);
            if (pTerminalState == LWorkState.LWorkStateDone && pOutputBytes is not > 0)
            {
                pExitClean = false;
                pTerminalState = LWorkState.LWorkStateFailed;
                pFailureMessage = pOutputBytes is null
                    ? "FFmpeg reported success but the output file was not produced."
                    : "FFmpeg reported success but the output file is empty.";
                LRunnerTrace.LRunnerRecord($"Encode failed '{lJobItem.LWorkOutputName}': {pFailureMessage}");
            }

            bool pSucceeded = pTerminalState == LWorkState.LWorkStateDone;

            if (lJobItem.LWorkKind == LWorkKind.LWorkKindFix && !pSucceeded)
            {
                await lJobFile.LJobOutputClear().ConfigureAwait(false);
            }

            long? pSourceBytes = lJobItem.LWorkSourceBytes ?? LScoutBytes.LScoutInputRead(lJobItem);
            IReadOnlyList<long> pMergeBytes = lJobItem.LWorkMergeBytes.Count > 0
                ? lJobItem.LWorkMergeBytes
                : lJobMeasure.LJobMergeRead();
            LWorkMedia? pSourceMedia = lJobItem.LWorkSourceMedia
                ?? await LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken, lJobOwner.LRunnerLevel)
                    .ConfigureAwait(false);
            LWorkMedia? pOutputMedia = await lJobMeasure.LJobOutputResolve(
                await LScout.LScoutMediaRead(lJobItem.LWorkOutputPath, lJobToken, lJobOwner.LRunnerLevel)
                    .ConfigureAwait(false),
                lJobItem.LWorkOutputPath,
                pSourceMedia).ConfigureAwait(false);
            lJobOwner.LRunnerDispatch(() =>
            {
                lJobItem.LWorkRunState.LWorkFinishTime = DateTimeOffset.Now;
                lJobItem.LWorkRunState.LWorkOutputBytes = pOutputBytes;
                lJobItem.LWorkSourceBytes = pSourceBytes;
                lJobItem.LWorkMergeBytes = pMergeBytes;
                lJobItem.LWorkSourceMedia = pSourceMedia;
                lJobItem.LWorkRunState.LWorkOutputMedia = pOutputMedia;
                lJobItem.LWorkRunState.LWorkProgress = pSucceeded ? 1 : lJobItem.LWorkRunState.LWorkProgress;
                lJobItem.LWorkRunState.LWorkStateCurrent = pTerminalState;
                lJobItem.LWorkRunState.LWorkMessage = pExitClean
                    ? lJobVerdict.LJobValidateState is null ? string.Empty : lJobVerdict.LJobValidateMessage
                    : pFailureMessage;

                lJobOwner.lRunnerTransition.LScheduleCommit(lJobItem, pSucceeded, lJobItem.LWorkRunState.LWorkMessage);
                lJobSalvage.LJobSalvageRecord();
                lJobOwner.lRunnerSchedule.LScheduleLoad();
            });

            if (pSucceeded && lJobItem.LWorkAudio.LWorkAudioActive
                && pOutputMedia is { LWorkMediaSamplerate: > 0 })
            {
                LSubsidiary.LSubsidiaryOutputDefer(lJobItem, lJobItem.LWorkOutputPath);
            }

            if (pExitCode != 0)
            {
                lJobOwner.LRunnerFailureApply();
            }
        }
        catch (OperationCanceledException) when (lJobCancelled)
        {
            LRunnerTrace.LRunnerRecord(
                $"Encode cancelled '{lJobItem.LWorkOutputName}' after {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff}; " +
                $"job kept as cancelled (restartable), continuing with the queue");
            lJobOwner.LRunnerDispatch(() =>
            {
                lJobItem.LWorkRunState.LWorkFinishTime = DateTimeOffset.Now;
                lJobOwner.lRunnerTransition.LScheduleItemCancel(lJobItem);
            });
        }
        catch (OperationCanceledException)
        {
            LRunnerTrace.LRunnerRecord(
                $"Encode stopped '{lJobItem.LWorkOutputName}' after {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff}; " +
                $"returned to the queue");
            await LJobAttemptClear().ConfigureAwait(false);
            lJobOwner.LRunnerDispatch(() => lJobOwner.lRunnerScheduleLease.LScheduleItemRelease(
                lJobItem.LWorkId, lJobOwner.LRunnerIdentity, string.Empty));
        }
        catch (Exception pException)
        {
            LRunnerTrace.LRunnerRecord(
                $"Encode failed '{lJobItem.LWorkOutputName}' after {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff}", pException);
            if (!lJobToken.IsCancellationRequested
                && await LJobRetryStart(pException.Message).ConfigureAwait(false))
            {
                return;
            }

            if (lJobItem.LWorkKind == LWorkKind.LWorkKindFix)
            {
                await lJobFile.LJobOutputClear().ConfigureAwait(false);
            }

            LJobFailureCommit(pException.Message);
        }
        finally
        {
            await LJobAttemptClear().ConfigureAwait(false);
            lJobSource.Dispose();
        }
    }

    private async Task<(int, string)> LJobStagesRun(LJobStage pStage)
    {
        if (!LEncodeSmart.LEncodeSmartCheck(lJobItem))
        {
            return await new LJobSmart(lJobOwner, lJobItem, lJobToken, pStage).LJobSmartRun().ConfigureAwait(false);
        }

        if (string.Equals(
            lJobItem.LWorkOutput.LEncodingVideo.LEncodingMode, "Smart", StringComparison.OrdinalIgnoreCase))
        {
            LRunnerTrace.LRunnerRecord(
                lJobItem.LWorkKind != LWorkKind.LWorkKindSplit
                    ? $"Smart encoding fallback for '{lJobItem.LWorkOutputName}': " +
                        $"smart encoding applies to split tabs only; encoding the full requested interval"
                    : $"Smart encoding fallback for '{lJobItem.LWorkOutputName}': " +
                        $"the item has edits or an fps change; encoding the full requested interval");
        }

        if (lJobItem.LWorkKind == LWorkKind.LWorkKindFix)
        {
            return await new LJobFix(lJobItem, lJobToken, pStage, lJobVerdict, lJobSalvage)
                .LJobFixRun().ConfigureAwait(false);
        }

        IReadOnlyList<LEncodeStage> pStages = LEncodePlan.LEncodeStagesBuild(lJobItem);
        if (pStages.Count == 0)
        {
            throw new InvalidOperationException("the stored job produced no encode steps (incomplete or corrupt)");
        }

        return await pStage.LJobBatchRun(pStages, 0, pStages.Count).ConfigureAwait(false);
    }

    private void LJobFailureCommit(string pJobFailure)
    {
        lJobOwner.LRunnerDispatch(() =>
        {
            lJobItem.LWorkRunState.LWorkFinishTime = DateTimeOffset.Now;
            lJobItem.LWorkRunState.LWorkStateCurrent = LWorkState.LWorkStateFailed;
            lJobItem.LWorkRunState.LWorkMessage = pJobFailure;
            lJobOwner.lRunnerTransition.LScheduleCommit(lJobItem, false, pJobFailure);
            lJobOwner.lRunnerSchedule.LScheduleLoad();
        });
        lJobOwner.LRunnerFailureApply();
    }

    private async Task<bool> LJobRetryStart(string pJobReason)
    {
        if (!lJobOwner.LRunnerRetryAllowed
            || lJobOwner.LRunnerRetryMaximum <= 0
            || lJobItem.LWorkRunState.LWorkRetryCount >= lJobOwner.LRunnerRetryMaximum)
        {
            return false;
        }

        await LJobAttemptClear().ConfigureAwait(false);
        int pRetry = 0;
        lJobOwner.LRunnerDispatch(() => pRetry = lJobOwner.lRunnerScheduleLease.LScheduleRetryRelease(
            lJobItem.LWorkId, lJobOwner.LRunnerIdentity, lJobOwner.LRunnerRetryMaximum, pJobReason));
        if (pRetry == 0)
        {
            return false;
        }

        LRunnerTrace.LRunnerRecord(
            $"Encode requeued '{lJobItem.LWorkOutputName}': {pJobReason} " +
            $"Retry {pRetry} of {lJobOwner.LRunnerRetryMaximum}.");
        return true;
    }

    private async Task LJobAttemptClear()
    {
        lJobOwner.LRunnerProcess.LRunnerProcessDetach(lJobItem.LWorkId);
        lJobOwner.LRunnerLease.LRunnerLeaseStop(lJobItem.LWorkId);
        await LJobFile.LJobTempClear(lJobStage?.LJobStagesDone ?? Array.Empty<LEncodeStage>())
            .ConfigureAwait(false);
        lJobReservation.LJobReservedClear();
        LEncodeJoin.LEncodeBridgeClear(lJobItem.LWorkId);
    }
}
