namespace Cadroue.Core;

public sealed record LWorkMedia(
    int LWorkMediaWidth,
    int LWorkMediaHeight,
    double LWorkMediaFramerate,
    long LWorkMediaMilliseconds,
    bool LWorkMediaVideo)
{
    public TimeSpan LWorkMediaDuration => TimeSpan.FromMilliseconds(LWorkMediaMilliseconds);

    public double? LWorkKeyframeInterval { get; init; }

    public double? LWorkMediaLoudness { get; init; }

    public string LWorkMediaCodec { get; init; } = "";

    public string LWorkAudioCodec { get; init; } = "";

    public int LWorkMediaBitrate { get; init; }

    public int LWorkMediaSamplerate { get; init; }

    public string LWorkMediaPixel { get; init; } = "";

    public string LWorkMediaRange { get; init; } = "";
}
