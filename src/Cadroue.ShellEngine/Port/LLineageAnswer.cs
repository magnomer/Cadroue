namespace Cadroue.ShellEngine;

public sealed record LLineageAnswer(
    IReadOnlyList<LLineageGroup> LLineageGroups,
    IReadOnlyList<LLineageBatch> LLineageBatches,
    IReadOnlyList<LLineageStep> LLineageSteps);

public sealed record LLineageGroup(
    Guid LLineageGroupBatch,
    Guid LLineageGroupLineage,
    string LLineageGroupSubject,
    string? LLineageGroupName,
    IReadOnlyList<Guid> LLineageGroupIds,
    long? LLineageGroupOrigin);

public sealed record LLineageBatch(
    Guid LLineageBatchId,
    IReadOnlySet<Guid> LLineageBatchStages,
    int LLineageBatchInitial,
    DateTimeOffset LLineageBatchEarliest,
    IReadOnlyList<string> LLineageBatchSources,
    IReadOnlyList<string> LLineageBatchOutputs,
    long? LLineageSourceTotal,
    long? LLineageOutputTotal,
    int LLineageSourceCount,
    int LLineageOutputCount,
    IReadOnlyList<Guid> LLineageBatchIds,
    bool LLineageBatchCompleted)
{
    // The time the batch's started items spent, each to its finish or to now while it runs; an item whose span runs
    // backwards is left out. None when no item counts or the total is not above zero.
    public TimeSpan? LLineageBatchSpent { get; init; }
}

public sealed record LLineageStep(
    Guid LLineageStepId,
    Guid LLineageStepBatch,
    Guid LLineageStepRelay,
    string LLineageStepTab,
    string LLineageStepKind,
    bool LLineageStepSubject,
    string? LLineageStepName,
    long? LLineageStepSource,
    double? LLineageStepRatio,
    string LLineageSourceExtension,
    string LLineageOutputExtension);
