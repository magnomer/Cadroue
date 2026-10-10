using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PAudioTab : PTabSurface
{
    private readonly PFlow pFlow = new();
    private readonly PViewer pViewer = new(pAudioEligible: true);
    private readonly PList pList = new(new LDocket());
    private readonly PProcessing pProcessing = new();
    private readonly PInspector pInspector = new();
    private readonly System.Windows.Controls.Grid pTabGrid;
    private readonly System.Windows.Threading.DispatcherTimer pAudioViewerTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(150)
    };

    public PAudioTab(LPresetSelection lPresetOwner, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        LAudioTab = new LAudioTab(
            lPresetOwner,
            pInspector.LInspector,
            pViewer.LViewer,
            pList.LList,
            pList.LList.LListDocket,
            pProcessing.LProcessing);
        LAudioTab.LAudioRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        pProcessing.PProcessingStepChanged += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpened += _ => pInspector.PInspectorMinimizeSet(false);
        pProcessing.PProcessingMonitorShown += PAudioMonitorShow;
        pProcessing.PProcessingMonitorSet();

        var pAction = new PAction();
        PTabAction = pAction;
        LAction lAction = pAction.LAction;
        lAction.LActionExecuted += lPriority =>
            LAudioTab.LAudioRun(lPriority, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionAllAdded += () => LAudioTab.LAudioAllRun(lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionItemsAdded += pAudioPaths =>
            LAudioTab.LAudioItemsRun(pAudioPaths, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        pAction.PActionListAttach(pList);
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.AudioAll.Tooltip"));
        pList.LList.LListPathChanged += LAudioTab.LAudioPathHandle;
        pList.LList.LListItemsAdded += LAudioTab.LAudioItemsHandle;
        pViewer.LViewer.LViewerSource.LViewerPathsDrop += pDropPaths => _ = pList.LList.LListPathsAdd(pDropPaths);

        LAudioTab.LAudioPresetMissing += PExport.PExportMissingShow;
        LAudioTab.LAudioPresetIncompatible += PExport.PExportIncompatibleShow;
        LAudioTab.LAudioViewerDeferred += PAudioViewerDefer;
        pAudioViewerTimer.Tick += PAudioViewerTick;

        var pExport = new PExport(lPresetOwner, LWorkKind.LWorkKindAudio);
        PTabLockAttach(pList, pProcessing, pInspector, pExport);
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pProcessing, pInspector, pViewer, pExport },
            new PCompass(pFlow, pViewer),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        Content = pTabGrid;
        LAudioTab.LAudioLayoutApply(lPreferenceTabLayout);
    }

    public LAudioTab LAudioTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override void PTabClose()
    {
        pAudioViewerTimer.Stop();
        base.PTabClose();
        LAudioTab.LAudioClose();
    }

    public override LSceneTabEntry PTabLayoutRead() => LAudioTab.LAudioLayoutRead(PTabLayoutCreate());

    private void PAudioMonitorShow() =>
        PSMonitor.PSMonitorShow(System.Windows.Window.GetWindow(this), LAudioTab.LAudioMonitor, pFlow, pViewer);

    private void PAudioViewerDefer()
    {
        pAudioViewerTimer.Stop();
        pAudioViewerTimer.Start();
    }

    private void PAudioViewerTick(object? pSender, EventArgs pEvent)
    {
        pAudioViewerTimer.Stop();
        LAudioTab.LAudioViewerApply();
    }
}
