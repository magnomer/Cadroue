using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PFunnelTab : PTabSurface
{
    private readonly PFlow pFlow;
    private readonly PViewer pViewer;
    private readonly PList pList;
    private readonly PFunnelRules pFunnelRules;

    public PFunnelTab(QWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.QWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.QWorkspaceViewer!);
        pList = new PList(lWorkspace.QWorkspaceList!);
        QFunnelTab = lWorkspace.QWorkspaceMode.QFunnelTabCreate();
        pFunnelRules = new PFunnelRules(QFunnelTab.QFunnel);
        var pAction = new PAction(lWorkspace.QWorkspaceMode.QWorkspaceAction!);
        pAction.PActionAllSet(true, QLocalizationCatalog.QLocalizationTextRead("Action.FunnelAll.Tooltip"));
        pAction.PActionRelayHide();
        Content = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pFunnelRules, pViewer },
            new PCompass(pFlow, pViewer, lWorkspace.QWorkspaceCompass!),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        QFunnelTab.QFunnelLayoutApply(lPreferenceTabLayout);
    }

    public QFunnelTab QFunnelTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override void PTabStripAttach(QStrip lStrip, QStripTab lStripTab) =>
        QFunnelTab.QFunnel.QFunnelRelay.QFunnelStripAttach(lStrip, lStripTab.QStripTabId);

    public override void PTabTargetsResolve() => QFunnelTab.QFunnel.QFunnelRelay.QFunnelTargetsResolve();

    public override void PTabClose()
    {
        QFunnelTab.QFunnelClose();
        base.PTabClose();
    }

    public override LSceneTabEntry PTabLayoutRead() => QFunnelTab.QFunnelLayoutRead(PTabLayoutCreate());
}
