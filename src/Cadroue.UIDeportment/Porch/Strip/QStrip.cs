using System.Collections.ObjectModel;
using Cadroue.Conduct;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QStrip
{
    private readonly ObservableCollection<QStripTab> qStripTabs = [];
    private readonly CAtelier qStripAtelier;
    private QStripTab? qStripSelected;
    private int qStripSuspendDepth;

    public QStrip(CAtelier lAtelier)
        : this(lAtelier, QStripTitle.QStripTitleResolve, QStripNumberResolve)
    {
    }

    // A tab reads each gate it needs from the atelier, so a new gate leaves the strip as it is.
    public QStrip(CAtelier lAtelier, Func<string, string> lTitleSource, Func<string, int, string> lNumberSource)
    {
        qStripAtelier = lAtelier;
        QStripTitle = new QStripTitle(this, lTitleSource, lNumberSource);
        QStripSeparator = new QStripSeparator(this, qStripTabs);
        QStripClosure = new QStripClosure(
            this,
            qStripTabs,
            QLocalizationCatalog.QLocalizationTextRead,
            LTraceLog.LTraceInfoRecord,
            LCartographerRoute.LCartographerTabRemove);
        QStripRail = new QRail(this);
        QStripDeck = new QDeck(this, QLocalizationCatalog.QLocalizationTextRead);
        QStripRelay = new QStripRelay(this, lAtelier.CAtelierRelay);
    }

    public event Action? QStripChanged;
    public event Action<QStripTab?>? QStripSelectChanged;
    public event Action<QStripTab>? QStripTabChanged;
    public event Action<QStripTab>? QStripTabAdded;

    public QStripTitle QStripTitle { get; }

    public QStripSeparator QStripSeparator { get; }

    public QStripClosure QStripClosure { get; }

    public QRail QStripRail { get; }

    public QDeck QStripDeck { get; }

    public QStripRelay QStripRelay { get; }

    public IReadOnlyList<QStripTab> QStripTabs => qStripTabs;

    public QStripTab? QStripSelected => qStripSelected;

    public bool QStripSuspended => qStripSuspendDepth > 0;

    public QStripTab QStripTabCreate(string lKey) => new(QStripCatalog.QStripKeyResolve(lKey), qStripAtelier);

    public QStripTab? QStripTabFind(Guid lId) => qStripTabs.FirstOrDefault(lTab => lTab.QStripTabId == lId);

    public void QStripUpdateSuspend() => qStripSuspendDepth++;

    public void QStripUpdateResume() => QStripDepthSet(QStripDepthResolve());

    private int QStripDepthResolve() => Math.Max(0, qStripSuspendDepth - 1);

    private void QStripDepthSet(int lDepth) => qStripSuspendDepth = lDepth;

    public void QStripAdd(QStripTab lTab)
    {
        lTab.QStripTabWorkspace.QWorkspaceMode.QWorkspaceAction?.QActionRelayAttach(this, lTab);
        qStripTabs.Add(lTab);
        QStripChanged?.Invoke();
        QStripTitle.QStripTitleReset(lTab);
        QStripTabAdded?.Invoke(lTab);
        LTraceLog.LTraceInfoRecord(
            $"Tab opened '{lTab.QStripTabTitle}' ({lTab.QStripTabKey}): {qStripTabs.Count} tab(s) open");
        if (QStripSuspended)
        {
            return;
        }

        QStripTitle.QStripTitleUpdate();
        if (qStripSelected is null)
        {
            QStripSelect(lTab);
        }
        else
        {
            QStripSeparator.QStripSeparatorUpdate();
        }
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
            QStripChosenSet(lItem, ReferenceEquals(lItem, lTab));
        }

        QStripSelectedSet(lTab);
        QStripSeparator.QStripSeparatorUpdate();
    }

    private void QStripChosenSet(QStripTab lTab, bool lSelected)
    {
        if (lTab.QStripTabSelected == lSelected)
        {
            return;
        }

        lTab.QStripTabSelected = lSelected;
        QStripTabRaise(lTab);
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
        QStripTitle.QStripTitleUpdate();
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
            lCleared |= lTab.QStripTabWorkspace.QWorkspaceMedia.QWorkspaceMediaClear(lActiveBatches);
        }

        LTraceLog.LTraceInfoRecord($"Tabs cleared across {qStripTabs.Count} tab(s)");
        return lCleared;
    }

    public void QStripTabRaise(QStripTab lTab)
    {
        lTab.QStripTabUpdate();
        QStripTabChanged?.Invoke(lTab);
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

    private static string QStripNumberResolve(string lName, int lOrdinal) =>
        QLocalizationCatalog.QLocalizationFormat("Tab.Numbered", lName, lOrdinal);
}
