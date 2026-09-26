using System.Diagnostics;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    private readonly LRunner lJobOwner;
    private readonly LWorkItem lJobItem;
    private readonly CancellationTokenSource lJobSource;
    private readonly CancellationToken lJobToken;
    private volatile bool lJobCancelled;

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

    internal LJob(LRunner lJobRunner, LWorkItem lJobWorkItem, CancellationToken lJobStopToken)
    {
        lJobOwner = lJobRunner;
        lJobItem = lJobWorkItem;
        lJobSource = CancellationTokenSource.CreateLinkedTokenSource(lJobStopToken);
        lJobToken = lJobSource.Token;
    }

    internal LWorkItem LJobItem => lJobItem;

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
        lJobClock = pJobClock;
        if (lJobOwner.lRunnerCancelled.TryRemove(lJobItem.LWorkId, out _))
        {
            LJobCancel();
        }

        try
        {
            string pJobInvalid = await LJobValidate().ConfigureAwait(false);
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
                lJobOwner.LRunnerFailureApply();
                return;
            }

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
                lJobOwner.LRunnerFailureApply();
                return;
            }

            lJobOwner.lRunnerSchedule.LScheduleOutputCommit(
                lJobItem.LWorkId, lJobOwner.LRunnerIdentity, lJobItem.LWorkOutputPath, lJobItem.LWorkOutputName);

            lJobItem.LWorkSourceMedia ??=
                await LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken).ConfigureAwait(false);

            lJobDirectory = pDirectory;
            lJobItem.LWorkStartTime = DateTimeOffset.Now;
            lJobItem.LWorkFinishTime = null;
            LRunner.LRunnerRecord(
                $"Encode started '{lJobItem.LWorkOutputName}': {lJobItem.LWorkKind} at {lJobItem.LWorkPriority}, " +
                $"{lJobItem.LWorkOrigin:hh\\:mm\\:ss\\.fff}-{lJobItem.LWorkEnd:hh\\:mm\\:ss\\.fff} " +
                $"from '{Path.GetFileName(lJobItem.LWorkSourcePath)}' to '{lJobItem.LWorkOutputPath}'");

            double pTotalSeconds = lJobItem.LWorkKind switch
            {
                LWorkKind.LWorkKindAudio =>
                    (await LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken).ConfigureAwait(false))
                        ?.LWorkMediaDuration.TotalSeconds ?? 0,
                LWorkKind.LWorkKindMerge =>
                    await LScout.LScoutMergeRead(lJobItem.LWorkMergeSources, lJobToken).ConfigureAwait(false),
                _ => lJobItem.LWorkDuration.TotalSeconds
            };

            if (pTotalSeconds <= 0 && lJobItem.LWorkKind != LWorkKind.LWorkKindMerge)
            {
                pTotalSeconds = (lJobItem.LWorkSourceMedia
                    ?? await LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken).ConfigureAwait(false))
                    ?.LWorkMediaDuration.TotalSeconds ?? 0;
            }

            lJobRunSeconds = pTotalSeconds;
            (int pExitCode, string pJobError) = await LJobStagesRun().ConfigureAwait(false);
            lJobToken.ThrowIfCancellationRequested();

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
                    : $"FFmpeg exited with code {pExitCode}. {LJobTailShorten(pTail)}";

                if (await LJobRetryStart(pFailureMessage).ConfigureAwait(false))
                {
                    return;
                }
            }

            bool pExitClean = pExitCode == 0;
            LWorkState pTerminalState = pExitClean
                ? lJobValidateState ?? LWorkState.LWorkStateDone
                : LWorkState.LWorkStateFailed;
            long? pOutputBytes = LScout.LScoutBytesRead(lJobItem.LWorkOutputPath);
            if (pTerminalState == LWorkState.LWorkStateDone && pOutputBytes is not > 0)
            {
                pExitClean = false;
                pTerminalState = LWorkState.LWorkStateFailed;
                pFailureMessage = pOutputBytes is null
                    ? "FFmpeg reported success but the output file was not produced."
                    : "FFmpeg reported success but the output file is empty.";
                LRunner.LRunnerRecord($"Encode failed '{lJobItem.LWorkOutputName}': {pFailureMessage}");
            }

            bool pSucceeded = pTerminalState == LWorkState.LWorkStateDone;

            if (lJobItem.LWorkKind == LWorkKind.LWorkKindFix && !pSucceeded)
            {
                await LJobOutputClear().ConfigureAwait(false);
            }

            long? pSourceBytes = lJobItem.LWorkSourceBytes ?? LScout.LScoutInputRead(lJobItem, lJobToken);
            IReadOnlyList<long> pMergeBytes = lJobItem.LWorkMergeBytes.Count > 0
                ? lJobItem.LWorkMergeBytes
                : LJobMergeRead();
            LWorkMedia? pSourceMedia = lJobItem.LWorkSourceMedia
                ?? await LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken).ConfigureAwait(false);
            LWorkMedia? pOutputMedia = await LJobOutputResolve(
                await LScout.LScoutMediaRead(lJobItem.LWorkOutputPath, lJobToken).ConfigureAwait(false),
                lJobItem.LWorkOutputPath,
                pSourceMedia).ConfigureAwait(false);
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
            LRunner.LRunnerRecord(
                $"Encode cancelled '{lJobItem.LWorkOutputName}' after {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff}; " +
                $"job kept as cancelled (restartable), continuing with the queue");
            lJobOwner.LRunnerDispatch(() =>
            {
                lJobItem.LWorkFinishTime = DateTimeOffset.Now;
                lJobOwner.lRunnerSchedule.LScheduleItemCancel(lJobItem);
            });
        }
        catch (OperationCanceledException)
        {
            LRunner.LRunnerRecord(
                $"Encode stopped '{lJobItem.LWorkOutputName}' after {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff}; " +
                $"returned to the queue");
            await LJobAttemptClear().ConfigureAwait(false);
            lJobOwner.LRunnerDispatch(() => lJobOwner.lRunnerSchedule.LScheduleItemRelease(
                lJobItem.LWorkId, lJobOwner.LRunnerIdentity, string.Empty));
        }
        catch (Exception pException)
        {
            LRunner.LRunnerRecord(
                $"Encode failed '{lJobItem.LWorkOutputName}' after {pJobClock.Elapsed:hh\\:mm\\:ss\\.fff}", pException);
            if (!lJobToken.IsCancellationRequested
                && await LJobRetryStart(pException.Message).ConfigureAwait(false))
            {
                return;
            }

            if (lJobItem.LWorkKind == LWorkKind.LWorkKindFix)
            {
                await LJobOutputClear().ConfigureAwait(false);
            }

            lJobOwner.LRunnerDispatch(() =>
            {
                lJobItem.LWorkFinishTime = DateTimeOffset.Now;
                lJobItem.LWorkStateCurrent = LWorkState.LWorkStateFailed;
                lJobItem.LWorkMessage = pException.Message;
                lJobOwner.lRunnerSchedule.LScheduleCommit(lJobItem, false, pException.Message);
                lJobOwner.lRunnerSchedule.LScheduleLoad();
            });

            lJobOwner.LRunnerFailureApply();
        }
        finally
        {
            await LJobAttemptClear().ConfigureAwait(false);
            lJobSource.Dispose();
        }
    }

    private async Task LJobAttemptClear()
    {
        lJobOwner.lRunnerProcesses.TryRemove(lJobItem.LWorkId, out _);
        lJobOwner.LRunnerLeaseStop(lJobItem.LWorkId);
        await LJobTempClear(lJobStagesDone).ConfigureAwait(false);
        LJobReservedClear();
        LEncode.LEncodeBridgeClear(lJobItem.LWorkId);
    }
}
