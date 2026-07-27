using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PMainArea;

public sealed class PSplitTab : PTabSurface
{
    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PSection pSection = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PSplitTab(LExportSpecificState lExportSpecificState, LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        var pAction = new PAction();
        pAction.PActionRun += lPriority => LSplit.LSplitDescribe(
            lPriority,
            pViewer.PViewerSourcePath,
            pSection.PSectionSplitRead(),
            lExportSpecificState);
        pFlow.PFlowSectionShow(true);
        pSection.PSectionAttach(pFlow);
        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pSection, pViewer, new PExport(lExportSpecificState) }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
        Content = pTabGrid;
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override LPreferenceTabLayoutRecord PTabLayoutRead() => PTabLayoutRead(pTabGrid);
}
