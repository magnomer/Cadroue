using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class ScheduleOrderingTests
{
    [Fact]
    public void NormalPriorityWork_PreservesSubmissionOrder()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork first = schedule.WorkCreate(batchId, "first");
        TScheduleWork second = schedule.WorkCreate(batchId, "second");
        TScheduleWork third = schedule.WorkCreate(batchId, "third");
        schedule.Submit(first, second, third);

        Assert.Equal(new[] { first.WorkId, second.WorkId, third.WorkId },
            schedule.ExecutionOrderRead().Select(item => item.WorkId));
    }

    [Fact]
    public void HighPriorityWork_IsPlacedBeforeNormalPendingWork()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork normal = schedule.WorkCreate(batchId, "normal");
        TScheduleWork high = schedule.WorkCreate(
            batchId, "high", LWorkPriority.LWorkPriorityHigh);
        schedule.Submit(normal, high);

        Assert.Equal(new[] { high.WorkId, normal.WorkId },
            schedule.ExecutionOrderRead().Select(item => item.WorkId));
    }

    [Fact]
    public void Reordering_ChangesPendingExecutionOrderWithoutChangingIdentity()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork first = schedule.WorkCreate(batchId, "first");
        TScheduleWork second = schedule.WorkCreate(batchId, "second");
        TScheduleWork third = schedule.WorkCreate(batchId, "third");
        schedule.Submit(first, second, third);

        Assert.True(schedule.Reorder(batchId, third, first, second));

        Assert.Equal(new[] { third.WorkId, first.WorkId, second.WorkId },
            schedule.ExecutionOrderRead().Select(item => item.WorkId));
    }

    [Fact]
    public void ReorderingOneBatch_DoesNotMutateUnrelatedBatch()
    {
        using var schedule = new TSchedule();
        Guid changedBatch = Guid.NewGuid();
        Guid unrelatedBatch = Guid.NewGuid();
        TScheduleWork changedFirst = schedule.WorkCreate(changedBatch, "changed-first");
        TScheduleWork unrelatedFirst = schedule.WorkCreate(unrelatedBatch, "unrelated-first");
        TScheduleWork changedSecond = schedule.WorkCreate(changedBatch, "changed-second");
        TScheduleWork unrelatedSecond = schedule.WorkCreate(unrelatedBatch, "unrelated-second");
        schedule.Submit(changedFirst, unrelatedFirst, changedSecond, unrelatedSecond);

        Assert.True(schedule.Reorder(changedBatch, changedSecond, changedFirst));

        IReadOnlyList<Guid> unrelatedOrder = schedule.ExecutionOrderRead()
            .Where(item => item.BatchId == unrelatedBatch)
            .Select(item => item.WorkId)
            .ToArray();
        Assert.Equal(new[] { unrelatedFirst.WorkId, unrelatedSecond.WorkId }, unrelatedOrder);
    }

    [Fact]
    public void InvalidOrderChange_CannotDuplicateOrDropWork()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork first = schedule.WorkCreate(batchId, "first");
        TScheduleWork second = schedule.WorkCreate(batchId, "second");
        TScheduleWork third = schedule.WorkCreate(batchId, "third");
        schedule.Submit(first, second, third);

        Assert.False(schedule.Reorder(batchId, second, second, third));

        Assert.Equal(new[] { first.WorkId, second.WorkId, third.WorkId },
            schedule.ExecutionOrderRead().Select(item => item.WorkId));
    }
}
