using Cadroue.Conduct;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QStripRelay
{
    private readonly QStrip qStrip;
    private readonly CUsher qStripUsher;

    public QStripRelay(QStrip lOwner, CUsher lUsher)
    {
        qStrip = lOwner;
        qStripUsher = lUsher;
    }

    public void QStripRelayAttach()
    {
        LCartographer.LCartographerTabsSource = QStripCartographerRead;
        LCartographer.LCartographerTitleSource = QStripTitleRead;
        LMessenger.LMessengerTitleSource = QStripTitleRead;
        LMessenger.LMessengerDrainSource = QStripPathsRemove;
        LSeal.LSealNodesSource = QStripSealRead;
        LSeal.LSealFireSeam = QStripNodeRun;
        QAction.QActionStripAttach(qStrip);
        QListRelay.QListRelayAttach(qStrip, qStripUsher);
    }

    public string QStripTitleRead(Guid lId) =>
        qStrip.QStripTabFind(lId)?.QStripTabTitle ?? LCartographer.LCartographerStageRead(lId);

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
