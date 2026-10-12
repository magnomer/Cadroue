using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QFunnelTab
{
    private readonly QList qFunnelList;
    private readonly LDocket qFunnelDocket;

    public QFunnelTab(QViewer lViewer, QList lList)
    {
        qFunnelList = lList;
        qFunnelDocket = lList.QListDocket;
        lList.QListPathChanged += lViewer.QViewerPathHandle;
        lViewer.QViewerSource.QViewerPathsDrop += lDropPaths => _ = lList.QListPathsAdd(lDropPaths);
    }

    public QFunnel QFunnel { get; } = new();

    public void QFunnelClose() => QFunnel.QFunnelStripDetach();

    public IReadOnlyList<string> QFunnelEligibleRead() => qFunnelDocket.LDocketPathsRead();

    public void QFunnelLayoutApply(LSceneTabEntry? lLayout)
    {
        if (lLayout is { } lRecord)
        {
            QFunnel.QFunnelRulesRestore(lRecord.LSceneFunnelRules);
        }
    }

    public LSceneTabEntry QFunnelLayoutRead(LSceneTabEntry lLayout)
    {
        lLayout.LSceneFunnelRules = QFunnel.QFunnelRules.Select(QFunnelLayoutCreate).ToList();
        return lLayout;
    }

    public void QFunnelRun() =>
        QFunnelDispatch(
            qFunnelList.QListPathCurrent is { } lPath && qFunnelDocket.LDocketItemFind(lPath) is { } lItem
                ? [lItem]
                : []);

    public void QFunnelAllRun() => QFunnelDispatch(qFunnelDocket.LDocketItemsRead());

    public void QFunnelItemsRun(IReadOnlyList<string> lPaths) =>
        QFunnelDispatch(
            qFunnelDocket.LDocketItemsRead()
                .Where(lItem => lPaths.Contains(lItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                .ToArray());

    private void QFunnelDispatch(IReadOnlyList<LDocketEntry> lItems)
    {
        if (lItems.Count == 0)
        {
            return;
        }

        var lLive = QFunnel.QFunnelTargetsRead().Select(lTarget => lTarget.QFunnelTargetId).ToHashSet();
        IReadOnlyList<QFunnelRule> lRules = QFunnel.QFunnelRules;
        LMessenger.LMessengerFunnelDescribe(
            QFunnel.QFunnelSelf,
            lRules.Select(QFunnel.QFunnelRecordCreate).ToList(),
            lRules
                .Select(lRule => lLive.Contains(lRule.QFunnelRuleTarget) ? lRule.QFunnelRuleTarget : Guid.Empty)
                .ToList(),
            lItems.Select(lItem => (lItem.LDocketEntryPath, lItem.LDocketEntryBatch)).ToList());
    }

    private LSceneFunnelRule QFunnelLayoutCreate(QFunnelRule lRule)
    {
        LSceneFunnelRule lRecord = QFunnel.QFunnelRecordCreate(lRule);
        lRecord.LSceneFunnelTarget = QFunnel.QFunnelIndexRead(lRule.QFunnelRuleTarget);
        return lRecord;
    }
}
