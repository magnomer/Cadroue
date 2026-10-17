using System.IO;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    private IReadOnlyList<string> lJobSalvaged = Array.Empty<string>();

    private void LJobSalvageRecord()
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
            long pBytes = LScout.LScoutBytesRead(pPath) ?? 0;
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
