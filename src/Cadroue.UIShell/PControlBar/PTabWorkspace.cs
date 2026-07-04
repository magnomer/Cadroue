using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PMainArea;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PControlBar;

public sealed class PTabWorkspace
{
    public PTabWorkspace(string pTabLayoutKey)
    {
        PTabWorkspaceExportSpecificState = new LExportSpecificState();
        PTabWorkspaceTabSurface = PTabWorkspaceTabSurfaceCreate(pTabLayoutKey, PTabWorkspaceExportSpecificState);
        bool pHasSourceInfo = pTabLayoutKey is not ("Merge" or "Worklist");
        bool pAudioOnlyAllowed = pTabLayoutKey == "Audio";
        PTabWorkspaceSourcePanel = pHasSourceInfo ? new PSourcePanel(pAudioOnlyAllowed) : null;
        PTabWorkspaceInfoPanel = pHasSourceInfo ? new PInfoPanel() : null;
        PTabWorkspaceFlow = PTabWorkspaceTabSurface.PTabFlow;
        PTabWorkspaceViewer = PTabWorkspaceTabSurface.PTabViewer;
        PTabWorkspaceViewer?.PViewerPanelAudioOnlyAllowSet(pAudioOnlyAllowed);
        PTabWorkspaceSourcePanel?.PSourcePanelAttach(PTabWorkspaceViewer);
        PTabWorkspaceInfoPanel?.PInfoPanelAttach(PTabWorkspaceViewer);
        PTabWorkspaceMainAreaRoot = PTabWorkspaceMainAreaRootCreate();
    }

    public FrameworkElement PTabWorkspaceMainAreaRoot { get; }

    public PTabSurface PTabWorkspaceTabSurface { get; }

    internal LExportSpecificState PTabWorkspaceExportSpecificState { get; }

    public PFlowControl? PTabWorkspaceFlow { get; }

    public PViewerPanel? PTabWorkspaceViewer { get; }

    public PSectionPanel? PTabWorkspaceSectionPanel { get; }

    public PSourcePanel? PTabWorkspaceSourcePanel { get; }

    public PInfoPanel? PTabWorkspaceInfoPanel { get; }

    public void PTabWorkspaceCloseRequest()
    {
        PTabWorkspaceFlow?.PFlowCloseRequest();
        PTabWorkspaceViewer?.PViewerPanelCloseRequest();
    }

    private FrameworkElement PTabWorkspaceMainAreaRootCreate()
    {
        if (PTabWorkspaceSourcePanel is null || PTabWorkspaceInfoPanel is null)
        {
            return PTabWorkspaceTabSurface;
        }

        var pRoot = new Grid();
        pRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(PTabWorkspaceSourcePanel, 0);
        Grid.SetRow(PTabWorkspaceInfoPanel, 1);
        Grid.SetRow(PTabWorkspaceTabSurface, 2);
        pRoot.Children.Add(PTabWorkspaceSourcePanel);
        pRoot.Children.Add(PTabWorkspaceInfoPanel);
        pRoot.Children.Add(PTabWorkspaceTabSurface);
        return pRoot;
    }

    private static PTabSurface PTabWorkspaceTabSurfaceCreate(string pTabLayoutKey, LExportSpecificState lExportSpecificState)
    {
        return pTabLayoutKey switch
        {
            "Edit" => new PEditTab(lExportSpecificState),
            "Audio" => new PAudioTab(lExportSpecificState),
            "Convert" => new PConvertTab(lExportSpecificState),
            "Merge" => new PMergeTab(lExportSpecificState),
            "Worklist" => new PWorklistTab(),
            _ => new PSplitTab(lExportSpecificState)
        };
    }
}
