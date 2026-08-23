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

public readonly record struct LDetectorStep(
    LDetectorKind LDetectorStepKind,
    bool LDetectorStepEnabled,
    double LDetectorStepThreshold,
    double LDetectorStepMinimum);

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
        LDetectorKind.LDetectorKindScene => new LDetectorBound(0, 100, 10),
        LDetectorKind.LDetectorKindStill => new LDetectorBound(-80, 0, -60),
        LDetectorKind.LDetectorKindSilence => new LDetectorBound(-80, 0, -30),
        LDetectorKind.LDetectorKindVolume => new LDetectorBound(-60, 0, -20),
        _ => new LDetectorBound(0, 1, 0)
    };

    public static LDetectorBound LDetectorMinimumRead(LDetectorKind lDetectorKind) => lDetectorKind switch
    {
        LDetectorKind.LDetectorKindBlank => new LDetectorBound(0, 60, LDetectorBlank.LDetectorBlankGap),
        LDetectorKind.LDetectorKindScene => new LDetectorBound(0, 30, 0.5),
        _ => new LDetectorBound(0, 60, 2.0)
    };

    public static LDetectorBound LDetectorToleranceRead() => new(0, 0.5, 0.05);

    public static LDetectorBound LDetectorCoverageRead() => new(0.5, 1.0, 0.98);

    public static LDetectorBound LDetectorBrightnessRead() => new(0, 1.0, 0);

    public static LDetectorStep LDetectorCreate(LDetectorKind lDetectorKind) => new(
        lDetectorKind,
        false,
        LDetectorThresholdRead(lDetectorKind).LDetectorBoundDefault,
        LDetectorMinimumRead(lDetectorKind).LDetectorBoundDefault);

    private const double LDetectorSceneCurve = 3.0;

    public static double LDetectorPositionResolve(double lDetectorPosition)
    {
        double lDetectorNormal = Math.Clamp(lDetectorPosition, 0.0, 1.0);
        return Math.Pow(lDetectorNormal, LDetectorSceneCurve) * 100.0;
    }

    public static double LDetectorThresholdResolve(double lDetectorThreshold)
    {
        double lDetectorNormal = Math.Clamp(lDetectorThreshold, 0.0, 100.0) / 100.0;
        return Math.Pow(lDetectorNormal, 1.0 / LDetectorSceneCurve);
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
}
