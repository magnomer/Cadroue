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

    internal static QRoster TRosterCreate(LScheduleContract schedule, LStation station)
    {
        CAtelier atelier = TInterface.TAtelierCreate();
        return new(atelier.CAtelierUsher, atelier.CAtelierLineage, schedule, station);
    }
    internal static void TRosterClose(QRoster roster) => roster.QRosterClose();
    internal static void TRosterRebuild(QRoster roster) => roster.QRosterRebuild();
    internal static void TRosterStepSelect(QRoster roster, Guid id, bool range, bool toggle) =>
        roster.QRosterSelection.QRosterStepSelect(id, range, toggle);
    internal static void TRosterCardSelect(QRoster roster, Guid batchId) =>
        roster.QRosterSelection.QRosterCardSelect(batchId);
    internal static bool TRosterSelectedCheck(QRoster roster, Guid id) =>
        roster.QRosterSelection.QRosterSelectedCheck(id);
    internal static bool TRosterCollapseToggle(QRoster roster, Guid batchId) =>
        roster.QRosterCollapse.QRosterCollapseToggle(batchId);
    internal static bool TRosterCollapsedCheck(QRoster roster, Guid batchId) =>
        roster.QRosterCollapse.QRosterCollapsedCheck(batchId);
    internal static IReadOnlyList<QRosterEntry> TRosterEntriesRead(QRoster roster) => roster.QRosterEntries;
    internal static IReadOnlyList<QRosterCard> TRosterCardsRead(QRoster roster) =>
        roster.QRosterEntries
            .Where(entry => entry.QRosterEntryKind == QRosterEntry.QRosterKindHead)
            .Select(entry => entry.QRosterEntryCard)
            .ToArray();
    internal static void TRosterDetailAttach(QRoster roster, Action handler) => roster.QRosterDetailApplied += handler;
    internal static void TRosterDeferAttach(QRoster roster, Action handler) =>
        roster.QRosterDeferral.QRosterDeferralRaised += handler;
    internal static void TRosterWarningAttach(QRoster roster, Action<string, string> handler) =>
        roster.QRosterWarningShown += handler;
    internal static void TRosterRemove(QRoster roster, Guid batchId) => roster.QRosterRemove(batchId);
    internal static IReadOnlyList<QRosterItem> TRosterMenuRead(QRoster roster, Guid id, QStrip? strip) =>
        roster.QRosterMenu.QRosterMenuRead(id, strip);
    internal static void TRosterItemRun(QRosterItem item) => item.QRosterItemAction();
    internal static bool TRosterDoneSet(QRoster roster, bool? isChecked) => roster.QRosterDoneSet(isChecked);
    internal static bool TRosterSharedSet(QRoster roster, bool? isChecked) => roster.QRosterSharedSet(isChecked);
    internal static LWorkItem? TRosterSelectRead(QRoster roster) => roster.QRosterSelectRead();
    internal static IReadOnlyList<LWorkItem> TRosterSelectionRead(QRoster roster) => roster.QRosterSelectionRead();
    internal static void TRosterElapsedTick(QRoster roster, bool visible) => roster.QRosterElapsedTick(visible);
    internal static void TRosterDetailRun(QRoster roster) => roster.QRosterDeferral.QRosterDeferralRun();
    internal static string TRosterOwnerFormat(LWorkItem work, bool owned) => QRosterRow.QRosterOwnerFormat(work, owned);
    internal static string TRosterProgressFormat(LWorkItem work) => QRosterRow.QRosterProgressFormat(work);
    internal static string TRosterPriorityFormat(LWorkPriority priority) => QRosterRow.QRosterPriorityFormat(priority);
    internal static string TRosterStateFormat(LWorkState state) => QRosterRow.QRosterStateFormat(state);
    internal static string TRosterKeyRead(LWorkState state) => QRosterRow.QRosterKeyRead(state);
    internal static string TRosterPhaseFormat(LWorkState state, LWorkPhase phase) =>
        QRosterRow.QRosterPhaseFormat(state, phase);
    internal static string TRosterSpanFormat(TimeSpan span) => QRosterRow.QRosterSpanFormat(span);
    internal static string TRosterShadeResolve(bool selected, bool cardSelected, bool stage) =>
        QRosterRow.QRosterShadeResolve(selected, cardSelected, stage);
    internal static QRosterRow TRosterRowCreate(
        CLineageAnswer answer, LWorkItem work, bool last, bool stage, bool owned, string shade) =>
        QRosterRow.QRosterRowCreate(
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
        return QRosterCard.QRosterTitleFormat(batch.CLineageBatchEarliest, batch.CLineageBatchInitial);
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
        TLineageRowCreate(answer, work).QRosterRowStep;
    internal static string TLineageRatioFormat(CLineageAnswer answer, LWorkItem work) =>
        TLineageRowCreate(answer, work).QRosterRowRatio;
    private static QRosterRow TLineageRowCreate(CLineageAnswer answer, LWorkItem work) =>
        TRosterRowCreate(answer, work, last: true, stage: false, owned: false, QRosterRow.QRosterShadePlain);
    internal static string TLineageTitleFormat(CLineageEntry entry) => QLineage.QLineageTitleFormat(entry);
    internal static HashSet<Guid> TLineageStageRead(LScheduleContract schedule, IReadOnlyList<LWorkItem> items) =>
        TRosterLineageRead(schedule, items).CLineageBatches.Single().CLineageBatchStages.ToHashSet();
    internal static int TLineageInitialRead(LScheduleContract schedule, IReadOnlyList<LWorkItem> items) =>
        TRosterLineageRead(schedule, items).CLineageBatches.Single().CLineageBatchInitial;
    internal static void TWorkOutputSet(LWorkItem work, string path) => work.LWorkOutputSet(path, work.LWorkOutputName);
    internal static QRosterDetailKind TRosterKindRead(QRoster roster) =>
        QRosterDetail.QRosterKindRead(roster);
    internal static QRosterDetail TRosterDetailRead(QRoster roster) => QRosterDetail.QRosterDetailRead(roster);
    internal static QRosterDetail TRosterDetailCreate(LScheduleContract schedule, LWorkItem work, string owner)
    {
        CAtelier atelier = TInterface.TAtelierCreate();
        CLineageStep step = TRosterLineageRead(schedule, [work]).CLineageSteps.Single(
            item => item.CLineageStepId == work.LWorkId);
        return QRosterDetail.QRosterDetailCreate(
            work,
            owner,
            step.CLineageStepSource,
            atelier.CAtelierLineage.CLineageTitleRead(
                step.CLineageStepBatch, step.CLineageStepRelay, step.CLineageStepTab),
            [step.CLineageSourceExtension, step.CLineageOutputExtension]);
    }
    internal static void TStripTitleAttach(QStrip? strip) =>
        LCartographer.LCartographerTitleSource = strip is null ? null : strip.QStripTitleRead;
    internal static IReadOnlyList<QRosterBar> TRosterBarsCreate(long? source, long? output) =>
        QRosterDetail.QRosterBarsCreate(source, output);
    internal static QRosterCompareRow TRosterCompareCreate(string source, string output) =>
        QRosterDetail.QRosterCompareCreate(source, output);
    internal static string TRosterMebiFormat(long? bytes) => QRosterFormatter.QRosterMebiFormat(bytes);
    internal static string TRosterSpentFormat(LWorkItem work) => QRosterFormatter.QRosterSpentFormat(work);
    internal static string TRosterSpeedFormat(LWorkItem work) => QRosterFormatter.QRosterSpeedFormat(work);
    internal static string TRosterElapsedFormat(TimeSpan spent) =>
        QRosterFormatter.QRosterElapsedFormat(spent);
    internal static string TRosterClockFormat(TimeSpan span) => QRosterFormatter.QRosterClockFormat(span);
    internal static string TRosterStampFormat(DateTimeOffset? stamp) => QRosterFormatter.QRosterStampFormat(stamp);
    internal static bool TRosterReencodeCheck(string mode) => QRosterFormatter.QRosterReencodeCheck(mode);
    internal static QSummary TSummaryRead(LScheduleContract schedule, Guid batchId)
    {
        QRoster roster = TRosterCreate(schedule, TStationCreate(schedule));
        try
        {
            roster.QRosterRebuild();
            roster.QRosterSelection.QRosterCardSelect(batchId);
            return QSummary.QSummaryRead(roster);
        }
        finally
        {
            roster.QRosterClose();
        }
    }
    internal static IReadOnlyList<Guid> TRosterIdsRead(LScheduleContract schedule)
    {
        QRoster roster = TRosterCreate(schedule, TStationCreate(schedule));
        try
        {
            roster.QRosterRebuild();
            return roster.QRosterOrderedIds.ToArray();
        }
        finally
        {
            roster.QRosterClose();
        }
    }
    internal static Guid TStationRunnerRead(LStation station) => station.LStationRunner.LRunnerIdentity;
    internal static void TStationClose(LStation station) => station.LStationClose();
    internal static string? TSummaryMeterFormat(IReadOnlyList<LWorkItem> items, long? output) =>
        QSummary.QSummaryMeterFormat(items, output);
    internal static string TSummaryFilesFormat(int count) => QSummary.QSummaryFilesFormat(count);
}
