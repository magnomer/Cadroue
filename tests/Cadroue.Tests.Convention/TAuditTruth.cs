using System.Text;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditTruth
{
    private const string TAuditTruthAudit = "AUDITTRUTH";

    private static readonly Lazy<(IReadOnlyList<TViolation> TAuditHits, IReadOnlyList<string> TAuditSources)>
        TAuditTruthHits = new(TAuditTruthRead);

    private static readonly Lazy<string> TAuditTruthWritten = new(TAuditReportSave);

    private static readonly string[] TAuditTruthKinds =
        [
            "Replaying", "Gatekeeping", "Contesting", "Duplicating", "Tampering", "Misfiring", "Moonlighting",
            "Homoglyph", "Laundering", "Spoonfeeding", "Mismatching", "Puppeteering", "Zeroing",
        ];

    private static readonly string[] TAuditLineKinds =
        ["Tampering", "Moonlighting", "Homoglyph", "Laundering", "Spoonfeeding", "Mismatching"];

    private readonly ITestOutputHelper _tAuditOutput;

    public TAuditTruth(ITestOutputHelper output)
    {
        _tAuditOutput = output;
    }

    [Fact]
    public void AuditTruth_DriverFields_ReachNoRequest()
    {
        TAuditTruthCheck("Replaying", "driver value(s) carried into a request");
        TAuditTruthCheck("Gatekeeping", "driver value(s) or answer(s) decide a request");
        TAuditTruthCheck("Zeroing", "driver literal zero(s) at a gate-bound position");
    }

    [Fact]
    public void AuditTruth_DriverFields_KeepOneWriter()
    {
        TAuditTruthCheck("Contesting", "driver field(s) written by the engine and by the driver");
    }

    [Fact]
    public void AuditTruth_DriverFields_HoldNoEngine()
    {
        TAuditTruthCheck("Duplicating", "driver field(s) hold a value from below Conduct");
    }

    [Fact]
    public void AuditTruth_DriverSources_MutateNoLogic()
    {
        TAuditTruthCheck("Tampering", "driver line(s) assign a logic member");
    }

    [Fact]
    public void AuditTruth_DriverRequests_HoldNoMisfiring()
    {
        TAuditTruthCheck("Misfiring", "driver control(s), clock(s), handler(s) or second request(s)");
    }

    [Fact]
    public void AuditTruth_DriverSources_HoldNoMoonlighting()
    {
        TAuditTruthCheck("Moonlighting", "driver line(s) compute over a value from below Conduct");
    }

    [Fact]
    public void AuditTruth_DriverNames_HoldNoHomoglyph()
    {
        TAuditTruthCheck("Homoglyph", "driver identifier(s) carry a non-ASCII glyph");
    }

    [Fact]
    public void AuditTruth_DriverLocals_CarryNoData()
    {
        TAuditTruthCheck("Laundering", "driver line(s) compute over a carried value or medium input");
    }

    [Fact]
    public void AuditTruth_DriverSources_HoldNoSpoonfeeding()
    {
        TAuditTruthCheck("Spoonfeeding", "driver line(s) hand the surface a type from below the cut");
    }

    [Fact]
    public void AuditTruth_DriverGates_HoldNoMismatching()
    {
        TAuditTruthCheck("Mismatching", "driver line(s) reach a Conduct member or port the other driver lacks");
    }

    [Fact]
    public void AuditTruth_ControlTypes_HoldNoPuppeteering()
    {
        TAuditTruthCheck("Puppeteering", "control member(s) request logic");
    }

    [Fact]
    public void AuditTruth_Ledger_MatchesHits()
    {
        TAuditLedger.TAuditStaleCheck(
            TAuditTruthAudit, TAuditTruthSetting.TAuditLedgerFile, TAuditTruthKinds, TAuditTruthHits.Value.TAuditHits);
    }

    [Fact]
    public void AuditTruth_Sources_WalkEveryFile()
    {
        TAuditBinder.TAuditCoverCheck(TAuditTruthAudit, TAuditTruthHits.Value.TAuditSources);
    }

    private void TAuditTruthCheck(string kind, string summary)
    {
        string report = TAuditTruthWritten.Value;
        int count = TAuditLedger.TAuditLedgerCheck(
            TAuditTruthAudit,
            TAuditTruthSetting.TAuditLedgerFile,
            kind,
            TAuditTruthHits.Value.TAuditHits,
            TAuditTruthSetting.TAuditTruthEnforced && TAuditEnforceCheck(kind),
            summary);
        _tAuditOutput.WriteLine($"{TAuditTruthAudit} {kind}: {count} {summary}. Report: {report}");
    }

    private static bool TAuditEnforceCheck(string kind)
    {
        return !TAuditTruthSetting.TAuditTruthInformative.TryGetValue(kind, out string[]? folders)
            || folders.All(folder => TAuditTruthHits.Value.TAuditSources.Any(source =>
                TAuditBinder.TAuditRelativeRead(source).StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase)));
    }

    private static int TAuditKindRead(string kind)
    {
        return TAuditTruthHits.Value.TAuditHits.Count(hit =>
            string.Equals(hit.TViolationKind, kind, StringComparison.Ordinal));
    }

    private static string TAuditReportSave()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        string version = TAuditSource.TAuditVersionRead(repoRoot);
        string path = Path.Combine(repoRoot, string.Format(TAuditTruthSetting.TAuditTruthReport, version));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        IReadOnlyList<TViolation> hits = TAuditTruthHits.Value.TAuditHits;

        StringBuilder text = new();
        text.AppendLine($"# Driver audit {version}");
        text.AppendLine();
        text.AppendLine($"- Generation: {TAuditConvention.TAuditGeneration}");
        text.AppendLine($"- Enforced: {TAuditTruthSetting.TAuditTruthEnforced}");
        foreach (string kind in TAuditTruthSetting.TAuditTruthInformative.Keys.Where(kind => !TAuditEnforceCheck(kind)))
        {
            text.AppendLine($"- Informative: {kind}");
        }

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
                         .Where(hit => string.Equals(hit.TViolationKind, kind, StringComparison.Ordinal))
                         .OrderBy(hit => hit.TViolationPath, StringComparer.Ordinal)
                         .ThenBy(hit => hit.TViolationLine))
            {
                text.AppendLine(
                    $"- `{hit.TViolationPath}:{hit.TViolationLine}` `{hit.TViolationName}` {hit.TViolationReason}");
            }
        }

        File.WriteAllText(path, text.ToString());
        return Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
    }

    private static (IReadOnlyList<TViolation> TAuditHits, IReadOnlyList<string> TAuditSources) TAuditTruthRead()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            [],
            TAuditTruthSetting.TAuditTruthInclude,
            TAuditNameSetting.TAuditExcludedSegments,
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            []);
        IReadOnlyList<string> sources = TAuditSource.TAuditFileRead(repoRoot, scope);
        Assert.True(sources.Count > 0, TAuditConvention.TAuditReportFormat(
            TAuditTruthAudit, $"No tracked file matches {string.Join(' ', TAuditTruthSetting.TAuditTruthInclude)}."));

        IReadOnlySet<ISymbol> readers = TAuditTruthWalker.TAuditReaderRead(sources);
        List<TViolation> hits = TAuditTruthWalker.TAuditRun(sources)
            .Concat(TAuditMoonlightingWalker.TAuditRun(sources))
            .Concat(TAuditLaunderingWalker.TAuditRun(sources, readers))
            .Select(hit => hit with
            {
                TViolationPath = Path.GetRelativePath(repoRoot, hit.TViolationPath).Replace('\\', '/')
            })
            .ToList();
        return (hits, sources);
    }
}
