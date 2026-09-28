using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditStrict
{
    private const string TAuditStrictAudit = "AUDITSTRICT";

    private static readonly Lazy<(IReadOnlyList<TViolation> TAuditHits, IReadOnlyList<string> TAuditVeneers,
        IReadOnlyList<string> TAuditSources)> TAuditStrictHits = new(TAuditStrictRead);

    private static readonly Lazy<string> TAuditStrictWritten = new(TAuditReportSave);

    private static readonly string[] TAuditStrictKinds =
        ["Storage", "Static", "Call", "Depth", "Reach", "Trigger", "Glyph", "Wiring", "Hook", "Shell", "Pack",
            "Scaffold", "Contract"];

    private static readonly Regex TAuditLiteralPattern = new(
        @"@?""(?:[^""\\]|\\.)*""|//.*$",
        RegexOptions.Compiled);

    private readonly ITestOutputHelper _tAuditOutput;

    public TAuditStrict(ITestOutputHelper output)
    {
        _tAuditOutput = output;
    }

    [Fact]
    public void AuditStrict_SurfaceFields_HoldNothing()
    {
        TAuditStrictCheck("Storage", "surface field(s), property(ies) or parameter(s) hold state");
    }

    [Fact]
    public void AuditStrict_DriverStatics_HoldNothing()
    {
        TAuditStrictCheck("Static", "driver static field(s) hold mutable state");
    }

    [Fact]
    public void AuditStrict_SurfaceMembers_CallOnly()
    {
        TAuditStrictCheck("Call", "surface line(s) do more than call a function");
    }

    [Fact]
    public void AuditStrict_SurfaceSources_NameOnlyDriver()
    {
        TAuditStrictCheck("Depth", "surface line(s) name a type from below the driver");
    }

    [Fact]
    public void AuditStrict_SurfaceMarkup_NameOnlyDriver()
    {
        TAuditStrictCheck("Reach", "markup line(s) name a type from below the driver");
    }

    [Fact]
    public void AuditStrict_SurfaceMarkup_BranchNever()
    {
        TAuditStrictCheck("Trigger", "markup line(s) branch or compute");
    }

    [Fact]
    public void AuditStrict_SurfaceNames_HoldNoGlyph()
    {
        TAuditStrictCheck("Glyph", "surface identifier(s) carry a non-ASCII glyph");
    }

    [Fact]
    public void AuditStrict_HostSources_WireOnly()
    {
        TAuditStrictCheck("Wiring", "host line(s) do more than construct and wire");
    }

    [Fact]
    public void AuditStrict_SurfaceMarkup_HookNever()
    {
        TAuditStrictCheck("Hook", "markup line(s) hook logic into markup");
    }

    [Fact]
    public void AuditStrict_SurfaceTypes_ShellOnly()
    {
        TAuditStrictCheck("Shell", "surface member(s) other than a constructor");
    }

    [Fact]
    public void AuditStrict_DriverSource_PackNever()
    {
        TAuditStrictCheck("Pack", "driver line(s) hold a pack URI or name the surface");
    }

    [Fact]
    public void AuditStrict_DriverTypes_ScaffoldNever()
    {
        TAuditStrictCheck("Scaffold", "driver type(s) derive from a scaffold type");
    }

    [Fact]
    public void AuditStrict_SurfaceMarkup_ContractHeld()
    {
        TAuditStrictCheck("Contract", "contract ID(s) have no x:Name or x:Key in the surface");
    }

    [Fact]
    public void AuditStrict_DriverSources_TouchNoDisk()
    {
        List<string> hits = TAuditSourceScan(
            TAuditStrictSetting.TAuditDeportmentInclude,
            TAuditStrictSetting.TAuditDiskPatterns,
            TAuditStrictSetting.TAuditDiskExempt);

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditStrictAudit,
            $"{hits.Count} driver line(s) reach the file system:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditStrict_SurfaceSources_HoldNoCatalog()
    {
        List<string> hits = TAuditSourceScan(
            TAuditStrictSetting.TAuditVeneerInclude,
            TAuditStrictSetting.TAuditCatalogPatterns,
            TAuditStrictSetting.TAuditCatalogExempt);

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditStrictAudit,
            $"{hits.Count} surface line(s) do file, JSON, regex, process or task work:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditStrict_Exempt_MatchesSource()
    {
        List<string> stale = TAuditExemptRead(
                TAuditStrictSetting.TAuditVeneerInclude,
                TAuditStrictSetting.TAuditCatalogPatterns,
                TAuditStrictSetting.TAuditCatalogExempt)
            .Concat(TAuditExemptRead(
                TAuditStrictSetting.TAuditDeportmentInclude,
                TAuditStrictSetting.TAuditDiskPatterns,
                TAuditStrictSetting.TAuditDiskExempt))
            .ToList();

        Assert.True(stale.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditStrictAudit,
            $"{stale.Count} exempt file(s) hold no line the exemption spares and must be removed:\n"
            + string.Join('\n', stale)));
    }

    [Fact]
    public void AuditStrict_Ledger_MatchesHits()
    {
        TAuditLedger.TAuditStaleCheck(
            TAuditStrictAudit,
            TAuditStrictSetting.TAuditLedgerFile,
            TAuditStrictKinds,
            TAuditStrictHits.Value.TAuditHits);
    }

    [Fact]
    public void AuditStrict_Sources_WalkEveryFile()
    {
        TAuditBinder.TAuditCoverCheck(TAuditStrictAudit, TAuditStrictHits.Value.TAuditSources);
    }

    private void TAuditStrictCheck(string kind, string summary)
    {
        string report = TAuditStrictWritten.Value;
        int count = TAuditLedger.TAuditLedgerCheck(
            TAuditStrictAudit,
            TAuditStrictSetting.TAuditLedgerFile,
            kind,
            TAuditStrictHits.Value.TAuditHits,
            TAuditStrictSetting.TAuditStrictEnforced,
            summary);
        _tAuditOutput.WriteLine($"{TAuditStrictAudit} {kind}: {count} {summary}. Report: {report}");
    }

    private static IEnumerable<string> TAuditExemptRead(
        IReadOnlyList<string> include, IReadOnlyList<string> forbidden, IReadOnlyList<string> exempt)
    {
        HashSet<string> used = TAuditSourceScan(include, forbidden, [])
            .Select(hit => Path.GetFileName(hit.Trim().Split(':')[0]))
            .ToHashSet(StringComparer.Ordinal);
        return exempt.Where(name => !used.Contains(name)).Select(name => $"  {name}");
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

    private static (IReadOnlyList<TViolation> TAuditHits, IReadOnlyList<string> TAuditVeneers,
        IReadOnlyList<string> TAuditSources) TAuditStrictRead()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        IReadOnlyList<string> sources = TAuditScopeRead(repoRoot, TAuditTruthSetting.TAuditShellInclude);
        IReadOnlyList<string> hosts = TAuditScopeRead(repoRoot, TAuditStrictSetting.TAuditHostInclude);
        IReadOnlyList<string> markups = TAuditScopeRead(repoRoot, TAuditStrictSetting.TAuditReachInclude);
        IReadOnlyList<string> drivers = TAuditScopeRead(repoRoot, TAuditStrictSetting.TAuditDeportmentInclude);
        Assert.True(sources.Count > 0 && hosts.Count > 0, TAuditConvention.TAuditReportFormat(
            TAuditStrictAudit,
            "No tracked shell or host file was enumerated; the audit would pass vacuously."));
        IReadOnlyList<TViolation> hits = TAuditStrictWalker.TAuditRun(sources, out List<string> veneers)
            .Concat(TAuditHostWalker.TAuditRun(hosts))
            .Concat(TAuditReachWalker.TAuditRun(markups))
            .Concat(TAuditContractWalker.TAuditRun(drivers, markups))
            .Select(hit => hit with
            {
                TViolationPath = Path.GetRelativePath(repoRoot, hit.TViolationPath).Replace('\\', '/')
            })
            .OrderBy(hit => hit.TViolationPath, StringComparer.Ordinal)
            .ThenBy(hit => hit.TViolationLine)
            .ToList();
        veneers.Sort(StringComparer.Ordinal);
        return (hits, veneers, [.. sources, .. hosts]);
    }

    private static IReadOnlyList<string> TAuditScopeRead(string repoRoot, IReadOnlyList<string> include)
    {
        return TAuditSource.TAuditFileRead(repoRoot, new TAuditScope(
            [],
            include,
            TAuditNameSetting.TAuditExcludedSegments,
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            []));
    }

    private static string TAuditReportSave()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        (IReadOnlyList<TViolation> hits, IReadOnlyList<string> veneers, _) = TAuditStrictHits.Value;
        string version = TAuditSource.TAuditVersionRead(repoRoot);
        string path = Path.Combine(repoRoot, string.Format(TAuditStrictSetting.TAuditStrictReport, version));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        StringBuilder text = new();
        text.AppendLine($"# Surface audit {version}");
        text.AppendLine();
        text.AppendLine($"- Generation: {TAuditConvention.TAuditGeneration}");
        text.AppendLine($"- Enforced: {TAuditStrictSetting.TAuditStrictEnforced}");
        text.AppendLine($"- Surface types: {veneers.Count}");
        foreach (string kind in TAuditStrictKinds)
        {
            int count = hits.Count(hit => string.Equals(hit.TViolationKind, kind, StringComparison.Ordinal));
            text.AppendLine($"- {kind}: {count}");
        }

        text.AppendLine();
        text.AppendLine("## Surface types by member");
        text.AppendLine();
        text.AppendLine("| Class | Storage | Call | Depth |");
        text.AppendLine("|---|---|---|---|");
        foreach (string veneer in veneers)
        {
            int storage = TAuditClassRead(hits, veneer, "Storage");
            int call = TAuditClassRead(hits, veneer, "Call");
            int depth = TAuditClassRead(hits, veneer, "Depth");
            text.AppendLine($"| {veneer} | {storage} | {call} | {depth} |");
        }

        foreach (string kind in TAuditStrictKinds)
        {
            text.AppendLine();
            text.AppendLine($"## {kind}");
            text.AppendLine();
            foreach (TViolation hit in hits.Where(hit =>
                         string.Equals(hit.TViolationKind, kind, StringComparison.Ordinal)))
            {
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
