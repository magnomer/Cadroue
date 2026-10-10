using System.Text.RegularExpressions;
using Xunit;

namespace Convention.Tests;

public sealed class TAuditComment
{
    private static readonly Regex TAuditLiteralPattern = new(
        """"(?:"{3,})[\s\S]*?"{3,}|"""" +
        """"(?:@\$?|\$@)"(?:[^"]|"")*"|"(?:\\.|[^"\\\r\n])*"|'(?:\\.|[^'\\\r\n])'"""",
        RegexOptions.Compiled);

    private static readonly string[] TAuditCodeExtensions = ["", ".cs", ".xaml", ".xaml.cs"];

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
        TAuditScope scope = new(
            TAuditCommentSetting.TAuditCommentRoots,
            [TAuditCommentSetting.TAuditCommentPattern],
            TAuditCommentSetting.TAuditCommentSegments,
            [],
            [],
            []);
        IEnumerable<string> comments = TAuditCommentFile.TAuditScanRead(repoRoot, scope).Concat(
            TAuditCommentSetting.TAuditCommentFiles
                .Select(file => TAuditCommentFile.TAuditCommentRead(Path.Combine(repoRoot, file)))
                .Where(File.Exists))
            .Where(path => !TAuditCommentFile.TAuditReservedCheck(repoRoot, path));
        foreach (string path in comments)
        {
            string[] lines = File.ReadAllLines(path);
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

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
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

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"{hits.Count} in-code comment(s) found. "
            + $"Prose belongs in the {TAuditCommentSetting.TAuditCommentPattern} file beside the source.\n"
            + string.Join('\n', hits)));
    }

    [Fact]
    public void AuditComment_Sources_CarryCommentFile()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        List<string> hits = TAuditCommentFile.TAuditOwnerRead(repoRoot)
            .Where(path => !File.Exists(TAuditCommentFile.TAuditCommentRead(path)))
            .Select(path => "  " + Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
            .ToList();

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"{hits.Count} source(s) have no comment file beside them.\n" + string.Join('\n', hits)));
    }

    [Fact]
    public void AuditComment_CommentFiles_HaveSource()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        HashSet<string> expected = new(
            TAuditCommentFile.TAuditOwnerRead(repoRoot).Select(TAuditCommentFile.TAuditCommentRead),
            StringComparer.OrdinalIgnoreCase);
        TAuditScope scope = new(
            TAuditCommentSetting.TAuditCommentRoots,
            [TAuditCommentSetting.TAuditCommentPattern],
            TAuditCommentSetting.TAuditCommentSegments,
            [],
            [],
            []);
        List<string> hits = TAuditCommentFile.TAuditScanRead(repoRoot, scope)
            .Where(path => !expected.Contains(path))
            .Select(path => "  " + Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
            .ToList();

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"{hits.Count} comment file(s) have no source beside them.\n" + string.Join('\n', hits)));
    }

    [Fact]
    public void AuditComment_Headings_NameMembers()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            TAuditCommentSetting.TAuditCommentRoots,
            [TAuditCommentSetting.TAuditCommentPattern],
            TAuditCommentSetting.TAuditCommentSegments,
            [],
            [],
            []);
        List<string> hits = [];
        IEnumerable<string> comments = TAuditCommentFile.TAuditScanRead(repoRoot, scope).Concat(
            TAuditCommentSetting.TAuditCommentFiles
                .Select(file => TAuditCommentFile.TAuditCommentRead(Path.Combine(repoRoot, file)))
                .Where(File.Exists))
            .Where(path => !TAuditCommentFile.TAuditReservedCheck(repoRoot, path));
        foreach (string comment in comments)
        {
            string stem = comment[..^TAuditCommentSetting.TAuditCommentPattern.TrimStart('*').Length];
            string[] sources = [.. TAuditCodeExtensions.Select(extension => stem + extension).Where(File.Exists)];
            if (sources.Length == 0 || TAuditCommentSetting.TAuditCommentExempt.Contains(
                    Path.GetFileName(sources[0]), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            string text = string.Concat(sources.Select(File.ReadAllText));

            string[] lines = File.ReadAllLines(comment);
            for (int index = 0; index < lines.Length; index++)
            {
                Match heading = TAuditHeadingPattern.Match(lines[index]);
                string? name = heading.Success ? TAuditMemberRead(heading.Groups["span"].Value) : null;
                if (name is not null && !Regex.IsMatch(text, $@"\b{Regex.Escape(name)}\b"))
                {
                    string relative = Path.GetRelativePath(repoRoot, comment).Replace('\\', '/');
                    hits.Add($"  {relative}:{index + 1} {name}");
                }
            }
        }

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"{hits.Count} comment heading(s) name nothing in their source.\n" + string.Join('\n', hits)));
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
