using System.Text.RegularExpressions;
using Xunit;

namespace Convention.Tests;

public sealed class TAuditBoundary
{
    private static readonly string[] TAuditBoundaryHidden =
    [
        @"^\s*#\s*if\b",
        @"\bdynamic\b",
        @"\bType\.GetType\s*\(",
        @"\bActivator\.",
        @"\.GetMethods?\s*\(",
        @"\.GetPropert(y|ies)\s*\(",
        @"\.GetFields?\s*\(",
        @"<x:Code\b",
        @"\bEnum\.(Try)?Parse\b",
        @"^\s*(global\s+)?using\s+\w+\s*=",
        @"^\s*extern\s+alias\b",
    ];

    private const string TAuditBoundaryReflection = @"\bSystem\.Reflection\b";

    [Fact]
    public void AuditBoundary_ShellSources_HideNothing()
    {
        List<string> hits = TAuditBoundaryScan(static _ => true, TAuditBoundaryHidden);

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITBOUNDARY",
            $"{hits.Count} shell line(s) hide code from the walkers:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditBoundary_PanelSources_ReflectNothing()
    {
        List<string> hits = TAuditBoundaryScan(static _ => true, [TAuditBoundaryReflection]);

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITBOUNDARY",
            $"{hits.Count} shell line(s) reflect:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditBoundary_Tracked_SkipNoSource()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new([], ["src/*", "tests/*"], [], [], [], []);
        List<string> hits = TAuditSource.TAuditFileRead(repoRoot, scope)
            .Select(path => Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
            .Where(path => path.Split('/').Any(segment =>
                               TAuditNameSetting.TAuditExcludedSegments.Contains(segment, StringComparer.Ordinal))
                           || TAuditNameSetting.TAuditExcludedSuffixes.Any(suffix =>
                               !suffix.Equals(".md", StringComparison.Ordinal)
                               && path.EndsWith(suffix, StringComparison.Ordinal))
                           || TAuditNameSetting.TAuditExcludedPrefixes.Any(prefix =>
                               Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal)))
            .Select(path => $"  {path}")
            .ToList();

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITBOUNDARY",
            $"{hits.Count} tracked file(s) sit where the audits never look:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditBoundary_LogicSources_HoldNoPanel()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new([], ["src/*.cs"], TAuditNameSetting.TAuditExcludedSegments, [], [], []);
        IReadOnlyList<string> shells = TAuditShellRead(repoRoot);
        List<string> hits = [];
        foreach (string path in TAuditSource.TAuditFileRead(repoRoot, scope))
        {
            if (shells.Any(shell => path.StartsWith(shell, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(path);
            for (int index = 0; index < lines.Length; index++)
            {
                if (Regex.IsMatch(lines[index], @"\b(class|struct|record)\s+PS?[A-Z]"))
                {
                    hits.Add($"  {Path.GetRelativePath(repoRoot, path).Replace('\\', '/')}:{index + 1}");
                }
            }
        }

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITBOUNDARY",
            $"{hits.Count} panel type(s) declared outside the shell:\n{string.Join('\n', hits)}"));
    }

    private static List<string> TAuditBoundaryScan(Func<string, bool> chosen, IReadOnlyList<string> forbidden)
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            [],
            [.. TAuditTruthSetting.TAuditShellInclude, .. TAuditStrictSetting.TAuditReachInclude],
            TAuditNameSetting.TAuditExcludedSegments,
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            []);
        IReadOnlyList<string> sources = TAuditSource.TAuditFileRead(repoRoot, scope);
        Assert.True(sources.Count > 0, TAuditConvention.TAuditReportFormat(
            "AUDITBOUNDARY", "No tracked shell file was enumerated; the audit would pass vacuously."));

        List<string> hits = [];
        foreach (string path in sources)
        {
            if (!chosen(Path.GetFileName(path)))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(path);
            for (int index = 0; index < lines.Length; index++)
            {
                foreach (string pattern in forbidden)
                {
                    if (Regex.IsMatch(lines[index], pattern))
                    {
                        string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                        hits.Add($"  {relative}:{index + 1} {pattern}");
                    }
                }
            }
        }

        return hits;
    }

    private static IReadOnlyList<string> TAuditShellRead(string repoRoot)
    {
        return TAuditSemantic.TAuditRootRead(TAuditTruthSetting.TAuditShellInclude)
            .Select(root => Path.Combine(repoRoot, root.Replace('/', Path.DirectorySeparatorChar))
                            + Path.DirectorySeparatorChar)
            .ToList();
    }
}
