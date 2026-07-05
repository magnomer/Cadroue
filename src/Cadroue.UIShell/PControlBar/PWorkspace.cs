using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PMainArea;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PControlBar;

public sealed class PWorkspace
{
    public PWorkspace(string pTabLayoutKey)
    {
        PWorkspaceExportState = new LExportSpecificState();
        PWorkspaceSurface = PWorkspaceSurfaceCreate(pTabLayoutKey, PWorkspaceExportState);
        bool pHasSourceInfo = pTabLayoutKey is not ("Merge" or "Worklist");
        bool pAudioOnlyAllowed = pTabLayoutKey == "Audio";
        PWorkspaceSource = pHasSourceInfo ? new PSource(pAudioOnlyAllowed) : null;
        PWorkspaceInfo = pHasSourceInfo ? new PInfo() : null;
        PWorkspaceFlow = PWorkspaceSurface.PTabFlow;
        PWorkspaceViewer = PWorkspaceSurface.PTabViewer;
        PWorkspaceViewer?.PViewerAudioSet(pAudioOnlyAllowed);
        PWorkspaceSource?.PSourceAttach(PWorkspaceViewer);
        PWorkspaceInfo?.PInfoAttach(PWorkspaceViewer);
        PWorkspaceRoot = PWorkspaceRootCreate();
    }

    public FrameworkElement PWorkspaceRoot { get; }

    public PTabSurface PWorkspaceSurface { get; }

    internal LExportSpecificState PWorkspaceExportState { get; }

    public PFlowControl? PWorkspaceFlow { get; }

    public PViewer? PWorkspaceViewer { get; }

    public PSection? PWorkspaceSection { get; }

    public PSource? PWorkspaceSource { get; }

    public PInfo? PWorkspaceInfo { get; }

    public void PWorkspaceClose()
    {
        PWorkspaceFlow?.PFlowClose();
        PWorkspaceViewer?.PViewerClose();
    }

    private FrameworkElement PWorkspaceRootCreate()
    {
        if (PWorkspaceSource is null || PWorkspaceInfo is null)
        {
            return PWorkspaceSurface;
        }

        var pRoot = new Grid();
        pRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pRoot.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(PWorkspaceSource, 0);
        Grid.SetRow(PWorkspaceInfo, 1);
        Grid.SetRow(PWorkspaceSurface, 2);
        pRoot.Children.Add(PWorkspaceSource);
        pRoot.Children.Add(PWorkspaceInfo);
        pRoot.Children.Add(PWorkspaceSurface);
        return pRoot;
    }

    private static PTabSurface PWorkspaceSurfaceCreate(string pTabLayoutKey, LExportSpecificState lExportSpecificState)
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
