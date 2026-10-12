using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Schedule")]
public sealed class TRosterCards
{
    private static QRoster TRosterOpen(TSchedule schedule)
    {
        QRoster roster = TInterfaceRoster.TRosterCreate(
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
        QRoster roster = TRosterOpen(schedule);

        Assert.Equal(2, TInterfaceRoster.TRosterCardsRead(roster).Count);
        QRosterCard card = TInterfaceRoster.TRosterCardsRead(roster)[0];
        Assert.Equal(first, card.QRosterCardBatch);
        Assert.Equal(QRosterCard.QRosterCardPlain, card.QRosterCardKey);
        Assert.Equal(QRosterCard.QRosterCardPlain, card.QRosterCardBody);
        Assert.False(card.QRosterCardCollapsed);
        Assert.Equal(2, card.QRosterCardFiles.Count);
        QRosterRow row = Assert.Single(card.QRosterCardFiles[0].QRosterFileRows);
        Assert.Equal(one.TWorkId, row.QRosterRowId);
        Assert.True(row.QRosterRowLast);
        Assert.False(row.QRosterRowStage);
        Assert.Equal(QRosterRow.QRosterShadePlain, row.QRosterRowShade);
        Assert.Equal("Pending", row.QRosterRowKey);
        Assert.Equal("-", row.QRosterRowProgress);
        Assert.Equal("-", row.QRosterRowOwner);
        Assert.Equal(new[] { one.TWorkId, two.TWorkId, three.TWorkId }, roster.QRosterOrderedIds);
        TInterfaceRoster.TRosterClose(roster);
    }

    [Fact]
    public void Rebuild_MarksCompletedBatchesDone_AndCollapsesThemWhenAsked()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        schedule.TScheduleAdd(schedule.TWorkCreate(batch, "one"));
        schedule.TScheduleCommit(schedule.TScheduleNextClaim(), true);
        QRoster roster = TRosterOpen(schedule);
        LPreferenceState before = LPreference.LPreferenceStateCurrent;
        try
        {
            QRosterCard done = Assert.Single(TInterfaceRoster.TRosterCardsRead(roster));
            Assert.Equal(QRosterCard.QRosterCardDone, done.QRosterCardKey);
            Assert.Equal(QRosterCard.QRosterCardDone, done.QRosterCardBody);
            Assert.False(done.QRosterCardCollapsed);
            Assert.Equal("Done", done.QRosterCardFiles[0].QRosterFileRows[0].QRosterRowKey);

            Assert.True(TInterfaceRoster.TRosterDoneSet(roster, true));
            Assert.False(TInterfaceRoster.TRosterDoneSet(roster, true));
            Assert.True(roster.QRosterCollapseDone);
            Assert.True(TInterfaceRoster.TRosterCollapsedCheck(roster, batch));
            Assert.True(Assert.Single(TInterfaceRoster.TRosterCardsRead(roster)).QRosterCardCollapsed);

            TInterfaceRoster.TRosterCardSelect(roster, batch);
            QRosterCard selected = Assert.Single(TInterfaceRoster.TRosterCardsRead(roster));
            Assert.Equal(QRosterCard.QRosterCardSelected, selected.QRosterCardKey);
            Assert.Equal(QRosterCard.QRosterCardDone, selected.QRosterCardBody);
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
        QRoster roster = TRosterOpen(schedule);
        QRosterEntry firstHead = TInterfaceRoster.TRosterEntriesRead(roster)[0];

        Assert.True(TInterfaceRoster.TRosterCollapseToggle(roster, second));

        IReadOnlyList<QRosterEntry> entries = TInterfaceRoster.TRosterEntriesRead(roster);
        Assert.Same(firstHead, entries[0]);
        Assert.True(TInterfaceRoster.TRosterCardsRead(roster)[1].QRosterCardCollapsed);
        Assert.Equal(QRosterEntry.QRosterFrameWhole, entries[^1].QRosterEntryFrame);
        Assert.False(TInterfaceRoster.TRosterCollapseToggle(roster, second));
        Assert.False(TInterfaceRoster.TRosterCardsRead(roster)[1].QRosterCardCollapsed);
        Assert.Equal(
            QRosterEntry.QRosterFrameBottom, TInterfaceRoster.TRosterEntriesRead(roster)[^1].QRosterEntryFrame);
        TInterfaceRoster.TRosterClose(roster);
    }

    [Fact]
    public void Rebuild_KeepsUnchangedCardEntries()
    {
        using var schedule = new TSchedule();
        Guid first = Guid.NewGuid();
        schedule.TScheduleAdd(schedule.TWorkCreate(first, "one"));
        QRoster roster = TRosterOpen(schedule);
        QRosterEntry[] before = TInterfaceRoster.TRosterEntriesRead(roster).ToArray();

        schedule.TScheduleAdd(schedule.TWorkCreate(Guid.NewGuid(), "two"));

        IReadOnlyList<QRosterEntry> after = TInterfaceRoster.TRosterEntriesRead(roster);
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
        schedule.TScheduleAdd(origin);
        schedule.TScheduleAdd(derived);

        Assert.Equal(1, TInterfaceRoster.TLineageInitialRead(schedule.TScheduleRead(), items));
        Assert.Empty(TInterfaceRoster.TLineageStageRead(schedule.TScheduleRead(), items));
        CLineageAnswer lineage = TInterfaceRoster.TRosterLineageRead(schedule.TScheduleRead(), items);
        CLineageEntry entry = Assert.Single(lineage.CLineageEntries);
        Assert.Equal(2, entry.CLineageEntryIds.Count);
        Assert.Equal(origin.TWorkItem.LWorkSourcePath, entry.CLineageEntrySubject);
        Assert.NotEmpty(TInterfaceRoster.TLineageTitleFormat(entry));
        Assert.Equal(1, lineage.CLineageBatches.Single().CLineageBatchInitial);
        Assert.Empty(lineage.CLineageBatches.Single().CLineageBatchStages);
        Assert.Equal("-", TInterfaceRoster.TLineageRatioFormat(lineage, derived.TWorkItem));
        origin.TWorkItem.LWorkSourceBytes = 1000;
        Assert.StartsWith("12.3", TInterfaceRoster.TLineageRatioFormat(
            TInterfaceRoster.TRosterLineageRead(schedule.TScheduleRead(), items), derived.TWorkItem));
        Assert.NotEmpty(TInterfaceRoster.TRosterTitleFormat(lineage));
    }

    [Fact]
    public void ElapsedTick_RefreshesTheDetailOnlyWhileVisibleAndRunning()
    {
        using var schedule = new TSchedule();
        schedule.TScheduleAdd(schedule.TWorkCreate(Guid.NewGuid(), "one"));
        QRoster roster = TRosterOpen(schedule);
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
