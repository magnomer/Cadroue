using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TScheduleDelivered
{
    [Fact]
    public void DeliveredWork_IsFiledAsDone()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork origin = schedule.TWorkCreate(batchId, "origin");
        schedule.TScheduleAdd(origin);
        TScheduleWork claimed = schedule.TScheduleNextClaim();
        schedule.TScheduleCommit(claimed, true);

        TScheduleWork first = schedule.TWorkDeriveCreate(origin, "salvage-1");
        TScheduleWork second = schedule.TWorkDeriveCreate(origin, "salvage-2");

        Assert.Equal(2, schedule.TScheduleDeliverAdd(first, second));

        IReadOnlyList<TScheduleItem> records = schedule.TScheduleRecordsRead();
        Assert.Contains(records, item => item.TWorkId == first.TWorkId && item.TScheduleState == LWorkState.LWorkStateDone);
        Assert.Contains(records, item => item.TWorkId == second.TWorkId && item.TScheduleState == LWorkState.LWorkStateDone);
    }

    [Fact]
    public void DeliveredWork_SurvivesReloadAsDone()
    {
        using var schedule = new TSchedule();
        TScheduleWork origin = schedule.TWorkCreate(Guid.NewGuid(), "origin");
        schedule.TScheduleAdd(origin);
        schedule.TScheduleCommit(schedule.TScheduleNextClaim(), true);

        TScheduleWork derived = schedule.TWorkDeriveCreate(origin, "salvage");
        schedule.TScheduleDeliverAdd(derived);
        schedule.TScheduleDiskLoad();

        TScheduleItem reloaded = Assert.Single(
            schedule.TScheduleRecordsRead(), item => item.TWorkId == derived.TWorkId);
        Assert.Equal(LWorkState.LWorkStateDone, reloaded.TScheduleState);
    }

    [Fact]
    public void DeliveredWork_SharesTheSourceLineage()
    {
        using var schedule = new TSchedule();
        TScheduleWork origin = schedule.TWorkCreate(Guid.NewGuid(), "origin");
        schedule.TScheduleAdd(origin);

        TScheduleWork derived = schedule.TWorkDeriveCreate(origin, "salvage");
        schedule.TScheduleDeliverAdd(derived);

        Assert.Equal(schedule.TLineageRead(origin), schedule.TLineageRead(derived));
    }

    [Fact]
    public void EmptyDelivery_IsRejected()
    {
        using var schedule = new TSchedule();

        Assert.Equal(0, schedule.TScheduleDeliverAdd());
        Assert.Empty(schedule.TScheduleRecordsRead());
    }

    [Fact]
    public void DeliveredWork_IsNotClaimable()
    {
        using var schedule = new TSchedule();
        TScheduleWork origin = schedule.TWorkCreate(Guid.NewGuid(), "origin");
        schedule.TScheduleAdd(origin);
        schedule.TScheduleCommit(schedule.TScheduleNextClaim(), true);

        schedule.TScheduleDeliverAdd(schedule.TWorkDeriveCreate(origin, "salvage"));

        Assert.Null(schedule.TScheduleTryClaim());
    }
}
