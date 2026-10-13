namespace Cadroue.Application;

public sealed class LPresetVideo
{
    public string LPresetStream { get; set; } = "Include";
    public string LPresetMode { get; set; } = "Encode";
    public string LPresetEncoder { get; set; } = "H.264, x264 / libx264";
    public string LPresetRateControl { get; set; } = "CRF (constant quality)";
    public string LPresetQuality { get; set; } = "23";
    public string LPresetSpeedPreset { get; set; } = "medium";
    public string LPresetSize { get; set; } = "Same as source";
    public bool LPresetSizeReactive { get; set; }
    public string LPresetFps { get; set; } = "Same as source";
    public string LPresetPixelLayout { get; set; } = "Auto";
    public Dictionary<string, string> LPresetExtras { get; set; } = new(StringComparer.Ordinal);

    public LPresetVideo LPresetVideoClone() => new()
    {
        LPresetStream = LPresetStream,
        LPresetMode = LPresetMode,
        LPresetEncoder = LPresetEncoder,
        LPresetRateControl = LPresetRateControl,
        LPresetQuality = LPresetQuality,
        LPresetSpeedPreset = LPresetSpeedPreset,
        LPresetSize = LPresetSize,
        LPresetSizeReactive = LPresetSizeReactive,
        LPresetFps = LPresetFps,
        LPresetPixelLayout = LPresetPixelLayout,
        LPresetExtras = new Dictionary<string, string>(LPresetExtras, StringComparer.Ordinal)
    };
}
