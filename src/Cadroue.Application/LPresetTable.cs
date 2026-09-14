using Cadroue.Core;
using System.Collections.ObjectModel;
using System.IO;

namespace Cadroue.Application;

public sealed partial class LPreset
{
    private static readonly Dictionary<string, LPreset> LPresetMap = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> LPresetNativeNames = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> LPresetGroupMap = new(StringComparer.OrdinalIgnoreCase);

    public static ObservableCollection<string> LPresetNames { get; } = new();

    public static event Action? LPresetStoreChange;

    public static LPreset LPresetInitialCreate(string lPresetTabKey)
    {
        var lPresetState = new LPreset();
        string? lPresetName = lPresetTabKey switch
        {
            "Split" => LPresetSplitDefault,
            "Merge" => LPresetMergeDefault,
            _ => LPresetFirstName
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

    public static bool LPresetMatch(string lPresetName, LPreset lSource) =>
        LPresetMap.TryGetValue(lPresetName, out var lPreset) && LPresetValueMatch(lPreset, lSource);

    public static bool LPresetSave(string lPresetName, LPreset lSource)
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

        return LPresetTransactionRun(() =>
        {
            var lPreset = lSource.LPresetClone();
            lPreset.LPresetName = lName;
            LPresetMap[lName] = lPreset;
            if (!LPresetNames.Any(lExisting => string.Equals(lExisting, lName, StringComparison.OrdinalIgnoreCase)))
            {
                LPresetNames.Add(lName);
            }

            return true;
        });
    }

    public static bool LPresetDelete(string lPresetName)
    {
        if (string.IsNullOrWhiteSpace(lPresetName))
        {
            return false;
        }

        string lName = lPresetName.Trim();
        if (LPresetNativeCheck(lName) || !LPresetMap.ContainsKey(lName))
        {
            return false;
        }

        return LPresetTransactionRun(() =>
        {
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

            LPresetSelection.LPresetDraftSync(lName, null, null);
            return true;
        });
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

        return LPresetTransactionRun(() =>
        {
            var lPreset = lSource.LPresetClone();
            lPreset.LPresetName = lName;
            LPresetMap.Remove(lOldName);
            LPresetMap[lName] = lPreset;
            LPresetNames[lIndex] = lName;
            return true;
        });
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

        return LPresetTransactionRun(() =>
        {
            string lName = LPresetNames[lSourceIndex];
            LPresetNames.RemoveAt(lSourceIndex);
            LPresetNames.Insert(lTargetIndex, lName);
            return true;
        });
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

    public static bool LPresetNativeCheck(string lPresetName) =>
        LPresetNativeNames.Contains(lPresetName);

    public static string? LPresetGroupRead(string lPresetName) =>
        LPresetGroupMap.TryGetValue(lPresetName, out string? lGroupName) ? lGroupName : null;

    public static string LPresetDisplayRead(string lPresetName) => lPresetName switch
    {
        LPresetSplitDefault => "Split",
        LPresetMergeDefault => "Merge",
        _ => lPresetName
    };

    private static int LPresetNativeRead() =>
        LPresetNames.Count(LPresetNativeCheck);

    public static IReadOnlyList<string> LPresetExtensionsRead(string lContainer) =>
        LPresetExtensionTable.TryGetValue(lContainer, out IReadOnlyList<string>? lExtensions) ? lExtensions : [];
}
