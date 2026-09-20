using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PMergeTab : PTabSurface
{
    private readonly PFlow pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PList pList = new(new LDocket());
    private readonly PGroup pGroup;
    private readonly PAction pAction = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PMergeTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        LMergeTab = new LMergeTab(lPresetOwner, pList.PListDocketRead(), lPreferenceTabLayout);
        pGroup = new PGroup(LMergeTab.LMergeGroup);
        PTabAction = pAction;
        LAction lAction = pAction.LAction;
        lAction.LActionRun += lPriority =>
            LMergeTab.LMergeRun(lPriority, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionAllAdd += () => LMergeTab.LMergeAllRun(lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionCohortAdd += lCohort =>
            LMergeTab.LMergeCohortRun(lCohort, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionEligibleAttach(LMergeTab.LMergeEligibleRead);
        LMergeTab.LMergePresetMissing += PExport.PExportMissingShow;
        pList.PListPathChange += pViewer.LViewer.LViewerPathHandle;
        pGroup.PGroupItemOpen += pViewer.LViewer.LViewerPathHandle;
        pGroup.PGroupSourceFiles = LMergeTab.LMergePathsRead;
        pGroup.PGroupFileRequest = pDropPaths => _ = pList.PListPathsAdd(pDropPaths);
        pList.PListClearChange += pGroup.PGroupPathsRemove;
        pViewer.PDropPathsChange += pDropPaths => _ = pList.PListPathsAdd(pDropPaths);
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

    public override LSceneTabRecord PTabLayoutRead() => LMergeTab.LMergeLayoutRead(PTabLayoutCreate());
}
