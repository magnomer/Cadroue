using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PMainArea;

public sealed class PSplitTab : PTabSurface
{
    private const string PSplitBlankIcon = "/PAssets/PPanels/PProcessingBlank.svg";
    private const string PSplitSceneIcon = "/PAssets/PPanels/PProcessingScene.svg";
    private const string PSplitStillIcon = "/PAssets/PPanels/PProcessingStill.svg";
    private const string PSplitLuminanceIcon = "/PAssets/PPanels/PProcessingLuminance.svg";
    private const string PSplitSilenceIcon = "/PAssets/PPanels/PProcessingSilence.svg";
    private const string PSplitVolumeIcon = "/PAssets/PPanels/PProcessingReducedVolume.svg";

    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PSection pSection = new();
    private readonly PList pList = new(new LDocket());
    private readonly PProcessing pProcessing = new();
    private readonly PInspector pInspector = new();
    private readonly System.Windows.Controls.Grid pTabGrid;
    private System.Threading.CancellationTokenSource? pSplitSweepSource;
    private bool pSplitDetectorLoading;

    public PSplitTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
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

            if (pList.PListEditableRead() is not { } pSplitSelected)
            {
                return;
            }

            LMessenger.LMessengerSplitDescribe(
                lPriority,
                pSplitSelected.LDocketEntryPath,
                pFlow.PFlowSplitRead(),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab,
                pSplitSelected.LDocketEntryBatch);
        };
        pAction.PActionAllAdd += () =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            _ = LMessenger.LMessengerSplitDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionItemsAdd += pSplitPaths =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            _ = LMessenger.LMessengerSplitDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Where(pItem => pSplitPaths.Contains(pItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionSelectionSource = () => pList.PListSelectionRead();
        pAction.PActionAllSet(true, LLocalization.LLocalizationTextRead("Action.AddAll.SplitTooltip"));

        pProcessing.PProcessingStepAdd(
            PInspector.PSensorNameRead(LDetectorKind.LDetectorKindBlank), PSplitBlankIcon, "Processing.Step.Blank");
        pProcessing.PProcessingStepAdd(
            PInspector.PSensorNameRead(LDetectorKind.LDetectorKindScene), PSplitSceneIcon, "Processing.Step.Scene");
        pProcessing.PProcessingStepAdd(
            PInspector.PSensorNameRead(LDetectorKind.LDetectorKindStill), PSplitStillIcon, "Processing.Step.Still");
        pProcessing.PProcessingStepAdd(
            PInspector.PSensorNameRead(LDetectorKind.LDetectorKindLuminance), PSplitLuminanceIcon, "Processing.Step.Luminance");
        pProcessing.PProcessingStepAdd(
            PInspector.PSensorNameRead(LDetectorKind.LDetectorKindSilence), PSplitSilenceIcon, "Processing.Step.Silence");
        pProcessing.PProcessingStepAdd(
            PInspector.PSensorNameRead(LDetectorKind.LDetectorKindVolume), PSplitVolumeIcon, "Processing.Step.Volume");
        pProcessing.PProcessingStepChange += pInspector.PInspectorStepShow;
        pProcessing.PProcessingStepOpen += _ => pInspector.PInspectorMinimizeSet(false);
        pInspector.PSensorChange += PSplitActiveUpdate;
        pInspector.PSensorChange += PSplitDetectorSave;

        pInspector.PSensorRunShow();
        pInspector.PSensorRun += PSplitSweepRun;
        pInspector.PSensorStop += () => pSplitSweepSource?.Cancel();
        pInspector.PSensorPersistentChange += PSplitPersistentHandle;
        pInspector.PBlankPickChange += pArmed =>
            pViewer.PViewerNeutralSet(pArmed, LNeutralTarget.LNeutralTargetGrey);
        pViewer.PViewerNeutralChange += pSample =>
            pInspector.PBlankSampleApply(pSample.LNeutralRed, pSample.LNeutralGreen, pSample.LNeutralBlue);

        pFlow.PFlowSectionShow(true);
        pSection.PSectionAttach(pFlow);
        pList.PListPathChange += PSplitPathShow;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);
        var pExport = new PExport(lPresetOwner, pExportSmartAllowed: true);
        PTabLockAttach(pList, pSection, pProcessing, pInspector, pExport);
        pList.PListLockChange += pLocked => pFlow.PFlowEditSet(!pLocked);
        pFlow.PFlowEditSet(!pList.PListLockCheck());
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pSection, pProcessing, pInspector, pViewer, pExport },
            new PCompass(pFlow, true), pAction, pFlow, lPreferenceTabLayout);
        if (lPreferenceTabLayout is null)
        {
            pProcessing.PProcessingMinimizeSet(true);
            pInspector.PInspectorMinimizeSet(true);
        }

        Content = pTabGrid;
        PSplitDetectorRestore(lPreferenceTabLayout);
    }

    private void PSplitPathShow(string? pSourcePath)
    {
        if (!string.IsNullOrWhiteSpace(pSourcePath))
        {
            pViewer.PViewerSourceOpen(pSourcePath);
            PSplitDetectorLoad(pSourcePath);
        }
    }

    private async void PSplitSweepRun()
    {
        if (pSplitSweepSource is not null)
        {
            return;
        }

        if (pList.PListEditableRead() is not { } pSplitSelected || !pFlow.PFlowSweepReady)
        {
            return;
        }

        LDetectorBlank pSplitBlank = pInspector.PBlankRead();
        LDetectorStep pSplitScene = pInspector.PSensorStepRead(LDetectorKind.LDetectorKindScene);
        if (!pSplitBlank.LDetectorBlankEnabled && !pSplitScene.LDetectorStepEnabled)
        {
            return;
        }

        var pSplitSource = new System.Threading.CancellationTokenSource();
        pSplitSweepSource = pSplitSource;
        pInspector.PSensorRunningSet(true);
        pInspector.PSensorLockSet(true);
        pProcessing.IsEnabled = false;
        pInspector.PSensorProgressShow();
        var pSplitProgress = new Progress<double>(pValue => pInspector.PSensorProgressApply(pValue));
        try
        {
            if (pSplitBlank.LDetectorBlankEnabled)
            {
                IReadOnlyList<(TimeSpan Start, TimeSpan End)> pSplitBlanks =
                    await LSweep.LSweepScan(
                        pSplitSelected.LDocketEntryPath,
                        pSplitBlank,
                        pFlow.PFlowSweepDuration,
                        pSplitSource.Token,
                        pSplitProgress);
                if (!pSplitSource.IsCancellationRequested)
                {
                    pFlow.PFlowSweepApply(pSplitBlanks);
                }
            }

            if (pSplitScene.LDetectorStepEnabled && !pSplitSource.IsCancellationRequested)
            {
                IReadOnlyList<TimeSpan> pSplitBoundaries =
                    await LSweep.LSweepSceneScan(
                        pSplitSelected.LDocketEntryPath,
                        pSplitScene.LDetectorStepThreshold,
                        pFlow.PFlowSweepDuration,
                        pSplitSource.Token,
                        pSplitProgress);
                if (!pSplitSource.IsCancellationRequested)
                {
                    pFlow.PFlowSceneApply(pSplitBoundaries, TimeSpan.FromSeconds(pSplitScene.LDetectorStepMinimum));
                }
            }
        }
        catch (Exception pSplitException) when (pSplitException is System.ComponentModel.Win32Exception
            or System.IO.IOException
            or InvalidOperationException
            or OperationCanceledException)
        {
        }
        finally
        {
            pInspector.PSensorProgressHide();
            pInspector.PSensorLockSet(false);
            pInspector.PSensorRunningSet(false);
            pProcessing.IsEnabled = true;
            pSplitSweepSource = null;
            pSplitSource.Dispose();
        }
    }

    private void PSplitPersistentHandle(bool pSplitPersistent)
    {
        PSplitDetectorSave();
        if (pSplitPersistent)
        {
            PSplitSweepRun();
        }
    }

    private LSidecarSplitRecord PSplitSidecarRead()
    {
        var pSplitDetectors = new List<LSidecarDetectorRecord>();
        foreach (LDetectorKind pDetectorKind in LDetector.LDetectorKinds)
        {
            if (pDetectorKind == LDetectorKind.LDetectorKindBlank)
            {
                LDetectorBlank pBlank = pInspector.PBlankRead();
                pSplitDetectors.Add(new LSidecarDetectorRecord
                {
                    LSidecarDetectorKind = (int)pDetectorKind,
                    LSidecarDetectorEnabled = pBlank.LDetectorBlankEnabled,
                    LSidecarDetectorType = (int)pBlank.LDetectorBlankType,
                    LSidecarDetectorHue = pBlank.LDetectorBlankHue,
                    LSidecarDetectorSaturation = pBlank.LDetectorBlankSaturation,
                    LSidecarDetectorBrightness = pBlank.LDetectorBlankBrightness,
                    LSidecarDetectorTolerance = pBlank.LDetectorBlankTolerance,
                    LSidecarDetectorCoverage = pBlank.LDetectorBlankCoverage,
                    LSidecarDetectorMinimum = pBlank.LDetectorBlankMinimum
                });
                continue;
            }

            LDetectorStep pStep = pInspector.PSensorStepRead(pDetectorKind);
            pSplitDetectors.Add(new LSidecarDetectorRecord
            {
                LSidecarDetectorKind = (int)pDetectorKind,
                LSidecarDetectorEnabled = pStep.LDetectorStepEnabled,
                LSidecarDetectorThreshold = pStep.LDetectorStepThreshold,
                LSidecarDetectorMinimum = pStep.LDetectorStepMinimum
            });
        }

        return new LSidecarSplitRecord
        {
            LSidecarSplitPersistent = pInspector.PSensorPersistentCheck(),
            LSidecarSplitDetectors = pSplitDetectors
        };
    }

    private void PSplitDetectorSave()
    {
        if (pSplitDetectorLoading
            || pViewer.PViewerSourcePath is not { } pSplitSourcePath
            || pList.PListLockCheck(pSplitSourcePath))
        {
            return;
        }

        LSidecarSplitRecord pSplitRecord = PSplitSidecarRead();
        if (!pSplitRecord.LSidecarSplitActive && LLibrarian.LLibrarianSplitLoad(pSplitSourcePath) is null)
        {
            return;
        }

        LLibrarian.LLibrarianSplitSave(pSplitSourcePath, pSplitRecord);
    }

    private void PSplitDetectorLoad(string pSplitSourcePath)
    {
        if (LLibrarian.LLibrarianSplitLoad(pSplitSourcePath) is not { } pSplitRecord)
        {
            return;
        }

        pSplitDetectorLoading = true;
        try
        {
            pInspector.PSensorPersistentApply(pSplitRecord.LSidecarSplitPersistent);
            foreach (LSidecarDetectorRecord pDetector in pSplitRecord.LSidecarSplitDetectors)
            {
                if (!Enum.IsDefined(typeof(LDetectorKind), pDetector.LSidecarDetectorKind))
                {
                    continue;
                }

                var pDetectorKind = (LDetectorKind)pDetector.LSidecarDetectorKind;
                if (pDetectorKind == LDetectorKind.LDetectorKindBlank)
                {
                    pInspector.PBlankApply(new LDetectorBlank(
                        pDetector.LSidecarDetectorEnabled,
                        Enum.IsDefined(typeof(LDetectorType), pDetector.LSidecarDetectorType)
                            ? (LDetectorType)pDetector.LSidecarDetectorType
                            : LDetectorType.LDetectorTypeBlack,
                        pDetector.LSidecarDetectorHue,
                        pDetector.LSidecarDetectorSaturation,
                        pDetector.LSidecarDetectorBrightness,
                        pDetector.LSidecarDetectorTolerance,
                        pDetector.LSidecarDetectorCoverage,
                        pDetector.LSidecarDetectorMinimum));
                    continue;
                }

                pInspector.PSensorApply(new LDetectorStep(
                    pDetectorKind,
                    pDetector.LSidecarDetectorEnabled,
                    pDetector.LSidecarDetectorThreshold,
                    pDetector.LSidecarDetectorMinimum));
            }

            PSplitActiveUpdate();
        }
        finally
        {
            pSplitDetectorLoading = false;
        }
    }

    private void PSplitActiveUpdate()
    {
        foreach (LDetectorKind pDetectorKind in LDetector.LDetectorKinds)
        {
            pProcessing.PProcessingActiveSet(
                PInspector.PSensorNameRead(pDetectorKind),
                pInspector.PSensorStepRead(pDetectorKind).LDetectorStepEnabled);
        }
    }

    private List<LSceneDetector> PSplitDetectorRead()
    {
        var pDetectors = new List<LSceneDetector>();
        foreach (LDetectorKind pDetectorKind in LDetector.LDetectorKinds)
        {
            if (pDetectorKind == LDetectorKind.LDetectorKindBlank)
            {
                LDetectorBlank pBlank = pInspector.PBlankRead();
                pDetectors.Add(new LSceneDetector
                {
                    LSceneDetectorKind = (int)pDetectorKind,
                    LSceneDetectorEnabled = pBlank.LDetectorBlankEnabled,
                    LSceneDetectorType = (int)pBlank.LDetectorBlankType,
                    LSceneDetectorHue = pBlank.LDetectorBlankHue,
                    LSceneDetectorSaturation = pBlank.LDetectorBlankSaturation,
                    LSceneDetectorBrightness = pBlank.LDetectorBlankBrightness,
                    LSceneDetectorTolerance = pBlank.LDetectorBlankTolerance,
                    LSceneDetectorCoverage = pBlank.LDetectorBlankCoverage,
                    LSceneDetectorMinimum = pBlank.LDetectorBlankMinimum
                });
                continue;
            }

            LDetectorStep pStep = pInspector.PSensorStepRead(pDetectorKind);
            pDetectors.Add(new LSceneDetector
            {
                LSceneDetectorKind = (int)pDetectorKind,
                LSceneDetectorEnabled = pStep.LDetectorStepEnabled,
                LSceneDetectorThreshold = pStep.LDetectorStepThreshold,
                LSceneDetectorMinimum = pStep.LDetectorStepMinimum
            });
        }

        return pDetectors;
    }

    private void PSplitDetectorRestore(LSceneTabRecord? lPreferenceTabLayout)
    {
        if (lPreferenceTabLayout is null)
        {
            return;
        }

        foreach (LSceneDetector pDetector in lPreferenceTabLayout.LSceneDetectors)
        {
            if (!Enum.IsDefined(typeof(LDetectorKind), pDetector.LSceneDetectorKind))
            {
                continue;
            }

            var pDetectorKind = (LDetectorKind)pDetector.LSceneDetectorKind;
            if (pDetectorKind == LDetectorKind.LDetectorKindBlank)
            {
                pInspector.PBlankApply(new LDetectorBlank(
                    pDetector.LSceneDetectorEnabled,
                    Enum.IsDefined(typeof(LDetectorType), pDetector.LSceneDetectorType)
                        ? (LDetectorType)pDetector.LSceneDetectorType
                        : LDetectorType.LDetectorTypeBlack,
                    pDetector.LSceneDetectorHue,
                    pDetector.LSceneDetectorSaturation,
                    pDetector.LSceneDetectorBrightness,
                    pDetector.LSceneDetectorTolerance,
                    pDetector.LSceneDetectorCoverage,
                    pDetector.LSceneDetectorMinimum));
                continue;
            }

            pInspector.PSensorApply(new LDetectorStep(
                pDetectorKind,
                pDetector.LSceneDetectorEnabled,
                pDetector.LSceneDetectorThreshold,
                pDetector.LSceneDetectorMinimum));
        }

        pInspector.PSensorPersistentApply(lPreferenceTabLayout.LSceneDetectPersistent);
        PSplitActiveUpdate();
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override bool PTabSectionVisible => true;
    public override LSceneTabRecord PTabLayoutRead()
    {
        LSceneTabRecord lPreferenceTabLayout = PTabLayoutRead(pTabGrid);
        lPreferenceTabLayout.LSceneDetectors = PSplitDetectorRead();
        lPreferenceTabLayout.LSceneDetectPersistent = pInspector.PSensorPersistentCheck();
        return lPreferenceTabLayout;
    }
}
