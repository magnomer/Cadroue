using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TRosterMenu
{
    private static LRoster TRosterOpen(TSchedule schedule)
    {
        LRoster roster = TInterface.TRosterCreate(
            schedule.TScheduleRead(), TInterface.TStationCreate(schedule.TScheduleRead()));
        TInterface.TRosterRebuild(roster);
        return roster;
    }

    [Fact]
    public void Pending_OffersCancel_AndCancelRunsOverTheSelection()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork one = schedule.TWorkCreate(batch, "one");
        TScheduleWork two = schedule.TWorkCreate(batch, "two");
        schedule.TScheduleAdd(one, two);
        LRoster roster = TRosterOpen(schedule);
        TInterface.TRosterStepSelect(roster, one.TWorkId, false, false);
        TInterface.TRosterStepSelect(roster, two.TWorkId, false, true);

        LRosterItem cancel = Assert.Single(TInterface.TRosterMenuRead(roster, one.TWorkId, null));
        Assert.True(cancel.LRosterItemEnabled);
        Assert.Empty(cancel.LRosterItemIcon);
        TInterface.TRosterItemRun(cancel);

        Assert.All(schedule.TScheduleRecordsRead(), item =>
            Assert.Equal(LWorkState.LWorkStateCancelled, item.TScheduleState));
        TInterface.TRosterClose(roster);
    }

    [Fact]
    public void Cancelled_OffersRestart_ForTheClickedItemWhenItIsNotSelected()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork one = schedule.TWorkCreate(batch, "one");
        TScheduleWork two = schedule.TWorkCreate(batch, "two");
        schedule.TScheduleAdd(one, two);
        schedule.TScheduleCancel(one, two);
        LRoster roster = TRosterOpen(schedule);
        TInterface.TRosterStepSelect(roster, two.TWorkId, false, false);

        LRosterItem restart = Assert.Single(TInterface.TRosterMenuRead(roster, one.TWorkId, null));
        TInterface.TRosterItemRun(restart);

        IReadOnlyList<TScheduleItem> records = schedule.TScheduleRecordsRead();
        Assert.Equal(
            LWorkState.LWorkStatePending, records.Single(item => item.TWorkId == one.TWorkId).TScheduleState);
        Assert.Equal(
            LWorkState.LWorkStateCancelled, records.Single(item => item.TWorkId == two.TWorkId).TScheduleState);
        TInterface.TRosterClose(roster);
    }

    [Fact]
    public void Done_WithoutAnOutputFileOrStrip_HasNoMenu()
    {
        using var schedule = new TSchedule();
        schedule.TScheduleAdd(schedule.TWorkCreate(Guid.NewGuid(), "one"));
        TScheduleWork claimed = schedule.TScheduleNextClaim();
        schedule.TScheduleCommit(claimed, true);
        LRoster roster = TRosterOpen(schedule);

        Assert.Empty(TInterface.TRosterMenuRead(roster, claimed.TWorkId, null));
        LStrip strip = TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");
        Assert.Empty(TInterface.TRosterMenuRead(roster, claimed.TWorkId, strip));
        Assert.Empty(TInterface.TRosterMenuRead(roster, Guid.NewGuid(), null));
        TInterface.TRosterClose(roster);
    }
}
