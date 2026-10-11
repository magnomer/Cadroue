using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Schedule")]
public sealed class TRosterCards
{
    private static LRoster TRosterOpen(TSchedule schedule)
    {
        LRoster roster = TInterfaceRoster.TRosterCreate(
            schedule.TScheduleRead(), TInterfaceRoster.TStationCreate(schedule.TScheduleRead()));
        TInterfaceRoster.TRosterRebuild(roster);
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

        Assert.Equal(2, TInterfaceRoster.TRosterCardsRead(roster).Count);
        LRosterCard card = TInterfaceRoster.TRosterCardsRead(roster)[0];
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
        TInterfaceRoster.TRosterClose(roster);
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
            LRosterCard done = Assert.Single(TInterfaceRoster.TRosterCardsRead(roster));
            Assert.Equal(LRosterCard.LRosterCardDone, done.LRosterCardKey);
            Assert.Equal(LRosterCard.LRosterCardDone, done.LRosterCardBody);
            Assert.False(done.LRosterCardCollapsed);
            Assert.Equal("Done", done.LRosterCardFiles[0].LRosterFileRows[0].LRosterRowKey);

            Assert.True(TInterfaceRoster.TRosterDoneSet(roster, true));
            Assert.False(TInterfaceRoster.TRosterDoneSet(roster, true));
            Assert.True(roster.LRosterCollapseDone);
            Assert.True(TInterfaceRoster.TRosterCollapsedCheck(roster, batch));
            Assert.True(Assert.Single(TInterfaceRoster.TRosterCardsRead(roster)).LRosterCardCollapsed);

            TInterfaceRoster.TRosterCardSelect(roster, batch);
            LRosterCard selected = Assert.Single(TInterfaceRoster.TRosterCardsRead(roster));
            Assert.Equal(LRosterCard.LRosterCardSelected, selected.LRosterCardKey);
            Assert.Equal(LRosterCard.LRosterCardDone, selected.LRosterCardBody);
        }
        finally
        {
            TInterface.TPreferenceRestore(before);
            TInterfaceRoster.TRosterClose(roster);
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
        LRosterEntry firstHead = TInterfaceRoster.TRosterEntriesRead(roster)[0];

        Assert.True(TInterfaceRoster.TRosterCollapseToggle(roster, second));

        IReadOnlyList<LRosterEntry> entries = TInterfaceRoster.TRosterEntriesRead(roster);
        Assert.Same(firstHead, entries[0]);
        Assert.True(TInterfaceRoster.TRosterCardsRead(roster)[1].LRosterCardCollapsed);
        Assert.Equal(LRosterEntry.LRosterFrameWhole, entries[^1].LRosterEntryFrame);
        Assert.False(TInterfaceRoster.TRosterCollapseToggle(roster, second));
        Assert.False(TInterfaceRoster.TRosterCardsRead(roster)[1].LRosterCardCollapsed);
        Assert.Equal(
            LRosterEntry.LRosterFrameBottom, TInterfaceRoster.TRosterEntriesRead(roster)[^1].LRosterEntryFrame);
        TInterfaceRoster.TRosterClose(roster);
    }

    [Fact]
    public void Rebuild_KeepsUnchangedCardEntries()
    {
        using var schedule = new TSchedule();
        Guid first = Guid.NewGuid();
        schedule.TScheduleAdd(schedule.TWorkCreate(first, "one"));
        LRoster roster = TRosterOpen(schedule);
        LRosterEntry[] before = TInterfaceRoster.TRosterEntriesRead(roster).ToArray();

        schedule.TScheduleAdd(schedule.TWorkCreate(Guid.NewGuid(), "two"));

        IReadOnlyList<LRosterEntry> after = TInterfaceRoster.TRosterEntriesRead(roster);
        Assert.Equal(2, TInterfaceRoster.TRosterCardsRead(roster).Count);
        Assert.Equal(before.Length * 2, after.Count);
        Assert.All(before, entry => Assert.Contains(after, kept => ReferenceEquals(kept, entry)));
        TInterfaceRoster.TRosterClose(roster);
    }

    [Fact]
    public void Lineage_TitleAndStageFollowDerivedOutputs()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork origin = schedule.TWorkCreate(batch, "origin");
        TScheduleWork derived = schedule.TWorkDeriveCreate(origin, "derived");
        LWorkItem[] items = [origin.TWorkItem, derived.TWorkItem];

        Assert.Equal(1, TInterfaceRoster.TLineageInitialRead(items));
        Assert.Empty(TInterfaceRoster.TLineageStageRead(items));
        IReadOnlyList<LLineageEntry> lineages = TInterfaceRoster.TLineageRead(items, _ => batch);
        LLineageEntry entry = Assert.Single(lineages);
        Assert.Equal(2, entry.LLineageEntryItems.Count);
        Assert.Equal(origin.TWorkItem.LWorkSourcePath, entry.LLineageEntrySubject);
        Assert.NotEmpty(TInterfaceRoster.TLineageTitleFormat(entry));
        Assert.Equal("-", TInterfaceRoster.TLineageRatioFormat(derived.TWorkItem, entry.LLineageEntrySubject, null));
        Assert.StartsWith(
            "12.3", TInterfaceRoster.TLineageRatioFormat(derived.TWorkItem, entry.LLineageEntrySubject, 1000));
        Assert.NotEmpty(TInterfaceRoster.TRosterTitleFormat(items));
    }

    [Fact]
    public void ElapsedTick_RefreshesTheDetailOnlyWhileVisibleAndRunning()
    {
        using var schedule = new TSchedule();
        schedule.TScheduleAdd(schedule.TWorkCreate(Guid.NewGuid(), "one"));
        LRoster roster = TRosterOpen(schedule);
        int detailApplied = 0;
        TInterfaceRoster.TRosterDetailAttach(roster, () => detailApplied++);

        TInterfaceRoster.TRosterElapsedTick(roster, true);
        Assert.Equal(0, detailApplied);

        schedule.TScheduleNextClaim();
        int afterClaim = detailApplied;
        TInterfaceRoster.TRosterElapsedTick(roster, false);
        Assert.Equal(afterClaim, detailApplied);
        TInterfaceRoster.TRosterElapsedTick(roster, true);
        Assert.Equal(afterClaim + 1, detailApplied);
        TInterfaceRoster.TRosterClose(roster);
    }
}
