using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PEditTab : PTabSurface
{
    private readonly PFlow pFlow;
    private readonly PViewer pViewer;
    private readonly PInspector pInspector;
    private readonly PList pList;
    private readonly PProcessing pProcessing;
    private readonly PDebounce pEditColorTimer;
    private readonly PDebounce pEditHistogramTimer;

    public PEditTab(QWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.QWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.QWorkspaceViewer!);
        pList = new PList(lWorkspace.QWorkspaceList!);
        QEditTab = lWorkspace.QWorkspaceMode.QEditTabCreate();
        pEditColorTimer = new PDebounce(TimeSpan.FromMilliseconds(80), () => QEditTab.QEditColor.QEditColorApply());
        pEditHistogramTimer = new PDebounce(
            TimeSpan.FromMilliseconds(220), () => QEditTab.QEditColor.QEditHistogramRun());
        pInspector = new PInspector(QEditTab.QEditInspector.QInspectorFrame);
        pProcessing = new PProcessing(QEditTab.QEditProcessing);
        var pAction = new PAction(lWorkspace.QWorkspaceMode.QWorkspaceAction!);
        pAction.PActionAllSet(true, QLocalizationCatalog.QLocalizationTextRead("Action.EditAll.Tooltip"));

        QEditProcessing.QEditRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        PEditEventsAttach();
        QEditTab.QEditColor.QEditCapableHandle();

        var pExport = new PExport(lWorkspace.QWorkspaceExport, lWorkspace.QWorkspacePresetOwner);
        PTabLock.PTabLockAttach(pList, pProcessing, pInspector, pExport);
        Content = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pProcessing, pInspector, pViewer, pExport },
            new PCompass(pFlow, pViewer, lWorkspace.QWorkspaceCompass!),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        QEditTab.QEditLayoutApply(lPreferenceTabLayout);
        QEditTab.QEditStart();
    }

    public QEditTab QEditTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override void PTabClose()
    {
        pEditColorTimer.PDebounceStop();
        pEditHistogramTimer.PDebounceStop();
        QEditTab.QEditClose();
        base.PTabClose();
    }

    public override LSceneTabEntry PTabLayoutRead() => QEditTab.QEditLayoutRead(PTabLayoutCreate());

    private void PEditEventsAttach()
    {
        pProcessing.PProcessingStepChanged += pInspector.QInspectorFrame.QInspectorFrameSet;
        pProcessing.PProcessingStepOpened += _ => pInspector.QInspectorFrame.QInspectorFrameSet(false);
        pViewer.QViewer.QViewerPlayback.QViewerClockTicked += PEditHistogramDefer;

        QEditTab.QEditPresetMissing += PExportNotice.PExportMissingShow;
        QEditTab.QEditPresetIncompatible += PExportNotice.PExportIncompatibleShow;
        QEditTab.QEditHistogramDeferred += pEditHistogramTimer.PDebounceDefer;
        QEditTab.QEditColorDeferred += pEditColorTimer.PDebounceDefer;
    }

    private void PEditHistogramDefer(TimeSpan lPosition) => pEditHistogramTimer.PDebounceDefer();
}
