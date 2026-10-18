using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed class LJobFix
{
    private const int LJobPassMax = 2;

    private readonly LWorkItem lJobItem;
    private readonly CancellationToken lJobToken;
    private readonly LJobStage lJobStage;
    private readonly LJobVerdict lJobVerdict;
    private readonly LJobSalvage lJobSalvage;

    internal LJobFix(
        LWorkItem lJobWorkItem,
        CancellationToken lJobStopToken,
        LJobStage lJobWorkStage,
        LJobVerdict lJobWorkVerdict,
        LJobSalvage lJobWorkSalvage)
    {
        lJobItem = lJobWorkItem;
        lJobToken = lJobStopToken;
        lJobStage = lJobWorkStage;
        lJobVerdict = lJobWorkVerdict;
        lJobSalvage = lJobWorkSalvage;
    }

    internal async Task<(int, string)> LJobFixRun()
    {
        await LJobDossiersCreate().ConfigureAwait(false);

        (int pFixExit, string pFixError) = await LJobPassRun().ConfigureAwait(false);

        await lJobSalvage.LJobSalvageRun(lJobVerdict.LJobValidateState).ConfigureAwait(false);

        return (pFixExit, pFixError);
    }

    private async Task<(int, string)> LJobPassRun()
    {
        IReadOnlyList<LDossier> pRepairable =
            LFixPlan.LFixRepairResolve(lJobItem.LWorkDossiers, lJobItem.LWorkFixPlan);
        int pExit = 0;
        string pError = string.Empty;
        HashSet<LFlawKind>? pPrevRemaining = null;

        for (int pPass = 0; ; pPass++)
        {
            IReadOnlyList<LEncodeStage> pStages =
                LEncodeFix.LEncodeFixBuild(lJobItem, pRepairable, pPass == 0);
            (pExit, pError) = await lJobStage.LJobBatchRun(pStages, 0, pStages.Count).ConfigureAwait(false);
            if (pExit != 0)
            {
                return (pExit, pError);
            }

            if (lJobVerdict.LJobValidateState == LWorkState.LWorkStateDone || pPass + 1 >= LJobPassMax)
            {
                break;
            }

            IReadOnlyList<LDossier> pRescan =
                await LFlawScanner.LFlawScanRun(lJobItem.LWorkOutputPath, Array.Empty<LFlawKind>(), lJobToken)
                    .ConfigureAwait(false);
            var pRemaining = LFixPlan.LFixRepairResolve(pRescan, lJobItem.LWorkFixPlan)
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

            LRunnerTrace.LRunnerRecord(
                $"Fix recompose for '{lJobItem.LWorkOutputName}': " +
                $"{pRemaining.Count} defect(s) still present after pass {pPass + 1}; repairing again");
            pPrevRemaining = pRemainingKinds;
            pRepairable = pRemaining;
        }

        return (pExit, pError);
    }

    private async Task LJobDossiersCreate()
    {
        if (lJobItem.LWorkDossiers.Count > 0)
        {
            return;
        }

        IReadOnlyList<LDossier>? pCached = LCheckupEngine.LCheckupReadSeam(lJobItem.LWorkSourcePath);
        if (pCached is not null)
        {
            lJobItem.LWorkDossiers = pCached;
            return;
        }

        IReadOnlyList<LDossier> pScanned = await LFlawScanner.LFlawScanRun(lJobItem, lJobToken).ConfigureAwait(false);
        await LCheckupEngine.LCheckupSaveSeam(lJobItem.LWorkSourcePath, pScanned).ConfigureAwait(false);
        lJobItem.LWorkDossiers = pScanned;
    }
}
