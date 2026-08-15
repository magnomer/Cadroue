using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;
using Cadroue.Media;

namespace Cadroue.UIShell.PMainArea;

public sealed class PAudioTab : PTabSurface
{
    private const string PAudioVolumeIcon = "/PAssets/PPanels/PProcessingVolume.svg";
    private const string PAudioNormalizeIcon = "/PAssets/PPanels/PProcessingNormalize.svg";
    private const string PAudioNoiseIcon = "/PAssets/PPanels/PProcessingNoiseReduction.svg";
    private const string PAudioHighIcon = "/PAssets/PPanels/PProcessingHighPass.svg";
    private const string PAudioLowIcon = "/PAssets/PPanels/PProcessingLowPass.svg";
    private const string PAudioEqualizerIcon = "/PAssets/PPanels/PProcessingEqualizer.svg";

    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PList pList = new(new LDocket());
    private readonly PProcessing pProcessing = new();
    private readonly PInspector pInspector = new();
    private readonly LSMonitor pAudioMonitor = new();
    private readonly System.Windows.Controls.Grid pTabGrid;
    private bool pAudioPlanLoading;

    public PAudioTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        pProcessing.PProcessingOrderedSet(true);
        pProcessing.PProcessingStepAdd("High Pass", PAudioHighIcon, "Processing.Step.HighPass");
        pProcessing.PProcessingStepAdd("Low Pass", PAudioLowIcon, "Processing.Step.LowPass");
        pProcessing.PProcessingStepAdd("Noise Reduction", PAudioNoiseIcon, "Processing.Step.NoiseReduction");
        pProcessing.PProcessingStepAdd("Equalizer", PAudioEqualizerIcon, "Processing.Step.Equalizer");
        pProcessing.PProcessingStepAdd("Volume", PAudioVolumeIcon, "Processing.Step.Volume");
        pProcessing.PProcessingStepAdd("Normalize", PAudioNormalizeIcon, "Processing.Step.Normalize");
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepChange += PAudioStepHandle;
        pProcessing.PProcessingStepOpen += _ => pInspector.PInspectorMinimizeSet(false);
        pProcessing.PProcessingOrderChange += PAudioPlanSave;
        pInspector.PSkipActiveChange += PAudioSkipHandle;
        pInspector.PInspectorPlanChange += PAudioPersistentWrite;
        pInspector.PInspectorAudioChange += PAudioChangeHandle;
        pInspector.PInspectorMonitorShow += PAudioMonitorShow;

        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += lPriority =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            if (pList.PListEditableRead() is not { } pAudioSelected)
            {
                return;
            }

            PAudioPlanSave();
            _ = LMessenger.LMessengerAudioDescribe(
                lPriority,
                pAudioSelected.LDocketEntryPath,
                PAudioProcessingRead(),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab,
                pAudioSelected.LDocketEntryBatch);
        };
        pAction.PActionAllAdd += () =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            PAudioPlanSave();
            _ = LMessenger.LMessengerAudioDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionItemsAdd += pAudioPaths =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            PAudioPlanSave();
            _ = LMessenger.LMessengerAudioDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Where(pItem => pAudioPaths.Contains(pItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionAllSet(
            true,
            LLocalization.LLocalizationTextRead("Action.AudioAll.Tooltip"));
        pList.PListPathChange += PAudioPathShow;
        pList.PListItemsAdd += PAudioItemsHandle;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);
        var pExport = new PExport(lPresetOwner);
        PTabLockAttach(pList, pProcessing, pInspector, pExport);
        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pProcessing, pInspector, pViewer, pExport }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
        Content = pTabGrid;
        PAudioPersistentRestore(lPreferenceTabLayout);
        PAudioActiveUpdate();
    }

    private void PAudioPersistentRestore(LSceneTabRecord? lPreferenceTabLayout)
    {
        if (lPreferenceTabLayout?.LSceneInspector?.LSceneInspectorAudio is not { } pAudioPersistentRecord)
        {
            return;
        }

        pAudioPlanLoading = true;
        try
        {
            LWorkAudio pAudioPersistentPlan = LAudio.LAudioPersistentRead(pAudioPersistentRecord);
            pInspector.PInspectorPlanApply(pAudioPersistentPlan);
            pInspector.PInspectorPersistentApply(pAudioPersistentPlan);
        }
        finally
        {
            pAudioPlanLoading = false;
        }
    }

    private void PAudioSkipHandle()
    {
        pProcessing.PProcessingSkipSet(pInspector.PSkipActiveCheck());
        PAudioPlanSave();
    }

    private void PAudioPersistentWrite()
    {
        if (pAudioPlanLoading || !pInspector.PInspectorPersistentCheck())
        {
            return;
        }

        LWorkAudio pAudioPersistent = pInspector.PInspectorPersistentRead();
        foreach (string pAudioPath in pList.PListUnlockedRead().Select(pItem => pItem.LDocketEntryPath))
        {
            LAudio.LAudioPlanSave(
                pAudioPath,
                LAudio.LAudioPlanResolve(LAudio.LAudioPlanRead(pAudioPath, LLibrarian.LLibrarianAudioLoad), pAudioPersistent),
                LLibrarian.LLibrarianAudioSave);
        }
    }

    private void PAudioItemsHandle(IReadOnlyList<LDocketEntry> pAudioAddedItems)
    {
        if (pAudioPlanLoading || !pInspector.PInspectorPersistentCheck())
        {
            return;
        }

        LWorkAudio pAudioPersistent = pInspector.PInspectorPersistentRead();
        foreach (LDocketEntry pAudioAddedItem in pAudioAddedItems)
        {
            string pAudioPath = pAudioAddedItem.LDocketEntryPath;
            LAudio.LAudioPlanSave(
                pAudioPath,
                LAudio.LAudioPlanResolve(LAudio.LAudioPlanRead(pAudioPath, LLibrarian.LLibrarianAudioLoad), pAudioPersistent),
                LLibrarian.LLibrarianAudioSave);
        }
    }

    private void PAudioStepHandle(string? pStepName)
    {
        if (string.IsNullOrEmpty(pStepName) || pStepName == "No Processing")
        {
            return;
        }

        if (pInspector.PSkipPersistentCheck())
        {
            pInspector.PSkipPersistentApply(false);
        }

        if (pInspector.PSkipActiveCheck())
        {
            pInspector.PSkipApply(false);
        }
    }

    private void PAudioActiveUpdate()
    {
        foreach (string pStepName in pProcessing.PProcessingStepsRead())
        {
            if (PAudioKindRead(pStepName) is LAudioKind pStepKind)
            {
                pProcessing.PProcessingActiveSet(pStepName, pInspector.PInspectorStepRead(pStepKind).LWorkStepActive);
            }
        }
    }

    private void PAudioChangeHandle()
    {
        PAudioActiveUpdate();
        PAudioPlanSave();
        pAudioMonitor.LSMonitorPlanApply(PAudioProcessingRead());
    }

    private void PAudioMonitorShow() =>
        PSMonitor.PSMonitorShow(System.Windows.Window.GetWindow(this), pAudioMonitor);

    private LWorkAudio PAudioProcessingRead()
    {
        var pSteps = new List<LWorkAudioStep>();
        foreach (string pStepName in pProcessing.PProcessingStepsRead())
        {
            if (PAudioKindRead(pStepName) is LAudioKind pStepKind)
            {
                pSteps.Add(pInspector.PInspectorStepRead(pStepKind));
            }
        }

        return new LWorkAudio(pSteps) { LWorkAudioSkip = pInspector.PSkipActiveCheck() };
    }

    private static LAudioKind? PAudioKindRead(string pStepName) => pStepName switch
    {
        "Volume" => LAudioKind.LAudioKindVolume,
        "Normalize" => LAudioKind.LAudioKindLeveling,
        "Noise Reduction" => LAudioKind.LAudioKindDenoise,
        "High Pass" => LAudioKind.LAudioKindHighpass,
        "Low Pass" => LAudioKind.LAudioKindLowpass,
        "Equalizer" => LAudioKind.LAudioKindEqualizer,
        _ => null
    };

    private void PAudioPathShow(string? pSourcePath)
    {
        if (!string.IsNullOrWhiteSpace(pSourcePath))
        {
            PAudioPlanSave();
            pViewer.PViewerSourceOpen(pSourcePath);
            PAudioPlanRestore();
        }
    }

    private void PAudioPlanRestore()
    {
        pAudioPlanLoading = true;
        try
        {
            LWorkAudio? pSaved = pViewer.PViewerSourcePath is { } pSourcePath
                ? LAudio.LAudioPlanRead(pSourcePath, LLibrarian.LLibrarianAudioLoad)
                : null;
            LWorkAudio? pPersistent = pInspector.PInspectorPersistentCheck()
                ? pInspector.PInspectorPersistentRead()
                : null;
            LWorkAudio pResolved = LAudio.LAudioPlanResolve(pSaved, pPersistent);
            pInspector.PInspectorPlanApply(pResolved);
            pInspector.PSkipApply(pResolved.LWorkAudioSkip);
        }
        finally
        {
            pAudioPlanLoading = false;
        }

        pProcessing.PProcessingSkipSet(pInspector.PSkipActiveCheck());
        PAudioActiveUpdate();
        pAudioMonitor.LSMonitorSourceOpen(pViewer.PViewerSourcePath, pViewer.PViewerDurationRead());
        pAudioMonitor.LSMonitorPlanApply(PAudioProcessingRead());
    }

    private void PAudioPlanSave()
    {
        if (pAudioPlanLoading
            || pViewer.PViewerSourcePath is not { } pSourcePath
            || pList.PListLockCheck(pSourcePath))
        {
            return;
        }

        LWorkAudio pAudioPlan = PAudioProcessingRead();
        if (!pAudioPlan.LWorkAudioActive && LAudio.LAudioPlanRead(pSourcePath, LLibrarian.LLibrarianAudioLoad) is null)
        {
            return;
        }

        LAudio.LAudioPlanSave(pSourcePath, pAudioPlan, LLibrarian.LLibrarianAudioSave);
        PAudioPersistentWrite();
    }

    public override void PTabClose()
    {
        base.PTabClose();
        pAudioMonitor.Dispose();
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override LSceneTabRecord PTabLayoutRead()
    {
        LSceneTabRecord lPreferenceTabLayout = PTabLayoutRead(pTabGrid);
        if (pInspector.PInspectorPersistentCheck())
        {
            lPreferenceTabLayout.LSceneInspector = new LSceneInspectorRecord
            {
                LSceneInspectorAudio = LAudio.LAudioPersistentCreate(pInspector.PInspectorPersistentRead())
            };
        }

        return lPreferenceTabLayout;
    }
}
