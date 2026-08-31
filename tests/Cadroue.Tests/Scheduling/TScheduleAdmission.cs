using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TScheduleAdmission
{
    [Fact]
    public void ValidWork_BecomesPending()
    {
        using var schedule = new TSchedule();
        TScheduleWork work = schedule.TWorkCreate(Guid.NewGuid(), "first");

        Assert.Equal(1, schedule.TScheduleAdd(work));

        TScheduleItem pending = Assert.Single(schedule.TSchedulePendingRead());
        Assert.Equal(work.TWorkId, pending.TWorkId);
        Assert.Equal(LWorkState.LWorkStatePending, pending.TScheduleState);
    }

    [Fact]
    public void MultipleValidWorkItems_AreAdmittedExactlyOnce()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork first = schedule.TWorkCreate(batchId, "first");
        TScheduleWork second = schedule.TWorkCreate(batchId, "second");

        Assert.Equal(2, schedule.TScheduleAdd(first, second));

        Assert.Equal(new[] { first.TWorkId, second.TWorkId },
            schedule.TSchedulePendingRead().Select(item => item.TWorkId));
    }

    [Fact]
    public void SameWorkItem_CannotBeAdmittedTwice()
    {
        using var schedule = new TSchedule();
        TScheduleWork work = schedule.TWorkCreate(Guid.NewGuid(), "only");

        Assert.Equal(1, schedule.TScheduleAdd(work));
        Assert.Equal(0, schedule.TScheduleAdd(work));

        Assert.Equal(work.TWorkId, Assert.Single(schedule.TSchedulePendingRead()).TWorkId);
    }

    [Fact]
    public void SeparateBatches_RemainDistinguishable()
    {
        using var schedule = new TSchedule();
        Guid firstBatch = Guid.NewGuid();
        Guid secondBatch = Guid.NewGuid();

        schedule.TScheduleAdd(
            schedule.TWorkCreate(firstBatch, "first"),
            schedule.TWorkCreate(secondBatch, "second"));

        Assert.Equal(new[] { firstBatch, secondBatch },
            schedule.TSchedulePendingRead().Select(item => item.TScheduleBatchId));
    }

    [Fact]
    public void WorkAndBatchIdentity_SurviveAdmission()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork work = schedule.TWorkCreate(batchId, "identity");

        schedule.TScheduleAdd(work);

        TScheduleItem pending = Assert.Single(schedule.TSchedulePendingRead());
        Assert.Equal(work.TWorkId, pending.TWorkId);
        Assert.Equal(work.TScheduleBatchId, pending.TScheduleBatchId);
        Assert.Equal("identity", pending.TWorkName);
    }

    [Fact]
    public void EmptySubmission_IsRejected()
    {
        using var schedule = new TSchedule();

        Assert.Equal(0, schedule.TScheduleAdd());
        Assert.Empty(schedule.TSchedulePendingRead());
    }
}
