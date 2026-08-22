using System.Collections.Generic;

namespace Cadroue.Core;

public sealed class LPresetRecord
{
    public string LPresetName { get; set; } = "MP4_H264_AAC_Default";
    public string LPresetDisplay { get; set; } = "OriginalName_export";
    public string LPresetContainer { get; set; } = "MP4";
    public string LPresetExtension { get; set; } = string.Empty;
    public string LPresetCollision { get; set; } = "Overwrite";
    public string LPresetCollisionSuffix { get; set; } = "_1";
    public string LPresetLocation { get; set; } = "Same as source";
    public string LPresetLocationFolder { get; set; } = string.Empty;
    public LPresetVideoRecord LPresetVideo { get; set; } = new();
    public LPresetAudioRecord LPresetAudio { get; set; } = new();
}

public sealed class LPresetVideoRecord
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
    public Dictionary<string, string> LPresetExtras { get; set; } = new();
}

public sealed class LPresetAudioRecord
{
    public string LPresetStream { get; set; } = "Include first audio track";
    public string LPresetMode { get; set; } = "Auto";
    public string LPresetEncoder { get; set; } = "AAC";
    public string LPresetRateControl { get; set; } = "Target bitrate";
    public string LPresetQuality { get; set; } = "192k";
    public string LPresetSpeed { get; set; } = string.Empty;
    public Dictionary<string, string> LPresetExtras { get; set; } = new();
    public string LPresetSampleRate { get; set; } = "Same as source";
    public string LPresetChannels { get; set; } = "Same as source";
}
