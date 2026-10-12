using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PAudioTab : PTabSurface
{
    private readonly PFlow pFlow;
    private readonly PViewer pViewer;
    private readonly PList pList;
    private readonly PProcessing pProcessing;
    private readonly PInspector pInspector;
    private readonly System.Windows.Controls.Grid pTabGrid;
    private readonly System.Windows.Threading.DispatcherTimer pAudioViewerTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(150)
    };

    public PAudioTab(LWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.LWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.LWorkspaceViewer!);
        pList = new PList(lWorkspace.LWorkspaceList!);
        LAudioTab = lWorkspace.QWorkspaceMode.LAudioTabCreate();
        pProcessing = new PProcessing(LAudioTab.LAudioProcessing);
        pInspector = new PInspector(LAudioTab.LAudioInspector);
        LAudioTab.LAudioRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        pProcessing.PProcessingStepChanged += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpened += _ => pInspector.PInspectorMinimizeSet(false);
        pProcessing.PProcessingMonitorShown += PAudioMonitorShow;
        pProcessing.PProcessingMonitorSet();

        var pAction = new PAction(lWorkspace.QWorkspaceMode.LWorkspaceAction!);
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.AudioAll.Tooltip"));

        LAudioTab.LAudioPresetMissing += PExport.PExportMissingShow;
        LAudioTab.LAudioPresetIncompatible += PExport.PExportIncompatibleShow;
        LAudioTab.LAudioViewerDeferred += PAudioViewerDefer;
        pAudioViewerTimer.Tick += PAudioViewerTick;

        var pExport = new PExport(lWorkspace.LWorkspaceExport, lWorkspace.LWorkspacePresetOwner);
        PTabLockAttach(pList, pProcessing, pInspector, pExport);
        pTabGrid = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pProcessing, pInspector, pViewer, pExport },
            new PCompass(pFlow, pViewer, lWorkspace.LWorkspaceCompass!),
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
