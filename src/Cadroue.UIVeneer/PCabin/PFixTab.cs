using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PFixTab : PTabSurface
{
    private readonly PFlow pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PClinic pClinic = new();
    private readonly PList pList = new(new LDocket());
    private readonly PProcessing pProcessing = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PFixTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        LFixTab = new LFixTab(
            lPresetOwner,
            pClinic.LClinic,
            pViewer.LViewer,
            pList.LList,
            pList.LList.LListDocket,
            pProcessing.LProcessing);
        var pAction = new PAction();
        PTabAction = pAction;
        LAction lAction = pAction.LAction;
        lAction.LActionRun += lPriority =>
            LFixTab.LFixRun(lPriority, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionAllAdd += () => LFixTab.LFixAllRun(lAction.LActionRelayTarget, lAction.LActionSourceTab);
        lAction.LActionItemsAdd += pFixPaths =>
            LFixTab.LFixItemsRun(pFixPaths, lAction.LActionRelayTarget, lAction.LActionSourceTab);
        pAction.PActionListAttach(pList);
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.EditAll.Tooltip"));

        LFixTab.LFixRows.ToList().ForEach(pProcessing.PProcessingRowAdd);
        pProcessing.PProcessingStepChange += pClinic.PClinicStepShow;
        pClinic.PClinicDiagnosisRun += LFixTab.LFixDiagnosisRun;
        LFixTab.LFixCheckup.LCheckupReady += PFixCheckupHandle;
        LFixTab.LFixCheckup.LCheckupProgress += PFixProgressHandle;
        LFixTab.LFixPresetMissing += PExport.PExportMissingShow;

        pList.LList.LListPathChange += LFixTab.LFixPathHandle;
        pList.LList.LListItemsAdd += LFixTab.LFixItemsHandle;
        pList.LList.LListClearChange += LFixTab.LFixClearHandle;
        pViewer.LViewer.LViewerSource.LViewerPathsDrop += pDropPaths => _ = pList.LList.LListPathsAdd(pDropPaths);

        var pExport = new PExport(lPresetOwner, pExportSmartAllowed: true);
        PTabLockAttach(pList, pProcessing, pClinic, pExport);
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pProcessing, pClinic, pViewer, pExport },
            new PCompass(pFlow, pViewer),
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

    public override LSceneTabRecord PTabLayoutRead() => LFixTab.LFixLayoutRead(PTabLayoutCreate());

    private void PFixCheckupHandle(LCheckupResult lResult) =>
        Dispatcher.BeginInvoke(() => LFixTab.LFixCheckupHandle(lResult));

    private void PFixProgressHandle(string lPath, double lProgress) =>
        Dispatcher.BeginInvoke(() => LFixTab.LFixProgressHandle(lPath, lProgress));
}
