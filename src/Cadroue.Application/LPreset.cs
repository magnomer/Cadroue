using Cadroue.Core;
using System.IO;

namespace Cadroue.Application;

public sealed partial class LPreset
{
    public string LPresetName { get; set; } = "MP4_H264_AAC_Default";
    public string LPresetDisplay { get; set; } = "{OriginalName}_export";
    public string LPresetContainer { get; set; } = "MP4";
    public string LPresetExtension { get; set; } = "mp4";
    public string LPresetCollision { get; set; } = "Overwrite";
    public string LPresetOutputSuffix { get; set; } = "(output)";
    public string LPresetSourceSuffix { get; set; } = "(original)";
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

    public string LPresetSuffixRead(string lMode) => lMode switch
    {
        "Rename existing" => LPresetSourceSuffix,
        _ => LPresetOutputSuffix
    };

    public void LPresetSuffixSet(string lMode, string lSuffix)
    {
        switch (lMode)
        {
            case "Rename output":
                LPresetOutputSuffix = string.IsNullOrEmpty(lSuffix) ? "(output)" : lSuffix;
                break;
            case "Rename existing":
                LPresetSourceSuffix = string.IsNullOrEmpty(lSuffix) ? "(original)" : lSuffix;
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
        LPresetSuffixRead(LPresetCollision));

    public LPreset LPresetClone() => new()
    {
        LPresetName = LPresetName,
        LPresetDisplay = LPresetDisplay,
        LPresetContainer = LPresetContainer,
        LPresetExtension = LPresetExtension,
        LPresetCollision = LPresetCollision,
        LPresetOutputSuffix = LPresetOutputSuffix,
        LPresetSourceSuffix = LPresetSourceSuffix,
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
        LPresetOutputSuffix = lSource.LPresetOutputSuffix;
        LPresetSourceSuffix = lSource.LPresetSourceSuffix;
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
        LPresetOutputSuffix = LPresetOutputSuffix,
        LPresetSourceSuffix = LPresetSourceSuffix,
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
            LPresetOutputSuffix = LPresetSuffixResolve(lRecord.LPresetOutputSuffix, lRecord.LPresetCollisionSuffix, "(output)"),
            LPresetSourceSuffix = LPresetSuffixResolve(lRecord.LPresetSourceSuffix, lRecord.LPresetCollisionSuffix, "(original)"),
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

    private static string LPresetSuffixResolve(string lSlot, string lLegacy, string lDefault) =>
        !string.IsNullOrEmpty(lSlot) ? lSlot : !string.IsNullOrEmpty(lLegacy) ? lLegacy : lDefault;

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

    // Storage is shared: another Cadroue process can have committed changes since this one last
    // read the catalogue. The merge replays this process's own intent (its difference from the
    // baseline it loaded) onto whatever is durable now, so a write can neither resurrect what
    // this process deleted nor erase what another process committed.
    internal static IReadOnlyList<LPresetRecord> LPresetMergeCreate(
        IReadOnlyList<LPresetRecord> lPresetBaseline,
        IReadOnlyList<LPresetRecord> lPresetLocal,
        IReadOnlyList<LPresetRecord> lPresetStored)
    {
        Dictionary<string, LPresetRecord> lPresetBaselineMap = LPresetLookupCreate(lPresetBaseline);
        Dictionary<string, LPresetRecord> lPresetStoredMap = LPresetLookupCreate(lPresetStored);
        var lPresetMerged = new List<LPresetRecord>();
        var lPresetTaken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (LPresetRecord lPresetRecord in lPresetLocal)
        {
            string lPresetName = lPresetRecord.LPresetName;
            bool lPresetUntouched = lPresetBaselineMap.TryGetValue(lPresetName, out LPresetRecord? lPresetBase)
                && LPresetRecordMatch(lPresetBase, lPresetRecord);
            if (lPresetUntouched)
            {
                if (lPresetStoredMap.TryGetValue(lPresetName, out LPresetRecord? lPresetDurable))
                {
                    lPresetMerged.Add(lPresetDurable);
                    lPresetTaken.Add(lPresetName);
                }

                continue;
            }

            lPresetMerged.Add(lPresetRecord);
            lPresetTaken.Add(lPresetName);
        }

        foreach (LPresetRecord lPresetRecord in lPresetStored)
        {
            if (!lPresetTaken.Contains(lPresetRecord.LPresetName)
                && !lPresetBaselineMap.ContainsKey(lPresetRecord.LPresetName))
            {
                lPresetMerged.Add(lPresetRecord);
            }
        }

        return lPresetMerged;
    }

    private static Dictionary<string, LPresetRecord> LPresetLookupCreate(IReadOnlyList<LPresetRecord> lPresetRecords)
    {
        var lPresetMap = new Dictionary<string, LPresetRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (LPresetRecord lPresetRecord in lPresetRecords)
        {
            lPresetMap[lPresetRecord.LPresetName] = lPresetRecord;
        }

        return lPresetMap;
    }

    internal static bool LPresetValueMatch(LPreset lFirstPreset, LPreset lSecondPreset)
    {
        return string.Equals(lFirstPreset.LPresetDisplay, lSecondPreset.LPresetDisplay, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetContainer, lSecondPreset.LPresetContainer, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetExtension, lSecondPreset.LPresetExtension, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetCollision, lSecondPreset.LPresetCollision, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetOutputSuffix, lSecondPreset.LPresetOutputSuffix, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetSourceSuffix, lSecondPreset.LPresetSourceSuffix, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetVideo.LPresetStream, lSecondPreset.LPresetVideo.LPresetStream, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetAudio.LPresetStream, lSecondPreset.LPresetAudio.LPresetStream, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetVideo.LPresetMode, lSecondPreset.LPresetVideo.LPresetMode, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetAudio.LPresetMode, lSecondPreset.LPresetAudio.LPresetMode, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetVideo.LPresetEncoder, lSecondPreset.LPresetVideo.LPresetEncoder, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetVideo.LPresetRateControl, lSecondPreset.LPresetVideo.LPresetRateControl, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetVideo.LPresetQuality, lSecondPreset.LPresetVideo.LPresetQuality, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetVideo.LPresetSpeedPreset, lSecondPreset.LPresetVideo.LPresetSpeedPreset, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetLocation, lSecondPreset.LPresetLocation, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetLocationSubfolder, lSecondPreset.LPresetLocationSubfolder, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetLocationSibling, lSecondPreset.LPresetLocationSibling, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetLocationCustom, lSecondPreset.LPresetLocationCustom, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetVideo.LPresetSize, lSecondPreset.LPresetVideo.LPresetSize, StringComparison.Ordinal)
            && lFirstPreset.LPresetVideo.LPresetSizeReactive == lSecondPreset.LPresetVideo.LPresetSizeReactive
            && string.Equals(lFirstPreset.LPresetVideo.LPresetFps, lSecondPreset.LPresetVideo.LPresetFps, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetVideo.LPresetPixelLayout, lSecondPreset.LPresetVideo.LPresetPixelLayout, StringComparison.Ordinal)
            && LPresetExtraMatch(lFirstPreset.LPresetVideo.LPresetExtras, lSecondPreset.LPresetVideo.LPresetExtras)
            && string.Equals(lFirstPreset.LPresetAudio.LPresetEncoder, lSecondPreset.LPresetAudio.LPresetEncoder, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetAudio.LPresetRateControl, lSecondPreset.LPresetAudio.LPresetRateControl, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetAudio.LPresetQuality, lSecondPreset.LPresetAudio.LPresetQuality, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetAudio.LPresetSpeed, lSecondPreset.LPresetAudio.LPresetSpeed, StringComparison.Ordinal)
            && LPresetExtraMatch(lFirstPreset.LPresetAudio.LPresetExtras, lSecondPreset.LPresetAudio.LPresetExtras)
            && string.Equals(lFirstPreset.LPresetAudio.LPresetSampleRate, lSecondPreset.LPresetAudio.LPresetSampleRate, StringComparison.Ordinal)
            && string.Equals(lFirstPreset.LPresetAudio.LPresetChannels, lSecondPreset.LPresetAudio.LPresetChannels, StringComparison.Ordinal);
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

    public static string LPresetNameCreate(string lBaseName)
    {
        if (!LPresetNames.Any(lName => string.Equals(lName, lBaseName, StringComparison.OrdinalIgnoreCase)))
        {
            return lBaseName;
        }

        for (int lIndex = 2; ; lIndex++)
        {
            string lCandidate = $"{lBaseName} {lIndex}";
            if (!LPresetNames.Any(lName => string.Equals(lName, lCandidate, StringComparison.OrdinalIgnoreCase)))
            {
                return lCandidate;
            }
        }
    }

    public static string LPresetFileFormat(string lPresetName)
    {
        char[] lInvalidCharacters = Path.GetInvalidFileNameChars();
        return new string(lPresetName
            .Trim()
            .Select(lCharacter => lInvalidCharacters.Contains(lCharacter) ? '_' : lCharacter)
            .ToArray());
    }

    public static string LPresetNameResolve(string lStoredName, string lFilePath)
    {
        string lName = lStoredName.Trim();
        return string.IsNullOrWhiteSpace(lName)
            ? Path.GetFileNameWithoutExtension(lFilePath).Trim()
            : lName;
    }

    internal static bool LPresetRecordMatch(LPresetRecord lFirstRecord, LPresetRecord lSecondRecord) =>
        LPresetValueMatch(LPresetStateCreate(lFirstRecord), LPresetStateCreate(lSecondRecord));

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
