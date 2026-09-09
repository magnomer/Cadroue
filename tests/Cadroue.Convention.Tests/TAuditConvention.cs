using Xunit;

namespace Convention.Tests;

public sealed class TAuditConvention
{
    // The sidecar is generated and committed, so a checkout can carry one written by an older
    // generation of the tooling. The two numbers are compared rather than assumed equal.
    [Fact]
    public void AuditSetting_SidecarGeneration_MatchesTheTooling()
    {
        Assert.True(
            TAuditSetting.TAuditGeneration == TAuditName.TAuditGeneration,
            $"The settings sidecar is generation {TAuditSetting.TAuditGeneration} but this tooling " +
            $"is generation {TAuditName.TAuditGeneration}. Regenerate the registry.");
    }

    [Fact]
    public void AuditRun_AllSourceNames_ReportsNoViolation()
    {
        TAuditRegistry registry = TAuditRegistry.TAuditLoad();
        string repoRoot = TAuditSource.TAuditRootRead();
        IReadOnlyList<string> sources = TAuditSource.TAuditFileRead(repoRoot);

        Assert.True(sources.Count > 0, "No source files were enumerated; the audit would pass vacuously.");

        IReadOnlyList<TViolation> violations = TAuditName.TAuditRun(sources, registry);

        Assert.True(violations.Count == 0, TAuditReportFormat(repoRoot, violations));
    }

    private static string TAuditReportFormat(string repoRoot, IReadOnlyList<TViolation> violations)
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
