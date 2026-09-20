using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PSplitTab : PTabSurface
{
    private readonly PFlow pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PSection pSection = new();
    private readonly PList pList = new(new LDocket());
    private readonly PProcessing pProcessing = new();
    private readonly PInspector pInspector = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PSplitTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        LSplitTab = new LSplitTab(
            lPresetOwner,
            pInspector.LInspector,
            pViewer.LViewer,
            pList.LList,
            pList.PListDocketRead(),
            pProcessing.LProcessing,
            pFlow.LFlow);
        var pAction = new PAction();
        PTabAction = pAction;
        LAction lAction = pAction.LAction;
        lAction.LActionRun += lPriority =>
            LSplitTab.LSplitRun(lPriority, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionAllAdd += () => LSplitTab.LSplitAllRun(lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionItemsAdd += pSplitPaths =>
            LSplitTab.LSplitItemsRun(pSplitPaths, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        pAction.PActionListAttach(pList);
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.AddAll.SplitTooltip"));

        LSplitTab.LSplitRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpen += _ => pInspector.PInspectorMinimizeSet(false);

        pInspector.PSensorRunShow();
        pInspector.LSensor.LSensorRunApply += PSplitSweepStart;
        pInspector.LSensor.LSensorStopApply += LSplitTab.LSplitSweep.LSplitSweepCancel;
        LSplitTab.LSplitPresetMissing += PExport.PExportMissingShow;
        LSplitTab.LSplitSweep.LSplitBusyApply += PSplitBusyApply;
        LSplitTab.LSplitSweep.LSplitProgressApply += pInspector.PSensorProgressApply;
        LSplitTab.LSplitSweep.LSplitFailRaise += PSplitFailShow;

        pSection.PSectionAttach(pFlow);
        pList.PListPathChange += LSplitTab.LSplitPathHandle;
        pList.PListLockChange += LSplitTab.LSplitLockHandle;
        pViewer.LViewer.LViewerSource.LViewerPathsDrop += pDropPaths => _ = pList.PListPathsAdd(pDropPaths);
        var pExport = new PExport(lPresetOwner, pExportSmartAllowed: true);
        PTabLockAttach(pList, pSection, pProcessing, pInspector, pExport);
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pSection, pProcessing, pInspector, pViewer, pExport },
            new PCompass(pFlow, pViewer, true), pAction, pFlow, lPreferenceTabLayout);
        Content = pTabGrid;
        LSplitTab.LSplitLayoutApply(lPreferenceTabLayout);
        LSplitTab.LSplitStart();
    }

    public LSplitTab LSplitTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override void PTabClose()
    {
        LSplitTab.LSplitClose();
        base.PTabClose();
    }

    public override LSceneTabRecord PTabLayoutRead() => LSplitTab.LSplitLayoutRead(PTabLayoutCreate());

    private void PSplitSweepStart() => _ = LSplitTab.LSplitSweep.LSplitSweepStart();

    private void PSplitBusyApply(bool pBusy)
    {
        pInspector.PSensorLockSet(pBusy);
        pInspector.PSensorProgressSet(pBusy);
        pProcessing.IsEnabled = !pBusy;
    }

    private void PSplitFailShow(string pTitle, string pMessage) =>
        PSWarning.PSWarningShow(System.Windows.Window.GetWindow(this), pTitle, pMessage);
}
