using System.Text.RegularExpressions;
using Xunit;

namespace Convention.Tests;

internal static class TAuditCommentFile
{
    private static readonly Regex TAuditStampPattern = new(@"^##\s+Stamp\s*$", RegexOptions.Compiled);

    public static IEnumerable<string> TAuditOwnerRead(string repoRoot)
    {
        TAuditScope scope = new(
            TAuditCommentSetting.TAuditCommentRoots,
            TAuditCommentSetting.TAuditCommentSources,
            TAuditCommentSetting.TAuditCommentSegments,
            TAuditCommentSetting.TAuditCommentSuffixes,
            [],
            []);
        return TAuditScanRead(repoRoot, scope);
    }

    public static IReadOnlyList<string> TAuditScanRead(string repoRoot, TAuditScope scope)
    {
        List<string> missing = TAuditCommentSetting.TAuditCommentRoots
            .Where(root => !Directory.Exists(Path.Combine(repoRoot, root)))
            .Concat(TAuditCommentSetting.TAuditCommentReserved
                .Where(file => !File.Exists(Path.Combine(repoRoot, file))))
            .ToList();
        Assert.True(missing.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"{missing.Count} configured root(s) or file(s) do not exist: {string.Join(", ", missing)}."));

        string rootFull = Path.GetFullPath(repoRoot).TrimEnd('\\', '/');
        IReadOnlyList<string> paths = [.. TAuditSource.TAuditFileRead(repoRoot, scope)
            .Where(path => !string.Equals(
                Path.GetDirectoryName(Path.GetFullPath(path))!.TrimEnd('\\', '/'),
                rootFull,
                StringComparison.OrdinalIgnoreCase))];
        Assert.True(paths.Count > 0, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS", "No tracked file lies in the comment scope, so the audit cannot judge."));
        return paths;
    }

    public static IEnumerable<string> TAuditSidecarScan(string repoRoot)
    {
        TAuditScope scope = new(
            TAuditCommentSetting.TAuditCommentRoots,
            [TAuditCommentSetting.TAuditCommentPattern],
            TAuditCommentSetting.TAuditCommentSegments,
            [],
            [],
            []);
        return TAuditScanRead(repoRoot, scope)
            .Where(path => !TAuditReservedCheck(repoRoot, path));
    }

    public static bool TAuditReservedCheck(string repoRoot, string path)
    {
        return TAuditCommentSetting.TAuditCommentReserved.Any(file => string.Equals(
            Path.GetFullPath(Path.Combine(repoRoot, file)),
            Path.GetFullPath(path),
            StringComparison.OrdinalIgnoreCase));
    }

    public static string TAuditCommentRead(string repoRoot, string path)
    {
        string directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        string name = Path.GetFileName(directory) + TAuditCommentSetting.TAuditCommentPattern.TrimStart('*');
        return Path.Combine(directory, name);
    }

    public static string[] TAuditProseRead(string path)
    {
        string[] lines = File.ReadAllLines(path);
        int stamp = Array.FindIndex(lines, line => TAuditStampPattern.IsMatch(line));
        return stamp < 0 ? lines : lines[..stamp];
    }

    public static int TAuditStampFind(string[] lines)
    {
        return Array.FindIndex(lines, line => TAuditStampPattern.IsMatch(line));
    }
}
