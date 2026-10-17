using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The lineage reads: the schedule's groups, batches and steps, each batch with the time its items spent, timed by the
// clock the host hands in.
public sealed class LLineageOutlet : LLineagePort
{
    private readonly Func<DateTimeOffset> _lLineageOutletClock;

    public LLineageOutlet(Func<DateTimeOffset> lClock)
    {
        ArgumentNullException.ThrowIfNull(lClock);
        _lLineageOutletClock = lClock;
    }

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
            lAnswer.LScheduleLineageBatches
                .Select(lBatch => LLineageBatchCreate(
                    lBatch, LScheduleLookup.LScheduleBatchRead(lSchedule, lBatch.LScheduleBatchId)))
                .ToArray(),
            lAnswer.LScheduleLineageSteps.Select(LLineageStepCreate).ToArray());
    }

    public LLineageBatch LLineageBatchResolve(Guid lBatchId)
    {
        LScheduleContract? lSchedule = LStation.LStationSchedule;
        return LLineageBatchCreate(
            LScheduleLineagePlan.LScheduleBatchResolve(lSchedule, lBatchId, LUsherEnvironmentEngine.LUsherPathSeam),
            lSchedule is null ? [] : LScheduleLookup.LScheduleBatchRead(lSchedule, lBatchId));
    }

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

    // The spent time counts each started item to its finish, or to now while it runs, and leaves out a backward span.
    private LLineageBatch LLineageBatchCreate(LScheduleLineageBatch lBatch, IReadOnlyList<LWorkItem> lItems)
    {
        TimeSpan[] lSpans = lItems
            .Where(lItem => lItem.LWorkStartTime is not null)
            .Select(lItem => (lItem.LWorkFinishTime ?? _lLineageOutletClock()) - lItem.LWorkStartTime!.Value)
            .Where(lSpan => lSpan >= TimeSpan.Zero)
            .ToArray();
        TimeSpan lTotal = lSpans.Aggregate(TimeSpan.Zero, (lSum, lSpan) => lSum + lSpan);
        return new(
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
            lBatch.LScheduleBatchCompleted)
        {
            LLineageBatchSpent = lTotal > TimeSpan.Zero ? lTotal : null
        };
    }

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
