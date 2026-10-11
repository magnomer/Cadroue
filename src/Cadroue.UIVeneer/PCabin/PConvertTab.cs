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

    public PConvertTab(LStrip lStrip, LPresetSelection lPresetOwner, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lStrip);
        pViewer = new PViewer(lStrip);
        pList = new PList(lStrip, new LDocket());
        LConvertTab = new LConvertTab(lPresetOwner, pList.LList, pList.LList.LListDocket);
        var pAction = new PAction();
        PTabAction = pAction;
        LAction lAction = pAction.LAction;
        lAction.LActionExecuted += lPriority =>
            LConvertTab.LConvertRun(lPriority, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionAllAdded += () =>
            LConvertTab.LConvertAllRun(lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionItemsAdded += pConvertPaths =>
            LConvertTab.LConvertItemsRun(pConvertPaths, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        pAction.PActionListAttach(pList);
        LConvertTab.LConvertPresetMissing += PExport.PExportMissingShow;
        pList.LList.LListPathChanged += pViewer.LViewer.LViewerPathHandle;
        pViewer.LViewer.LViewerSource.LViewerPathsDrop += pDropPaths => _ = pList.LList.LListPathsAdd(pDropPaths);
        var pExport = new PExport(lPresetOwner);
        PTabLockAttach(pList, pExport);
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pViewer, pExport },
            new PCompass(pFlow, pViewer),
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
