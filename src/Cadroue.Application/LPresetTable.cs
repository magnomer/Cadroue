using Cadroue.Core;
using System.Collections.ObjectModel;
using System.IO;

namespace Cadroue.Application;

public sealed partial class LPreset
{
    private static readonly Dictionary<string, LPreset> LPresetMap = new(StringComparer.OrdinalIgnoreCase);

    public static Func<IReadOnlyList<LPresetRecord>?>? LPresetLoadSeam;
    public static Action<IReadOnlyList<LPresetRecord>>? LPresetSaveSeam;

    private static bool LPresetPrepared;

    public static void LPresetPrepare()
    {
        if (LPresetPrepared)
        {
            return;
        }
        LPresetPrepared = true;

        LPresetNativeAdd(LPresetAudioCreate());
        LPresetNativeAdd(LPresetSplitCreate());
        LPresetNativeAdd(LPresetMergeCreate());
        IReadOnlyList<LPresetRecord>? lStoredPresets = LPresetLoadSeam?.Invoke();
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

    public static event Action? LPresetStoreChange;

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

        lPresetState.LPresetName = string.Empty;
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
            && string.Equals(lPreset.LPresetVideo.LPresetStream, lSource.LPresetVideo.LPresetStream, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetAudio.LPresetStream, lSource.LPresetAudio.LPresetStream, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetVideo.LPresetMode, lSource.LPresetVideo.LPresetMode, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetAudio.LPresetMode, lSource.LPresetAudio.LPresetMode, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetVideo.LPresetEncoder, lSource.LPresetVideo.LPresetEncoder, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetVideo.LPresetRateControl, lSource.LPresetVideo.LPresetRateControl, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetVideo.LPresetQuality, lSource.LPresetVideo.LPresetQuality, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetVideo.LPresetSpeedPreset, lSource.LPresetVideo.LPresetSpeedPreset, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetLocation, lSource.LPresetLocation, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetLocationSubfolder, lSource.LPresetLocationSubfolder, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetLocationSibling, lSource.LPresetLocationSibling, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetLocationCustom, lSource.LPresetLocationCustom, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetVideo.LPresetSize, lSource.LPresetVideo.LPresetSize, StringComparison.Ordinal)
            && lPreset.LPresetVideo.LPresetSizeReactive == lSource.LPresetVideo.LPresetSizeReactive
            && string.Equals(lPreset.LPresetVideo.LPresetFps, lSource.LPresetVideo.LPresetFps, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetVideo.LPresetPixelLayout, lSource.LPresetVideo.LPresetPixelLayout, StringComparison.Ordinal)
            && LPresetExtraMatch(lPreset.LPresetVideo.LPresetExtras, lSource.LPresetVideo.LPresetExtras)
            && string.Equals(lPreset.LPresetAudio.LPresetEncoder, lSource.LPresetAudio.LPresetEncoder, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetAudio.LPresetRateControl, lSource.LPresetAudio.LPresetRateControl, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetAudio.LPresetQuality, lSource.LPresetAudio.LPresetQuality, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetAudio.LPresetSpeed, lSource.LPresetAudio.LPresetSpeed, StringComparison.Ordinal)
            && LPresetExtraMatch(lPreset.LPresetAudio.LPresetExtras, lSource.LPresetAudio.LPresetExtras)
            && string.Equals(lPreset.LPresetAudio.LPresetSampleRate, lSource.LPresetAudio.LPresetSampleRate, StringComparison.Ordinal)
            && string.Equals(lPreset.LPresetAudio.LPresetChannels, lSource.LPresetAudio.LPresetChannels, StringComparison.Ordinal);
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
        LPresetSaveSeam?.Invoke(lPresets.Select(lPreset => lPreset.LPresetRecordCreate()).ToList());
        LPresetStoreChange?.Invoke();
    }

    public static bool LPresetNativeCheck(string lPresetName) =>
        string.Equals(lPresetName, LPresetAudioDefault, StringComparison.OrdinalIgnoreCase)
        || string.Equals(lPresetName, LPresetSplitDefault, StringComparison.OrdinalIgnoreCase)
        || string.Equals(lPresetName, LPresetMergeDefault, StringComparison.OrdinalIgnoreCase);

    public static string LPresetDisplayRead(string lPresetName) => lPresetName switch
    {
        LPresetAudioDefault => "Audio",
        LPresetSplitDefault => "Split",
        LPresetMergeDefault => "Merge",
        _ => lPresetName
    };

    private static int LPresetNativeRead() =>
        LPresetNames.Count(LPresetNativeCheck);

    public static IReadOnlyList<string> LPresetExtensionsRead(string lContainer) =>
        LPresetExtensionTable.TryGetValue(lContainer, out string[]? lExtensions) ? lExtensions : [];
}
