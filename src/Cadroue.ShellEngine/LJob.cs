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
        var pJobClock = Stopwatch.StartNew();
        lJobClock = pJobClock;

        try
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
                lJobOwner.LRunnerFailureApply();
                return;
            }

            lJobOwner.lRunnerSchedule.LScheduleOutputCommit(
                lJobItem.LWorkId, lJobOwner.LRunnerIdentity, lJobItem.LWorkOutputPath, lJobItem.LWorkOutputName);

            lJobItem.LWorkSourceMedia ??= LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken);

            lJobDirectory = pDirectory;
            lJobItem.LWorkStartTime = DateTimeOffset.Now;
            lJobItem.LWorkFinishTime = null;
            LRunner.LRunnerRecord(
                $"Encode started '{lJobItem.LWorkOutputName}': {lJobItem.LWorkKind} at {lJobItem.LWorkPriority}, " +
                $"{lJobItem.LWorkOrigin:hh\\:mm\\:ss\\.fff}-{lJobItem.LWorkEnd:hh\\:mm\\:ss\\.fff} " +
                $"from '{Path.GetFileName(lJobItem.LWorkSourcePath)}' to '{lJobItem.LWorkOutputPath}'");

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

            if (lJobItem.LWorkKind == LWorkKind.LWorkKindFix && !pSucceeded)
            {
                LJobOutputClear();
            }

            long? pOutputBytes = LScout.LScoutBytesRead(lJobItem.LWorkOutputPath);
            long? pSourceBytes = lJobItem.LWorkSourceBytes ?? LScout.LScoutInputRead(lJobItem, lJobToken);
            IReadOnlyList<long> pMergeBytes = lJobItem.LWorkMergeBytes.Count > 0
                ? lJobItem.LWorkMergeBytes
                : LJobMergeRead();
            LWorkMedia? pSourceMedia = lJobItem.LWorkSourceMedia
                ?? LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken);
            LWorkMedia? pOutputMedia = LJobOutputResolve(
                LScout.LScoutMediaRead(lJobItem.LWorkOutputPath, lJobToken),
                lJobItem.LWorkOutputPath,
                pSourceMedia);
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
}
