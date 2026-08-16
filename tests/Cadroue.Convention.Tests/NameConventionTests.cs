using Xunit;

namespace Cadroue.Convention.Tests;

public sealed class NameConventionTests
{
    [Fact]
    public void SourceNames_ConformToTheNamingRegistry()
    {
        AuditRegistry registry = AuditRegistry.Load();
        string repoRoot = AuditSource.RepoRoot();
        IReadOnlyList<string> sources = AuditSource.Files(repoRoot);

        Assert.True(sources.Count > 0, "No source files were enumerated; the audit would pass vacuously.");

        IReadOnlyList<NameViolation> violations = AuditName.Audit(sources, registry);

        Assert.True(violations.Count == 0, Report(repoRoot, violations));
    }

    private static string Report(string repoRoot, IReadOnlyList<NameViolation> violations)
    {
        IEnumerable<string> lines = violations
            .OrderBy(violation => violation.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(violation => violation.Line)
            .Select(violation =>
            {
                string relative = Path.GetRelativePath(repoRoot, violation.Path).Replace('\\', '/');
                return $"  {relative}:{violation.Line} [{violation.Kind}] {violation.Name} — {violation.Reason}";
            });

        return $"{violations.Count} non-conforming name(s):\n{string.Join('\n', lines)}";
    }
}
