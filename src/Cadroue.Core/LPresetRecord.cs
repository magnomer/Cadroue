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
    public string LPresetExportMode { get; set; } = "Smart export";
    public string LPresetVideoStream { get; set; } = "Include";
    public string LPresetAudioStream { get; set; } = "Include first audio track";
    public string LPresetVideoMode { get; set; } = "Auto";
    public string LPresetAudioMode { get; set; } = "Auto";

    public string LPresetVideoEncoder { get; set; } = "H.264, x264 / libx264";
    public string LPresetRateControl { get; set; } = "CRF (constant quality)";
    public string LPresetVideoQuality { get; set; } = "23";
    public string LPresetSpeedPreset { get; set; } = "medium";
    public string LPresetLocation { get; set; } = "Same as source";
    public string LPresetLocationFolder { get; set; } = string.Empty;
    public string LPresetVideoSize { get; set; } = "Same as source";
    public bool LPresetSizeReactive { get; set; }
    public string LPresetVideoFps { get; set; } = "Same as source";
    public string LPresetPixelLayout { get; set; } = "Auto";
    public Dictionary<string, string> LPresetVideoExtras { get; set; } = new();
    public string LPresetAudioEncoder { get; set; } = "AAC";
    public string LPresetAudioRateControl { get; set; } = "Target bitrate";
    public string LPresetAudioQuality { get; set; } = "192k";
    public string LPresetAudioSpeed { get; set; } = string.Empty;
    public Dictionary<string, string> LPresetAudioExtras { get; set; } = new();
    public string LPresetSampleRate { get; set; } = "Same as source";
    public string LPresetAudioChannels { get; set; } = "Same as source";
}
