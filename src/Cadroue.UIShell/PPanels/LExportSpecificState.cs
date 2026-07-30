using System.Collections.ObjectModel;
using Cadroue.Core;

namespace Cadroue.UIShell.PPanels;

public sealed class LExportSpecificState
{
    public const string LPresetAudioDefaultName = "Audio Processing (default)";
    public const string LPresetSplitDefaultName = "Split (default)";

    private static readonly Dictionary<string, LExportSpecificState> LPresetMap = new(StringComparer.OrdinalIgnoreCase);

    static LExportSpecificState()
    {
        LPresetNativeAdd(LPresetAudioDefaultCreate());
        LPresetNativeAdd(LPresetSplitDefaultCreate());
        IReadOnlyList<LExportSpecificState>? lStoredPresets = LExportSpecificPresetStore.LPresetLoad();
        if (lStoredPresets is null)
        {
            var lDefault = new LExportSpecificState { PresetName = "MP4_H264_AAC_Default" };
            LPresetStoredAdd(lDefault);
            return;
        }

        foreach (LExportSpecificState lPreset in lStoredPresets)
        {
            LPresetStoredAdd(lPreset);
        }
    }

    private static void LPresetNativeAdd(LExportSpecificState lPreset)
    {
        LPresetMap[lPreset.PresetName] = lPreset.LPresetClone();
        LPresetNames.Add(lPreset.PresetName);
    }

    private static void LPresetStoredAdd(LExportSpecificState lPreset)
    {
        if (string.IsNullOrWhiteSpace(lPreset.PresetName))
        {
            return;
        }

        string lName = lPreset.PresetName.Trim();
        if (LPresetNativeCheck(lName))
        {
            return;
        }

        lPreset.PresetName = lName;
        LPresetMap[lName] = lPreset.LPresetClone();
        LPresetNames.Add(lName);
    }

    public static ObservableCollection<string> LPresetNames { get; } = new();

    public string PresetName { get; set; } = "MP4_H264_AAC_Default";
    public string Name { get; set; } = "{OriginalName}_export";
    public string Container { get; set; } = "MP4";
    public string ExportMode { get; set; } = "Smart export";
    public string VideoStream { get; set; } = "Include";
    public string AudioStream { get; set; } = "Include first audio track";
    public string VideoMode { get; set; } = "Auto";
    public string AudioMode { get; set; } = "Auto";

    public string VideoEncoder { get; set; } = "H.264, x264 / libx264";
    public string VideoRateControl { get; set; } = "CRF (constant quality)";
    public string VideoQuality { get; set; } = "23";
    public string VideoSpeedPreset { get; set; } = "medium";

    public string Location { get; set; } = "Same as source";
    public string LocationFolder { get; set; } = string.Empty;

    public string VideoSize { get; set; } = "Same as source";
    public bool VideoSizeReactive { get; set; }
    public string VideoFps { get; set; } = "Same as source";
    public string PixelFormat { get; set; } = "Auto";

    public Dictionary<string, string> VideoExtras { get; set; } = new(StringComparer.Ordinal);

    public string AudioEncoder { get; set; } = "AAC";
    public string AudioBitrate { get; set; } = "96k";
    public string AudioSampleRate { get; set; } = "Same as source";
    public string AudioChannels { get; set; } = "Same as source";

    public string VideoSummary => $"{VideoMode} ({VideoStream})";
    public string AudioSummary => $"{AudioMode} ({LAudioStreamSummary})";
    public string OutputSummary => string.IsNullOrWhiteSpace(LContainerExtension) ? Name : $"{Name}.{LContainerExtension}";

    public LWorkOutput LPresetOutputCreate() => new(
        Name,
        Container,
        LContainerExtension,
        Location,
        LocationFolder,
        ExportMode,
        VideoStream,
        VideoMode,
        VideoEncoder,
        VideoRateControl,
        VideoQuality,
        VideoSpeedPreset,
        VideoSize,
        VideoSizeReactive,
        VideoFps,
        PixelFormat,
        new Dictionary<string, string>(VideoExtras, StringComparer.Ordinal),
        AudioStream,
        AudioMode,
        AudioEncoder,
        AudioBitrate,
        AudioSampleRate,
        AudioChannels,
        PresetName);

    public LExportSpecificState LPresetClone() => new()
    {
        PresetName = PresetName,
        Name = Name,
        Container = Container,
        ExportMode = ExportMode,
        VideoStream = VideoStream,
        AudioStream = AudioStream,
        VideoMode = VideoMode,
        AudioMode = AudioMode,
        VideoEncoder = VideoEncoder,
        VideoRateControl = VideoRateControl,
        VideoQuality = VideoQuality,
        VideoSpeedPreset = VideoSpeedPreset,
        Location = Location,
        LocationFolder = LocationFolder,
        VideoSize = VideoSize,
        VideoSizeReactive = VideoSizeReactive,
        VideoFps = VideoFps,
        PixelFormat = PixelFormat,
        VideoExtras = new Dictionary<string, string>(VideoExtras, StringComparer.Ordinal),
        AudioEncoder = AudioEncoder,
        AudioBitrate = AudioBitrate,
        AudioSampleRate = AudioSampleRate,
        AudioChannels = AudioChannels
    };

    public static LExportSpecificState LPresetInitialCreate(string lPresetTabKey)
    {
        var lPresetState = new LExportSpecificState();
        string? lPresetName = lPresetTabKey switch
        {
            "Audio" => LPresetAudioDefaultName,
            "Split" => LPresetSplitDefaultName,
            _ => null
        };

        if (lPresetName is not null && LPresetTryLoad(lPresetName, lPresetState))
        {
            return lPresetState;
        }

        return lPresetState;
    }

    public event Action? LPresetChange;

    public void LPresetCopy(LExportSpecificState lSource)
    {
        PresetName = lSource.PresetName;
        Name = lSource.Name;
        Container = lSource.Container;
        ExportMode = lSource.ExportMode;
        VideoStream = lSource.VideoStream;
        AudioStream = lSource.AudioStream;
        VideoMode = lSource.VideoMode;
        AudioMode = lSource.AudioMode;
        VideoEncoder = lSource.VideoEncoder;
        VideoRateControl = lSource.VideoRateControl;
        VideoQuality = lSource.VideoQuality;
        VideoSpeedPreset = lSource.VideoSpeedPreset;
        Location = lSource.Location;
        LocationFolder = lSource.LocationFolder;
        VideoSize = lSource.VideoSize;
        VideoSizeReactive = lSource.VideoSizeReactive;
        VideoFps = lSource.VideoFps;
        PixelFormat = lSource.PixelFormat;
        VideoExtras = new Dictionary<string, string>(lSource.VideoExtras, StringComparer.Ordinal);
        AudioEncoder = lSource.AudioEncoder;
        AudioBitrate = lSource.AudioBitrate;
        AudioSampleRate = lSource.AudioSampleRate;
        AudioChannels = lSource.AudioChannels;
        LPresetChange?.Invoke();
    }

    public static bool LPresetTryLoad(string lPresetName, LExportSpecificState lTarget)
    {
        if (!LPresetMap.TryGetValue(lPresetName, out var lPreset))
        {
            return false;
        }

        lTarget.LPresetCopy(lPreset);
        lTarget.PresetName = lPresetName;
        return true;
    }

    public static LExportSpecificState? LPresetRead(string lPresetName)
    {
        if (string.IsNullOrWhiteSpace(lPresetName))
        {
            return null;
        }

        return LPresetMap.TryGetValue(lPresetName.Trim(), out var lPreset) ? lPreset.LPresetClone() : null;
    }

    public static bool LPresetMatch(string lPresetName, LExportSpecificState lSource)
    {
        if (!LPresetMap.TryGetValue(lPresetName, out var lPreset))
        {
            return false;
        }

        return string.Equals(lPreset.Name, lSource.Name, StringComparison.Ordinal)
            && string.Equals(lPreset.Container, lSource.Container, StringComparison.Ordinal)
            && string.Equals(lPreset.ExportMode, lSource.ExportMode, StringComparison.Ordinal)
            && string.Equals(lPreset.VideoStream, lSource.VideoStream, StringComparison.Ordinal)
            && string.Equals(lPreset.AudioStream, lSource.AudioStream, StringComparison.Ordinal)
            && string.Equals(lPreset.VideoMode, lSource.VideoMode, StringComparison.Ordinal)
            && string.Equals(lPreset.AudioMode, lSource.AudioMode, StringComparison.Ordinal)
            && string.Equals(lPreset.VideoEncoder, lSource.VideoEncoder, StringComparison.Ordinal)
            && string.Equals(lPreset.VideoRateControl, lSource.VideoRateControl, StringComparison.Ordinal)
            && string.Equals(lPreset.VideoQuality, lSource.VideoQuality, StringComparison.Ordinal)
            && string.Equals(lPreset.VideoSpeedPreset, lSource.VideoSpeedPreset, StringComparison.Ordinal)
            && string.Equals(lPreset.Location, lSource.Location, StringComparison.Ordinal)
            && string.Equals(lPreset.LocationFolder, lSource.LocationFolder, StringComparison.Ordinal)
            && string.Equals(lPreset.VideoSize, lSource.VideoSize, StringComparison.Ordinal)
            && lPreset.VideoSizeReactive == lSource.VideoSizeReactive
            && string.Equals(lPreset.VideoFps, lSource.VideoFps, StringComparison.Ordinal)
            && string.Equals(lPreset.PixelFormat, lSource.PixelFormat, StringComparison.Ordinal)
            && LPresetExtraMatch(lPreset.VideoExtras, lSource.VideoExtras)
            && string.Equals(lPreset.AudioEncoder, lSource.AudioEncoder, StringComparison.Ordinal)
            && string.Equals(lPreset.AudioBitrate, lSource.AudioBitrate, StringComparison.Ordinal)
            && string.Equals(lPreset.AudioSampleRate, lSource.AudioSampleRate, StringComparison.Ordinal)
            && string.Equals(lPreset.AudioChannels, lSource.AudioChannels, StringComparison.Ordinal);
    }

    private static bool LPresetExtraMatch(
        IReadOnlyDictionary<string, string> lFirstExtras,
        IReadOnlyDictionary<string, string> lSecondExtras)
    {
        if (lFirstExtras.Count != lSecondExtras.Count)
        {
            return false;
        }

        foreach ((string lKey, string lValue) in lFirstExtras)
        {
            if (!lSecondExtras.TryGetValue(lKey, out string? lSecondValue)
                || !string.Equals(lValue, lSecondValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public static void LPresetSave(string lPresetName, LExportSpecificState lSource)
    {
        if (string.IsNullOrWhiteSpace(lPresetName))
        {
            return;
        }

        string lName = lPresetName.Trim();
        if (LPresetNativeCheck(lName))
        {
            return;
        }

        var lPreset = lSource.LPresetClone();
        lPreset.PresetName = lName;
        LPresetMap[lName] = lPreset;
        if (!LPresetNames.Any(lExisting => string.Equals(lExisting, lName, StringComparison.OrdinalIgnoreCase)))
        {
            LPresetNames.Add(lName);
        }
        LPresetPersist();
    }

    public static bool LPresetDelete(string lPresetName)
    {
        if (string.IsNullOrWhiteSpace(lPresetName))
        {
            return false;
        }

        string lName = lPresetName.Trim();
        if (LPresetNativeCheck(lName))
        {
            return false;
        }

        if (!LPresetMap.Remove(lName))
        {
            return false;
        }

        for (int lIndex = LPresetNames.Count - 1; lIndex >= 0; lIndex--)
        {
            if (string.Equals(LPresetNames[lIndex], lName, StringComparison.OrdinalIgnoreCase))
            {
                LPresetNames.RemoveAt(lIndex);
            }
        }
        LPresetPersist();
        return true;
    }

    public static bool LPresetMoveToIndex(string lPresetName, int lTargetIndex)
    {
        if (string.IsNullOrWhiteSpace(lPresetName))
        {
            return false;
        }

        int lSourceIndex = LPresetIndexRead(lPresetName);
        if (lSourceIndex < 0)
        {
            return false;
        }

        if (LPresetNativeCheck(lPresetName))
        {
            return false;
        }

        lTargetIndex = Math.Clamp(lTargetIndex, LPresetNativeCountRead(), LPresetNames.Count);
        if (lSourceIndex < lTargetIndex)
        {
            lTargetIndex--;
        }

        if (lSourceIndex == lTargetIndex)
        {
            return false;
        }

        string lName = LPresetNames[lSourceIndex];
        LPresetNames.RemoveAt(lSourceIndex);
        LPresetNames.Insert(lTargetIndex, lName);
        LPresetPersist();
        return true;
    }

    public static string? LPresetFirstName => LPresetNames.Count > 0 ? LPresetNames[0] : null;

    private static int LPresetIndexRead(string lPresetName)
    {
        for (int lIndex = 0; lIndex < LPresetNames.Count; lIndex++)
        {
            if (string.Equals(LPresetNames[lIndex], lPresetName, StringComparison.OrdinalIgnoreCase))
            {
                return lIndex;
            }
        }

        return -1;
    }

    private static void LPresetPersist()
    {
        var lPresets = new List<LExportSpecificState>();
        foreach (string lName in LPresetNames)
        {
            if (LPresetNativeCheck(lName))
            {
                continue;
            }

            if (LPresetMap.TryGetValue(lName, out LExportSpecificState? lPreset))
            {
                lPresets.Add(lPreset.LPresetClone());
            }
        }
        LExportSpecificPresetStore.LPresetSave(lPresets);
    }

    public static bool LPresetNativeCheck(string lPresetName) =>
        string.Equals(lPresetName, LPresetAudioDefaultName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(lPresetName, LPresetSplitDefaultName, StringComparison.OrdinalIgnoreCase);

    public static string LPresetDisplayNameRead(string lPresetName) => lPresetName switch
    {
        LPresetAudioDefaultName => "Audio Processing",
        LPresetSplitDefaultName => "Split",
        _ => lPresetName
    };

    private static int LPresetNativeCountRead() =>
        LPresetNames.Count(LPresetNativeCheck);

    private static LExportSpecificState LPresetAudioDefaultCreate() => new()
    {
        PresetName = LPresetAudioDefaultName,
        Name = "{OriginalName}",
        Container = "Same as source",
        ExportMode = "Smart export",
        VideoStream = "Include",
        AudioStream = "Include first audio track",
        VideoMode = "Copy",
        AudioMode = "Auto",
        VideoEncoder = "H.264, x264 / libx264",
        VideoRateControl = "CRF (constant quality)",
        VideoQuality = "28",
        VideoSpeedPreset = "medium",
        Location = "Subfolder",
        LocationFolder = "Audio",
        VideoSize = "Same as source",
        VideoSizeReactive = false,
        VideoFps = "Same as source",
        PixelFormat = "Auto",
        VideoExtras = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["-tune"] = "none"
        },
        AudioEncoder = "AAC",
        AudioBitrate = "320k",
        AudioSampleRate = "48000",
        AudioChannels = "Same as source"
    };

    private static LExportSpecificState LPresetSplitDefaultCreate() => new()
    {
        PresetName = LPresetSplitDefaultName,
        Name = "{OriginalName} ({SectionNumber}) {Prefix}{SectionName}{Suffix}",
        Container = "Same as source",
        ExportMode = "Smart export",
        VideoStream = "Include",
        AudioStream = "Include first audio track",
        VideoMode = "Copy",
        AudioMode = "Copy",
        VideoEncoder = "H.264, x264 / libx264",
        VideoRateControl = "CRF (constant quality)",
        VideoQuality = "28",
        VideoSpeedPreset = "medium",
        Location = "Same as source",
        LocationFolder = string.Empty,
        VideoSize = "Same as source",
        VideoSizeReactive = false,
        VideoFps = "Same as source",
        PixelFormat = "Auto",
        VideoExtras = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["-tune"] = "none"
        },
        AudioEncoder = "AAC",
        AudioBitrate = "96k",
        AudioSampleRate = "Same as source",
        AudioChannels = "Same as source"
    };

    private string LAudioStreamSummary => AudioStream switch
    {
        "Include first audio track" => "Include the first track",
        "Include all audio tracks" => "Include all tracks",
        _ => AudioStream
    };

    private string LContainerExtension => Container switch
    {
        "Same as source" => "",
        "MP4" => "mp4",
        "Matroska" => "mkv",
        "MOV" => "mov",
        "WebM" => "webm",
        "M4A" => "m4a",
        "MP3" => "mp3",
        "WAV" => "wav",
        "FLAC" => "flac",
        "OGG" => "ogg",
        _ => ""
    };
}
