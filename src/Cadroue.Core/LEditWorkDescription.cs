namespace Cadroue.Core;

public sealed record LEditWorkDescription(
    string? LEditSourcePath,
    TimeSpan LEditDuration,
    LWorkCrop LEditCrop,
    LWorkVideo LEditVideo,
    LEncoding LEditOutput);
