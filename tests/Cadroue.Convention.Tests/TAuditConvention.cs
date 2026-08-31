using Xunit;

namespace Cadroue.Convention.Tests;

public sealed class TAuditConvention
{
    [Fact]
    public void SourceNames_ConformToTheNamingRegistry()
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
