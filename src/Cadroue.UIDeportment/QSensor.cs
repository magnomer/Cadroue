using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QSensor
{
    private const string QSensorBlank = "Blank";
    private const string QSensorScene = "Scene";
    private const string QSensorStill = "Still Image";
    private const string QSensorLuminance = "Luminance";
    private const string QSensorSilence = "Silence";
    private const string QSensorVolume = "Volume";

    private readonly Dictionary<LDetectorKind, LDetectorStep> qSensorSteps = new();
    private readonly Dictionary<LDetectorKind, string?> qSensorTokens = new();
    private LDetectorStillMode qSensorMode = LDetectorStillMode.LDetectorStillDiscard;
    private LDetectorLuminanceMode qSensorSpeed = LDetectorLuminanceMode.LDetectorLuminanceNormal;
    private LDetectorMetricMode qSensorMetric = LDetectorMetricMode.LDetectorMetricLufs;
    private bool qSensorRunning;
    private bool qSensorPersistent;

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
    public event Action<bool>? QSensorRunningChanged;
    public event Action<bool>? QSensorPersistentChanged;
    public event Action? QSensorRunApplied;
    public event Action? QSensorStopApplied;

    public QSensor()
    {
        foreach (LDetectorKind lKind in LDetector.LDetectorKinds)
        {
            if (lKind == LDetectorKind.LDetectorKindBlank)
            {
                continue;
            }

            qSensorSteps[lKind] = LDetector.LDetectorCreate(lKind);
            qSensorTokens[lKind] = QSensorPresetCheck(lKind) ? LDetector.LDetectorTokenDefault : null;
        }

        QSensorPlans = qSensorSteps.Keys.Select(QSensorPlan.QSensorPlanCreate).ToList();
    }

    public IReadOnlyList<QSensorPlan> QSensorPlans { get; }

    public int QSensorModeIndex => Array.IndexOf(QSensorModes, qSensorMode);

    public int QSensorSpeedIndex => Array.IndexOf(QSensorSpeeds, qSensorSpeed);

    public int QSensorMetricIndex => Array.IndexOf(QSensorMetrics, qSensorMetric);

    public string QSensorRunText =>
        LLocalization.LLocalizationTextRead(qSensorRunning ? "Inspector.Detect.Stop" : "Inspector.Detect.Run");

    public LDetectorStillMode QSensorMode => qSensorMode;

    public LDetectorLuminanceMode QSensorSpeed => qSensorSpeed;

    public LDetectorMetricMode QSensorMetric => qSensorMetric;

    public bool QSensorRunning => qSensorRunning;

    public bool QSensorPersistent => qSensorPersistent;

    public static bool QSensorPresetCheck(LDetectorKind lKind) => LDetector.LDetectorTokensRead(lKind).Count > 0;

    public static string QSensorNameRead(LDetectorKind lKind) => lKind switch
    {
        LDetectorKind.LDetectorKindBlank => QSensorBlank,
        LDetectorKind.LDetectorKindScene => QSensorScene,
        LDetectorKind.LDetectorKindStill => QSensorStill,
        LDetectorKind.LDetectorKindLuminance => QSensorLuminance,
        LDetectorKind.LDetectorKindSilence => QSensorSilence,
        LDetectorKind.LDetectorKindVolume => QSensorVolume,
        _ => string.Empty
    };

    public static LDetectorKind? QSensorKindRead(string? lName) => lName switch
    {
        QSensorBlank => LDetectorKind.LDetectorKindBlank,
        QSensorScene => LDetectorKind.LDetectorKindScene,
        QSensorStill => LDetectorKind.LDetectorKindStill,
        QSensorLuminance => LDetectorKind.LDetectorKindLuminance,
        QSensorSilence => LDetectorKind.LDetectorKindSilence,
        QSensorVolume => LDetectorKind.LDetectorKindVolume,
        _ => null
    };

    public LDetectorStep QSensorStepRead(LDetectorKind lKind) =>
        qSensorSteps.TryGetValue(lKind, out LDetectorStep lStep) ? lStep : LDetector.LDetectorCreate(lKind);

    public string? QSensorTokenRead(LDetectorKind lKind) => qSensorTokens.GetValueOrDefault(lKind);

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

    public QInspectorChoice QSensorChoiceRead(LDetectorKind lKind) => QInspectorPlan.QInspectorChoiceRead(
        LDetector.LDetectorTokensRead(lKind),
        lToken => QSensorPlan.QSensorKeyRead(lKind, lToken),
        QSensorTokenRead(lKind),
        QSensorMatchRead(lKind));

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
        if (lIndex >= 0 && lIndex < QSensorModes.Length)
        {
            QSensorModeSet(QSensorModes[lIndex]);
        }
    }

    public void QSensorSpeedSelect(int lIndex)
    {
        if (lIndex >= 0 && lIndex < QSensorSpeeds.Length)
        {
            QSensorSpeedSet(QSensorSpeeds[lIndex]);
        }
    }

    public void QSensorMetricSelect(int lIndex)
    {
        if (lIndex >= 0 && lIndex < QSensorMetrics.Length)
        {
            QSensorMetricSet(QSensorMetrics[lIndex]);
        }
    }

    public void QSensorChoiceSelect(LDetectorKind lKind, int lIndex)
    {
        string? lToken = QInspectorPlan.QInspectorChoiceResolve(
            LDetector.LDetectorTokensRead(lKind), lIndex, QSensorTokenRead(lKind), QSensorMatchRead(lKind));
        if (lToken is not null)
        {
            QSensorPresetSelect(lKind, lToken);
        }
    }

    public void QSensorRunHandle()
    {
        if (qSensorRunning)
        {
            QSensorStopApplied?.Invoke();
        }
        else
        {
            QSensorRunApplied?.Invoke();
        }
    }

    public string? QSensorMatchRead(LDetectorKind lKind)
    {
        LDetectorStep lStep = QSensorStepRead(lKind);
        return lKind switch
        {
            LDetectorKind.LDetectorKindScene => QSensorSceneMatch(lStep.LDetectorStepThreshold),
            LDetectorKind.LDetectorKindStill => LDetector.LDetectorStillMatch(
                lStep.LDetectorStepThreshold, lStep.LDetectorStepMinimum),
            LDetectorKind.LDetectorKindLuminance => LDetector.LDetectorLuminanceMatch(
                lStep.LDetectorStepThreshold, lStep.LDetectorStepWindow, lStep.LDetectorStepMinimum),
            LDetectorKind.LDetectorKindVolume => LDetector.LDetectorPresetMatch(
                qSensorMetric, lStep.LDetectorStepThreshold, lStep.LDetectorStepWindow, lStep.LDetectorStepMinimum),
            _ => null
        };
    }

    public void QSensorStepSet(LDetectorStep lStep)
    {
        if (!qSensorSteps.ContainsKey(lStep.LDetectorStepKind))
        {
            return;
        }

        QSensorStepApply(QSensorNormalize(lStep));
    }

    public void QSensorEnabledSet(LDetectorKind lKind, bool lEnabled) =>
        QSensorStepApply(QSensorStepRead(lKind) with { LDetectorStepEnabled = lEnabled });

    public void QSensorThresholdSet(LDetectorKind lKind, double lThreshold) =>
        QSensorStepApply(QSensorNormalize(QSensorStepRead(lKind) with { LDetectorStepThreshold = lThreshold }));

    public void QSensorMinimumSet(LDetectorKind lKind, double lMinimum) =>
        QSensorStepApply(QSensorNormalize(QSensorStepRead(lKind) with { LDetectorStepMinimum = lMinimum }));

    public void QSensorWindowSet(LDetectorKind lKind, double lWindow) =>
        QSensorStepApply(QSensorNormalize(QSensorStepRead(lKind) with { LDetectorStepWindow = lWindow }));

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

        string? lMatch = QSensorMatchRead(LDetectorKind.LDetectorKindVolume);
        qSensorMetric = lMetric;
        if (lMatch is { } lToken && LDetector.LDetectorPresetRead(lToken) is { } lPreset)
        {
            LDetectorStep lStep = QSensorStepRead(LDetectorKind.LDetectorKindVolume);
            qSensorSteps[LDetectorKind.LDetectorKindVolume] = QSensorNormalize(lStep with
            {
                LDetectorStepThreshold = LDetector.LDetectorPresetResolve(lPreset, lMetric)
            });
        }

        QSensorChanged?.Invoke();
    }

    public void QSensorPresetSelect(LDetectorKind lKind, string lToken)
    {
        if (!QSensorPresetCheck(lKind) || !qSensorSteps.ContainsKey(lKind))
        {
            return;
        }

        LDetectorStep lStep = QSensorStepRead(lKind);
        LDetectorStep? lTarget = lKind switch
        {
            LDetectorKind.LDetectorKindScene => LDetector.LDetectorSceneResolve(lToken) is { } lSensitivity
                ? lStep with { LDetectorStepThreshold = lSensitivity }
                : null,
            LDetectorKind.LDetectorKindStill => LDetector.LDetectorStillResolve(lToken) is { } lStill
                ? lStep with
                {
                    LDetectorStepThreshold = lStill.LDetectorTolerance,
                    LDetectorStepMinimum = lStill.LDetectorMinimum
                }
                : null,
            LDetectorKind.LDetectorKindLuminance => LDetector.LDetectorLuminanceResolve(lToken) is { } lLuminance
                ? lStep with
                {
                    LDetectorStepThreshold = lLuminance.LDetectorThreshold,
                    LDetectorStepWindow = lLuminance.LDetectorWindow,
                    LDetectorStepMinimum = lLuminance.LDetectorMinimum
                }
                : null,
            _ => LDetector.LDetectorPresetRead(lToken) is { } lPreset
                ? lStep with
                {
                    LDetectorStepThreshold = LDetector.LDetectorPresetResolve(lPreset, qSensorMetric),
                    LDetectorStepWindow = lPreset.LDetectorPresetWindow,
                    LDetectorStepMinimum = lPreset.LDetectorPresetMinimum
                }
                : null
        };
        if (lTarget is not { } lResolved)
        {
            return;
        }

        LDetectorStep lNormal = QSensorNormalize(lResolved);
        if (qSensorSteps[lKind] == lNormal && qSensorTokens[lKind] == lToken)
        {
            return;
        }

        qSensorSteps[lKind] = lNormal;
        qSensorTokens[lKind] = lToken;
        QSensorChanged?.Invoke();
    }

    public void QSensorTokenSet(LDetectorKind lKind, string lToken)
    {
        if (!qSensorSteps.ContainsKey(lKind))
        {
            return;
        }

        string? lNormal = LDetector.LDetectorTokensRead(lKind).Contains(lToken) ? lToken : null;
        if (qSensorTokens[lKind] == lNormal)
        {
            return;
        }

        qSensorTokens[lKind] = lNormal;
        QSensorChanged?.Invoke();
    }

    public void QSensorRunningSet(bool lRunning)
    {
        if (qSensorRunning == lRunning)
        {
            return;
        }

        qSensorRunning = lRunning;
        QSensorRunningChanged?.Invoke(lRunning);
    }

    public void QSensorPersistentSet(bool lPersistent)
    {
        if (qSensorPersistent == lPersistent)
        {
            return;
        }

        qSensorPersistent = lPersistent;
        QSensorPersistentChanged?.Invoke(lPersistent);
    }

    private void QSensorStepApply(LDetectorStep lNormal)
    {
        if (qSensorSteps[lNormal.LDetectorStepKind] == lNormal)
        {
            return;
        }

        qSensorSteps[lNormal.LDetectorStepKind] = lNormal;
        QSensorChanged?.Invoke();
    }

    private static LDetectorStep QSensorNormalize(LDetectorStep lStep)
    {
        LDetectorKind lKind = lStep.LDetectorStepKind;
        return lStep with
        {
            LDetectorStepThreshold = lKind == LDetectorKind.LDetectorKindScene
                ? LDetector.LDetectorSensitivityClamp(lStep.LDetectorStepThreshold)
                : LDetector.LDetectorThresholdClamp(lKind, lStep.LDetectorStepThreshold),
            LDetectorStepMinimum = LDetector.LDetectorMinimumClamp(lKind, lStep.LDetectorStepMinimum),
            LDetectorStepWindow = LDetector.LDetectorWindowClamp(lKind, lStep.LDetectorStepWindow)
        };
    }

    private static string? QSensorSceneMatch(double lSensitivity)
    {
        foreach (string lToken in LDetector.LDetectorScenePresets)
        {
            if (LDetector.LDetectorSceneResolve(lToken) is { } lTarget && Math.Abs(lSensitivity - lTarget) < 0.5)
            {
                return lToken;
            }
        }

        return null;
    }
}
