using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PConvertTab : PTabSurface
{
    private readonly PFlow pFlow;
    private readonly PViewer pViewer;
    private readonly PList pList;

    public PConvertTab(QWorkspace lWorkspace, PExport pExport, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.QWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.QWorkspaceViewer!);
        pList = new PList(lWorkspace.QWorkspaceList!);
        QConvertTab = lWorkspace.QWorkspaceMode.QConvertTabCreate();
        var pAction = new PAction(lWorkspace.QWorkspaceMode.QWorkspaceAction!);
        PConvertEventsAttach();
        PTabLock.PTabLockAttach(pList, pExport);
        Content = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pViewer, pExport },
            new PCompass(pFlow, pViewer, lWorkspace.QWorkspaceCompass!),
            pAction,
            pFlow,
            lPreferenceTabLayout);
    }

    public QConvertTab QConvertTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override LSceneTabEntry PTabLayoutRead() => PTabLayoutCreate();

    private void PConvertEventsAttach() => QConvertTab.QConvertPresetMissing += PExportNotice.PExportMissingShow;
}
