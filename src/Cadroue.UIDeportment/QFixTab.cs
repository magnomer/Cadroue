using Cadroue.Application;
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

    private readonly LPresetSelection qFixPreset;
    private readonly QViewer qFixViewer;
    private readonly QList qFixList;
    private readonly LDocket qFixDocket;

    public QFixTab(LPresetSelection lPreset, QViewer lViewer, QList lList)
    {
        qFixPreset = lPreset;
        qFixViewer = lViewer;
        qFixList = lList;
        qFixDocket = lList.QListDocket;
        QFixProcessing.QProcessingOrderedSet(false);
        QFixAttach();
    }

    public event Action? QFixPresetMissing;

    public QClinic QFixClinic { get; } = new();

    public QProcessing QFixProcessing { get; } = new();

    public LCheckup QFixCheckup { get; } = new();

    public void QFixClose()
    {
        QFixDetach();
        QFixCheckup.Dispose();
    }

    private void QFixAttach()
    {
        QFixClinic.QClinicPlanChanged += QFixChangeHandle;
        qFixList.QListPathChanged += qFixViewer.QViewerWorkspace.CWorkspaceOpen;
        qFixViewer.QViewerOpenNotice += QFixOpenHandle;
        qFixViewer.QViewerOpenRequest += QFixPlanRestore;
        qFixList.QListItemsAdded += QFixItemsHandle;
        qFixList.QListClearChanged += QFixClearHandle;
        qFixViewer.QViewerSource.QViewerPathsDrop += lDropPaths => _ = qFixList.QListPathsAdd(lDropPaths);
    }

    private void QFixDetach()
    {
        QFixClinic.QClinicPlanChanged -= QFixChangeHandle;
        qFixViewer.QViewerOpenNotice -= QFixOpenHandle;
        qFixViewer.QViewerOpenRequest -= QFixPlanRestore;
    }

    public LSceneTabEntry QFixLayoutRead(LSceneTabEntry lLayout)
    {
        LWorkFix lPlan = QFixClinic.QClinicPlanRead();
        LWorkFix lPersistent = LFix.LFixPersistentResolve(lPlan);
        bool lSalvagePersistent = lPlan.LWorkFixSalvage.LWorkSalvagePersistent;
        if (lPersistent.LWorkFixSteps.Any() || lSalvagePersistent)
        {
            lLayout.LSceneInspector = new LSceneInspectorEntry
            {
                LSceneInspectorFix = LFix.LFixPersistentCreate(lPersistent),
                LSceneInspectorSalvage = lSalvagePersistent
            };
        }

        return lLayout;
    }

    public void QFixLayoutApply(LSceneTabEntry? lLayout)
    {
        if (lLayout is null)
        {
            QFixClinic.QClinicMinimizedSet(true);
        }

        if (lLayout?.LSceneInspector is { LSceneInspectorFix: { } lRecord } lInspector)
        {
            QFixClinic.QClinicSaveSuspend();
            try
            {
                QFixClinic.QClinicPlanApply(LFix.LFixPersistentRead(lRecord, lInspector.LSceneInspectorSalvage));
            }
            finally
            {
                QFixClinic.QClinicSaveResume();
            }
        }

        QFixActiveUpdate();
    }

    public void QFixRun(LWorkPriority lPriority, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QFixPresetCheck())
        {
            return;
        }

        LMessenger.LMessengerFixDescribe(
            lPriority,
            QFixSelectedRead() is { } lSelected ? [QFixSourceCreate(lSelected)] : [],
            qFixPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void QFixAllRun(Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QFixPresetCheck())
        {
            return;
        }

        LMessenger.LMessengerFixDescribe(
            LWorkPriority.LWorkPriorityNormal,
            qFixDocket.LDocketUnlockedRead().Select(QFixSourceCreate).ToArray(),
            qFixPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void QFixItemsRun(IReadOnlyList<string> lPaths, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QFixPresetCheck())
        {
            return;
        }

        LMessenger.LMessengerFixDescribe(
            LWorkPriority.LWorkPriorityNormal,
            qFixDocket.LDocketUnlockedRead()
                .Where(lItem => lPaths.Contains(lItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                .Select(QFixSourceCreate)
                .ToArray(),
            qFixPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    // Before the viewer opens a new path: the plan of the source it leaves is saved, then the clinic follows.
    private void QFixOpenHandle(string lPath)
    {
        QFixStateSave();
        QFixClinic.QClinicSourceSet(lPath);
    }

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
        QFixClinic.QClinicResultsRemove(lRemoved);
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
            QFixClinic.QClinicResultSet(lResult.LCheckupSource, lResult.LCheckupKind, lResult);
        }
    }

    public void QFixProgressHandle(string lPath, double lProgress)
    {
        if (QFixListedCheck(lPath))
        {
            QFixClinic.QClinicProgressSet(lPath, lProgress);
        }
    }

    private void QFixItemsHandle(IReadOnlyList<LDocketEntry> lAdded) =>
        QFixPersistentSave(lAdded.Select(lItem => lItem.LDocketEntryPath));

    public void QFixChangeHandle()
    {
        QFixActiveUpdate();
        QFixStateSave();
    }

    public void QFixStateSave()
    {
        if (QFixClinic.QClinicSaveSuspended
            || qFixViewer.QViewerWorkspace.CWorkspaceSource is not { } lSourcePath
            || qFixDocket.LDocketLockCheck(lSourcePath))
        {
            return;
        }

        LWorkFix lPlan = QFixClinic.QClinicPlanRead();
        if (!lPlan.LWorkFixActive && LFix.LFixPlanRead(lSourcePath, LLibrarian.LLibrarianFixLoad) is null)
        {
            return;
        }

        LFix.LFixPlanSave(lSourcePath, lPlan, LLibrarian.LLibrarianFixSave);
        QFixPersistentSave();
    }

    public void QFixPersistentSave() =>
        QFixPersistentSave(qFixDocket.LDocketUnlockedRead().Select(lItem => lItem.LDocketEntryPath));

    public void QFixPlanRestore(string lSourcePath)
    {
        QFixClinic.QClinicSaveSuspend();
        try
        {
            LWorkFix? lSaved = LFix.LFixPlanRead(lSourcePath, LLibrarian.LLibrarianFixLoad);
            LWorkFix lPersistent = LFix.LFixPersistentResolve(QFixClinic.QClinicPlanRead());
            QFixClinic.QClinicPlanApply(LFix.LFixPlanResolve(lSaved, lPersistent));
        }
        finally
        {
            QFixClinic.QClinicSaveResume();
        }

        QFixActiveUpdate();
    }

    public void QFixActiveUpdate()
    {
        LWorkFix lPlan = QFixClinic.QClinicPlanRead();
        foreach (QFixRow lRow in QFixKinds)
        {
            QFixProcessing.QProcessingActiveSet(lRow.QFixRowStep.QProcessingRowKey, QFixActiveCheck(lPlan, lRow));
        }

        QFixProcessing.QProcessingActiveSet(QFixSalvageKey, lPlan.LWorkFixSalvage.LWorkSalvageActive);
    }

    // A fix row is active when the plan repairs its flaw kind.
    private static bool QFixActiveCheck(LWorkFix lPlan, QFixRow lRow) =>
        lPlan.LWorkFixSteps.Any(lStep => lStep.LWorkFixKind == lRow.QFixRowKind && lStep.LWorkFixRepair);

    private void QFixPersistentSave(IEnumerable<string> lPaths)
    {
        if (QFixClinic.QClinicSaveSuspended)
        {
            return;
        }

        LWorkFix lPersistent = LFix.LFixPersistentResolve(QFixClinic.QClinicPlanRead());
        if (!lPersistent.LWorkFixSteps.Any())
        {
            return;
        }

        foreach (string lPath in lPaths)
        {
            LWorkFix? lSaved = LFix.LFixPlanRead(lPath, LLibrarian.LLibrarianFixLoad);
            LFix.LFixPlanSave(lPath, LFix.LFixPlanResolve(lSaved, lPersistent), LLibrarian.LLibrarianFixSave);
        }
    }

    private bool QFixListedCheck(string lPath) => qFixDocket.LDocketItemFind(lPath) is not null;

    private bool QFixPresetCheck()
    {
        if (qFixPreset.LPresetSelectionValid)
        {
            return true;
        }

        QFixPresetMissing?.Invoke();
        return false;
    }

    private LDocketEntry? QFixSelectedRead() =>
        qFixList.QListPathCurrent is { } lPath
        && qFixDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;

    private static LWorkSource QFixSourceCreate(LDocketEntry lItem) =>
        new(lItem.LDocketEntryPath, lItem.LDocketEntryBatch);

    private static QFixRow QFixRowCreate(LFlawKind lKind, string lName, string? lIcon = null) =>
        new(lKind, new QProcessingRow(
            lName, $"/PAsset/PPanel/PProcessingFix{lIcon ?? lName}.svg", $"Processing.Step.{lName}"));
}
