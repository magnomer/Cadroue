namespace Cadroue.Core;

public sealed record LCargo(
    string LCargoSourcePath,
    LMediaInfo? LCargoMediaInfo,
    bool LCargoProcessable,
    bool LCargoPreviewAvailable,
    string? LCargoFfmpegError,
    string? LCargoPreviewError);
