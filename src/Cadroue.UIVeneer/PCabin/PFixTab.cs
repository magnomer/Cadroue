using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;
using Cadroue.ShellEngine;

namespace Cadroue.UIVeneer.PCabin;

public sealed class PFixTab : PTabSurface
{
    private sealed record PFixStep(
        LFlawKind PFixStepKind,
        string PFixStepName,
        string PFixStepIcon,
        string PFixStepLabel);

    private static readonly PFixStep[] pFixSteps =
    {
        new(
            LFlawKind.LFlawKindTruncation,
            "Truncation",
            "/PAsset/PPanel/PProcessingFixTruncation.svg",
            "Processing.Step.Truncation"),
        new(LFlawKind.LFlawKindIndex, "Index", "/PAsset/PPanel/PProcessingFixIndex.svg", "Processing.Step.Index"),
        new(
            LFlawKind.LFlawKindContainer,
            "Container",
            "/PAsset/PPanel/PProcessingFixContainer.svg",
            "Processing.Step.Container"),
        new(LFlawKind.LFlawKindTiming, "Timing", "/PAsset/PPanel/PProcessingFixTiming.svg", "Processing.Step.Timing"),
        new(
            LFlawKind.LFlawKindMetadata,
            "Metadata",
            "/PAsset/PPanel/PProcessingFixMetadata.svg",
            "Processing.Step.Metadata"),
        new(LFlawKind.LFlawKindCoded, "Coded", "/PAsset/PPanel/PProcessingFixCoded.svg", "Processing.Step.Coded"),
        new(
            LFlawKind.LFlawKindFraming,
            "Framing",
            "/PAsset/PPanel/PProcessingFixFraming.svg",
            "Processing.Step.Framing"),
        new(
            LFlawKind.LFlawKindConfig,
            "Config",
            "/PAsset/PPanel/PProcessingFixConfiguration.svg",
            "Processing.Step.Config"),
        new(
            LFlawKind.LFlawKindTransport,
            "Transport",
            "/PAsset/PPanel/PProcessingFixTransport.svg",
            "Processing.Step.Transport"),
        new(
            LFlawKind.LFlawKindSecondary,
            "Secondary",
            "/PAsset/PPanel/PProcessingFixSecondary.svg",
            "Processing.Step.Secondary"),
        new(LFlawKind.LFlawKindFfvone, "Ffvone", "/PAsset/PPanel/PProcessingFixFfvone.svg", "Processing.Step.Ffvone")
    };

    private readonly PFlow pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PClinic pClinic = new();
    private readonly PList pList = new(new LDocket());
    private readonly PProcessing pProcessing = new();
    private readonly LCheckup pFixCheckup = new();
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PFixTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += lPriority =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            LMessenger.LMessengerFixDescribe(
                lPriority,
                pList.PListEditableRead() is { } pFixSelected
                    ? new[] { new LWorkSource(pFixSelected.LDocketEntryPath, pFixSelected.LDocketEntryBatch) }
                    : Array.Empty<LWorkSource>(),
                lPresetOwner.LPresetSelectionEncoding,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionAllAdd += () =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            LMessenger.LMessengerFixDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner.LPresetSelectionEncoding,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionItemsAdd += pFixPaths =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            LMessenger.LMessengerFixDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Where(pItem => pFixPaths.Contains(pItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner.LPresetSelectionEncoding,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionListAttach(pList);
        pAction.PActionAllSet(
            true,
            LLocalization.LLocalizationTextRead("Action.EditAll.Tooltip"));

        pProcessing.PProcessingOrderedSet(false);
        foreach (PFixStep pFixStep in pFixSteps)
        {
            pProcessing.PProcessingStepAdd(pFixStep.PFixStepName, pFixStep.PFixStepIcon, pFixStep.PFixStepLabel);
        }

        pProcessing.PProcessingStepAdd(
            "Salvage", "/PAsset/PPanel/PProcessingFixSalvage.svg", "Processing.Step.Salvage");

        pProcessing.PProcessingStepChange += pClinic.PClinicStepShow;
        pClinic.PClinicPlanChange += PFixChangeHandle;
        pClinic.PClinicDiagnosisRun += PFixDiagnosisRun;
        pFixCheckup.LCheckupReady += PFixCheckupHandle;
        pFixCheckup.LCheckupProgress += PFixProgressHandle;

        pList.PListPathChange += PFixPathShow;
        pList.PListItemsAdd += PFixItemsHandle;
        pList.PListClearChange += PFixClearHandle;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => _ = pList.PListPathsAdd(pDropPaths);

        var pExport = new PExport(lPresetOwner, pExportSmartAllowed: true);
        PTabLockAttach(pList, pProcessing, pClinic, pExport);
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pProcessing, pClinic, pViewer, pExport },
            new PCompass(pFlow, pViewer),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        if (lPreferenceTabLayout is null)
        {
            pClinic.PClinicMinimizeSet(true);
        }

        Content = pTabGrid;
        PFixPersistentRestore(lPreferenceTabLayout);
        PFixActiveUpdate();
    }

    public override void PTabClose()
    {
        base.PTabClose();
        pFixCheckup.LCheckupReady -= PFixCheckupHandle;
        pFixCheckup.LCheckupProgress -= PFixProgressHandle;
        pFixCheckup.Dispose();
    }

    public override PFlow PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override LSceneTabRecord PTabLayoutRead()
    {
        LSceneTabRecord lPreferenceTabLayout = PTabLayoutRead(pTabGrid);
        LWorkFix pFixPlan = pClinic.PClinicPlanRead();
        LWorkFix pFixPersistent = LFix.LFixPersistentResolve(pFixPlan);
        bool pFixSalvagePersistent = pFixPlan.LWorkFixSalvage.LWorkSalvagePersistent;
        if (pFixPersistent.LWorkFixSteps.Any() || pFixSalvagePersistent)
        {
            lPreferenceTabLayout.LSceneInspector = new LSceneInspectorRecord
            {
                LSceneInspectorFix = LFix.LFixPersistentCreate(pFixPersistent),
                LSceneInspectorSalvage = pFixSalvagePersistent
            };
        }

        return lPreferenceTabLayout;
    }

    private void PFixPathShow(string? pSourcePath)
    {
        if (string.IsNullOrWhiteSpace(pSourcePath) || pViewer.LViewer.LViewerSourceMatch(pSourcePath))
        {
            return;
        }

        PFixPlanSave();
        pClinic.PClinicSourceSet(pSourcePath);
        pViewer.PViewerSourceOpen(pSourcePath);
        PFixPlanRestore(pSourcePath);
    }

    private void PFixDiagnosisRun()
    {
        if (pList.PListEditableRead() is not { } pFixSelected)
        {
            return;
        }

        LFlawKind[] pFixKinds = pFixSteps.Select(pFixStep => pFixStep.PFixStepKind).ToArray();
        pFixCheckup.LCheckupStart(new[] { pFixSelected.LDocketEntryPath }, pFixKinds, lCheckupForce: true);
    }

    private void PFixClearHandle(IReadOnlyList<string> pFixRemoved)
    {
        foreach (string pFixPath in pFixRemoved)
        {
            pFixCheckup.LCheckupSourceCancel(pFixPath);
        }

        pClinic.PClinicResultsRemove(pFixRemoved);
    }

    private void PFixCheckupHandle(LCheckupResult pFixResult)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (PFixListedCheck(pFixResult.LCheckupSource))
            {
                pClinic.PClinicResultShow(pFixResult.LCheckupSource, pFixResult.LCheckupKind, pFixResult);
            }
        });
    }

    private void PFixProgressHandle(string pFixPath, double pFixProgress)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (PFixListedCheck(pFixPath))
            {
                pClinic.PClinicProgressShow(pFixPath, pFixProgress);
            }
        });
    }

    private bool PFixListedCheck(string pFixPath) => pList.PListDocketRead().LDocketItemFind(pFixPath) is not null;

    private void PFixPlanRestore(string pSourcePath)
    {
        pClinic.LClinic.LClinicSaveSuspend();
        try
        {
            LWorkFix? pFixSaved = LFix.LFixPlanRead(pSourcePath, LLibrarian.LLibrarianFixLoad);
            LWorkFix pFixPersistent = LFix.LFixPersistentResolve(pClinic.PClinicPlanRead());
            LWorkFix pFixResolved = LFix.LFixPlanResolve(pFixSaved, pFixPersistent);
            pClinic.PClinicPlanApply(pFixResolved);
        }
        finally
        {
            pClinic.LClinic.LClinicSaveResume();
        }

        PFixActiveUpdate();
    }

    private void PFixPlanSave()
    {
        if (pClinic.LClinic.LClinicSaveSuspended
            || pViewer.PViewerSourcePath is not { } pSourcePath
            || pList.PListLockCheck(pSourcePath))
        {
            return;
        }

        LWorkFix pFixPlan = pClinic.PClinicPlanRead();
        if (!pFixPlan.LWorkFixActive && LFix.LFixPlanRead(pSourcePath, LLibrarian.LLibrarianFixLoad) is null)
        {
            return;
        }

        LFix.LFixPlanSave(pSourcePath, pFixPlan, LLibrarian.LLibrarianFixSave);
        PFixPersistentSave();
    }

    private void PFixChangeHandle()
    {
        PFixActiveUpdate();
        PFixPlanSave();
    }

    private void PFixPersistentSave()
    {
        if (pClinic.LClinic.LClinicSaveSuspended)
        {
            return;
        }

        LWorkFix pFixPersistent = LFix.LFixPersistentResolve(pClinic.PClinicPlanRead());
        if (!pFixPersistent.LWorkFixSteps.Any())
        {
            return;
        }

        foreach (string pFixPath in pList.PListUnlockedRead().Select(pItem => pItem.LDocketEntryPath))
        {
            LWorkFix? pFixFileSaved = LFix.LFixPlanRead(pFixPath, LLibrarian.LLibrarianFixLoad);
            LWorkFix pFixMerged = LFix.LFixPlanResolve(pFixFileSaved, pFixPersistent);
            LFix.LFixPlanSave(pFixPath, pFixMerged, LLibrarian.LLibrarianFixSave);
        }
    }

    private void PFixItemsHandle(IReadOnlyList<LDocketEntry> pFixAddedItems)
    {
        if (pClinic.LClinic.LClinicSaveSuspended)
        {
            return;
        }

        LWorkFix pFixPersistent = LFix.LFixPersistentResolve(pClinic.PClinicPlanRead());
        if (!pFixPersistent.LWorkFixSteps.Any())
        {
            return;
        }

        foreach (LDocketEntry pFixAddedItem in pFixAddedItems)
        {
            LWorkFix? pFixFileSaved = LFix.LFixPlanRead(pFixAddedItem.LDocketEntryPath, LLibrarian.LLibrarianFixLoad);
            LWorkFix pFixMerged = LFix.LFixPlanResolve(pFixFileSaved, pFixPersistent);
            LFix.LFixPlanSave(pFixAddedItem.LDocketEntryPath, pFixMerged, LLibrarian.LLibrarianFixSave);
        }
    }

    private void PFixPersistentRestore(LSceneTabRecord? lPreferenceTabLayout)
    {
        if (lPreferenceTabLayout?.LSceneInspector is not
            { LSceneInspectorFix: { } pFixPersistentRecord } pFixInspector)
        {
            return;
        }

        pClinic.LClinic.LClinicSaveSuspend();
        try
        {
            LWorkFix pFixPersistentPlan = LFix.LFixPersistentRead(
                pFixPersistentRecord, pFixInspector.LSceneInspectorSalvage);
            pClinic.PClinicPlanApply(pFixPersistentPlan);
        }
        finally
        {
            pClinic.LClinic.LClinicSaveResume();
        }
    }

    private void PFixActiveUpdate()
    {
        LWorkFix pFixPlan = pClinic.PClinicPlanRead();
        foreach (PFixStep pFixStep in pFixSteps)
        {
            bool pFixActive = pFixPlan.LWorkFixSteps.Any(
                pStep => pStep.LWorkFixKind == pFixStep.PFixStepKind && pStep.LWorkFixRepair);
            pProcessing.PProcessingActiveSet(pFixStep.PFixStepName, pFixActive);
        }

        pProcessing.PProcessingActiveSet("Salvage", pFixPlan.LWorkFixSalvage.LWorkSalvageActive);
    }
}
