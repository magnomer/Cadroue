using Cadroue.Core;
using System.Collections.ObjectModel;

namespace Cadroue.Application;

// The preset catalogue in memory and its only writer: the native presets first, then the stored ones in their order.
// Every edit runs as a transaction that the durable side persists, and is rolled back when it cannot.
public static class LPresetTable
{
    private static readonly Dictionary<string, LPreset> LPresetMap = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> LPresetNativeNames = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> LPresetGroupMap = new(StringComparer.OrdinalIgnoreCase);

    public static ObservableCollection<string> LPresetNames { get; } = new();

    public static event Action? LPresetStoreChanged;

    public static LPreset LPresetInitialCreate(string lPresetTabKey)
    {
        var lPresetState = new LPreset();
        string? lPresetName = lPresetTabKey switch
        {
            "Split" => LPresetNaming.LPresetSplitDefault,
            "Merge" => LPresetNaming.LPresetMergeDefault,
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
        LPresetMap.TryGetValue(lPresetName, out var lPreset) && LPresetComparison.LPresetValueMatch(lPreset, lSource);

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

            LPresetDraft.LPresetDraftSync(lName, null, null);
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

    public static LPresetDocument? LPresetSelectionRead(string lName) =>
        LPresetRead(lName)?.LPresetRecordCreate();

    public static bool LPresetSelectionSave(string lName, LPresetDocument lRecord) =>
        LPresetSave(lName, LPreset.LPresetStateCreate(lRecord));

    public static bool LPresetSelectionSet(string lOldName, string lNewName, LPresetDocument lRecord) =>
        LPresetNameSet(lOldName, lNewName, LPreset.LPresetStateCreate(lRecord));

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

    private static int LPresetNativeRead() =>
        LPresetNames.Count(LPresetNativeCheck);

    internal static void LPresetNativeAdd(LPresetDocument lRecord, string lGroupName)
    {
        LPreset lPreset = LPreset.LPresetStateCreate(lRecord);
        if (string.IsNullOrWhiteSpace(lPreset.LPresetName))
        {
            return;
        }

        lPreset.LPresetName = lPreset.LPresetName.Trim();
        LPresetMap[lPreset.LPresetName] = lPreset.LPresetClone();
        LPresetNativeNames.Add(lPreset.LPresetName);
        LPresetGroupMap[lPreset.LPresetName] = lGroupName;
        LPresetNames.Add(lPreset.LPresetName);
    }

    internal static void LPresetStoredAdd(LPreset lPreset)
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

    // The stored presets, in their order, as the documents the durable side writes.
    internal static IReadOnlyList<LPresetDocument> LPresetRecordsCreate()
    {
        var lPresets = new List<LPresetDocument>();
        foreach (string lName in LPresetNames)
        {
            if (!LPresetNativeCheck(lName) && LPresetMap.TryGetValue(lName, out LPreset? lPreset))
            {
                lPresets.Add(lPreset.LPresetClone().LPresetRecordCreate());
            }
        }

        return lPresets;
    }

    // The merged catalogue the durable side wrote replaces the stored presets; every draft follows its preset.
    internal static void LPresetMergeApply(IReadOnlyList<LPresetDocument> lPresetRecords)
    {
        var lPresetStoredNames = new List<string>();
        foreach (LPresetDocument lPresetRecord in lPresetRecords)
        {
            LPreset lPreset = LPreset.LPresetStateCreate(lPresetRecord);
            if (string.IsNullOrWhiteSpace(lPreset.LPresetName) || LPresetNativeCheck(lPreset.LPresetName))
            {
                continue;
            }

            lPreset.LPresetName = lPreset.LPresetName.Trim();
            LPresetMap.TryGetValue(lPreset.LPresetName, out LPreset? lPresetPrevious);
            LPresetMap[lPreset.LPresetName] = lPreset;
            lPresetStoredNames.Add(lPreset.LPresetName);
            LPresetDraft.LPresetDraftSync(
                lPreset.LPresetName, lPresetRecord, lPresetPrevious?.LPresetRecordCreate());
        }

        LPresetNamesApply(lPresetStoredNames);
        foreach (string lName in LPresetMap.Keys.ToArray())
        {
            if (!LPresetNativeCheck(lName)
                && !lPresetStoredNames.Contains(lName, StringComparer.OrdinalIgnoreCase))
            {
                LPresetMap.Remove(lName);
                LPresetDraft.LPresetDraftSync(lName, null, null);
            }
        }
    }

    private static void LPresetNamesApply(IReadOnlyList<string> lPresetStoredNames)
    {
        if (LPresetNames.Where(lName => !LPresetNativeCheck(lName))
            .SequenceEqual(lPresetStoredNames, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        for (int lIndex = LPresetNames.Count - 1; lIndex >= 0; lIndex--)
        {
            if (!LPresetNativeCheck(LPresetNames[lIndex]))
            {
                LPresetNames.RemoveAt(lIndex);
            }
        }

        foreach (string lName in lPresetStoredNames)
        {
            LPresetNames.Add(lName);
        }
    }

    private static bool LPresetTransactionRun(Func<bool> lPresetMutate)
    {
        string[] lPresetNamesBackup = [.. LPresetNames];
        var lPresetMapBackup = new Dictionary<string, LPreset>(LPresetMap, StringComparer.OrdinalIgnoreCase);
        if (lPresetMutate() && LPresetPersistence.LPresetPersist())
        {
            LPresetStoreChanged?.Invoke();
            return true;
        }

        LPresetRestore(lPresetNamesBackup, lPresetMapBackup);
        return false;
    }

    private static void LPresetRestore(
        IReadOnlyList<string> lPresetNamesBackup,
        Dictionary<string, LPreset> lPresetMapBackup)
    {
        LPresetMap.Clear();
        foreach ((string lName, LPreset lPreset) in lPresetMapBackup)
        {
            LPresetMap[lName] = lPreset;
        }

        LPresetNames.Clear();
        foreach (string lName in lPresetNamesBackup)
        {
            LPresetNames.Add(lName);
        }

        LPresetStoreChanged?.Invoke();
    }
}
