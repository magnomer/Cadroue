namespace Cadroue.Core;

public sealed record LMediaProbeResult(
    string LMediaProbeSourcePath,
    LMediaInfo? LMediaProbeInfo,
    string? LMediaProbeError);
