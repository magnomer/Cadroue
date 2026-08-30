using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class ScheduleLineageTests
{
    [Fact]
    public void SameBatchContinuation_InheritsLineage()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork origin = schedule.WorkCreate(batchId, "origin");
        schedule.Submit(origin);

        TScheduleWork continuation = schedule.WorkCreate(batchId, "continuation", parent: origin);
        schedule.Submit(continuation);

        Assert.Equal(schedule.LineageRead(origin), schedule.LineageRead(continuation));
    }

    [Fact]
    public void DifferentBatchWithSamePath_StartsNewLineage()
    {
        using var schedule = new TSchedule();
        TScheduleWork origin = schedule.WorkCreate(Guid.NewGuid(), "origin");
        schedule.Submit(origin);

        TScheduleWork fresh = schedule.WorkCreate(Guid.NewGuid(), "fresh", parent: origin);
        schedule.Submit(fresh);

        Assert.NotEqual(schedule.LineageRead(origin), schedule.LineageRead(fresh));
    }
}
