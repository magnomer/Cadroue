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
    private readonly PProcessing pProcessing;
    private readonly PInspector pInspector;
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PSplitTab(QWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.QWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.QWorkspaceViewer!);
        pList = new PList(lWorkspace.QWorkspaceList!);
        QSplitTab = lWorkspace.QWorkspaceMode.QSplitTabCreate();
        pInspector = new PInspector(QSplitTab.QSplitInspector);
        pProcessing = new PProcessing(QSplitTab.QSplitProcessing);
        pSection = new PSection(QSplitTab.QSplitSection);
        var pAction = new PAction(lWorkspace.QWorkspaceMode.QWorkspaceAction!);
        pAction.PActionAllSet(true, QLocalizationCatalog.QLocalizationTextRead("Action.AddAll.SplitTooltip"));

        QSplitTab.QSplitRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        pProcessing.PProcessingStepChanged += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpened += _ => pInspector.PInspectorMinimizeSet(false);

        pInspector.PSensorRunShow();
        QSplitTab.QSplitPresetMissing += PExport.PExportMissingShow;
        QSplitTab.QSplitSweep.QSplitBusyApplied += PSplitBusyApply;
        QSplitTab.QSplitSweep.QSplitProgressApplied += pInspector.PSensorProgressApply;
        QSplitTab.QSplitSweep.QSplitFailRaised += PSplitFailShow;

        var pExport = new PExport(lWorkspace.QWorkspaceExport, lWorkspace.QWorkspacePresetOwner);
        PTabLockAttach(pList, pSection, pProcessing, pInspector, pExport);
        pTabGrid = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pSection, pProcessing, pInspector, pViewer, pExport },
            new PCompass(pFlow, pViewer, lWorkspace.QWorkspaceCompass!), pAction, pFlow, lPreferenceTabLayout);
        Content = pTabGrid;
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

    private void PSplitBusyApply(bool pBusy)
    {
        pInspector.PSensorLockSet(pBusy);
        pInspector.PSensorProgressSet(pBusy);
        pProcessing.IsEnabled = !pBusy;
    }

    private void PSplitFailShow(string pTitle, string pMessage) =>
        PSWarning.PSWarningShow(System.Windows.Window.GetWindow(this), pTitle, pMessage);
}
