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
    public void AuditName_Types_PrefixMatchesTurf()
    {
        List<string> hits = TAuditTurfRead()
            .Where(hit => !hit.TAuditSealed)
            .Select(hit => hit.TAuditText)
            .ToList();
        string listed = string.Join('\n', hits.Select(hit => $"  {hit}"));

        Assert.True(hits.Count <= TAuditNameSetting.TAuditPrefixCeiling, TAuditConvention.TAuditReportFormat(
            "AUDITNAMES",
            $"{hits.Count} type(s) carry a prefix outside their turf, above the ceiling "
            + $"{TAuditNameSetting.TAuditPrefixCeiling}:\n{listed}"));
        Assert.True(hits.Count >= TAuditNameSetting.TAuditPrefixCeiling, TAuditConvention.TAuditReportFormat(
            "AUDITNAMES",
            $"{hits.Count} type(s) carry a prefix outside their turf, so the ceiling "
            + $"{TAuditNameSetting.TAuditPrefixCeiling} is stale and must be lowered."));
    }

    [Fact]
    public void AuditName_SealedTurfs_HoldNoPublic()
    {
        List<string> hits = TAuditTurfRead()
            .Where(hit => hit.TAuditSealed)
            .Select(hit => $"  {hit.TAuditText}")
            .ToList();

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITNAMES",
            $"{hits.Count} public type(s) carry a prefix outside the turf of a sealed folder:\n"
            + string.Join('\n', hits)));
    }

    [Fact]
    public void AuditName_PublicSealedTurf_ReportsOneHit()
    {
        SyntaxNode root = CSharpSyntaxTree
            .ParseText("namespace Cadroue.Conduct;\npublic sealed class LDisplay { }")
            .GetRoot();
        List<(int TAuditLine, string TAuditType, string TAuditText, bool TAuditSealed)> hits =
            TAuditTurfScan("src/Cadroue.Conduct/Display/LDisplay.cs", root).ToList();

        Assert.True(Assert.Single(hits).TAuditSealed);
    }

    [Fact]
    public void AuditName_InternalSealedTurf_AllowsTheType()
    {
        SyntaxNode root = CSharpSyntaxTree
            .ParseText("namespace Cadroue.Conduct;\ninternal sealed class LDisplay { }")
            .GetRoot();
        List<(int TAuditLine, string TAuditType, string TAuditText, bool TAuditSealed)> hits =
            TAuditTurfScan("src/Cadroue.Conduct/Display/LDisplay.cs", root).ToList();

        Assert.DoesNotContain(hits, hit => hit.TAuditSealed);
    }

    internal static IEnumerable<(int TAuditLine, string TAuditType, string TAuditText, bool TAuditSealed)>
        TAuditTurfScan(string relative, SyntaxNode root)
    {
        string? turf = TAuditNameSetting.TAuditPrefixTurfs.Keys
            .FirstOrDefault(key => relative.StartsWith(key + "/", StringComparison.OrdinalIgnoreCase));
        if (turf is null)
        {
            yield break;
        }

        string[] allowed = TAuditNameSetting.TAuditPrefixTurfs[turf];
        bool sealedTurf = TAuditNameSetting.TAuditTurfSealed.Contains(turf, StringComparer.Ordinal);
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
            bool exposed = sealedTurf && TAuditPublicCheck(node);
            yield return (line, name,
                $"{relative}:{line} [{kind}] {name} - prefix `{prefix}` is outside the turf of {turf} "
                + $"({string.Join(", ", allowed.Select(item => $"`{item}`"))})"
                + (exposed ? " and public in a sealed turf" : ""),
                exposed);
        }
    }

    private static bool TAuditPublicCheck(SyntaxNode node)
    {
        return node.AncestorsAndSelf()
            .OfType<MemberDeclarationSyntax>()
            .Where(member => member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
            .All(member => member.Modifiers.Any(SyntaxKind.PublicKeyword));
    }

    private static List<(string TAuditText, bool TAuditSealed)> TAuditTurfRead()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            [],
            TAuditNameSetting.TAuditSourceInclude,
            TAuditNameSetting.TAuditExcludedSegments,
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            TAuditNameSetting.TAuditSelfExcluded);
        CSharpParseOptions options = new(LanguageVersion.Preview, DocumentationMode.None, SourceCodeKind.Regular);
        List<(string TAuditPath, int TAuditLine, string TAuditType, string TAuditText, bool TAuditSealed)> hits = [];
        foreach (string path in TAuditSource.TAuditFileRead(repoRoot, scope)
                     .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
        {
            string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
            if (!TAuditNameSetting.TAuditPrefixTurfs.Keys
                    .Any(key => relative.StartsWith(key + "/", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            SyntaxNode root = CSharpSyntaxTree.ParseText(File.ReadAllText(path), options, path).GetRoot();
            hits.AddRange(TAuditTurfScan(relative, root)
                .Select(hit => (relative, hit.TAuditLine, hit.TAuditType, hit.TAuditText, hit.TAuditSealed)));
        }

        return hits
            .OrderBy(hit => hit.TAuditPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(hit => hit.TAuditLine)
            .ThenBy(hit => hit.TAuditType, StringComparer.Ordinal)
            .Select(hit => (hit.TAuditText, hit.TAuditSealed))
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
