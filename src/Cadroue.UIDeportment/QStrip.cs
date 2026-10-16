using System.Collections.ObjectModel;
using Cadroue.Conduct;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QStrip
{
    private const string QStripDefaultKey = "Split";
    private static readonly string[] QStripKeys =
        ["Split", "Edit", "Fix", "Audio", "Convert", "Merge", "Funnel", "Worklist"];
    private readonly ObservableCollection<QStripTab> qStripTabs = [];
    private readonly Func<string, string> qStripTitleSource;
    private readonly Func<string, int, string> qStripNumberSource;
    private readonly CUsher qStripUsher;
    private readonly CInfo qStripInfo;
    private readonly CLineage qStripLineage;
    private readonly Func<CWorkspace> qStripWorkspaceSource;
    private readonly Func<CInspector> qStripInspectorSource;
    private QStripTab? qStripSelected;
    private int qStripSuspendDepth;

    public QStrip(
        CUsher lUsher,
        CInfo lInfo,
        CLineage lLineage,
        Func<CWorkspace> lWorkspaceSource,
        Func<CInspector> lInspectorSource)
        : this(lUsher, lInfo, lLineage, lWorkspaceSource, lInspectorSource, QStripTitleResolve, QStripNumberResolve)
    {
    }

    public QStrip(
        CUsher lUsher,
        CInfo lInfo,
        CLineage lLineage,
        Func<CWorkspace> lWorkspaceSource,
        Func<CInspector> lInspectorSource,
        Func<string, string> lTitleSource,
        Func<string, int, string> lNumberSource)
    {
        qStripUsher = lUsher;
        qStripInfo = lInfo;
        qStripLineage = lLineage;
        qStripWorkspaceSource = lWorkspaceSource;
        qStripInspectorSource = lInspectorSource;
        qStripTitleSource = lTitleSource;
        qStripNumberSource = lNumberSource;
        QStripSeparator = new QStripSeparator(this, qStripTabs);
        QStripClosure = new QStripClosure(
            this,
            qStripTabs,
            QLocalizationCatalog.QLocalizationTextRead,
            LTraceLog.LTraceInfoRecord,
            LCartographer.LCartographerTabRemove);
        QStripRail = new QRail(this);
        QStripDeck = new QDeck(this, QLocalizationCatalog.QLocalizationTextRead);
        QStripRelay = new QStripRelay(this, lUsher);
    }

    public event Action? QStripChanged;
    public event Action<QStripTab?>? QStripSelectChanged;
    public event Action<QStripTab>? QStripTabChanged;
    public event Action<QStripTab>? QStripTabAdded;
    public event Action? QStripTitleChanged;

    public QStripSeparator QStripSeparator { get; }

    public QStripClosure QStripClosure { get; }

    public QRail QStripRail { get; }

    public QDeck QStripDeck { get; }

    public QStripRelay QStripRelay { get; }

    public IReadOnlyList<QStripTab> QStripTabs => qStripTabs;

    public QStripTab? QStripSelected => qStripSelected;

    public bool QStripSuspended => qStripSuspendDepth > 0;

    public static IReadOnlyList<string> QStripKeysRead() => QStripKeys;

    public static string QStripKeyResolve(string lKey) =>
        QStripKeys.Contains(lKey, StringComparer.Ordinal) ? lKey : QStripDefaultKey;

    public QStripTab QStripTabCreate(string lKey) =>
        new(
            QStripKeyResolve(lKey),
            qStripUsher,
            qStripInfo,
            qStripLineage,
            qStripWorkspaceSource(),
            qStripInspectorSource());

    public QStripTab? QStripTabFind(Guid lId) => qStripTabs.FirstOrDefault(lTab => lTab.QStripTabId == lId);

    public QStripTab? QStripTabFind(LWorkItem lWorkItem)
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

        return QStripTabFind(lSourceTab);
    }

    public void QStripUpdateSuspend() => qStripSuspendDepth++;

    public void QStripUpdateResume() => qStripSuspendDepth = Math.Max(0, qStripSuspendDepth - 1);

    public void QStripAdd(QStripTab lTab)
    {
        lTab.QStripTabWorkspace.QWorkspaceMode.QWorkspaceAction?.QActionRelayAttach(this, lTab);
        qStripTabs.Add(lTab);
        QStripChanged?.Invoke();
        QStripTitleSet(lTab, qStripTitleSource(lTab.QStripTabKey));
        QStripTabAdded?.Invoke(lTab);
        LTraceLog.LTraceInfoRecord(
            $"Tab opened '{lTab.QStripTabTitle}' ({lTab.QStripTabKey}): {qStripTabs.Count} tab(s) open");
        if (QStripSuspended)
        {
            return;
        }

        QStripTitleUpdate();
        if (qStripSelected is null)
        {
            QStripSelect(lTab);
        }
        else
        {
            QStripSeparator.QStripSeparatorUpdate();
        }
    }

    public void QStripTitleUpdate()
    {
        if (QStripSuspended)
        {
            return;
        }

        IReadOnlyList<LTabsetSlot> lSlots = qStripTabs
            .Select(lTab => new LTabsetSlot(lTab.QStripTabId, lTab.QStripTabKey, lTab.QStripTabCustom))
            .ToList();
        foreach (LTabsetTitlePlan lPlan in LTabset.LTabsetTitleResolve(lSlots))
        {
            if (QStripTabFind(lPlan.LTabsetId) is not { } lTab)
            {
                continue;
            }

            string lTitle = lPlan.LTabsetCustom
                ? lTab.QStripTabCustom
                : lPlan.LTabsetNumbered
                    ? qStripNumberSource(qStripTitleSource(lTab.QStripTabKey), lPlan.LTabsetOrdinal)
                    : qStripTitleSource(lTab.QStripTabKey);
            QStripTitleSet(lTab, lTitle);
        }

        QStripTitleChanged?.Invoke();
    }

    public bool QStripNameSet(QStripTab lTab, string? lName)
    {
        bool lHadCustom = lTab.QStripTabCustom.Length > 0;
        string lTrimmed = (lName ?? string.Empty).Trim();
        if (lTrimmed.Length == 0
            || string.Equals(lTrimmed, qStripTitleSource(lTab.QStripTabKey), StringComparison.Ordinal))
        {
            lTab.QStripTabCustom = string.Empty;
            QStripTitleUpdate();
            if (lHadCustom)
            {
                LTraceLog.LTraceInfoRecord($"Tab name reset to the standard name for {lTab.QStripTabKey}");
            }

            return false;
        }

        var lTaken = qStripTabs
            .Where(lOther => !ReferenceEquals(lOther, lTab))
            .Select(lOther => lOther.QStripTabTitle)
            .ToList();
        lTab.QStripTabCustom = LTabset.LTabsetNameResolve(lTaken, lTrimmed, qStripNumberSource);
        QStripTitleUpdate();
        LTraceLog.LTraceInfoRecord($"Tab renamed to '{lTab.QStripTabTitle}' ({lTab.QStripTabKey})");
        return true;
    }

    public void QStripEditSet(QStripTab lTab, bool lEditing)
    {
        if (lTab.QStripTabEditing == lEditing)
        {
            return;
        }

        lTab.QStripTabEditing = lEditing;
        QStripTabRaise(lTab);
    }

    public void QStripPendingSet(QStripTab lTab, bool lPending)
    {
        if (lTab.QStripTabPending == lPending)
        {
            return;
        }

        lTab.QStripTabPending = lPending;
        QStripTabRaise(lTab);
    }

    public void QStripSelect(QStripTab? lTab)
    {
        foreach (QStripTab lItem in qStripTabs)
        {
            bool lSelected = ReferenceEquals(lItem, lTab);
            if (lItem.QStripTabSelected != lSelected)
            {
                lItem.QStripTabSelected = lSelected;
                QStripTabRaise(lItem);
            }
        }

        QStripSelectedSet(lTab);
        QStripSeparator.QStripSeparatorUpdate();
    }

    public bool QStripMove(QStripTab lTab, int lTargetIndex)
    {
        int lSourceIndex = qStripTabs.IndexOf(lTab);
        if (lSourceIndex < 0)
        {
            return false;
        }

        int lClamped = Math.Clamp(lTargetIndex, 0, qStripTabs.Count - 1);
        if (lSourceIndex == lClamped)
        {
            return false;
        }

        qStripTabs.Move(lSourceIndex, lClamped);
        QStripChanged?.Invoke();
        QStripSeparator.QStripSeparatorUpdate();
        return true;
    }

    public void QStripRemove(QStripTab lTab)
    {
        int lIndex = qStripTabs.IndexOf(lTab);
        if (lIndex < 0)
        {
            return;
        }

        bool lWasSelected = ReferenceEquals(qStripSelected, lTab);
        QStripSeparator.QStripHoverReset(lTab);

        qStripTabs.RemoveAt(lIndex);
        QStripChanged?.Invoke();
        QStripTitleUpdate();
        if (!lWasSelected)
        {
            QStripSeparator.QStripSeparatorUpdate();
            return;
        }

        if (qStripTabs.Count == 0)
        {
            QStripSelectedSet(null);
            return;
        }

        QStripSelect(qStripTabs[LTabset.LTabsetNextResolve(qStripTabs.Count, lIndex)]);
    }

    public void QStripClear()
    {
        QStripSeparator.QStripHoverReset(null);
        qStripTabs.Clear();
        QStripChanged?.Invoke();
        QStripSelectedSet(null);
    }

    public bool QStripBusyCheck(QStripTab? lTab = null) =>
        lTab is null ? qStripTabs.Any(lItem => lItem.QStripTabBusy) : lTab.QStripTabBusy;

    public bool QStripContentClear()
    {
        bool lCleared = false;
        IReadOnlySet<Guid> lActiveBatches = LBastion.LBastionCohortsRead();
        foreach (QStripTab lTab in qStripTabs)
        {
            lCleared |= lTab.QStripTabWorkspace.QWorkspaceMediaClear(lActiveBatches);
        }

        LTraceLog.LTraceInfoRecord($"Tabs cleared across {qStripTabs.Count} tab(s)");
        return lCleared;
    }

    public static string QStripTitleResolve(string lKey) => QLocalizationCatalog.QLocalizationTextRead("Tab." + lKey);

    private static string QStripNumberResolve(string lName, int lOrdinal) =>
        QLocalizationCatalog.QLocalizationFormat("Tab.Numbered", lName, lOrdinal);

    public void QStripTabRaise(QStripTab lTab)
    {
        lTab.QStripTabUpdate();
        QStripTabChanged?.Invoke(lTab);
    }

    private void QStripTitleSet(QStripTab lTab, string lTitle)
    {
        if (lTab.QStripTabTitle == lTitle)
        {
            return;
        }

        lTab.QStripTabTitle = lTitle;
        QStripTabRaise(lTab);
    }

    private void QStripSelectedSet(QStripTab? lTab)
    {
        if (ReferenceEquals(qStripSelected, lTab))
        {
            return;
        }

        qStripSelected = lTab;
        QStripSelectChanged?.Invoke(lTab);
    }
}
