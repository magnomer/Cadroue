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
    private readonly HashSet<Guid> lRosterSelectedIds = [];
    private readonly HashSet<Guid> lRosterCollapsedIds = [];
    private readonly HashSet<Guid> lRosterCompletedIds = [];
    private readonly List<LRosterBatch> lRosterBatches = [];
    private readonly List<LRosterCard> lRosterCards = [];
    private bool lRosterClosed;
    private bool lRosterDetailPending;
    private Guid lRosterCurrentId;
    private Guid lRosterCardId;

    public LRoster(LScheduleContract lScheduleOwner, LStation lStationOwner)
    {
        lSchedule = lScheduleOwner;
        lStation = lStationOwner;
        lStation.LStationSelectionSource = LRosterSelectionRead;
        lSchedule.LScheduleChange += LRosterScheduleHandle;
        lSchedule.LScheduleItemChange += LRosterItemHandle;
        LRosterMenu = new LRosterMenu(lSchedule, lStation, LRosterSelectionRead);
    }

    public static LRoster LRosterCreate() => new(
        LProgram.LScheduleCurrent,
        LStation.LStationCreate(LLocalization.LLocalizationTextRead("Roster.Title.Worklist")));

    public event Action? LRosterCardsApply;

    public event Action<int, LRosterCard>? LRosterCardApply;

    public event Action? LRosterDetailApply;

    public event Action? LRosterDetailDefer;

    public event Action<string, string>? LRosterWarningShow;

    public LRosterMenu LRosterMenu { get; }

    public LStation LRosterStation => lStation;

    public IReadOnlyList<LRosterCard> LRosterCards => lRosterCards;

    public bool LRosterClosed => lRosterClosed;

    public bool LRosterDetailPending => lRosterDetailPending;

    public Guid LRosterCurrentId => lRosterCurrentId;

    public Guid LRosterCardId => lRosterCardId;

    public IReadOnlyList<Guid> LRosterOrderedIds => lRosterOrderedIds;

    public IReadOnlyCollection<Guid> LRosterSelectedIds => lRosterSelectedIds;

    public bool LRosterShared => LPreference.LPreferenceStateCurrent.LPreferenceWorklistShared;

    public bool LRosterCollapseDone => LPreference.LPreferenceStateCurrent.LPreferenceCollapseDone;

    public void LRosterClose()
    {
        if (lRosterClosed)
        {
            return;
        }

        lRosterClosed = true;
        lSchedule.LScheduleChange -= LRosterScheduleHandle;
        lSchedule.LScheduleItemChange -= LRosterItemHandle;
        lStation.LStationClose();
    }

    public LSceneTabRecord LRosterLayoutRead(IReadOnlyList<double> lWeights)
    {
        var lLayout = new LSceneTabRecord();
        lLayout.LScenePanelWidths.AddRange(lWeights);
        return lLayout;
    }

    public static double LRosterWidthResolve(double lColumnTotal) => lColumnTotal + LRosterWidthPadding;

    private void LRosterScheduleHandle(LScheduleContract lScheduleChanged)
    {
        LRosterRebuild();
        LRosterDetailApply?.Invoke();
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
            LRosterDeferRaise();
        }
        else
        {
            LRosterDetailApply?.Invoke();
        }
    }

    private void LRosterDeferRaise()
    {
        if (lRosterDetailPending)
        {
            return;
        }

        lRosterDetailPending = true;
        LRosterDetailDefer?.Invoke();
    }

    public void LRosterDetailRun()
    {
        lRosterDetailPending = false;
        if (!lRosterClosed)
        {
            LRosterDetailApply?.Invoke();
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
            LRosterDetailApply?.Invoke();
        }
    }

    private bool LRosterRunningCheck() =>
        lSchedule.LScheduleRecords.Any(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning);

    private bool LRosterMeasuringCheck()
    {
        if (lRosterCardId != Guid.Empty)
        {
            return LRosterBatchRead().Any(lWorkItem => !lWorkItem.LWorkSourceMeasured);
        }

        return LRosterSelectRead() is { LWorkSourceMeasured: false };
    }

    public void LRosterRebuild()
    {
        LWorkItem[] lItems = lSchedule.LScheduleRecords.Where(LRosterVisibleCheck).ToArray();
        LRosterCompletedSync(lItems);
        IReadOnlyList<LLineageEntry> lLineages = LLineage.LLineageRead(lItems, lSchedule.LScheduleLineageRead);
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
        LRosterStaleRemove(lRosterBatches.Select(lBatch => lBatch.LRosterBatchId).ToHashSet());
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
                LRosterCollapseSet(lBatch.Key, lCompleted);
            }
        }
    }

    private void LRosterCardsUpdate()
    {
        lRosterCards.Clear();
        lRosterCards.AddRange(lRosterBatches.Select(LRosterCardCreate));
        LRosterCardsApply?.Invoke();
    }

    private void LRosterCardUpdate(Guid lBatchId)
    {
        int lIndex = lRosterBatches.FindIndex(lBatch => lBatch.LRosterBatchId == lBatchId);
        if (lIndex < 0)
        {
            return;
        }

        lRosterCards[lIndex] = LRosterCardCreate(lRosterBatches[lIndex]);
        LRosterCardApply?.Invoke(lIndex, lRosterCards[lIndex]);
    }

    private LRosterCard LRosterCardCreate(LRosterBatch lBatch) => LRosterCard.LRosterCardCreate(
        lBatch.LRosterBatchId,
        lBatch.LRosterBatchLineages,
        lBatch.LRosterBatchItems,
        LRosterCardCheck(lBatch.LRosterBatchId),
        lRosterCompletedIds.Contains(lBatch.LRosterBatchId),
        LRosterCollapsedCheck(lBatch.LRosterBatchId),
        LRosterOwnerCheck,
        LRosterSelectedCheck);

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

    public LWorkItem? LRosterSelectRead() =>
        lRosterCurrentId == Guid.Empty
            ? null
            : lSchedule.LScheduleRecords.FirstOrDefault(lWorkItem => lWorkItem.LWorkId == lRosterCurrentId);

    public IReadOnlyList<LWorkItem> LRosterSelectionRead() =>
        lSchedule.LScheduleRecords.Where(lWorkItem => lRosterSelectedIds.Contains(lWorkItem.LWorkId)).ToArray();

    public IReadOnlyList<LWorkItem> LRosterBatchRead() =>
        lSchedule.LScheduleRecords.Where(lWorkItem => LRosterCardCheck(lWorkItem.LWorkBatchId)).ToArray();

    public bool LRosterSelectedCheck(Guid lId) => lRosterSelectedIds.Contains(lId);

    public bool LRosterCollapsedCheck(Guid lBatchId) => lRosterCollapsedIds.Contains(lBatchId);

    public bool LRosterCardCheck(Guid lBatchId) => lBatchId == lRosterCardId;

    public void LRosterCollapseSet(Guid lBatchId, bool lCollapsed)
    {
        if (lCollapsed)
        {
            lRosterCollapsedIds.Add(lBatchId);
        }
        else
        {
            lRosterCollapsedIds.Remove(lBatchId);
        }
    }

    public bool LRosterCollapseToggle(Guid lBatchId)
    {
        bool lCollapsed = !lRosterCollapsedIds.Contains(lBatchId);
        LRosterCollapseSet(lBatchId, lCollapsed);
        LRosterCardUpdate(lBatchId);
        return lCollapsed;
    }

    private void LRosterStaleRemove(IReadOnlyCollection<Guid> lBatchIds)
    {
        var lPresent = lRosterOrderedIds.ToHashSet();
        lRosterSelectedIds.RemoveWhere(lId => !lPresent.Contains(lId));
        if (!lPresent.Contains(lRosterCurrentId))
        {
            lRosterCurrentId = Guid.Empty;
        }

        if (lRosterCardId != Guid.Empty && !lBatchIds.Contains(lRosterCardId))
        {
            lRosterCardId = Guid.Empty;
        }

        lRosterCollapsedIds.RemoveWhere(lBatchId => !lBatchIds.Contains(lBatchId));
    }

    public void LRosterStepSelect(Guid lId, bool lRange, bool lToggle)
    {
        lRosterCardId = Guid.Empty;
        if (lRange && lRosterCurrentId != Guid.Empty)
        {
            int lAnchor = lRosterOrderedIds.IndexOf(lRosterCurrentId);
            int lTarget = lRosterOrderedIds.IndexOf(lId);
            if (lAnchor >= 0 && lTarget >= 0)
            {
                lRosterSelectedIds.Clear();
                for (int lIndex = Math.Min(lAnchor, lTarget); lIndex <= Math.Max(lAnchor, lTarget); lIndex++)
                {
                    lRosterSelectedIds.Add(lRosterOrderedIds[lIndex]);
                }
            }
        }
        else if (lToggle)
        {
            if (!lRosterSelectedIds.Add(lId))
            {
                lRosterSelectedIds.Remove(lId);
            }

            lRosterCurrentId = lId;
        }
        else
        {
            lRosterSelectedIds.Clear();
            lRosterSelectedIds.Add(lId);
            lRosterCurrentId = lId;
        }

        LRosterSelectRaise();
    }

    public void LRosterCardSelect(Guid lBatchId)
    {
        lRosterCardId = lBatchId;
        lRosterSelectedIds.Clear();
        lRosterCurrentId = Guid.Empty;
        LRosterSelectRaise();
    }

    private void LRosterSelectRaise()
    {
        LRosterCardsUpdate();
        LRosterDetailApply?.Invoke();
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
            LRosterWarningShow?.Invoke(LLocalization.LLocalizationTextRead("Console.Remove.Title"), lMessage);
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
