using System.Collections.Generic;
using System.Linq;
using Cadroue.Core;

namespace Cadroue.Conduct;

public sealed record CDetectorGroup(
    string CDetectorGroupLabel,
    IReadOnlyList<string> CDetectorGroupNames,
    bool CDetectorGroupShown);

public sealed record CDetectorPlan(
    LDetectorKind CDetectorPlanKind,
    string CDetectorPlanSwitch,
    string CDetectorPlanTip,
    string CDetectorPlanLabel,
    string CDetectorPlanUnit,
    string CDetectorPlanPattern,
    LDetectorBound CDetectorPlanThreshold,
    LDetectorBound? CDetectorPlanWindow,
    LDetectorBound CDetectorPlanMinimum,
    CDetectorGroup CDetectorPlanMode,
    CDetectorGroup CDetectorPlanSpeed,
    CDetectorGroup CDetectorPlanMetric,
    string CDetectorPlanChoice,
    bool CDetectorPlanPreset)
{
    private static readonly string[] CDetectorModeKeys =
    {
        "Inspector.Detector.StillMode.Discard", "Inspector.Detector.StillMode.Treat"
    };

    private static readonly string[] CDetectorSpeedKeys =
    {
        "Inspector.Detector.LuminanceMode.Fast",
        "Inspector.Detector.LuminanceMode.Normal",
        "Inspector.Detector.LuminanceMode.Full"
    };

    private static readonly string[] CDetectorMetricKeys = { "Inspector.Metric.Lufs", "Inspector.Metric.Rms" };

    public static CDetectorPlan CDetectorPlanCreate(LDetectorKind kind)
    {
        (string label, string unit, string pattern) = CDetectorShapeRead(kind);
        LDetectorBound threshold = LDetector.LDetectorThresholdRead(kind);
        LDetectorBound minimum = LDetector.LDetectorMinimumRead(kind);
        LDetectorBound window = LDetector.LDetectorWindowRead(kind);
        return new CDetectorPlan(
            kind,
            "Inspector.Common.Apply",
            "Inspector.Detector.ApplyTooltip",
            label,
            unit,
            pattern,
            threshold,
            kind is LDetectorKind.LDetectorKindLuminance or LDetectorKind.LDetectorKindVolume
                ? window
                : null,
            minimum,
            CDetectorGroupCreate(
                "Inspector.Detector.StillMode", CDetectorModeKeys, kind == LDetectorKind.LDetectorKindStill),
            CDetectorGroupCreate(
                "Inspector.Detector.LuminanceMode",
                CDetectorSpeedKeys,
                kind == LDetectorKind.LDetectorKindLuminance),
            CDetectorGroupCreate(
                "Inspector.Detector.Metric", CDetectorMetricKeys, kind == LDetectorKind.LDetectorKindVolume),
            "Inspector.Common.Preset",
            CDetectorPreset.CDetectorPresetCheck(kind));
    }

    public static string CDetectorKeyRead(LDetectorKind kind, string token) =>
        LDetectorCatalog.LDetectorTokensRead(kind).Contains(token)
            ? "Inspector.Detector." + token
            : "Inspector.Common.Custom";

    private static CDetectorGroup CDetectorGroupCreate(string label, string[] names, bool shown) =>
        new(label, names.ToList().AsReadOnly(), shown);

    private static (string, string, string) CDetectorShapeRead(LDetectorKind kind) => kind switch
    {
        LDetectorKind.LDetectorKindBlank => ("Inspector.Detector.BlackRatio", string.Empty, "0.00"),
        LDetectorKind.LDetectorKindScene => ("Inspector.Detector.Sensitivity", string.Empty, "0"),
        LDetectorKind.LDetectorKindStill => ("Inspector.Detector.Tolerance", "%", "0.00"),
        LDetectorKind.LDetectorKindLuminance => ("Inspector.Detector.LuminanceChange", "%", "0"),
        LDetectorKind.LDetectorKindSilence => ("Inspector.Detector.Threshold", "dB", "0"),
        LDetectorKind.LDetectorKindVolume => ("Inspector.Detector.Threshold", "LU", "0"),
        _ => ("Inspector.Detector.Threshold", string.Empty, "0")
    };

    public static string CDetectorNameRead(LDetectorKind kind) => kind switch
    {
        LDetectorKind.LDetectorKindBlank => "Blank",
        LDetectorKind.LDetectorKindScene => "Scene",
        LDetectorKind.LDetectorKindStill => "Still Image",
        LDetectorKind.LDetectorKindLuminance => "Luminance",
        LDetectorKind.LDetectorKindSilence => "Silence",
        LDetectorKind.LDetectorKindVolume => "Volume",
        _ => string.Empty
    };

    public static LDetectorKind? CDetectorKindRead(string? name) => name switch
    {
        "Blank" => LDetectorKind.LDetectorKindBlank,
        "Scene" => LDetectorKind.LDetectorKindScene,
        "Still Image" => LDetectorKind.LDetectorKindStill,
        "Luminance" => LDetectorKind.LDetectorKindLuminance,
        "Silence" => LDetectorKind.LDetectorKindSilence,
        "Volume" => LDetectorKind.LDetectorKindVolume,
        _ => null
    };
}
