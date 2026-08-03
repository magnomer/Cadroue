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

    public PMergeTab(LPreset lExportSpecificState, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        PTabAction = pAction;
        pAction.PActionRun += pPriority => LMerge.LMergeDescribe(
            pPriority, PMergeGroupsRead(), lExportSpecificState,
            pAction.PActionRelayTarget, pAction.PActionSourceTab, PMergeRelaysRead());
        pAction.PActionAllAdd += () => LMerge.LMergeDescribe(
            LWorkPriority.LWorkPriorityNormal,
            PMergeGroupsRead(),
            lExportSpecificState,
            pAction.PActionRelayTarget, pAction.PActionSourceTab, PMergeRelaysRead());
        pList.PListPathChange += PMergePathShow;
        pGroup.PGroupItemOpen += PMergePathShow;
        pGroup.PGroupSourceFiles = () => pList.PListUnlockedRead()
            .Select(pItem => pItem.PListItemPath)
            .ToArray();
        pGroup.PGroupFileRequest = pDropPaths =>
        {
            pList.PListPathsAdd(pDropPaths);
            return PList.PListMediaScan(pDropPaths);
        };
        pList.PListItemsAdd += PMergeItemsHandle;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);
        var pExport = new PExport(lExportSpecificState);
        PTabLockAttach(pList, pGroup, pExport);
        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pGroup, pViewer, pExport }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
        Content = pTabGrid;
        pGroup.PGroupModeRestore(
            lPreferenceTabLayout?.LSceneGroupAuto ?? false,
            lPreferenceTabLayout?.LSceneGroupStrict ?? true);
    }

    private IReadOnlyDictionary<string, Guid> PMergeRelaysRead()
    {
        var pMergeRelays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (PListItem pItem in pList.PListUnlockedRead())
        {
            pMergeRelays[pItem.PListItemPath] = pItem.PListItemRelay;
        }

        return pMergeRelays;
    }

    private IReadOnlyList<LWorkGroup> PMergeGroupsRead() =>
        pGroup.PGroupGroupsRead()
            .Select(pGroupSelection => new LWorkGroup(
                pGroupSelection.PGroupSelectionName,
                pGroupSelection.PGroupSelectionPaths
                    .Where(pPath => !pList.PListLockCheck(pPath))
                    .ToArray()))
            .Where(pGroupSelection => pGroupSelection.LWorkGroupPaths.Count > 0)
            .ToArray();

    private void PMergeItemsHandle(IReadOnlyList<PListItem> pAddedItems)
    {
        if (pGroup.PGroupAutoCheck())
        {
            pGroup.PGroupAutoUpdate();
        }
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

    public override LSceneTabRecord PTabLayoutRead()
    {
        LSceneTabRecord lPreferenceTabLayout = PTabLayoutRead(pTabGrid);
        lPreferenceTabLayout.LSceneGroupAuto = pGroup.PGroupAutoCheck();
        lPreferenceTabLayout.LSceneGroupStrict = pGroup.PGroupStrictCheck();
        return lPreferenceTabLayout;
    }
}
