using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed class LJobSalvage
{
    private readonly LRunner lJobOwner;
    private readonly LWorkItem lJobItem;
    private readonly CancellationToken lJobToken;
    private IReadOnlyList<string> lJobSalvaged = Array.Empty<string>();

    internal LJobSalvage(LRunner lJobRunner, LWorkItem lJobWorkItem, CancellationToken lJobStopToken)
    {
        lJobOwner = lJobRunner;
        lJobItem = lJobWorkItem;
        lJobToken = lJobStopToken;
    }

    internal async Task LJobSalvageRun(LWorkState? pValidateState)
    {
        LWorkFixSalvage pSalvage = lJobItem.LWorkFixPlan.LWorkFixSalvage;
        if (!pSalvage.LWorkSalvageActive)
        {
            return;
        }

        lJobToken.ThrowIfCancellationRequested();
        bool pHasRepair = lJobItem.LWorkFixPlan.LWorkFixSteps.Any(pStep => pStep.LWorkFixRepair);
        LSalvageBasis pBasis = pHasRepair
            ? pSalvage.LWorkSalvageBasis
            : LSalvageBasis.LSalvageBasisSource;
        bool pFromFixed = pBasis == LSalvageBasis.LSalvageBasisFixed;
        bool pRun = !pHasRepair || pFromFixed || pValidateState != LWorkState.LWorkStateDone;
        if (!pRun)
        {
            return;
        }

        string pInput = pFromFixed && File.Exists(lJobItem.LWorkOutputPath)
            ? lJobItem.LWorkOutputPath
            : lJobItem.LWorkSourcePath;
        IReadOnlyList<LSalvageSpan> pSpans =
            await LSalvageScanner.LSalvageScanRun(pInput, lJobToken).ConfigureAwait(false);
        IReadOnlyList<string> pSalvaged =
            await LSalvageExtract.LSalvageExtractRun(lJobItem, pInput, pSpans, lJobToken).ConfigureAwait(false);
        if (pSalvaged.Count > 0)
        {
            lJobSalvaged = pSalvaged;
            string pFrom = pInput == lJobItem.LWorkOutputPath ? "repaired result" : "original source";
            LRunnerTrace.LRunnerRecord(
                $"Salvage recovered {pSalvaged.Count} output(s) for '{lJobItem.LWorkOutputName}' " +
                $"from the {pFrom}");
        }
    }

    internal void LJobSalvageRecord()
    {
        if (lJobSalvaged.Count == 0)
        {
            return;
        }

        Guid pLineage = lJobItem.LWorkLineage != Guid.Empty
            ? lJobItem.LWorkLineage
            : LLineageEngine.LLineageWorkSeam(lJobItem);

        var pItems = new List<LWorkItem>(lJobSalvaged.Count);
        foreach (string pPath in lJobSalvaged)
        {
            long pBytes = LScoutBytes.LScoutBytesRead(pPath) ?? 0;
            pItems.Add(LJobSalvageCreate(pPath, pBytes, pLineage));
        }

        lJobOwner.lRunnerIntake.LScheduleDeliveredAdd(pItems);
    }

    private LWorkItem LJobSalvageCreate(string pPath, long pBytes, Guid pLineage) =>
        new(
            lJobItem.LWorkBatchId,
            LWorkKind.LWorkKindFix,
            lJobItem.LWorkPriority,
            lJobItem.LWorkSourcePath,
            lJobItem.LWorkOrigin,
            lJobItem.LWorkEnd,
            Path.GetFileName(pPath),
            pPath,
            lJobItem.LWorkOutput)
        {
            LWorkLineage = pLineage,
            LWorkRelayTarget = lJobItem.LWorkRelayTarget,
            LWorkRelaySource = lJobItem.LWorkRelaySource,
            LWorkRelayProcess = lJobItem.LWorkRelayProcess,
            LWorkRelayStamp = lJobItem.LWorkRelayStamp,
            LWorkTab = lJobItem.LWorkTab,
            LWorkSignet = lJobItem.LWorkSignet,
            LWorkSourceBytes = lJobItem.LWorkSourceBytes,
            LWorkRunState =
            {
                LWorkOwnerProcess = Environment.ProcessId,
                LWorkOwnerStamp = lJobItem.LWorkRunState.LWorkOwnerStamp,
                LWorkOwnerRunner = lJobItem.LWorkRunState.LWorkOwnerRunner,
                LWorkStartTime = lJobItem.LWorkRunState.LWorkStartTime,
                LWorkFinishTime = DateTimeOffset.Now,
                LWorkOutputBytes = pBytes,
                LWorkStateCurrent = LWorkState.LWorkStateDone,
                LWorkProgress = 1
            }
        };
}
