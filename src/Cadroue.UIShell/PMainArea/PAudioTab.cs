using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PAudioTab : PTabSurface
{
    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PList pList = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PAudioTab(LExportSpecificState lExportSpecificState, LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        var pAction = new PAction();
        pAction.PActionRun += LAudio.LAudioDescribe;
        pList.PListPathChange += PAudioPathShow;
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);
        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, new PProcessing(), new PInspector(), pViewer, new PExport(lExportSpecificState) }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
        Content = pTabGrid;
    }

    private void PAudioPathShow(string? pSourcePath)
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
