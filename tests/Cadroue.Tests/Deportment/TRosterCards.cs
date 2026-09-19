using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TRosterCards
{
    private static LRoster TRosterOpen(TSchedule schedule)
    {
        LRoster roster = TInterface.TRosterCreate(
            schedule.TScheduleRead(), TInterface.TStationCreate(schedule.TScheduleRead()));
        TInterface.TRosterRebuild(roster);
        return roster;
    }

    [Fact]
    public void Rebuild_GroupsRecordsIntoCardsFilesAndRows()
    {
        using var schedule = new TSchedule();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        TScheduleWork one = schedule.TWorkCreate(first, "one");
        TScheduleWork two = schedule.TWorkCreate(first, "two");
        TScheduleWork three = schedule.TWorkCreate(second, "three");
        schedule.TScheduleAdd(one, two, three);
        LRoster roster = TRosterOpen(schedule);

        Assert.Equal(2, roster.LRosterCards.Count);
        LRosterCard card = roster.LRosterCards[0];
        Assert.Equal(first, card.LRosterCardBatch);
        Assert.Equal(LRosterCard.LRosterCardPlain, card.LRosterCardKey);
        Assert.Equal(LRosterCard.LRosterCardPlain, card.LRosterCardBody);
        Assert.False(card.LRosterCardCollapsed);
        Assert.Equal(2, card.LRosterCardFiles.Count);
        LRosterRow row = Assert.Single(card.LRosterCardFiles[0].LRosterFileRows);
        Assert.Equal(one.TWorkId, row.LRosterRowId);
        Assert.True(row.LRosterRowLast);
        Assert.False(row.LRosterRowStage);
        Assert.Equal(LRosterRow.LRosterShadePlain, row.LRosterRowShade);
        Assert.Equal("Pending", row.LRosterRowKey);
        Assert.Equal("-", row.LRosterRowProgress);
        Assert.Equal("-", row.LRosterRowOwner);
        Assert.Equal(new[] { one.TWorkId, two.TWorkId, three.TWorkId }, roster.LRosterOrderedIds);
        TInterface.TRosterClose(roster);
    }

    [Fact]
    public void Rebuild_MarksCompletedBatchesDone_AndCollapsesThemWhenAsked()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        schedule.TScheduleAdd(schedule.TWorkCreate(batch, "one"));
        schedule.TScheduleCommit(schedule.TScheduleNextClaim(), true);
        LRoster roster = TRosterOpen(schedule);
        LPreferenceState before = LPreference.LPreferenceStateCurrent;
        try
        {
            LRosterCard done = Assert.Single(roster.LRosterCards);
            Assert.Equal(LRosterCard.LRosterCardDone, done.LRosterCardKey);
            Assert.Equal(LRosterCard.LRosterCardDone, done.LRosterCardBody);
            Assert.False(done.LRosterCardCollapsed);
            Assert.Equal("Done", done.LRosterCardFiles[0].LRosterFileRows[0].LRosterRowKey);

            Assert.True(TInterface.TRosterDoneSet(roster, true));
            Assert.False(TInterface.TRosterDoneSet(roster, true));
            Assert.True(roster.LRosterCollapseDone);
            Assert.True(TInterface.TRosterCollapsedCheck(roster, batch));
            Assert.True(Assert.Single(roster.LRosterCards).LRosterCardCollapsed);

            TInterface.TRosterCardSelect(roster, batch);
            LRosterCard selected = Assert.Single(roster.LRosterCards);
            Assert.Equal(LRosterCard.LRosterCardSelected, selected.LRosterCardKey);
            Assert.Equal(LRosterCard.LRosterCardDone, selected.LRosterCardBody);
        }
        finally
        {
            TInterface.TPreferenceRestore(before);
            TInterface.TRosterClose(roster);
        }
    }

    [Fact]
    public void CollapseToggle_ReplacesOnlyThatCard()
    {
        using var schedule = new TSchedule();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        schedule.TScheduleAdd(schedule.TWorkCreate(first, "one"), schedule.TWorkCreate(second, "two"));
        LRoster roster = TRosterOpen(schedule);
        var applied = new List<(int, LRosterCard)>();
        int cardsApplied = 0;
        TInterface.TRosterCardAttach(roster, (index, card) => applied.Add((index, card)));
        TInterface.TRosterCardsAttach(roster, () => cardsApplied++);

        Assert.True(TInterface.TRosterCollapseToggle(roster, second));

        (int index, LRosterCard card) = Assert.Single(applied);
        Assert.Equal(1, index);
        Assert.True(card.LRosterCardCollapsed);
        Assert.Same(card, roster.LRosterCards[1]);
        Assert.Equal(0, cardsApplied);
        Assert.False(TInterface.TRosterCollapseToggle(roster, second));
        Assert.False(roster.LRosterCards[1].LRosterCardCollapsed);
        TInterface.TRosterClose(roster);
    }

    [Fact]
    public void Lineage_TitleAndStageFollowDerivedOutputs()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork origin = schedule.TWorkCreate(batch, "origin");
        TScheduleWork derived = schedule.TWorkDeriveCreate(origin, "derived");
        LWorkItem[] items = [origin.TWorkItem, derived.TWorkItem];

        Assert.Equal(1, TInterface.TLineageInitialRead(items));
        Assert.Empty(TInterface.TLineageStageRead(items));
        IReadOnlyList<LLineageEntry> lineages = TInterface.TLineageRead(items, _ => batch);
        LLineageEntry entry = Assert.Single(lineages);
        Assert.Equal(2, entry.LLineageEntryItems.Count);
        Assert.Equal(origin.TWorkItem.LWorkSourcePath, entry.LLineageEntrySubject);
        Assert.NotEmpty(TInterface.TLineageTitleFormat(entry));
        Assert.Equal("-", TInterface.TLineageRatioFormat(derived.TWorkItem, entry.LLineageEntrySubject, null));
        Assert.StartsWith(
            "12.3", TInterface.TLineageRatioFormat(derived.TWorkItem, entry.LLineageEntrySubject, 1000));
        Assert.NotEmpty(TInterface.TRosterTitleFormat(items));
    }

    [Fact]
    public void ElapsedTick_RefreshesTheDetailOnlyWhileVisibleAndRunning()
    {
        using var schedule = new TSchedule();
        schedule.TScheduleAdd(schedule.TWorkCreate(Guid.NewGuid(), "one"));
        LRoster roster = TRosterOpen(schedule);
        int detailApplied = 0;
        TInterface.TRosterDetailAttach(roster, () => detailApplied++);

        TInterface.TRosterElapsedTick(roster, true);
        Assert.Equal(0, detailApplied);

        schedule.TScheduleNextClaim();
        int afterClaim = detailApplied;
        TInterface.TRosterElapsedTick(roster, false);
        Assert.Equal(afterClaim, detailApplied);
        TInterface.TRosterElapsedTick(roster, true);
        Assert.Equal(afterClaim + 1, detailApplied);
        TInterface.TRosterClose(roster);
    }
}
