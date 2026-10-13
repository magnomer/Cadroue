using System.Text.RegularExpressions;
using Xunit;

namespace Convention.Tests;

public sealed class TAuditComment
{
    private static readonly Regex TAuditLiteralPattern = new(
        """"(?:"{3,})[\s\S]*?"{3,}|"""" +
        """"(?:@\$?|\$@)"(?:[^"]|"")*"|"(?:\\.|[^"\\\r\n])*"|'(?:\\.|[^'\\\r\n])'"""",
        RegexOptions.Compiled);


    private static readonly Regex TAuditHeadingPattern = new(@"^##\s+`(?<span>[^`]+)`\s*$", RegexOptions.Compiled);

    private static readonly Regex TAuditFilePattern = new(
        @"^[\w.]+\.(cs|xaml|json|csproj|props|slnx|md)$", RegexOptions.Compiled);

    private static readonly Regex TAuditGenericPattern = new(@"<[^<>]*>", RegexOptions.Compiled);

    private static readonly Regex TAuditIdentifierPattern = new(@"@?[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);

    [Fact]
    public void AuditComment_CommentLines_KeepLineRules()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        List<string> hits = [];
        foreach (string path in TAuditCommentFile.TAuditSidecarScan(repoRoot))
        {
            string[] lines = TAuditCommentFile.TAuditProseRead(path);
            for (int index = 0; index < lines.Length; index++)
            {
                string problem = TAuditCommentLine.TAuditLineCheck(lines[index]);
                if (problem.Length > 0)
                {
                    string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                    hits.Add($"  {relative}:{index + 1} {problem}");
                }
            }
        }

        Assert.True(!TAuditCommentSetting.TAuditCommentEnforced || hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"{hits.Count} comment line(s) break the line rules: one sentence, "
            + $"at most {TAuditCommentSetting.TAuditCommentWords} words, "
            + $"none of {string.Join(' ', TAuditCommentSetting.TAuditCommentForbidden)}.\n"
            + string.Join('\n', hits)));
    }

    [Fact]
    public void AuditComment_Sources_CarryNoRemark()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            TAuditCommentSetting.TAuditCommentRoots,
            TAuditCommentSetting.TAuditCommentMarkers.Keys.Select(extension => "*" + extension).ToArray(),
            TAuditCommentSetting.TAuditCommentSegments,
            TAuditCommentSetting.TAuditCommentSuffixes,
            [],
            TAuditCommentSetting.TAuditCommentExempt);
        IEnumerable<string> sources = TAuditCommentFile.TAuditScanRead(repoRoot, scope).Concat(
            TAuditCommentSetting.TAuditCommentFiles
                .Where(file => !TAuditCommentSetting.TAuditCommentExempt.Contains(
                    Path.GetFileName(file), StringComparer.OrdinalIgnoreCase))
                .Select(file => Path.Combine(repoRoot, file)));

        List<string> hits = [];
        foreach (string path in sources)
        {
            string extension = Path.GetExtension(path);
            if (!TAuditCommentSetting.TAuditCommentMarkers.TryGetValue(extension, out string[]? markers))
            {
                continue;
            }

            bool code = extension.Equals(".cs", StringComparison.OrdinalIgnoreCase);
            string[] lines = TAuditSourceRead(path, code);
            string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
            string? open = null;
            for (int index = 0; index < lines.Length; index++)
            {
                string text = lines[index];
                if (open is not null)
                {
                    hits.Add($"  {relative}:{index + 1} {open}");
                    string end = TAuditCommentSetting.TAuditCommentClosers[open];
                    open = text.Contains(end, StringComparison.Ordinal) ? null : open;
                    continue;
                }

                string? marker = markers.FirstOrDefault(entry => text.Contains(entry, StringComparison.Ordinal));
                if (marker is null)
                {
                    continue;
                }

                hits.Add($"  {relative}:{index + 1} {marker}");
                int after = text.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
                if (TAuditCommentSetting.TAuditCommentClosers.TryGetValue(marker, out string? closer)
                    && text.IndexOf(closer, after, StringComparison.Ordinal) < 0)
                {
                    open = marker;
                }
            }
        }

        Assert.True(!TAuditCommentSetting.TAuditCommentEnforced || hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"{hits.Count} in-code comment(s) found. "
            + $"Prose belongs in the folder's {TAuditCommentSetting.TAuditCommentPattern} sidecar.\n"
            + string.Join('\n', hits)));
    }

    [Fact]
    public void AuditComment_Folders_CarrySidecar()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        List<string> hits = TAuditCommentFile.TAuditOwnerRead(repoRoot)
            .GroupBy(path => TAuditCommentFile.TAuditCommentRead(repoRoot, path), StringComparer.OrdinalIgnoreCase)
            .Where(group => !File.Exists(group.Key))
            .Select(group => "  " + Path.GetRelativePath(repoRoot, group.First()).Replace('\\', '/'))
            .ToList();

        Assert.True(!TAuditCommentSetting.TAuditCommentEnforced || hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"{hits.Count} folder(s) hold a source but no sidecar.\n" + string.Join('\n', hits)));
    }

    [Fact]
    public void AuditComment_CommentFiles_SideFolder()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        IEnumerable<string> owners = TAuditCommentFile.TAuditOwnerRead(repoRoot);
        HashSet<string> expected = new(
            owners.Select(path => TAuditCommentFile.TAuditCommentRead(repoRoot, path)),
            StringComparer.OrdinalIgnoreCase);
        List<string> hits = TAuditCommentFile.TAuditSidecarScan(repoRoot)
            .Where(path => !expected.Contains(Path.GetFullPath(path)))
            .Select(path => "  " + Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
            .ToList();

        Assert.True(!TAuditCommentSetting.TAuditCommentEnforced || hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"{hits.Count} comment file(s) are no folder's sidecar.\n" + string.Join('\n', hits)));
    }

    [Fact]
    public void AuditComment_Headings_NameMembers()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        List<string> hits = [];
        IEnumerable<IGrouping<string, string>> folders = TAuditCommentFile.TAuditOwnerRead(repoRoot)
            .GroupBy(path => TAuditCommentFile.TAuditCommentRead(repoRoot, path), StringComparer.OrdinalIgnoreCase)
            .Where(group => File.Exists(group.Key) && !TAuditCommentFile.TAuditReservedCheck(repoRoot, group.Key));
        foreach (IGrouping<string, string> folder in folders)
        {
            string comment = folder.Key;
            string[] sources = [.. folder.Where(path => !TAuditCommentSetting.TAuditCommentExempt.Contains(
                Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))];
            if (sources.Length == 0)
            {
                continue;
            }

            HashSet<string> names = new(folder.Select(Path.GetFileName)!, StringComparer.OrdinalIgnoreCase);
            string text = string.Concat(sources.Select(File.ReadAllText));

            string[] lines = TAuditCommentFile.TAuditProseRead(comment);
            for (int index = 0; index < lines.Length; index++)
            {
                Match heading = TAuditHeadingPattern.Match(lines[index]);
                if (!heading.Success)
                {
                    continue;
                }

                string span = heading.Groups["span"].Value;
                bool file = TAuditFilePattern.IsMatch(span);
                string? name = file ? span : TAuditMemberRead(span);
                bool named = file
                    ? names.Contains(span)
                    : name is null || Regex.IsMatch(text, $@"\b{Regex.Escape(name)}\b");
                if (!named)
                {
                    string relative = Path.GetRelativePath(repoRoot, comment).Replace('\\', '/');
                    hits.Add($"  {relative}:{index + 1} {name}");
                }
            }
        }

        Assert.True(!TAuditCommentSetting.TAuditCommentEnforced || hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"{hits.Count} comment heading(s) name nothing in their folder.\n" + string.Join('\n', hits)));
    }

    private static string? TAuditMemberRead(string span)
    {
        if (span.StartsWith('<') || TAuditFilePattern.IsMatch(span))
        {
            return null;
        }

        int stop = span.IndexOfAny(['(', '=', ';', '{', ':']);
        string head = stop < 0 ? span : span[..stop];
        while (TAuditGenericPattern.IsMatch(head))
        {
            head = TAuditGenericPattern.Replace(head, string.Empty);
        }

        Match last = TAuditIdentifierPattern.Matches(head).LastOrDefault() ?? Match.Empty;
        return last.Success ? last.Value : null;
    }

    private static string[] TAuditSourceRead(string path, bool code)
    {
        string text = File.ReadAllText(path);
        if (code)
        {
            text = TAuditLiteralPattern.Replace(
                text,
                match => "\"\"" + new string('\n', match.Value.Count(character => character == '\n')));
        }

        return text.Split('\n');
    }
}
