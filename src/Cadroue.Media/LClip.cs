namespace Cadroue.Media;

public sealed record LClip(
    int LClipIndex,
    decimal? LClipStartSeconds,
    decimal? LClipEndSeconds,
    string LClipName,
    bool LClipStartSpecified,
    bool LClipEndSpecified,
    bool LClipObject);
