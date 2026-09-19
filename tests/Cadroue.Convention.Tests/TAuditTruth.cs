using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditTruth
{
    private static readonly Lazy<IReadOnlyList<TViolation>> TAuditTruthHits = new(TAuditTruthRead);

    private static readonly Lazy<string> TAuditTruthWritten = new(TAuditReportSave);

    private static readonly string[] TAuditTruthKinds = ["Argument", "Guard", "Fork", "Mirror", "Mutation", "Shape"];

    private readonly ITestOutputHelper _tAuditOutput;

    public TAuditTruth(ITestOutputHelper output)
    {
        _tAuditOutput = output;
    }

    [Fact]
    public void AuditTruth_ShellFields_ReachNoRequest()
    {
        TAuditTruthCheck("Argument", "shell field(s) carry a value into an engine request");
        TAuditTruthCheck("Guard", "shell field(s) decide an engine request");
    }

    [Fact]
    public void AuditTruth_ShellFields_KeepOneWriter()
    {
        TAuditTruthCheck("Fork", "shell field(s) are written by the engine and by the shell");
    }

    [Fact]
    public void AuditTruth_ShellFields_HoldNoLogic()
    {
        TAuditTruthCheck("Mirror", "shell field(s) hold a logic value");
    }

    [Fact]
    public void AuditTruth_ShellSources_MutateNoLogic()
    {
        TAuditTruthCheck("Mutation", "shell line(s) assign a logic member");
    }

    [Fact]
    public void AuditTruth_ShellShape_DrivesNoRequest()
    {
        TAuditTruthCheck("Shape", "shell control(s), timer(s) or handler(s) drive a request");
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
        text.AppendLine($"# Custody audit {version}");
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
            .Where(hit => !string.Equals(hit.TViolationKind, "Mutation", StringComparison.Ordinal))
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

        return TAuditTruthWalker.TAuditRun(sources)
            .Select(hit => hit with
            {
                TViolationPath = Path.GetRelativePath(repoRoot, hit.TViolationPath).Replace('\\', '/')
            })
            .ToList();
    }
}
