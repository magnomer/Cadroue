using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class ScheduleDeliveredTests
{
    [Fact]
    public void DeliveredWork_IsFiledAsDone()
    {
        using var schedule = new TSchedule();
        Guid batchId = Guid.NewGuid();
        TScheduleWork origin = schedule.WorkCreate(batchId, "origin");
        schedule.Submit(origin);
        TScheduleWork claimed = schedule.ClaimNext();
        schedule.Complete(claimed, true);

        TScheduleWork first = schedule.WorkCreateDerived(origin, "salvage-1");
        TScheduleWork second = schedule.WorkCreateDerived(origin, "salvage-2");

        Assert.Equal(2, schedule.DeliveredAdd(first, second));

        IReadOnlyList<TScheduleItem> records = schedule.RecordsRead();
        Assert.Contains(records, item => item.WorkId == first.WorkId && item.State == LWorkState.LWorkStateDone);
        Assert.Contains(records, item => item.WorkId == second.WorkId && item.State == LWorkState.LWorkStateDone);
    }

    [Fact]
    public void DeliveredWork_SurvivesReloadAsDone()
    {
        using var schedule = new TSchedule();
        TScheduleWork origin = schedule.WorkCreate(Guid.NewGuid(), "origin");
        schedule.Submit(origin);
        schedule.Complete(schedule.ClaimNext(), true);

        TScheduleWork derived = schedule.WorkCreateDerived(origin, "salvage");
        schedule.DeliveredAdd(derived);
        schedule.Reload();

        TScheduleItem reloaded = Assert.Single(
            schedule.RecordsRead(), item => item.WorkId == derived.WorkId);
        Assert.Equal(LWorkState.LWorkStateDone, reloaded.State);
    }

    [Fact]
    public void DeliveredWork_SharesTheSourceLineage()
    {
        using var schedule = new TSchedule();
        TScheduleWork origin = schedule.WorkCreate(Guid.NewGuid(), "origin");
        schedule.Submit(origin);

        TScheduleWork derived = schedule.WorkCreateDerived(origin, "salvage");
        schedule.DeliveredAdd(derived);

        Assert.Equal(schedule.LineageRead(origin), schedule.LineageRead(derived));
    }

    [Fact]
    public void EmptyDelivery_IsRejected()
    {
        using var schedule = new TSchedule();

        Assert.Equal(0, schedule.DeliveredAdd());
        Assert.Empty(schedule.RecordsRead());
    }

    [Fact]
    public void DeliveredWork_IsNotClaimable()
    {
        using var schedule = new TSchedule();
        TScheduleWork origin = schedule.WorkCreate(Guid.NewGuid(), "origin");
        schedule.Submit(origin);
        schedule.Complete(schedule.ClaimNext(), true);

        schedule.DeliveredAdd(schedule.WorkCreateDerived(origin, "salvage"));

        Assert.Null(schedule.TryClaimNext());
    }
}
