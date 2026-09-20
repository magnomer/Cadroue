using System.Collections.ObjectModel;
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
    private LStripTab? lStripSelected;
    private LStripTab? lStripHovered;
    private int lStripSuspendDepth;
    private bool lStripVertical;

    public LStrip()
        : this(LStripTitleResolve, LStripNumberResolve)
    {
    }

    public LStrip(Func<string, string> lTitleSource, Func<string, int, string> lNumberSource)
    {
        lStripTitleSource = lTitleSource;
        lStripNumberSource = lNumberSource;
    }

    public event Action? LStripChange;
    public event Action<LStripTab?>? LStripSelectChange;
    public event Action<LStripTab>? LStripTabChange;
    public event Action<LStripTab>? LStripTabAdd;
    public event Action<LStripTab>? LStripTabClose;
    public event Action? LStripTitleChange;

    public IReadOnlyList<LStripTab> LStripTabs => lStripTabs;

    public LStripTab? LStripSelected => lStripSelected;

    public bool LStripSuspended => lStripSuspendDepth > 0;

    public bool LStripVertical => lStripVertical;

    public static IReadOnlyList<string> LStripKeysRead() => LStripKeys;

    public static string LStripKeyResolve(string lKey) =>
        LStripKeys.Contains(lKey, StringComparer.Ordinal) ? lKey : LStripDefaultKey;

    public static LStripTab LStripTabCreate(string lKey) => new(LStripKeyResolve(lKey));

    public LStripTab? LStripTabFind(Guid lId) => lStripTabs.FirstOrDefault(lTab => lTab.LStripTabId == lId);

    public LStripTab? LStripTabFind(LWorkItem lWorkItem)
    {
        Guid lSourceTab = lWorkItem.LWorkRelaySource;
        if (lSourceTab == Guid.Empty)
        {
            return null;
        }

        if (LCartographerPlanStore.LCartographerPlanRead(lWorkItem.LWorkBatchId, out LCartographerPlanRecord lPlan)
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
        LListRelay.LListRelayAttach(this);
    }

    public string LStripTitleRead(Guid lId) =>
        LStripTabFind(lId)?.LStripTabTitle ?? LCartographer.LCartographerStageRead(lId);

    public static Guid LStripTargetRead(Guid lId) => LCartographer.LCartographerTargetRead(lId);

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

    public void LStripVerticalSet(bool lVertical)
    {
        if (lStripVertical == lVertical)
        {
            return;
        }

        lStripVertical = lVertical;
        LStripHoverClear(null);
    }

    public void LStripAdd(LStripTab lTab)
    {
        lStripTabs.Add(lTab);
        LStripChange?.Invoke();
        LStripTitleSet(lTab, lStripTitleSource(lTab.LStripTabKey));
        LStripTabAdd?.Invoke(lTab);
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
            LStripSeparatorUpdate();
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

        LStripTitleChange?.Invoke();
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

    public void LStripHoverSet(LStripTab lTab)
    {
        if (ReferenceEquals(lStripHovered, lTab))
        {
            return;
        }

        lStripHovered = lTab;
        LStripSeparatorUpdate();
    }

    public void LStripHoverClear(LStripTab? lTab)
    {
        if (lStripHovered is null || (lTab is not null && !ReferenceEquals(lStripHovered, lTab)))
        {
            return;
        }

        lStripHovered = null;
        LStripSeparatorUpdate();
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
        LStripSeparatorUpdate();
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
        LStripChange?.Invoke();
        LStripSeparatorUpdate();
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
        if (ReferenceEquals(lStripHovered, lTab))
        {
            lStripHovered = null;
        }

        lStripTabs.RemoveAt(lIndex);
        LStripChange?.Invoke();
        LStripTitleUpdate();
        if (!lWasSelected)
        {
            LStripSeparatorUpdate();
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
        lStripHovered = null;
        lStripTabs.Clear();
        LStripChange?.Invoke();
        LStripSelectedSet(null);
    }

    public bool LStripBusyCheck(LStripTab? lTab = null) =>
        lTab is null ? lStripTabs.Any(lItem => lItem.LStripTabBusy) : lTab.LStripTabBusy;

    public LAsk? LStripCloseResolve(LStripTab? lTab = null) =>
        LStripBusyCheck(lTab)
            ? new LAsk(
                LLocalization.LLocalizationTextRead(
                    lTab is null ? "Tab.Close.BusyAllMessage" : "Tab.Close.BusyMessage"),
                LLocalization.LLocalizationTextRead("Terms.Stop"))
            {
                LAskTitle = LLocalization.LLocalizationTextRead("Tab.Close.BusyTitle")
            }
            : null;

    public bool LStripCloseConfirm(LStripTab? lTab = null)
    {
        bool lConfirmed = false;
        LAskNotice.LAskPublish(LStripCloseResolve(lTab), lAnswer => lConfirmed = lAnswer);
        if (!lConfirmed)
        {
            LTraceLog.LTraceInfoRecord("Close declined: a worklist is still working");
        }

        return lConfirmed;
    }

    public void LStripClose(LStripTab lTab)
    {
        if (!lStripTabs.Contains(lTab))
        {
            return;
        }

        string lClosedTitle = lTab.LStripTabTitle;
        LStripTabClose?.Invoke(lTab);
        LCartographer.LCartographerTabRemove(lTab.LStripTabId);
        LStripRemove(lTab);
        LTraceLog.LTraceInfoRecord($"Tab closed '{lClosedTitle}': {lStripTabs.Count} tab(s) open");
    }

    public void LStripAllClose()
    {
        foreach (LStripTab lTab in lStripTabs.ToArray())
        {
            LStripTabClose?.Invoke(lTab);
            LCartographer.LCartographerTabRemove(lTab.LStripTabId);
        }

        LStripClear();
        LTraceLog.LTraceInfoRecord("All tabs closed");
    }

    public bool LStripContentClear()
    {
        bool lCleared = false;
        IReadOnlySet<Guid> lActiveBatches = LBastion.LBastionCohortsRead();
        foreach (LStripTab lTab in lStripTabs)
        {
            lCleared |= lTab.LStripTabWorkspace?.LWorkspaceMediaClear(lActiveBatches) == true;
        }

        LTraceLog.LTraceInfoRecord($"Tabs cleared across {lStripTabs.Count} tab(s)");
        return lCleared;
    }

    public static string LStripTitleResolve(string lKey) => LLocalization.LLocalizationTextRead("Tab." + lKey);

    private static string LStripNumberResolve(string lName, int lOrdinal) =>
        LLocalization.LLocalizationFormat("Tab.Numbered", lName, lOrdinal);

    private void LStripTabRaise(LStripTab lTab)
    {
        lTab.LStripTabUpdate();
        LStripTabChange?.Invoke(lTab);
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
        LStripSelectChange?.Invoke(lTab);
    }

    private void LStripSeparatorUpdate()
    {
        int lSelectedIndex = lStripSelected is null ? -1 : lStripTabs.IndexOf(lStripSelected);
        int lHoveredIndex = lStripHovered is null ? -1 : lStripTabs.IndexOf(lStripHovered);
        for (int lIndex = 0; lIndex < lStripTabs.Count; lIndex++)
        {
            bool lSeparator = lIndex < lStripTabs.Count - 1
                && lIndex != lSelectedIndex
                && lIndex != lSelectedIndex - 1
                && lIndex != lHoveredIndex
                && lIndex != lHoveredIndex - 1;
            if (lStripTabs[lIndex].LStripTabSeparator == lSeparator)
            {
                continue;
            }

            lStripTabs[lIndex].LStripTabSeparator = lSeparator;
            LStripTabRaise(lStripTabs[lIndex]);
        }
    }
}
