using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed class CLineage
{
    private readonly LLineagePort _cLineagePort;

    internal CLineage(LLineagePort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cLineagePort = port;
    }

    public CLineageAnswer CLineageRead(IReadOnlyList<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        LLineageAnswer answer = _cLineagePort.LLineageResolve(ids);
        return new CLineageAnswer(
            answer.LLineageGroups.Select(CLineageEntryCreate).ToArray(),
            answer.LLineageBatches.Select(CLineageBatchCreate).ToArray(),
            answer.LLineageSteps.Select(CLineageStepCreate).ToArray());
    }

    public CLineageBatch CLineageBatchRead(Guid batch) =>
        CLineageBatchCreate(_cLineagePort.LLineageBatchResolve(batch));

    public string CLineageTitleRead(Guid batch, Guid relay, string tab) =>
        _cLineagePort.LLineageTitleRead(batch, relay, tab);

    private static CLineageEntry CLineageEntryCreate(LLineageGroup group) =>
        new(
            group.LLineageGroupBatch,
            group.LLineageGroupLineage,
            group.LLineageGroupSubject,
            group.LLineageGroupName,
            group.LLineageGroupIds.ToArray(),
            group.LLineageGroupOrigin);

    private static CLineageBatch CLineageBatchCreate(LLineageBatch batch) =>
        new(
            batch.LLineageBatchId,
            batch.LLineageBatchStages.ToHashSet(),
            batch.LLineageBatchInitial,
            batch.LLineageBatchEarliest,
            batch.LLineageBatchSources.ToArray(),
            batch.LLineageBatchOutputs.ToArray(),
            batch.LLineageSourceTotal,
            batch.LLineageOutputTotal,
            batch.LLineageSourceCount,
            batch.LLineageOutputCount,
            batch.LLineageBatchIds.ToArray(),
            batch.LLineageBatchCompleted);

    private static CLineageStep CLineageStepCreate(LLineageStep step) =>
        new(
            step.LLineageStepId,
            step.LLineageStepBatch,
            step.LLineageStepRelay,
            step.LLineageStepTab,
            step.LLineageStepKind,
            step.LLineageStepSubject,
            step.LLineageStepName,
            step.LLineageStepSource,
            step.LLineageStepRatio,
            step.LLineageSourceExtension,
            step.LLineageOutputExtension);
}
