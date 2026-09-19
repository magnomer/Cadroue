using System.Text;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditStrict
{
    private static readonly Lazy<(IReadOnlyList<TViolation> TAuditHits, IReadOnlyList<string> TAuditVeneers)>
        TAuditStrictHits = new(TAuditStrictRead);

    private static readonly Lazy<string> TAuditStrictWritten = new(TAuditReportSave);

    private static readonly string[] TAuditStrictKinds = ["Storage", "Flow", "Treat", "Reach", "Taint"];

    private readonly ITestOutputHelper _tAuditOutput;

    public TAuditStrict(ITestOutputHelper output)
    {
        _tAuditOutput = output;
    }

    [Fact]
    public void AuditStrict_VeneerFields_HoldNothing()
    {
        TAuditStrictCheck("Storage", "veneer field(s) hold state");
    }

    [Fact]
    public void AuditStrict_VeneerMembers_BranchNever()
    {
        TAuditStrictCheck("Flow", "veneer member(s) branch or compute");
    }

    [Fact]
    public void AuditStrict_ShellSources_TreatNoData()
    {
        TAuditStrictCheck("Treat", "shell line(s) compute over logic values");
    }

    [Fact]
    public void AuditStrict_ShellMarkup_ReachNoLogic()
    {
        TAuditStrictCheck("Reach", "markup line(s) reach into logic");
    }

    [Fact]
    public void AuditStrict_ShellLocals_CarryNoLogic()
    {
        TAuditStrictCheck("Taint", "shell line(s) compute over a carried logic value or control text");
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
        IReadOnlySet<ISymbol> readers = TAuditTruthWalker.TAuditReaderRead(sources);
        IReadOnlyList<TViolation> hits = TAuditStrictWalker.TAuditRun(sources, out List<string> veneers)
            .Concat(TAuditReachWalker.TAuditRun(markups))
            .Concat(TAuditTaintWalker.TAuditRun(sources, readers))
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
        text.AppendLine($"# Strict shell audit {version}");
        text.AppendLine();
        text.AppendLine($"- Generation: {TAuditConvention.TAuditGeneration}");
        text.AppendLine($"- Enforced: {TAuditStrictSetting.TAuditStrictEnforced}");
        text.AppendLine($"- Veneer classes: {veneers.Count}");
        foreach (string kind in TAuditStrictKinds)
        {
            int count = hits.Count(hit => string.Equals(hit.TViolationKind, kind, StringComparison.Ordinal));
            text.AppendLine($"- {kind}: {count}");
        }

        text.AppendLine();
        text.AppendLine("## Veneer classes by member");
        text.AppendLine();
        text.AppendLine("| Class | Storage | Flow | Treat |");
        text.AppendLine("|---|---|---|---|");
        foreach (string veneer in veneers)
        {
            int storage = TAuditClassRead(hits, veneer, "Storage");
            int flow = TAuditClassRead(hits, veneer, "Flow");
            int treat = hits.Count(hit => string.Equals(hit.TViolationKind, "Treat", StringComparison.Ordinal)
                && Path.GetFileName(hit.TViolationPath).StartsWith(veneer, StringComparison.Ordinal));
            text.AppendLine($"| {veneer} | {storage} | {flow} | {treat} |");
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
