using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PSplitTab : PTabSurface
{
    private readonly PFlow pFlow;
    private readonly PViewer pViewer;
    private readonly PSection pSection;
    private readonly PList pList;
    private readonly PProcessing pProcessing = new();
    private readonly PInspector pInspector = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PSplitTab(LStrip lStrip, LPresetSelection lPresetOwner, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lStrip);
        pViewer = new PViewer(lStrip);
        pList = new PList(lStrip, new LDocket());
        LSplitTab = new LSplitTab(
            lPresetOwner,
            pInspector.LInspector,
            pViewer.LViewer,
            pList.LList,
            pList.LList.LListDocket,
            pProcessing.LProcessing,
            pFlow.LFlow);
        pSection = new PSection(new LSection(pFlow.LFlow));
        var pAction = new PAction();
        PTabAction = pAction;
        LAction lAction = pAction.LAction;
        lAction.LActionExecuted += lPriority =>
            LSplitTab.LSplitRun(lPriority, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionAllAdded += () => LSplitTab.LSplitAllRun(lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionItemsAdded += pSplitPaths =>
            LSplitTab.LSplitItemsRun(pSplitPaths, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        pAction.PActionListAttach(pList);
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.AddAll.SplitTooltip"));

        LSplitTab.LSplitRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        pProcessing.PProcessingStepChanged += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpened += _ => pInspector.PInspectorMinimizeSet(false);

        pInspector.PSensorRunShow();
        pInspector.LSensor.LSensorRunApplied += PSplitSweepStart;
        pInspector.LSensor.LSensorStopApplied += LSplitTab.LSplitSweep.LSplitSweepCancel;
        LSplitTab.LSplitPresetMissing += PExport.PExportMissingShow;
        LSplitTab.LSplitSweep.LSplitBusyApplied += PSplitBusyApply;
        LSplitTab.LSplitSweep.LSplitProgressApplied += pInspector.PSensorProgressApply;
        LSplitTab.LSplitSweep.LSplitFailRaised += PSplitFailShow;

        pList.LList.LListPathChanged += LSplitTab.LSplitPathHandle;
        pList.LList.LListLockChanged += LSplitTab.LSplitLockHandle;
        pViewer.LViewer.LViewerSource.LViewerPathsDrop += pDropPaths => _ = pList.LList.LListPathsAdd(pDropPaths);
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

    public override LSceneTabEntry PTabLayoutRead() => LSplitTab.LSplitLayoutRead(PTabLayoutCreate());

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
