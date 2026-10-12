using Cadroue.Core;
using Cadroue.Application;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PEditTab : PTabSurface
{
    private readonly PFlow pFlow;
    private readonly PViewer pViewer;
    private readonly PInspector pInspector;
    private readonly PList pList;
    private readonly PProcessing pProcessing;
    private readonly System.Windows.Controls.Grid pTabGrid;
    private readonly System.Windows.Threading.DispatcherTimer pEditColorTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(80)
    };
    private readonly System.Windows.Threading.DispatcherTimer pEditHistogramTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(220)
    };

    public PEditTab(LWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.LWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.LWorkspaceViewer!);
        pList = new PList(lWorkspace.LWorkspaceList!);
        LEditTab = lWorkspace.QWorkspaceMode.LEditTabCreate();
        pInspector = new PInspector(LEditTab.LEditInspector);
        pProcessing = new PProcessing(LEditTab.LEditProcessing);
        var pAction = new PAction(lWorkspace.QWorkspaceMode.LWorkspaceAction!);
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.EditAll.Tooltip"));

        QEditProcessing.LEditRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        pProcessing.PProcessingStepChanged += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpened += _ => pInspector.PInspectorMinimizeSet(false);
        pViewer.LViewer.LViewerPlayback.LViewerClockTicked += PEditHistogramDefer;

        LEditTab.LEditPresetMissing += PExport.PExportMissingShow;
        LEditTab.LEditPresetIncompatible += PExport.PExportIncompatibleShow;
        LEditTab.LEditHistogramDeferred += PEditHistogramDefer;
        LEditTab.LEditColorDeferred += PEditColorDefer;
        pEditColorTimer.Tick += PEditColorTick;
        pEditHistogramTimer.Tick += PEditHistogramTick;
        LEditTab.LEditColor.LEditCapableHandle();

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
        LEditTab.LEditLayoutApply(lPreferenceTabLayout);
        LEditTab.LEditStart();
    }

    public LEditTab LEditTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override void PTabClose()
    {
        pEditColorTimer.Stop();
        pEditHistogramTimer.Stop();
        LEditTab.LEditClose();
        base.PTabClose();
    }

    public override LSceneTabEntry PTabLayoutRead() => LEditTab.LEditLayoutRead(PTabLayoutCreate());

    private void PEditHistogramDefer(TimeSpan lPosition) => PEditHistogramDefer();

    private void PEditHistogramDefer()
    {
        pEditHistogramTimer.Stop();
        pEditHistogramTimer.Start();
    }

    private void PEditColorDefer()
    {
        pEditColorTimer.Stop();
        pEditColorTimer.Start();
    }

    private void PEditColorTick(object? pSender, EventArgs pEvent)
    {
        pEditColorTimer.Stop();
        LEditTab.LEditColor.LEditColorApply();
    }

    private void PEditHistogramTick(object? pSender, EventArgs pEvent)
    {
        pEditHistogramTimer.Stop();
        LEditTab.LEditColor.LEditHistogramRun();
    }
}
