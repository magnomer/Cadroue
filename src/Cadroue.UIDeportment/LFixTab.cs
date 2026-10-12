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
    private readonly LViewer lFixViewer;
    private readonly LList lFixList;
    private readonly LDocket lFixDocket;

    public LFixTab(LPresetSelection lPreset, LViewer lViewer, LList lList)
    {
        lFixPreset = lPreset;
        lFixViewer = lViewer;
        lFixList = lList;
        lFixDocket = lList.LListDocket;
        LFixProcessing.LProcessingOrderedSet(false);
        LFixClinic.LClinicPlanChanged += LFixChangeHandle;
        lList.LListPathChanged += LFixPathHandle;
        lList.LListItemsAdded += LFixItemsHandle;
        lList.LListClearChanged += LFixClearHandle;
        lViewer.LViewerSource.LViewerPathsDrop += lDropPaths => _ = lList.LListPathsAdd(lDropPaths);
    }

    public event Action? LFixPresetMissing;

    public LClinic LFixClinic { get; } = new();

    public LProcessing LFixProcessing { get; } = new();

    public LCheckup LFixCheckup { get; } = new();

    public void LFixClose()
    {
        LFixClinic.LClinicPlanChanged -= LFixChangeHandle;
        LFixCheckup.Dispose();
    }

    public LSceneTabEntry LFixLayoutRead(LSceneTabEntry lLayout)
    {
        LWorkFix lPlan = LFixClinic.LClinicPlanRead();
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

    public void LFixLayoutApply(LSceneTabEntry? lLayout)
    {
        if (lLayout is null)
        {
            LFixClinic.LClinicMinimizedSet(true);
        }

        if (lLayout?.LSceneInspector is { LSceneInspectorFix: { } lRecord } lInspector)
        {
            LFixClinic.LClinicSaveSuspend();
            try
            {
                LFixClinic.LClinicPlanApply(LFix.LFixPersistentRead(lRecord, lInspector.LSceneInspectorSalvage));
            }
            finally
            {
                LFixClinic.LClinicSaveResume();
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
        LFixClinic.LClinicSourceSet(lPath);
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

        LFixClinic.LClinicResultsRemove(lRemoved);
    }

    public void LFixCheckupHandle(LCheckupResult lResult)
    {
        if (LFixListedCheck(lResult.LCheckupSource))
        {
            LFixClinic.LClinicResultSet(lResult.LCheckupSource, lResult.LCheckupKind, lResult);
        }
    }

    public void LFixProgressHandle(string lPath, double lProgress)
    {
        if (LFixListedCheck(lPath))
        {
            LFixClinic.LClinicProgressSet(lPath, lProgress);
        }
    }

    private void LFixItemsHandle(IReadOnlyList<LDocketEntry> lAdded) =>
        LFixPersistentSave(lAdded.Select(lItem => lItem.LDocketEntryPath));

    public void LFixChangeHandle()
    {
        LFixActiveUpdate();
        LFixStateSave();
    }

    public void LFixStateSave()
    {
        if (LFixClinic.LClinicSaveSuspended
            || lFixViewer.LViewerSourcePath is not { } lSourcePath
            || lFixDocket.LDocketLockCheck(lSourcePath))
        {
            return;
        }

        LWorkFix lPlan = LFixClinic.LClinicPlanRead();
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
        LFixClinic.LClinicSaveSuspend();
        try
        {
            LWorkFix? lSaved = LFix.LFixPlanRead(lSourcePath, LLibrarian.LLibrarianFixLoad);
            LWorkFix lPersistent = LFix.LFixPersistentResolve(LFixClinic.LClinicPlanRead());
            LFixClinic.LClinicPlanApply(LFix.LFixPlanResolve(lSaved, lPersistent));
        }
        finally
        {
            LFixClinic.LClinicSaveResume();
        }

        LFixActiveUpdate();
    }

    public void LFixActiveUpdate()
    {
        LWorkFix lPlan = LFixClinic.LClinicPlanRead();
        foreach (LFixRow lRow in LFixKinds)
        {
            bool lActive = lPlan.LWorkFixSteps.Any(
                lStep => lStep.LWorkFixKind == lRow.LFixRowKind && lStep.LWorkFixRepair);
            LFixProcessing.LProcessingActiveSet(lRow.LFixRowStep.LProcessingRowKey, lActive);
        }

        LFixProcessing.LProcessingActiveSet(LFixSalvageKey, lPlan.LWorkFixSalvage.LWorkSalvageActive);
    }

    private void LFixPersistentSave(IEnumerable<string> lPaths)
    {
        if (LFixClinic.LClinicSaveSuspended)
        {
            return;
        }

        LWorkFix lPersistent = LFix.LFixPersistentResolve(LFixClinic.LClinicPlanRead());
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
