namespace Cadroue.Application;

public sealed partial class LPreset
{
    public const string LPresetAudioDefault = "Audio Processing (default)";
    public const string LPresetSplitDefault = "Split (default)";
    public const string LPresetMergeDefault = "Merge (default)";

    private static LPreset LPresetAudioCreate() => new()
    {
        LPresetName = LPresetAudioDefault,
        LPresetDisplay = "{OriginalName}",
        LPresetContainer = "Same as source",
        LPresetExtension = "",
        LPresetExportMode = "Smart export",
        LPresetLocation = "Subfolder",
        LPresetLocationFolder = "Audio",
        LPresetVideo = new LPresetVideo
        {
            LPresetStream = "Include",
            LPresetMode = "Copy",
            LPresetEncoder = "H.264, x264 / libx264",
            LPresetRateControl = "CRF (constant quality)",
            LPresetQuality = "28",
            LPresetSpeedPreset = "medium",
            LPresetSize = "Same as source",
            LPresetSizeReactive = false,
            LPresetFps = "Same as source",
            LPresetPixelLayout = "Auto",
            LPresetExtras = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["-tune"] = "none"
            }
        },
        LPresetAudio = new LPresetAudio
        {
            LPresetStream = "Include first audio track",
            LPresetMode = "Auto",
            LPresetEncoder = "AAC, native / aac",
            LPresetRateControl = "Target bitrate",
            LPresetQuality = "320k",
            LPresetSampleRate = "48000",
            LPresetChannels = "Same as source"
        }
    };

    private static LPreset LPresetSplitCreate() => new()
    {
        LPresetName = LPresetSplitDefault,
        LPresetDisplay = "{OriginalName} ({SectionNumber}) {Prefix}{SectionName}{Suffix}",
        LPresetContainer = "Same as source",
        LPresetExtension = "",
        LPresetExportMode = "Smart export",
        LPresetLocation = "Same as source",
        LPresetLocationFolder = string.Empty,
        LPresetVideo = new LPresetVideo
        {
            LPresetStream = "Include",
            LPresetMode = "Copy",
            LPresetEncoder = "H.264, x264 / libx264",
            LPresetRateControl = "CRF (constant quality)",
            LPresetQuality = "28",
            LPresetSpeedPreset = "medium",
            LPresetSize = "Same as source",
            LPresetSizeReactive = false,
            LPresetFps = "Same as source",
            LPresetPixelLayout = "Auto",
            LPresetExtras = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["-tune"] = "none"
            }
        },
        LPresetAudio = new LPresetAudio
        {
            LPresetStream = "Include first audio track",
            LPresetMode = "Copy",
            LPresetEncoder = "AAC, native / aac",
            LPresetRateControl = "Target bitrate",
            LPresetQuality = "192k",
            LPresetSampleRate = "Same as source",
            LPresetChannels = "Same as source"
        }
    };

    private static LPreset LPresetMergeCreate() => new()
    {
        LPresetName = LPresetMergeDefault,
        LPresetDisplay = "{OriginalName}",
        LPresetContainer = "Same as source",
        LPresetExtension = "",
        LPresetExportMode = "Smart export",
        LPresetLocation = "Same as source",
        LPresetLocationFolder = string.Empty,
        LPresetVideo = new LPresetVideo
        {
            LPresetStream = "Include",
            LPresetMode = "Copy",
            LPresetEncoder = "H.264, x264 / libx264",
            LPresetRateControl = "CRF (constant quality)",
            LPresetQuality = "28",
            LPresetSpeedPreset = "medium",
            LPresetSize = "Same as source",
            LPresetSizeReactive = false,
            LPresetFps = "Same as source",
            LPresetPixelLayout = "Auto",
            LPresetExtras = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["-tune"] = "none"
            }
        },
        LPresetAudio = new LPresetAudio
        {
            LPresetStream = "Include first audio track",
            LPresetMode = "Copy",
            LPresetEncoder = "AAC, native / aac",
            LPresetRateControl = "Target bitrate",
            LPresetQuality = "192k",
            LPresetSampleRate = "Same as source",
            LPresetChannels = "Same as source"
        }
    };

    private static readonly Dictionary<string, string[]> LPresetExtensionTable = new(StringComparer.Ordinal)
    {
        ["MP4"] = ["mp4", "m4v"],
        ["Matroska"] = ["mkv"],
        ["MOV"] = ["mov"],
        ["WebM"] = ["webm"],
        ["AVI"] = ["avi"],
        ["MPEG-TS"] = ["ts", "m2ts", "mts"],
        ["FLV"] = ["flv", "f4v"],
        ["Ogg"] = ["ogv"]
    };
}
