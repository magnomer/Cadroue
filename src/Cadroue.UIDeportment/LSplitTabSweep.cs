using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LSplitTabSweep
{
    private delegate Task LSplitSweepStep(IProgress<double> lProgress, CancellationToken lToken);

    private readonly LInspector lSplitInspector;
    private readonly LList lSplitList;
    private readonly LDocket lSplitDocket;
    private readonly LFlow lSplitFlow;
    private CancellationTokenSource? lSplitSweepSource;

    public LSplitTabSweep(
        LInspector lInspector,
        LList lList,
        LDocket lDocket,
        LFlow lFlow)
    {
        lSplitInspector = lInspector;
        lSplitList = lList;
        lSplitDocket = lDocket;
        lSplitFlow = lFlow;
    }

    public event Action<bool>? LSplitBusyApply;
    public event Action<double>? LSplitProgressApply;
    public event Action<string, string>? LSplitFailRaise;

    public bool LSplitSweepRunning => lSplitSweepSource is not null;

    public IReadOnlyList<LDetectorKind> LSplitStepsRead()
    {
        LSensor lSensor = lSplitInspector.LInspectorSensor;
        return LDetector.LDetectorKinds
            .Where(lKind => lKind == LDetectorKind.LDetectorKindBlank
                ? lSplitInspector.LInspectorBlank.LBlankStep.LDetectorBlankEnabled
                : lSensor.LSensorStepRead(lKind).LDetectorStepEnabled)
            .ToArray();
    }

    public void LSplitSweepCancel() => lSplitSweepSource?.Cancel();

    public async Task LSplitSweepStart()
    {
        if (lSplitSweepSource is not null
            || LSplitSelectedRead() is not { } lSelected
            || lSplitFlow.LFlowSpool is null)
        {
            return;
        }

        var lExcluded = new List<LSweepSpan>();
        var lKept = new List<LSweepSpan>();
        var lBoundaries = new List<LSweepBoundary>();
        IReadOnlyList<LSplitSweepStep> lSteps = LSplitStepsCreate(
            lSelected.LDocketEntryPath, lExcluded, lKept, lBoundaries);
        if (lSteps.Count == 0)
        {
            return;
        }

        var lSource = new CancellationTokenSource();
        lSplitSweepSource = lSource;
        lSplitInspector.LInspectorSensor.LSensorRunningSet(true);
        lSplitFlow.LFlowEditSet(false);
        LSplitBusyApply?.Invoke(true);
        LSplitProgressApply?.Invoke(0);
        try
        {
            for (int lStage = 0; lStage < lSteps.Count; lStage++)
            {
                double lOffset = lStage;
                var lProgress = new Progress<double>(
                    lValue => LSplitProgressApply?.Invoke((lOffset + lValue) / lSteps.Count));
                await lSteps[lStage](lProgress, lSource.Token);
            }

            if (!lSplitFlow.LFlowSection.LFlowCombineApply(lExcluded, lKept, lBoundaries))
            {
                LSplitFailRaise?.Invoke(
                    LLocalization.LLocalizationTextRead("Inspector.Detect.FailTitle"),
                    LLocalization.LLocalizationFormat("Inspector.Detect.CeilingMessage", LPiece.LPieceCeiling));
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception lException) when (LSweep.LSweepFaultCheck(lException))
        {
            LTraceLog.LTraceErrorRecord($"Detection failed for '{lSelected.LDocketEntryPath}'", lException);
            LSplitFailRaise?.Invoke(
                LLocalization.LLocalizationTextRead("Inspector.Detect.FailTitle"),
                LLocalization.LLocalizationFormat("Inspector.Detect.FailMessage", lException.Message));
        }
        finally
        {
            LSplitBusyApply?.Invoke(false);
            lSplitFlow.LFlowEditSet(!LSplitLockedCheck());
            lSplitInspector.LInspectorSensor.LSensorRunningSet(false);
            lSplitSweepSource = null;
            lSource.Dispose();
        }
    }

    private IReadOnlyList<LSplitSweepStep> LSplitStepsCreate(
        string lPath,
        List<LSweepSpan> lExcluded,
        List<LSweepSpan> lKept,
        List<LSweepBoundary> lBoundaries)
    {
        LSensor lSensor = lSplitInspector.LInspectorSensor;
        TimeSpan lDuration = lSplitFlow.LFlowDuration;
        var lSteps = new List<LSplitSweepStep>();

        LDetectorBlank lBlank = lSplitInspector.LInspectorBlank.LBlankStep;
        if (lBlank.LDetectorBlankEnabled)
        {
            lSteps.Add(async (lProgress, lToken) => lExcluded.AddRange(
                await LSweep.LSweepScan(lPath, lBlank, lDuration, lToken, lProgress)));
        }

        LDetectorStep lScene = lSensor.LSensorStepRead(LDetectorKind.LDetectorKindScene);
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

        LDetectorStep lStill = lSensor.LSensorStepRead(LDetectorKind.LDetectorKindStill);
        if (lStill.LDetectorStepEnabled)
        {
            List<LSweepSpan> lTarget = lSensor.LSensorMode == LDetectorStillMode.LDetectorStillTreat
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

        LDetectorStep lLuminance = lSensor.LSensorStepRead(LDetectorKind.LDetectorKindLuminance);
        if (lLuminance.LDetectorStepEnabled)
        {
            LDetectorLuminanceMode lSpeed = lSensor.LSensorSpeed;
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

        LDetectorStep lSilence = lSensor.LSensorStepRead(LDetectorKind.LDetectorKindSilence);
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

        LDetectorStep lVolume = lSensor.LSensorStepRead(LDetectorKind.LDetectorKindVolume);
        if (lVolume.LDetectorStepEnabled)
        {
            LDetectorMetricMode lMetric = lSensor.LSensorMetric;
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

    private bool LSplitLockedCheck() =>
        lSplitList.LListPathCurrent is { } lPath && lSplitDocket.LDocketLockCheck(lPath);

    private LDocketEntry? LSplitSelectedRead() =>
        lSplitList.LListPathCurrent is { } lPath
        && lSplitDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;
}
