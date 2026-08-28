namespace Cadroue.Core;

public sealed record LFixWorkDescription(
    string? LFixSourcePath,
    TimeSpan LFixDuration,
    LWorkCrop LFixCrop,
    LWorkVideo LFixVideo,
    LEncoding LFixOutput);
