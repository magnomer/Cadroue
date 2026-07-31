using System.Collections.ObjectModel;
using Cadroue.Core;

namespace Cadroue.UIShell.PPanels;

public sealed class LPreset
{
    public const string LPresetAudioDefault = "Audio Processing (default)";
    public const string LPresetSplitDefault = "Split (default)";
    public const string LPresetMergeDefault = "Merge (default)";

    private static readonly Dictionary<string, LPreset> LPresetMap = new(StringComparer.OrdinalIgnoreCase);

    static LPreset()
    {
        LPresetNativeAdd(LPresetAudioCreate());
        LPresetNativeAdd(LPresetSplitCreate());
        LPresetNativeAdd(LPresetMergeCreate());
        IReadOnlyList<LPreset>? lStoredPresets = LPresetStore.LPresetLoad();
        if (lStoredPresets is null)
        {
            var lDefault = new LPreset { LPresetName = "MP4_H264_AAC_Default" };
            LPresetStoredAdd(lDefault);
            return;
        }

        foreach (LPreset lPreset in lStoredPresets)
        {
            LPresetStoredAdd(lPreset);
        }
    }

    private static void LPresetNativeAdd(LPreset lPreset)
    {
        LPresetMap[lPreset.LPresetName] = lPreset.LPresetClone();
        LPresetNames.Add(lPreset.LPresetName);
    }

    private static void LPresetStoredAdd(LPreset lPreset)
    {
        if (string.IsNullOrWhiteSpace(lPreset.LPresetName))
        {
            return;
        }

        string lName = lPreset.LPresetName.Trim();
        if (LPresetNativeCheck(lName))
        {
            return;
        }

        lPreset.LPresetName = lName;
        LPresetMap[lName] = lPreset.LPresetClone();
        LPresetNames.Add(lName);
    }

    public static ObservableCollection<string> LPresetNames { get; } = new();

    public string LPresetName { get; set; } = "MP4_H264_AAC_Default";
    public string LPresetDisplay { get; set; } = "{OriginalName}_export";
    public string LPresetContainer { get; set; } = "MP4";
    public string LPresetExtension { get; set; } = "mp4";
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

    public string LPresetAudioEncoder { get; set; } = "AAC";
    public string LPresetAudioBitrate { get; set; } = "96k";
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
        LPresetAudioBitrate,
        LPresetSampleRate,
        LPresetAudioChannels,
        LPresetName);

    public LPreset LPresetClone() => new()
    {
        LPresetName = LPresetName,
        LPresetDisplay = LPresetDisplay,
        LPresetContainer = LPresetContainer,
        LPresetExtension = LPresetExtension,
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
        LPresetAudioBitrate = LPresetAudioBitrate,
        LPresetSampleRate = LPresetSampleRate,
        LPresetAudioChannels = LPresetAudioChannels
    };

    public static LPreset LPresetInitialCreate(string lPresetTabKey)
    {
        var lPresetState = new LPreset();
        string? lPresetName = lPresetTabKey switch
        {
            "Audio" => LPresetAudioDefault,
            "Split" => LPresetSplitDefault,
            "Merge" => LPresetMergeDefault,
            _ => null
        };

        if (lPresetName is not null && LPresetTryLoad(lPresetName, lPresetState))
        {
            return lPresetState;
        }

        return lPresetState;
    }

    public event Action? LPresetChange;

    public void LPresetCopy(LPreset lSource)
    {
        LPresetName = lSource.LPresetName;
        LPresetDisplay = lSource.LPresetDisplay;
        LPresetContainer = lSource.LPresetContainer;
        LPresetExtension = lSource.LPresetExtension;
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
        LPresetAudioBitrate = lSource.LPresetAudioBitrate;
        LPresetSampleRate = lSource.LPresetSampleRate;
        LPresetAudioChannels = lSource.LPresetAudioChannels;
        LPresetChange?.Invoke();
    }

    public static bool LPresetTryLoad(string lPresetName, LPreset lTarget)
    {
        if (!LPresetMap.TryGetValue(lPresetName, out var lPreset))
        {
            return false;
        }

        lTarget.LPresetCopy(lPreset);
        lTarget.LPresetName = lPresetName;
        return true;
    }

    public static LPreset? LPresetRead(string lPresetName)
    {
        if (string.IsNullOrWhiteSpace(lPresetName))
        {
            return null;
        }

        return LPresetMap.TryGetValue(lPresetName.Trim(), out var lPreset) ? lPreset.LPresetClone() : null;
    }

    public static bool LPresetMatch(string lPresetName, LPreset lSource)
    {
        if (!LPresetMap.TryGetValue(lPresetName, out var lPreset))
        {
            return false;
        }

        return string.Equals(lPreset.LPresetDisplay, lSource.LPresetDisplay, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetContainer, lSource.LPresetContainer, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetExtension, lSource.LPresetExtension, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetExportMode, lSource.LPresetExportMode, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetVideoStream, lSource.LPresetVideoStream, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetAudioStream, lSource.LPresetAudioStream, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetVideoMode, lSource.LPresetVideoMode, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetAudioMode, lSource.LPresetAudioMode, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetVideoEncoder, lSource.LPresetVideoEncoder, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetRateControl, lSource.LPresetRateControl, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetVideoQuality, lSource.LPresetVideoQuality, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetSpeedPreset, lSource.LPresetSpeedPreset, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetLocation, lSource.LPresetLocation, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetLocationFolder, lSource.LPresetLocationFolder, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetVideoSize, lSource.LPresetVideoSize, StringComparison.Ordinal)
            && lPreset.LPresetSizeReactive == lSource.LPresetSizeReactive
            && string.Equals(lPreset.LPresetVideoFps, lSource.LPresetVideoFps, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetPixelLayout, lSource.LPresetPixelLayout, StringComparison.Ordinal)
            && LPresetExtraMatch(lPreset.LPresetVideoExtras, lSource.LPresetVideoExtras)
            && string.Equals(lPreset.LPresetAudioEncoder, lSource.LPresetAudioEncoder, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetAudioBitrate, lSource.LPresetAudioBitrate, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetSampleRate, lSource.LPresetSampleRate, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetAudioChannels, lSource.LPresetAudioChannels, StringComparison.Ordinal);
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

    public static void LPresetSave(string lPresetName, LPreset lSource)
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
        lPreset.LPresetName = lName;
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

    public static bool LPresetNameSet(string lOldPresetName, string lNewPresetName, LPreset lSource)
    {
        if (string.IsNullOrWhiteSpace(lOldPresetName) || string.IsNullOrWhiteSpace(lNewPresetName))
        {
            return false;
        }

        string lOldName = lOldPresetName.Trim();
        string lName = lNewPresetName.Trim();
        if (LPresetNativeCheck(lOldName) || LPresetNativeCheck(lName))
        {
            return false;
        }

        int lIndex = LPresetIndexRead(lOldName);
        if (lIndex < 0)
        {
            return false;
        }

        if (!string.Equals(lOldName, lName, StringComparison.OrdinalIgnoreCase) && LPresetIndexRead(lName) >= 0)
        {
            return false;
        }

        var lPreset = lSource.LPresetClone();
        lPreset.LPresetName = lName;
        LPresetMap.Remove(lOldName);
        LPresetMap[lName] = lPreset;
        LPresetNames[lIndex] = lName;
        LPresetPersist();
        return true;
    }

    public static bool LPresetMove(string lPresetName, int lTargetIndex)
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

        lTargetIndex = Math.Clamp(lTargetIndex, LPresetNativeRead(), LPresetNames.Count);
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
        var lPresets = new List<LPreset>();
        foreach (string lName in LPresetNames)
        {
            if (LPresetNativeCheck(lName))
            {
                continue;
            }

            if (LPresetMap.TryGetValue(lName, out LPreset? lPreset))
            {
                lPresets.Add(lPreset.LPresetClone());
            }
        }
        LPresetStore.LPresetSave(lPresets);
    }

    public static bool LPresetNativeCheck(string lPresetName) =>
        string.Equals(lPresetName, LPresetAudioDefault, StringComparison.OrdinalIgnoreCase)
        || string.Equals(lPresetName, LPresetSplitDefault, StringComparison.OrdinalIgnoreCase)
        || string.Equals(lPresetName, LPresetMergeDefault, StringComparison.OrdinalIgnoreCase);

    public static string LPresetDisplayRead(string lPresetName) => lPresetName switch
    {
        LPresetAudioDefault => "Audio Processing",
        LPresetSplitDefault => "Split",
        LPresetMergeDefault => "Merge",
        _ => lPresetName
    };

    private static int LPresetNativeRead() =>
        LPresetNames.Count(LPresetNativeCheck);

    private static LPreset LPresetAudioCreate() => new()
    {
        LPresetName = LPresetAudioDefault,
        LPresetDisplay = "{OriginalName}",
        LPresetContainer = "Same as source",
        LPresetExtension = "",
        LPresetExportMode = "Smart export",
        LPresetVideoStream = "Include",
        LPresetAudioStream = "Include first audio track",
        LPresetVideoMode = "Copy",
        LPresetAudioMode = "Auto",
        LPresetVideoEncoder = "H.264, x264 / libx264",
        LPresetRateControl = "CRF (constant quality)",
        LPresetVideoQuality = "28",
        LPresetSpeedPreset = "medium",
        LPresetLocation = "Subfolder",
        LPresetLocationFolder = "Audio",
        LPresetVideoSize = "Same as source",
        LPresetSizeReactive = false,
        LPresetVideoFps = "Same as source",
        LPresetPixelLayout = "Auto",
        LPresetVideoExtras = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["-tune"] = "none"
        },
        LPresetAudioEncoder = "AAC",
        LPresetAudioBitrate = "320k",
        LPresetSampleRate = "48000",
        LPresetAudioChannels = "Same as source"
    };

    private static LPreset LPresetSplitCreate() => new()
    {
        LPresetName = LPresetSplitDefault,
        LPresetDisplay = "{OriginalName} ({SectionNumber}) {Prefix}{SectionName}{Suffix}",
        LPresetContainer = "Same as source",
        LPresetExtension = "",
        LPresetExportMode = "Smart export",
        LPresetVideoStream = "Include",
        LPresetAudioStream = "Include first audio track",
        LPresetVideoMode = "Copy",
        LPresetAudioMode = "Copy",
        LPresetVideoEncoder = "H.264, x264 / libx264",
        LPresetRateControl = "CRF (constant quality)",
        LPresetVideoQuality = "28",
        LPresetSpeedPreset = "medium",
        LPresetLocation = "Same as source",
        LPresetLocationFolder = string.Empty,
        LPresetVideoSize = "Same as source",
        LPresetSizeReactive = false,
        LPresetVideoFps = "Same as source",
        LPresetPixelLayout = "Auto",
        LPresetVideoExtras = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["-tune"] = "none"
        },
        LPresetAudioEncoder = "AAC",
        LPresetAudioBitrate = "96k",
        LPresetSampleRate = "Same as source",
        LPresetAudioChannels = "Same as source"
    };

    private static LPreset LPresetMergeCreate() => new()
    {
        LPresetName = LPresetMergeDefault,
        LPresetDisplay = "{OriginalName}",
        LPresetContainer = "Same as source",
        LPresetExtension = "",
        LPresetExportMode = "Smart export",
        LPresetVideoStream = "Include",
        LPresetAudioStream = "Include first audio track",
        LPresetVideoMode = "Copy",
        LPresetAudioMode = "Copy",
        LPresetVideoEncoder = "H.264, x264 / libx264",
        LPresetRateControl = "CRF (constant quality)",
        LPresetVideoQuality = "28",
        LPresetSpeedPreset = "medium",
        LPresetLocation = "Same as source",
        LPresetLocationFolder = string.Empty,
        LPresetVideoSize = "Same as source",
        LPresetSizeReactive = false,
        LPresetVideoFps = "Same as source",
        LPresetPixelLayout = "Auto",
        LPresetVideoExtras = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["-tune"] = "none"
        },
        LPresetAudioEncoder = "AAC",
        LPresetAudioBitrate = "96k",
        LPresetSampleRate = "Same as source",
        LPresetAudioChannels = "Same as source"
    };

    private string LAudioStreamSummary => LPresetAudioStream switch
    {
        "Include first audio track" => "Include the first track",
        "Include all audio tracks" => "Include all tracks",
        _ => LPresetAudioStream
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

    public static IReadOnlyList<string> LPresetExtensionsRead(string lContainer) =>
        LPresetExtensionTable.TryGetValue(lContainer, out string[]? lExtensions) ? lExtensions : [];
}
