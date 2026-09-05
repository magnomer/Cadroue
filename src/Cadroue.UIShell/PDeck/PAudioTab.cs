using Cadroue.Core;
using Cadroue.UIShell.PPanel;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;
using Cadroue.Media;

namespace Cadroue.UIShell.PDeck;

public sealed class PAudioTab : PTabSurface
{
    private const string PAudioVolumeIcon = "/PAsset/PPanel/PProcessingVolume.svg";
    private const string PAudioNormalizeIcon = "/PAsset/PPanel/PProcessingNormalize.svg";
    private const string PAudioNoiseIcon = "/PAsset/PPanel/PProcessingNoiseReduction.svg";
    private const string PAudioHighIcon = "/PAsset/PPanel/PProcessingHighPass.svg";
    private const string PAudioLowIcon = "/PAsset/PPanel/PProcessingLowPass.svg";
    private const string PAudioEqualizerIcon = "/PAsset/PPanel/PProcessingEqualizer.svg";

    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new() { PViewerAudioEligible = true };
    private readonly PList pList = new(new LDocket());
    private readonly PProcessing pProcessing = new();
    private readonly PInspector pInspector = new();
    private readonly LSMonitor pAudioMonitor = new();
    private readonly System.Windows.Controls.Grid pTabGrid;
    private bool pAudioPlanLoading;
    private string? pAudioOwnerPath;
    private int pAudioOwnerRate;
    private string? pAudioSaveFailure;
    private System.Windows.Threading.DispatcherTimer? pAudioViewerTimer;

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
        pProcessing.PProcessingMonitorShow += PAudioMonitorShow;
        pProcessing.PProcessingMonitorSet();
        pInspector.PSkipActiveChange += PAudioSkipHandle;
        pInspector.PInspectorPlanChange += PAudioPersistentSave;
        pInspector.PInspectorAudioChange += PAudioChangeHandle;

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
        pAction.PActionSelectionSource = () => pList.PListSelectionRead();
        pAction.PActionAllSet(
            true,
            LLocalization.LLocalizationTextRead("Action.AudioAll.Tooltip"));
        pList.PListPathChange += PAudioPathShow;
        pList.PListItemsAdd += PAudioItemsHandle;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PViewerMediaChange += PAudioMediaHandle;
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
        if (lPreferenceTabLayout?.LSceneInspector is not { LSceneInspectorAudio: { } pAudioPersistentRecord } pAudioInspector)
        {
            return;
        }

        pAudioPlanLoading = true;
        try
        {
            LWorkAudio pAudioPersistentPlan = LAudio.LAudioPersistentRead(pAudioPersistentRecord);
            pInspector.PInspectorPlanApply(pAudioPersistentPlan);
            pInspector.PInspectorPersistentApply(pAudioPersistentPlan, pAudioInspector.LSceneInspectorSkip);
            pInspector.PSkipApply(pAudioPersistentPlan.LWorkAudioSkip);
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

    private void PAudioPersistentSave()
    {
        if (pAudioPlanLoading || !pInspector.PInspectorPersistentCheck())
        {
            return;
        }

        PAudioFanoutSave(pList.PListUnlockedRead().Select(pItem => pItem.LDocketEntryPath));
    }

    private void PAudioFanoutSave(IEnumerable<string> pAudioPaths)
    {
        LWorkAudio pAudioPersistent = pInspector.PInspectorPersistentRead();
        bool pAudioSkipPersistent = pInspector.PSkipPersistentCheck();
        bool pAudioSkipApply = pInspector.PSkipActiveCheck();
        var pAudioFailed = new List<string>();
        foreach (string pAudioPath in pAudioPaths)
        {
            bool pAudioStored = LAudio.LAudioPlanSave(
                pAudioPath,
                LAudio.LAudioPlanResolve(
                    LAudio.LAudioPlanRead(pAudioPath, LLibrarian.LLibrarianAudioLoad),
                    pAudioPersistent, pAudioSkipPersistent, pAudioSkipApply),
                LLibrarian.LLibrarianAudioSave);
            if (!pAudioStored)
            {
                pAudioFailed.Add(pAudioPath);
            }
        }

        if (pAudioFailed.Count > 0)
        {
            LTraceLog.LTraceWarningRecord(
                $"Audio persistent save failed for {pAudioFailed.Count} file(s): those sidecars were not written",
                string.Join(Environment.NewLine, pAudioFailed));
        }
    }

    private void PAudioItemsHandle(IReadOnlyList<LDocketEntry> pAudioAddedItems)
    {
        if (pAudioPlanLoading || !pInspector.PInspectorPersistentCheck())
        {
            return;
        }

        PAudioFanoutSave(pAudioAddedItems.Select(pAudioAddedItem => pAudioAddedItem.LDocketEntryPath));
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
        PAudioViewerDefer();
    }

    private void PAudioViewerDefer()
    {
        if (pAudioViewerTimer is null)
        {
            pAudioViewerTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            pAudioViewerTimer.Tick += PAudioViewerHandle;
        }

        pAudioViewerTimer.Stop();
        pAudioViewerTimer.Start();
    }

    private void PAudioViewerHandle(object? pSender, EventArgs pArgs)
    {
        pAudioViewerTimer?.Stop();
        PAudioViewerApply();
    }

    private void PAudioViewerApply()
    {
        LWorkAudio pAudioPlan = PAudioProcessingRead();
        pViewer.PViewerAudioSet(
            pAudioPlan.LWorkAudioSkip ? string.Empty : pAudioPlan.LWorkAudioFormat(pAudioOwnerRate));
    }

    private void PAudioMonitorShow() =>
        PSMonitor.PSMonitorShow(System.Windows.Window.GetWindow(this), pAudioMonitor, pFlow, pViewer);

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
        }
    }

    private void PAudioMediaHandle(LCargo pMediaStatus)
    {
        bool pAudioOwnerFirst = pAudioOwnerPath is null;
        pAudioOwnerPath = pMediaStatus.LCargoSourcePath;
        pAudioOwnerRate = pMediaStatus.LCargoMediaInfo?.LMediaSampleRate ?? 0;
        PAudioPlanRestore(pMediaStatus.LCargoSourcePath, pAudioOwnerFirst);
        pAudioMonitor.LSMonitorSourceOpen(
            pMediaStatus.LCargoSourcePath,
            pMediaStatus.LCargoMediaInfo?.LMediaInfoDuration ?? TimeSpan.Zero,
            pAudioOwnerRate);
        pAudioMonitor.LSMonitorPlanApply(PAudioProcessingRead());
    }

    private void PAudioPlanRestore(string pSourcePath, bool pAudioOwnerFirst)
    {
        bool pAudioAdopted = false;
        pAudioPlanLoading = true;
        try
        {
            LWorkAudio? pSaved = LAudio.LAudioPlanRead(pSourcePath, LLibrarian.LLibrarianAudioLoad);
            LWorkAudio? pPersistent = pInspector.PInspectorPersistentCheck()
                ? pInspector.PInspectorPersistentRead()
                : null;
            if (pSaved is null && pPersistent is null && pAudioOwnerFirst
                && PAudioProcessingRead() is { LWorkAudioActive: true } pAudioPending)
            {
                pSaved = pAudioPending;
                pAudioAdopted = true;
            }

            LWorkAudio pResolved = LAudio.LAudioPlanResolve(
                pSaved, pPersistent, pInspector.PSkipPersistentCheck(), pInspector.PSkipActiveCheck());
            pInspector.PInspectorPlanApply(pResolved);
            pInspector.PSkipApply(pResolved.LWorkAudioSkip);
        }
        finally
        {
            pAudioPlanLoading = false;
        }

        pProcessing.PProcessingSkipSet(pInspector.PSkipActiveCheck());
        PAudioActiveUpdate();
        PAudioViewerApply();
        if (pAudioAdopted)
        {
            PAudioPlanSave();
        }
    }

    private void PAudioPlanSave()
    {
        if (pAudioPlanLoading
            || pAudioOwnerPath is not { } pSourcePath
            || pList.PListLockCheck(pSourcePath))
        {
            return;
        }

        LWorkAudio pAudioPlan = PAudioProcessingRead();
        if (!pAudioPlan.LWorkAudioActive && LAudio.LAudioPlanRead(pSourcePath, LLibrarian.LLibrarianAudioLoad) is null)
        {
            return;
        }

        if (LAudio.LAudioPlanSave(pSourcePath, pAudioPlan, LLibrarian.LLibrarianAudioSave))
        {
            pAudioSaveFailure = null;
        }
        else if (!string.Equals(pAudioSaveFailure, pSourcePath, StringComparison.OrdinalIgnoreCase))
        {
            pAudioSaveFailure = pSourcePath;
            LTraceLog.LTraceWarningRecord(
                $"Audio edit not saved for '{System.IO.Path.GetFileName(pSourcePath)}': the sidecar could not be written",
                pSourcePath);
        }

        PAudioPersistentSave();
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
                LSceneInspectorAudio = LAudio.LAudioPersistentCreate(pInspector.PInspectorPersistentRead()),
                LSceneInspectorSkip = pInspector.PSkipPersistentCheck()
            };
        }

        return lPreferenceTabLayout;
    }
}
