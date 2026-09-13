using System.IO;
using System.Linq;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    private const int LJobPassMax = 2;

    private async Task<(int, string)> LJobFixRun()
    {
        LJobDossiersCreate();

        (int pFixExit, string pFixError) = await LJobPassRun().ConfigureAwait(false);

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

            if (lJobValidateState == LWorkState.LWorkStateDone || pPass + 1 >= LJobPassMax)
            {
                break;
            }

            IReadOnlyList<LDossier> pRescan =
                LFlawScan.LFlawScanRun(lJobItem.LWorkOutputPath, Array.Empty<LFlawKind>(), lJobToken);
            var pRemaining = LFix.LFixRepairResolve(pRescan, lJobItem.LWorkFixPlan)
                .Where(pDossier => pDossier.LDossierRepair != LFlawFfvone.LFlawReport)
                .ToList();
            if (pRemaining.Count == 0)
            {
                break;
            }

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
}
