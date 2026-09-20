using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record LSensorGroup(
    string LSensorGroupLabel,
    IReadOnlyList<string> LSensorGroupNames,
    bool LSensorGroupShown);

public sealed record LSensorPlan(
    LDetectorKind LSensorPlanKind,
    string LSensorPlanSwitch,
    string LSensorPlanTip,
    IReadOnlyList<LInspectorRow> LSensorPlanRows,
    LSensorGroup LSensorPlanMode,
    LSensorGroup LSensorPlanSpeed,
    LSensorGroup LSensorPlanMetric,
    string LSensorPlanChoice,
    bool LSensorPlanChoice__B)
{
    public const int LSensorPlanThreshold = 0;
    public const int LSensorPlanWindow = 1;
    public const int LSensorPlanMinimum = 2;

    private static readonly string[] LSensorModeKeys =
    {
        "Inspector.Detector.StillMode.Discard", "Inspector.Detector.StillMode.Treat"
    };

    private static readonly string[] LSensorSpeedKeys =
    {
        "Inspector.Detector.LuminanceMode.Fast",
        "Inspector.Detector.LuminanceMode.Normal",
        "Inspector.Detector.LuminanceMode.Full"
    };

    private static readonly string[] LSensorMetricKeys = { "Inspector.Metric.Lufs", "Inspector.Metric.Rms" };

    public static LSensorPlan LSensorPlanCreate(LDetectorKind lKind)
    {
        (string lLabelKey, string lUnit, string lFormat) = LSensorShapeRead(lKind);
        LDetectorBound lThreshold = LDetector.LDetectorThresholdRead(lKind);
        LDetectorBound lMinimum = LDetector.LDetectorMinimumRead(lKind);
        LDetectorBound lWindow = LDetector.LDetectorWindowRead(lKind);
        var lRows = new List<LInspectorRow>
        {
            LInspectorPlan.LInspectorRowCreate(
                LSensorPlanThreshold,
                lLabelKey,
                lUnit,
                lFormat,
                lThreshold.LDetectorBoundLeast,
                lThreshold.LDetectorBoundMost)
        };
        if (lKind is LDetectorKind.LDetectorKindLuminance or LDetectorKind.LDetectorKindVolume)
        {
            lRows.Add(LInspectorPlan.LInspectorRowCreate(
                LSensorPlanWindow,
                "Inspector.Detector.Window",
                "s",
                "0.0",
                lWindow.LDetectorBoundLeast,
                lWindow.LDetectorBoundMost));
        }

        lRows.Add(LInspectorPlan.LInspectorRowCreate(
            LSensorPlanMinimum,
            lKind == LDetectorKind.LDetectorKindBlank ? "Inspector.Detector.Minimum" : "Inspector.Detector.Minimal",
            "s",
            "0.0",
            lMinimum.LDetectorBoundLeast,
            lMinimum.LDetectorBoundMost));
        return new LSensorPlan(
            lKind,
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Detector.ApplyTooltip"),
            lRows,
            LSensorGroupCreate(
                "Inspector.Detector.StillMode", LSensorModeKeys, lKind == LDetectorKind.LDetectorKindStill),
            LSensorGroupCreate(
                "Inspector.Detector.LuminanceMode", LSensorSpeedKeys, lKind == LDetectorKind.LDetectorKindLuminance),
            LSensorGroupCreate(
                "Inspector.Detector.Metric", LSensorMetricKeys, lKind == LDetectorKind.LDetectorKindVolume),
            LLocalization.LLocalizationTextRead("Inspector.Common.Preset"),
            LSensor.LSensorPresetCheck(lKind));
    }

    public static string LSensorKeyRead(LDetectorKind lKind, string lToken) =>
        LDetector.LDetectorTokensRead(lKind).Contains(lToken)
            ? "Inspector.Detector." + lToken
            : "Inspector.Common.Custom";

    private static LSensorGroup LSensorGroupCreate(string lLabelKey, string[] lKeys, bool lShown) => new(
        LLocalization.LLocalizationTextRead(lLabelKey),
        lKeys.Select(LLocalization.LLocalizationTextRead).ToList(),
        lShown);

    private static (string, string, string) LSensorShapeRead(LDetectorKind lKind) => lKind switch
    {
        LDetectorKind.LDetectorKindBlank => ("Inspector.Detector.BlackRatio", string.Empty, "0.00"),
        LDetectorKind.LDetectorKindScene => ("Inspector.Detector.Sensitivity", string.Empty, "0"),
        LDetectorKind.LDetectorKindStill => ("Inspector.Detector.Tolerance", "%", "0.00"),
        LDetectorKind.LDetectorKindLuminance => ("Inspector.Detector.LuminanceChange", "%", "0"),
        LDetectorKind.LDetectorKindSilence => ("Inspector.Detector.Threshold", "dB", "0"),
        LDetectorKind.LDetectorKindVolume => ("Inspector.Detector.Threshold", "LU", "0"),
        _ => ("Inspector.Detector.Threshold", string.Empty, "0")
    };
}
