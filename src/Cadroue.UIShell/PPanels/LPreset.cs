using Cadroue.Core;

namespace Cadroue.UIShell.PPanels;

public sealed partial class LPreset
{
    public const string LPresetAudioDefault = "Audio Processing (default)";
    public const string LPresetSplitDefault = "Split (default)";
    public const string LPresetMergeDefault = "Merge (default)";

    public string LPresetName { get; set; } = "MP4_H264_AAC_Default";
    public string LPresetDisplay { get; set; } = "{OriginalName}_export";
    public string LPresetContainer { get; set; } = "MP4";
    public string LPresetExtension { get; set; } = "mp4";
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

    public Dictionary<string, string> LPresetVideoExtras { get; set; } = new(StringComparer.Ordinal);

    public string LPresetAudioEncoder { get; set; } = "AAC, native / aac";
    public string LPresetAudioRateControl { get; set; } = "Target bitrate";
    public string LPresetAudioQuality { get; set; } = "192k";
    public string LPresetAudioSpeed { get; set; } = string.Empty;
    public Dictionary<string, string> LPresetAudioExtras { get; set; } = new(StringComparer.Ordinal);
    public string LPresetSampleRate { get; set; } = "Same as source";
    public string LPresetAudioChannels { get; set; } = "Same as source";

    public string LPresetVideoSummary => $"{LPresetVideoMode} ({LPresetVideoStream})";
    public string LPresetAudioSummary => $"{LPresetAudioMode} ({LAudioStreamSummary})";
    public string LPresetOutputSummary => string.IsNullOrWhiteSpace(LPresetExtension) ? LPresetDisplay : $"{LPresetDisplay}.{LPresetExtension}";

    public LWorkOutput LPresetOutputCreate() => new(
        LPresetDisplay,
        LPresetContainer,
        LPresetExtension,
        LPresetLocation,
        LPresetLocationFolder,
        LPresetExportMode,
        LPresetVideoStream,
        LPresetVideoMode,
        LPresetVideoEncoder,
        LPresetRateControl,
        LPresetVideoQuality,
        LPresetSpeedPreset,
        LPresetVideoSize,
        LPresetSizeReactive,
        LPresetVideoFps,
        LPresetPixelLayout,
        new Dictionary<string, string>(LPresetVideoExtras, StringComparer.Ordinal),
        LPresetAudioStream,
        LPresetAudioMode,
        LPresetAudioEncoder,
        LPresetAudioRateControl,
        LPresetAudioQuality,
        LPresetAudioSpeed,
        new Dictionary<string, string>(LPresetAudioExtras, StringComparer.Ordinal),
        LPresetSampleRate,
        LPresetAudioChannels,
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
        LPresetExportMode = LPresetExportMode,
        LPresetVideoStream = LPresetVideoStream,
        LPresetAudioStream = LPresetAudioStream,
        LPresetVideoMode = LPresetVideoMode,
        LPresetAudioMode = LPresetAudioMode,
        LPresetVideoEncoder = LPresetVideoEncoder,
        LPresetRateControl = LPresetRateControl,
        LPresetVideoQuality = LPresetVideoQuality,
        LPresetSpeedPreset = LPresetSpeedPreset,
        LPresetLocation = LPresetLocation,
        LPresetLocationFolder = LPresetLocationFolder,
        LPresetVideoSize = LPresetVideoSize,
        LPresetSizeReactive = LPresetSizeReactive,
        LPresetVideoFps = LPresetVideoFps,
        LPresetPixelLayout = LPresetPixelLayout,
        LPresetVideoExtras = new Dictionary<string, string>(LPresetVideoExtras, StringComparer.Ordinal),
        LPresetAudioEncoder = LPresetAudioEncoder,
        LPresetAudioRateControl = LPresetAudioRateControl,
        LPresetAudioQuality = LPresetAudioQuality,
        LPresetAudioSpeed = LPresetAudioSpeed,
        LPresetAudioExtras = new Dictionary<string, string>(LPresetAudioExtras, StringComparer.Ordinal),
        LPresetSampleRate = LPresetSampleRate,
        LPresetAudioChannels = LPresetAudioChannels
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
        LPresetExportMode = lSource.LPresetExportMode;
        LPresetVideoStream = lSource.LPresetVideoStream;
        LPresetAudioStream = lSource.LPresetAudioStream;
        LPresetVideoMode = lSource.LPresetVideoMode;
        LPresetAudioMode = lSource.LPresetAudioMode;
        LPresetVideoEncoder = lSource.LPresetVideoEncoder;
        LPresetRateControl = lSource.LPresetRateControl;
        LPresetVideoQuality = lSource.LPresetVideoQuality;
        LPresetSpeedPreset = lSource.LPresetSpeedPreset;
        LPresetLocation = lSource.LPresetLocation;
        LPresetLocationFolder = lSource.LPresetLocationFolder;
        LPresetVideoSize = lSource.LPresetVideoSize;
        LPresetSizeReactive = lSource.LPresetSizeReactive;
        LPresetVideoFps = lSource.LPresetVideoFps;
        LPresetPixelLayout = lSource.LPresetPixelLayout;
        LPresetVideoExtras = new Dictionary<string, string>(lSource.LPresetVideoExtras, StringComparer.Ordinal);
        LPresetAudioEncoder = lSource.LPresetAudioEncoder;
        LPresetAudioRateControl = lSource.LPresetAudioRateControl;
        LPresetAudioQuality = lSource.LPresetAudioQuality;
        LPresetAudioSpeed = lSource.LPresetAudioSpeed;
        LPresetAudioExtras = new Dictionary<string, string>(lSource.LPresetAudioExtras, StringComparer.Ordinal);
        LPresetSampleRate = lSource.LPresetSampleRate;
        LPresetAudioChannels = lSource.LPresetAudioChannels;
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
        LPresetExportMode = LPresetExportMode,
        LPresetVideoStream = LPresetVideoStream,
        LPresetAudioStream = LPresetAudioStream,
        LPresetVideoMode = LPresetVideoMode,
        LPresetAudioMode = LPresetAudioMode,
        LPresetVideoEncoder = LPresetVideoEncoder,
        LPresetRateControl = LPresetRateControl,
        LPresetVideoQuality = LPresetVideoQuality,
        LPresetSpeedPreset = LPresetSpeedPreset,
        LPresetLocation = LPresetLocation,
        LPresetLocationFolder = LPresetLocationFolder,
        LPresetVideoSize = LPresetVideoSize,
        LPresetSizeReactive = LPresetSizeReactive,
        LPresetVideoFps = LPresetVideoFps,
        LPresetPixelLayout = LPresetPixelLayout,
        LPresetVideoExtras = new Dictionary<string, string>(LPresetVideoExtras),
        LPresetAudioEncoder = LPresetAudioEncoder,
        LPresetAudioRateControl = LPresetAudioRateControl,
        LPresetAudioQuality = LPresetAudioQuality,
        LPresetAudioSpeed = LPresetAudioSpeed,
        LPresetAudioExtras = new Dictionary<string, string>(LPresetAudioExtras, StringComparer.Ordinal),
        LPresetSampleRate = LPresetSampleRate,
        LPresetAudioChannels = LPresetAudioChannels
    };

    public static LPreset LPresetStateCreate(LPresetRecord lRecord) => new()
    {
        LPresetName = lRecord.LPresetName,
        LPresetDisplay = lRecord.LPresetDisplay,
        LPresetContainer = lRecord.LPresetContainer,
        LPresetExtension = string.IsNullOrEmpty(lRecord.LPresetExtension)
            ? LPresetExtensionsRead(lRecord.LPresetContainer).FirstOrDefault() ?? string.Empty
            : lRecord.LPresetExtension,
        LPresetCollision = lRecord.LPresetCollision,
        LPresetCollisionSuffix = lRecord.LPresetCollisionSuffix,
        LPresetExportMode = lRecord.LPresetExportMode,
        LPresetVideoStream = lRecord.LPresetVideoStream,
        LPresetAudioStream = lRecord.LPresetAudioStream,
        LPresetVideoMode = lRecord.LPresetVideoMode,
        LPresetAudioMode = lRecord.LPresetAudioMode,
        LPresetVideoEncoder = lRecord.LPresetVideoEncoder,
        LPresetRateControl = lRecord.LPresetRateControl,
        LPresetVideoQuality = lRecord.LPresetVideoQuality,
        LPresetSpeedPreset = lRecord.LPresetSpeedPreset,
        LPresetLocation = string.Equals(lRecord.LPresetLocation, "Custom folder", StringComparison.Ordinal) ? "Custom location" : lRecord.LPresetLocation,
        LPresetLocationFolder = lRecord.LPresetLocationFolder,
        LPresetVideoSize = lRecord.LPresetVideoSize,
        LPresetSizeReactive = lRecord.LPresetSizeReactive,
        LPresetVideoFps = lRecord.LPresetVideoFps,
        LPresetPixelLayout = lRecord.LPresetPixelLayout,
        LPresetVideoExtras = new Dictionary<string, string>(lRecord.LPresetVideoExtras, StringComparer.Ordinal),
        LPresetAudioEncoder = lRecord.LPresetAudioEncoder,
        LPresetAudioRateControl = lRecord.LPresetAudioRateControl,
        LPresetAudioQuality = lRecord.LPresetAudioQuality,
        LPresetAudioSpeed = lRecord.LPresetAudioSpeed,
        LPresetAudioExtras = new Dictionary<string, string>(lRecord.LPresetAudioExtras, StringComparer.Ordinal),
        LPresetSampleRate = lRecord.LPresetSampleRate,
        LPresetAudioChannels = lRecord.LPresetAudioChannels
    };

    private string LAudioStreamSummary => LPresetAudioStream switch
    {
        "Include first audio track" => "Include the first track",
        "Include all audio tracks" => "Include all tracks",
        _ => LPresetAudioStream
    };
}
