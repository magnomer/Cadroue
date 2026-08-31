using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TScheduleOrdering
{
    [Fact]
    public void NormalPriorityWork_PreservesSubmissionOrder()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork first = schedule.TWorkCreate(batchId, "first");
        TScheduleWork second = schedule.TWorkCreate(batchId, "second");
        TScheduleWork third = schedule.TWorkCreate(batchId, "third");
        schedule.TScheduleAdd(first, second, third);

        Assert.Equal(new[] { first.TWorkId, second.TWorkId, third.TWorkId },
            schedule.TScheduleOrderRead().Select(item => item.TWorkId));
    }

    [Fact]
    public void HighPriorityWork_IsPlacedBeforeNormalPendingWork()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork normal = schedule.TWorkCreate(batchId, "normal");
        TScheduleWork high = schedule.TWorkCreate(
            batchId, "high", LWorkPriority.LWorkPriorityHigh);
        schedule.TScheduleAdd(normal, high);

        Assert.Equal(new[] { high.TWorkId, normal.TWorkId },
            schedule.TScheduleOrderRead().Select(item => item.TWorkId));
    }

    [Fact]
    public void Reordering_ChangesPendingExecutionOrderWithoutChangingIdentity()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork first = schedule.TWorkCreate(batchId, "first");
        TScheduleWork second = schedule.TWorkCreate(batchId, "second");
        TScheduleWork third = schedule.TWorkCreate(batchId, "third");
        schedule.TScheduleAdd(first, second, third);

        Assert.True(schedule.TScheduleMove(batchId, third, first, second));

        Assert.Equal(new[] { third.TWorkId, first.TWorkId, second.TWorkId },
            schedule.TScheduleOrderRead().Select(item => item.TWorkId));
    }

    [Fact]
    public void ReorderingOneBatch_DoesNotMutateUnrelatedBatch()
    {
        using var schedule = new TSchedule();
        Guid changedBatch = Guid.NewGuid();
        Guid unrelatedBatch = Guid.NewGuid();
        TScheduleWork changedFirst = schedule.TWorkCreate(changedBatch, "changed-first");
        TScheduleWork unrelatedFirst = schedule.TWorkCreate(unrelatedBatch, "unrelated-first");
        TScheduleWork changedSecond = schedule.TWorkCreate(changedBatch, "changed-second");
        TScheduleWork unrelatedSecond = schedule.TWorkCreate(unrelatedBatch, "unrelated-second");
        schedule.TScheduleAdd(changedFirst, unrelatedFirst, changedSecond, unrelatedSecond);

        Assert.True(schedule.TScheduleMove(changedBatch, changedSecond, changedFirst));

        IReadOnlyList<Guid> unrelatedOrder = schedule.TScheduleOrderRead()
            .Where(item => item.TScheduleBatchId == unrelatedBatch)
            .Select(item => item.TWorkId)
            .ToArray();
        Assert.Equal(new[] { unrelatedFirst.TWorkId, unrelatedSecond.TWorkId }, unrelatedOrder);
    }

    [Fact]
    public void InvalidOrderChange_CannotDuplicateOrDropWork()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork first = schedule.TWorkCreate(batchId, "first");
        TScheduleWork second = schedule.TWorkCreate(batchId, "second");
        TScheduleWork third = schedule.TWorkCreate(batchId, "third");
        schedule.TScheduleAdd(first, second, third);

        Assert.False(schedule.TScheduleMove(batchId, second, second, third));

        Assert.Equal(new[] { first.TWorkId, second.TWorkId, third.TWorkId },
            schedule.TScheduleOrderRead().Select(item => item.TWorkId));
    }
}
