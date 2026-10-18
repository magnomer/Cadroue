using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed record QFixRow(LFlawKind QFixRowKind, QProcessingRow QFixRowStep);

public sealed class QFixTab
{
    private const string QFixSalvageKey = "Salvage";

    public static readonly IReadOnlyList<QFixRow> QFixKinds =
    [
        QFixRowCreate(LFlawKind.LFlawKindTruncation, "Truncation"),
        QFixRowCreate(LFlawKind.LFlawKindIndex, "Index"),
        QFixRowCreate(LFlawKind.LFlawKindContainer, "Container"),
        QFixRowCreate(LFlawKind.LFlawKindTiming, "Timing"),
        QFixRowCreate(LFlawKind.LFlawKindMetadata, "Metadata"),
        QFixRowCreate(LFlawKind.LFlawKindCoded, "Coded"),
        QFixRowCreate(LFlawKind.LFlawKindFraming, "Framing"),
        QFixRowCreate(LFlawKind.LFlawKindConfig, "Config", "Configuration"),
        QFixRowCreate(LFlawKind.LFlawKindTransport, "Transport"),
        QFixRowCreate(LFlawKind.LFlawKindSecondary, "Secondary"),
        QFixRowCreate(LFlawKind.LFlawKindFfvone, "Ffvone"),
    ];

    public static readonly IReadOnlyList<QProcessingRow> QFixRows =
        QFixKinds.Select(lRow => lRow.QFixRowStep)
            .Append(new QProcessingRow(
                QFixSalvageKey, "/PAsset/PPanel/PProcessingFixSalvage.svg", "Processing.Step.Salvage"))
            .ToList();

    private readonly CFix qFixGate;
    private readonly LPresetSelection qFixPreset;
    private readonly QViewer qFixViewer;
    private readonly QList qFixList;
    private readonly LDocket qFixDocket;

    public QFixTab(CInspector lInspector, LPresetSelection lPreset, QViewer lViewer, QList lList, CFix lFix)
    {
        QFixClinicGate = lInspector.CInspectorClinic;
        QFixClinic = new QClinic(QFixClinicGate);
        qFixGate = lFix;
        qFixPreset = lPreset;
        qFixViewer = lViewer;
        qFixList = lList;
        qFixDocket = lList.QListDocket;
        QFixProcessing.QProcessingOrderedSet(false);
        QFixAttach();
    }

    public event Action? QFixPresetMissing;

    public CClinic QFixClinicGate { get; }

    public QClinic QFixClinic { get; }

    public QProcessing QFixProcessing { get; } = new();

    public LCheckup QFixCheckup { get; } = new();

    public void QFixClose()
    {
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
        qFixViewer.QViewerSource.QViewerPathsDrop += lDropPaths => _ = qFixList.QListPathsAdd(lDropPaths);
    }

    private void QFixDetach()
    {
        QFixClinicGate.CClinicPlanChanged -= QFixChangeHandle;
        qFixViewer.QViewerOpenNotice -= QFixOpenHandle;
        qFixViewer.QViewerOpenRequest -= QFixPlanRestore;
    }

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
        QFixActiveApply(qFixGate.CFixLayoutApply(
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
                qFixDocket.LDocketItemsRead()
                    .Select(lItem => (lItem.LDocketEntryPath, lItem.LDocketEntryBatch, lItem.LDocketEntryLocked))
                    .ToArray(),
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
                qFixDocket.LDocketItemsRead()
                    .Select(lItem => (lItem.LDocketEntryPath, lItem.LDocketEntryBatch, lItem.LDocketEntryLocked))
                    .ToArray(),
                lPaths,
                lRelayTarget,
                lSourceTab))
        {
            QFixPresetMissing?.Invoke();
        }
    }

    // Before the viewer opens a new path: the plan of the source it leaves is saved, then the clinic follows.
    private void QFixOpenHandle(string lPath) =>
        qFixGate.CFixSourceOpen(
            QFixClinicGate,
            qFixViewer.QViewerWorkspace,
            qFixDocket.LDocketItemsRead()
                .Select(lItem => (lItem.LDocketEntryPath, lItem.LDocketEntryBatch, lItem.LDocketEntryLocked))
                .ToArray(),
            lPath);

    public void QFixDiagnosisRun()
    {
        if (QFixSelectedRead() is not { } lSelected)
        {
            return;
        }

        QFixCheckup.LCheckupStart(
            [lSelected.LDocketEntryPath], QFixKinds.Select(lRow => lRow.QFixRowKind).ToArray(), lCheckupForce: true);
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
        qFixGate.CFixPersistentSave(QFixClinicGate, lAdded.Select(lItem => lItem.LDocketEntryPath).ToArray());

    // A plan change saves the plan of the open source; the gate answers which steps the plan has on.
    public void QFixChangeHandle() =>
        QFixActiveApply(qFixGate.CFixStateSave(
            QFixClinicGate,
            qFixViewer.QViewerWorkspace,
            qFixDocket.LDocketItemsRead()
                .Select(lItem => (lItem.LDocketEntryPath, lItem.LDocketEntryBatch, lItem.LDocketEntryLocked))
                .ToArray()));

    public void QFixPlanRestore(string lSourcePath) =>
        QFixActiveApply(qFixGate.CFixPlanRestore(QFixClinicGate, lSourcePath));

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

    private bool QFixListedCheck(string lPath) => qFixDocket.LDocketItemFind(lPath) is not null;

    private LDocketEntry? QFixSelectedRead() =>
        qFixList.QListPathCurrent is { } lPath
        && qFixDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;

    private static QFixRow QFixRowCreate(LFlawKind lKind, string lName, string? lIcon = null) =>
        new(lKind, new QProcessingRow(
            lName, $"/PAsset/PPanel/PProcessingFix{lIcon ?? lName}.svg", $"Processing.Step.{lName}"));
}
