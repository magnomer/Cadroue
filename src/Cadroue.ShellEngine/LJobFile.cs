using System.IO;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    private (int, string) LJobCopyRun(LEncodeStage pStage, int pStageNumber, int pStageCount)
    {
        string pSource = pStage.LEncodeStageArguments;
        string pOutput = pStage.LEncodeStagePath;
        lJobOwner.LRunnerDispatch(() =>
        {
            lJobItem.LWorkProgress = 0;
            lJobItem.LWorkStageCurrent = pStage.LEncodeStageKind;
            lJobItem.LWorkMessage = pStageCount > 1
                ? $"Stage {pStageNumber}/{pStageCount}: {pStage.LEncodeStageLabel}"
                : string.Empty;
            lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeStatus);
        });

        try
        {
            long pSourceBytes = new FileInfo(pSource).Length;
            File.Copy(pSource, pOutput, true);
            long pOutputBytes = new FileInfo(pOutput).Length;
            LRunner.LRunnerRecord(
                $"Copying '{lJobItem.LWorkOutputName}': copied {pSourceBytes:N0} bytes from " +
                $"'{Path.GetFileName(pSource)}' to '{pOutput}' ({pOutputBytes:N0} bytes)");
            lJobOwner.LRunnerDispatch(() =>
            {
                lJobItem.LWorkProgress = 1;
                lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeProgress);
            });
            return (0, string.Empty);
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
        {
            LRunner.LRunnerRecord($"Copy failed for '{lJobItem.LWorkOutputName}'", pException);
            return (1, pException.Message);
        }
    }

    private (int, string) LJobValidateRun(LEncodeStage pStage, int pStageNumber, int pStageCount)
    {
        string pOutput = pStage.LEncodeStagePath;
        lJobOwner.LRunnerDispatch(() =>
        {
            lJobItem.LWorkProgress = 0;
            lJobItem.LWorkStageCurrent = pStage.LEncodeStageKind;
            lJobItem.LWorkMessage = pStageCount > 1
                ? $"Stage {pStageNumber}/{pStageCount}: {pStage.LEncodeStageLabel}"
                : string.Empty;
            lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeStatus);
        });

        LWorkMedia? pOutputMedia = LScout.LScoutMediaRead(pOutput, lJobToken);
        if (pOutputMedia is null)
        {
            lJobValidateState = LWorkState.LWorkStateUnresolved;
            lJobValidateMessage = "Validation: the repaired output could not be re-probed; the defect is still present.";
        }
        else if (!LScout.LScoutDecodeCheck(pOutput, lJobToken))
        {
            lJobValidateState = LWorkState.LWorkStatePartial;
            lJobValidateMessage = "Validation: the output re-probes but still reports decode errors; damage was reduced, not resolved.";
        }
        else
        {
            lJobValidateState = LWorkState.LWorkStateDone;
            lJobValidateMessage = string.Empty;
        }

        LRunner.LRunnerRecord(
            $"Validating '{lJobItem.LWorkOutputName}': re-probed '{Path.GetFileName(pOutput)}' → {lJobValidateState}");
        lJobOwner.LRunnerDispatch(() =>
        {
            lJobItem.LWorkProgress = 1;
            lJobOwner.lRunnerSchedule.LScheduleItemRaise(lJobItem, LScheduleNotice.LScheduleNoticeProgress);
        });
        return (0, string.Empty);
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
