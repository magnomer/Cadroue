using Cadroue.Core;

namespace Cadroue.Application;

// The three-way merge of the local catalogue with the stored one, against the baseline both started from: a local
// preset left untouched takes the stored version, and a stored preset the baseline never held is added.
internal static class LPresetMerge
{
    internal static IReadOnlyList<LPresetDocument> LPresetMergeCreate(
        IReadOnlyList<LPresetDocument> lPresetBaseline,
        IReadOnlyList<LPresetDocument> lPresetLocal,
        IReadOnlyList<LPresetDocument> lPresetStored)
    {
        Dictionary<string, LPresetDocument> lPresetBaselineMap = LPresetLookupCreate(lPresetBaseline);
        Dictionary<string, LPresetDocument> lPresetStoredMap = LPresetLookupCreate(lPresetStored);
        var lPresetMerged = new List<LPresetDocument>();
        var lPresetTaken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (LPresetDocument lPresetRecord in lPresetLocal)
        {
            string lPresetName = lPresetRecord.LPresetName;
            bool lPresetUntouched = lPresetBaselineMap.TryGetValue(lPresetName, out LPresetDocument? lPresetBase)
                && LPresetComparison.LPresetRecordMatch(lPresetBase, lPresetRecord);
            if (lPresetUntouched)
            {
                if (lPresetStoredMap.TryGetValue(lPresetName, out LPresetDocument? lPresetDurable))
                {
                    lPresetMerged.Add(lPresetDurable);
                    lPresetTaken.Add(lPresetName);
                }

                continue;
            }

            lPresetMerged.Add(lPresetRecord);
            lPresetTaken.Add(lPresetName);
        }

        foreach (LPresetDocument lPresetRecord in lPresetStored)
        {
            if (!lPresetTaken.Contains(lPresetRecord.LPresetName)
                && !lPresetBaselineMap.ContainsKey(lPresetRecord.LPresetName))
            {
                lPresetMerged.Add(lPresetRecord);
            }
        }

        return lPresetMerged;
    }

    private static Dictionary<string, LPresetDocument> LPresetLookupCreate(
        IReadOnlyList<LPresetDocument> lPresetRecords)
    {
        var lPresetMap = new Dictionary<string, LPresetDocument>(StringComparer.OrdinalIgnoreCase);
        foreach (LPresetDocument lPresetRecord in lPresetRecords)
        {
            lPresetMap[lPresetRecord.LPresetName] = lPresetRecord;
        }

        return lPresetMap;
    }
}
