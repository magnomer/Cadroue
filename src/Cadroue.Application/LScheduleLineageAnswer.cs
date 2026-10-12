using System;
using System.Collections.Generic;

namespace Cadroue.Application;

public sealed record LScheduleLineageAnswer(
    IReadOnlyList<LScheduleLineageEntry> LScheduleLineageEntries,
    IReadOnlyList<LScheduleLineageBatch> LScheduleLineageBatches,
    IReadOnlyList<LScheduleLineageStep> LScheduleLineageSteps);

public sealed record LScheduleLineageEntry(
    Guid LScheduleEntryBatch,
    Guid LScheduleEntryLineage,
    string LScheduleEntrySubject,
    string? LScheduleEntryName,
    IReadOnlyList<Guid> LScheduleEntryIds,
    long? LScheduleEntryOrigin);

public sealed record LScheduleLineageBatch(
    Guid LScheduleBatchId,
    IReadOnlySet<Guid> LScheduleBatchStages,
    int LScheduleBatchInitial,
    DateTimeOffset LScheduleBatchEarliest,
    IReadOnlyList<string> LScheduleBatchSources,
    IReadOnlyList<string> LScheduleBatchOutputs,
    long? LScheduleSourceTotal,
    long? LScheduleOutputTotal,
    int LScheduleSourceCount,
    int LScheduleOutputCount,
    IReadOnlyList<Guid> LScheduleBatchIds,
    bool LScheduleBatchCompleted);
