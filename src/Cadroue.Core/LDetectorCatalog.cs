namespace Cadroue.Core;

public readonly record struct LDetectorPreset(
    string LDetectorPresetToken,
    double LDetectorPresetLufs,
    double LDetectorPresetDecibel,
    double LDetectorPresetWindow,
    double LDetectorPresetMinimum);

public readonly record struct LDetectorStillTuning(double LDetectorTolerance, double LDetectorMinimum);

public readonly record struct LDetectorLuminanceTuning(
    double LDetectorThreshold,
    double LDetectorWindow,
    double LDetectorMinimum);

public static class LDetectorCatalog
{
    public const string LDetectorTokenDefault = "Normal";

    public static IReadOnlyList<string> LDetectorTokensRead(LDetectorKind lDetectorKind) => lDetectorKind switch
    {
        LDetectorKind.LDetectorKindScene => LDetectorScenePresets,
        LDetectorKind.LDetectorKindStill => LDetectorStillPresets,
        LDetectorKind.LDetectorKindLuminance => LDetectorLuminancePresets,
        LDetectorKind.LDetectorKindVolume => LDetectorVolumePresets,
        _ => Array.Empty<string>()
    };

    public static readonly IReadOnlyList<LDetectorPreset> LDetectorPresets = new[]
    {
        new LDetectorPreset("Conservative", 24, 21, 2, 0.5),
        new LDetectorPreset("Normal", 20, 19, 2, 0.5),
        new LDetectorPreset("Sensitive", 16, 16, 2, 0.5)
    };

    private static readonly IReadOnlyList<string> LDetectorVolumePresets =
        LDetectorPresets.Select(lDetectorPreset => lDetectorPreset.LDetectorPresetToken).ToArray();

    public static LDetectorPreset? LDetectorPresetRead(string lDetectorToken)
    {
        foreach (LDetectorPreset lDetectorPreset in LDetectorPresets)
        {
            if (lDetectorPreset.LDetectorPresetToken == lDetectorToken)
            {
                return lDetectorPreset;
            }
        }

        return null;
    }

    public static double LDetectorPresetResolve(LDetectorPreset lDetectorPreset, LDetectorMetricMode lDetectorMetric) =>
        lDetectorMetric == LDetectorMetricMode.LDetectorMetricRms
            ? lDetectorPreset.LDetectorPresetDecibel
            : lDetectorPreset.LDetectorPresetLufs;

    public static string? LDetectorPresetMatch(
        LDetectorMetricMode lDetectorMetric,
        double lDetectorThreshold,
        double lDetectorWindow,
        double lDetectorMinimum)
    {
        foreach (LDetectorPreset lDetectorPreset in LDetectorPresets)
        {
            if (Math.Abs(lDetectorThreshold - LDetectorPresetResolve(lDetectorPreset, lDetectorMetric)) < 0.05
                && Math.Abs(lDetectorWindow - lDetectorPreset.LDetectorPresetWindow) < 0.05
                && Math.Abs(lDetectorMinimum - lDetectorPreset.LDetectorPresetMinimum) < 0.05)
            {
                return lDetectorPreset.LDetectorPresetToken;
            }
        }

        return null;
    }

    public static readonly IReadOnlyList<string> LDetectorScenePresets = new[]
    {
        "Conservative",
        "Normal",
        "Sensitive"
    };

    public static double? LDetectorSceneResolve(string lDetectorToken) => lDetectorToken switch
    {
        "Conservative" => 25.0,
        "Normal" => 50.0,
        "Sensitive" => 75.0,
        _ => null
    };

    public static string? LDetectorSceneMatch(double lDetectorSensitivity)
    {
        foreach (string lDetectorToken in LDetectorScenePresets)
        {
            if (LDetectorSceneResolve(lDetectorToken) is { } lDetectorTarget
                && Math.Abs(lDetectorSensitivity - lDetectorTarget) < 0.5)
            {
                return lDetectorToken;
            }
        }

        return null;
    }

    public static readonly IReadOnlyList<string> LDetectorStillPresets = new[]
    {
        "Conservative",
        "Normal",
        "Sensitive"
    };

    public static LDetectorStillTuning? LDetectorStillResolve(string lDetectorToken) => lDetectorToken switch
    {
        "Conservative" => new LDetectorStillTuning(0.05, 2.0),
        "Normal" => new LDetectorStillTuning(0.10, 1.0),
        "Sensitive" => new LDetectorStillTuning(0.50, 0.5),
        _ => null
    };

    public static string? LDetectorStillMatch(double lDetectorTolerance, double lDetectorMinimum)
    {
        foreach (string lDetectorToken in LDetectorStillPresets)
        {
            if (LDetectorStillResolve(lDetectorToken) is { } lDetectorStill
                && Math.Abs(lDetectorTolerance - lDetectorStill.LDetectorTolerance) < 0.05
                && Math.Abs(lDetectorMinimum - lDetectorStill.LDetectorMinimum) < 0.05)
            {
                return lDetectorToken;
            }
        }

        return null;
    }

    public static readonly IReadOnlyList<string> LDetectorLuminancePresets = new[]
    {
        "Conservative",
        "Normal",
        "Sensitive"
    };

    public static LDetectorLuminanceTuning? LDetectorLuminanceResolve(string lDetectorToken) => lDetectorToken switch
    {
        "Conservative" => new LDetectorLuminanceTuning(14.0, 1.0, 1.5),
        "Normal" => new LDetectorLuminanceTuning(8.0, 0.5, 0.5),
        "Sensitive" => new LDetectorLuminanceTuning(4.0, 0.3, 0.3),
        _ => null
    };

    public static string? LDetectorLuminanceMatch(
        double lDetectorThreshold,
        double lDetectorWindow,
        double lDetectorMinimum)
    {
        foreach (string lDetectorToken in LDetectorLuminancePresets)
        {
            if (LDetectorLuminanceResolve(lDetectorToken) is { } lDetectorLuminance
                && Math.Abs(lDetectorThreshold - lDetectorLuminance.LDetectorThreshold) < 0.05
                && Math.Abs(lDetectorWindow - lDetectorLuminance.LDetectorWindow) < 0.05
                && Math.Abs(lDetectorMinimum - lDetectorLuminance.LDetectorMinimum) < 0.05)
            {
                return lDetectorToken;
            }
        }

        return null;
    }

    public static string? LDetectorStepMatch(LDetectorStep lDetectorStep, LDetectorMetricMode lDetectorMetric) =>
        lDetectorStep.LDetectorStepKind switch
        {
            LDetectorKind.LDetectorKindScene => LDetectorSceneMatch(lDetectorStep.LDetectorStepThreshold),
            LDetectorKind.LDetectorKindStill => LDetectorStillMatch(
                lDetectorStep.LDetectorStepThreshold, lDetectorStep.LDetectorStepMinimum),
            LDetectorKind.LDetectorKindLuminance => LDetectorLuminanceMatch(
                lDetectorStep.LDetectorStepThreshold,
                lDetectorStep.LDetectorStepWindow,
                lDetectorStep.LDetectorStepMinimum),
            LDetectorKind.LDetectorKindVolume => LDetectorPresetMatch(
                lDetectorMetric,
                lDetectorStep.LDetectorStepThreshold,
                lDetectorStep.LDetectorStepWindow,
                lDetectorStep.LDetectorStepMinimum),
            _ => null
        };

    public static LDetectorStep? LDetectorStepResolve(
        LDetectorStep lDetectorStep,
        string lDetectorToken,
        LDetectorMetricMode lDetectorMetric) => lDetectorStep.LDetectorStepKind switch
    {
        LDetectorKind.LDetectorKindScene => LDetectorSceneResolve(lDetectorToken) is { } lDetectorSensitivity
            ? lDetectorStep with { LDetectorStepThreshold = lDetectorSensitivity }
            : null,
        LDetectorKind.LDetectorKindStill => LDetectorStillResolve(lDetectorToken) is { } lDetectorStill
            ? lDetectorStep with
            {
                LDetectorStepThreshold = lDetectorStill.LDetectorTolerance,
                LDetectorStepMinimum = lDetectorStill.LDetectorMinimum
            }
            : null,
        LDetectorKind.LDetectorKindLuminance => LDetectorLuminanceResolve(lDetectorToken) is { } lDetectorLuminance
            ? lDetectorStep with
            {
                LDetectorStepThreshold = lDetectorLuminance.LDetectorThreshold,
                LDetectorStepWindow = lDetectorLuminance.LDetectorWindow,
                LDetectorStepMinimum = lDetectorLuminance.LDetectorMinimum
            }
            : null,
        _ => LDetectorPresetRead(lDetectorToken) is { } lDetectorPreset
            ? lDetectorStep with
            {
                LDetectorStepThreshold = LDetectorPresetResolve(lDetectorPreset, lDetectorMetric),
                LDetectorStepWindow = lDetectorPreset.LDetectorPresetWindow,
                LDetectorStepMinimum = lDetectorPreset.LDetectorPresetMinimum
            }
            : null
    };
}
