namespace Cadroue.Core;

public enum LDetectorKind
{
    LDetectorKindBlank,
    LDetectorKindScene,
    LDetectorKindStill,
    LDetectorKindLuminance,
    LDetectorKindSilence,
    LDetectorKindVolume
}

public enum LDetectorStillMode
{
    LDetectorStillDiscard,
    LDetectorStillTreat
}

public enum LDetectorLuminanceMode
{
    LDetectorLuminanceNormal,
    LDetectorLuminanceFast,
    LDetectorLuminanceFull
}

public readonly record struct LDetectorStep(
    LDetectorKind LDetectorStepKind,
    bool LDetectorStepEnabled,
    double LDetectorStepThreshold,
    double LDetectorStepMinimum,
    double LDetectorStepWindow);

public readonly record struct LDetectorBound(
    double LDetectorBoundLeast,
    double LDetectorBoundMost,
    double LDetectorBoundDefault);

public static class LDetector
{
    public static readonly IReadOnlyList<LDetectorKind> LDetectorKinds = new[]
    {
        LDetectorKind.LDetectorKindBlank,
        LDetectorKind.LDetectorKindScene,
        LDetectorKind.LDetectorKindStill,
        LDetectorKind.LDetectorKindLuminance,
        LDetectorKind.LDetectorKindSilence,
        LDetectorKind.LDetectorKindVolume
    };

    public static LDetectorBound LDetectorThresholdRead(LDetectorKind lDetectorKind) => lDetectorKind switch
    {
        LDetectorKind.LDetectorKindBlank => new LDetectorBound(0.80, 1.00, 0.98),
        LDetectorKind.LDetectorKindScene => new LDetectorBound(0, 100, 50),
        LDetectorKind.LDetectorKindStill => new LDetectorBound(0, 5, 0.1),
        LDetectorKind.LDetectorKindLuminance => new LDetectorBound(0, 50, 10),
        LDetectorKind.LDetectorKindSilence => new LDetectorBound(-80, 0, -30),
        LDetectorKind.LDetectorKindVolume => new LDetectorBound(-60, 0, -20),
        _ => new LDetectorBound(0, 1, 0)
    };

    public static LDetectorBound LDetectorMinimumRead(LDetectorKind lDetectorKind) => lDetectorKind switch
    {
        LDetectorKind.LDetectorKindBlank => new LDetectorBound(0, 60, LDetectorBlank.LDetectorBlankGap),
        LDetectorKind.LDetectorKindScene => new LDetectorBound(0, 30, 0.5),
        LDetectorKind.LDetectorKindStill => new LDetectorBound(0, 60, 0.5),
        LDetectorKind.LDetectorKindLuminance => new LDetectorBound(0, 10, 0.5),
        LDetectorKind.LDetectorKindSilence => new LDetectorBound(0, 60, 0.5),
        _ => new LDetectorBound(0, 60, 2.0)
    };

    public static LDetectorBound LDetectorWindowRead(LDetectorKind lDetectorKind) => lDetectorKind switch
    {
        LDetectorKind.LDetectorKindLuminance => new LDetectorBound(0.1, 5, 0.5),
        _ => new LDetectorBound(0, 0, 0)
    };

    public static LDetectorBound LDetectorToleranceRead() => new(0, 0.5, 0.05);

    public static LDetectorBound LDetectorCoverageRead() => new(0.5, 1.0, 0.98);

    public static LDetectorBound LDetectorBrightnessRead() => new(0, 1.0, 0);

    public static LDetectorStep LDetectorCreate(LDetectorKind lDetectorKind) => new(
        lDetectorKind,
        false,
        LDetectorThresholdRead(lDetectorKind).LDetectorBoundDefault,
        LDetectorMinimumRead(lDetectorKind).LDetectorBoundDefault,
        LDetectorWindowRead(lDetectorKind).LDetectorBoundDefault);

    private const double LDetectorSceneCeiling = 12.0;
    private const double LDetectorSceneFloor = 3.0;
    private const double LDetectorSceneLimit = 100.0;

    public static double LDetectorThresholdResolve(double lDetectorSensitivity)
    {
        double lDetectorThreshold = LDetectorSceneCeiling
            - (LDetectorSceneCeiling - LDetectorSceneFloor) * lDetectorSensitivity / 100.0;
        return Math.Clamp(lDetectorThreshold, 0.0, LDetectorSceneLimit);
    }

    public static double LDetectorSensitivityClamp(double lDetectorSensitivity)
    {
        double lDetectorThreshold = LDetectorThresholdResolve(lDetectorSensitivity);
        return (LDetectorSceneCeiling - lDetectorThreshold) * 100.0
            / (LDetectorSceneCeiling - LDetectorSceneFloor);
    }

    public static double LDetectorThresholdClamp(LDetectorKind lDetectorKind, double lDetectorValue)
    {
        LDetectorBound lDetectorBound = LDetectorThresholdRead(lDetectorKind);
        return Math.Clamp(lDetectorValue, lDetectorBound.LDetectorBoundLeast, lDetectorBound.LDetectorBoundMost);
    }

    public static double LDetectorMinimumClamp(LDetectorKind lDetectorKind, double lDetectorValue)
    {
        LDetectorBound lDetectorBound = LDetectorMinimumRead(lDetectorKind);
        return Math.Clamp(lDetectorValue, lDetectorBound.LDetectorBoundLeast, lDetectorBound.LDetectorBoundMost);
    }

    public static double LDetectorWindowClamp(LDetectorKind lDetectorKind, double lDetectorValue)
    {
        LDetectorBound lDetectorBound = LDetectorWindowRead(lDetectorKind);
        return Math.Clamp(lDetectorValue, lDetectorBound.LDetectorBoundLeast, lDetectorBound.LDetectorBoundMost);
    }
}
