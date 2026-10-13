using Cadroue.Conduct;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QRoster
{
    private const double QRosterWidthPadding = 16;

    private sealed record QRosterBatch(
        Guid QRosterBatchId,
        CLineageAnswer QRosterBatchAnswer,
        IReadOnlyList<LWorkItem> QRosterBatchItems,
        bool QRosterBatchCompleted);

    private readonly CUsher qRosterUsher;
    private readonly CLineage qRosterLineage;
    private readonly LScheduleContract qSchedule;
    private readonly LStation qStation;
    private readonly List<Guid> qRosterOrderedIds = [];
    private readonly HashSet<Guid> qRosterPresentIds = [];
    private readonly List<QRosterBatch> qRosterBatches = [];
    private bool qRosterClosed;
    private QColumnPlan? qRosterPlan;

    public QRoster(CUsher lUsher, CLineage lLineage, LScheduleContract lScheduleOwner, LStation lStationOwner)
    {
        qRosterUsher = lUsher;
        qRosterLineage = lLineage;
        qSchedule = lScheduleOwner;
        qStation = lStationOwner;
        QRosterCollapse = new QRosterCollapse(QRosterCardUpdate);
        QRosterDeferral = new QRosterDeferral(() => qRosterClosed, () => QRosterDetailApplied?.Invoke());
        QRosterSelection = new QRosterSelection(
            qRosterPresentIds.Contains, QRosterBatchCheck, () => qRosterOrderedIds, QRosterSelectRaise);
        qStation.LStationSelectionSource = QRosterSelectionRead;
        qSchedule.LScheduleChanged += QRosterScheduleHandle;
        qSchedule.LScheduleItemChanged += QRosterItemHandle;
        QRosterRemoval = new QRosterRemoval();
        QRosterRemoval.QRosterRemovalConfirmed += QRosterBatchRemove;
        QRosterMenu = new QRosterMenu(lUsher, qSchedule, qStation, QRosterSelectionRead);
        QRosterFeed = new QRosterFeed();
    }

    public static QRoster QRosterCreate(CUsher lUsher, CLineage lLineage) => new(
        lUsher,
        lLineage,
        QProgram.QScheduleCurrent,
        LStation.LStationCreate(LLocalization.LLocalizationTextRead("Roster.Title.Worklist")));

    public event Action? QRosterDetailApplied;

    public QRosterMenu QRosterMenu { get; }

    public QRosterCollapse QRosterCollapse { get; }

    public QRosterDeferral QRosterDeferral { get; }

    public QRosterSelection QRosterSelection { get; }

    public QRosterRemoval QRosterRemoval { get; }

    public LStation QRosterStation => qStation;

    public QRosterFeed QRosterFeed { get; }

    public bool QRosterClosed => qRosterClosed;

    public IReadOnlyList<Guid> QRosterOrderedIds => qRosterOrderedIds;

    public bool QRosterShared => LPreference.LPreferenceStateCurrent.LPreferenceWorklistShared;

    public bool QRosterCollapseDone => LPreference.LPreferenceStateCurrent.LPreferenceCollapseDone;

    public void QRosterClose()
    {
        if (qRosterClosed)
        {
            return;
        }

        qRosterClosed = true;
        qSchedule.LScheduleChanged -= QRosterScheduleHandle;
        qSchedule.LScheduleItemChanged -= QRosterItemHandle;
        qStation.LStationClose();
    }

    public string? QRosterPathOpen(string lPath) =>
        qRosterUsher.CUsherPathOpen(lPath, failure: "Could not open '{0}': {1}");

    public QColumnPlan QRosterPlanCreate(IReadOnlyList<double> lMinimumWidths, IReadOnlyList<double>? lStoredWidths)
    {
        qRosterPlan = new QColumnPlan(lMinimumWidths, lStoredWidths, null, -1);
        return qRosterPlan;
    }

    public LSceneTabEntry QRosterLayoutRead()
    {
        var lLayout = new LSceneTabEntry();
        lLayout.LScenePanelWidths.AddRange(qRosterPlan?.QColumnWeightsRead() ?? []);
        return lLayout;
    }

    public static double QRosterWidthResolve(double lColumnTotal) => lColumnTotal + QRosterWidthPadding;

    private void QRosterScheduleHandle(LScheduleContract lScheduleChanged)
    {
        QRosterRebuild();
        QRosterDetailApplied?.Invoke();
    }

    private void QRosterItemHandle(LWorkItem lWorkItem, LScheduleNotice lNotice)
    {
        if (lNotice == LScheduleNotice.LScheduleNoticeStatus)
        {
            QRosterRebuild();
        }
        else
        {
            QRosterCardUpdate(lWorkItem.LWorkBatchId);
        }

        if (!ReferenceEquals(lWorkItem, QRosterSelectRead()))
        {
            return;
        }

        if (lNotice == LScheduleNotice.LScheduleNoticeProgress)
        {
            QRosterDeferral.QRosterDeferralRaise();
        }
        else
        {
            QRosterDetailApplied?.Invoke();
        }
    }

    public void QRosterElapsedTick(bool lVisible)
    {
        if (qRosterClosed || !lVisible)
        {
            return;
        }

        if (QRosterRunningCheck() || QRosterMeasuringCheck())
        {
            QRosterDetailApplied?.Invoke();
        }
    }

    private bool QRosterRunningCheck() =>
        qSchedule.LScheduleRecords.Any(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning);

    private bool QRosterMeasuringCheck()
    {
        if (QRosterSelection.QRosterCardId != Guid.Empty)
        {
            return QRosterBatchRead().Any(lWorkItem => !lWorkItem.LWorkSourceMeasured);
        }

        return QRosterSelectRead() is { LWorkSourceMeasured: false };
    }

    public void QRosterRebuild()
    {
        LWorkItem[] lItems = qSchedule.LScheduleRecords.Where(QRosterVisibleCheck).ToArray();
        CLineageAnswer lAnswer =
            qRosterLineage.CLineageRead(lItems.Select(lWorkItem => lWorkItem.LWorkId).ToArray());
        QRosterCompletedSync(lAnswer.CLineageBatches);
        qRosterBatches.Clear();
        qRosterBatches.AddRange(lAnswer.CLineageBatches.Select(
            lBatch => new QRosterBatch(lBatch.CLineageBatchId, lAnswer, lItems, lBatch.CLineageBatchCompleted)));
        qRosterOrderedIds.Clear();
        qRosterOrderedIds.AddRange(lAnswer.CLineageBatches.SelectMany(lBatch => lBatch.CLineageBatchIds));
        qRosterPresentIds.Clear();
        qRosterPresentIds.UnionWith(qRosterOrderedIds);
        QRosterCollapse.QRosterStaleRemove(qRosterBatches.Select(lBatch => lBatch.QRosterBatchId).ToHashSet());
        QRosterCardsUpdate();
    }

    private void QRosterCompletedSync(IReadOnlyList<CLineageBatch> lBatches)
    {
        bool lCollapseDone = QRosterCollapseDone;
        foreach (CLineageBatch lBatch in lBatches)
        {
            QRosterCollapse.QRosterCompletedApply(lBatch.CLineageBatchId, lBatch.CLineageBatchCompleted, lCollapseDone);
        }
    }

    private void QRosterCardsUpdate() => QRosterFeed.QRosterFeedApply(qRosterBatches.Select(QRosterCardCreate));

    private void QRosterCardUpdate(Guid lBatchId)
    {
        int lIndex = qRosterBatches.FindIndex(lBatch => lBatch.QRosterBatchId == lBatchId);
        if (lIndex < 0)
        {
            return;
        }

        QRosterFeed.QRosterCardSet(lIndex, QRosterCardCreate(qRosterBatches[lIndex]));
    }

    private QRosterCard QRosterCardCreate(QRosterBatch lBatch) => QRosterCard.QRosterCardCreate(
        qRosterLineage,
        lBatch.QRosterBatchId,
        lBatch.QRosterBatchAnswer,
        lBatch.QRosterBatchItems,
        QRosterSelection.QRosterCardCheck(lBatch.QRosterBatchId),
        lBatch.QRosterBatchCompleted,
        QRosterCollapse.QRosterCollapsedCheck(lBatch.QRosterBatchId),
        QRosterOwnerCheck,
        QRosterSelection.QRosterSelectedCheck);

    private bool QRosterVisibleCheck(LWorkItem lWorkItem)
    {
        if (lWorkItem.LWorkStateCurrent != LWorkState.LWorkStateRunning
            || lWorkItem.LWorkOwnerRunner == Guid.Empty
            || QRosterOwnerCheck(lWorkItem))
        {
            return true;
        }

        return !LSentinel.LSentinelOwnerCheck(
            lWorkItem.LWorkOwnerProcess, lWorkItem.LWorkOwnerStamp, lWorkItem.LWorkOwnerRunner);
    }

    public bool QRosterOwnerCheck(LWorkItem lWorkItem) => qStation.LStationRunner.LRunnerOwnerCheck(lWorkItem);

    public string QRosterOwnerFormat(LWorkItem lWorkItem) =>
        QRosterRow.QRosterOwnerFormat(lWorkItem, QRosterOwnerCheck(lWorkItem));

    public LWorkItem? QRosterSelectRead()
    {
        Guid lCurrentId = QRosterSelection.QRosterCurrentId;
        return lCurrentId == Guid.Empty
            ? null
            : qSchedule.LScheduleRecords.FirstOrDefault(lWorkItem => lWorkItem.LWorkId == lCurrentId);
    }

    public IReadOnlyList<LWorkItem> QRosterSelectionRead() =>
        qSchedule.LScheduleRecords
            .Where(lWorkItem => QRosterSelection.QRosterSelectedCheck(lWorkItem.LWorkId))
            .ToArray();

    public IReadOnlyList<LWorkItem> QRosterBatchRead() =>
        qSchedule.LScheduleRecords
            .Where(lWorkItem => QRosterSelection.QRosterCardCheck(lWorkItem.LWorkBatchId))
            .ToArray();

    internal CLineageStep QRosterStepRead(Guid lWorkId) =>
        qRosterBatches
            .SelectMany(lBatch => lBatch.QRosterBatchAnswer.CLineageSteps)
            .First(lStep => lStep.CLineageStepId == lWorkId);

    internal string QRosterTitleRead(CLineageStep lStep) =>
        qRosterLineage.CLineageTitleRead(lStep.CLineageStepBatch, lStep.CLineageStepRelay, lStep.CLineageStepTab);

    internal CLineageBatch QRosterSummaryRead() => qRosterLineage.CLineageBatchRead(QRosterSelection.QRosterCardId);

    public void QRosterRemove(Guid lBatchId) => QRosterRemoval.QRosterRemoveConfirm(
        qSchedule.LScheduleRemovableRead(qSchedule.LScheduleRecords
            .Where(lWorkItem => lWorkItem.LWorkBatchId == lBatchId)
            .Select(lWorkItem => lWorkItem.LWorkId)));

    private void QRosterBatchRemove(IReadOnlyList<Guid> lRemovable) =>
        QRosterRemoval.QRosterWarningRaise(QConsole.QConsoleRemovalFormat(qSchedule.LScheduleBatchRemove(lRemovable)));

    private bool QRosterBatchCheck(Guid lBatchId) =>
        qRosterBatches.Exists(lBatch => lBatch.QRosterBatchId == lBatchId);

    private void QRosterSelectRaise()
    {
        QRosterCardsUpdate();
        QRosterDetailApplied?.Invoke();
        qStation.LStationSelectionRaise();
    }

    public bool QRosterSharedSet(bool? lChecked)
    {
        bool lShared = lChecked == true;
        if (lShared == QRosterShared)
        {
            return false;
        }

        LPreferenceState lNext = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        lNext.LPreferenceWorklistShared = lShared;
        LPreference.LPreferenceStateSet(lNext);
        qSchedule.LScheduleLoad();
        return true;
    }

    public bool QRosterDoneSet(bool? lChecked)
    {
        bool lCollapseDone = lChecked == true;
        if (lCollapseDone == QRosterCollapseDone)
        {
            return false;
        }

        LPreferenceState lNext = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        lNext.LPreferenceCollapseDone = lCollapseDone;
        LPreference.LPreferenceStateSet(lNext);
        if (lCollapseDone)
        {
            QRosterRebuild();
        }

        return true;
    }
}
