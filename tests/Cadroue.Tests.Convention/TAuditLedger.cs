using System.Text;
using System.Text.Json;
using Xunit;

namespace Convention.Tests;

internal static class TAuditLedger
{
    private const string TAuditLedgerProposal = "temp/audit/{0}.json";

    public static IReadOnlyDictionary<string, Dictionary<string, int>> TAuditLedgerRead(string file)
    {
        string path = Path.Combine(
            TAuditSource.TAuditRootRead(), $"tests/{TAuditNameSetting.TAuditProject}.Tests.Convention", file + ".json");
        Dictionary<string, Dictionary<string, int>> ledger = new(StringComparer.Ordinal);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonProperty kind in document.RootElement.EnumerateObject())
        {
            ledger[kind.Name] = kind.Value.EnumerateObject()
                .ToDictionary(entry => entry.Name, entry => entry.Value.GetInt32(), StringComparer.Ordinal);
        }

        return ledger;
    }

    public static int TAuditLedgerCheck(
        string audit, string file, string kind, IReadOnlyList<TViolation> hits, bool enforced, string summary)
    {
        Dictionary<string, int> ceilings = TAuditLedgerRead(file).GetValueOrDefault(kind) ?? [];
        List<string> over = [];
        List<TViolation> held = hits.Where(hit => hit.TViolationKind == kind).ToList();
        foreach (IGrouping<string, TViolation> place in held
                     .GroupBy(hit => hit.TViolationPath, StringComparer.Ordinal)
                     .OrderBy(place => place.Key, StringComparer.Ordinal))
        {
            int ceiling = ceilings.GetValueOrDefault(place.Key);
            if (place.Count() <= ceiling)
            {
                continue;
            }

            over.Add($"  {place.Key}: {place.Count()} hit(s), ceiling {ceiling}");
            over.AddRange(place
                .OrderBy(hit => hit.TViolationLine)
                .Select(hit => $"    :{hit.TViolationLine} `{hit.TViolationName}` {hit.TViolationReason}"));
        }

        Assert.True(!enforced || over.Count == 0, TAuditConvention.TAuditReportFormat(
            audit,
            $"{kind}: {summary} in a file above its ledger ceiling in {file}.json.\n{string.Join('\n', over)}"));
        return held.Count;
    }

    public static void TAuditStaleCheck(
        string audit, string file, IReadOnlyList<string> kinds, IReadOnlyList<TViolation> hits)
    {
        IReadOnlyDictionary<string, Dictionary<string, int>> ledger = TAuditLedgerRead(file);
        SortedDictionary<string, SortedDictionary<string, int>> counts = new(StringComparer.Ordinal);
        foreach (string kind in kinds)
        {
            counts[kind] = new SortedDictionary<string, int>(hits
                .Where(hit => hit.TViolationKind == kind)
                .GroupBy(hit => hit.TViolationPath, StringComparer.Ordinal)
                .ToDictionary(place => place.Key, place => place.Count(), StringComparer.Ordinal),
                StringComparer.Ordinal);
        }

        List<string> stale = [];
        foreach ((string kind, Dictionary<string, int> ceilings) in ledger.OrderBy(
                     pair => pair.Key, StringComparer.Ordinal))
        {
            SortedDictionary<string, int> found = counts.GetValueOrDefault(kind) ?? [];
            if (!kinds.Contains(kind, StringComparer.Ordinal))
            {
                stale.Add($"  {kind} is no kind this audit counts");
                continue;
            }

            stale.AddRange(ceilings
                .Where(pair => found.GetValueOrDefault(pair.Key) < pair.Value)
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair =>
                    $"  {kind} {pair.Key}: {found.GetValueOrDefault(pair.Key)} hit(s), ceiling {pair.Value}"));
        }

        string proposal = stale.Count == 0 ? string.Empty : TAuditProposalSave(file, counts, ledger);
        Assert.True(stale.Count == 0, TAuditConvention.TAuditReportFormat(
            audit,
            $"{stale.Count} ledger ceiling(s) in {file}.json sit above the count and must be lowered. "
            + $"The lowered ledger is at {proposal}.\n{string.Join('\n', stale)}"));
    }

    private static string TAuditProposalSave(
        string file,
        SortedDictionary<string, SortedDictionary<string, int>> counts,
        IReadOnlyDictionary<string, Dictionary<string, int>> ledger)
    {
        StringBuilder text = new();
        text.Append("{\n");
        List<string> kinds = counts.Keys.Where(kind => counts[kind].Count > 0).ToList();
        for (int index = 0; index < kinds.Count; index++)
        {
            string kind = kinds[index];
            Dictionary<string, int> ceilings = ledger.GetValueOrDefault(kind) ?? [];
            List<KeyValuePair<string, int>> rows = counts[kind]
                .Select(pair => new KeyValuePair<string, int>(
                    pair.Key, Math.Min(pair.Value, ceilings.GetValueOrDefault(pair.Key, pair.Value))))
                .ToList();
            text.Append($"  {JsonSerializer.Serialize(kind)}: {{\n");
            IEnumerable<string> lines = rows.Select(row => $"    {JsonSerializer.Serialize(row.Key)}: {row.Value}");
            text.Append(string.Join(",\n", lines));
            text.Append(index == kinds.Count - 1 ? "\n  }\n" : "\n  },\n");
        }

        text.Append("}\n");
        string root = TAuditSource.TAuditRootRead();
        string path = Path.Combine(root, string.Format(TAuditLedgerProposal, file));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
        return Path.GetRelativePath(root, path).Replace('\\', '/');
    }
}
