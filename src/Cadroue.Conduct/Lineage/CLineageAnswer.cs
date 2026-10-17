using System;
using System.Collections.Generic;

namespace Cadroue.Conduct;

public sealed record CLineageAnswer(
    IReadOnlyList<CLineageEntry> CLineageEntries,
    IReadOnlyList<CLineageBatch> CLineageBatches,
    IReadOnlyList<CLineageStep> CLineageSteps);

public sealed record CLineageEntry(
    Guid CLineageEntryBatch,
    Guid CLineageEntryLineage,
    string CLineageEntrySubject,
    string? CLineageEntryName,
    IReadOnlyList<Guid> CLineageEntryIds,
    long? CLineageEntryOrigin);

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
    string CLineageOutputExtension);
