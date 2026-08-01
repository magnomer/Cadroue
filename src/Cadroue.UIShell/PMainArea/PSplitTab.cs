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

    public PSplitTab(LPreset lExportSpecificState, LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += lPriority => LSplit.LSplitDescribe(
            lPriority,
            pViewer.PViewerSourcePath,
            pSection.PSectionSplitRead(),
            lExportSpecificState,
            pAction.PActionRelayTarget,
            pAction.PActionSourceTab);
        pAction.PActionAllAdd += () => _ = LSplit.LSplitAllDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pList.PListItemsRead()
                .Select(pItem => new LWorkSource(pItem.PListItemPath, pItem.PListItemRelay))
                .ToArray(),
            lExportSpecificState,
            pAction.PActionRelayTarget,
            pAction.PActionSourceTab);
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.AddAll.SplitTooltip"));
        pFlow.PFlowSectionShow(true);
        pSection.PSectionAttach(pFlow);
        pList.PListPathChange += PSplitPathShow;
        PTabViewerAttach(pList, pViewer);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);
        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pSection, pViewer, new PExport(lExportSpecificState) }, new PCompass(pFlow, true), pAction, pFlow, lPreferenceTabLayout);
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
    public override LPreferenceTabLayoutRecord PTabLayoutRead() => PTabLayoutRead(pTabGrid);
}
