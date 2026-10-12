using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Schedule")]
public sealed class TRosterMenu
{
    private static QRoster TRosterOpen(TSchedule schedule)
    {
        QRoster roster = TInterfaceRoster.TRosterCreate(
            schedule.TScheduleRead(), TInterfaceRoster.TStationCreate(schedule.TScheduleRead()));
        TInterfaceRoster.TRosterRebuild(roster);
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
        QRoster roster = TRosterOpen(schedule);
        TInterfaceRoster.TRosterStepSelect(roster, one.TWorkId, false, false);
        TInterfaceRoster.TRosterStepSelect(roster, two.TWorkId, false, true);

        QRosterItem cancel = Assert.Single(TInterfaceRoster.TRosterMenuRead(roster, one.TWorkId, null));
        Assert.True(cancel.QRosterItemEnabled);
        Assert.Empty(cancel.QRosterItemIcon);
        TInterfaceRoster.TRosterItemRun(cancel);

        Assert.All(schedule.TScheduleRecordsRead(), item =>
            Assert.Equal(LWorkState.LWorkStateCancelled, item.TScheduleState));
        TInterfaceRoster.TRosterClose(roster);
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
        QRoster roster = TRosterOpen(schedule);
        TInterfaceRoster.TRosterStepSelect(roster, two.TWorkId, false, false);

        QRosterItem restart = Assert.Single(TInterfaceRoster.TRosterMenuRead(roster, one.TWorkId, null));
        TInterfaceRoster.TRosterItemRun(restart);

        IReadOnlyList<TScheduleItem> records = schedule.TScheduleRecordsRead();
        Assert.Equal(
            LWorkState.LWorkStatePending, records.Single(item => item.TWorkId == one.TWorkId).TScheduleState);
        Assert.Equal(
            LWorkState.LWorkStateCancelled, records.Single(item => item.TWorkId == two.TWorkId).TScheduleState);
        TInterfaceRoster.TRosterClose(roster);
    }

    [Fact]
    public void Done_WithoutAnOutputFileOrStrip_HasNoMenu()
    {
        using var schedule = new TSchedule();
        schedule.TScheduleAdd(schedule.TWorkCreate(Guid.NewGuid(), "one"));
        TScheduleWork claimed = schedule.TScheduleNextClaim();
        schedule.TScheduleCommit(claimed, true);
        QRoster roster = TRosterOpen(schedule);

        Assert.Empty(TInterfaceRoster.TRosterMenuRead(roster, claimed.TWorkId, null));
        QStrip strip = TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");
        Assert.Empty(TInterfaceRoster.TRosterMenuRead(roster, claimed.TWorkId, strip));
        Assert.Empty(TInterfaceRoster.TRosterMenuRead(roster, Guid.NewGuid(), null));
        TInterfaceRoster.TRosterClose(roster);
    }
}
