using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record QSensorGroup(
    string QSensorGroupLabel,
    IReadOnlyList<string> QSensorGroupNames,
    bool QSensorGroupShown);

public sealed record QSensorPlan(
    LDetectorKind QSensorPlanKind,
    string QSensorPlanSwitch,
    string QSensorPlanTip,
    IReadOnlyList<QInspectorRow> QSensorPlanRows,
    QSensorGroup QSensorPlanMode,
    QSensorGroup QSensorPlanSpeed,
    QSensorGroup QSensorPlanMetric,
    string QSensorPlanChoice,
    bool QSensorPlanPreset)
{
    public const int QSensorPlanThreshold = 0;
    public const int QSensorPlanWindow = 1;
    public const int QSensorPlanMinimum = 2;

    private static readonly string[] QSensorModeKeys =
    {
        "Inspector.Detector.StillMode.Discard", "Inspector.Detector.StillMode.Treat"
    };

    private static readonly string[] QSensorSpeedKeys =
    {
        "Inspector.Detector.LuminanceMode.Fast",
        "Inspector.Detector.LuminanceMode.Normal",
        "Inspector.Detector.LuminanceMode.Full"
    };

    private static readonly string[] QSensorMetricKeys = { "Inspector.Metric.Lufs", "Inspector.Metric.Rms" };

    public static QSensorPlan QSensorPlanCreate(LDetectorKind lKind)
    {
        (string lLabelKey, string lUnit, string lFormat) = QSensorShapeRead(lKind);
        LDetectorBound lThreshold = LDetector.LDetectorThresholdRead(lKind);
        LDetectorBound lMinimum = LDetector.LDetectorMinimumRead(lKind);
        LDetectorBound lWindow = LDetector.LDetectorWindowRead(lKind);
        var lRows = new List<QInspectorRow>
        {
            QInspectorPlan.QInspectorRowCreate(
                QSensorPlanThreshold,
                lLabelKey,
                lUnit,
                lFormat,
                lThreshold.LDetectorBoundLeast,
                lThreshold.LDetectorBoundMost)
        };
        if (lKind is LDetectorKind.LDetectorKindLuminance or LDetectorKind.LDetectorKindVolume)
        {
            lRows.Add(QInspectorPlan.QInspectorRowCreate(
                QSensorPlanWindow,
                "Inspector.Detector.Window",
                "s",
                "0.0",
                lWindow.LDetectorBoundLeast,
                lWindow.LDetectorBoundMost));
        }

        lRows.Add(QInspectorPlan.QInspectorRowCreate(
            QSensorPlanMinimum,
            lKind == LDetectorKind.LDetectorKindBlank ? "Inspector.Detector.Minimum" : "Inspector.Detector.Minimal",
            "s",
            "0.0",
            lMinimum.LDetectorBoundLeast,
            lMinimum.LDetectorBoundMost));
        return new QSensorPlan(
            lKind,
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Detector.ApplyTooltip"),
            lRows,
            QSensorGroupCreate(
                "Inspector.Detector.StillMode", QSensorModeKeys, lKind == LDetectorKind.LDetectorKindStill),
            QSensorGroupCreate(
                "Inspector.Detector.LuminanceMode", QSensorSpeedKeys, lKind == LDetectorKind.LDetectorKindLuminance),
            QSensorGroupCreate(
                "Inspector.Detector.Metric", QSensorMetricKeys, lKind == LDetectorKind.LDetectorKindVolume),
            LLocalization.LLocalizationTextRead("Inspector.Common.Preset"),
            QSensor.QSensorPresetCheck(lKind));
    }

    public static string QSensorKeyRead(LDetectorKind lKind, string lToken) =>
        LDetector.LDetectorTokensRead(lKind).Contains(lToken)
            ? "Inspector.Detector." + lToken
            : "Inspector.Common.Custom";

    private static QSensorGroup QSensorGroupCreate(string lLabelKey, string[] lKeys, bool lShown) => new(
        LLocalization.LLocalizationTextRead(lLabelKey),
        lKeys.Select(LLocalization.LLocalizationTextRead).ToList(),
        lShown);

    private static (string, string, string) QSensorShapeRead(LDetectorKind lKind) => lKind switch
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
