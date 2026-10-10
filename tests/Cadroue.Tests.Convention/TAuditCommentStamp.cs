using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditCommentStamp
{
    private const string TAuditMissingProblem = "no hash";

    private const string TAuditChangedProblem = "source changed";

    private const string TAuditUnstampedKind = "Unstamped";

    private static readonly Regex TAuditHashPattern = new("^Hash: `(?<hash>[0-9a-f]{16})`$", RegexOptions.Compiled);

    private readonly ITestOutputHelper _tAuditOutput;

    public TAuditCommentStamp(ITestOutputHelper output)
    {
        _tAuditOutput = output;
    }

    [Fact]
    public void AuditComment_StampedFiles_MatchSource()
    {
        List<string> hits = TAuditStampRead(TAuditSource.TAuditRootRead())
            .Where(hit => hit.EndsWith(TAuditChangedProblem, StringComparison.Ordinal))
            .ToList();

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"{hits.Count} comment file(s) carry a hash their source no longer matches.\n"
            + "A stale hash means the source changed under this prose. Do not just restamp it.\n"
            + "For each file, diff its sources (git diff -- <source>) and reread every section the diff touches.\n"
            + "Rewrite prose that no longer holds, add sections for new members and drop sections for removed ones.\n"
            + "Only then run scripts/StampComment.ps1 on the file. It lists the sections the source changes touch.\n"
            + "A restamp that leaves the prose word for word is flagged on the next run.\n"
            + string.Join('\n', hits)));
    }

    [Fact]
    public void AuditComment_RestampedFiles_ReviseProse()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        List<string> hits = TAuditCommentSnapshot.TAuditRestampRead(repoRoot, TAuditStateRead(repoRoot));
        if (hits.Count > 0)
        {
            _tAuditOutput.WriteLine(TAuditConvention.TAuditReportFormat(
                "AUDITCOMMENTS",
                "WARNING: HASH BUMPED, COMMENT NOT REVISED.\n"
                + "THESE FILES WERE RESTAMPED, YET THEIR PROSE MATCHES THE TEXT THEY HELD WHILE STALE.\n"
                + "DO NOT BUMP HASHES MINDLESSLY. A STAMP VOUCHES THAT THE PROSE WAS REREAD.\n"
                + "DIFF EACH SOURCE, REREAD EVERY SECTION IT TOUCHES, REWRITE WHAT NO LONGER HOLDS.\n"
                + "IF THE PROSE STILL HOLDS, STATE WHY FOR EACH FILE IN THE REPORT OF THIS CHANGE.\n"
                + "THE WARNING CLEARS WHEN THE PROSE CHANGES OR A COMMIT CARRIES THE NEW STAMP."));
            _tAuditOutput.WriteLine(string.Join('\n', hits));
        }
    }

    [Fact]
    public void AuditComment_UnstampedFiles_HoldWithinCeiling()
    {
        List<string> hits = TAuditStampRead(TAuditSource.TAuditRootRead())
            .Where(hit => hit.EndsWith(TAuditMissingProblem, StringComparison.Ordinal))
            .ToList();
        int ceiling = TAuditCommentSetting.TAuditCommentCeiling.GetValueOrDefault(TAuditUnstampedKind);
        if (hits.Count > 0)
        {
            _tAuditOutput.WriteLine(TAuditConvention.TAuditReportFormat(
                "AUDITCOMMENTS",
                $"WARNING (not a failure while at or below the ceiling {ceiling}): "
                + $"{hits.Count} comment file(s) carry no hash. "
                + "Reread each one, then stamp it with scripts/StampComment.ps1."));
            _tAuditOutput.WriteLine(string.Join('\n', hits));
        }

        Assert.True(hits.Count <= ceiling, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"{hits.Count} comment file(s) carry no hash, above the ceiling {ceiling}. "
            + "A new comment file is stamped when it is written.\n"
            + string.Join('\n', hits)));
    }

    [Fact]
    public void AuditComment_UnstampedCeiling_MatchesCount()
    {
        int count = TAuditStampRead(TAuditSource.TAuditRootRead())
            .Count(hit => hit.EndsWith(TAuditMissingProblem, StringComparison.Ordinal));
        int ceiling = TAuditCommentSetting.TAuditCommentCeiling.GetValueOrDefault(TAuditUnstampedKind);

        Assert.True(count >= ceiling, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"The ceiling {ceiling} sits above the {count} unstamped comment file(s) and must be lowered."));
    }

    private static List<string> TAuditStampRead(string repoRoot)
    {
        List<string> hits = [];
        foreach ((string path, string second, string expected) in TAuditStateRead(repoRoot))
        {
            Match stamp = TAuditHashPattern.Match(second);
            string problem = !stamp.Success
                ? TAuditMissingProblem
                : stamp.Groups["hash"].Value == expected ? string.Empty : TAuditChangedProblem;
            if (problem.Length > 0)
            {
                string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                hits.Add($"  {relative}:2 {problem}");
            }
        }

        return hits;
    }

    private static List<(string, string, string)> TAuditStateRead(string repoRoot)
    {
        List<(string, string, string)> states = [];
        HashSet<string> rooted = new(
            TAuditCommentSetting.TAuditCommentFiles.Select(
                file => TAuditCommentFile.TAuditCommentRead(Path.Combine(repoRoot, file))),
            StringComparer.OrdinalIgnoreCase);
        IEnumerable<IGrouping<string, string>> pairs = TAuditCommentFile.TAuditOwnerRead(repoRoot)
            .GroupBy(TAuditCommentFile.TAuditCommentRead, StringComparer.OrdinalIgnoreCase)
            .Where(group => File.Exists(group.Key))
            .Where(group => !TAuditCommentFile.TAuditReservedCheck(repoRoot, group.Key))
            .OrderBy(group => rooted.Contains(group.Key))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase);
        foreach (IGrouping<string, string> pair in pairs)
        {
            string[] owners = [.. pair.OrderBy(Path.GetFileName, StringComparer.Ordinal)];
            if (owners.Any(owner => TAuditCommentSetting.TAuditCommentExempt.Contains(
                    Path.GetFileName(owner), StringComparer.OrdinalIgnoreCase)))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(pair.Key);
            states.Add((pair.Key, lines.Length >= 2 ? lines[1] : string.Empty, TAuditHashRead(owners)));
        }

        return states;
    }

    private static string TAuditHashRead(IEnumerable<string> owners)
    {
        string text = string.Concat(owners.Select(owner => File.ReadAllText(owner)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')));
        byte[] digest = SHA256.HashData(new UTF8Encoding(false).GetBytes(text));
        return Convert.ToHexStringLower(digest)[..16];
    }
}
