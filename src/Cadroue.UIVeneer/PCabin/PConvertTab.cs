using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PConvertTab : PTabSurface
{
    private readonly PFlow pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PList pList = new(new LDocket());
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PConvertTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        LConvertTab = new LConvertTab(lPresetOwner, pList.LList, pList.PListDocketRead());
        var pAction = new PAction();
        PTabAction = pAction;
        LAction lAction = pAction.LAction;
        lAction.LActionRun += lPriority =>
            LConvertTab.LConvertRun(lPriority, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionAllAdd += () =>
            LConvertTab.LConvertAllRun(lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionItemsAdd += pConvertPaths =>
            LConvertTab.LConvertItemsRun(pConvertPaths, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        pAction.PActionListAttach(pList);
        LConvertTab.LConvertPresetMissing += PExport.PExportMissingShow;
        pList.PListPathChange += pViewer.LViewer.LViewerPathHandle;
        pViewer.LViewer.LViewerSource.LViewerPathsDrop += pDropPaths => _ = pList.PListPathsAdd(pDropPaths);
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

    public override LSceneTabRecord PTabLayoutRead() => PTabLayoutCreate();
}
