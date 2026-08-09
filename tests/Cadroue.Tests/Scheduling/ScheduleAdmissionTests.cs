using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class ScheduleAdmissionTests
{
    [Fact]
    public void ValidWork_BecomesPending()
    {
        using var schedule = new TSchedule();
        TScheduleWork work = schedule.WorkCreate(Guid.NewGuid(), "first");

        Assert.Equal(1, schedule.Submit(work));

        TScheduleItem pending = Assert.Single(schedule.PendingRead());
        Assert.Equal(work.WorkId, pending.WorkId);
        Assert.Equal(LWorkState.LWorkStatePending, pending.State);
    }

    [Fact]
    public void MultipleValidWorkItems_AreAdmittedExactlyOnce()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork first = schedule.WorkCreate(batchId, "first");
        TScheduleWork second = schedule.WorkCreate(batchId, "second");

        Assert.Equal(2, schedule.Submit(first, second));

        Assert.Equal(new[] { first.WorkId, second.WorkId },
            schedule.PendingRead().Select(item => item.WorkId));
    }

    [Fact]
    public void SameWorkItem_CannotBeAdmittedTwice()
    {
        using var schedule = new TSchedule();
        TScheduleWork work = schedule.WorkCreate(Guid.NewGuid(), "only");

        Assert.Equal(1, schedule.Submit(work));
        Assert.Equal(0, schedule.Submit(work));

        Assert.Equal(work.WorkId, Assert.Single(schedule.PendingRead()).WorkId);
    }

    [Fact]
    public void SeparateBatches_RemainDistinguishable()
    {
        using var schedule = new TSchedule();
        Guid firstBatch = Guid.NewGuid();
        Guid secondBatch = Guid.NewGuid();

        schedule.Submit(
            schedule.WorkCreate(firstBatch, "first"),
            schedule.WorkCreate(secondBatch, "second"));

        Assert.Equal(new[] { firstBatch, secondBatch },
            schedule.PendingRead().Select(item => item.BatchId));
    }

    [Fact]
    public void WorkAndBatchIdentity_SurviveAdmission()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork work = schedule.WorkCreate(batchId, "identity");

        schedule.Submit(work);

        TScheduleItem pending = Assert.Single(schedule.PendingRead());
        Assert.Equal(work.WorkId, pending.WorkId);
        Assert.Equal(work.BatchId, pending.BatchId);
        Assert.Equal("identity", pending.Name);
    }

    [Fact]
    public void EmptySubmission_IsRejected()
    {
        using var schedule = new TSchedule();

        Assert.Equal(0, schedule.Submit());
        Assert.Empty(schedule.PendingRead());
    }
}
