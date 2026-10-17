namespace Cadroue.Core;

public sealed record LDetectorBundle(
    IReadOnlyList<LDetectorStep> LDetectorSetSteps,
    LDetectorBlank? LDetectorSetBlank,
    LDetectorStillMode? LDetectorSetStill,
    LDetectorLuminanceMode? LDetectorSetLuminance,
    LDetectorMetricMode? LDetectorSetMetric,
    IReadOnlyDictionary<LDetectorKind, string> LDetectorSetPresets);
