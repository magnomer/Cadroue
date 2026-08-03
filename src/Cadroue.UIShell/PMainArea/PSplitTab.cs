using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PSplitTab : PTabSurface
{
    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PSection pSection = new();
    private readonly PList pList = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PSplitTab(LPreset lExportSpecificState, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += lPriority =>
        {
            if (pList.PListUnlockedItemRead() is not { } pSplitSelected)
            {
                return;
            }

            LSplit.LSplitDescribe(
                lPriority,
                pSplitSelected.PListItemPath,
                pSection.PSectionSplitRead(),
                lExportSpecificState,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab,
                pSplitSelected.PListItemRelay,
                LCourier.LCourierPlanPrepare(pAction.PActionRelayTarget));
        };
        pAction.PActionAllAdd += () => _ = LSplit.LSplitAllDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pList.PListUnlockedItemsRead()
                .Select(pItem => new LWorkSource(pItem.PListItemPath, pItem.PListItemRelay))
                .ToArray(),
            lExportSpecificState,
            pAction.PActionRelayTarget,
            pAction.PActionSourceTab,
            LCourier.LCourierPlanPrepare(pAction.PActionRelayTarget));
        pAction.PActionItemsAdd += pSplitPaths => _ = LSplit.LSplitAllDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pList.PListUnlockedItemsRead()
                .Where(pItem => pSplitPaths.Contains(pItem.PListItemPath, StringComparer.OrdinalIgnoreCase))
                .Select(pItem => new LWorkSource(pItem.PListItemPath, pItem.PListItemRelay))
                .ToArray(),
            lExportSpecificState,
            pAction.PActionRelayTarget,
            pAction.PActionSourceTab,
            LCourier.LCourierPlanPrepare(pAction.PActionRelayTarget));
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.AddAll.SplitTooltip"));
        pFlow.PFlowSectionShow(true);
        pSection.PSectionAttach(pFlow);
        pList.PListPathChange += PSplitPathShow;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);
        var pExport = new PExport(lExportSpecificState);
        PTabLockAttach(pList, pSection, pExport);
        pList.PListCurrentLockChange += pLocked => pFlow.PFlowSectionEditSet(!pLocked);
        pFlow.PFlowSectionEditSet(!pList.PListCurrentLockedCheck());
        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pSection, pViewer, pExport }, new PCompass(pFlow, true), pAction, pFlow, lPreferenceTabLayout);
        Content = pTabGrid;
    }

    private void PSplitPathShow(string? pSourcePath)
    {
        if (!string.IsNullOrWhiteSpace(pSourcePath))
        {
            pViewer.PViewerSourceOpen(pSourcePath);
        }
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override LSceneTabRecord PTabLayoutRead() => PTabLayoutRead(pTabGrid);
}
