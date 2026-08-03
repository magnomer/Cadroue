using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PConvertTab : PTabSurface
{
    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PList pList = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PConvertTab(LPreset lExportSpecificState, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += lPriority => _ = LConvert.LConvertDescribe(
            lPriority,
            pList.PListItemRead() is { } pConvertSelected
                ? new[] { new LWorkSource(pConvertSelected.PListItemPath, pConvertSelected.PListItemRelay) }
                : Array.Empty<LWorkSource>(),
            lExportSpecificState,
            pAction.PActionRelayTarget,
            pAction.PActionSourceTab);
        pAction.PActionAllAdd += () => _ = LConvert.LConvertDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pList.PListItemsRead()
                .Select(pItem => new LWorkSource(pItem.PListItemPath, pItem.PListItemRelay))
                .ToArray(),
            lExportSpecificState,
            pAction.PActionRelayTarget,
            pAction.PActionSourceTab);
        pAction.PActionItemsAdd += pConvertPaths => _ = LConvert.LConvertDescribe(
            LWorkPriority.LWorkPriorityNormal,
            pList.PListItemsRead()
                .Where(pItem => pConvertPaths.Contains(pItem.PListItemPath, StringComparer.OrdinalIgnoreCase))
                .Select(pItem => new LWorkSource(pItem.PListItemPath, pItem.PListItemRelay))
                .ToArray(),
            lExportSpecificState,
            pAction.PActionRelayTarget,
            pAction.PActionSourceTab);
        pList.PListPathChange += PConvertPathShow;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);
        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pViewer, new PExport(lExportSpecificState) }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
        Content = pTabGrid;
    }

    private void PConvertPathShow(string? pSourcePath)
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
