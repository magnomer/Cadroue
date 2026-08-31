using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cadroue.Core;

public readonly record struct LSeriesGroup(string LSeriesName, IReadOnlyList<string> LSeriesPaths);

public enum LSeriesNameMode
{
    LSeriesNameBase,
    LSeriesNameFirst
}

public static class LSeries
{
    private static readonly Regex lSeriesNumberPattern =
        new(@"^(?<base>.*?)\s*\((?<number>\d+)\)\s*$", RegexOptions.CultureInvariant);

    public static IReadOnlyList<LSeriesGroup> LSeriesResolve(
        IReadOnlyList<string> lSeriesPaths,
        bool lSeriesStrict,
        LSeriesNameMode lSeriesNameMode = LSeriesNameMode.LSeriesNameBase)
    {
        var lSeriesBuckets = new List<(string? Base, List<LSeriesItem> Items)>();
        var lSeriesBaseIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (string lSeriesPath in lSeriesPaths)
        {
            LSeriesItem lSeriesItem = LSeriesItemParse(lSeriesPath);
            if (lSeriesItem.LSeriesNumber is null)
            {
                lSeriesBuckets.Add((null, new List<LSeriesItem> { lSeriesItem }));
                continue;
            }

            if (!lSeriesBaseIndex.TryGetValue(lSeriesItem.LSeriesBase, out int lSeriesIndex))
            {
                lSeriesIndex = lSeriesBuckets.Count;
                lSeriesBaseIndex[lSeriesItem.LSeriesBase] = lSeriesIndex;
                lSeriesBuckets.Add((lSeriesItem.LSeriesBase, new List<LSeriesItem>()));
            }

            lSeriesBuckets[lSeriesIndex].Items.Add(lSeriesItem);
        }

        var lSeriesGroups = new List<LSeriesGroup>();
        foreach ((string? lSeriesBase, List<LSeriesItem> lSeriesItems) in lSeriesBuckets)
        {
            if (lSeriesBase is null)
            {
                lSeriesGroups.Add(LSeriesGroupCreate(lSeriesItems[0].LSeriesStem, lSeriesItems));
                continue;
            }

            List<LSeriesItem> lSeriesSorted = lSeriesItems.OrderBy(lSeriesItem => lSeriesItem.LSeriesNumber!.Value).ToList();
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
                    : lSeriesRun[0].LSeriesStem;
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
            if (lSeriesCurrent is null || lSeriesItem.LSeriesNumber != lSeriesPrevious + 1)
            {
                lSeriesCurrent = new List<LSeriesItem>();
                lSeriesRuns.Add(lSeriesCurrent);
            }

            lSeriesCurrent.Add(lSeriesItem);
            lSeriesPrevious = lSeriesItem.LSeriesNumber;
        }

        return lSeriesRuns;
    }

    private static LSeriesGroup LSeriesGroupCreate(string lSeriesName, IEnumerable<LSeriesItem> lSeriesItems) =>
        new(lSeriesName, lSeriesItems.Select(lSeriesItem => lSeriesItem.LSeriesPath).ToList());

    private static string LSeriesNameResolve(
        string lSeriesBase,
        IReadOnlyList<LSeriesItem> lSeriesItems,
        LSeriesNameMode lSeriesNameMode) =>
        lSeriesItems.Count > 1 && lSeriesNameMode == LSeriesNameMode.LSeriesNameBase
            ? lSeriesBase
            : lSeriesItems[0].LSeriesStem;

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

    private readonly record struct LSeriesItem(string LSeriesPath, string LSeriesStem, string LSeriesBase, int? LSeriesNumber);
}
