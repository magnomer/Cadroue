using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PMergeTab : PTabSurface
{
    private readonly PFlow pFlow;
    private readonly PViewer pViewer;
    private readonly PList pList;
    private readonly PGroup pGroup;
    private readonly PAction pAction;
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PMergeTab(QWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.QWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.QWorkspaceViewer!);
        pList = new PList(lWorkspace.QWorkspaceList!);
        QMergeTab = lWorkspace.QWorkspaceMode.QMergeTabCreate();
        pGroup = new PGroup(QMergeTab.QMergeGroup);
        pAction = new PAction(lWorkspace.QWorkspaceMode.QWorkspaceAction!);
        PMergeEventsAttach();
        var pExport = new PExport(lWorkspace.QWorkspaceExport, lWorkspace.QWorkspacePresetOwner);
        PTabLockAttach(pList, pExport);
        pTabGrid = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pGroup, pViewer, pExport },
            new PCompass(pFlow, pViewer, lWorkspace.QWorkspaceCompass!),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        Content = pTabGrid;
        QMergeTab.QMergeLayoutApply(lPreferenceTabLayout);
    }

    public QMergeTab QMergeTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override PGroup? PTabGroup => pGroup;

    public override void PTabClose()
    {
        QMergeTab.QMergeClose();
        base.PTabClose();
    }

    public override LSceneTabEntry PTabLayoutRead() => QMergeTab.QMergeLayoutRead(PTabLayoutCreate());

    private void PMergeEventsAttach() => QMergeTab.QMergePresetMissing += PExport.PExportMissingShow;
}
