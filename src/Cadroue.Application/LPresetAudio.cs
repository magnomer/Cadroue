namespace Cadroue.Application;

public sealed class LPresetAudio
{
    public string LPresetStream { get; set; } = "Include first audio track";
    public string LPresetMode { get; set; } = "Copy";
    public string LPresetEncoder { get; set; } = "AAC, native / aac";
    public string LPresetRateControl { get; set; } = "Target bitrate";
    public string LPresetQuality { get; set; } = "192k";
    public string LPresetSpeed { get; set; } = string.Empty;
    public Dictionary<string, string> LPresetExtras { get; set; } = new(StringComparer.Ordinal);
    public string LPresetSampleRate { get; set; } = "Same as source";
    public string LPresetChannels { get; set; } = "Same as source";

    public LPresetAudio LPresetAudioClone() => new()
    {
        LPresetStream = LPresetStream,
        LPresetMode = LPresetMode,
        LPresetEncoder = LPresetEncoder,
        LPresetRateControl = LPresetRateControl,
        LPresetQuality = LPresetQuality,
        LPresetSpeed = LPresetSpeed,
        LPresetExtras = new Dictionary<string, string>(LPresetExtras, StringComparer.Ordinal),
        LPresetSampleRate = LPresetSampleRate,
        LPresetChannels = LPresetChannels
    };
}
