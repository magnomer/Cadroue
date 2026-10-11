using Cadroue.Conduct;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LRoster
{
    private const double LRosterWidthPadding = 16;

    private sealed record LRosterBatch(
        Guid LRosterBatchId,
        IReadOnlyList<LLineageEntry> LRosterBatchLineages,
        IReadOnlyList<LWorkItem> LRosterBatchItems);

    private readonly LScheduleContract lSchedule;
    private readonly LStation lStation;
    private readonly List<Guid> lRosterOrderedIds = [];
    private readonly HashSet<Guid> lRosterPresentIds = [];
    private readonly HashSet<Guid> lRosterCompletedIds = [];
    private readonly List<LRosterBatch> lRosterBatches = [];
    private readonly List<LRosterCard> lRosterCards = [];
    private bool lRosterClosed;
    private readonly IList<LRosterEntry> lRosterEntries;
    private readonly List<int> lRosterEntryCounts = [];

    public LRoster(CUsher lUsher, LScheduleContract lScheduleOwner, LStation lStationOwner)
    {
        LRosterUsher = lUsher;
        lSchedule = lScheduleOwner;
        lStation = lStationOwner;
        QRosterCollapse = new QRosterCollapse(LRosterCardUpdate);
        QRosterDetail = new QRosterDetail(() => lRosterClosed, () => LRosterDetailApplied?.Invoke());
        QRosterSelection = new QRosterSelection(
            lRosterPresentIds.Contains, LRosterBatchCheck, () => lRosterOrderedIds, LRosterSelectRaise);
        lStation.LStationSelectionSource = LRosterSelectionRead;
        lSchedule.LScheduleChanged += LRosterScheduleHandle;
        lSchedule.LScheduleItemChanged += LRosterItemHandle;
        LRosterMenu = new LRosterMenu(lUsher, lSchedule, lStation, LRosterSelectionRead);
        System.Collections.ObjectModel.ObservableCollection<LRosterEntry> lEntries = [];
        lRosterEntries = lEntries;
        LRosterEntries = lEntries;
    }

    public static LRoster LRosterCreate(LStrip lStrip) => new(
        lStrip.LStripUsher,
        LProgram.LScheduleCurrent,
        LStation.LStationCreate(LLocalization.LLocalizationTextRead("Roster.Title.Worklist")));

    public event Action? LRosterDetailApplied;

    public event Action<string, string>? LRosterWarningShown;

    public CUsher LRosterUsher { get; }

    public LRosterMenu LRosterMenu { get; }

    public QRosterCollapse QRosterCollapse { get; }

    public QRosterDetail QRosterDetail { get; }

    public QRosterSelection QRosterSelection { get; }

    public LStation LRosterStation => lStation;

    public IReadOnlyList<LRosterEntry> LRosterEntries { get; }

    public bool LRosterClosed => lRosterClosed;

    public IReadOnlyList<Guid> LRosterOrderedIds => lRosterOrderedIds;

    public bool LRosterShared => LPreference.LPreferenceStateCurrent.LPreferenceWorklistShared;

    public bool LRosterCollapseDone => LPreference.LPreferenceStateCurrent.LPreferenceCollapseDone;

    public void LRosterClose()
    {
        if (lRosterClosed)
        {
            return;
        }

        lRosterClosed = true;
        lSchedule.LScheduleChanged -= LRosterScheduleHandle;
        lSchedule.LScheduleItemChanged -= LRosterItemHandle;
        lStation.LStationClose();
    }

    public LSceneTabEntry LRosterLayoutRead(IReadOnlyList<double> lWeights)
    {
        var lLayout = new LSceneTabEntry();
        lLayout.LScenePanelWidths.AddRange(lWeights);
        return lLayout;
    }

    public static double LRosterWidthResolve(double lColumnTotal) => lColumnTotal + LRosterWidthPadding;

    private void LRosterScheduleHandle(LScheduleContract lScheduleChanged)
    {
        LRosterRebuild();
        LRosterDetailApplied?.Invoke();
    }

    private void LRosterItemHandle(LWorkItem lWorkItem, LScheduleNotice lNotice)
    {
        if (lNotice == LScheduleNotice.LScheduleNoticeStatus)
        {
            LRosterRebuild();
        }
        else
        {
            LRosterCardUpdate(lWorkItem.LWorkBatchId);
        }

        if (!ReferenceEquals(lWorkItem, LRosterSelectRead()))
        {
            return;
        }

        if (lNotice == LScheduleNotice.LScheduleNoticeProgress)
        {
            QRosterDetail.LRosterDeferRaise();
        }
        else
        {
            LRosterDetailApplied?.Invoke();
        }
    }

    public void LRosterElapsedTick(bool lVisible)
    {
        if (lRosterClosed || !lVisible)
        {
            return;
        }

        if (LRosterRunningCheck() || LRosterMeasuringCheck())
        {
            LRosterDetailApplied?.Invoke();
        }
    }

    private bool LRosterRunningCheck() =>
        lSchedule.LScheduleRecords.Any(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning);

    private bool LRosterMeasuringCheck()
    {
        if (QRosterSelection.LRosterCardId != Guid.Empty)
        {
            return LRosterBatchRead().Any(lWorkItem => !lWorkItem.LWorkSourceMeasured);
        }

        return LRosterSelectRead() is { LWorkSourceMeasured: false };
    }

    public void LRosterRebuild()
    {
        LWorkItem[] lItems = lSchedule.LScheduleRecords.Where(LRosterVisibleCheck).ToArray();
        LRosterCompletedSync(lItems);
        IReadOnlyList<LLineageEntry> lLineages =
            LLineage.LLineageRead(LRosterUsher, lItems, lSchedule.LScheduleLineageRead);
        lRosterBatches.Clear();
        foreach (IGrouping<Guid, LLineageEntry> lGroup in lLineages.GroupBy(lLineage => lLineage.LLineageEntryBatch))
        {
            LLineageEntry[] lBatchLineages = lGroup.ToArray();
            LWorkItem[] lBatchItems = lBatchLineages.SelectMany(lLineage => lLineage.LLineageEntryItems).ToArray();
            lRosterBatches.Add(new LRosterBatch(lGroup.Key, lBatchLineages, lBatchItems));
        }

        lRosterOrderedIds.Clear();
        lRosterOrderedIds.AddRange(
            lRosterBatches.SelectMany(lBatch => lBatch.LRosterBatchItems).Select(lWorkItem => lWorkItem.LWorkId));
        lRosterPresentIds.Clear();
        lRosterPresentIds.UnionWith(lRosterOrderedIds);
        QRosterCollapse.LRosterStaleRemove(lRosterBatches.Select(lBatch => lBatch.LRosterBatchId).ToHashSet());
        LRosterCardsUpdate();
    }

    private void LRosterCompletedSync(IReadOnlyList<LWorkItem> lItems)
    {
        IGrouping<Guid, LWorkItem>[] lBatches = lItems.GroupBy(lWorkItem => lWorkItem.LWorkBatchId).ToArray();
        HashSet<Guid> lPresent = lBatches.Select(lBatch => lBatch.Key).ToHashSet();
        lRosterCompletedIds.RemoveWhere(lBatchId => !lPresent.Contains(lBatchId));
        bool lCollapseDone = LRosterCollapseDone;
        foreach (IGrouping<Guid, LWorkItem> lBatch in lBatches)
        {
            bool lCompleted = lBatch.All(lWorkItem => lWorkItem.LWorkStateCurrent is not
                (LWorkState.LWorkStatePending or LWorkState.LWorkStateRunning));
            if (lCompleted)
            {
                lRosterCompletedIds.Add(lBatch.Key);
            }
            else
            {
                lRosterCompletedIds.Remove(lBatch.Key);
            }

            if (lCollapseDone)
            {
                QRosterCollapse.LRosterCollapseSet(lBatch.Key, lCompleted);
            }
        }
    }

    private void LRosterCardsUpdate()
    {
        LRosterCard[] lNext = LRosterCard.LRosterReuseResolve(lRosterCards, lRosterBatches.Select(LRosterCardCreate));
        LRosterCard.LRosterStepsResolve(lRosterCards.ToArray(), lNext).ToList().ForEach(LRosterEntriesApply);
        lRosterCards.Clear();
        lRosterCards.AddRange(lNext);
    }

    private void LRosterCardUpdate(Guid lBatchId)
    {
        int lIndex = lRosterBatches.FindIndex(lBatch => lBatch.LRosterBatchId == lBatchId);
        if (lIndex < 0)
        {
            return;
        }

        lRosterCards[lIndex] = LRosterCardCreate(lRosterBatches[lIndex]);
        LRosterEntriesRemove(lIndex);
        LRosterEntriesInsert(lIndex, lRosterCards[lIndex]);
    }

    private void LRosterEntriesApply(LRosterCardStep lStep)
    {
        if (lStep.LRosterStepCard is { } lCard)
        {
            LRosterEntriesInsert(lStep.LRosterStepIndex, lCard);
            return;
        }

        LRosterEntriesRemove(lStep.LRosterStepIndex);
    }

    private void LRosterEntriesInsert(int lCardIndex, LRosterCard lCard)
    {
        int lOffset = lRosterEntryCounts.Take(lCardIndex).Sum();
        IReadOnlyList<LRosterEntry> lEntries = LRosterEntry.LRosterEntriesCreate(lCard);
        for (int lIndex = 0; lIndex < lEntries.Count; lIndex++)
        {
            lRosterEntries.Insert(lOffset + lIndex, lEntries[lIndex]);
        }

        lRosterEntryCounts.Insert(lCardIndex, lEntries.Count);
    }

    private void LRosterEntriesRemove(int lCardIndex)
    {
        int lOffset = lRosterEntryCounts.Take(lCardIndex).Sum();
        for (int lIndex = 0; lIndex < lRosterEntryCounts[lCardIndex]; lIndex++)
        {
            lRosterEntries.RemoveAt(lOffset);
        }

        lRosterEntryCounts.RemoveAt(lCardIndex);
    }

    private LRosterCard LRosterCardCreate(LRosterBatch lBatch) => LRosterCard.LRosterCardCreate(
        LRosterUsher,
        lBatch.LRosterBatchId,
        lBatch.LRosterBatchLineages,
        lBatch.LRosterBatchItems,
        QRosterSelection.LRosterCardCheck(lBatch.LRosterBatchId),
        lRosterCompletedIds.Contains(lBatch.LRosterBatchId),
        QRosterCollapse.LRosterCollapsedCheck(lBatch.LRosterBatchId),
        LRosterOwnerCheck,
        QRosterSelection.LRosterSelectedCheck);

    private bool LRosterVisibleCheck(LWorkItem lWorkItem)
    {
        if (lWorkItem.LWorkStateCurrent != LWorkState.LWorkStateRunning
            || lWorkItem.LWorkOwnerRunner == Guid.Empty
            || LRosterOwnerCheck(lWorkItem))
        {
            return true;
        }

        return !LSentinel.LSentinelOwnerCheck(
            lWorkItem.LWorkOwnerProcess, lWorkItem.LWorkOwnerStamp, lWorkItem.LWorkOwnerRunner);
    }

    public bool LRosterOwnerCheck(LWorkItem lWorkItem) => lStation.LStationRunner.LRunnerOwnerCheck(lWorkItem);

    public string LRosterOwnerFormat(LWorkItem lWorkItem) =>
        LRosterRow.LRosterOwnerFormat(lWorkItem, LRosterOwnerCheck(lWorkItem));

    public LWorkItem? LRosterSelectRead()
    {
        Guid lCurrentId = QRosterSelection.LRosterCurrentId;
        return lCurrentId == Guid.Empty
            ? null
            : lSchedule.LScheduleRecords.FirstOrDefault(lWorkItem => lWorkItem.LWorkId == lCurrentId);
    }

    public IReadOnlyList<LWorkItem> LRosterSelectionRead() =>
        lSchedule.LScheduleRecords
            .Where(lWorkItem => QRosterSelection.LRosterSelectedCheck(lWorkItem.LWorkId))
            .ToArray();

    public IReadOnlyList<LWorkItem> LRosterBatchRead() =>
        lSchedule.LScheduleRecords
            .Where(lWorkItem => QRosterSelection.LRosterCardCheck(lWorkItem.LWorkBatchId))
            .ToArray();

    private bool LRosterBatchCheck(Guid lBatchId) =>
        lRosterBatches.Exists(lBatch => lBatch.LRosterBatchId == lBatchId);

    private void LRosterSelectRaise()
    {
        LRosterCardsUpdate();
        LRosterDetailApplied?.Invoke();
        lStation.LStationSelectionRaise();
    }

    public void LRosterRemove(Guid lBatchId)
    {
        IReadOnlyList<Guid> lRemovable = lSchedule.LScheduleRemovableRead(
            lSchedule.LScheduleRecords
                .Where(lWorkItem => lWorkItem.LWorkBatchId == lBatchId)
                .Select(lWorkItem => lWorkItem.LWorkId));
        if (lRemovable.Count == 0)
        {
            return;
        }

        LAsk? lAsk = LPreference.LPreferenceStateCurrent.LPreferenceConfirmDestructive
            ? new LAsk(
                LLocalization.LLocalizationFormat("Roster.Card.Confirm", lRemovable.Count),
                LLocalization.LLocalizationTextRead("Terms.Remove"))
            {
                LAskTitle = LLocalization.LLocalizationTextRead("Console.Confirm.Title")
            }
            : null;
        LAskNotice.LAskPublish(lAsk, lAnswer => LRosterRemoveRun(lAnswer, lRemovable));
    }

    private void LRosterRemoveRun(bool lAnswer, IReadOnlyList<Guid> lRemovable)
    {
        if (!lAnswer)
        {
            return;
        }

        if (LConsole.LConsoleRemovalFormat(lSchedule.LScheduleBatchRemove(lRemovable)) is { } lMessage)
        {
            LRosterWarningShown?.Invoke(LLocalization.LLocalizationTextRead("Console.Remove.Title"), lMessage);
        }
    }

    public bool LRosterSharedSet(bool? lChecked)
    {
        bool lShared = lChecked == true;
        if (lShared == LRosterShared)
        {
            return false;
        }

        LPreferenceState lNext = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        lNext.LPreferenceWorklistShared = lShared;
        LPreference.LPreferenceStateSet(lNext);
        lSchedule.LScheduleLoad();
        return true;
    }

    public bool LRosterDoneSet(bool? lChecked)
    {
        bool lCollapseDone = lChecked == true;
        if (lCollapseDone == LRosterCollapseDone)
        {
            return false;
        }

        LPreferenceState lNext = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        lNext.LPreferenceCollapseDone = lCollapseDone;
        LPreference.LPreferenceStateSet(lNext);
        if (lCollapseDone)
        {
            LRosterRebuild();
        }

        return true;
    }
}
