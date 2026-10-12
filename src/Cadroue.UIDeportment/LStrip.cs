using System.Collections.ObjectModel;
using Cadroue.Conduct;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LStrip
{
    private const string LStripDefaultKey = "Split";
    private static readonly string[] LStripKeys =
        ["Split", "Edit", "Fix", "Audio", "Convert", "Merge", "Funnel", "Worklist"];
    private readonly ObservableCollection<LStripTab> lStripTabs = [];
    private readonly Func<string, string> lStripTitleSource;
    private readonly Func<string, int, string> lStripNumberSource;
    private readonly CUsher lStripUsher;
    private readonly CLineage lStripLineage;
    private LStripTab? lStripSelected;
    private int lStripSuspendDepth;

    public LStrip(CUsher lUsher, CLineage lLineage)
        : this(lUsher, lLineage, LStripTitleResolve, LStripNumberResolve)
    {
    }

    public LStrip(
        CUsher lUsher, CLineage lLineage, Func<string, string> lTitleSource, Func<string, int, string> lNumberSource)
    {
        lStripUsher = lUsher;
        lStripLineage = lLineage;
        lStripTitleSource = lTitleSource;
        lStripNumberSource = lNumberSource;
        QStripSeparator = new QStripSeparator(this, lStripTabs);
        QStripClosure = new QStripClosure(
            this,
            lStripTabs,
            LLocalization.LLocalizationTextRead,
            LTraceLog.LTraceInfoRecord,
            LCartographer.LCartographerTabRemove);
        LStripRail = new LRail(this);
        LStripDeck = new LDeck(this);
    }

    public event Action? LStripChanged;
    public event Action<LStripTab?>? LStripSelectChanged;
    public event Action<LStripTab>? LStripTabChanged;
    public event Action<LStripTab>? LStripTabAdded;
    public event Action? LStripTitleChanged;

    public QStripSeparator QStripSeparator { get; }

    public QStripClosure QStripClosure { get; }

    public LRail LStripRail { get; }

    public LDeck LStripDeck { get; }

    public IReadOnlyList<LStripTab> LStripTabs => lStripTabs;

    public LStripTab? LStripSelected => lStripSelected;

    public bool LStripSuspended => lStripSuspendDepth > 0;

    public static IReadOnlyList<string> LStripKeysRead() => LStripKeys;

    public static string LStripKeyResolve(string lKey) =>
        LStripKeys.Contains(lKey, StringComparer.Ordinal) ? lKey : LStripDefaultKey;

    public LStripTab LStripTabCreate(string lKey) => new(LStripKeyResolve(lKey), lStripUsher, lStripLineage);

    public LStripTab? LStripTabFind(Guid lId) => lStripTabs.FirstOrDefault(lTab => lTab.LStripTabId == lId);

    public LStripTab? LStripTabFind(LWorkItem lWorkItem)
    {
        Guid lSourceTab = lWorkItem.LWorkRelaySource;
        if (lSourceTab == Guid.Empty)
        {
            return null;
        }

        if (LCartographerPlanStore.LCartographerPlanRead(lWorkItem.LWorkBatchId, out LCartographerPlanDocument lPlan)
            && lPlan.LCartographerStages.FirstOrDefault(lStage => lStage.LCartographerStageId == lSourceTab)
                is { } lSourceStage)
        {
            lSourceTab = lSourceStage.LCartographerOriginalTab;
        }

        return LStripTabFind(lSourceTab);
    }

    public void LStripRelayAttach()
    {
        LCartographer.LCartographerTabsSource = LStripCartographerRead;
        LCartographer.LCartographerTitleSource = LStripTitleRead;
        LMessenger.LMessengerTitleSource = LStripTitleRead;
        LMessenger.LMessengerDrainSource = LStripPathsRemove;
        LSeal.LSealNodesSource = LStripSealRead;
        LSeal.LSealFireSeam = LStripNodeRun;
        LAction.LActionStripAttach(this);
        LListRelay.LListRelayAttach(this, lStripUsher);
    }

    public string LStripTitleRead(Guid lId) =>
        LStripTabFind(lId)?.LStripTabTitle ?? LCartographer.LCartographerStageRead(lId);

    public IReadOnlyList<LStripTab> LStripRelayRead(Guid lSourceTab) =>
        lStripTabs
            .Where(lTab => lTab.LStripTabId != lSourceTab && lTab.LStripTabDocket is not null)
            .ToArray();

    public IReadOnlyList<LCartographerTab> LStripCartographerRead() =>
        lStripTabs
            .Select(lTab => (lTab, lLayout: lTab.LStripLayoutRead()))
            .Where(lPair => lPair.lTab.LStripTabPreset is not null && lPair.lLayout is not null)
            .Select(lPair => new LCartographerTab(
                lPair.lTab.LStripTabId,
                lPair.lTab.LStripTabKey,
                lPair.lTab.LStripTabTitle,
                lPair.lTab.LStripTabPreset!.LPresetRecordCreate(),
                lPair.lLayout!.LSceneTabClone(),
                lPair.lTab.LStripTabFunnel))
            .ToArray();

    public IReadOnlyList<LSealNode> LStripSealRead() =>
        lStripTabs
            .Where(lTab => lTab.LStripTabDocket is not null)
            .Select(lTab => new LSealNode(
                lTab.LStripTabId,
                lTab.LStripTabMerge,
                lTab.LStripRelayCheck(),
                lTab.LStripTabDocket!.LDocketItemsRead()
                    .Where(lItem => lItem.LDocketEntryDelivered && lItem.LDocketEntryBatch != Guid.Empty)
                    .Select(lItem => lItem.LDocketEntryBatch)
                    .Distinct()
                    .ToArray()))
            .ToArray();

    public void LStripPathsRemove(Guid lId, IReadOnlyList<string> lPaths) =>
        LStripTabFind(lId)?.LStripTabDocket?.LDocketPathsRemove(lPaths);

    public bool LStripNodeRun(Guid lId, Guid lCohort) => LStripTabFind(lId)?.LStripCohortRun(lCohort) ?? false;

    public void LStripUpdateSuspend() => lStripSuspendDepth++;

    public void LStripUpdateResume() => lStripSuspendDepth = Math.Max(0, lStripSuspendDepth - 1);

    public void LStripAdd(LStripTab lTab)
    {
        lTab.LStripTabWorkspace.QWorkspaceMode.LWorkspaceAction?.LActionRelayAttach(this, lTab);
        lStripTabs.Add(lTab);
        LStripChanged?.Invoke();
        LStripTitleSet(lTab, lStripTitleSource(lTab.LStripTabKey));
        LStripTabAdded?.Invoke(lTab);
        LTraceLog.LTraceInfoRecord(
            $"Tab opened '{lTab.LStripTabTitle}' ({lTab.LStripTabKey}): {lStripTabs.Count} tab(s) open");
        if (LStripSuspended)
        {
            return;
        }

        LStripTitleUpdate();
        if (lStripSelected is null)
        {
            LStripSelect(lTab);
        }
        else
        {
            QStripSeparator.LStripSeparatorUpdate();
        }
    }

    public void LStripTitleUpdate()
    {
        if (LStripSuspended)
        {
            return;
        }

        IReadOnlyList<LTabsetSlot> lSlots = lStripTabs
            .Select(lTab => new LTabsetSlot(lTab.LStripTabId, lTab.LStripTabKey, lTab.LStripTabCustom))
            .ToList();
        foreach (LTabsetTitlePlan lPlan in LTabset.LTabsetTitleResolve(lSlots))
        {
            if (LStripTabFind(lPlan.LTabsetId) is not { } lTab)
            {
                continue;
            }

            string lTitle = lPlan.LTabsetCustom
                ? lTab.LStripTabCustom
                : lPlan.LTabsetNumbered
                    ? lStripNumberSource(lStripTitleSource(lTab.LStripTabKey), lPlan.LTabsetOrdinal)
                    : lStripTitleSource(lTab.LStripTabKey);
            LStripTitleSet(lTab, lTitle);
        }

        LStripTitleChanged?.Invoke();
    }

    public bool LStripNameSet(LStripTab lTab, string? lName)
    {
        bool lHadCustom = lTab.LStripTabCustom.Length > 0;
        string lTrimmed = (lName ?? string.Empty).Trim();
        if (lTrimmed.Length == 0
            || string.Equals(lTrimmed, lStripTitleSource(lTab.LStripTabKey), StringComparison.Ordinal))
        {
            lTab.LStripTabCustom = string.Empty;
            LStripTitleUpdate();
            if (lHadCustom)
            {
                LTraceLog.LTraceInfoRecord($"Tab name reset to the standard name for {lTab.LStripTabKey}");
            }

            return false;
        }

        var lTaken = lStripTabs
            .Where(lOther => !ReferenceEquals(lOther, lTab))
            .Select(lOther => lOther.LStripTabTitle)
            .ToList();
        lTab.LStripTabCustom = LTabset.LTabsetNameResolve(lTaken, lTrimmed, lStripNumberSource);
        LStripTitleUpdate();
        LTraceLog.LTraceInfoRecord($"Tab renamed to '{lTab.LStripTabTitle}' ({lTab.LStripTabKey})");
        return true;
    }

    public void LStripEditSet(LStripTab lTab, bool lEditing)
    {
        if (lTab.LStripTabEditing == lEditing)
        {
            return;
        }

        lTab.LStripTabEditing = lEditing;
        LStripTabRaise(lTab);
    }

    public void LStripPendingSet(LStripTab lTab, bool lPending)
    {
        if (lTab.LStripTabPending == lPending)
        {
            return;
        }

        lTab.LStripTabPending = lPending;
        LStripTabRaise(lTab);
    }

    public void LStripSelect(LStripTab? lTab)
    {
        foreach (LStripTab lItem in lStripTabs)
        {
            bool lSelected = ReferenceEquals(lItem, lTab);
            if (lItem.LStripTabSelected != lSelected)
            {
                lItem.LStripTabSelected = lSelected;
                LStripTabRaise(lItem);
            }
        }

        LStripSelectedSet(lTab);
        QStripSeparator.LStripSeparatorUpdate();
    }

    public bool LStripMove(LStripTab lTab, int lTargetIndex)
    {
        int lSourceIndex = lStripTabs.IndexOf(lTab);
        if (lSourceIndex < 0)
        {
            return false;
        }

        int lClamped = Math.Clamp(lTargetIndex, 0, lStripTabs.Count - 1);
        if (lSourceIndex == lClamped)
        {
            return false;
        }

        lStripTabs.Move(lSourceIndex, lClamped);
        LStripChanged?.Invoke();
        QStripSeparator.LStripSeparatorUpdate();
        return true;
    }

    public void LStripRemove(LStripTab lTab)
    {
        int lIndex = lStripTabs.IndexOf(lTab);
        if (lIndex < 0)
        {
            return;
        }

        bool lWasSelected = ReferenceEquals(lStripSelected, lTab);
        QStripSeparator.LStripHoverReset(lTab);

        lStripTabs.RemoveAt(lIndex);
        LStripChanged?.Invoke();
        LStripTitleUpdate();
        if (!lWasSelected)
        {
            QStripSeparator.LStripSeparatorUpdate();
            return;
        }

        if (lStripTabs.Count == 0)
        {
            LStripSelectedSet(null);
            return;
        }

        LStripSelect(lStripTabs[LTabset.LTabsetNextResolve(lStripTabs.Count, lIndex)]);
    }

    public void LStripClear()
    {
        QStripSeparator.LStripHoverReset(null);
        lStripTabs.Clear();
        LStripChanged?.Invoke();
        LStripSelectedSet(null);
    }

    public bool LStripBusyCheck(LStripTab? lTab = null) =>
        lTab is null ? lStripTabs.Any(lItem => lItem.LStripTabBusy) : lTab.LStripTabBusy;

    public bool LStripContentClear()
    {
        bool lCleared = false;
        IReadOnlySet<Guid> lActiveBatches = LBastion.LBastionCohortsRead();
        foreach (LStripTab lTab in lStripTabs)
        {
            lCleared |= lTab.LStripTabWorkspace.LWorkspaceMediaClear(lActiveBatches);
        }

        LTraceLog.LTraceInfoRecord($"Tabs cleared across {lStripTabs.Count} tab(s)");
        return lCleared;
    }

    public static string LStripTitleResolve(string lKey) => LLocalization.LLocalizationTextRead("Tab." + lKey);

    private static string LStripNumberResolve(string lName, int lOrdinal) =>
        LLocalization.LLocalizationFormat("Tab.Numbered", lName, lOrdinal);

    public void LStripTabRaise(LStripTab lTab)
    {
        lTab.LStripTabUpdate();
        LStripTabChanged?.Invoke(lTab);
    }

    private void LStripTitleSet(LStripTab lTab, string lTitle)
    {
        if (lTab.LStripTabTitle == lTitle)
        {
            return;
        }

        lTab.LStripTabTitle = lTitle;
        LStripTabRaise(lTab);
    }

    private void LStripSelectedSet(LStripTab? lTab)
    {
        if (ReferenceEquals(lStripSelected, lTab))
        {
            return;
        }

        lStripSelected = lTab;
        LStripSelectChanged?.Invoke(lTab);
    }
}
