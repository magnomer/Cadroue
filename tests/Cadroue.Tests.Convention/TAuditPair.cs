using Xunit;

namespace Convention.Tests;

internal static class TAuditPair
{
    public static void TAuditPairCheck(
        string audit,
        IReadOnlyList<TAuditHit> hits,
        string kind,
        IReadOnlyDictionary<string, int> ceilings,
        IReadOnlyList<string> waivers,
        string summary)
    {
        List<string> over = [];
        int pairs = 0;
        foreach (IGrouping<string, TAuditHit> pair in hits
                     .Where(hit => hit.TAuditHitKind == kind && hit.TAuditWaiverRead(waivers).Length == 0)
                     .GroupBy(hit => hit.TAuditPairRead(), StringComparer.Ordinal))
        {
            int names = pair.Count();
            int ceiling = ceilings.GetValueOrDefault(pair.Key);
            if (names <= ceiling)
            {
                continue;
            }

            pairs++;
            over.Add($"  {pair.Key}: {names} name(s), ceiling {ceiling}");
            IOrderedEnumerable<TAuditHit> ordered = pair
                .OrderBy(hit => hit.TAuditHitPath, StringComparer.Ordinal)
                .ThenBy(hit => hit.TAuditHitLine);
            foreach (TAuditHit hit in ordered)
            {
                over.Add($"    {hit.TAuditHitPath}:{hit.TAuditHitLine} {hit.TAuditHitName}");
            }
        }

        Assert.True(over.Count == 0, TAuditConvention.TAuditReportFormat(
            audit,
            $"{pairs} pair(s) hold {summary} above their ceiling:\n{string.Join('\n', over)}"));
    }

    public static void TAuditStaleCheck(
        string audit,
        IReadOnlyList<TAuditHit> hits,
        IReadOnlyList<string> kinds,
        IReadOnlyDictionary<string, int> ceilings,
        IReadOnlyList<string> waivers)
    {
        Dictionary<string, int> counts = hits
            .Where(hit => kinds.Contains(hit.TAuditHitKind, StringComparer.Ordinal))
            .Where(hit => hit.TAuditWaiverRead(waivers).Length == 0)
            .GroupBy(hit => hit.TAuditPairRead(), StringComparer.Ordinal)
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Count(),
                StringComparer.Ordinal);
        List<string> stale = ceilings
            .Where(pair => counts.GetValueOrDefault(pair.Key) < pair.Value)
            .Select(pair => $"  {pair.Key}: {counts.GetValueOrDefault(pair.Key)} name(s), ceiling {pair.Value}")
            .ToList();

        Assert.True(stale.Count == 0, TAuditConvention.TAuditReportFormat(
            audit,
            $"{stale.Count} ceiling(s) sit above the count and must be lowered.\n{string.Join('\n', stale)}"));
    }

    public static void TAuditWaiverCheck(string audit, IReadOnlyList<TAuditHit> hits, IReadOnlyList<string> waivers)
    {
        HashSet<string> used = hits
            .Select(hit => hit.TAuditWaiverRead(waivers))
            .Where(row => row.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        List<string> stale = waivers
            .Where(row => !used.Contains(row))
            .Select(row => $"  {row}")
            .ToList();

        Assert.True(stale.Count == 0, TAuditConvention.TAuditReportFormat(
            audit,
            $"{stale.Count} waiver row(s) match no source line and must be deleted:\n{string.Join('\n', stale)}"));
    }
}
