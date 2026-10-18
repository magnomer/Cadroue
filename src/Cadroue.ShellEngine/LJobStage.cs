using System.Diagnostics;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed class LJobStage
{
    private readonly LRunner lJobOwner;
    private readonly LWorkItem lJobItem;
    private readonly CancellationToken lJobToken;
    private readonly LJobReservation lJobReservation;
    private readonly LJobVerdict lJobVerdict;
    private readonly Stopwatch lJobClock;
    private readonly string lJobDirectory;
    private readonly double lJobRunSeconds;
    private readonly List<LEncodeStage> lJobStagesDone = new();

    internal LJobStage(
        LRunner lJobRunner,
        LWorkItem lJobWorkItem,
        CancellationToken lJobStopToken,
        LJobReservation lJobWorkReservation,
        LJobVerdict lJobWorkVerdict,
        Stopwatch lJobRunClock,
        string lJobRunDirectory,
        double lJobTotalSeconds)
    {
        lJobOwner = lJobRunner;
        lJobItem = lJobWorkItem;
        lJobToken = lJobStopToken;
        lJobReservation = lJobWorkReservation;
        lJobVerdict = lJobWorkVerdict;
        lJobClock = lJobRunClock;
        lJobDirectory = lJobRunDirectory;
        lJobRunSeconds = lJobTotalSeconds;
    }

    internal IReadOnlyList<LEncodeStage> LJobStagesDone => lJobStagesDone;

    internal async Task<(int, string)> LJobBatchRun(
        IReadOnlyList<LEncodeStage> pStages,
        int pBaseNumber,
        int pTotalCount)
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
                (pExitCode, pJobError) =
                    await LJobValidateRun(pStage, pBaseNumber + pStageIndex + 1, pTotalCount)
                        .ConfigureAwait(false);
                if (pExitCode != 0)
                {
                    break;
                }

                continue;
            }

            if (pStage.LEncodeStageKind == LWorkStage.LWorkStageRepair)
            {
                (pExitCode, pJobError) =
                    await LJobRepairRun(pStage, pBaseNumber + pStageIndex + 1, pTotalCount)
                        .ConfigureAwait(false);
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
                pStageArguments = pStageArguments.Replace(
                    LEncode.LEncodeMeasureToken, pMeasured, StringComparison.Ordinal);
            }

            (pExitCode, pJobError) = await LJobStageRun(
                pStage, pStageArguments, pBaseNumber + pStageIndex + 1, pTotalCount).ConfigureAwait(false);

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
        int pStageCount)
    {
        string pExecutableArguments = lJobOwner.LRunnerArgumentTransform?.Invoke(pStageArguments)
            ?? pStageArguments;
        LJobStageRaise(pStage, pStageNumber, pStageCount);
        LRunnerTrace.LRunnerRecord(
            $"{pStage.LEncodeStageLabel} '{lJobItem.LWorkOutputName}': " +
            $"{lJobOwner.LRunnerProgramPath} {pExecutableArguments}");
        LRunnerTrace.LRunnerFfmpegRecord(
            $"{pStage.LEncodeStageLabel} command for '{lJobItem.LWorkOutputName}'",
            $"{lJobOwner.LRunnerProgramPath} {pExecutableArguments}\n"
            + $"working folder {(string.IsNullOrWhiteSpace(lJobDirectory) ? "(process default)" : lJobDirectory)}\n"
            + $"source {lJobItem.LWorkSourcePath}\n"
            + $"output {pStage.LEncodeStagePath}");

        var pProgress = new LJobProgress(lJobOwner, lJobItem, lJobRunSeconds);

        Guid pWorkId = lJobItem.LWorkId;
        lJobOwner.LRunnerProcess.LRunnerProcessAttach(lJobItem);
        LEmployerResult pJobResult = await LEmployerEngine.LEmployerRunSeam(
            lJobOwner.LRunnerProgramPath,
            lJobOwner.LRunnerArgumentPrefix,
            LCustodyFamily.LCustodyFamilyEncode,
            lJobOwner.LRunnerLevel,
            null,
            pExecutableArguments,
            lJobToken,
            pWorkId,
            pProgress.LJobOutputRead,
            pProgress.LJobStderrRead).ConfigureAwait(false);

        LRunnerTrace.LRunnerFfmpegRecord(
            $"Exit code {pJobResult.LEmployerExit} for '{lJobItem.LWorkOutputName}' [{pStage.LEncodeStageLabel}]",
            $"ran for {lJobClock.Elapsed:hh\\:mm\\:ss\\.fff}");

        lJobOwner.LRunnerProcess.LRunnerProcessDetach(pWorkId);
        return (pJobResult.LEmployerExit, pJobResult.LEmployerError);
    }

    private void LJobStageRaise(LEncodeStage pStage, int pStageNumber, int pStageCount)
    {
        lJobOwner.LRunnerDispatch(() =>
        {
            lJobItem.LWorkRunState.LWorkProgress = 0;
            lJobItem.LWorkRunState.LWorkStageCurrent = pStage.LEncodeStageKind;
            lJobItem.LWorkRunState.LWorkMessage =pStageCount > 1
                ? $"Stage {pStageNumber}/{pStageCount}: {pStage.LEncodeStageLabel}"
                : string.Empty;
            lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeStatus);
        });
    }

    private void LJobProgressRaise(double pProgress)
    {
        lJobOwner.LRunnerDispatch(() =>
        {
            lJobItem.LWorkRunState.LWorkProgress = pProgress;
            lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeProgress);
        });
    }

    private (int, string) LJobCopyRun(LEncodeStage pStage, int pStageNumber, int pStageCount)
    {
        string pSource = pStage.LEncodeStageArguments;
        string pOutput = pStage.LEncodeStagePath;
        LJobStageRaise(pStage, pStageNumber, pStageCount);

        try
        {
            long pSourceBytes = new FileInfo(pSource).Length;
            File.Copy(pSource, pOutput, true);
            long pOutputBytes = new FileInfo(pOutput).Length;
            LRunnerTrace.LRunnerRecord(
                $"Copying '{lJobItem.LWorkOutputName}': copied {pSourceBytes:N0} bytes from " +
                $"'{Path.GetFileName(pSource)}' to '{pOutput}' ({pOutputBytes:N0} bytes)");
            LJobProgressRaise(1);
            return (0, string.Empty);
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
        {
            LRunnerTrace.LRunnerRecord($"Copy failed for '{lJobItem.LWorkOutputName}'", pException);
            return (1, pException.Message);
        }
    }

    private (int, string) LJobLeadingRun(LEncodeStage pStage)
    {
        try
        {
            byte[] pBytes = File.ReadAllBytes(pStage.LEncodeStagePath);
            if (LBridgeLeading.LBridgeLeadingNormalize(pBytes))
            {
                File.WriteAllBytes(pStage.LEncodeStagePath, pBytes);
                LRunnerTrace.LRunnerRecord(
                    $"Smart encoding neutralized the copied middle's leading keyframe " +
                    $"for '{lJobItem.LWorkOutputName}'");
            }

            return (0, string.Empty);
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
        {
            LRunnerTrace.LRunnerRecord(
                $"Smart encoding could not adjust the copied middle for '{lJobItem.LWorkOutputName}'", pException);
            return (1, "copied middle could not be adjusted");
        }
    }

    private async Task<(int, string)> LJobRepairRun(LEncodeStage pStage, int pStageNumber, int pStageCount)
    {
        string pOutput = pStage.LEncodeStagePath;
        string pTemp = lJobReservation.LJobPathResolve(pOutput, ".cadfix");
        string pArguments = LEncodeFix.LEncodeRepairBuild(
            pOutput, pStage.LEncodeStageInput, pStage.LEncodeStageArguments, pTemp);
        LEncodeStage pRepairStage = pStage with { LEncodeStagePath = pTemp };

        (int pExit, string pError) = await LJobStageRun(
            pRepairStage, pArguments, pStageNumber, pStageCount).ConfigureAwait(false);
        if (pExit != 0)
        {
            return (pExit, pError);
        }

        try
        {
            File.Move(pTemp, pOutput, true);
            LRunnerTrace.LRunnerRecord(
                $"Repairing '{lJobItem.LWorkOutputName}': applied '{pStage.LEncodeStageArguments}' " +
                $"to '{Path.GetFileName(pOutput)}'");
            return (0, string.Empty);
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
        {
            LRunnerTrace.LRunnerRecord(
                $"Repair could not replace the output for '{lJobItem.LWorkOutputName}'", pException);
            return (1, pException.Message);
        }
    }

    private async Task<(int, string)> LJobValidateRun(LEncodeStage pStage, int pStageNumber, int pStageCount)
    {
        LJobStageRaise(pStage, pStageNumber, pStageCount);
        await lJobVerdict.LJobVerdictResolve(pStage.LEncodeStagePath).ConfigureAwait(false);
        LJobProgressRaise(1);
        return (0, string.Empty);
    }
}
