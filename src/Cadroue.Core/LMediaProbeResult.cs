namespace Cadroue.Core;

public sealed record LMediaProbeResult(
    string LMediaProbePath,
    LMediaInfo? LMediaProbeInfo,
    string? LMediaProbeError);
