using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LSensor
{
    private const string LSensorBlank = "Blank";
    private const string LSensorScene = "Scene";
    private const string LSensorStill = "Still Image";
    private const string LSensorLuminance = "Luminance";
    private const string LSensorSilence = "Silence";
    private const string LSensorVolume = "Volume";

    private readonly Dictionary<LDetectorKind, LDetectorStep> lSensorSteps = new();
    private readonly Dictionary<LDetectorKind, string?> lSensorTokens = new();
    private LDetectorStillMode lSensorMode = LDetectorStillMode.LDetectorStillDiscard;
    private LDetectorLuminanceMode lSensorSpeed = LDetectorLuminanceMode.LDetectorLuminanceNormal;
    private LDetectorMetricMode lSensorMetric = LDetectorMetricMode.LDetectorMetricLufs;
    private bool lSensorRunning;
    private bool lSensorPersistent;

    public event Action? LSensorChange;
    public event Action<bool>? LSensorRunningChange;
    public event Action<bool>? LSensorPersistentChange;

    public LSensor()
    {
        foreach (LDetectorKind lKind in LDetector.LDetectorKinds)
        {
            if (lKind == LDetectorKind.LDetectorKindBlank)
            {
                continue;
            }

            lSensorSteps[lKind] = LDetector.LDetectorCreate(lKind);
            lSensorTokens[lKind] = LSensorPresetCheck(lKind) ? LDetector.LDetectorTokenDefault : null;
        }
    }

    public LDetectorStillMode LSensorMode => lSensorMode;

    public LDetectorLuminanceMode LSensorSpeed => lSensorSpeed;

    public LDetectorMetricMode LSensorMetric => lSensorMetric;

    public bool LSensorRunning => lSensorRunning;

    public bool LSensorPersistent => lSensorPersistent;

    public static bool LSensorPresetCheck(LDetectorKind lKind) => LDetector.LDetectorTokensRead(lKind).Count > 0;

    public static string LSensorNameRead(LDetectorKind lKind) => lKind switch
    {
        LDetectorKind.LDetectorKindBlank => LSensorBlank,
        LDetectorKind.LDetectorKindScene => LSensorScene,
        LDetectorKind.LDetectorKindStill => LSensorStill,
        LDetectorKind.LDetectorKindLuminance => LSensorLuminance,
        LDetectorKind.LDetectorKindSilence => LSensorSilence,
        LDetectorKind.LDetectorKindVolume => LSensorVolume,
        _ => string.Empty
    };

    public static LDetectorKind? LSensorKindRead(string? lName) => lName switch
    {
        LSensorBlank => LDetectorKind.LDetectorKindBlank,
        LSensorScene => LDetectorKind.LDetectorKindScene,
        LSensorStill => LDetectorKind.LDetectorKindStill,
        LSensorLuminance => LDetectorKind.LDetectorKindLuminance,
        LSensorSilence => LDetectorKind.LDetectorKindSilence,
        LSensorVolume => LDetectorKind.LDetectorKindVolume,
        _ => null
    };

    public LDetectorStep LSensorStepRead(LDetectorKind lKind) =>
        lSensorSteps.TryGetValue(lKind, out LDetectorStep lStep) ? lStep : LDetector.LDetectorCreate(lKind);

    public string? LSensorTokenRead(LDetectorKind lKind) => lSensorTokens.GetValueOrDefault(lKind);

    public string? LSensorMatchRead(LDetectorKind lKind)
    {
        LDetectorStep lStep = LSensorStepRead(lKind);
        return lKind switch
        {
            LDetectorKind.LDetectorKindScene => LSensorSceneMatch(lStep.LDetectorStepThreshold),
            LDetectorKind.LDetectorKindStill => LDetector.LDetectorStillMatch(
                lStep.LDetectorStepThreshold, lStep.LDetectorStepMinimum),
            LDetectorKind.LDetectorKindLuminance => LDetector.LDetectorLuminanceMatch(
                lStep.LDetectorStepThreshold, lStep.LDetectorStepWindow, lStep.LDetectorStepMinimum),
            LDetectorKind.LDetectorKindVolume => LDetector.LDetectorPresetMatch(
                lSensorMetric, lStep.LDetectorStepThreshold, lStep.LDetectorStepWindow, lStep.LDetectorStepMinimum),
            _ => null
        };
    }

    public void LSensorStepSet(LDetectorStep lStep)
    {
        if (!lSensorSteps.ContainsKey(lStep.LDetectorStepKind))
        {
            return;
        }

        LSensorStepApply(LSensorNormalize(lStep));
    }

    public void LSensorEnabledSet(LDetectorKind lKind, bool lEnabled) =>
        LSensorStepApply(LSensorStepRead(lKind) with { LDetectorStepEnabled = lEnabled });

    public void LSensorThresholdSet(LDetectorKind lKind, double lThreshold) =>
        LSensorStepApply(LSensorNormalize(LSensorStepRead(lKind) with { LDetectorStepThreshold = lThreshold }));

    public void LSensorMinimumSet(LDetectorKind lKind, double lMinimum) =>
        LSensorStepApply(LSensorNormalize(LSensorStepRead(lKind) with { LDetectorStepMinimum = lMinimum }));

    public void LSensorWindowSet(LDetectorKind lKind, double lWindow) =>
        LSensorStepApply(LSensorNormalize(LSensorStepRead(lKind) with { LDetectorStepWindow = lWindow }));

    public void LSensorModeSet(LDetectorStillMode lMode)
    {
        if (lSensorMode == lMode)
        {
            return;
        }

        lSensorMode = lMode;
        LSensorChange?.Invoke();
    }

    public void LSensorSpeedSet(LDetectorLuminanceMode lSpeed)
    {
        if (lSensorSpeed == lSpeed)
        {
            return;
        }

        lSensorSpeed = lSpeed;
        LSensorChange?.Invoke();
    }

    public void LSensorMetricSet(LDetectorMetricMode lMetric)
    {
        if (lSensorMetric == lMetric)
        {
            return;
        }

        string? lMatch = LSensorMatchRead(LDetectorKind.LDetectorKindVolume);
        lSensorMetric = lMetric;
        if (lMatch is { } lToken && LDetector.LDetectorPresetRead(lToken) is { } lPreset)
        {
            LDetectorStep lStep = LSensorStepRead(LDetectorKind.LDetectorKindVolume);
            lSensorSteps[LDetectorKind.LDetectorKindVolume] = LSensorNormalize(lStep with
            {
                LDetectorStepThreshold = LDetector.LDetectorPresetResolve(lPreset, lMetric)
            });
        }

        LSensorChange?.Invoke();
    }

    public void LSensorPresetSelect(LDetectorKind lKind, string lToken)
    {
        if (!LSensorPresetCheck(lKind) || !lSensorSteps.ContainsKey(lKind))
        {
            return;
        }

        LDetectorStep lStep = LSensorStepRead(lKind);
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
                    LDetectorStepThreshold = LDetector.LDetectorPresetResolve(lPreset, lSensorMetric),
                    LDetectorStepWindow = lPreset.LDetectorPresetWindow,
                    LDetectorStepMinimum = lPreset.LDetectorPresetMinimum
                }
                : null
        };
        if (lTarget is not { } lResolved)
        {
            return;
        }

        LDetectorStep lNormal = LSensorNormalize(lResolved);
        if (lSensorSteps[lKind] == lNormal && lSensorTokens[lKind] == lToken)
        {
            return;
        }

        lSensorSteps[lKind] = lNormal;
        lSensorTokens[lKind] = lToken;
        LSensorChange?.Invoke();
    }

    public void LSensorTokenSet(LDetectorKind lKind, string lToken)
    {
        if (!lSensorSteps.ContainsKey(lKind))
        {
            return;
        }

        string? lNormal = LDetector.LDetectorTokensRead(lKind).Contains(lToken) ? lToken : null;
        if (lSensorTokens[lKind] == lNormal)
        {
            return;
        }

        lSensorTokens[lKind] = lNormal;
        LSensorChange?.Invoke();
    }

    public void LSensorRunningSet(bool lRunning)
    {
        if (lSensorRunning == lRunning)
        {
            return;
        }

        lSensorRunning = lRunning;
        LSensorRunningChange?.Invoke(lRunning);
    }

    public void LSensorPersistentSet(bool lPersistent)
    {
        if (lSensorPersistent == lPersistent)
        {
            return;
        }

        lSensorPersistent = lPersistent;
        LSensorPersistentChange?.Invoke(lPersistent);
    }

    private void LSensorStepApply(LDetectorStep lNormal)
    {
        if (lSensorSteps[lNormal.LDetectorStepKind] == lNormal)
        {
            return;
        }

        lSensorSteps[lNormal.LDetectorStepKind] = lNormal;
        LSensorChange?.Invoke();
    }

    private static LDetectorStep LSensorNormalize(LDetectorStep lStep)
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

    private static string? LSensorSceneMatch(double lSensitivity)
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
