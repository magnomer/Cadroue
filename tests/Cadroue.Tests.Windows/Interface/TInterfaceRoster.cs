using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static class TInterfaceRoster
{
    internal static LStation TStationCreate(LScheduleContract schedule)
    {
        TStationAttach(schedule);
        return LStation.LStationCreate("Test worklist");
    }

    internal static void TStationAttach(LScheduleContract schedule)
    {
        LStation.LStationSchedule = schedule;
        LStation.LStationPost = action => action();
        LStation.LStationProgramSource = () => "ffmpeg";
        LStation.LStationPreferenceSource = () => LPreference.LPreferenceStateCurrent;
    }

    internal static LRoster TRosterCreate(LScheduleContract schedule, LStation station)
    {
        CAtelier atelier = TInterface.TAtelierCreate();
        return new(atelier.CAtelierUsher, atelier.CAtelierLineage, schedule, station);
    }
    internal static void TRosterClose(LRoster roster) => roster.LRosterClose();
    internal static void TRosterRebuild(LRoster roster) => roster.LRosterRebuild();
    internal static void TRosterStepSelect(LRoster roster, Guid id, bool range, bool toggle) =>
        roster.QRosterSelection.LRosterStepSelect(id, range, toggle);
    internal static void TRosterCardSelect(LRoster roster, Guid batchId) =>
        roster.QRosterSelection.LRosterCardSelect(batchId);
    internal static bool TRosterSelectedCheck(LRoster roster, Guid id) =>
        roster.QRosterSelection.LRosterSelectedCheck(id);
    internal static bool TRosterCollapseToggle(LRoster roster, Guid batchId) =>
        roster.QRosterCollapse.LRosterCollapseToggle(batchId);
    internal static bool TRosterCollapsedCheck(LRoster roster, Guid batchId) =>
        roster.QRosterCollapse.LRosterCollapsedCheck(batchId);
    internal static IReadOnlyList<LRosterEntry> TRosterEntriesRead(LRoster roster) => roster.LRosterEntries;
    internal static IReadOnlyList<LRosterCard> TRosterCardsRead(LRoster roster) =>
        roster.LRosterEntries
            .Where(entry => entry.LRosterEntryKind == LRosterEntry.LRosterKindHead)
            .Select(entry => entry.LRosterEntryCard)
            .ToArray();
    internal static void TRosterDetailAttach(LRoster roster, Action handler) => roster.LRosterDetailApplied += handler;
    internal static void TRosterDeferAttach(LRoster roster, Action handler) =>
        roster.QRosterDetail.LRosterDetailDeferred += handler;
    internal static void TRosterWarningAttach(LRoster roster, Action<string, string> handler) =>
        roster.LRosterWarningShown += handler;
    internal static void TRosterRemove(LRoster roster, Guid batchId) => roster.LRosterRemove(batchId);
    internal static IReadOnlyList<LRosterItem> TRosterMenuRead(LRoster roster, Guid id, LStrip? strip) =>
        roster.LRosterMenu.LRosterMenuRead(id, strip);
    internal static void TRosterItemRun(LRosterItem item) => item.LRosterItemAction();
    internal static bool TRosterDoneSet(LRoster roster, bool? isChecked) => roster.LRosterDoneSet(isChecked);
    internal static bool TRosterSharedSet(LRoster roster, bool? isChecked) => roster.LRosterSharedSet(isChecked);
    internal static LWorkItem? TRosterSelectRead(LRoster roster) => roster.LRosterSelectRead();
    internal static IReadOnlyList<LWorkItem> TRosterSelectionRead(LRoster roster) => roster.LRosterSelectionRead();
    internal static void TRosterElapsedTick(LRoster roster, bool visible) => roster.LRosterElapsedTick(visible);
    internal static void TRosterDetailRun(LRoster roster) => roster.QRosterDetail.LRosterDetailRun();
    internal static string TRosterOwnerFormat(LWorkItem work, bool owned) => LRosterRow.LRosterOwnerFormat(work, owned);
    internal static string TRosterProgressFormat(LWorkItem work) => LRosterRow.LRosterProgressFormat(work);
    internal static string TRosterPriorityFormat(LWorkPriority priority) => LRosterRow.LRosterPriorityFormat(priority);
    internal static string TRosterStateFormat(LWorkState state) => LRosterRow.LRosterStateFormat(state);
    internal static string TRosterKeyRead(LWorkState state) => LRosterRow.LRosterKeyRead(state);
    internal static string TRosterPhaseFormat(LWorkState state, LWorkPhase phase) =>
        LRosterRow.LRosterPhaseFormat(state, phase);
    internal static string TRosterSpanFormat(TimeSpan span) => LRosterRow.LRosterSpanFormat(span);
    internal static string TRosterShadeResolve(bool selected, bool cardSelected, bool stage) =>
        LRosterRow.LRosterShadeResolve(selected, cardSelected, stage);
    internal static LRosterRow TRosterRowCreate(
        CLineageAnswer answer, LWorkItem work, bool last, bool stage, bool owned, string shade) =>
        LRosterRow.LRosterRowCreate(
            TInterface.TAtelierCreate().CAtelierLineage,
            work,
            answer.CLineageSteps.First(step => step.CLineageStepId == work.LWorkId),
            last,
            stage,
            owned,
            shade);
    internal static string TRosterTitleFormat(CLineageAnswer answer)
    {
        CLineageBatch batch = answer.CLineageBatches.Single();
        return LRosterCard.LRosterTitleFormat(batch.CLineageBatchEarliest, batch.CLineageBatchInitial);
    }
    internal static CLineageAnswer TRosterLineageRead(LScheduleContract schedule, IReadOnlyList<LWorkItem> items)
    {
        LScheduleContract? previous = LStation.LStationSchedule;
        LStation.LStationSchedule = schedule;
        try
        {
            return TInterface.TAtelierCreate().CAtelierLineage.CLineageRead(
                items.Select(item => item.LWorkId).ToArray());
        }
        finally
        {
            LStation.LStationSchedule = previous;
        }
    }
    internal static string TLineageStepFormat(CLineageAnswer answer, LWorkItem work) =>
        TLineageRowCreate(answer, work).LRosterRowStep;
    internal static string TLineageRatioFormat(CLineageAnswer answer, LWorkItem work) =>
        TLineageRowCreate(answer, work).LRosterRowRatio;
    private static LRosterRow TLineageRowCreate(CLineageAnswer answer, LWorkItem work) =>
        TRosterRowCreate(answer, work, last: true, stage: false, owned: false, LRosterRow.LRosterShadePlain);
    internal static string TLineageTitleFormat(CLineageEntry entry) => LLineage.LLineageTitleFormat(entry);
    internal static HashSet<Guid> TLineageStageRead(LScheduleContract schedule, IReadOnlyList<LWorkItem> items) =>
        TRosterLineageRead(schedule, items).CLineageBatches.Single().CLineageBatchStages.ToHashSet();
    internal static int TLineageInitialRead(LScheduleContract schedule, IReadOnlyList<LWorkItem> items) =>
        TRosterLineageRead(schedule, items).CLineageBatches.Single().CLineageBatchInitial;
    internal static void TWorkOutputSet(LWorkItem work, string path) => work.LWorkOutputSet(path, work.LWorkOutputName);
    internal static LRosterDetailKind TRosterKindRead(LRoster roster) =>
        LRosterDetail.LRosterKindRead(roster);
    internal static LRosterDetail TRosterDetailRead(LRoster roster) => LRosterDetail.LRosterDetailRead(roster);
    internal static LRosterDetail TRosterDetailCreate(LScheduleContract schedule, LWorkItem work, string owner)
    {
        CAtelier atelier = TInterface.TAtelierCreate();
        CLineageStep step = TRosterLineageRead(schedule, [work]).CLineageSteps.Single(
            item => item.CLineageStepId == work.LWorkId);
        return LRosterDetail.LRosterDetailCreate(
            work,
            owner,
            step.CLineageStepSource,
            atelier.CAtelierLineage.CLineageTitleRead(
                step.CLineageStepBatch, step.CLineageStepRelay, step.CLineageStepTab),
            [step.CLineageSourceExtension, step.CLineageOutputExtension]);
    }
    internal static void TStripTitleAttach(LStrip? strip) =>
        LCartographer.LCartographerTitleSource = strip is null ? null : strip.LStripTitleRead;
    internal static IReadOnlyList<LRosterBar> TRosterBarsCreate(long? source, long? output) =>
        LRosterDetail.LRosterBarsCreate(source, output);
    internal static LRosterCompareRow TRosterCompareCreate(string source, string output) =>
        LRosterDetail.LRosterCompareCreate(source, output);
    internal static string TRosterMebiFormat(long? bytes) => LRosterFormatter.LRosterMebiFormat(bytes);
    internal static string TRosterSpentFormat(LWorkItem work) => LRosterFormatter.LRosterSpentFormat(work);
    internal static string TRosterSpeedFormat(LWorkItem work) => LRosterFormatter.LRosterSpeedFormat(work);
    internal static string TRosterElapsedFormat(TimeSpan spent) =>
        LRosterFormatter.LRosterElapsedFormat(spent);
    internal static string TRosterClockFormat(TimeSpan span) => LRosterFormatter.LRosterClockFormat(span);
    internal static string TRosterStampFormat(DateTimeOffset? stamp) => LRosterFormatter.LRosterStampFormat(stamp);
    internal static bool TRosterReencodeCheck(string mode) => LRosterFormatter.LRosterReencodeCheck(mode);
    internal static LSummary TSummaryRead(LScheduleContract schedule, Guid batchId)
    {
        LRoster roster = TRosterCreate(schedule, TStationCreate(schedule));
        try
        {
            roster.LRosterRebuild();
            roster.QRosterSelection.LRosterCardSelect(batchId);
            return LSummary.LSummaryRead(roster);
        }
        finally
        {
            roster.LRosterClose();
        }
    }
    internal static IReadOnlyList<Guid> TRosterIdsRead(LScheduleContract schedule)
    {
        LRoster roster = TRosterCreate(schedule, TStationCreate(schedule));
        try
        {
            roster.LRosterRebuild();
            return roster.LRosterOrderedIds.ToArray();
        }
        finally
        {
            roster.LRosterClose();
        }
    }
    internal static Guid TStationRunnerRead(LStation station) => station.LStationRunner.LRunnerIdentity;
    internal static void TStationClose(LStation station) => station.LStationClose();
    internal static string? TSummaryMeterFormat(IReadOnlyList<LWorkItem> items, long? output) =>
        LSummary.LSummaryMeterFormat(items, output);
    internal static string TSummaryFilesFormat(int count) => LSummary.LSummaryFilesFormat(count);
}
