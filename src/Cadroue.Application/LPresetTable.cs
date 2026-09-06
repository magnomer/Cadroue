using Cadroue.Core;
using System.Collections.ObjectModel;
using System.IO;

namespace Cadroue.Application;

public sealed partial class LPreset
{
    private static readonly Dictionary<string, LPreset> LPresetMap = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> LPresetNativeNames = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> LPresetGroupMap = new(StringComparer.OrdinalIgnoreCase);

    private static readonly List<LPresetRecord> LPresetBaseline = new();

    public static Func<LPresetCatalog>? LPresetLoadSeam;
    public static Func<IReadOnlyList<LPresetGroup>>? LPresetNativeSeam;
    public static Func<Func<LPresetCatalog, IReadOnlyList<LPresetRecord>?>, bool>? LPresetSaveSeam;
    public static Action<string>? LPresetTraceSeam;

    private static bool LPresetPrepared;
    private static bool LPresetBlocked;

    public static void LPresetPrepare()
    {
        if (LPresetPrepared)
        {
            return;
        }
        LPresetPrepared = true;

        IReadOnlyList<LPresetGroup> lNativeGroups = LPresetNativeSeam?.Invoke() ?? [];
        foreach (LPresetGroup lGroup in lNativeGroups)
        {
            foreach (LPresetRecord lRecord in lGroup.LPresetGroupPresets)
            {
                LPresetNativeAdd(lRecord, lGroup.LPresetGroupName);
            }
        }
        LPresetCatalog lStoredCatalog = LPresetLoadSeam?.Invoke()
            ?? new LPresetCatalog(LPresetOutcome.LPresetMissing, []);
        LPresetBlocked = lStoredCatalog.LPresetOutcome == LPresetOutcome.LPresetUnreadable;
        if (LPresetBlocked)
        {
            LPresetTraceSeam?.Invoke(
                "Export presets could not be read; the stored catalogue is left untouched and preset changes are blocked");
        }

        if (lStoredCatalog.LPresetOutcome != LPresetOutcome.LPresetLoaded)
        {
            var lDefault = new LPreset { LPresetName = "MP4_H264_AAC_Default" };
            LPresetStoredAdd(lDefault);
            LPresetBaselineReset([]);
            return;
        }

        foreach (LPresetRecord lRecord in lStoredCatalog.LPresetRecords)
        {
            LPresetStoredAdd(LPreset.LPresetStateCreate(lRecord));
        }

        LPresetBaselineReset(lStoredCatalog.LPresetRecords);
    }

    private static void LPresetNativeAdd(LPresetRecord lRecord, string lGroupName)
    {
        LPreset lPreset = LPresetStateCreate(lRecord);
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

    // Every mutation is staged the same way: change memory, write storage, and keep the change
    // only if the write lands. A failed or blocked write restores the catalogue exactly as it
    // was, so nothing stays committed in memory that storage does not hold.
    private static bool LPresetTransactionRun(Func<bool> lPresetMutate)
    {
        string[] lPresetNamesBackup = [.. LPresetNames];
        var lPresetMapBackup = new Dictionary<string, LPreset>(LPresetMap, StringComparer.OrdinalIgnoreCase);
        if (lPresetMutate() && LPresetPersist())
        {
            LPresetStoreChange?.Invoke();
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

        LPresetStoreChange?.Invoke();
    }

    private static bool LPresetPersist()
    {
        if (LPresetBlocked)
        {
            LPresetTraceSeam?.Invoke("Export presets were not saved: the stored catalogue is unreadable");
            return false;
        }

        IReadOnlyList<LPresetRecord> lPresetLocal = LPresetRecordsCreate();
        if (LPresetSaveSeam is null)
        {
            LPresetBaselineReset(lPresetLocal);
            return true;
        }

        IReadOnlyList<LPresetRecord>? lPresetMerged = null;
        bool lPresetSaved = LPresetSaveSeam(lPresetCatalog =>
        {
            if (lPresetCatalog.LPresetOutcome == LPresetOutcome.LPresetUnreadable)
            {
                LPresetBlocked = true;
                return null;
            }

            lPresetMerged = LPresetMergeCreate(LPresetBaseline, lPresetLocal, lPresetCatalog.LPresetRecords);
            return lPresetMerged;
        });

        if (!lPresetSaved || lPresetMerged is null)
        {
            LPresetTraceSeam?.Invoke("Export presets could not be written; the change was discarded");
            return false;
        }

        LPresetMergeApply(lPresetMerged);
        return true;
    }

    private static IReadOnlyList<LPresetRecord> LPresetRecordsCreate()
    {
        var lPresets = new List<LPresetRecord>();
        foreach (string lName in LPresetNames)
        {
            if (!LPresetNativeCheck(lName) && LPresetMap.TryGetValue(lName, out LPreset? lPreset))
            {
                lPresets.Add(lPreset.LPresetClone().LPresetRecordCreate());
            }
        }

        return lPresets;
    }

    // The merged list is what storage now holds, including anything another process committed,
    // so the live catalogue adopts it instead of keeping a view storage no longer matches.
    private static void LPresetMergeApply(IReadOnlyList<LPresetRecord> lPresetRecords)
    {
        LPresetBaselineReset(lPresetRecords);
        var lPresetStoredNames = new List<string>();
        foreach (LPresetRecord lPresetRecord in lPresetRecords)
        {
            LPreset lPreset = LPresetStateCreate(lPresetRecord);
            if (string.IsNullOrWhiteSpace(lPreset.LPresetName) || LPresetNativeCheck(lPreset.LPresetName))
            {
                continue;
            }

            lPreset.LPresetName = lPreset.LPresetName.Trim();
            LPresetMap.TryGetValue(lPreset.LPresetName, out LPreset? lPresetPrevious);
            LPresetMap[lPreset.LPresetName] = lPreset;
            lPresetStoredNames.Add(lPreset.LPresetName);
            LPresetSelection.LPresetDraftSync(
                lPreset.LPresetName, lPresetRecord, lPresetPrevious?.LPresetRecordCreate());
        }

        foreach (string lName in LPresetMap.Keys.ToArray())
        {
            if (!LPresetNativeCheck(lName)
                && !lPresetStoredNames.Contains(lName, StringComparer.OrdinalIgnoreCase))
            {
                LPresetMap.Remove(lName);
                LPresetSelection.LPresetDraftSync(lName, null, null);
            }
        }

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

    private static void LPresetBaselineReset(IReadOnlyList<LPresetRecord> lPresetRecords)
    {
        LPresetBaseline.Clear();
        LPresetBaseline.AddRange(lPresetRecords);
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
