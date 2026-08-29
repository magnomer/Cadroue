using System.IO;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    // The salvage recoveries this run extracted from the original source, awaiting the
    // terminal outcome. Empty when salvage was off or recovered nothing, in which case
    // the single-output path is left untouched.
    private IReadOnlyList<string> lJobSalvaged = Array.Empty<string>();

    // Record each salvage recovery as a completed derived work item sharing the source
    // item's batch and lineage, then file them as delivered so the Roster/Summary display
    // and the relay path treat each like any other finished Fix output.
    private void LJobSalvageRecord()
    {
        if (lJobSalvaged.Count == 0)
        {
            return;
        }

        Guid pLineage = lJobItem.LWorkLineage != Guid.Empty
            ? lJobItem.LWorkLineage
            : lJobOwner.lRunnerSchedule.LScheduleLineageRead(lJobItem);

        var pItems = new List<LWorkItem>(lJobSalvaged.Count);
        foreach (string pPath in lJobSalvaged)
        {
            long pBytes = LScout.LScoutBytesRead(pPath) ?? 0;
            pItems.Add(LJobSalvageCreate(pPath, pBytes, pLineage));
        }

        lJobOwner.lRunnerSchedule.LScheduleDeliveredAdd(pItems);
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
            LWorkTab = lJobItem.LWorkTab,
            LWorkSignet = lJobItem.LWorkSignet,
            LWorkOwnerProcess = Environment.ProcessId,
            LWorkOwnerStamp = lJobItem.LWorkOwnerStamp,
            LWorkOwnerRunner = lJobItem.LWorkOwnerRunner,
            LWorkStartTime = lJobItem.LWorkStartTime,
            LWorkFinishTime = DateTimeOffset.Now,
            LWorkOutputBytes = pBytes,
            LWorkSourceBytes = lJobItem.LWorkSourceBytes,
            LWorkStateCurrent = LWorkState.LWorkStateDone,
            LWorkProgress = 1
        };
}
