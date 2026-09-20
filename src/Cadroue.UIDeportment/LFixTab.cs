using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed record LFixRow(LFlawKind LFixRowKind, LProcessingRow LFixRowStep);

public sealed class LFixTab
{
    private const string LFixSalvageKey = "Salvage";

    public static readonly IReadOnlyList<LFixRow> LFixKinds =
    [
        LFixRowCreate(LFlawKind.LFlawKindTruncation, "Truncation"),
        LFixRowCreate(LFlawKind.LFlawKindIndex, "Index"),
        LFixRowCreate(LFlawKind.LFlawKindContainer, "Container"),
        LFixRowCreate(LFlawKind.LFlawKindTiming, "Timing"),
        LFixRowCreate(LFlawKind.LFlawKindMetadata, "Metadata"),
        LFixRowCreate(LFlawKind.LFlawKindCoded, "Coded"),
        LFixRowCreate(LFlawKind.LFlawKindFraming, "Framing"),
        LFixRowCreate(LFlawKind.LFlawKindConfig, "Config", "Configuration"),
        LFixRowCreate(LFlawKind.LFlawKindTransport, "Transport"),
        LFixRowCreate(LFlawKind.LFlawKindSecondary, "Secondary"),
        LFixRowCreate(LFlawKind.LFlawKindFfvone, "Ffvone"),
    ];

    public static readonly IReadOnlyList<LProcessingRow> LFixRows =
        LFixKinds.Select(lRow => lRow.LFixRowStep)
            .Append(new LProcessingRow(
                LFixSalvageKey, "/PAsset/PPanel/PProcessingFixSalvage.svg", "Processing.Step.Salvage"))
            .ToList();

    private readonly LPresetSelection lFixPreset;
    private readonly LClinic lFixClinic;
    private readonly LViewer lFixViewer;
    private readonly LList lFixList;
    private readonly LDocket lFixDocket;
    private readonly LProcessing lFixProcessing;

    public LFixTab(
        LPresetSelection lPreset,
        LClinic lClinic,
        LViewer lViewer,
        LList lList,
        LDocket lDocket,
        LProcessing lProcessing)
    {
        lFixPreset = lPreset;
        lFixClinic = lClinic;
        lFixViewer = lViewer;
        lFixList = lList;
        lFixDocket = lDocket;
        lFixProcessing = lProcessing;
        lProcessing.LProcessingOrderedSet(false);
        lClinic.LClinicPlanChange += LFixChangeHandle;
    }

    public event Action? LFixPresetMissing;

    public LCheckup LFixCheckup { get; } = new();

    public void LFixClose()
    {
        lFixClinic.LClinicPlanChange -= LFixChangeHandle;
        LFixCheckup.Dispose();
    }

    public LSceneTabRecord LFixLayoutRead(LSceneTabRecord lLayout)
    {
        LWorkFix lPlan = lFixClinic.LClinicPlanRead();
        LWorkFix lPersistent = LFix.LFixPersistentResolve(lPlan);
        bool lSalvagePersistent = lPlan.LWorkFixSalvage.LWorkSalvagePersistent;
        if (lPersistent.LWorkFixSteps.Any() || lSalvagePersistent)
        {
            lLayout.LSceneInspector = new LSceneInspectorRecord
            {
                LSceneInspectorFix = LFix.LFixPersistentCreate(lPersistent),
                LSceneInspectorSalvage = lSalvagePersistent
            };
        }

        return lLayout;
    }

    public void LFixLayoutApply(LSceneTabRecord? lLayout)
    {
        if (lLayout is null)
        {
            lFixClinic.LClinicMinimizedSet(true);
        }

        if (lLayout?.LSceneInspector is { LSceneInspectorFix: { } lRecord } lInspector)
        {
            lFixClinic.LClinicSaveSuspend();
            try
            {
                lFixClinic.LClinicPlanApply(LFix.LFixPersistentRead(lRecord, lInspector.LSceneInspectorSalvage));
            }
            finally
            {
                lFixClinic.LClinicSaveResume();
            }
        }

        LFixActiveUpdate();
    }

    public void LFixRun(LWorkPriority lPriority, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LFixPresetCheck())
        {
            return;
        }

        LMessenger.LMessengerFixDescribe(
            lPriority,
            LFixSelectedRead() is { } lSelected ? [LFixSourceCreate(lSelected)] : [],
            lFixPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void LFixAllRun(Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LFixPresetCheck())
        {
            return;
        }

        LMessenger.LMessengerFixDescribe(
            LWorkPriority.LWorkPriorityNormal,
            lFixDocket.LDocketUnlockedRead().Select(LFixSourceCreate).ToArray(),
            lFixPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void LFixItemsRun(IReadOnlyList<string> lPaths, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LFixPresetCheck())
        {
            return;
        }

        LMessenger.LMessengerFixDescribe(
            LWorkPriority.LWorkPriorityNormal,
            lFixDocket.LDocketUnlockedRead()
                .Where(lItem => lPaths.Contains(lItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                .Select(LFixSourceCreate)
                .ToArray(),
            lFixPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void LFixPathHandle(string? lPath)
    {
        if (string.IsNullOrWhiteSpace(lPath) || lFixViewer.LViewerSourceMatch(lPath))
        {
            return;
        }

        LFixStateSave();
        lFixClinic.LClinicSourceSet(lPath);
        lFixViewer.LViewerPathHandle(lPath);
        LFixPlanRestore(lPath);
    }

    public void LFixDiagnosisRun()
    {
        if (LFixSelectedRead() is not { } lSelected)
        {
            return;
        }

        LFixCheckup.LCheckupStart(
            [lSelected.LDocketEntryPath], LFixKinds.Select(lRow => lRow.LFixRowKind).ToArray(), lCheckupForce: true);
    }

    public void LFixClearHandle(IReadOnlyList<string> lRemoved)
    {
        foreach (string lPath in lRemoved)
        {
            LFixCheckup.LCheckupSourceCancel(lPath);
        }

        lFixClinic.LClinicResultsRemove(lRemoved);
    }

    public void LFixCheckupHandle(LCheckupResult lResult)
    {
        if (LFixListedCheck(lResult.LCheckupSource))
        {
            lFixClinic.LClinicResultSet(lResult.LCheckupSource, lResult.LCheckupKind, lResult);
        }
    }

    public void LFixProgressHandle(string lPath, double lProgress)
    {
        if (LFixListedCheck(lPath))
        {
            lFixClinic.LClinicProgressSet(lPath, lProgress);
        }
    }

    public void LFixItemsHandle(IReadOnlyList<LDocketEntry> lAdded) =>
        LFixPersistentSave(lAdded.Select(lItem => lItem.LDocketEntryPath));

    public void LFixChangeHandle()
    {
        LFixActiveUpdate();
        LFixStateSave();
    }

    public void LFixStateSave()
    {
        if (lFixClinic.LClinicSaveSuspended
            || lFixViewer.LViewerSourcePath is not { } lSourcePath
            || lFixDocket.LDocketLockCheck(lSourcePath))
        {
            return;
        }

        LWorkFix lPlan = lFixClinic.LClinicPlanRead();
        if (!lPlan.LWorkFixActive && LFix.LFixPlanRead(lSourcePath, LLibrarian.LLibrarianFixLoad) is null)
        {
            return;
        }

        LFix.LFixPlanSave(lSourcePath, lPlan, LLibrarian.LLibrarianFixSave);
        LFixPersistentSave();
    }

    public void LFixPersistentSave() =>
        LFixPersistentSave(lFixDocket.LDocketUnlockedRead().Select(lItem => lItem.LDocketEntryPath));

    public void LFixPlanRestore(string lSourcePath)
    {
        lFixClinic.LClinicSaveSuspend();
        try
        {
            LWorkFix? lSaved = LFix.LFixPlanRead(lSourcePath, LLibrarian.LLibrarianFixLoad);
            LWorkFix lPersistent = LFix.LFixPersistentResolve(lFixClinic.LClinicPlanRead());
            lFixClinic.LClinicPlanApply(LFix.LFixPlanResolve(lSaved, lPersistent));
        }
        finally
        {
            lFixClinic.LClinicSaveResume();
        }

        LFixActiveUpdate();
    }

    public void LFixActiveUpdate()
    {
        LWorkFix lPlan = lFixClinic.LClinicPlanRead();
        foreach (LFixRow lRow in LFixKinds)
        {
            bool lActive = lPlan.LWorkFixSteps.Any(
                lStep => lStep.LWorkFixKind == lRow.LFixRowKind && lStep.LWorkFixRepair);
            lFixProcessing.LProcessingActiveSet(lRow.LFixRowStep.LProcessingRowKey, lActive);
        }

        lFixProcessing.LProcessingActiveSet(LFixSalvageKey, lPlan.LWorkFixSalvage.LWorkSalvageActive);
    }

    private void LFixPersistentSave(IEnumerable<string> lPaths)
    {
        if (lFixClinic.LClinicSaveSuspended)
        {
            return;
        }

        LWorkFix lPersistent = LFix.LFixPersistentResolve(lFixClinic.LClinicPlanRead());
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

    private bool LFixListedCheck(string lPath) => lFixDocket.LDocketItemFind(lPath) is not null;

    private bool LFixPresetCheck()
    {
        if (lFixPreset.LPresetSelectionValid)
        {
            return true;
        }

        LFixPresetMissing?.Invoke();
        return false;
    }

    private LDocketEntry? LFixSelectedRead() =>
        lFixList.LListPathCurrent is { } lPath
        && lFixDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;

    private static LWorkSource LFixSourceCreate(LDocketEntry lItem) =>
        new(lItem.LDocketEntryPath, lItem.LDocketEntryBatch);

    private static LFixRow LFixRowCreate(LFlawKind lKind, string lName, string? lIcon = null) =>
        new(lKind, new LProcessingRow(
            lName, $"/PAsset/PPanel/PProcessingFix{lIcon ?? lName}.svg", $"Processing.Step.{lName}"));
}
