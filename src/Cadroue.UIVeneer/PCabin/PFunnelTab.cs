using Cadroue.Application;
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

    public PFunnelTab(LWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.LWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.LWorkspaceViewer!);
        pList = new PList(lWorkspace.LWorkspaceList!);
        LFunnelTab = lWorkspace.QWorkspaceMode.LFunnelTabCreate();
        pFunnelRules = new PFunnelRules(LFunnelTab.LFunnel);
        var pAction = new PAction(lWorkspace.QWorkspaceMode.LWorkspaceAction!);
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.FunnelAll.Tooltip"));
        pAction.PActionRelayHide();
        pTabGrid = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pFunnelRules, pViewer },
            new PCompass(pFlow, pViewer, lWorkspace.LWorkspaceCompass!),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        Content = pTabGrid;
        LFunnelTab.LFunnelLayoutApply(lPreferenceTabLayout);
    }

    public LFunnelTab LFunnelTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override void PTabStripAttach(LStrip lStrip, LStripTab lStripTab) =>
        LFunnelTab.LFunnel.LFunnelStripAttach(lStrip, lStripTab.LStripTabId);

    public override void PTabTargetsResolve() => LFunnelTab.LFunnel.LFunnelTargetsResolve();

    public override void PTabClose()
    {
        LFunnelTab.LFunnelClose();
        base.PTabClose();
    }

    public override LSceneTabEntry PTabLayoutRead() => LFunnelTab.LFunnelLayoutRead(PTabLayoutCreate());
}
