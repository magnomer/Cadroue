using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static class TInterfaceRoster
{
    internal static LStation TStationCreate(LScheduleContract schedule)
    {
        LStation.LStationSchedule = schedule;
        LStation.LStationPost = action => action();
        LStation.LStationProgramSource = () => "ffmpeg";
        LStation.LStationPreferenceSource = () => LPreference.LPreferenceStateCurrent;
        return LStation.LStationCreate("Test worklist");
    }

    internal static LRoster TRosterCreate(LScheduleContract schedule, LStation station) =>
        new(TInterface.TAtelierCreate().CAtelierUsher, schedule, station);
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
        LWorkItem work, LLineageEntry lineage, bool last, bool stage, bool owned, string shade) =>
        LRosterRow.LRosterRowCreate(
            TInterface.TAtelierCreate().CAtelierUsher, work, lineage, last, stage, owned, shade);
    internal static string TRosterTitleFormat(IReadOnlyList<LWorkItem> items) =>
        LRosterCard.LRosterTitleFormat(TInterface.TAtelierCreate().CAtelierUsher, items);
    internal static IReadOnlyList<LLineageEntry> TLineageRead(
        IReadOnlyList<LWorkItem> items, Func<LWorkItem, Guid> read) =>
        LLineage.LLineageRead(TInterface.TAtelierCreate().CAtelierUsher, items, read);
    internal static string TLineageStepFormat(LWorkItem work, string subject) =>
        LLineage.LLineageStepFormat(TInterface.TAtelierCreate().CAtelierUsher, work, subject);
    internal static string TLineageRatioFormat(LWorkItem work, string subject, long? origin) =>
        LLineage.LLineageRatioFormat(TInterface.TAtelierCreate().CAtelierUsher, work, subject, origin);
    internal static string TLineageTitleFormat(LLineageEntry entry) =>
        LLineage.LLineageTitleFormat(TInterface.TAtelierCreate().CAtelierUsher, entry);
    internal static HashSet<Guid> TLineageStageRead(IReadOnlyList<LWorkItem> items) =>
        LLineage.LLineageStageRead(TInterface.TAtelierCreate().CAtelierUsher, items);
    internal static int TLineageInitialRead(IReadOnlyList<LWorkItem> items) =>
        LLineage.LLineageInitialRead(TInterface.TAtelierCreate().CAtelierUsher, items);
    internal static long? TLineageSourceRead(LWorkItem work) => LLineage.LLineageSourceRead(work);
    internal static string? TLineagePathRead(string path) =>
        LLineage.LLineagePathRead(TInterface.TAtelierCreate().CAtelierUsher, path);
    internal static LRosterDetailKind TRosterKindRead(LRoster roster) =>
        LRosterDetail.LRosterKindRead(roster);
    internal static LRosterDetail TRosterDetailRead(LRoster roster) => LRosterDetail.LRosterDetailRead(roster);
    internal static LRosterDetail TRosterDetailCreate(LWorkItem? work, string owner) =>
        LRosterDetail.LRosterDetailCreate(TInterface.TAtelierCreate().CAtelierUsher, work, owner);
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
    internal static string TRosterContainerFormat(string path) =>
        LRosterFormatter.LRosterContainerFormat(TInterface.TAtelierCreate().CAtelierUsher, path);
    internal static bool TRosterReencodeCheck(string mode) => LRosterFormatter.LRosterReencodeCheck(mode);
    internal static LSummary TSummaryRead(IReadOnlyList<LWorkItem> items) =>
        LSummary.LSummaryRead(TInterface.TAtelierCreate().CAtelierUsher, items);
    internal static (long?, long?) TSummarySizeRead(IReadOnlyList<LWorkItem> items) =>
        LSummary.LSummarySizeRead(TInterface.TAtelierCreate().CAtelierUsher, items);
    internal static (IReadOnlyList<string>, IReadOnlyList<string>) TSummaryPathsRead(
        IReadOnlyList<LWorkItem> items) => LSummary.LSummaryPathsRead(TInterface.TAtelierCreate().CAtelierUsher, items);
    internal static string? TSummaryMeterFormat(IReadOnlyList<LWorkItem> items, long? output) =>
        LSummary.LSummaryMeterFormat(items, output);
    internal static string TSummaryFilesFormat(int count) => LSummary.LSummaryFilesFormat(count);
}
