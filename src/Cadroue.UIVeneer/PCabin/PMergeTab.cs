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

    public PMergeTab(LWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.LWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.LWorkspaceViewer!);
        pList = new PList(lWorkspace.LWorkspaceList!);
        LMergeTab = lWorkspace.QWorkspaceMode.LMergeTabCreate();
        pGroup = new PGroup(LMergeTab.LMergeGroup);
        pAction = new PAction(lWorkspace.QWorkspaceMode.LWorkspaceAction!);
        LMergeTab.LMergePresetMissing += PExport.PExportMissingShow;
        var pExport = new PExport(lWorkspace.LWorkspaceExport, lWorkspace.LWorkspacePresetOwner);
        PTabLockAttach(pList, pExport);
        pTabGrid = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pGroup, pViewer, pExport },
            new PCompass(pFlow, pViewer, lWorkspace.LWorkspaceCompass!),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        Content = pTabGrid;
        LMergeTab.LMergeLayoutApply(lPreferenceTabLayout);
    }

    public LMergeTab LMergeTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override PGroup? PTabGroup => pGroup;

    public override void PTabClose()
    {
        LMergeTab.LMergeClose();
        base.PTabClose();
    }

    public override LSceneTabEntry PTabLayoutRead() => LMergeTab.LMergeLayoutRead(PTabLayoutCreate());
}
