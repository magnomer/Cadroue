using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QSplitTabSweep
{
    private delegate Task QSplitSweepStep(IProgress<double> lProgress, CancellationToken lToken);

    private readonly QInspector qSplitInspector;
    private readonly QList qSplitList;
    private readonly LDocket qSplitDocket;
    private readonly QFlow qSplitFlow;
    private CancellationTokenSource? qSplitSweepSource;

    public QSplitTabSweep(
        QInspector lInspector,
        QList lList,
        LDocket lDocket,
        QFlow lFlow)
    {
        qSplitInspector = lInspector;
        qSplitList = lList;
        qSplitDocket = lDocket;
        qSplitFlow = lFlow;
    }

    public event Action<bool>? QSplitBusyApplied;
    public event Action<double>? QSplitProgressApplied;
    public event Action<string, string>? QSplitFailRaised;

    public bool QSplitSweepRunning => qSplitSweepSource is not null;

    public IReadOnlyList<LDetectorKind> QSplitStepsRead()
    {
        QSensor lSensor = qSplitInspector.QInspectorSensor;
        return LDetector.LDetectorKinds
            .Where(lKind => lKind == LDetectorKind.LDetectorKindBlank
                ? qSplitInspector.QInspectorBlank.QBlankStep.LDetectorBlankEnabled
                : lSensor.QSensorStepRead(lKind).LDetectorStepEnabled)
            .ToArray();
    }

    public void QSplitSweepCancel() => qSplitSweepSource?.Cancel();

    public async Task QSplitSweepStart()
    {
        if (qSplitSweepSource is not null
            || QSplitSelectedRead() is not { } lSelected
            || qSplitFlow.QFlowRange is null)
        {
            return;
        }

        var lExcluded = new List<LSweepSpan>();
        var lKept = new List<LSweepSpan>();
        var lBoundaries = new List<LSweepBoundary>();
        IReadOnlyList<QSplitSweepStep> lSteps = QSplitStepsCreate(
            lSelected.LDocketEntryPath, lExcluded, lKept, lBoundaries);
        if (lSteps.Count == 0)
        {
            return;
        }

        var lSource = new CancellationTokenSource();
        QSplitSweepSet(lSource);
        qSplitInspector.QInspectorSensor.QSensorRunningSet(true);
        QSplitBusyApplied?.Invoke(true);
        QSplitProgressApplied?.Invoke(0);
        try
        {
            // The gate turns the editable switch off for the scan and back by the lock, then applies the result.
            if (!await qSplitFlow.QFlowSection.QFlowSectionGate.CSectionSweepRun(
                    () => QSplitStepsRun(lSteps, lSource.Token), lExcluded, lKept, lBoundaries, QSplitLockedCheck))
            {
                QSplitFailRaised?.Invoke(
                    QLocalizationCatalog.QLocalizationTextRead("Inspector.Detect.FailTitle"),
                    QLocalizationCatalog.QLocalizationFormat("Inspector.Detect.CeilingMessage", LPiece.LPieceCeiling));
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception lException) when (LSweep.LSweepFaultCheck(lException))
        {
            LTraceLog.LTraceErrorRecord($"Detection failed for '{lSelected.LDocketEntryPath}'", lException);
            QSplitFailRaised?.Invoke(
                QLocalizationCatalog.QLocalizationTextRead("Inspector.Detect.FailTitle"),
                QLocalizationCatalog.QLocalizationFormat("Inspector.Detect.FailMessage", lException.Message));
        }
        finally
        {
            QSplitBusyApplied?.Invoke(false);
            qSplitInspector.QInspectorSensor.QSensorRunningSet(false);
            QSplitSweepSet(null);
            lSource.Dispose();
        }
    }

    private void QSplitSweepSet(CancellationTokenSource? lSource) => qSplitSweepSource = lSource;

    private async Task QSplitStepsRun(IReadOnlyList<QSplitSweepStep> lSteps, CancellationToken lToken)
    {
        for (int lStage = 0; lStage < lSteps.Count; lStage++)
        {
            double lOffset = lStage;
            var lProgress = new Progress<double>(
                lValue => QSplitProgressApplied?.Invoke(QSplitProgressResolve(lOffset, lValue, lSteps.Count)));
            await lSteps[lStage](lProgress, lToken);
        }
    }

    private static double QSplitProgressResolve(double lOffset, double lValue, int lCount) =>
        (lOffset + lValue) / lCount;

    private IReadOnlyList<QSplitSweepStep> QSplitStepsCreate(
        string lPath,
        List<LSweepSpan> lExcluded,
        List<LSweepSpan> lKept,
        List<LSweepBoundary> lBoundaries)
    {
        QSensor lSensor = qSplitInspector.QInspectorSensor;
        TimeSpan lDuration = qSplitFlow.QFlowDuration;
        var lSteps = new List<QSplitSweepStep>();

        LDetectorBlank lBlank = qSplitInspector.QInspectorBlank.QBlankStep;
        if (lBlank.LDetectorBlankEnabled)
        {
            lSteps.Add(async (lProgress, lToken) => lExcluded.AddRange(
                await LSweep.LSweepScan(lPath, lBlank, lDuration, lToken, lProgress)));
        }

        LDetectorStep lScene = lSensor.QSensorStepRead(LDetectorKind.LDetectorKindScene);
        if (lScene.LDetectorStepEnabled)
        {
            TimeSpan lSceneMinimum = TimeSpan.FromSeconds(lScene.LDetectorStepMinimum);
            lSteps.Add(async (lProgress, lToken) => lBoundaries.AddRange(
                (await LSweep.LSweepSceneScan(
                    lPath,
                    LDetector.LDetectorThresholdResolve(lScene.LDetectorStepThreshold),
                    lDuration,
                    lToken,
                    lProgress))
                .Select(lTime => new LSweepBoundary(lTime, lSceneMinimum))));
        }

        LDetectorStep lStill = lSensor.QSensorStepRead(LDetectorKind.LDetectorKindStill);
        if (lStill.LDetectorStepEnabled)
        {
            List<LSweepSpan> lTarget = lSensor.QSensorMode == LDetectorStillMode.LDetectorStillTreat
                ? lKept
                : lExcluded;
            lSteps.Add(async (lProgress, lToken) => lTarget.AddRange(
                await LSweep.LSweepStillScan(
                    lPath,
                    lStill.LDetectorStepThreshold,
                    lStill.LDetectorStepMinimum,
                    lDuration,
                    lToken,
                    lProgress)));
        }

        LDetectorStep lLuminance = lSensor.QSensorStepRead(LDetectorKind.LDetectorKindLuminance);
        if (lLuminance.LDetectorStepEnabled)
        {
            LDetectorLuminanceMode lSpeed = lSensor.QSensorSpeed;
            lSteps.Add(async (lProgress, lToken) => lBoundaries.AddRange(
                (await LSweep.LSweepLuminanceScan(
                    lPath,
                    lLuminance.LDetectorStepWindow,
                    lLuminance.LDetectorStepThreshold,
                    lLuminance.LDetectorStepMinimum,
                    lSpeed,
                    lDuration,
                    lToken,
                    lProgress))
                .Select(lTime => new LSweepBoundary(lTime, TimeSpan.Zero))));
        }

        LDetectorStep lSilence = lSensor.QSensorStepRead(LDetectorKind.LDetectorKindSilence);
        if (lSilence.LDetectorStepEnabled)
        {
            lSteps.Add(async (lProgress, lToken) => lExcluded.AddRange(
                await LSweep.LSweepSilenceScan(
                    lPath,
                    lSilence.LDetectorStepThreshold,
                    lSilence.LDetectorStepMinimum,
                    lDuration,
                    lToken,
                    lProgress)));
        }

        LDetectorStep lVolume = lSensor.QSensorStepRead(LDetectorKind.LDetectorKindVolume);
        if (lVolume.LDetectorStepEnabled)
        {
            LDetectorMetricMode lMetric = lSensor.QSensorMetric;
            lSteps.Add(async (lProgress, lToken) => lBoundaries.AddRange(
                (await LSweep.LSweepVolumeScan(
                    lPath,
                    lVolume.LDetectorStepWindow,
                    lVolume.LDetectorStepThreshold,
                    lVolume.LDetectorStepMinimum,
                    lMetric,
                    lDuration,
                    lToken,
                    lProgress))
                .Select(lTime => new LSweepBoundary(lTime, TimeSpan.Zero))));
        }

        return lSteps;
    }

    private bool QSplitLockedCheck() =>
        qSplitList.QListPathCurrent is { } lPath && qSplitDocket.LDocketLockCheck(lPath);

    private LDocketEntry? QSplitSelectedRead() =>
        qSplitList.QListPathCurrent is { } lPath
        && qSplitDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;
}
