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
    private readonly PAction pAction = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PMergeTab(LPreset lExportSpecificState, LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        PTabAction = pAction;
        pAction.PActionRun += pPriority => LMerge.LMergeDescribe(
            pPriority, pGroup.PGroupGroupsRead(), lExportSpecificState,
            pAction.PActionRelayTarget, pAction.PActionSourceTab, PMergeRelaysRead());
        pAction.PActionAllAdd += () => LMerge.LMergeDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pGroup.PGroupGroupsRead(),
            lExportSpecificState,
            pAction.PActionRelayTarget, pAction.PActionSourceTab, PMergeRelaysRead());
        pList.PListPathChange += PMergePathShow;
        pGroup.PGroupItemOpen += PMergePathShow;
        pGroup.PGroupSourceFiles = () => pList.PListPathsRead();
        pGroup.PGroupFileRequest = pDropPaths =>
        {
            pList.PListPathsAdd(pDropPaths);
            return PList.PListMediaScan(pDropPaths);
        };
        pList.PListItemsAdd += PMergeItemsHandle;
        LCourier.LCourierBatchFinish += PMergeRelayFinishHandle;
        PTabViewerAttach(pList, pViewer);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);
        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pGroup, pViewer, new PExport(lExportSpecificState) }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
        Content = pTabGrid;
        pGroup.PGroupModeSeed(
            lPreferenceTabLayout?.LPreferenceGroupAuto ?? false,
            lPreferenceTabLayout?.LPreferenceGroupStrict ?? true);
    }

    private IReadOnlyDictionary<string, Guid> PMergeRelaysRead()
    {
        var pMergeRelays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (PListItem pItem in pList.PListItemsRead())
        {
            pMergeRelays[pItem.PListItemPath] = pItem.PListItemRelay;
        }

        return pMergeRelays;
    }

    private void PMergeItemsHandle(IReadOnlyList<PListItem> pAddedItems)
    {
        if (pGroup.PGroupAutoCheck() && pAddedItems.Any(pItem => pItem.PListItemRelay == Guid.Empty))
        {
            pGroup.PGroupAutoRegroup();
        }
    }

    private void PMergeRelayFinishHandle(Guid pRelayId, Guid pTargetTabId)
    {
        if (pGroup.PGroupAutoCheck() && pTargetTabId == pAction.PActionSourceTab)
        {
            pGroup.PGroupAutoRegroup();
        }
    }

    public override void PTabClose() => LCourier.LCourierBatchFinish -= PMergeRelayFinishHandle;

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

    public override LPreferenceTabLayoutRecord PTabLayoutRead()
    {
        LPreferenceTabLayoutRecord lPreferenceTabLayout = PTabLayoutRead(pTabGrid);
        lPreferenceTabLayout.LPreferenceGroupAuto = pGroup.PGroupAutoCheck();
        lPreferenceTabLayout.LPreferenceGroupStrict = pGroup.PGroupStrictCheck();
        return lPreferenceTabLayout;
    }
}
