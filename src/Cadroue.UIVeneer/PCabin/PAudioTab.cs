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

    public PAudioTab(QWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.QWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.QWorkspaceViewer!);
        pList = new PList(lWorkspace.QWorkspaceList!);
        QAudioTab = lWorkspace.QWorkspaceMode.QAudioTabCreate();
        pProcessing = new PProcessing(QAudioTab.QAudioProcessing);
        pInspector = new PInspector(QAudioTab.QAudioInspector);
        QAudioTab.QAudioRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        pProcessing.PProcessingStepChanged += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpened += _ => pInspector.PInspectorMinimizeSet(false);
        pProcessing.PProcessingMonitorShown += PAudioMonitorShow;
        pProcessing.PProcessingMonitorSet();

        var pAction = new PAction(lWorkspace.QWorkspaceMode.QWorkspaceAction!);
        pAction.PActionAllSet(true, QLocalizationCatalog.QLocalizationTextRead("Action.AudioAll.Tooltip"));

        QAudioTab.QAudioPresetMissing += PExport.PExportMissingShow;
        QAudioTab.QAudioPresetIncompatible += PExport.PExportIncompatibleShow;
        QAudioTab.QAudioViewerDeferred += PAudioViewerDefer;
        pAudioViewerTimer.Tick += PAudioViewerTick;

        var pExport = new PExport(lWorkspace.QWorkspaceExport, lWorkspace.QWorkspacePresetOwner);
        PTabLockAttach(pList, pProcessing, pInspector, pExport);
        pTabGrid = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pProcessing, pInspector, pViewer, pExport },
            new PCompass(pFlow, pViewer, lWorkspace.QWorkspaceCompass!),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        Content = pTabGrid;
        QAudioTab.QAudioLayoutApply(lPreferenceTabLayout);
    }

    public QAudioTab QAudioTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override void PTabClose()
    {
        pAudioViewerTimer.Stop();
        base.PTabClose();
        QAudioTab.QAudioClose();
    }

    public override LSceneTabEntry PTabLayoutRead() => QAudioTab.QAudioLayoutRead(PTabLayoutCreate());

    private void PAudioMonitorShow() =>
        PSMonitor.PSMonitorShow(System.Windows.Window.GetWindow(this), QAudioTab.QAudioMonitor, pFlow, pViewer);

    private void PAudioViewerDefer()
    {
        pAudioViewerTimer.Stop();
        pAudioViewerTimer.Start();
    }

    private void PAudioViewerTick(object? pSender, EventArgs pEvent)
    {
        pAudioViewerTimer.Stop();
        QAudioTab.QAudioViewerApply();
    }
}
