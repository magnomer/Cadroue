using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Convention.Tests;

public sealed class TAuditName
{
    [Fact]
    public void AuditName_AllSourceNames_ReportsNoViolation()
    {
        TAuditRegistry registry = TAuditRegistry.TAuditLoad();
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            [],
            TAuditNameSetting.TAuditSourceInclude,
            TAuditNameSetting.TAuditExcludedSegments,
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            TAuditNameSetting.TAuditSelfExcluded);
        IReadOnlyList<string> sources = TAuditSource.TAuditFileRead(repoRoot, scope);

        Assert.True(sources.Count > 0, TAuditConvention.TAuditReportFormat(
            "AUDITNAMES", "No source files were enumerated; the audit would pass vacuously."));

        IReadOnlyList<TViolation> violations = TAuditNameWalker.TAuditRun(sources, registry);

        Assert.True(violations.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITNAMES", TViolationFormat(repoRoot, violations)));
    }

    [Fact]
    public void AuditName_Exempt_MatchesSource()
    {
        TAuditRegistry registry = TAuditRegistry.TAuditLoad();
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            [],
            TAuditNameSetting.TAuditSourceInclude,
            TAuditNameSetting.TAuditExcludedSegments,
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            TAuditNameSetting.TAuditSelfExcluded);
        List<TSpecimen> specimens = TAuditNameWalker.TAuditSpecimenRead(TAuditSource.TAuditFileRead(repoRoot, scope));
        List<string> stale = registry.TAuditExempt.Keys
            .Where(name => !specimens.Any(specimen =>
                string.Equals(specimen.TSpecimenName, name, StringComparison.Ordinal)
                && registry.TAuditExemptValidate(name, specimen.TSpecimenPath)))
            .Order(StringComparer.Ordinal)
            .Select(name => $"  {name}")
            .ToList();

        Assert.True(stale.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITNAMES",
            $"{stale.Count} registry exemption(s) spare no name in the sources and must be removed:\n"
            + string.Join('\n', stale)));
    }

    [Fact]
    public void AuditName_Types_PrefixMatchesRing()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            [],
            TAuditNameSetting.TAuditSourceInclude,
            TAuditNameSetting.TAuditExcludedSegments,
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            TAuditNameSetting.TAuditSelfExcluded);
        List<string> hits = TAuditRingRead(repoRoot, TAuditSource.TAuditFileRead(repoRoot, scope));
        string listed = string.Join('\n', hits.Select(hit => $"  {hit}"));

        Assert.True(hits.Count <= TAuditNameSetting.TAuditPrefixCeiling, TAuditConvention.TAuditReportFormat(
            "AUDITNAMES",
            $"{hits.Count} type(s) carry a prefix outside their ring, above the ceiling "
            + $"{TAuditNameSetting.TAuditPrefixCeiling}:\n{listed}"));
        Assert.True(hits.Count >= TAuditNameSetting.TAuditPrefixCeiling, TAuditConvention.TAuditReportFormat(
            "AUDITNAMES",
            $"{hits.Count} type(s) carry a prefix outside their ring, so the ceiling "
            + $"{TAuditNameSetting.TAuditPrefixCeiling} is stale and must be lowered."));
    }

    private static List<string> TAuditRingRead(string repoRoot, IReadOnlyList<string> sources)
    {
        CSharpParseOptions options = new(LanguageVersion.Preview, DocumentationMode.None, SourceCodeKind.Regular);
        List<(string TAuditPath, int TAuditLine, string TAuditType, string TAuditText)> hits = [];
        foreach (string path in sources.Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
        {
            string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
            string? ring = TAuditNameSetting.TAuditPrefixRings.Keys
                .FirstOrDefault(key => relative.StartsWith(key + "/", StringComparison.OrdinalIgnoreCase));
            if (ring is null)
            {
                continue;
            }

            string[] allowed = TAuditNameSetting.TAuditPrefixRings[ring];
            SyntaxNode root = CSharpSyntaxTree.ParseText(File.ReadAllText(path), options, path).GetRoot();
            foreach (SyntaxNode node in root.DescendantNodes())
            {
                (SyntaxToken identifier, string kind) = node switch
                {
                    BaseTypeDeclarationSyntax type => (type.Identifier, type.Kind().ToString()),
                    DelegateDeclarationSyntax shape => (shape.Identifier, "Delegate"),
                    _ => (default, string.Empty),
                };
                string name = identifier.ValueText;
                string? prefix = kind.Length == 0 ? null : TAuditNameFilter.TAuditPrefixRead(name);
                if (prefix is null || allowed.Contains(prefix, StringComparer.Ordinal))
                {
                    continue;
                }

                int line = identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                hits.Add((relative, line, name,
                    $"{relative}:{line} [{kind}] {name} - prefix `{prefix}` is outside the ring of {ring} "
                    + $"({string.Join(", ", allowed.Select(item => $"`{item}`"))})"));
            }
        }

        return hits
            .OrderBy(hit => hit.TAuditPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(hit => hit.TAuditLine)
            .ThenBy(hit => hit.TAuditType, StringComparer.Ordinal)
            .Select(hit => hit.TAuditText)
            .ToList();
    }

    private static string TViolationFormat(string repoRoot, IReadOnlyList<TViolation> violations)
    {
        IEnumerable<string> lines = violations
            .Select(violation => (TViolationRelative:
                Path.GetRelativePath(repoRoot, violation.TViolationPath).Replace('\\', '/'), TViolationItem: violation))
            .OrderBy(entry => entry.TViolationRelative, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.TViolationItem.TViolationLine)
            .ThenBy(entry => entry.TViolationItem.TViolationName, StringComparer.Ordinal)
            .Select(entry =>
                $"  {entry.TViolationRelative}:{entry.TViolationItem.TViolationLine} " +
                $"[{entry.TViolationItem.TViolationKind}] {entry.TViolationItem.TViolationName} - " +
                entry.TViolationItem.TViolationReason);

        return $"{violations.Count} non-conforming name(s):\n{string.Join('\n', lines)}";
    }
}
