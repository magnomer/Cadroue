using System.Diagnostics;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
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
}
