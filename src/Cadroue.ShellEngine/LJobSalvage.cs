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
