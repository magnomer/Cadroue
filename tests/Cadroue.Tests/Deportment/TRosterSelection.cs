using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TRosterSelection
{
    private static LRoster TRosterOpen(TSchedule schedule)
    {
        LRoster roster = TInterface.TRosterCreate(
            schedule.TScheduleRead(), TInterface.TStationCreate(schedule.TScheduleRead()));
        TInterface.TRosterRebuild(roster);
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
        LRoster roster = TRosterOpen(schedule);
        int cardsApplied = 0;
        TInterface.TRosterCardsAttach(roster, () => cardsApplied++);

        TInterface.TRosterStepSelect(roster, ids[0], false, false);
        TInterface.TRosterStepSelect(roster, ids[2], false, false);

        Assert.False(TInterface.TRosterSelectedCheck(roster, ids[0]));
        Assert.True(TInterface.TRosterSelectedCheck(roster, ids[2]));
        Assert.Equal(ids[2], roster.LRosterCurrentId);
        Assert.Equal(ids[2], TInterface.TRosterSelectRead(roster)?.LWorkId);
        Assert.Equal(2, cardsApplied);
        LRosterRow third = roster.LRosterCards[0].LRosterCardFiles[2].LRosterFileRows[0];
        Assert.Equal(LRosterRow.LRosterShadeSelected, third.LRosterRowShade);
        TInterface.TRosterClose(roster);
    }

    [Fact]
    public void StepSelect_RangeAndToggle()
    {
        using var schedule = new TSchedule();
        Guid[] ids = TRosterQueueAdd(schedule, Guid.NewGuid(), 4);
        LRoster roster = TRosterOpen(schedule);

        TInterface.TRosterStepSelect(roster, ids[3], false, false);
        TInterface.TRosterStepSelect(roster, ids[1], true, false);
        Assert.Equal(3, roster.LRosterSelectedIds.Count);
        Assert.False(TInterface.TRosterSelectedCheck(roster, ids[0]));
        Assert.Equal(ids[3], roster.LRosterCurrentId);
        Assert.Equal(3, TInterface.TRosterSelectionRead(roster).Count);

        TInterface.TRosterStepSelect(roster, ids[2], false, true);
        Assert.False(TInterface.TRosterSelectedCheck(roster, ids[2]));
        Assert.Equal(ids[2], roster.LRosterCurrentId);
        TInterface.TRosterClose(roster);
    }

    [Fact]
    public void CardSelect_ClearsSteps_AndStepClearsCard()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        Guid[] ids = TRosterQueueAdd(schedule, batch, 2);
        LRoster roster = TRosterOpen(schedule);

        TInterface.TRosterStepSelect(roster, ids[0], false, false);
        TInterface.TRosterCardSelect(roster, batch);
        Assert.Equal(batch, roster.LRosterCardId);
        Assert.Empty(roster.LRosterSelectedIds);
        Assert.Equal(Guid.Empty, roster.LRosterCurrentId);
        Assert.Equal(LRosterCard.LRosterCardSelected, roster.LRosterCards[0].LRosterCardKey);
        Assert.Null(TInterface.TRosterSelectRead(roster));

        TInterface.TRosterStepSelect(roster, ids[1], false, false);
        Assert.Equal(Guid.Empty, roster.LRosterCardId);
        Assert.Equal(LRosterCard.LRosterCardPlain, roster.LRosterCards[0].LRosterCardKey);
        TInterface.TRosterClose(roster);
    }

    [Fact]
    public void Rebuild_DropsMissingSelectionAndCollapse()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        Guid[] ids = TRosterQueueAdd(schedule, batch, 3);
        LRoster roster = TRosterOpen(schedule);
        TInterface.TRosterStepSelect(roster, ids[1], false, false);
        Assert.True(TInterface.TRosterCollapseToggle(roster, batch));
        TInterface.TRosterCardSelect(roster, batch);

        schedule.TScheduleAllClear();
        TInterface.TRosterRebuild(roster);

        Assert.Empty(roster.LRosterCards);
        Assert.Equal(Guid.Empty, roster.LRosterCardId);
        Assert.Equal(Guid.Empty, roster.LRosterCurrentId);
        Assert.Empty(roster.LRosterSelectedIds);
        Assert.False(TInterface.TRosterCollapsedCheck(roster, batch));
        TInterface.TRosterClose(roster);
    }

    [Fact]
    public void Close_IsIdempotent_AndStopsTheDetailRefresh()
    {
        using var schedule = new TSchedule();
        TRosterQueueAdd(schedule, Guid.NewGuid(), 1);
        LRoster roster = TRosterOpen(schedule);
        int detailApplied = 0;
        TInterface.TRosterDetailAttach(roster, () => detailApplied++);

        TInterface.TRosterClose(roster);
        TInterface.TRosterClose(roster);
        TInterface.TRosterDetailRun(roster);
        TInterface.TRosterElapsedTick(roster, true);

        Assert.True(roster.LRosterClosed);
        Assert.Equal(0, detailApplied);
    }
}
