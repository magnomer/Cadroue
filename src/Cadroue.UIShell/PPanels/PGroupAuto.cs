using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PGroup
{
    private static readonly Regex pGroupNumberPattern =
        new(@"^(?<base>.*?)\s*\((?<number>\d+)\)\s*$", RegexOptions.CultureInvariant);

    private void PGroupStrictApply() => PGroupAutoApply(true);

    private void PGroupLooseApply() => PGroupAutoApply(false);

    private void PGroupAutoApply(bool pGroupStrict)
    {
        IReadOnlyList<string> pFiles = PGroupSourceFiles?.Invoke() ?? Array.Empty<string>();
        List<PGroupRecord> pRecords = PGroupAutoCompute(pFiles, pGroupStrict);

        pGroupRecords.Clear();
        pGroupRecords.AddRange(pRecords);
        PGroupRebuild();
    }

    private static List<PGroupRecord> PGroupAutoCompute(IReadOnlyList<string> pFiles, bool pGroupStrict)
    {
        var pBuckets = new List<(string? Base, List<PGroupItem> Items)>();
        var pBaseIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (string pFile in pFiles)
        {
            PGroupItem pItem = PGroupItemParse(pFile);
            if (pItem.Number is null)
            {
                pBuckets.Add((null, new List<PGroupItem> { pItem }));
                continue;
            }

            if (!pBaseIndex.TryGetValue(pItem.Base, out int pIndex))
            {
                pIndex = pBuckets.Count;
                pBaseIndex[pItem.Base] = pIndex;
                pBuckets.Add((pItem.Base, new List<PGroupItem>()));
            }

            pBuckets[pIndex].Items.Add(pItem);
        }

        var pRecords = new List<PGroupRecord>();
        foreach ((string? pBase, List<PGroupItem> pItems) in pBuckets)
        {
            if (pBase is null)
            {
                pRecords.Add(PGroupRecordCreate(pItems[0].Stem, pItems));
                continue;
            }

            List<PGroupItem> pSorted = pItems.OrderBy(pItem => pItem.Number!.Value).ToList();
            if (!pGroupStrict)
            {
                pRecords.Add(PGroupRecordCreate(pSorted.Count > 1 ? pBase : pSorted[0].Stem, pSorted));
                continue;
            }

            List<List<PGroupItem>> pRuns = PGroupRunsSplit(pSorted);
            bool pMultipleRuns = pRuns.Count > 1;
            foreach (List<PGroupItem> pRun in pRuns)
            {
                string pRunName = !pMultipleRuns && pRun.Count > 1 ? pBase : pRun[0].Stem;
                pRecords.Add(PGroupRecordCreate(pRunName, pRun));
            }
        }

        return pRecords;
    }

    private static List<List<PGroupItem>> PGroupRunsSplit(List<PGroupItem> pSorted)
    {
        var pRuns = new List<List<PGroupItem>>();
        List<PGroupItem>? pCurrent = null;
        int? pPrevious = null;
        foreach (PGroupItem pItem in pSorted)
        {
            if (pCurrent is null || pItem.Number != pPrevious + 1)
            {
                pCurrent = new List<PGroupItem>();
                pRuns.Add(pCurrent);
            }

            pCurrent.Add(pItem);
            pPrevious = pItem.Number;
        }

        return pRuns;
    }

    private static PGroupRecord PGroupRecordCreate(string pName, IEnumerable<PGroupItem> pItems)
    {
        var pRecord = new PGroupRecord { PGroupRecordName = pName };
        pRecord.PGroupRecordPaths.AddRange(pItems.Select(pItem => pItem.Path));
        return pRecord;
    }

    private static PGroupItem PGroupItemParse(string pPath)
    {
        string pStem = Path.GetFileNameWithoutExtension(pPath);
        Match pMatch = pGroupNumberPattern.Match(pStem);
        if (pMatch.Success
            && int.TryParse(pMatch.Groups["number"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pNumber))
        {
            return new PGroupItem(pPath, pStem, pMatch.Groups["base"].Value.Trim(), pNumber);
        }

        return new PGroupItem(pPath, pStem, pStem, null);
    }

    private sealed record PGroupItem(string Path, string Stem, string Base, int? Number);
}
