using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PConvertTab : PTabSurface
{
    private readonly PFlow pFlow;
    private readonly PViewer pViewer;
    private readonly PList pList;
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PConvertTab(LWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.LWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.LWorkspaceViewer!);
        pList = new PList(lWorkspace.LWorkspaceList!);
        LConvertTab = lWorkspace.QWorkspaceMode.LConvertTabCreate();
        var pAction = new PAction(lWorkspace.QWorkspaceMode.LWorkspaceAction!);
        LConvertTab.LConvertPresetMissing += PExport.PExportMissingShow;
        var pExport = new PExport(lWorkspace.LWorkspaceExport, lWorkspace.LWorkspacePresetOwner);
        PTabLockAttach(pList, pExport);
        pTabGrid = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pViewer, pExport },
            new PCompass(pFlow, pViewer, lWorkspace.LWorkspaceCompass!),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        Content = pTabGrid;
    }

    public LConvertTab LConvertTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override LSceneTabEntry PTabLayoutRead() => PTabLayoutCreate();
}
