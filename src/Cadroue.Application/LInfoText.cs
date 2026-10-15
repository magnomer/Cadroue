namespace Cadroue.Application;

public static class LInfoText
{
    private const int LInfoTextBudget = 80;

    public static string LInfoTextShorten(string lText) =>
        lText.Length <= LInfoTextBudget ? lText : lText[..LInfoTextBudget] + "…";

    public static string LInfoDurationFormat(TimeSpan lDuration) =>
        lDuration.TotalHours >= 1
            ? $"{(int)lDuration.TotalHours:D2}:{lDuration.Minutes:D2}:{lDuration.Seconds:D2}"
            : $"{lDuration.Minutes:D2}:{lDuration.Seconds:D2}";

    public static string LInfoCodecFormat(string lCodecName)
    {
        if (string.IsNullOrWhiteSpace(lCodecName))
        {
            return string.Empty;
        }

        return lCodecName.Trim().ToLowerInvariant() switch
        {
            "h264" or "avc" => "H.264",
            "hevc" or "h265" => "H.265",
            "aac" => "AAC",
            "mp3" => "MP3",
            "flac" => "FLAC",
            "pcm_s16le" => "PCM",
            _ => lCodecName.ToUpperInvariant()
        };
    }

    public static string LInfoChannelFormat(int lChannelCount) => lChannelCount switch
    {
        1 => LLocalization.LLocalizationTextRead("Encoder.Value.Mono"),
        2 => LLocalization.LLocalizationTextRead("Encoder.Value.Stereo"),
        _ => $"{lChannelCount}ch"
    };
}
