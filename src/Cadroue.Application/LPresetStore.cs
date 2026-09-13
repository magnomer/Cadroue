using Cadroue.Core;

namespace Cadroue.Application;

public sealed partial class LPreset
{
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
                "Export presets could not be read; the stored catalogue is left untouched " +
                "and preset changes are blocked");
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
}
