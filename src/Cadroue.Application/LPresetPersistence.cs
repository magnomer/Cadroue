using Cadroue.Core;

namespace Cadroue.Application;

// The catalogue's durable side: the native and stored presets loaded once into the table, and each change written
// through the save seam as a merge with what is stored, against the baseline last loaded or saved. An unreadable
// stored catalogue blocks every save.
public static class LPresetPersistence
{
    private static readonly List<LPresetDocument> LPresetBaseline = new();

    public static Func<LPresetCatalog>? LPresetLoadSeam;
    public static Func<IReadOnlyList<LPresetGroup>>? LPresetNativeSeam;
    public static Func<Func<LPresetCatalog, IReadOnlyList<LPresetDocument>?>, bool>? LPresetSaveSeam;
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
            foreach (LPresetDocument lRecord in lGroup.LPresetGroupPresets)
            {
                LPresetTable.LPresetNativeAdd(LPresetRange.LPresetRecordNormalize(lRecord), lGroup.LPresetGroupName);
            }
        }
        LPresetCatalog lStoredCatalog = LPresetLoadSeam?.Invoke()
            ?? new LPresetCatalog(LPresetOutcome.LPresetMissing, []);
        foreach (LPresetDocument lRecord in lStoredCatalog.LPresetRecords)
        {
            LPresetRange.LPresetRecordNormalize(lRecord);
        }

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
            LPresetTable.LPresetStoredAdd(lDefault);
            LPresetBaselineReset([]);
            return;
        }

        foreach (LPresetDocument lRecord in lStoredCatalog.LPresetRecords)
        {
            LPresetTable.LPresetStoredAdd(LPreset.LPresetStateCreate(lRecord));
        }

        LPresetBaselineReset(lStoredCatalog.LPresetRecords);
    }

    internal static bool LPresetPersist()
    {
        if (LPresetBlocked)
        {
            LPresetTraceSeam?.Invoke("Export presets were not saved: the stored catalogue is unreadable");
            return false;
        }

        IReadOnlyList<LPresetDocument> lPresetLocal = LPresetTable.LPresetRecordsCreate();
        if (LPresetSaveSeam is null)
        {
            LPresetBaselineReset(lPresetLocal);
            return true;
        }

        IReadOnlyList<LPresetDocument>? lPresetMerged = null;
        bool lPresetSaved = LPresetSaveSeam(lPresetCatalog =>
        {
            if (lPresetCatalog.LPresetOutcome == LPresetOutcome.LPresetUnreadable)
            {
                LPresetBlocked = true;
                return null;
            }

            foreach (LPresetDocument lPresetRecord in lPresetCatalog.LPresetRecords)
            {
                LPresetRange.LPresetRecordNormalize(lPresetRecord);
            }

            lPresetMerged = LPresetMerge.LPresetMergeCreate(
                LPresetBaseline, lPresetLocal, lPresetCatalog.LPresetRecords);
            return lPresetMerged;
        });

        if (!lPresetSaved || lPresetMerged is null)
        {
            LPresetTraceSeam?.Invoke("Export presets could not be written; the change was discarded");
            return false;
        }

        LPresetBaselineReset(lPresetMerged);
        LPresetTable.LPresetMergeApply(lPresetMerged);
        return true;
    }

    private static void LPresetBaselineReset(IReadOnlyList<LPresetDocument> lPresetRecords)
    {
        LPresetBaseline.Clear();
        LPresetBaseline.AddRange(lPresetRecords);
    }
}
