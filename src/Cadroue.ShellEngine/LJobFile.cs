using System.IO;
using System.Linq;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    // Total Fix repair passes over one source: the initial pass plus recompose passes.
    // A repair can resolve a defect a lower-precedence repair was also going to address,
    // or expose one the first scan could not see past the first defect. So after each
    // pass the output is re-scanned and the still-warranted repairs are recomposed and
    // run again — bounded here so a defect that never clears cannot loop forever.
    private const int LJobPassMax = 2;

    private async Task<(int, string)> LJobFixRun()
    {
        LJobDossiersCreate();

        (int pFixExit, string pFixError) = await LJobPassRun().ConfigureAwait(false);

        // Salvage is the last pass: it harvests the readable spans and extracts each as a
        // valid standalone file, failing safe so nothing partial is left behind. The
        // recovered paths are held for the terminal outcome to record as delivered derived
        // outputs (LJobSalvageRecord). What it reads and whether it runs depend on the plan:
        //   - No repair step selected: salvage is the only work, always run from the source.
        //   - From source: recover from the original source, but only when the repair did not
        //     fully succeed (any state other than Done counts as failed).
        //   - From fixed result: always recover, reading the repaired output (falling back to
        //     the source when the repair produced no output).
        LWorkFixSalvage pSalvage = lJobItem.LWorkFixPlan.LWorkFixSalvage;
        if (pSalvage.LWorkSalvageActive)
        {
            lJobToken.ThrowIfCancellationRequested();
            bool pHasRepair = lJobItem.LWorkFixPlan.LWorkFixSteps.Any(pStep => pStep.LWorkFixRepair);
            LSalvageBasis pBasis = pHasRepair
                ? pSalvage.LWorkSalvageBasis
                : LSalvageBasis.LSalvageBasisSource;
            bool pFromFixed = pBasis == LSalvageBasis.LSalvageBasisFixed;
            bool pRun = !pHasRepair || pFromFixed || lJobValidateState != LWorkState.LWorkStateDone;
            if (pRun)
            {
                string pInput = pFromFixed && File.Exists(lJobItem.LWorkOutputPath)
                    ? lJobItem.LWorkOutputPath
                    : lJobItem.LWorkSourcePath;
                IReadOnlyList<LSalvageSpan> pSpans =
                    await LSalvageScan.LSalvageScanRun(pInput, lJobToken).ConfigureAwait(false);
                IReadOnlyList<string> pSalvaged =
                    await LSalvageExtract.LSalvageExtractRun(lJobItem, pInput, pSpans, lJobToken).ConfigureAwait(false);
                if (pSalvaged.Count > 0)
                {
                    lJobSalvaged = pSalvaged;
                    string pFrom = pInput == lJobItem.LWorkOutputPath ? "repaired result" : "original source";
                    LRunner.LRunnerRecord(
                        $"Salvage recovered {pSalvaged.Count} output(s) for '{lJobItem.LWorkOutputName}' from the {pFrom}");
                }
            }
        }

        return (pFixExit, pFixError);
    }

    private async Task<(int, string)> LJobPassRun()
    {
        IReadOnlyList<LDossier> pRepairable =
            LFix.LFixRepairResolve(lJobItem.LWorkDossiers, lJobItem.LWorkFixPlan);
        int pExit = 0;
        string pError = string.Empty;
        HashSet<LFlawKind>? pPrevRemaining = null;

        for (int pPass = 0; ; pPass++)
        {
            IReadOnlyList<LEncodeStage> pStages =
                LEncode.LEncodeFixBuild(lJobItem, pRepairable, pPass == 0);
            (pExit, pError) = await LJobBatchRun(pStages, 0, pStages.Count).ConfigureAwait(false);
            if (pExit != 0)
            {
                return (pExit, pError);
            }

            // Validation cleared the file, or the pass budget is spent: stop here and let
            // the final validation state stand as this job's outcome.
            if (lJobValidateState == LWorkState.LWorkStateDone || pPass + 1 >= LJobPassMax)
            {
                break;
            }

            // Re-scan the repaired output and keep only the correctable repairs the user
            // asked for; a report-only FFV1 dossier is never re-run.
            IReadOnlyList<LDossier> pRescan =
                LFlawScan.LFlawScanRun(lJobItem.LWorkOutputPath, Array.Empty<LFlawKind>(), lJobToken);
            var pRemaining = LFix.LFixRepairResolve(pRescan, lJobItem.LWorkFixPlan)
                .Where(pDossier => pDossier.LDossierRepair != LFlawFfvone.LFlawReport)
                .ToList();
            if (pRemaining.Count == 0)
            {
                break;
            }

            // Only recompose when the correctable set actually changed; an unchanged set
            // would repeat the same repairs to the same effect and never converge.
            var pRemainingKinds = pRemaining.Select(pDossier => pDossier.LDossierKind).ToHashSet();
            if (pPrevRemaining is not null && pRemainingKinds.SetEquals(pPrevRemaining))
            {
                break;
            }

            LRunner.LRunnerRecord(
                $"Fix recompose for '{lJobItem.LWorkOutputName}': " +
                $"{pRemaining.Count} defect(s) still present after pass {pPass + 1}; repairing again");
            pPrevRemaining = pRemainingKinds;
            pRepairable = pRemaining;
        }

        return (pExit, pError);
    }

    private void LJobDossiersCreate()
    {
        if (lJobItem.LWorkDossiers.Count > 0)
        {
            return;
        }

        IReadOnlyList<LDossier>? pCached = LCheckup.LCheckupCachedRead(lJobItem.LWorkSourcePath);
        if (pCached is not null)
        {
            lJobItem.LWorkDossiers = pCached;
            return;
        }

        IReadOnlyList<LDossier> pScanned = LFlawScan.LFlawScanRun(lJobItem, lJobToken);
        LCheckup.LCheckupCachedSave(lJobItem.LWorkSourcePath, pScanned);
        lJobItem.LWorkDossiers = pScanned;
    }

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

    private async Task<(int, string)> LJobRepairRun(LEncodeStage pStage, int pStageNumber, int pStageCount)
    {
        string pOutput = pStage.LEncodeStagePath;
        string pTemp = LJobPathResolve(pOutput, ".cadfix");
        string pArguments = LEncode.LEncodeRepairBuild(
            pOutput, pStage.LEncodeStageInput, pStage.LEncodeStageArguments, pTemp);
        LEncodeStage pRepairStage = pStage with { LEncodeStagePath = pTemp };

        (int pExit, string pError) = await LJobStageRun(
            pRepairStage, pArguments, pStageNumber, pStageCount, lJobRunSeconds,
            lJobClock, lJobDirectory).ConfigureAwait(false);
        if (pExit != 0)
        {
            return (pExit, pError);
        }

        try
        {
            File.Move(pTemp, pOutput, true);
            LRunner.LRunnerRecord(
                $"Repairing '{lJobItem.LWorkOutputName}': applied '{pStage.LEncodeStageArguments}' to '{Path.GetFileName(pOutput)}'");
            return (0, string.Empty);
        }
        catch (Exception pException) when (pException is IOException or UnauthorizedAccessException)
        {
            LRunner.LRunnerRecord($"Repair could not replace the output for '{lJobItem.LWorkOutputName}'", pException);
            return (1, pException.Message);
        }
    }

    private async Task<(int, string)> LJobValidateRun(LEncodeStage pStage, int pStageNumber, int pStageCount)
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
        else if (lJobItem.LWorkSourceMedia is { LWorkMediaVideo: true } && !pOutputMedia.LWorkMediaVideo)
        {
            lJobValidateState = LWorkState.LWorkStateBlocked;
            lJobValidateMessage = "Validation: the repaired output has no principal video content; recovery is blocked.";
        }
        else if (!await LScout.LScoutDecodeCheck(lJobOwner, pOutput, lJobToken).ConfigureAwait(false))
        {
            lJobValidateState = LWorkState.LWorkStatePartial;
            lJobValidateMessage = "Validation: the output re-probes but still reports decode errors; damage was reduced, not resolved.";
        }
        else
        {
            lJobValidateState = LWorkState.LWorkStateDone;
            lJobValidateMessage = string.Empty;
        }

        // A report-only defect (FFV1 slice-CRC mismatch) cannot be corrected: the
        // output is a faithful copy, not a repair. Never let it read as resolved.
        // Only defects the plan asked to repair gate the outcome; a detected defect
        // the user left unselected is out of this job's scope.
        IReadOnlyList<LDossier> pRepairable =
            LFix.LFixRepairResolve(lJobItem.LWorkDossiers, lJobItem.LWorkFixPlan);
        if (lJobValidateState == LWorkState.LWorkStateDone
            && pRepairable.Any(pDossier => pDossier.LDossierRepair == LFlawFfvone.LFlawReport))
        {
            lJobValidateState = LWorkState.LWorkStateUnresolved;
            lJobValidateMessage = "Validation: FFV1 slice-CRC mismatch confirmed; the defect is detected but cannot be corrected. The file was copied unchanged.";
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
            if (LJobReserve(pCandidate))
            {
                return pCandidate;
            }
        }
    }

    // Atomically claim a path so no other process (or job) can take the same name:
    // an exclusive create is the OS-level compare-and-swap that closes the choose-then-
    // write race. The 0-byte placeholder is the reservation; the encode overwrites it
    // (ffmpeg runs with -y -nostdin), and any placeholder never written over is removed
    // by LJobReservedClear once the job ends.
    private bool LJobReserve(string pPath)
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

    // Remove reservation placeholders the encode never wrote into: a real output has
    // bytes, so an empty reserved file is a placeholder left behind by a failed, cancelled
    // or skipped job. Never touches a file that received content.
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

        // Never delete a file this job did not create. The recorded output can coincide
        // with a user-owned file: an input (source == output), or the pre-existing
        // collision target the encode staged around (lJobFinalPath). Preserve those.
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
