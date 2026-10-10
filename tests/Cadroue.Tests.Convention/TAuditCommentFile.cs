using Xunit;

namespace Convention.Tests;

internal static class TAuditCommentFile
{
    public static IEnumerable<string> TAuditOwnerRead(string repoRoot)
    {
        TAuditScope scope = new(
            TAuditCommentSetting.TAuditCommentRoots,
            TAuditCommentSetting.TAuditCommentSources,
            TAuditCommentSetting.TAuditCommentSegments,
            TAuditCommentSetting.TAuditCommentSuffixes,
            [],
            []);
        return TAuditScanRead(repoRoot, scope)
            .Concat(TAuditCommentSetting.TAuditCommentFiles.Select(file => Path.Combine(repoRoot, file)));
    }

    public static IReadOnlyList<string> TAuditScanRead(string repoRoot, TAuditScope scope)
    {
        List<string> missing = TAuditCommentSetting.TAuditCommentRoots
            .Where(root => !Directory.Exists(Path.Combine(repoRoot, root)))
            .Concat(TAuditCommentSetting.TAuditCommentFiles
                .Where(file => !File.Exists(Path.Combine(repoRoot, file))))
            .Concat(TAuditCommentSetting.TAuditCommentReserved
                .Where(file => !File.Exists(Path.Combine(repoRoot, file))))
            .ToList();
        Assert.True(missing.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"{missing.Count} configured root(s) or file(s) do not exist: {string.Join(", ", missing)}."));

        IReadOnlyList<string> paths = TAuditSource.TAuditFileRead(repoRoot, scope);
        Assert.True(paths.Count > 0, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS", "No tracked file lies in the comment scope, so the audit cannot judge."));
        return paths;
    }

    public static bool TAuditReservedCheck(string repoRoot, string path)
    {
        return TAuditCommentSetting.TAuditCommentReserved.Any(file => string.Equals(
            Path.GetFullPath(Path.Combine(repoRoot, file)),
            Path.GetFullPath(path),
            StringComparison.OrdinalIgnoreCase));
    }

    public static string TAuditCommentRead(string path)
    {
        string trimmed = path.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase)
            ? path[..^3]
            : Path.ChangeExtension(path, null);
        return trimmed + ".comment.md";
    }
}
