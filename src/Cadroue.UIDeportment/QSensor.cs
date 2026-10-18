using Cadroue.Core;

namespace Cadroue.UIDeportment;

// The detector settings the sensor panel edits: each kind's step and the still, luminance and metric modes.
public sealed class QSensor
{
    private readonly Dictionary<LDetectorKind, LDetectorStep> qSensorSteps = new();
    private LDetectorStillMode qSensorMode = LDetectorStillMode.LDetectorStillDiscard;
    private LDetectorLuminanceMode qSensorSpeed = LDetectorLuminanceMode.LDetectorLuminanceNormal;
    private LDetectorMetricMode qSensorMetric = LDetectorMetricMode.LDetectorMetricLufs;

    private static readonly LDetectorStillMode[] QSensorModes =
    {
        LDetectorStillMode.LDetectorStillDiscard, LDetectorStillMode.LDetectorStillTreat
    };

    private static readonly LDetectorLuminanceMode[] QSensorSpeeds =
    {
        LDetectorLuminanceMode.LDetectorLuminanceFast,
        LDetectorLuminanceMode.LDetectorLuminanceNormal,
        LDetectorLuminanceMode.LDetectorLuminanceFull
    };

    private static readonly LDetectorMetricMode[] QSensorMetrics =
    {
        LDetectorMetricMode.LDetectorMetricLufs, LDetectorMetricMode.LDetectorMetricRms
    };

    public event Action? QSensorChanged;

    public QSensor()
    {
        QSensorPreset = new QSensorPreset(this);
        QSensorStepPrepare();
        QSensorPlans = qSensorSteps.Keys.Select(QSensorPlan.QSensorPlanCreate).ToList();
        QSensorChangeAttach();
    }

    private static IEnumerable<LDetectorKind> QSensorKindsRead() =>
        LDetector.LDetectorKinds.Where(lKind => lKind != LDetectorKind.LDetectorKindBlank);

    private void QSensorStepPrepare()
    {
        foreach (LDetectorKind lKind in QSensorKindsRead())
        {
            qSensorSteps[lKind] = LDetector.LDetectorCreate(lKind);
            QSensorPreset.QSensorKindAdd(lKind);
        }
    }

    private void QSensorChangeAttach() => QSensorPreset.QSensorPresetChanged += () => QSensorChanged?.Invoke();

    public IReadOnlyList<QSensorPlan> QSensorPlans { get; }

    public QSensorPreset QSensorPreset { get; }

    public QSensorSweep QSensorSweep { get; } = new();

    public int QSensorModeIndex => Array.IndexOf(QSensorModes, qSensorMode);

    public int QSensorSpeedIndex => Array.IndexOf(QSensorSpeeds, qSensorSpeed);

    public int QSensorMetricIndex => Array.IndexOf(QSensorMetrics, qSensorMetric);

    public LDetectorStillMode QSensorMode => qSensorMode;

    public LDetectorLuminanceMode QSensorSpeed => qSensorSpeed;

    public LDetectorMetricMode QSensorMetric => qSensorMetric;

    public LDetectorStep QSensorStepRead(LDetectorKind lKind) =>
        qSensorSteps.TryGetValue(lKind, out LDetectorStep lStep) ? lStep : LDetector.LDetectorCreate(lKind);

    public bool QSensorEnabledRead(LDetectorKind lKind) => QSensorStepRead(lKind).LDetectorStepEnabled;

    public double QSensorValueRead(LDetectorKind lKind, int lRow)
    {
        LDetectorStep lStep = QSensorStepRead(lKind);
        return lRow switch
        {
            QSensorPlan.QSensorPlanWindow => lStep.LDetectorStepWindow,
            QSensorPlan.QSensorPlanMinimum => lStep.LDetectorStepMinimum,
            _ => lStep.LDetectorStepThreshold
        };
    }

    public double QSensorDefaultRead(LDetectorKind lKind, int lRow) => lRow switch
    {
        QSensorPlan.QSensorPlanWindow => LDetector.LDetectorWindowRead(lKind).LDetectorBoundDefault,
        QSensorPlan.QSensorPlanMinimum => LDetector.LDetectorMinimumRead(lKind).LDetectorBoundDefault,
        _ => LDetector.LDetectorThresholdRead(lKind).LDetectorBoundDefault
    };

    public string QSensorUnitRead(LDetectorKind lKind, int lRow) =>
        lKind == LDetectorKind.LDetectorKindVolume && lRow == QSensorPlan.QSensorPlanThreshold
            ? (qSensorMetric == LDetectorMetricMode.LDetectorMetricRms ? "dB" : "LU")
            : QSensorPlans.First(lPlan => lPlan.QSensorPlanKind == lKind)
                .QSensorPlanRows.First(lEntry => lEntry.QInspectorRowIndex == lRow).QInspectorRowUnit;

    public void QSensorValueSet(LDetectorKind lKind, int lRow, double lValue)
    {
        switch (lRow)
        {
            case QSensorPlan.QSensorPlanWindow:
                QSensorWindowSet(lKind, lValue);
                break;
            case QSensorPlan.QSensorPlanMinimum:
                QSensorMinimumSet(lKind, lValue);
                break;
            default:
                QSensorThresholdSet(lKind, lValue);
                break;
        }
    }

    public void QSensorModeSelect(int lIndex)
    {
        if (QSensorIndexCheck(lIndex, QSensorModes.Length))
        {
            QSensorModeSet(QSensorModes[lIndex]);
        }
    }

    public void QSensorSpeedSelect(int lIndex)
    {
        if (QSensorIndexCheck(lIndex, QSensorSpeeds.Length))
        {
            QSensorSpeedSet(QSensorSpeeds[lIndex]);
        }
    }

    public void QSensorMetricSelect(int lIndex)
    {
        if (QSensorIndexCheck(lIndex, QSensorMetrics.Length))
        {
            QSensorMetricSet(QSensorMetrics[lIndex]);
        }
    }

    private static bool QSensorIndexCheck(int lIndex, int lCount) => lIndex >= 0 && lIndex < lCount;

    public void QSensorStepSet(LDetectorStep lStep)
    {
        if (!qSensorSteps.ContainsKey(lStep.LDetectorStepKind))
        {
            return;
        }

        QSensorStepApply(LDetector.LDetectorStepNormalize(lStep));
    }

    public void QSensorEnabledSet(LDetectorKind lKind, bool lEnabled) =>
        QSensorStepApply(QSensorStepRead(lKind) with { LDetectorStepEnabled = lEnabled });

    public void QSensorThresholdSet(LDetectorKind lKind, double lThreshold) =>
        QSensorStepApply(
            LDetector.LDetectorStepNormalize(QSensorStepRead(lKind) with { LDetectorStepThreshold = lThreshold }));

    public void QSensorMinimumSet(LDetectorKind lKind, double lMinimum) =>
        QSensorStepApply(
            LDetector.LDetectorStepNormalize(QSensorStepRead(lKind) with { LDetectorStepMinimum = lMinimum }));

    public void QSensorWindowSet(LDetectorKind lKind, double lWindow) =>
        QSensorStepApply(
            LDetector.LDetectorStepNormalize(QSensorStepRead(lKind) with { LDetectorStepWindow = lWindow }));

    public void QSensorModeSet(LDetectorStillMode lMode)
    {
        if (qSensorMode == lMode)
        {
            return;
        }

        qSensorMode = lMode;
        QSensorChanged?.Invoke();
    }

    public void QSensorSpeedSet(LDetectorLuminanceMode lSpeed)
    {
        if (qSensorSpeed == lSpeed)
        {
            return;
        }

        qSensorSpeed = lSpeed;
        QSensorChanged?.Invoke();
    }

    public void QSensorMetricSet(LDetectorMetricMode lMetric)
    {
        if (qSensorMetric == lMetric)
        {
            return;
        }

        string? lMatch = QSensorPreset.QSensorMatchRead(LDetectorKind.LDetectorKindVolume);
        QSensorMetricCommit(lMetric);
        if (QSensorVolumeResolve(lMatch, lMetric) is { } lNormal)
        {
            QSensorStepCommit(lNormal);
        }

        QSensorChanged?.Invoke();
    }

    private void QSensorMetricCommit(LDetectorMetricMode lMetric) => qSensorMetric = lMetric;

    private LDetectorStep? QSensorVolumeResolve(string? lMatch, LDetectorMetricMode lMetric)
    {
        if (lMatch is { } lToken && LDetectorCatalog.LDetectorPresetRead(lToken) is { } lPreset)
        {
            LDetectorStep lStep = QSensorStepRead(LDetectorKind.LDetectorKindVolume);
            return LDetector.LDetectorStepNormalize(lStep with
            {
                LDetectorStepThreshold = LDetectorCatalog.LDetectorPresetResolve(lPreset, lMetric)
            });
        }

        return null;
    }

    internal void QSensorStepCommit(LDetectorStep lNormal) => qSensorSteps[lNormal.LDetectorStepKind] = lNormal;

    private void QSensorStepApply(LDetectorStep lNormal)
    {
        if (qSensorSteps[lNormal.LDetectorStepKind] == lNormal)
        {
            return;
        }

        QSensorStepCommit(lNormal);
        QSensorChanged?.Invoke();
    }
}
