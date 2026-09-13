using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PPanel;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PDeck;

public sealed partial class PSplitTab
{
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
        var pSplitExcluded = new List<LSweepSpan>();
        var pSplitKept = new List<LSweepSpan>();
        var pSplitBoundaries = new List<LSweepBoundary>();
        try
        {
            if (pSplitBlank.LDetectorBlankEnabled)
            {
                IReadOnlyList<LSweepSpan> pSplitBlanks =
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
                pSplitBoundaries.AddRange(
                    pSplitScenes.Select(pSplitTime => new LSweepBoundary(pSplitTime, pSplitSceneMinimum)));
            }

            if (pSplitStill.LDetectorStepEnabled && !pSplitSource.IsCancellationRequested)
            {
                IReadOnlyList<LSweepSpan> pSplitStills =
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
                pSplitBoundaries.AddRange(
                    pSplitLuminances.Select(pSplitTime => new LSweepBoundary(pSplitTime, TimeSpan.Zero)));
            }

            if (pSplitSilence.LDetectorStepEnabled && !pSplitSource.IsCancellationRequested)
            {
                IReadOnlyList<LSweepSpan> pSplitSilences =
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
                pSplitBoundaries.AddRange(
                    pSplitVolumes.Select(pSplitTime => new LSweepBoundary(pSplitTime, TimeSpan.Zero)));
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
}
