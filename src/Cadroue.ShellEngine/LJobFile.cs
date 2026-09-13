using System.IO;
using System.Linq;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

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

    private string LJobPathResolve(string pPath, string pSuffix)
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
            if (LJobClaim(pCandidate))
            {
                return pCandidate;
            }
        }
    }

    private bool LJobClaim(string pPath)
    {
        try
        {
            using (new FileStream(pPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
            }

            lJobReserved.Add(pPath);
            return true;
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void LJobReservedClear()
    {
        foreach (string pPath in lJobReserved)
        {
            try
            {
                if (File.Exists(pPath) && new FileInfo(pPath).Length == 0)
                {
                    File.Delete(pPath);
                }
            }
            catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
            {
            }
        }

        lJobReserved.Clear();
    }

    private void LJobOutputClear()
    {
        string pOutput = lJobItem.LWorkOutputPath;
        if (string.IsNullOrWhiteSpace(pOutput))
        {
            return;
        }

        bool pPreExisting = LJobCollisionCheck(pOutput, LJobInputsRead())
            || (lJobFinalPath.Length > 0 && string.Equals(
                Path.GetFullPath(pOutput),
                Path.GetFullPath(lJobFinalPath),
                StringComparison.OrdinalIgnoreCase));
        if (pPreExisting)
        {
            LRunner.LRunnerRecord($"Preserved '{Path.GetFileName(pOutput)}'; the unresolved Fix output is a pre-existing file, not this job's own output");
            return;
        }

        for (int pAttempt = 0; pAttempt < 5; pAttempt++)
        {
            try
            {
                if (!File.Exists(pOutput))
                {
                    return;
                }

                File.Delete(pOutput);
                LRunner.LRunnerRecord($"Discarded the unresolved Fix output '{Path.GetFileName(pOutput)}'");
                return;
            }
            catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
            {
                System.Threading.Thread.Sleep(200);
            }
        }

        LRunner.LRunnerRecord($"Could not delete the unresolved Fix output '{pOutput}'; it may remain on disk.", null);
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
}
