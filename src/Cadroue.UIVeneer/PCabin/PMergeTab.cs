using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PMergeTab : PTabSurface
{
    private readonly PFlow pFlow;
    private readonly PViewer pViewer;
    private readonly PList pList;
    private readonly PGroup pGroup;
    private readonly PAction pAction = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PMergeTab(LStrip lStrip, LPresetSelection lPresetOwner, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lStrip);
        pViewer = new PViewer(lStrip);
        pList = new PList(lStrip, new LDocket());
        LMergeTab = new LMergeTab(lStrip, lPresetOwner, pList.LList.LListDocket, lPreferenceTabLayout);
        pGroup = new PGroup(LMergeTab.LMergeGroup);
        PTabAction = pAction;
        LAction lAction = pAction.LAction;
        lAction.LActionExecuted += lPriority =>
            LMergeTab.LMergeRun(lPriority, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionAllAdded += () => LMergeTab.LMergeAllRun(lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionCohortAdded += lCohort =>
            LMergeTab.LMergeCohortRun(lCohort, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionEligibleAttach(LMergeTab.LMergeEligibleRead);
        LMergeTab.LMergePresetMissing += PExport.PExportMissingShow;
        pList.LList.LListPathChanged += pViewer.LViewer.LViewerPathHandle;
        pGroup.LGroup.LGroupDrag.LGroupItemOpened += pViewer.LViewer.LViewerPathHandle;
        pGroup.LGroup.LGroupDrag.LGroupPathsRequest += pDropPaths => _ = pList.LList.LListPathsAdd(pDropPaths);
        pViewer.LViewer.LViewerSource.LViewerPathsDrop += pDropPaths => _ = pList.LList.LListPathsAdd(pDropPaths);
        var pExport = new PExport(lPresetOwner);
        PTabLockAttach(pList, pExport);
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pGroup, pViewer, pExport },
            new PCompass(pFlow, pViewer),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        Content = pTabGrid;
    }

    public LMergeTab LMergeTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override PGroup? PTabGroup => pGroup;

    public override void PTabClose()
    {
        LMergeTab.LMergeClose();
        base.PTabClose();
    }

    public override LSceneTabEntry PTabLayoutRead() => LMergeTab.LMergeLayoutRead(PTabLayoutCreate());
}
