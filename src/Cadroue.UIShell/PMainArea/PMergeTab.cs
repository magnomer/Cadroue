using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PMergeTab : PTabSurface
{
    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PList pList = new();
    private readonly PGroup pGroup = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PMergeTab(LPreset lExportSpecificState, LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += pPriority => LMerge.LMergeDescribe(
            pPriority, pGroup.PGroupGroupsRead(), lExportSpecificState, pAction.PActionRelayTarget, pAction.PActionSourceTab);
        pAction.PActionAllAdd += () => LMerge.LMergeDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pGroup.PGroupGroupsRead(),
            lExportSpecificState,
            pAction.PActionRelayTarget, pAction.PActionSourceTab);
        pList.PListPathChange += PMergePathShow;
        pGroup.PGroupItemOpen += PMergePathShow;
        pGroup.PGroupSourceFiles = () => pList.PListPathsRead();
        pGroup.PGroupFileRequest = pDropPaths =>
        {
            pList.PListPathsAdd(pDropPaths);
            return PList.PListMediaScan(pDropPaths);
        };
        PTabViewerAttach(pList, pViewer);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);
        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pGroup, pViewer, new PExport(lExportSpecificState) }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
        Content = pTabGrid;
    }

    private void PMergePathShow(string? pSourcePath)
    {
        if (!string.IsNullOrWhiteSpace(pSourcePath))
        {
            pViewer.PViewerSourceOpen(pSourcePath);
        }
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override PGroup? PTabGroup => pGroup;
    public override LPreferenceTabLayoutRecord PTabLayoutRead() => PTabLayoutRead(pTabGrid);
}
