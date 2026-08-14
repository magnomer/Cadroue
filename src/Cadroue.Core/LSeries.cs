using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cadroue.Core;

public readonly record struct LSeriesGroup(string Name, IReadOnlyList<string> Paths);

public enum LSeriesNameMode
{
    LSeriesNameRemove,
    LSeriesNameFirst
}

public static class LSeries
{
    private static readonly Regex lSeriesNumberPattern =
        new(@"^(?<base>.*?)\s*\((?<number>\d+)\)\s*$", RegexOptions.CultureInvariant);

    public static IReadOnlyList<LSeriesGroup> LSeriesResolve(
        IReadOnlyList<string> lSeriesPaths,
        bool lSeriesStrict,
        LSeriesNameMode lSeriesNameMode = LSeriesNameMode.LSeriesNameRemove)
    {
        var lSeriesBuckets = new List<(string? Base, List<LSeriesItem> Items)>();
        var lSeriesBaseIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (string lSeriesPath in lSeriesPaths)
        {
            LSeriesItem lSeriesItem = LSeriesItemParse(lSeriesPath);
            if (lSeriesItem.Number is null)
            {
                lSeriesBuckets.Add((null, new List<LSeriesItem> { lSeriesItem }));
                continue;
            }

            if (!lSeriesBaseIndex.TryGetValue(lSeriesItem.Base, out int lSeriesIndex))
            {
                lSeriesIndex = lSeriesBuckets.Count;
                lSeriesBaseIndex[lSeriesItem.Base] = lSeriesIndex;
                lSeriesBuckets.Add((lSeriesItem.Base, new List<LSeriesItem>()));
            }

            lSeriesBuckets[lSeriesIndex].Items.Add(lSeriesItem);
        }

        var lSeriesGroups = new List<LSeriesGroup>();
        foreach ((string? lSeriesBase, List<LSeriesItem> lSeriesItems) in lSeriesBuckets)
        {
            if (lSeriesBase is null)
            {
                lSeriesGroups.Add(LSeriesGroupCreate(lSeriesItems[0].Stem, lSeriesItems));
                continue;
            }

            List<LSeriesItem> lSeriesSorted = lSeriesItems.OrderBy(lSeriesItem => lSeriesItem.Number!.Value).ToList();
            if (!lSeriesStrict)
            {
                lSeriesGroups.Add(LSeriesGroupCreate(
                    LSeriesNameResolve(lSeriesBase, lSeriesSorted, lSeriesNameMode), lSeriesSorted));
                continue;
            }

            List<List<LSeriesItem>> lSeriesRuns = LSeriesRunsDivide(lSeriesSorted);
            bool lSeriesMultipleRuns = lSeriesRuns.Count > 1;
            foreach (List<LSeriesItem> lSeriesRun in lSeriesRuns)
            {
                string lSeriesRunName = !lSeriesMultipleRuns
                    ? LSeriesNameResolve(lSeriesBase, lSeriesRun, lSeriesNameMode)
                    : lSeriesRun[0].Stem;
                lSeriesGroups.Add(LSeriesGroupCreate(lSeriesRunName, lSeriesRun));
            }
        }

        return lSeriesGroups;
    }

    private static List<List<LSeriesItem>> LSeriesRunsDivide(List<LSeriesItem> lSeriesSorted)
    {
        var lSeriesRuns = new List<List<LSeriesItem>>();
        List<LSeriesItem>? lSeriesCurrent = null;
        int? lSeriesPrevious = null;
        foreach (LSeriesItem lSeriesItem in lSeriesSorted)
        {
            if (lSeriesCurrent is null || lSeriesItem.Number != lSeriesPrevious + 1)
            {
                lSeriesCurrent = new List<LSeriesItem>();
                lSeriesRuns.Add(lSeriesCurrent);
            }

            lSeriesCurrent.Add(lSeriesItem);
            lSeriesPrevious = lSeriesItem.Number;
        }

        return lSeriesRuns;
    }

    private static LSeriesGroup LSeriesGroupCreate(string lSeriesName, IEnumerable<LSeriesItem> lSeriesItems) =>
        new(lSeriesName, lSeriesItems.Select(lSeriesItem => lSeriesItem.Path).ToList());

    private static string LSeriesNameResolve(
        string lSeriesBase,
        IReadOnlyList<LSeriesItem> lSeriesItems,
        LSeriesNameMode lSeriesNameMode) =>
        lSeriesItems.Count > 1 && lSeriesNameMode == LSeriesNameMode.LSeriesNameRemove
            ? lSeriesBase
            : lSeriesItems[0].Stem;

    private static LSeriesItem LSeriesItemParse(string lSeriesPath)
    {
        string lSeriesStem = Path.GetFileNameWithoutExtension(lSeriesPath);
        Match lSeriesMatch = lSeriesNumberPattern.Match(lSeriesStem);
        if (lSeriesMatch.Success
            && int.TryParse(lSeriesMatch.Groups["number"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int lSeriesNumber))
        {
            return new LSeriesItem(lSeriesPath, lSeriesStem, lSeriesMatch.Groups["base"].Value.Trim(), lSeriesNumber);
        }

        return new LSeriesItem(lSeriesPath, lSeriesStem, lSeriesStem, null);
    }

    private readonly record struct LSeriesItem(string Path, string Stem, string Base, int? Number);
}
