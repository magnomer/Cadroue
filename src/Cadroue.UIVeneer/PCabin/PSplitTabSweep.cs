using System.Windows;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PWing;
using Cadroue.ShellEngine;

namespace Cadroue.UIVeneer.PCabin;

public sealed partial class PSplitTab
{
    private delegate Task PSplitSweepStep(
        IProgress<double> pSplitProgress,
        System.Threading.CancellationToken pSplitToken);

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

        var pSplitExcluded = new List<LSweepSpan>();
        var pSplitKept = new List<LSweepSpan>();
        var pSplitBoundaries = new List<LSweepBoundary>();
        IReadOnlyList<PSplitSweepStep> pSplitSteps = PSplitStepsCreate(
            pSplitSelected.LDocketEntryPath, pSplitExcluded, pSplitKept, pSplitBoundaries);
        if (pSplitSteps.Count == 0)
        {
            return;
        }

        var pSplitSource = new System.Threading.CancellationTokenSource();
        pSplitSweepSource = pSplitSource;
        pInspector.PSensorRunningSet(true);
        pInspector.PSensorLockSet(true);
        pFlow.PFlowEditSet(false);
        pProcessing.IsEnabled = false;
        pInspector.PSensorProgressShow();
        try
        {
            for (int pSplitStage = 0; pSplitStage < pSplitSteps.Count; pSplitStage++)
            {
                double pSplitOffset = pSplitStage;
                var pSplitProgress = new Progress<double>(
                    pValue => pInspector.PSensorProgressApply((pSplitOffset + pValue) / pSplitSteps.Count));
                await pSplitSteps[pSplitStage](pSplitProgress, pSplitSource.Token);
            }

            if (!pFlow.PFlowCombineApply(pSplitExcluded, pSplitKept, pSplitBoundaries))
            {
                PSWarning.PSWarningShow(
                    Window.GetWindow(this),
                    LLocalization.LLocalizationTextRead("Inspector.Detect.FailTitle"),
                    LLocalization.LLocalizationFormat("Inspector.Detect.CeilingMessage", LPiece.LPieceCeiling));
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception pSplitException) when (pSplitException is System.ComponentModel.Win32Exception
            or System.IO.IOException
            or InvalidOperationException)
        {
            LTraceLog.LTraceErrorRecord($"Detection failed for '{pSplitSelected.LDocketEntryPath}'", pSplitException);
            PSWarning.PSWarningShow(
                Window.GetWindow(this),
                LLocalization.LLocalizationTextRead("Inspector.Detect.FailTitle"),
                LLocalization.LLocalizationFormat("Inspector.Detect.FailMessage", pSplitException.Message));
        }
        finally
        {
            pInspector.PSensorProgressHide();
            pFlow.PFlowEditSet(!pList.PListLockCheck());
            pInspector.PSensorLockSet(false);
            pInspector.PSensorRunningSet(false);
            pProcessing.IsEnabled = true;
            pSplitSweepSource = null;
            pSplitSource.Dispose();
        }
    }

    private IReadOnlyList<PSplitSweepStep> PSplitStepsCreate(
        string pSplitPath,
        List<LSweepSpan> pSplitExcluded,
        List<LSweepSpan> pSplitKept,
        List<LSweepBoundary> pSplitBoundaries)
    {
        TimeSpan pSplitDuration = pFlow.PFlowSweepDuration;
        var pSplitSteps = new List<PSplitSweepStep>();

        LDetectorBlank pSplitBlank = pInspector.PBlankRead();
        if (pSplitBlank.LDetectorBlankEnabled)
        {
            pSplitSteps.Add(async (pProgress, pToken) => pSplitExcluded.AddRange(
                await LSweep.LSweepScan(pSplitPath, pSplitBlank, pSplitDuration, pToken, pProgress)));
        }

        LDetectorStep pSplitScene = pInspector.PSensorStepRead(LDetectorKind.LDetectorKindScene);
        if (pSplitScene.LDetectorStepEnabled)
        {
            TimeSpan pSplitSceneMinimum = TimeSpan.FromSeconds(pSplitScene.LDetectorStepMinimum);
            pSplitSteps.Add(async (pProgress, pToken) => pSplitBoundaries.AddRange(
                (await LSweep.LSweepSceneScan(
                    pSplitPath,
                    LDetector.LDetectorThresholdResolve(pSplitScene.LDetectorStepThreshold),
                    pSplitDuration,
                    pToken,
                    pProgress))
                .Select(pSplitTime => new LSweepBoundary(pSplitTime, pSplitSceneMinimum))));
        }

        LDetectorStep pSplitStill = pInspector.PSensorStepRead(LDetectorKind.LDetectorKindStill);
        if (pSplitStill.LDetectorStepEnabled)
        {
            List<LSweepSpan> pSplitTarget = pInspector.PSensorModeRead(LDetectorKind.LDetectorKindStill)
                == LDetectorStillMode.LDetectorStillTreat
                ? pSplitKept
                : pSplitExcluded;
            pSplitSteps.Add(async (pProgress, pToken) => pSplitTarget.AddRange(
                await LSweep.LSweepStillScan(
                    pSplitPath,
                    pSplitStill.LDetectorStepThreshold,
                    pSplitStill.LDetectorStepMinimum,
                    pSplitDuration,
                    pToken,
                    pProgress)));
        }

        LDetectorStep pSplitLuminance = pInspector.PSensorStepRead(LDetectorKind.LDetectorKindLuminance);
        if (pSplitLuminance.LDetectorStepEnabled)
        {
            LDetectorLuminanceMode pSplitSpeed = pInspector.PSensorSpeedRead(LDetectorKind.LDetectorKindLuminance);
            pSplitSteps.Add(async (pProgress, pToken) => pSplitBoundaries.AddRange(
                (await LSweep.LSweepLuminanceScan(
                    pSplitPath,
                    pSplitLuminance.LDetectorStepWindow,
                    pSplitLuminance.LDetectorStepThreshold,
                    pSplitLuminance.LDetectorStepMinimum,
                    pSplitSpeed,
                    pSplitDuration,
                    pToken,
                    pProgress))
                .Select(pSplitTime => new LSweepBoundary(pSplitTime, TimeSpan.Zero))));
        }

        LDetectorStep pSplitSilence = pInspector.PSensorStepRead(LDetectorKind.LDetectorKindSilence);
        if (pSplitSilence.LDetectorStepEnabled)
        {
            pSplitSteps.Add(async (pProgress, pToken) => pSplitExcluded.AddRange(
                await LSweep.LSweepSilenceScan(
                    pSplitPath,
                    pSplitSilence.LDetectorStepThreshold,
                    pSplitSilence.LDetectorStepMinimum,
                    pSplitDuration,
                    pToken,
                    pProgress)));
        }

        LDetectorStep pSplitVolume = pInspector.PSensorStepRead(LDetectorKind.LDetectorKindVolume);
        if (pSplitVolume.LDetectorStepEnabled)
        {
            LDetectorMetricMode pSplitMetric = pInspector.PSensorMetricRead(LDetectorKind.LDetectorKindVolume);
            pSplitSteps.Add(async (pProgress, pToken) => pSplitBoundaries.AddRange(
                (await LSweep.LSweepVolumeScan(
                    pSplitPath,
                    pSplitVolume.LDetectorStepWindow,
                    pSplitVolume.LDetectorStepThreshold,
                    pSplitVolume.LDetectorStepMinimum,
                    pSplitMetric,
                    pSplitDuration,
                    pToken,
                    pProgress))
                .Select(pSplitTime => new LSweepBoundary(pSplitTime, TimeSpan.Zero))));
        }

        return pSplitSteps;
    }
}
