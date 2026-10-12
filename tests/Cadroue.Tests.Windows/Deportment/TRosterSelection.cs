using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Schedule")]
public sealed class TRosterSelection
{
    private static QRoster TRosterOpen(TSchedule schedule)
    {
        QRoster roster = TInterfaceRoster.TRosterCreate(
            schedule.TScheduleRead(), TInterfaceRoster.TStationCreate(schedule.TScheduleRead()));
        TInterfaceRoster.TRosterRebuild(roster);
        return roster;
    }

    private static Guid[] TRosterQueueAdd(TSchedule schedule, Guid batchId, int count)
    {
        TScheduleWork[] work = Enumerable.Range(0, count)
            .Select(index => schedule.TWorkCreate(batchId, "step" + index))
            .ToArray();
        schedule.TScheduleAdd(work);
        return work.Select(item => item.TWorkId).ToArray();
    }

    [Fact]
    public void StepSelect_Plain_ReplacesSelection()
    {
        using var schedule = new TSchedule();
        Guid[] ids = TRosterQueueAdd(schedule, Guid.NewGuid(), 3);
        QRoster roster = TRosterOpen(schedule);

        TInterfaceRoster.TRosterStepSelect(roster, ids[0], false, false);
        TInterfaceRoster.TRosterStepSelect(roster, ids[2], false, false);

        Assert.False(TInterfaceRoster.TRosterSelectedCheck(roster, ids[0]));
        Assert.True(TInterfaceRoster.TRosterSelectedCheck(roster, ids[2]));
        Assert.Equal(ids[2], roster.QRosterSelection.QRosterCurrentId);
        Assert.Equal(ids[2], TInterfaceRoster.TRosterSelectRead(roster)?.LWorkId);
        QRosterRow third = TInterfaceRoster.TRosterCardsRead(roster)[0].QRosterCardFiles[2].QRosterFileRows[0];
        Assert.Equal(QRosterRow.QRosterShadeSelected, third.QRosterRowShade);
        TInterfaceRoster.TRosterClose(roster);
    }

    [Fact]
    public void StepSelect_RangeAndToggle()
    {
        using var schedule = new TSchedule();
        Guid[] ids = TRosterQueueAdd(schedule, Guid.NewGuid(), 4);
        QRoster roster = TRosterOpen(schedule);

        TInterfaceRoster.TRosterStepSelect(roster, ids[3], false, false);
        TInterfaceRoster.TRosterStepSelect(roster, ids[1], true, false);
        Assert.Equal(3, roster.QRosterSelection.QRosterSelectedIds.Count);
        Assert.False(TInterfaceRoster.TRosterSelectedCheck(roster, ids[0]));
        Assert.Equal(ids[3], roster.QRosterSelection.QRosterCurrentId);
        Assert.Equal(3, TInterfaceRoster.TRosterSelectionRead(roster).Count);

        TInterfaceRoster.TRosterStepSelect(roster, ids[2], false, true);
        Assert.False(TInterfaceRoster.TRosterSelectedCheck(roster, ids[2]));
        Assert.Equal(ids[2], roster.QRosterSelection.QRosterCurrentId);
        TInterfaceRoster.TRosterClose(roster);
    }

    [Fact]
    public void CardSelect_ClearsSteps_AndStepClearsCard()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        Guid[] ids = TRosterQueueAdd(schedule, batch, 2);
        QRoster roster = TRosterOpen(schedule);

        TInterfaceRoster.TRosterStepSelect(roster, ids[0], false, false);
        TInterfaceRoster.TRosterCardSelect(roster, batch);
        Assert.Equal(batch, roster.QRosterSelection.QRosterCardId);
        Assert.Empty(roster.QRosterSelection.QRosterSelectedIds);
        Assert.Equal(Guid.Empty, roster.QRosterSelection.QRosterCurrentId);
        Assert.Equal(QRosterCard.QRosterCardSelected, TInterfaceRoster.TRosterCardsRead(roster)[0].QRosterCardKey);
        Assert.Null(TInterfaceRoster.TRosterSelectRead(roster));

        TInterfaceRoster.TRosterStepSelect(roster, ids[1], false, false);
        Assert.Equal(Guid.Empty, roster.QRosterSelection.QRosterCardId);
        Assert.Equal(QRosterCard.QRosterCardPlain, TInterfaceRoster.TRosterCardsRead(roster)[0].QRosterCardKey);
        TInterfaceRoster.TRosterClose(roster);
    }

    [Fact]
    public void Rebuild_DropsMissingSelectionAndCollapse()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        Guid[] ids = TRosterQueueAdd(schedule, batch, 3);
        QRoster roster = TRosterOpen(schedule);
        TInterfaceRoster.TRosterStepSelect(roster, ids[1], false, false);
        Assert.True(TInterfaceRoster.TRosterCollapseToggle(roster, batch));
        TInterfaceRoster.TRosterCardSelect(roster, batch);

        schedule.TScheduleAllClear();
        TInterfaceRoster.TRosterRebuild(roster);

        Assert.Empty(TInterfaceRoster.TRosterCardsRead(roster));
        Assert.Equal(Guid.Empty, roster.QRosterSelection.QRosterCardId);
        Assert.Equal(Guid.Empty, roster.QRosterSelection.QRosterCurrentId);
        Assert.Empty(roster.QRosterSelection.QRosterSelectedIds);
        Assert.False(TInterfaceRoster.TRosterCollapsedCheck(roster, batch));
        TInterfaceRoster.TRosterClose(roster);
    }

    [Fact]
    public void Close_IsIdempotent_AndStopsTheDetailRefresh()
    {
        using var schedule = new TSchedule();
        TRosterQueueAdd(schedule, Guid.NewGuid(), 1);
        QRoster roster = TRosterOpen(schedule);
        int detailApplied = 0;
        TInterfaceRoster.TRosterDetailAttach(roster, () => detailApplied++);

        TInterfaceRoster.TRosterClose(roster);
        TInterfaceRoster.TRosterClose(roster);
        TInterfaceRoster.TRosterDetailRun(roster);
        TInterfaceRoster.TRosterElapsedTick(roster, true);

        Assert.True(roster.QRosterClosed);
        Assert.Equal(0, detailApplied);
    }
}
