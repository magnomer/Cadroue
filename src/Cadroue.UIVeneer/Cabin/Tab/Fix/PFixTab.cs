using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PFixTab : PTabSurface
{
    private readonly PFlow pFlow;
    private readonly PViewer pViewer;
    private readonly PClinic pClinic;
    private readonly PList pList;
    private readonly PProcessing pProcessing;

    public PFixTab(QWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        pFlow = new PFlow(lWorkspace.QWorkspaceFlow!);
        pViewer = new PViewer(lWorkspace.QWorkspaceViewer!);
        pList = new PList(lWorkspace.QWorkspaceList!);
        QFixTab = lWorkspace.QWorkspaceMode.QFixTabCreate();
        pClinic = new PClinic(QFixTab.QFixClinic);
        pProcessing = new PProcessing(QFixTab.QFixProcessing);
        var pAction = new PAction(lWorkspace.QWorkspaceMode.QWorkspaceAction!);
        pAction.PActionAllSet(true, QLocalizationCatalog.QLocalizationTextRead("Action.EditAll.Tooltip"));

        QFixCatalog.QFixRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        PFixEventsAttach();

        var pExport = new PExport(lWorkspace.QWorkspaceExport, lWorkspace.QWorkspacePresetOwner);
        PTabLock.PTabLockAttach(pList, pProcessing, pClinic, pExport);
        Content = PTabGridBuild(
            lWorkspace.QWorkspaceMode,
            new System.Windows.UIElement[] { pList, pProcessing, pClinic, pViewer, pExport },
            new PCompass(pFlow, pViewer, lWorkspace.QWorkspaceCompass!),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        QFixTab.QFixLayoutApply(lPreferenceTabLayout);
    }

    public QFixTab QFixTab { get; }

    public override PFlow PTabFlow => pFlow;

    public override PViewer? PTabViewer => pViewer;

    public override PList? PTabList => pList;

    public override void PTabClose()
    {
        base.PTabClose();
        PFixEventsDetach();
        QFixTab.QFixClose();
    }

    public override LSceneTabEntry PTabLayoutRead() => QFixTab.QFixLayoutRead(PTabLayoutCreate());

    private void PFixEventsAttach()
    {
        pProcessing.PProcessingStepChanged += pClinic.PClinicStepShow;
        pClinic.PClinicDiagnosisExecuted += QFixTab.QFixDiagnosisRun;
        QFixTab.QFixCheckup.LCheckupReady += PFixCheckupHandle;
        QFixTab.QFixCheckup.LCheckupProgress += PFixProgressHandle;
        QFixTab.QFixPresetMissing += PExportNotice.PExportMissingShow;
    }

    private void PFixEventsDetach()
    {
        QFixTab.QFixCheckup.LCheckupReady -= PFixCheckupHandle;
        QFixTab.QFixCheckup.LCheckupProgress -= PFixProgressHandle;
    }

    private void PFixCheckupHandle(LCheckupResult lResult) =>
        Dispatcher.BeginInvoke(() => QFixTab.QFixCheckupHandle(lResult));

    private void PFixProgressHandle(string lPath, double lProgress) =>
        Dispatcher.BeginInvoke(() => QFixTab.QFixProgressHandle(lPath, lProgress));
}
