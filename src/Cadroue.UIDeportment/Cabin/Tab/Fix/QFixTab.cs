using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QFixTab
{
    private readonly CFix qFixGate;
    private readonly CFixPlan qFixPlanGate;
    private readonly LPresetSelection qFixPreset;
    private readonly QViewer qFixViewer;
    private readonly QList qFixList;
    private readonly LDocket qFixDocket;

    public QFixTab(CInspector lInspector, LPresetSelection lPreset, QViewer lViewer, QList lList, CFix lFix)
    {
        QFixClinicGate = lInspector.CInspectorClinic;
        QFixClinic = new QClinic(QFixClinicGate);
        qFixGate = lFix;
        qFixPlanGate = lFix.CFixPlan;
        qFixPreset = lPreset;
        qFixViewer = lViewer;
        qFixList = lList;
        qFixDocket = lList.QListDocket;
        QFixFace = new QFixFace(this);
        QFixProcessing.QProcessingOrderedSet(false);
        QFixAttach();
    }

    public event Action? QFixPresetMissing;

    public CClinic QFixClinicGate { get; }

    public QClinic QFixClinic { get; }

    public QProcessing QFixProcessing { get; } = new();

    public LCheckup QFixCheckup { get; } = new();

    public QFixFace QFixFace { get; }

    public void QFixFaceAttach(Dispatcher lDispatcher, Action lMissingNotice) =>
        QFixFace.QFixFaceAttach(QFixCheckup, lDispatcher, lMissingNotice);

    public void QFixClose()
    {
        QFixFace.QFixFaceDetach();
        QFixDetach();
        QFixCheckup.Dispose();
    }

    private void QFixAttach()
    {
        QFixClinicGate.CClinicPlanChanged += QFixChangeHandle;
        qFixList.QListPathChanged += qFixViewer.QViewerWorkspace.CWorkspaceOpen;
        qFixViewer.QViewerOpenNotice += QFixOpenHandle;
        qFixViewer.QViewerOpenRequest += QFixPlanRestore;
        qFixList.QListItemsAdded += QFixItemsHandle;
        qFixList.QListClearChanged += QFixClearHandle;
        qFixViewer.QViewerSource.QViewerPathsDrop += QFixDropHandle;
        QFixProcessing.QProcessingStepChanged += QFixClinic.QClinicStepSet;
        QFixClinic.QClinicDiagnosisRequested += QFixDiagnosisRun;
    }

    private void QFixDetach()
    {
        QFixClinicGate.CClinicPlanChanged -= QFixChangeHandle;
        qFixList.QListPathChanged -= qFixViewer.QViewerWorkspace.CWorkspaceOpen;
        qFixViewer.QViewerOpenNotice -= QFixOpenHandle;
        qFixViewer.QViewerOpenRequest -= QFixPlanRestore;
        qFixList.QListItemsAdded -= QFixItemsHandle;
        qFixList.QListClearChanged -= QFixClearHandle;
        qFixViewer.QViewerSource.QViewerPathsDrop -= QFixDropHandle;
        QFixProcessing.QProcessingStepChanged -= QFixClinic.QClinicStepSet;
        QFixClinic.QClinicDiagnosisRequested -= QFixDiagnosisRun;
    }

    private void QFixDropHandle(IReadOnlyList<string> lDropPaths) =>
        _ = qFixList.QListPathsAdd(lDropPaths);

    public LSceneTabEntry QFixLayoutRead(LSceneTabEntry lLayout)
    {
        LWorkFix lPlan = QFixPlanRead();
        LWorkFix lPersistent = LFixPlan.LFixPersistentResolve(lPlan);
        bool lSalvagePersistent = lPlan.LWorkFixSalvage.LWorkSalvagePersistent;
        if (lPersistent.LWorkFixSteps.Any() || lSalvagePersistent)
        {
            lLayout.LSceneInspector = new LSceneInspectorEntry
            {
                LSceneInspectorFix = LFixSidecar.LFixPersistentCreate(lPersistent),
                LSceneInspectorSalvage = lSalvagePersistent
            };
        }

        return lLayout;
    }

    // The layout's persistent plan becomes an engine record here until the layout reaches the gate through a port.
    public void QFixLayoutApply(LSceneTabEntry? lLayout) =>
        QFixActiveApply(qFixPlanGate.CFixLayoutApply(
            QFixClinicGate,
            lLayout is null,
            lLayout?.LSceneInspector is { LSceneInspectorFix: { } lRecord } lInspector
                ? new LClinicOutlet(LFixSidecar.LFixPersistentRead(lRecord, lInspector.LSceneInspectorSalvage))
                : null));

    // Each run gesture is one gate call; the gate answers false when the preset is missing, which raises the notice.
    public void QFixRun(bool lUrgent, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!qFixGate.CFixRun(
                qFixPreset.LPresetSelectionValid,
                qFixPreset.LPresetSelectionName,
                qFixList.QListPathCurrent,
                QFixItemsRead(),
                lUrgent,
                lRelayTarget,
                lSourceTab))
        {
            QFixPresetMissing?.Invoke();
        }
    }

    public void QFixAllRun(Guid lRelayTarget, Guid lSourceTab) => QFixItemsRun(null, lRelayTarget, lSourceTab);

    // No paths runs every unlocked item; the gate picks the items and dispatches.
    public void QFixItemsRun(IReadOnlyList<string>? lPaths, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!qFixGate.CFixAllRun(
                qFixPreset.LPresetSelectionValid,
                qFixPreset.LPresetSelectionName,
                QFixItemsRead(),
                lPaths,
                lRelayTarget,
                lSourceTab))
        {
            QFixPresetMissing?.Invoke();
        }
    }

    // Before the viewer opens a new path: the plan of the source it leaves is saved, then the clinic follows.
    private void QFixOpenHandle(string lPath) =>
        qFixPlanGate.CFixSourceOpen(QFixClinicGate, qFixViewer.QViewerWorkspace, QFixItemsRead(), lPath);

    public void QFixDiagnosisRun()
    {
        if (QFixSelectedRead() is not { } lSelected)
        {
            return;
        }

        QFixCheckup.LCheckupStart(
            [lSelected.LDocketEntryPath],
            QFixCatalog.QFixKinds.Select(lRow => lRow.QFixRowKind).ToArray(),
            lCheckupForce: true);
    }

    public void QFixClearHandle(IReadOnlyList<string> lRemoved)
    {
        QFixCheckupCancel(lRemoved);
        QFixClinicGate.CClinicResultsRemove(lRemoved);
    }

    private void QFixCheckupCancel(IReadOnlyList<string> lRemoved)
    {
        foreach (string lPath in lRemoved)
        {
            QFixCheckup.LCheckupSourceCancel(lPath);
        }
    }

    public void QFixCheckupHandle(LCheckupResult lResult)
    {
        if (QFixListedCheck(lResult.LCheckupSource))
        {
            QFixClinicGate.CClinicResultAccept(new LCheckupOutlet(lResult));
        }
    }

    public void QFixProgressHandle(string lPath, double lProgress)
    {
        if (QFixListedCheck(lPath))
        {
            QFixClinicGate.CClinicProgressSet(lPath, lProgress);
        }
    }

    private void QFixItemsHandle(IReadOnlyList<LDocketEntry> lAdded) =>
        qFixPlanGate.CFixPersistentSave(QFixClinicGate, lAdded.Select(lItem => lItem.LDocketEntryPath).ToArray());

    // A plan change saves the plan of the open source; the gate answers which steps the plan has on.
    public void QFixChangeHandle() =>
        QFixActiveApply(
            qFixPlanGate.CFixStateSave(QFixClinicGate, qFixViewer.QViewerWorkspace, QFixItemsRead()));

    public void QFixPlanRestore(string lSourcePath) =>
        QFixActiveApply(qFixPlanGate.CFixPlanRestore(QFixClinicGate, lSourcePath));

    private void QFixActiveApply(IReadOnlyList<(string CFixStep, bool CFixActive)> lSteps)
    {
        foreach ((string lStep, bool lActive) in lSteps)
        {
            QFixProcessing.QProcessingActiveSet(lStep, lActive);
        }
    }

    // The gate hands its plan to an empty engine record, which builds the fix plan.
    private LWorkFix QFixPlanRead()
    {
        LClinicOutlet lPlan = new();
        QFixClinicGate.CClinicPlanApply(lPlan);
        return lPlan.LClinicPlan;
    }

    // Every listed item as the fix gates take it: path, batch and lock.
    private (string CFixPath, Guid CFixBatch, bool CFixLocked)[] QFixItemsRead() =>
        qFixDocket.LDocketItemsRead()
            .Select(lItem => (lItem.LDocketEntryPath, lItem.LDocketEntryBatch, lItem.LDocketEntryLocked))
            .ToArray();

    private bool QFixListedCheck(string lPath) => qFixDocket.LDocketItemFind(lPath) is not null;

    private LDocketEntry? QFixSelectedRead() =>
        qFixList.QListPathCurrent is { } lPath
        && qFixDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;
}
