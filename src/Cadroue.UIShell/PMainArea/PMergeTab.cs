using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PMainArea;

public sealed class PMergeTab : PTabSurface
{
    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PList pList = new(new LDocket());
    private readonly LGroupSelection lGroupOwner;
    private readonly PGroup pGroup;
    private readonly PAction pAction = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PMergeTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        lGroupOwner = new LGroupSelection(
            lPreferenceTabLayout?.LSceneGroupAuto ?? false,
            lPreferenceTabLayout?.LSceneGroupStrict ?? true,
            lPreferenceTabLayout?.LSceneGroupMode ?? LSeriesNameMode.LSeriesNameBase);
        pGroup = new PGroup(lGroupOwner);
        PTabAction = pAction;
        pAction.PActionRun += pPriority => LMessenger.LMessengerMergeDescribe(
            pPriority, PMergeGroupsRead(), lPresetOwner,
            pAction.PActionRelayTarget, pAction.PActionSourceTab, PMergeRelaysRead());
        pAction.PActionAllAdd += () => LMessenger.LMessengerMergeDescribe(
            LWorkPriority.LWorkPriorityNormal,
            PMergeGroupsRead(),
            lPresetOwner,
            pAction.PActionRelayTarget, pAction.PActionSourceTab, PMergeRelaysRead());
        pList.PListPathChange += PMergePathShow;
        pGroup.PGroupItemOpen += PMergePathShow;
        pGroup.PGroupSourceFiles = () => pList.PListUnlockedRead()
            .Select(pItem => pItem.LDocketEntryPath)
            .ToArray();
        pGroup.PGroupFileRequest = pDropPaths =>
        {
            pList.PListPathsAdd(pDropPaths);
            return PList.PListMediaScan(pDropPaths);
        };
        pList.PListItemsAdd += PMergeItemsHandle;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);
        var pExport = new PExport(lPresetOwner);
        PTabLockAttach(pList, pGroup, pExport);
        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pGroup, pViewer, pExport }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
        Content = pTabGrid;
    }

    private IReadOnlyDictionary<string, Guid> PMergeRelaysRead()
    {
        var pMergeRelays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (LDocketEntry pItem in pList.PListUnlockedRead())
        {
            pMergeRelays[pItem.LDocketEntryPath] = pItem.LDocketEntryBatch;
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

    private void PMergeItemsHandle(IReadOnlyList<LDocketEntry> pAddedItems)
    {
        if (lGroupOwner.LGroupAuto)
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
        lPreferenceTabLayout.LSceneGroupAuto = lGroupOwner.LGroupAuto;
        lPreferenceTabLayout.LSceneGroupStrict = lGroupOwner.LGroupStrict;
        lPreferenceTabLayout.LSceneGroupMode = lGroupOwner.LGroupNameMode;
        return lPreferenceTabLayout;
    }
}
