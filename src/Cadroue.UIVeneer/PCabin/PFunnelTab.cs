using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PFunnelTab : PTabSurface
{
    private readonly PFlow pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PList pList = new(new LDocket());
    private readonly PFunnelRules pFunnelRules;
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PFunnelTab(LSceneTabRecord? lPreferenceTabLayout = null)
    {
        LFunnelTab = new LFunnelTab(pList.LList, pList.PListDocketRead());
        pFunnelRules = new PFunnelRules(LFunnelTab.LFunnel);
        var pAction = new PAction();
        PTabAction = pAction;
        LAction lAction = pAction.LAction;
        lAction.LActionRun += _ => LFunnelTab.LFunnelRun();
        lAction.LActionAllAdd += LFunnelTab.LFunnelAllRun;
        lAction.LActionItemsAdd += LFunnelTab.LFunnelItemsRun;
        pAction.PActionListAttach(pList);
        lAction.LActionEligibleAttach(LFunnelTab.LFunnelEligibleRead);
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.FunnelAll.Tooltip"));
        pAction.PActionRelayHide();

        pList.PListPathChange += pViewer.LViewer.LViewerPathHandle;
        pViewer.LViewer.LViewerSource.LViewerPathsDrop += pDropPaths => _ = pList.PListPathsAdd(pDropPaths);
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pFunnelRules, pViewer },
            new PCompass(pFlow, pViewer),
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

    public override LSceneTabRecord PTabLayoutRead() => LFunnelTab.LFunnelLayoutRead(PTabLayoutCreate());
}
