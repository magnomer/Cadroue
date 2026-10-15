using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PFunnelTab : PTabSurface
{
    private readonly PFlow pFlow;
    private readonly PViewer pViewer;
    private readonly PList pList;
    private readonly PFunnelRules pFunnelRules;
    private readonly System.Windows.Controls.Grid pTabGrid;

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
        pTabGrid = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pFunnelRules, pViewer },
            new PCompass(pFlow, pViewer, lWorkspace.QWorkspaceCompass!),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        Content = pTabGrid;
        QFunnelTab.QFunnelLayoutApply(lPreferenceTabLayout);
    }

    public QFunnelTab QFunnelTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override void PTabStripAttach(QStrip lStrip, QStripTab lStripTab) =>
        QFunnelTab.QFunnel.QFunnelStripAttach(lStrip, lStripTab.QStripTabId);

    public override void PTabTargetsResolve() => QFunnelTab.QFunnel.QFunnelTargetsResolve();

    public override void PTabClose()
    {
        QFunnelTab.QFunnelClose();
        base.PTabClose();
    }

    public override LSceneTabEntry PTabLayoutRead() => QFunnelTab.QFunnelLayoutRead(PTabLayoutCreate());
}
