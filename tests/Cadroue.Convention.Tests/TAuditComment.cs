using System.Text.RegularExpressions;
using Xunit;

namespace Convention.Tests;

public sealed class TAuditComment
{
    private static readonly Regex TAuditLiteralPattern = new(
        """"(?:"{3,})[\s\S]*?"{3,}|"""" +
        """"(?:@\$?|\$@)"(?:[^"]|"")*"|"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])'"""",
        RegexOptions.Compiled);

    private static readonly Regex TAuditSpanPattern = new("`[^`]*`", RegexOptions.Compiled);

    private static readonly Regex TAuditBulletPattern = new(@"^[-*>]\s+", RegexOptions.Compiled);

    private static readonly Regex TAuditSentencePattern = new(
        "[" + Regex.Escape(string.Concat(TAuditCommentSetting.TAuditCommentMarks)) + @"]\s+\p{Lu}",
        RegexOptions.Compiled);

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
        foreach (string path in TAuditSource.TAuditFileRead(repoRoot, scope))
        {
            string[] lines = File.ReadAllLines(path);
            for (int index = 0; index < lines.Length; index++)
            {
                string problem = TAuditLineCheck(lines[index]);
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
        IReadOnlyList<string> sources = TAuditSource.TAuditFileRead(repoRoot, scope);

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
            for (int index = 0; index < lines.Length; index++)
            {
                string text = lines[index];
                string? marker = markers.FirstOrDefault(entry => text.Contains(entry, StringComparison.Ordinal));
                if (marker is not null)
                {
                    string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                    hits.Add($"  {relative}:{index + 1} {marker}");
                }
            }
        }

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"{hits.Count} in-code comment(s) found. "
            + $"Prose belongs in the {TAuditCommentSetting.TAuditCommentPattern} file beside the source.\n"
            + string.Join('\n', hits)));
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

    private static string TAuditLineCheck(string line)
    {
        string text = line.Trim();
        if (text.Length == 0 || text.StartsWith('#'))
        {
            return string.Empty;
        }

        text = TAuditBulletPattern.Replace(text, string.Empty);
        List<string> problems = [];
        int words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (words > TAuditCommentSetting.TAuditCommentWords)
        {
            problems.Add($"{words} words");
        }

        string prose = TAuditSpanPattern.Replace(text, string.Empty);
        foreach (string token in TAuditCommentSetting.TAuditCommentForbidden)
        {
            if (prose.Contains(token, StringComparison.Ordinal))
            {
                problems.Add($"forbidden '{token}'");
            }
        }

        int sentences = 1 + TAuditSentencePattern.Matches(prose).Count;
        if (sentences > 1)
        {
            problems.Add($"{sentences} sentences");
        }

        return string.Join(", ", problems);
    }
}
