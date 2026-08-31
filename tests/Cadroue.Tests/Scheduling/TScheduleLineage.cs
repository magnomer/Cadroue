using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TScheduleLineage
{
    [Fact]
    public void SameBatchContinuation_InheritsLineage()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork origin = schedule.TWorkCreate(batchId, "origin");
        schedule.TScheduleAdd(origin);

        TScheduleWork continuation = schedule.TWorkCreate(batchId, "continuation", parent: origin);
        schedule.TScheduleAdd(continuation);

        Assert.Equal(schedule.TLineageRead(origin), schedule.TLineageRead(continuation));
    }

    [Fact]
    public void DifferentBatchWithSamePath_StartsNewLineage()
    {
        using var schedule = new TSchedule();
        TScheduleWork origin = schedule.TWorkCreate(Guid.NewGuid(), "origin");
        schedule.TScheduleAdd(origin);

        TScheduleWork fresh = schedule.TWorkCreate(Guid.NewGuid(), "fresh", parent: origin);
        schedule.TScheduleAdd(fresh);

        Assert.NotEqual(schedule.TLineageRead(origin), schedule.TLineageRead(fresh));
    }
}
