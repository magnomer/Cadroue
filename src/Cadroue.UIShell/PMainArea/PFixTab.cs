using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PMainArea;

public sealed class PFixTab : PTabSurface
{
    // Presentation order is by real-world defect frequency (most common first) so the
    // defect a user most likely faces is nearest the top. It deliberately differs from the
    // actual repair order, which LRemedy fixes by safety and dependency (lossless carriage
    // repairs first, lossy decode-reencode last); list position never decides repair semantics.
    private static readonly (LFlawKind Kind, string Name, string Icon, string LabelKey)[] pFixSteps =
    {
        (LFlawKind.LFlawKindTruncation, "Truncation", "/PAssets/PPanels/PProcessingFixTruncation.svg", "Processing.Step.Truncation"),
        (LFlawKind.LFlawKindIndex, "Index", "/PAssets/PPanels/PProcessingFixIndex.svg", "Processing.Step.Index"),
        (LFlawKind.LFlawKindContainer, "Container", "/PAssets/PPanels/PProcessingFixContainer.svg", "Processing.Step.Container"),
        (LFlawKind.LFlawKindTiming, "Timing", "/PAssets/PPanels/PProcessingFixTiming.svg", "Processing.Step.Timing"),
        (LFlawKind.LFlawKindMetadata, "Metadata", "/PAssets/PPanels/PProcessingFixMetadata.svg", "Processing.Step.Metadata"),
        (LFlawKind.LFlawKindCoded, "Coded", "/PAssets/PPanels/PProcessingFixCoded.svg", "Processing.Step.Coded"),
        (LFlawKind.LFlawKindFraming, "Framing", "/PAssets/PPanels/PProcessingFixFraming.svg", "Processing.Step.Framing"),
        (LFlawKind.LFlawKindConfig, "Config", "/PAssets/PPanels/PProcessingFixConfiguration.svg", "Processing.Step.Config"),
        (LFlawKind.LFlawKindTransport, "Transport", "/PAssets/PPanels/PProcessingFixTransport.svg", "Processing.Step.Transport"),
        (LFlawKind.LFlawKindSecondary, "Secondary", "/PAssets/PPanels/PProcessingFixSecondary.svg", "Processing.Step.Secondary"),
        (LFlawKind.LFlawKindFfvone, "Ffvone", "/PAssets/PPanels/PProcessingFixFfvone.svg", "Processing.Step.Ffvone")
    };

    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PClinic pClinic = new();
    private readonly PList pList = new(new LDocket());
    private readonly PProcessing pProcessing = new();
    private readonly LCheckup pFixCheckup = new();
    private readonly System.Windows.Controls.Grid pTabGrid;
    private bool pFixPlanLoading;

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

            _ = LMessenger.LMessengerFixDescribe(
                lPriority,
                pList.PListEditableRead() is { } pFixSelected
                    ? new[] { new LWorkSource(pFixSelected.LDocketEntryPath, pFixSelected.LDocketEntryBatch) }
                    : Array.Empty<LWorkSource>(),
                lPresetOwner,
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

            _ = LMessenger.LMessengerFixDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner,
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

            _ = LMessenger.LMessengerFixDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Where(pItem => pFixPaths.Contains(pItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionSelectionSource = () => pList.PListSelectionRead();
        pAction.PActionAllSet(
            true,
            LLocalization.LLocalizationTextRead("Action.EditAll.Tooltip"));

        pProcessing.PProcessingOrderedSet(false);
        foreach ((LFlawKind _, string pFixName, string pFixIcon, string pFixLabelKey) in pFixSteps)
        {
            pProcessing.PProcessingStepAdd(pFixName, pFixIcon, pFixLabelKey);
        }

        pProcessing.PProcessingStepAdd(
            "Salvage", "/PAssets/PPanels/PProcessingFixSalvage.svg", "Processing.Step.Salvage");

        pProcessing.PProcessingStepChange += pClinic.PClinicStepShow;
        pClinic.PClinicPlanChange += PFixPlanSave;
        pClinic.PClinicDiagnosisRequest += PFixDiagnosisHandle;
        pFixCheckup.LCheckupReady += PFixCheckupHandle;

        pList.PListPathChange += PFixPathShow;
        pList.PListItemsAdd += PFixItemsHandle;
        pList.PListClearChange += pClinic.PClinicResultsRemove;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);

        var pExport = new PExport(lPresetOwner, pExportSmartAllowed: true);
        PTabLockAttach(pList, pProcessing, pClinic, pExport);
        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pProcessing, pClinic, pViewer, pExport }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
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
        pFixCheckup.Dispose();
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override LSceneTabRecord PTabLayoutRead()
    {
        LSceneTabRecord lPreferenceTabLayout = PTabLayoutRead(pTabGrid);
        LWorkFix pFixPersistent = LFix.LFixPersistentResolve(pClinic.PClinicPlanRead());
        if (pFixPersistent.LWorkFixSteps.Any())
        {
            lPreferenceTabLayout.LSceneInspector = new LSceneInspectorRecord
            {
                LSceneInspectorFix = LFix.LFixPersistentCreate(pFixPersistent)
            };
        }

        return lPreferenceTabLayout;
    }

    private void PFixPathShow(string? pSourcePath)
    {
        if (!string.IsNullOrWhiteSpace(pSourcePath))
        {
            PFixPlanSave();
            pClinic.PClinicSourceSet(pSourcePath);
            pViewer.PViewerSourceOpen(pSourcePath);
            PFixPlanRestore(pSourcePath);
        }
    }

    private void PFixDiagnosisHandle(LFlawKind pFixKind)
    {
        if (pList.PListEditableRead() is not { } pFixSelected)
        {
            return;
        }

        pFixCheckup.LCheckupStart(new[] { pFixSelected.LDocketEntryPath }, new[] { pFixKind });
    }

    private void PFixPersistentStart(IEnumerable<string> pFixPaths)
    {
        LFlawKind[] pFixKinds = LFix.LFixPersistentResolve(pClinic.PClinicPlanRead()).LWorkFixSteps
            .Where(pStep => pStep.LWorkFixDiagnosis)
            .Select(pStep => pStep.LWorkFixKind)
            .ToArray();
        if (pFixKinds.Length == 0)
        {
            return;
        }

        string[] pFixSources = pFixPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (pFixSources.Length == 0)
        {
            return;
        }

        pFixCheckup.LCheckupStart(pFixSources, pFixKinds);
    }

    private void PFixCheckupHandle(LCheckupResult pFixResult)
    {
        Dispatcher.BeginInvoke(() =>
            pClinic.PClinicResultShow(pFixResult.LCheckupSource, pFixResult.LCheckupKind, pFixResult));
    }

    private void PFixPlanRestore(string pSourcePath)
    {
        pFixPlanLoading = true;
        try
        {
            LWorkFix? pFixSaved = LFix.LFixPlanRead(pSourcePath, LLibrarian.LLibrarianFixLoad);
            LWorkFix pFixPersistent = LFix.LFixPersistentResolve(pClinic.PClinicPlanRead());
            LWorkFix pFixResolved = LFix.LFixPlanResolve(pFixSaved, pFixPersistent);
            pClinic.PClinicPlanApply(pFixResolved);
        }
        finally
        {
            pFixPlanLoading = false;
        }

        PFixActiveUpdate();
    }

    private void PFixPlanSave()
    {
        if (pFixPlanLoading
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
        PFixActiveUpdate();
    }

    private void PFixPersistentSave()
    {
        if (pFixPlanLoading)
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

        PFixPersistentStart(pList.PListUnlockedRead().Select(pItem => pItem.LDocketEntryPath));
    }

    private void PFixItemsHandle(IReadOnlyList<LDocketEntry> pFixAddedItems)
    {
        if (pFixPlanLoading)
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

        PFixPersistentStart(pFixAddedItems.Select(pItem => pItem.LDocketEntryPath));
    }

    private void PFixPersistentRestore(LSceneTabRecord? lPreferenceTabLayout)
    {
        if (lPreferenceTabLayout?.LSceneInspector is not { LSceneInspectorFix: { } pFixPersistentRecord })
        {
            return;
        }

        pFixPlanLoading = true;
        try
        {
            LWorkFix pFixPersistentPlan = LFix.LFixPersistentRead(pFixPersistentRecord);
            pClinic.PClinicPlanApply(pFixPersistentPlan);
        }
        finally
        {
            pFixPlanLoading = false;
        }

        PFixPersistentStart(pList.PListUnlockedRead().Select(pItem => pItem.LDocketEntryPath));
    }

    private void PFixActiveUpdate()
    {
        LWorkFix pFixPlan = pClinic.PClinicPlanRead();
        foreach ((LFlawKind pFixKind, string pFixName, string _, string _) in pFixSteps)
        {
            bool pFixActive = pFixPlan.LWorkFixSteps.Any(
                pStep => pStep.LWorkFixKind == pFixKind && (pStep.LWorkFixRepair || pStep.LWorkFixDiagnosis));
            pProcessing.PProcessingActiveSet(pFixName, pFixActive);
        }

        pProcessing.PProcessingActiveSet("Salvage", pFixPlan.LWorkFixSalvage.LWorkSalvageActive);
    }
}
