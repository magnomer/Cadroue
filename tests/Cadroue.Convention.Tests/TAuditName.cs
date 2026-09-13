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

    private static string TViolationFormat(string repoRoot, IReadOnlyList<TViolation> violations)
    {
        IEnumerable<string> lines = violations
            .OrderBy(violation => violation.TViolationPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(violation => violation.TViolationLine)
            .Select(violation =>
            {
                string relative = Path.GetRelativePath(repoRoot, violation.TViolationPath).Replace('\\', '/');
                return $"  {relative}:{violation.TViolationLine} [{violation.TViolationKind}] {violation.TViolationName} — {violation.TViolationReason}";
            });

        return $"{violations.Count} non-conforming name(s):\n{string.Join('\n', lines)}";
    }
}
