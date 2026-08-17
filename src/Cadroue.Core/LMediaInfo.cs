namespace Cadroue.Core;

public sealed record LMediaInfo
{
    public LMediaInfo(
        TimeSpan mediaInfoDuration,
        int mediaInfoVideoWidth,
        int mediaInfoVideoHeight,
        double mediaInfoVideoFrameRate,
        string mediaInfoVideoCodecName,
        bool mediaInfoAudioPresent,
        string mediaInfoAudioCodecName,
        int mediaInfoAudioSampleRate,
        int mediaInfoAudioChannels)
    {
        if (mediaInfoDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaInfoDuration), "Media duration must be greater than zero.");
        }

        bool mediaInfoVideoPresent = mediaInfoVideoWidth > 0 || mediaInfoVideoHeight > 0;
        if (mediaInfoVideoPresent && mediaInfoVideoWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaInfoVideoWidth), "Video width must be greater than zero when video is present.");
        }

        if (mediaInfoVideoPresent && mediaInfoVideoHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaInfoVideoHeight), "Video height must be greater than zero when video is present.");
        }

        if (!mediaInfoVideoPresent && !mediaInfoAudioPresent)
        {
            throw new ArgumentException("Media must contain at least one video or audio stream.");
        }

        if (mediaInfoVideoFrameRate < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaInfoVideoFrameRate), "Video frame rate cannot be negative.");
        }

        if (mediaInfoAudioPresent && mediaInfoAudioSampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaInfoAudioSampleRate), "Audio sample rate must be greater than zero when audio is present.");
        }

        if (mediaInfoAudioPresent && mediaInfoAudioChannels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mediaInfoAudioChannels), "Audio channels must be greater than zero when audio is present.");
        }

        LMediaInfoDuration = mediaInfoDuration;
        LMediaVideoWidth = mediaInfoVideoWidth;
        LMediaVideoHeight = mediaInfoVideoHeight;
        LMediaVideoRate = mediaInfoVideoFrameRate;
        LMediaVideoPresent = mediaInfoVideoPresent;
        LMediaVideoCodec = mediaInfoVideoPresent
            ? string.IsNullOrWhiteSpace(mediaInfoVideoCodecName) ? "unknown" : mediaInfoVideoCodecName
            : "";
        LMediaAudioPresent = mediaInfoAudioPresent;
        LMediaAudioCodec = mediaInfoAudioPresent
            ? string.IsNullOrWhiteSpace(mediaInfoAudioCodecName) ? "unknown" : mediaInfoAudioCodecName
            : "";
        LMediaSampleRate = mediaInfoAudioPresent ? mediaInfoAudioSampleRate : 0;
        LMediaAudioChannels = mediaInfoAudioPresent ? mediaInfoAudioChannels : 0;
    }

    public TimeSpan LMediaInfoDuration { get; }

    public TimeSpan LMediaStartTime { get; init; }

    public bool LMediaVideoPresent { get; }

    public bool LMediaAudioOnly => !LMediaVideoPresent && LMediaAudioPresent;

    public int LMediaVideoWidth { get; }

    public int LMediaVideoHeight { get; }

    public double LMediaVideoRate { get; }

    public TimeSpan LMediaVideoDuration { get; init; }

    public TimeSpan? LMediaVideoEnd { get; init; }

    public string LMediaVideoCodec { get; }

    public bool LMediaAudioPresent { get; }

    public string LMediaAudioCodec { get; }

    public int LMediaSampleRate { get; }

    public int LMediaAudioChannels { get; }

    public int LMediaAudioBitrate { get; init; }

    public string LMediaVideoPixel { get; init; } = "";

    public string LMediaVideoRange { get; init; } = "";
}
