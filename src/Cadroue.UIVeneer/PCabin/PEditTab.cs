using Cadroue.Core;
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

    public PEditTab(QWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.QWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.QWorkspaceViewer!);
        pList = new PList(lWorkspace.QWorkspaceList!);
        QEditTab = lWorkspace.QWorkspaceMode.QEditTabCreate();
        pInspector = new PInspector(QEditTab.QEditInspector);
        pProcessing = new PProcessing(QEditTab.QEditProcessing);
        var pAction = new PAction(lWorkspace.QWorkspaceMode.QWorkspaceAction!);
        pAction.PActionAllSet(true, QLocalizationCatalog.QLocalizationTextRead("Action.EditAll.Tooltip"));

        QEditProcessing.QEditRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        PEditEventsAttach();
        QEditTab.QEditColor.QEditCapableHandle();

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
        QEditTab.QEditLayoutApply(lPreferenceTabLayout);
        QEditTab.QEditStart();
    }

    public QEditTab QEditTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override void PTabClose()
    {
        pEditColorTimer.Stop();
        pEditHistogramTimer.Stop();
        QEditTab.QEditClose();
        base.PTabClose();
    }

    public override LSceneTabEntry PTabLayoutRead() => QEditTab.QEditLayoutRead(PTabLayoutCreate());

    private void PEditEventsAttach()
    {
        pProcessing.PProcessingStepChanged += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpened += _ => pInspector.PInspectorMinimizeSet(false);
        pViewer.QViewer.QViewerPlayback.QViewerClockTicked += PEditHistogramDefer;

        QEditTab.QEditPresetMissing += PExport.PExportMissingShow;
        QEditTab.QEditPresetIncompatible += PExport.PExportIncompatibleShow;
        QEditTab.QEditHistogramDeferred += PEditHistogramDefer;
        QEditTab.QEditColorDeferred += PEditColorDefer;
        pEditColorTimer.Tick += PEditColorTick;
        pEditHistogramTimer.Tick += PEditHistogramTick;
    }

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
        QEditTab.QEditColor.QEditColorApply();
    }

    private void PEditHistogramTick(object? pSender, EventArgs pEvent)
    {
        pEditHistogramTimer.Stop();
        QEditTab.QEditColor.QEditHistogramRun();
    }
}
