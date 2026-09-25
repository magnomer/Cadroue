using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditStrict
{
    private static readonly Lazy<(IReadOnlyList<TViolation> TAuditHits, IReadOnlyList<string> TAuditVeneers)>
        TAuditStrictHits = new(TAuditStrictRead);

    private static readonly Lazy<string> TAuditStrictWritten = new(TAuditReportSave);

    private static readonly string[] TAuditStrictKinds = ["Storage", "Call", "Engine", "Reach", "Trigger"];

    private static readonly Regex TAuditLiteralPattern = new(
        @"@?""(?:[^""\\]|\\.)*""|//.*$",
        RegexOptions.Compiled);

    private readonly ITestOutputHelper _tAuditOutput;

    public TAuditStrict(ITestOutputHelper output)
    {
        _tAuditOutput = output;
    }

    [Fact]
    public void AuditStrict_VeneerFields_HoldNothing()
    {
        TAuditStrictCheck("Storage", "veneer field(s), property(ies) or parameter(s) hold state");
    }

    [Fact]
    public void AuditStrict_VeneerMembers_CallOnly()
    {
        TAuditStrictCheck("Call", "veneer line(s) do more than call a function");
    }

    [Fact]
    public void AuditStrict_VeneerSources_ReachNoEngine()
    {
        TAuditStrictCheck("Engine", "veneer line(s) reach the engine");
    }

    [Fact]
    public void AuditStrict_VeneerMarkup_ReachNoEngine()
    {
        TAuditStrictCheck("Reach", "markup line(s) reach the engine");
    }

    [Fact]
    public void AuditStrict_VeneerMarkup_BranchNever()
    {
        TAuditStrictCheck("Trigger", "markup line(s) branch or compute");
    }

    [Fact]
    public void AuditStrict_DeportmentSources_ReachNoMarkup()
    {
        List<string> hits = TAuditSourceScan(
            TAuditStrictSetting.TAuditDeportmentInclude,
            TAuditStrictSetting.TAuditMarkupPatterns,
            TAuditStrictSetting.TAuditMarkupExempt);

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITSTRICT",
            $"{hits.Count} deportment line(s) reach the file system:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditStrict_VeneerSources_HoldNoCatalog()
    {
        List<string> hits = TAuditSourceScan(
            TAuditStrictSetting.TAuditVeneerInclude,
            TAuditStrictSetting.TAuditCatalogPatterns,
            TAuditStrictSetting.TAuditCatalogExempt);

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITSTRICT",
            $"{hits.Count} veneer line(s) do file, JSON, regex, process or task work:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditStrict_Ceiling_MatchesHits()
    {
        List<string> stale = [];
        foreach ((string kind, int ceiling) in TAuditStrictSetting.TAuditStrictCeiling)
        {
            int count = TAuditStrictHits.Value.TAuditHits.Count(hit =>
                string.Equals(hit.TViolationKind, kind, StringComparison.Ordinal));
            if (count < ceiling)
            {
                stale.Add($"  {kind}: {count} hit(s), ceiling {ceiling}");
            }
        }

        Assert.True(stale.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITSTRICT",
            $"{stale.Count} ceiling(s) sit above the count and must be lowered.\n{string.Join('\n', stale)}"));
    }

    private void TAuditStrictCheck(string kind, string summary)
    {
        List<TViolation> hits = TAuditStrictHits.Value.TAuditHits
            .Where(hit => string.Equals(hit.TViolationKind, kind, StringComparison.Ordinal))
            .ToList();
        string report = TAuditStrictWritten.Value;
        int ceiling = TAuditStrictSetting.TAuditStrictCeiling.GetValueOrDefault(kind);
        _tAuditOutput.WriteLine($"AUDITSTRICT {kind}: {hits.Count} {summary}, ceiling {ceiling}. Report: {report}");

        bool held = !TAuditStrictSetting.TAuditStrictEnforced || hits.Count <= ceiling;
        Assert.True(held, TAuditConvention.TAuditReportFormat(
            "AUDITSTRICT",
            $"{hits.Count} {summary}, above the ceiling of {ceiling}. See {report}"));
    }

    private static List<string> TAuditSourceScan(
        IReadOnlyList<string> include, IReadOnlyList<string> forbidden, IReadOnlyList<string> exempt)
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            [],
            include,
            TAuditNameSetting.TAuditExcludedSegments,
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            []);
        List<string> hits = [];
        foreach (string path in TAuditSource.TAuditFileRead(repoRoot, scope))
        {
            if (exempt.Contains(Path.GetFileName(path), StringComparer.Ordinal))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(path);
            for (int index = 0; index < lines.Length; index++)
            {
                string bare = TAuditLiteralPattern.Replace(lines[index], string.Empty);
                foreach (string pattern in forbidden)
                {
                    if (Regex.IsMatch(bare, pattern))
                    {
                        string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                        hits.Add($"  {relative}:{index + 1} {pattern}");
                    }
                }
            }
        }

        return hits;
    }

    private static (IReadOnlyList<TViolation> TAuditHits, IReadOnlyList<string> TAuditVeneers) TAuditStrictRead()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            [],
            TAuditTruthSetting.TAuditShellInclude,
            TAuditNameSetting.TAuditExcludedSegments,
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            []);
        IReadOnlyList<string> sources = TAuditSource.TAuditFileRead(repoRoot, scope);
        TAuditScope markup = new(
            [],
            TAuditStrictSetting.TAuditReachInclude,
            TAuditNameSetting.TAuditExcludedSegments,
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            []);
        IReadOnlyList<string> markups = TAuditSource.TAuditFileRead(repoRoot, markup);
        IReadOnlyList<TViolation> hits = TAuditStrictWalker.TAuditRun(sources, out List<string> veneers)
            .Concat(TAuditReachWalker.TAuditRun(markups))
            .Select(hit => hit with
            {
                TViolationPath = Path.GetRelativePath(repoRoot, hit.TViolationPath).Replace('\\', '/')
            })
            .OrderBy(hit => hit.TViolationPath, StringComparer.Ordinal)
            .ThenBy(hit => hit.TViolationLine)
            .ToList();
        veneers.Sort(StringComparer.Ordinal);
        return (hits, veneers);
    }

    private static string TAuditReportSave()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        (IReadOnlyList<TViolation> hits, IReadOnlyList<string> veneers) = TAuditStrictHits.Value;
        string version = TAuditSource.TAuditVersionRead(repoRoot);
        string path = Path.Combine(repoRoot, string.Format(TAuditStrictSetting.TAuditStrictReport, version));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        StringBuilder text = new();
        text.AppendLine($"# Veneer audit {version}");
        text.AppendLine();
        text.AppendLine($"- Generation: {TAuditConvention.TAuditGeneration}");
        text.AppendLine($"- Enforced: {TAuditStrictSetting.TAuditStrictEnforced}");
        text.AppendLine($"- Veneer types: {veneers.Count}");
        foreach (string kind in TAuditStrictKinds)
        {
            int count = hits.Count(hit => string.Equals(hit.TViolationKind, kind, StringComparison.Ordinal));
            text.AppendLine($"- {kind}: {count}");
        }

        text.AppendLine();
        text.AppendLine("## Veneer types by member");
        text.AppendLine();
        text.AppendLine("| Class | Storage | Call | Engine |");
        text.AppendLine("|---|---|---|---|");
        foreach (string veneer in veneers)
        {
            int storage = TAuditClassRead(hits, veneer, "Storage");
            int call = TAuditClassRead(hits, veneer, "Call");
            int engine = TAuditClassRead(hits, veneer, "Engine");
            text.AppendLine($"| {veneer} | {storage} | {call} | {engine} |");
        }

        foreach (string kind in TAuditStrictKinds)
        {
            text.AppendLine();
            text.AppendLine($"## {kind}");
            text.AppendLine();
            foreach (TViolation hit in hits)
            {
                if (!string.Equals(hit.TViolationKind, kind, StringComparison.Ordinal))
                {
                    continue;
                }

                text.AppendLine(
                    $"- `{hit.TViolationPath}:{hit.TViolationLine}` `{hit.TViolationName}` {hit.TViolationReason}");
            }
        }

        File.WriteAllText(path, text.ToString());
        return Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
    }

    private static int TAuditClassRead(IReadOnlyList<TViolation> hits, string type, string kind)
    {
        return hits.Count(hit => string.Equals(hit.TViolationKind, kind, StringComparison.Ordinal)
            && hit.TViolationName.StartsWith(type + ".", StringComparison.Ordinal));
    }
}
