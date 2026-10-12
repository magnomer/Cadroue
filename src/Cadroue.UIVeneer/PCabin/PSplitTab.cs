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
    private readonly PProcessing pProcessing;
    private readonly PInspector pInspector;
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PSplitTab(LWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.LWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.LWorkspaceViewer!);
        pList = new PList(lWorkspace.LWorkspaceList!);
        LSplitTab = lWorkspace.QWorkspaceMode.LSplitTabCreate();
        pInspector = new PInspector(LSplitTab.LSplitInspector);
        pProcessing = new PProcessing(LSplitTab.LSplitProcessing);
        pSection = new PSection(LSplitTab.LSplitSection);
        var pAction = new PAction(lWorkspace.QWorkspaceMode.LWorkspaceAction!);
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.AddAll.SplitTooltip"));

        LSplitTab.LSplitRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        pProcessing.PProcessingStepChanged += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpened += _ => pInspector.PInspectorMinimizeSet(false);

        pInspector.PSensorRunShow();
        LSplitTab.LSplitPresetMissing += PExport.PExportMissingShow;
        LSplitTab.LSplitSweep.LSplitBusyApplied += PSplitBusyApply;
        LSplitTab.LSplitSweep.LSplitProgressApplied += pInspector.PSensorProgressApply;
        LSplitTab.LSplitSweep.LSplitFailRaised += PSplitFailShow;

        var pExport = new PExport(lWorkspace.LWorkspaceExport, lWorkspace.LWorkspacePresetOwner);
        PTabLockAttach(pList, pSection, pProcessing, pInspector, pExport);
        pTabGrid = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pSection, pProcessing, pInspector, pViewer, pExport },
            new PCompass(pFlow, pViewer, lWorkspace.LWorkspaceCompass!), pAction, pFlow, lPreferenceTabLayout);
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

    private void PSplitBusyApply(bool pBusy)
    {
        pInspector.PSensorLockSet(pBusy);
        pInspector.PSensorProgressSet(pBusy);
        pProcessing.IsEnabled = !pBusy;
    }

    private void PSplitFailShow(string pTitle, string pMessage) =>
        PSWarning.PSWarningShow(System.Windows.Window.GetWindow(this), pTitle, pMessage);
}
