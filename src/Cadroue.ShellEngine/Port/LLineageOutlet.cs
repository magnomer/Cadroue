using Cadroue.Application;

namespace Cadroue.ShellEngine;

public sealed class LLineageOutlet : LLineagePort
{
    public LLineageAnswer LLineageResolve(IReadOnlyList<Guid> lIds)
    {
        if (LStation.LStationSchedule is not { } lSchedule)
        {
            return new LLineageAnswer([], [], []);
        }

        LScheduleLineageAnswer lAnswer = LScheduleLineagePlan.LScheduleLineageResolve(
            lSchedule,
            lIds,
            LLineageEngine.LLineageFileSeam,
            LUsherEnvironmentEngine.LUsherPathSeam,
            LUsherTextEngine.LUsherNameSeam,
            LUsherTextEngine.LUsherExtensionSeam);
        return new LLineageAnswer(
            lAnswer.LScheduleLineageEntries.Select(LLineageGroupCreate).ToArray(),
            lAnswer.LScheduleLineageBatches.Select(LLineageBatchCreate).ToArray(),
            lAnswer.LScheduleLineageSteps.Select(LLineageStepCreate).ToArray());
    }

    public LLineageBatch LLineageBatchResolve(Guid lBatchId) =>
        LLineageBatchCreate(LScheduleLineagePlan.LScheduleBatchResolve(
            LStation.LStationSchedule, lBatchId, LUsherEnvironmentEngine.LUsherPathSeam));

    public string LLineageTitleRead(Guid lBatchId, Guid lRelaySource, string lTab) =>
        LCartographer.LCartographerTitleRead(lBatchId, lRelaySource, lTab);

    private static LLineageGroup LLineageGroupCreate(LScheduleLineageEntry lEntry) =>
        new(
            lEntry.LScheduleEntryBatch,
            lEntry.LScheduleEntryLineage,
            lEntry.LScheduleEntrySubject,
            lEntry.LScheduleEntryName,
            lEntry.LScheduleEntryIds.ToArray(),
            lEntry.LScheduleEntryOrigin);

    private static LLineageBatch LLineageBatchCreate(LScheduleLineageBatch lBatch) =>
        new(
            lBatch.LScheduleBatchId,
            lBatch.LScheduleBatchStages.ToHashSet(),
            lBatch.LScheduleBatchInitial,
            lBatch.LScheduleBatchEarliest,
            lBatch.LScheduleBatchSources.ToArray(),
            lBatch.LScheduleBatchOutputs.ToArray(),
            lBatch.LScheduleSourceTotal,
            lBatch.LScheduleOutputTotal,
            lBatch.LScheduleSourceCount,
            lBatch.LScheduleOutputCount,
            lBatch.LScheduleBatchIds.ToArray(),
            lBatch.LScheduleBatchCompleted);

    private static LLineageStep LLineageStepCreate(LScheduleLineageStep lStep) =>
        new(
            lStep.LScheduleStepId,
            lStep.LScheduleStepBatch,
            lStep.LScheduleStepRelay,
            lStep.LScheduleStepTab,
            lStep.LScheduleStepKind.ToString(),
            lStep.LScheduleStepSubject,
            lStep.LScheduleStepName,
            lStep.LScheduleStepSource,
            lStep.LScheduleStepRatio,
            lStep.LScheduleSourceExtension,
            lStep.LScheduleOutputExtension);
}
