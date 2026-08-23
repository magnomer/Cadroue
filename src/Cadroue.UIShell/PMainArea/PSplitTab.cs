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
        LDetectorStep pSplitStill = pInspector.PSensorStepRead(LDetectorKind.LDetectorKindStill);
        LDetectorStillMode pSplitMode = pInspector.PSensorModeRead(LDetectorKind.LDetectorKindStill);
        LDetectorStep pSplitLuminance = pInspector.PSensorStepRead(LDetectorKind.LDetectorKindLuminance);
        LDetectorStep pSplitSilence = pInspector.PSensorStepRead(LDetectorKind.LDetectorKindSilence);
        LDetectorStep pSplitVolume = pInspector.PSensorStepRead(LDetectorKind.LDetectorKindVolume);
        LDetectorMetricMode pSplitMetric = pInspector.PSensorMetricRead(LDetectorKind.LDetectorKindVolume);
        if (!pSplitBlank.LDetectorBlankEnabled
            && !pSplitScene.LDetectorStepEnabled
            && !pSplitStill.LDetectorStepEnabled
            && !pSplitLuminance.LDetectorStepEnabled
            && !pSplitSilence.LDetectorStepEnabled
            && !pSplitVolume.LDetectorStepEnabled)
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
        var pSplitExcluded = new List<(TimeSpan Start, TimeSpan End)>();
        var pSplitKept = new List<(TimeSpan Start, TimeSpan End)>();
        var pSplitBoundaries = new List<(TimeSpan Time, TimeSpan Minimum)>();
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
                pSplitExcluded.AddRange(pSplitBlanks);
            }

            if (pSplitScene.LDetectorStepEnabled && !pSplitSource.IsCancellationRequested)
            {
                IReadOnlyList<TimeSpan> pSplitScenes =
                    await LSweep.LSweepSceneScan(
                        pSplitSelected.LDocketEntryPath,
                        LDetector.LDetectorThresholdResolve(pSplitScene.LDetectorStepThreshold),
                        pFlow.PFlowSweepDuration,
                        pSplitSource.Token,
                        pSplitProgress);
                TimeSpan pSplitSceneMinimum = TimeSpan.FromSeconds(pSplitScene.LDetectorStepMinimum);
                pSplitBoundaries.AddRange(pSplitScenes.Select(pSplitTime => (pSplitTime, pSplitSceneMinimum)));
            }

            if (pSplitStill.LDetectorStepEnabled && !pSplitSource.IsCancellationRequested)
            {
                IReadOnlyList<(TimeSpan Start, TimeSpan End)> pSplitStills =
                    await LSweep.LSweepStillScan(
                        pSplitSelected.LDocketEntryPath,
                        pSplitStill.LDetectorStepThreshold,
                        pSplitStill.LDetectorStepMinimum,
                        pFlow.PFlowSweepDuration,
                        pSplitSource.Token,
                        pSplitProgress);
                if (pSplitMode == LDetectorStillMode.LDetectorStillTreat)
                {
                    pSplitKept.AddRange(pSplitStills);
                }
                else
                {
                    pSplitExcluded.AddRange(pSplitStills);
                }
            }

            if (pSplitLuminance.LDetectorStepEnabled && !pSplitSource.IsCancellationRequested)
            {
                IReadOnlyList<TimeSpan> pSplitLuminances =
                    await LSweep.LSweepLuminanceScan(
                        pSplitSelected.LDocketEntryPath,
                        pSplitLuminance.LDetectorStepWindow,
                        pSplitLuminance.LDetectorStepThreshold,
                        pSplitLuminance.LDetectorStepMinimum,
                        pInspector.PSensorSpeedRead(LDetectorKind.LDetectorKindLuminance),
                        pFlow.PFlowSweepDuration,
                        pSplitSource.Token,
                        pSplitProgress);
                pSplitBoundaries.AddRange(pSplitLuminances.Select(pSplitTime => (pSplitTime, TimeSpan.Zero)));
            }

            if (pSplitSilence.LDetectorStepEnabled && !pSplitSource.IsCancellationRequested)
            {
                IReadOnlyList<(TimeSpan Start, TimeSpan End)> pSplitSilences =
                    await LSweep.LSweepSilenceScan(
                        pSplitSelected.LDocketEntryPath,
                        pSplitSilence.LDetectorStepThreshold,
                        pSplitSilence.LDetectorStepMinimum,
                        pFlow.PFlowSweepDuration,
                        pSplitSource.Token,
                        pSplitProgress);
                pSplitExcluded.AddRange(pSplitSilences);
            }

            if (pSplitVolume.LDetectorStepEnabled && !pSplitSource.IsCancellationRequested)
            {
                IReadOnlyList<TimeSpan> pSplitVolumes =
                    await LSweep.LSweepVolumeScan(
                        pSplitSelected.LDocketEntryPath,
                        pSplitVolume.LDetectorStepWindow,
                        pSplitVolume.LDetectorStepThreshold,
                        pSplitVolume.LDetectorStepMinimum,
                        pSplitMetric,
                        pFlow.PFlowSweepDuration,
                        pSplitSource.Token,
                        pSplitProgress);
                pSplitBoundaries.AddRange(pSplitVolumes.Select(pSplitTime => (pSplitTime, TimeSpan.Zero)));
            }

            if (!pSplitSource.IsCancellationRequested)
            {
                pFlow.PFlowCombineApply(pSplitExcluded, pSplitKept, pSplitBoundaries);
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
                LSidecarDetectorMinimum = pStep.LDetectorStepMinimum,
                LSidecarDetectorWindow = pStep.LDetectorStepWindow,
                LSidecarDetectorType = PSplitModeRead(pDetectorKind)
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
                    pDetector.LSidecarDetectorMinimum,
                    pDetector.LSidecarDetectorWindow));

                if (pDetectorKind == LDetectorKind.LDetectorKindStill)
                {
                    pInspector.PSensorModeApply(pDetectorKind,
                        Enum.IsDefined(typeof(LDetectorStillMode), pDetector.LSidecarDetectorType)
                            ? (LDetectorStillMode)pDetector.LSidecarDetectorType
                            : LDetectorStillMode.LDetectorStillDiscard);
                }
                else if (pDetectorKind == LDetectorKind.LDetectorKindLuminance)
                {
                    pInspector.PSensorSpeedApply(pDetectorKind,
                        Enum.IsDefined(typeof(LDetectorLuminanceMode), pDetector.LSidecarDetectorType)
                            ? (LDetectorLuminanceMode)pDetector.LSidecarDetectorType
                            : LDetectorLuminanceMode.LDetectorLuminanceNormal);
                }
                else if (pDetectorKind == LDetectorKind.LDetectorKindVolume)
                {
                    pInspector.PSensorMetricApply(pDetectorKind,
                        Enum.IsDefined(typeof(LDetectorMetricMode), pDetector.LSidecarDetectorType)
                            ? (LDetectorMetricMode)pDetector.LSidecarDetectorType
                            : LDetectorMetricMode.LDetectorMetricLufs);
                }
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
                LSceneDetectorMinimum = pStep.LDetectorStepMinimum,
                LSceneDetectorWindow = pStep.LDetectorStepWindow,
                LSceneDetectorType = PSplitModeRead(pDetectorKind)
            });
        }

        return pDetectors;
    }

    private int PSplitModeRead(LDetectorKind pDetectorKind) => pDetectorKind switch
    {
        LDetectorKind.LDetectorKindStill => (int)pInspector.PSensorModeRead(pDetectorKind),
        LDetectorKind.LDetectorKindLuminance => (int)pInspector.PSensorSpeedRead(pDetectorKind),
        LDetectorKind.LDetectorKindVolume => (int)pInspector.PSensorMetricRead(pDetectorKind),
        _ => 0
    };

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
                pDetector.LSceneDetectorMinimum,
                pDetector.LSceneDetectorWindow));

            if (pDetectorKind == LDetectorKind.LDetectorKindStill)
            {
                pInspector.PSensorModeApply(pDetectorKind,
                    Enum.IsDefined(typeof(LDetectorStillMode), pDetector.LSceneDetectorType)
                        ? (LDetectorStillMode)pDetector.LSceneDetectorType
                        : LDetectorStillMode.LDetectorStillDiscard);
            }
            else if (pDetectorKind == LDetectorKind.LDetectorKindLuminance)
            {
                pInspector.PSensorSpeedApply(pDetectorKind,
                    Enum.IsDefined(typeof(LDetectorLuminanceMode), pDetector.LSceneDetectorType)
                        ? (LDetectorLuminanceMode)pDetector.LSceneDetectorType
                        : LDetectorLuminanceMode.LDetectorLuminanceNormal);
            }
            else if (pDetectorKind == LDetectorKind.LDetectorKindVolume)
            {
                pInspector.PSensorMetricApply(pDetectorKind,
                    Enum.IsDefined(typeof(LDetectorMetricMode), pDetector.LSceneDetectorType)
                        ? (LDetectorMetricMode)pDetector.LSceneDetectorType
                        : LDetectorMetricMode.LDetectorMetricLufs);
            }
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
