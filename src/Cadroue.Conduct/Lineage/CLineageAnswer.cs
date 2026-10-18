using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed record CLineageAnswer(
    IReadOnlyList<CLineageEntry> CLineageEntries,
    IReadOnlyList<CLineageBatch> CLineageBatches,
    IReadOnlyList<CLineageStep> CLineageSteps)
{
    internal static CLineageAnswer CLineageAnswerCreate(LLineageAnswer answer) =>
        new(
            answer.LLineageGroups.Select(CLineageEntry.CLineageEntryCreate).ToArray(),
            answer.LLineageBatches.Select(CLineageBatch.CLineageBatchCreate).ToArray(),
            answer.LLineageSteps.Select(CLineageStep.CLineageStepCreate).ToArray());
}

public sealed record CLineageEntry(
    Guid CLineageEntryBatch,
    Guid CLineageEntryLineage,
    string CLineageEntrySubject,
    string? CLineageEntryName,
    IReadOnlyList<Guid> CLineageEntryIds,
    long? CLineageEntryOrigin)
{
    internal static CLineageEntry CLineageEntryCreate(LLineageGroup group) =>
        new(
            group.LLineageGroupBatch,
            group.LLineageGroupLineage,
            group.LLineageGroupSubject,
            group.LLineageGroupName,
            group.LLineageGroupIds.ToArray(),
            group.LLineageGroupOrigin);
}

public sealed record CLineageBatch(
    Guid CLineageBatchId,
    IReadOnlySet<Guid> CLineageBatchStages,
    int CLineageBatchInitial,
    DateTimeOffset CLineageBatchEarliest,
    IReadOnlyList<string> CLineageBatchSources,
    IReadOnlyList<string> CLineageBatchOutputs,
    long? CLineageSourceTotal,
    long? CLineageOutputTotal,
    int CLineageSourceCount,
    int CLineageOutputCount,
    IReadOnlyList<Guid> CLineageBatchIds,
    bool CLineageBatchCompleted)
{
    // The engine's spent time for the batch; none when no item counts.
    public TimeSpan? CLineageBatchSpent { get; init; }

    internal static CLineageBatch CLineageBatchCreate(LLineageBatch batch) =>
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
            batch.LLineageBatchCompleted)
        {
            CLineageBatchSpent = batch.LLineageBatchSpent
        };
}

public sealed record CLineageStep(
    Guid CLineageStepId,
    Guid CLineageStepBatch,
    Guid CLineageStepRelay,
    string CLineageStepTab,
    string CLineageStepKind,
    bool CLineageStepSubject,
    string? CLineageStepName,
    long? CLineageStepSource,
    double? CLineageStepRatio,
    string CLineageSourceExtension,
    string CLineageOutputExtension)
{
    internal static CLineageStep CLineageStepCreate(LLineageStep step) =>
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
