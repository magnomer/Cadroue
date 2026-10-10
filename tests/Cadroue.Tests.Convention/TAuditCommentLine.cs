using System.Text.RegularExpressions;

namespace Convention.Tests;

internal static class TAuditCommentLine
{
    private static readonly Regex TAuditSpanPattern = new("`[^`]*`", RegexOptions.Compiled);

    private static readonly Regex TAuditBulletPattern = new(@"^[-*>]\s+", RegexOptions.Compiled);

    private static readonly Regex TAuditSentencePattern = new(
        "[" + Regex.Escape(string.Concat(TAuditCommentSetting.TAuditCommentMarks)) + @"]\s+\p{L}",
        RegexOptions.Compiled);

    private static readonly Regex TAuditAbbreviationPattern = new(
        @"(?<![\p{L}.])(?:"
        + string.Join('|', TAuditCommentSetting.TAuditCommentAbbreviations.Select(Regex.Escape))
        + @")\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TAuditLevelPattern = new(@"^#+\s*", RegexOptions.Compiled);

    public static string TAuditLineCheck(string line)
    {
        string text = line.Trim();
        bool heading = TAuditLevelPattern.IsMatch(text);
        text = TAuditLevelPattern.Replace(text, string.Empty);
        if (text.Length == 0)
        {
            return string.Empty;
        }

        text = TAuditBulletPattern.Replace(text, string.Empty);
        string prose = TAuditSpanPattern.Replace(text, string.Empty);
        List<string> problems = [];
        int words = (heading ? prose : text).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (words > TAuditCommentSetting.TAuditCommentWords)
        {
            problems.Add($"{words} words");
        }

        foreach (string token in TAuditCommentSetting.TAuditCommentForbidden)
        {
            if (prose.Contains(token, StringComparison.Ordinal))
            {
                problems.Add($"forbidden '{token}'");
            }
        }

        int sentences = 1 + TAuditSentencePattern.Matches(TAuditAbbreviationPattern.Replace(prose, "abbr")).Count;
        if (sentences > 1)
        {
            problems.Add($"{sentences} sentences");
        }

        return string.Join(", ", problems);
    }
}
