using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PFixTab : PTabSurface
{
    private readonly PFlow pFlow;
    private readonly PViewer pViewer;
    private readonly PClinic pClinic;
    private readonly PList pList;
    private readonly PProcessing pProcessing;
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PFixTab(LWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.LWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.LWorkspaceViewer!);
        pList = new PList(lWorkspace.LWorkspaceList!);
        LFixTab = lWorkspace.QWorkspaceMode.LFixTabCreate();
        pClinic = new PClinic(LFixTab.LFixClinic);
        pProcessing = new PProcessing(LFixTab.LFixProcessing);
        var pAction = new PAction(lWorkspace.QWorkspaceMode.LWorkspaceAction!);
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.EditAll.Tooltip"));

        LFixTab.LFixRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        pProcessing.PProcessingStepChanged += pClinic.PClinicStepShow;
        pClinic.PClinicDiagnosisExecuted += LFixTab.LFixDiagnosisRun;
        LFixTab.LFixCheckup.LCheckupReady += PFixCheckupHandle;
        LFixTab.LFixCheckup.LCheckupProgress += PFixProgressHandle;
        LFixTab.LFixPresetMissing += PExport.PExportMissingShow;

        var pExport = new PExport(lWorkspace.LWorkspaceExport, lWorkspace.LWorkspacePresetOwner);
        PTabLockAttach(pList, pProcessing, pClinic, pExport);
        pTabGrid = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pProcessing, pClinic, pViewer, pExport },
            new PCompass(pFlow, pViewer, lWorkspace.LWorkspaceCompass!),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        Content = pTabGrid;
        LFixTab.LFixLayoutApply(lPreferenceTabLayout);
    }

    public LFixTab LFixTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override void PTabClose()
    {
        base.PTabClose();
        LFixTab.LFixCheckup.LCheckupReady -= PFixCheckupHandle;
        LFixTab.LFixCheckup.LCheckupProgress -= PFixProgressHandle;
        LFixTab.LFixClose();
    }

    public override LSceneTabEntry PTabLayoutRead() => LFixTab.LFixLayoutRead(PTabLayoutCreate());

    private void PFixCheckupHandle(LCheckupResult lResult) =>
        Dispatcher.BeginInvoke(() => LFixTab.LFixCheckupHandle(lResult));

    private void PFixProgressHandle(string lPath, double lProgress) =>
        Dispatcher.BeginInvoke(() => LFixTab.LFixProgressHandle(lPath, lProgress));
}
