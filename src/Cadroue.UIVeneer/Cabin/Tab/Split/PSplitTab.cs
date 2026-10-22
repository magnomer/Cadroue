using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PSplitTab : PTabSurface
{
    private readonly PFlow pFlow;
    private readonly PViewer pViewer;
    private readonly PList pList;
    private readonly PProcessing pProcessing;
    private readonly PInspector pInspector;

    public PSplitTab(QWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.QWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.QWorkspaceViewer!);
        pList = new PList(lWorkspace.QWorkspaceList!);
        QSplitTab = lWorkspace.QWorkspaceMode.QSplitTabCreate();
        pInspector = new PInspector(QSplitTab.QSplitInspector.QInspectorFrame);
        pProcessing = new PProcessing(QSplitTab.QSplitProcessing);
        var pSection = new PSection(QSplitTab.QSplitSection);
        var pAction = new PAction(lWorkspace.QWorkspaceMode.QWorkspaceAction!);
        pAction.PActionAllSet(true, QLocalizationCatalog.QLocalizationTextRead("Action.AddAll.SplitTooltip"));

        QSplitTab.QSplitRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        PSplitStepsAttach();

        pInspector.QInspectorFrame.QInspectorFrameSet(false, true);
        PSplitEventsAttach();

        var pExport = new PExport(lWorkspace.QWorkspaceExport, lWorkspace.QWorkspacePresetOwner);
        PTabLock.PTabLockAttach(pList, pSection, pProcessing, pInspector, pExport);
        Content = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pSection, pProcessing, pInspector, pViewer, pExport },
            new PCompass(pFlow, pViewer, lWorkspace.QWorkspaceCompass!), pAction, pFlow, lPreferenceTabLayout);
        QSplitTab.QSplitLayoutApply(lPreferenceTabLayout);
        QSplitTab.QSplitStart();
    }

    public QSplitTab QSplitTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override void PTabClose()
    {
        QSplitTab.QSplitClose();
        base.PTabClose();
    }

    public override LSceneTabEntry PTabLayoutRead() => QSplitTab.QSplitLayoutRead(PTabLayoutCreate());

    private void PSplitStepsAttach()
    {
        pProcessing.PProcessingStepChanged += pInspector.QInspectorFrame.QInspectorFrameSet;
        pProcessing.PProcessingStepOpened += _ => pInspector.QInspectorFrame.QInspectorFrameSet(false);
    }

    private void PSplitEventsAttach()
    {
        QSplitTab.QSplitWork.QSplitPresetMissing += PExportNotice.PExportMissingShow;
        QSplitTab.QSplitSweep.QSplitBusyApplied += PSplitBusyApply;
        QSplitTab.QSplitSweep.QSplitProgressApplied += pInspector.QInspectorFrame.QInspectorFrameSet;
        QSplitTab.QSplitSweep.QSplitFailRaised += PSplitFailShow;
    }

    private void PSplitBusyApply(bool pBusy)
    {
        pInspector.QInspectorFrame.QInspectorFrameSet(pBusy, true);
        pProcessing.IsEnabled = !pBusy;
    }

    private void PSplitFailShow(string pTitle, string pMessage) =>
        PSWarning.PSWarningShow(System.Windows.Window.GetWindow(this), pTitle, pMessage);
}
