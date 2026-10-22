using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PAudioTab : PTabSurface
{
    private readonly PFlow pFlow;
    private readonly PViewer pViewer;
    private readonly PList pList;
    private readonly PProcessing pProcessing;
    private readonly PInspector pInspector;
    private readonly PDebounce pAudioViewerTimer;

    public PAudioTab(QWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.QWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.QWorkspaceViewer!);
        pList = new PList(lWorkspace.QWorkspaceList!);
        QAudioTab = lWorkspace.QWorkspaceMode.QAudioTabCreate();
        pAudioViewerTimer = new PDebounce(TimeSpan.FromMilliseconds(150), () => QAudioTab.QAudioViewerApply());
        pProcessing = new PProcessing(QAudioTab.QAudioProcessing);
        pInspector = new PInspector(QAudioTab.QAudioInspector.QInspectorFrame);
        QAudioProcessing.QAudioRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        PAudioStepsAttach();
        pProcessing.PProcessingMonitorSet();

        var pAction = new PAction(lWorkspace.QWorkspaceMode.QWorkspaceAction!);
        pAction.PActionAllSet(true, QLocalizationCatalog.QLocalizationTextRead("Action.AudioAll.Tooltip"));

        PAudioEventsAttach();

        var pExport = new PExport(lWorkspace.QWorkspaceExport, lWorkspace.QWorkspacePresetOwner);
        PTabLock.PTabLockAttach(pList, pProcessing, pInspector, pExport);
        Content = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pProcessing, pInspector, pViewer, pExport },
            new PCompass(pFlow, pViewer, lWorkspace.QWorkspaceCompass!),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        QAudioTab.QAudioLayoutApply(lPreferenceTabLayout);
    }

    public QAudioTab QAudioTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override void PTabClose()
    {
        pAudioViewerTimer.PDebounceStop();
        base.PTabClose();
        QAudioTab.QAudioClose();
    }

    public override LSceneTabEntry PTabLayoutRead() => QAudioTab.QAudioLayoutRead(PTabLayoutCreate());

    private void PAudioStepsAttach()
    {
        pProcessing.PProcessingStepChanged += pInspector.QInspectorFrame.QInspectorFrameSet;
        pProcessing.PProcessingStepOpened += _ => pInspector.QInspectorFrame.QInspectorFrameSet(false);
        pProcessing.PProcessingMonitorShown += PAudioMonitorShow;
    }

    private void PAudioEventsAttach()
    {
        QAudioTab.QAudioPresetMissing += PExportNotice.PExportMissingShow;
        QAudioTab.QAudioPresetIncompatible += PExportNotice.PExportIncompatibleShow;
        QAudioTab.QAudioViewerDeferred += pAudioViewerTimer.PDebounceDefer;
    }

    private void PAudioMonitorShow() =>
        PSMonitor.PSMonitorShow(System.Windows.Window.GetWindow(this), QAudioTab.QAudioMonitor, pFlow, pViewer);
}
