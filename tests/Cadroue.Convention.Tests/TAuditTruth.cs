using System.Text;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditTruth
{
    private static readonly Lazy<IReadOnlyList<TViolation>> TAuditTruthHits = new(TAuditTruthRead);

    private static readonly Lazy<string> TAuditTruthWritten = new(TAuditReportSave);

    private static readonly string[] TAuditTruthKinds =
        ["Argument", "Guard", "Fork", "Mirror", "Mutation", "Shape", "Treat", "Taint"];

    private static readonly string[] TAuditLineKinds = ["Mutation", "Treat", "Taint"];

    private readonly ITestOutputHelper _tAuditOutput;

    public TAuditTruth(ITestOutputHelper output)
    {
        _tAuditOutput = output;
    }

    [Fact]
    public void AuditTruth_DeportmentFields_ReachNoRequest()
    {
        TAuditTruthCheck("Argument", "deportment field(s) carry a value into an engine request");
        TAuditTruthCheck("Guard", "deportment field(s) decide an engine request");
    }

    [Fact]
    public void AuditTruth_DeportmentFields_KeepOneWriter()
    {
        TAuditTruthCheck("Fork", "deportment field(s) are written by the engine and by the deportment");
    }

    [Fact]
    public void AuditTruth_DeportmentFields_HoldNoLogic()
    {
        TAuditTruthCheck("Mirror", "deportment field(s) hold a logic value");
    }

    [Fact]
    public void AuditTruth_DeportmentSources_MutateNoLogic()
    {
        TAuditTruthCheck("Mutation", "deportment line(s) assign a logic member");
    }

    [Fact]
    public void AuditTruth_DeportmentShape_DrivesNoRequest()
    {
        TAuditTruthCheck("Shape", "deportment control(s), timer(s) or handler(s) drive a request");
    }

    [Fact]
    public void AuditTruth_DeportmentSources_TreatNoData()
    {
        TAuditTruthCheck("Treat", "deportment line(s) compute over logic values");
    }

    [Fact]
    public void AuditTruth_DeportmentLocals_CarryNoData()
    {
        TAuditTruthCheck("Taint", "deportment line(s) compute over a carried logic value or control text");
    }

    [Fact]
    public void AuditTruth_Ceiling_MatchesHits()
    {
        List<string> stale = [];
        foreach ((string kind, int ceiling) in TAuditTruthSetting.TAuditTruthCeiling)
        {
            int count = TAuditKindRead(kind);
            if (count < ceiling)
            {
                stale.Add($"  {kind}: {count} hit(s), ceiling {ceiling}");
            }
        }

        Assert.True(stale.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITTRUTH",
            $"{stale.Count} ceiling(s) sit above the count and must be lowered.\n{string.Join('\n', stale)}"));
    }

    private static int TAuditKindRead(string kind)
    {
        return TAuditTruthHits.Value.Count(hit => string.Equals(hit.TViolationKind, kind, StringComparison.Ordinal));
    }

    private void TAuditTruthCheck(string kind, string summary)
    {
        int count = TAuditKindRead(kind);
        int ceiling = TAuditTruthSetting.TAuditTruthCeiling.GetValueOrDefault(kind);
        string report = TAuditTruthWritten.Value;
        _tAuditOutput.WriteLine($"AUDITTRUTH {kind}: {count} {summary}, ceiling {ceiling}. Report: {report}");

        Assert.True(!TAuditTruthSetting.TAuditTruthEnforced || count <= ceiling, TAuditConvention.TAuditReportFormat(
            "AUDITTRUTH",
            $"{count} {summary}, above the ceiling of {ceiling}. See {report}"));
    }

    private static string TAuditReportSave()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        string version = TAuditSource.TAuditVersionRead(repoRoot);
        string path = Path.Combine(repoRoot, string.Format(TAuditTruthSetting.TAuditTruthReport, version));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        IReadOnlyList<TViolation> hits = TAuditTruthHits.Value;

        StringBuilder text = new();
        text.AppendLine($"# Deportment audit {version}");
        text.AppendLine();
        text.AppendLine($"- Generation: {TAuditConvention.TAuditGeneration}");
        text.AppendLine($"- Enforced: {TAuditTruthSetting.TAuditTruthEnforced}");
        foreach (string kind in TAuditTruthKinds)
        {
            text.AppendLine($"- {kind}: {TAuditKindRead(kind)}");
        }

        text.AppendLine();
        text.AppendLine("## Fields by hit count");
        text.AppendLine();
        text.AppendLine("| Field | Hits |");
        text.AppendLine("|---|---|");
        IEnumerable<IGrouping<string, TViolation>> fields = hits
            .Where(hit => !TAuditLineKinds.Contains(hit.TViolationKind, StringComparer.Ordinal))
            .GroupBy(hit => hit.TViolationName, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal);
        foreach (IGrouping<string, TViolation> field in fields)
        {
            text.AppendLine($"| `{field.Key}` | {field.Count()} |");
        }

        foreach (string kind in TAuditTruthKinds)
        {
            text.AppendLine();
            text.AppendLine($"## {kind}");
            text.AppendLine();
            foreach (TViolation hit in hits
                         .OrderBy(hit => hit.TViolationPath, StringComparer.Ordinal)
                         .ThenBy(hit => hit.TViolationLine))
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

    private static IReadOnlyList<TViolation> TAuditTruthRead()
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
        Assert.True(sources.Count > 0, TAuditConvention.TAuditReportFormat(
            "AUDITTRUTH", $"No tracked file matches {string.Join(' ', TAuditTruthSetting.TAuditShellInclude)}."));

        IReadOnlySet<ISymbol> readers = TAuditTruthWalker.TAuditReaderRead(sources);
        return TAuditTruthWalker.TAuditRun(sources)
            .Concat(TAuditTreatWalker.TAuditRun(sources))
            .Concat(TAuditTaintWalker.TAuditRun(sources, readers))
            .Select(hit => hit with
            {
                TViolationPath = Path.GetRelativePath(repoRoot, hit.TViolationPath).Replace('\\', '/')
            })
            .ToList();
    }
}
