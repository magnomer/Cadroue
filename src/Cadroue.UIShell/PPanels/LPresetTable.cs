using Cadroue.Infrastructure;
using Cadroue.Core;
using System.Collections.ObjectModel;

namespace Cadroue.UIShell.PPanels;

public sealed partial class LPreset
{
    private static readonly Dictionary<string, LPreset> LPresetMap = new(StringComparer.OrdinalIgnoreCase);

    static LPreset()
    {
        LPresetNativeAdd(LPresetAudioCreate());
        LPresetNativeAdd(LPresetSplitCreate());
        LPresetNativeAdd(LPresetMergeCreate());
        IReadOnlyList<LPresetRecord>? lStoredPresets = LPresetStore.LPresetLoad();
        if (lStoredPresets is null)
        {
            var lDefault = new LPreset { LPresetName = "MP4_H264_AAC_Default" };
            LPresetStoredAdd(lDefault);
            return;
        }

        foreach (LPresetRecord lRecord in lStoredPresets)
        {
            LPresetStoredAdd(LPreset.LPresetStateCreate(lRecord));
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
            && string.Equals(lPreset.LPresetCollision, lSource.LPresetCollision, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetCollisionSuffix, lSource.LPresetCollisionSuffix, StringComparison.Ordinal)
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
            && string.Equals(lPreset.LPresetAudioRateControl, lSource.LPresetAudioRateControl, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetAudioQuality, lSource.LPresetAudioQuality, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetAudioSpeed, lSource.LPresetAudioSpeed, StringComparison.Ordinal)
            && LPresetExtraMatch(lPreset.LPresetAudioExtras, lSource.LPresetAudioExtras)
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
        LPresetStore.LPresetSave(lPresets.Select(lPreset => lPreset.LPresetRecordCreate()).ToList());
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
        LPresetAudioEncoder = "AAC, native / aac",
        LPresetAudioRateControl = "Target bitrate",
        LPresetAudioQuality = "320k",
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
        LPresetAudioEncoder = "AAC, native / aac",
        LPresetAudioRateControl = "Target bitrate",
        LPresetAudioQuality = "192k",
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
        LPresetAudioEncoder = "AAC, native / aac",
        LPresetAudioRateControl = "Target bitrate",
        LPresetAudioQuality = "192k",
        LPresetSampleRate = "Same as source",
        LPresetAudioChannels = "Same as source"
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
