using System.Collections.Generic;

namespace Cadroue.Core;

public sealed class LPresetRecord
{
    public string LPresetName { get; set; } = "MP4_H264_AAC_Default";
    public string LPresetDisplay { get; set; } = "OriginalName_export";
    public string LPresetContainer { get; set; } = "MP4";
    public string LPresetExtension { get; set; } = string.Empty;
    public string LPresetCollision { get; set; } = "Overwrite";
    public string LPresetCollisionSuffix { get; set; } = string.Empty;
    public string LPresetOutputSuffix { get; set; } = string.Empty;
    public string LPresetSourceSuffix { get; set; } = string.Empty;
    public string LPresetLocation { get; set; } = "Same as source";
    public string LPresetLocationFolder { get; set; } = string.Empty;
    public string LPresetLocationSubfolder { get; set; } = string.Empty;
    public string LPresetLocationSibling { get; set; } = string.Empty;
    public string LPresetLocationCustom { get; set; } = string.Empty;
    public LPresetVideoRecord LPresetVideo { get; set; } = new();
    public LPresetAudioRecord LPresetAudio { get; set; } = new();

    public LPresetRecord LPresetRecordNormalize()
    {
        var lPresetDefault = new LPresetRecord();
        LPresetName ??= lPresetDefault.LPresetName;
        LPresetDisplay ??= lPresetDefault.LPresetDisplay;
        LPresetContainer ??= lPresetDefault.LPresetContainer;
        LPresetExtension ??= lPresetDefault.LPresetExtension;
        LPresetCollision ??= lPresetDefault.LPresetCollision;
        LPresetCollisionSuffix ??= lPresetDefault.LPresetCollisionSuffix;
        LPresetOutputSuffix ??= lPresetDefault.LPresetOutputSuffix;
        LPresetSourceSuffix ??= lPresetDefault.LPresetSourceSuffix;
        LPresetLocation ??= lPresetDefault.LPresetLocation;
        LPresetLocationFolder ??= lPresetDefault.LPresetLocationFolder;
        LPresetLocationSubfolder ??= lPresetDefault.LPresetLocationSubfolder;
        LPresetLocationSibling ??= lPresetDefault.LPresetLocationSibling;
        LPresetLocationCustom ??= lPresetDefault.LPresetLocationCustom;
        LPresetVideo = (LPresetVideo ?? new LPresetVideoRecord()).LPresetVideoNormalize();
        LPresetAudio = (LPresetAudio ?? new LPresetAudioRecord()).LPresetAudioNormalize();
        return this;
    }
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

    public LPresetVideoRecord LPresetVideoNormalize()
    {
        var lPresetDefault = new LPresetVideoRecord();
        LPresetStream ??= lPresetDefault.LPresetStream;
        LPresetMode ??= lPresetDefault.LPresetMode;
        LPresetEncoder = LRepertoireCatalog.LRepertoireTextNormalize(LPresetEncoder ?? lPresetDefault.LPresetEncoder);
        LPresetRateControl ??= lPresetDefault.LPresetRateControl;
        LPresetQuality ??= lPresetDefault.LPresetQuality;
        LPresetSpeedPreset ??= lPresetDefault.LPresetSpeedPreset;
        LPresetSize ??= lPresetDefault.LPresetSize;
        LPresetFps ??= lPresetDefault.LPresetFps;
        LPresetPixelLayout ??= lPresetDefault.LPresetPixelLayout;
        LPresetExtras = LPresetExtrasNormalize(LPresetExtras);
        return this;
    }

    internal static Dictionary<string, string> LPresetExtrasNormalize(Dictionary<string, string>? lPresetExtras)
    {
        var lPresetNormalized = new Dictionary<string, string>(StringComparer.Ordinal);
        if (lPresetExtras is null)
        {
            return lPresetNormalized;
        }

        foreach ((string lPresetKey, string lPresetValue) in lPresetExtras)
        {
            if (!string.IsNullOrEmpty(lPresetKey))
            {
                lPresetNormalized[lPresetKey] = lPresetValue ?? string.Empty;
            }
        }

        return lPresetNormalized;
    }
}

public sealed class LPresetAudioRecord
{
    public string LPresetStream { get; set; } = "Include first audio track";
    public string LPresetMode { get; set; } = "Copy";
    public string LPresetEncoder { get; set; } = "AAC";
    public string LPresetRateControl { get; set; } = "Target bitrate";
    public string LPresetQuality { get; set; } = "192k";
    public string LPresetSpeed { get; set; } = string.Empty;
    public Dictionary<string, string> LPresetExtras { get; set; } = new();
    public string LPresetSampleRate { get; set; } = "Same as source";
    public string LPresetChannels { get; set; } = "Same as source";

    public LPresetAudioRecord LPresetAudioNormalize()
    {
        var lPresetDefault = new LPresetAudioRecord();
        LPresetStream ??= lPresetDefault.LPresetStream;
        LPresetMode ??= lPresetDefault.LPresetMode;
        LPresetEncoder ??= lPresetDefault.LPresetEncoder;
        LPresetRateControl ??= lPresetDefault.LPresetRateControl;
        LPresetQuality ??= lPresetDefault.LPresetQuality;
        LPresetSpeed ??= lPresetDefault.LPresetSpeed;
        LPresetSampleRate ??= lPresetDefault.LPresetSampleRate;
        LPresetChannels ??= lPresetDefault.LPresetChannels;
        LPresetExtras = LPresetVideoRecord.LPresetExtrasNormalize(LPresetExtras);
        return this;
    }
}

public enum LPresetOutcome
{
    LPresetMissing = 0,
    LPresetLoaded,
    LPresetUnreadable
}

public sealed record LPresetCatalog(LPresetOutcome LPresetOutcome, IReadOnlyList<LPresetRecord> LPresetRecords);
