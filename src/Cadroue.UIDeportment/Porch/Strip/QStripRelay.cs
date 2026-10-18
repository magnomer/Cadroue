using Cadroue.Conduct;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QStripRelay
{
    private readonly QStrip qStrip;
    private readonly CRelay qStripRelayGate;

    public QStripRelay(QStrip lOwner, CRelay lRelay)
    {
        qStrip = lOwner;
        qStripRelayGate = lRelay;
    }

    public void QStripRelayAttach()
    {
        LCartographerPlan.LCartographerTabsSource = QStripCartographerRead;
        LCartographerTitle.LCartographerTitleSource = QStripTitleRead;
        LMessenger.LMessengerTitleSource = QStripTitleRead;
        LMessenger.LMessengerDrainSource = QStripPathsRemove;
        LSeal.LSealNodesSource = QStripSealRead;
        LSeal.LSealFireSeam = QStripNodeRun;
        QActionRelay.QActionStripAttach(qStrip);
        QStripListAttach();
    }

    // The relay gate reads this strip's open tabs at every call, each by id with its list, and names a tab by the
    // title the engine reads too; an arrival goes to the target tab's action.
    public void QStripListAttach() =>
        qStripRelayGate.CRelayAttach(
            () => qStrip.QStripTabs
                .Select(lTab => (lTab.QStripTabId, lTab.QStripTabWorkspace.QWorkspaceList?.QListGate))
                .ToArray(),
            QStripTitleRead,
            QActionRelay.QActionAccept);

    public string QStripTitleRead(Guid lId) =>
        qStrip.QStripTabFind(lId)?.QStripTabTitle ?? LCartographerTitle.LCartographerStageRead(lId);

    public IReadOnlyList<QStripTab> QStripRelayRead(Guid lSourceTab) =>
        qStrip.QStripTabs
            .Where(lTab => lTab.QStripTabId != lSourceTab && lTab.QStripTabDocket is not null)
            .ToArray();

    public IReadOnlyList<LCartographerTab> QStripCartographerRead() =>
        qStrip.QStripTabs
            .Select(lTab => (lTab, lLayout: lTab.QStripLayoutRead()))
            .Where(lPair => lPair.lTab.QStripTabPreset is not null && lPair.lLayout is not null)
            .Select(lPair => new LCartographerTab(
                lPair.lTab.QStripTabId,
                lPair.lTab.QStripTabKey,
                lPair.lTab.QStripTabTitle,
                lPair.lTab.QStripTabPreset!.LPresetRecordCreate(),
                lPair.lLayout!.LSceneTabClone(),
                lPair.lTab.QStripTabFunnel))
            .ToArray();

    public IReadOnlyList<LSealNode> QStripSealRead() =>
        qStrip.QStripTabs
            .Where(lTab => lTab.QStripTabDocket is not null)
            .Select(lTab => new LSealNode(
                lTab.QStripTabId,
                lTab.QStripTabMerge,
                lTab.QStripRelayCheck(),
                lTab.QStripTabDocket!.LDocketItemsRead()
                    .Where(lItem => lItem.LDocketEntryDelivered && lItem.LDocketEntryBatch != Guid.Empty)
                    .Select(lItem => lItem.LDocketEntryBatch)
                    .Distinct()
                    .ToArray()))
            .ToArray();

    public void QStripPathsRemove(Guid lId, IReadOnlyList<string> lPaths) =>
        qStrip.QStripTabFind(lId)?.QStripTabDocket?.LDocketPathsRemove(lPaths);

    public bool QStripNodeRun(Guid lId, Guid lCohort) => qStrip.QStripTabFind(lId)?.QStripCohortRun(lCohort) ?? false;
}
