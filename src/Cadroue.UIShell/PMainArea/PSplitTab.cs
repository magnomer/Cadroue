using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.MigrationInterface;

namespace Cadroue.UIShell.PMainArea;

public sealed class PSplitTab : PTabSurface
{
    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PSection pSection = new();
    private readonly PList pList = new(new LDocket());
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PSplitTab(LPreset lExportSpecificState, LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += lPriority =>
        {
            if (pList.PListEditableRead() is not { } pSplitSelected)
            {
                return;
            }

            LMessenger.LMessengerSplitDescribe(
                lPriority,
                pSplitSelected.LDocketEntryPath,
                pFlow.PFlowSplitRead(),
                lExportSpecificState,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab,
                pSplitSelected.LDocketEntryBatch,
                LCartographer.LCartographerPlanPrepare(pAction.PActionRelayTarget));
        };
        pAction.PActionAllAdd += () => _ = LMessenger.LMessengerSplitAllDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pList.PListUnlockedRead()
                .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                .ToArray(),
            lExportSpecificState,
            pAction.PActionRelayTarget,
            pAction.PActionSourceTab,
            LCartographer.LCartographerPlanPrepare(pAction.PActionRelayTarget));
        pAction.PActionItemsAdd += pSplitPaths => _ = LMessenger.LMessengerSplitAllDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pList.PListUnlockedRead()
                .Where(pItem => pSplitPaths.Contains(pItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                .ToArray(),
            lExportSpecificState,
            pAction.PActionRelayTarget,
            pAction.PActionSourceTab,
            LCartographer.LCartographerPlanPrepare(pAction.PActionRelayTarget));
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.AddAll.SplitTooltip"));
        pFlow.PFlowSectionShow(true);
        pSection.PSectionAttach(pFlow);
        pList.PListPathChange += PSplitPathShow;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);
        var pExport = new PExport(lPresetOwner);
        PTabLockAttach(pList, pSection, pExport);
        pList.PListLockChange += pLocked => pFlow.PFlowEditSet(!pLocked);
        pFlow.PFlowEditSet(!pList.PListLockCheck());
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
    public override bool PTabSectionShow => true;
    public override LSceneTabRecord PTabLayoutRead() => PTabLayoutRead(pTabGrid);
}
