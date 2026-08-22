using Cadroue.Core;

namespace Cadroue.Application;

public sealed partial class LPreset
{
    public string LPresetName { get; set; } = "MP4_H264_AAC_Default";
    public string LPresetDisplay { get; set; } = "{OriginalName}_export";
    public string LPresetContainer { get; set; } = "MP4";
    public string LPresetExtension { get; set; } = "mp4";
    public string LPresetCollision { get; set; } = "Overwrite";
    public string LPresetCollisionSuffix { get; set; } = "_1";
    public string LPresetLocation { get; set; } = "Same as source";
    public string LPresetLocationSubfolder { get; set; } = string.Empty;
    public string LPresetLocationSibling { get; set; } = string.Empty;
    public string LPresetLocationCustom { get; set; } = string.Empty;

    public LPresetVideo LPresetVideo { get; set; } = new();
    public LPresetAudio LPresetAudio { get; set; } = new();

    public string LPresetVideoSummary => $"{LPresetVideo.LPresetMode} ({LPresetVideo.LPresetStream})";
    public string LPresetAudioSummary => $"{LPresetAudio.LPresetMode} ({LAudioStreamSummary})";
    public string LPresetOutputSummary => string.IsNullOrWhiteSpace(LPresetExtension) ? LPresetDisplay : $"{LPresetDisplay}.{LPresetExtension}";

    public string LPresetLocationRead(string lMode) => lMode switch
    {
        "Sibling" => LPresetLocationSibling,
        "Custom location" or "Custom folder" => LPresetLocationCustom,
        "Subfolder" => LPresetLocationSubfolder,
        _ => string.Empty
    };

    public void LPresetLocationSet(string lMode, string lFolder)
    {
        switch (lMode)
        {
            case "Sibling":
                LPresetLocationSibling = lFolder;
                break;
            case "Custom location":
            case "Custom folder":
                LPresetLocationCustom = lFolder;
                break;
            case "Subfolder":
                LPresetLocationSubfolder = lFolder;
                break;
        }
    }

    public LEncoding LPresetOutputCreate() => new(
        LPresetDisplay,
        LPresetContainer,
        LPresetExtension,
        LPresetLocation,
        LPresetLocationRead(LPresetLocation),
        new LEncodingVideo(
            LPresetVideo.LPresetStream,
            LPresetVideo.LPresetMode,
            LPresetVideo.LPresetEncoder,
            LPresetVideo.LPresetRateControl,
            LPresetVideo.LPresetQuality,
            LPresetVideo.LPresetSpeedPreset,
            LPresetVideo.LPresetSize,
            LPresetVideo.LPresetSizeReactive,
            LPresetVideo.LPresetFps,
            LPresetVideo.LPresetPixelLayout,
            new Dictionary<string, string>(LPresetVideo.LPresetExtras, StringComparer.Ordinal)),
        new LEncodingAudio(
            LPresetAudio.LPresetStream,
            LPresetAudio.LPresetMode,
            LPresetAudio.LPresetEncoder,
            LPresetAudio.LPresetRateControl,
            LPresetAudio.LPresetQuality,
            LPresetAudio.LPresetSpeed,
            new Dictionary<string, string>(LPresetAudio.LPresetExtras, StringComparer.Ordinal),
            LPresetAudio.LPresetSampleRate,
            LPresetAudio.LPresetChannels),
        LPresetName,
        LPresetCollision,
        LPresetCollisionSuffix);

    public LPreset LPresetClone() => new()
    {
        LPresetName = LPresetName,
        LPresetDisplay = LPresetDisplay,
        LPresetContainer = LPresetContainer,
        LPresetExtension = LPresetExtension,
        LPresetCollision = LPresetCollision,
        LPresetCollisionSuffix = LPresetCollisionSuffix,
        LPresetLocation = LPresetLocation,
        LPresetLocationSubfolder = LPresetLocationSubfolder,
        LPresetLocationSibling = LPresetLocationSibling,
        LPresetLocationCustom = LPresetLocationCustom,
        LPresetVideo = LPresetVideo.LPresetVideoClone(),
        LPresetAudio = LPresetAudio.LPresetAudioClone()
    };

    public event Action? LPresetChange;

    public void LPresetCopy(LPreset lSource)
    {
        LPresetName = lSource.LPresetName;
        LPresetDisplay = lSource.LPresetDisplay;
        LPresetContainer = lSource.LPresetContainer;
        LPresetExtension = lSource.LPresetExtension;
        LPresetCollision = lSource.LPresetCollision;
        LPresetCollisionSuffix = lSource.LPresetCollisionSuffix;
        LPresetLocation = lSource.LPresetLocation;
        LPresetLocationSubfolder = lSource.LPresetLocationSubfolder;
        LPresetLocationSibling = lSource.LPresetLocationSibling;
        LPresetLocationCustom = lSource.LPresetLocationCustom;
        LPresetVideo = lSource.LPresetVideo.LPresetVideoClone();
        LPresetAudio = lSource.LPresetAudio.LPresetAudioClone();
        LPresetChange?.Invoke();
    }

    public LPresetRecord LPresetRecordCreate() => new()
    {
        LPresetName = LPresetName,
        LPresetDisplay = LPresetDisplay,
        LPresetContainer = LPresetContainer,
        LPresetExtension = LPresetExtension,
        LPresetCollision = LPresetCollision,
        LPresetCollisionSuffix = LPresetCollisionSuffix,
        LPresetLocation = LPresetLocation,
        LPresetLocationSubfolder = LPresetLocationSubfolder,
        LPresetLocationSibling = LPresetLocationSibling,
        LPresetLocationCustom = LPresetLocationCustom,
        LPresetVideo = new LPresetVideoRecord
        {
            LPresetStream = LPresetVideo.LPresetStream,
            LPresetMode = LPresetVideo.LPresetMode,
            LPresetEncoder = LPresetVideo.LPresetEncoder,
            LPresetRateControl = LPresetVideo.LPresetRateControl,
            LPresetQuality = LPresetVideo.LPresetQuality,
            LPresetSpeedPreset = LPresetVideo.LPresetSpeedPreset,
            LPresetSize = LPresetVideo.LPresetSize,
            LPresetSizeReactive = LPresetVideo.LPresetSizeReactive,
            LPresetFps = LPresetVideo.LPresetFps,
            LPresetPixelLayout = LPresetVideo.LPresetPixelLayout,
            LPresetExtras = new Dictionary<string, string>(LPresetVideo.LPresetExtras)
        },
        LPresetAudio = new LPresetAudioRecord
        {
            LPresetStream = LPresetAudio.LPresetStream,
            LPresetMode = LPresetAudio.LPresetMode,
            LPresetEncoder = LPresetAudio.LPresetEncoder,
            LPresetRateControl = LPresetAudio.LPresetRateControl,
            LPresetQuality = LPresetAudio.LPresetQuality,
            LPresetSpeed = LPresetAudio.LPresetSpeed,
            LPresetExtras = new Dictionary<string, string>(LPresetAudio.LPresetExtras, StringComparer.Ordinal),
            LPresetSampleRate = LPresetAudio.LPresetSampleRate,
            LPresetChannels = LPresetAudio.LPresetChannels
        }
    };

    public static LPreset LPresetStateCreate(LPresetRecord lRecord)
    {
        string lLocation = string.Equals(lRecord.LPresetLocation, "Custom folder", StringComparison.Ordinal) ? "Custom location" : lRecord.LPresetLocation;
        var lPreset = new LPreset
        {
            LPresetLocation = lLocation,
            LPresetLocationSubfolder = lRecord.LPresetLocationSubfolder,
            LPresetLocationSibling = lRecord.LPresetLocationSibling,
            LPresetLocationCustom = lRecord.LPresetLocationCustom,
            LPresetName = lRecord.LPresetName,
            LPresetDisplay = lRecord.LPresetDisplay,
            LPresetContainer = lRecord.LPresetContainer,
            LPresetExtension = string.IsNullOrEmpty(lRecord.LPresetExtension)
                ? LPresetExtensionsRead(lRecord.LPresetContainer).FirstOrDefault() ?? string.Empty
                : lRecord.LPresetExtension,
            LPresetCollision = lRecord.LPresetCollision,
            LPresetCollisionSuffix = lRecord.LPresetCollisionSuffix,
            LPresetVideo = new LPresetVideo
        {
            LPresetStream = lRecord.LPresetVideo.LPresetStream,
            LPresetMode = LPresetVideoNormalize(lRecord.LPresetVideo.LPresetMode),
            LPresetEncoder = lRecord.LPresetVideo.LPresetEncoder,
            LPresetRateControl = lRecord.LPresetVideo.LPresetRateControl,
            LPresetQuality = lRecord.LPresetVideo.LPresetQuality,
            LPresetSpeedPreset = lRecord.LPresetVideo.LPresetSpeedPreset,
            LPresetSize = lRecord.LPresetVideo.LPresetSize,
            LPresetSizeReactive = lRecord.LPresetVideo.LPresetSizeReactive,
            LPresetFps = lRecord.LPresetVideo.LPresetFps,
            LPresetPixelLayout = lRecord.LPresetVideo.LPresetPixelLayout,
            LPresetExtras = new Dictionary<string, string>(lRecord.LPresetVideo.LPresetExtras, StringComparer.Ordinal)
        },
        LPresetAudio = new LPresetAudio
        {
            LPresetStream = lRecord.LPresetAudio.LPresetStream,
            LPresetMode = LPresetAudioNormalize(lRecord.LPresetAudio.LPresetMode),
            LPresetEncoder = lRecord.LPresetAudio.LPresetEncoder,
            LPresetRateControl = lRecord.LPresetAudio.LPresetRateControl,
            LPresetQuality = lRecord.LPresetAudio.LPresetQuality,
            LPresetSpeed = lRecord.LPresetAudio.LPresetSpeed,
            LPresetExtras = new Dictionary<string, string>(lRecord.LPresetAudio.LPresetExtras, StringComparer.Ordinal),
            LPresetSampleRate = lRecord.LPresetAudio.LPresetSampleRate,
            LPresetChannels = lRecord.LPresetAudio.LPresetChannels
        }
        };

        if (string.IsNullOrEmpty(lPreset.LPresetLocationSubfolder)
            && string.IsNullOrEmpty(lPreset.LPresetLocationSibling)
            && string.IsNullOrEmpty(lPreset.LPresetLocationCustom)
            && !string.IsNullOrEmpty(lRecord.LPresetLocationFolder))
        {
            lPreset.LPresetLocationSet(lLocation, lRecord.LPresetLocationFolder);
        }

        return lPreset;
    }

    private static string LPresetVideoNormalize(string lMode) => lMode switch
    {
        "Copy" or "Smart" or "Encode" => lMode,
        _ => "Encode"
    };

    private static string LPresetAudioNormalize(string lMode) => lMode switch
    {
        "Copy" or "Encode" or "Exclude" => lMode,
        _ => "Copy"
    };

    private string LAudioStreamSummary => LPresetAudio.LPresetStream switch
    {
        "Include first audio track" => "Include the first track",
        "Include all audio tracks" => "Include all tracks",
        _ => LPresetAudio.LPresetStream
    };
}

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
