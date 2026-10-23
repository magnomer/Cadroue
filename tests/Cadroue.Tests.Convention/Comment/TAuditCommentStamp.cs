using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditCommentStamp
{
    private const string TAuditMissingProblem = "no stamp";

    private const string TAuditUnstampedKind = "Unstamped";

    private static readonly Regex TAuditCommitPattern =
        new(@"^Commit: `(?<commit>[0-9a-f]{7,40})`\s*$", RegexOptions.Compiled);

    private static readonly Regex TAuditRowPattern =
        new(@"^- `(?<name>[^`]+)` `(?<hash>[0-9a-f]{16})`\s*$", RegexOptions.Compiled);

    private readonly ITestOutputHelper _tAuditOutput;

    public TAuditCommentStamp(ITestOutputHelper output)
    {
        _tAuditOutput = output;
    }

    [Fact]
    public void AuditComment_ChangedFolders_Advise()
    {
        List<string> hits = TAuditStampRead(TAuditSource.TAuditRootRead())
            .Where(hit => !hit.EndsWith(TAuditMissingProblem, StringComparison.Ordinal))
            .ToList();
        if (hits.Count > 0)
        {
            _tAuditOutput.WriteLine(TAuditConvention.TAuditReportFormat(
                "AUDITCOMMENTS",
                $"ADVICE (never a failure): {hits.Count} folder(s) changed since their sidecar was stamped.\n"
                + "Better to check: run the listed diff and reread the sidecar where the change touches its intent.\n"
                + "Then run scripts/StampComment.ps1 on the sidecar."));
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
                + $"{hits.Count} sidecar(s) carry no stamp. "
                + "Reread each one, then stamp it with scripts/StampComment.ps1."));
            _tAuditOutput.WriteLine(string.Join('\n', hits));
        }

        bool held = !TAuditCommentSetting.TAuditCommentEnforced || hits.Count <= ceiling;
        Assert.True(held, TAuditConvention.TAuditReportFormat(
            "AUDITCOMMENTS",
            $"{hits.Count} sidecar(s) carry no stamp, above the ceiling {ceiling}. "
            + "A new sidecar is stamped when it is written.\n"
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
            $"The ceiling {ceiling} sits above the {count} unstamped sidecar(s) and must be lowered."));
    }

    [Fact]
    public void AuditComment_Folders_HoldFileLimit()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        int limit = TAuditCommentSetting.TAuditCommentFolderFiles;
        IReadOnlyDictionary<string, int> ceilings = TAuditCommentSetting.TAuditCommentFolders;
        List<string> hits = TAuditFolderRead(repoRoot)
            .Where(pair => pair.Value > limit
                && !(ceilings.TryGetValue(pair.Key, out int ceiling) && pair.Value <= ceiling))
            .Select(pair => $"  {pair.Key}: {pair.Value} files")
            .ToList();
        if (hits.Count > 0)
        {
            _tAuditOutput.WriteLine(TAuditConvention.TAuditReportFormat(
                "AUDITCOMMENTS",
                $"WARNING (never a failure): {hits.Count} folder(s) hold more than {limit} sources "
                + "above their ceiling. Split the folder by concern, or hold its count under a ceiling row."));
            _tAuditOutput.WriteLine(string.Join('\n', hits));
        }
    }

    [Fact]
    public void AuditComment_FolderCeilings_MatchCount()
    {
        Dictionary<string, int> counts = TAuditFolderRead(TAuditSource.TAuditRootRead());
        int limit = TAuditCommentSetting.TAuditCommentFolderFiles;
        List<string> hits = TAuditCommentSetting.TAuditCommentFolders
            .Where(pair => pair.Value <= limit || pair.Value > counts.GetValueOrDefault(pair.Key))
            .Select(pair => $"  {pair.Key}: ceiling {pair.Value}, count {counts.GetValueOrDefault(pair.Key)}")
            .ToList();
        if (hits.Count > 0)
        {
            _tAuditOutput.WriteLine(TAuditConvention.TAuditReportFormat(
                "AUDITCOMMENTS",
                $"WARNING (never a failure): {hits.Count} folder ceiling(s) sit above their count "
                + $"or at or under the limit {limit}.\n"
                + string.Join('\n', hits)));
        }
    }

    private static Dictionary<string, int> TAuditFolderRead(string repoRoot)
    {
        return TAuditCommentFile.TAuditOwnerRead(repoRoot)
            .GroupBy(path =>
            {
                string directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
                string folder = Path.GetRelativePath(repoRoot, directory).Replace('\\', '/');
                return folder.Length == 0 ? "." : folder;
            }, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    }

    private static List<string> TAuditStampRead(string repoRoot)
    {
        List<string> hits = [];
        IEnumerable<IGrouping<string, string>> folders = TAuditCommentFile.TAuditOwnerRead(repoRoot)
            .GroupBy(path => TAuditCommentFile.TAuditCommentRead(repoRoot, path), StringComparer.OrdinalIgnoreCase)
            .Where(group => File.Exists(group.Key))
            .Where(group => !TAuditCommentFile.TAuditReservedCheck(repoRoot, group.Key))
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);
        foreach (IGrouping<string, string> folder in folders)
        {
            string[] owners = [.. folder
                .Where(owner => !TAuditCommentSetting.TAuditCommentExempt.Contains(
                    Path.GetFileName(owner), StringComparer.OrdinalIgnoreCase))
                .OrderBy(Path.GetFileName, StringComparer.Ordinal)];
            if (owners.Length == 0)
            {
                continue;
            }

            string relative = Path.GetRelativePath(repoRoot, folder.Key).Replace('\\', '/');
            string[] lines = File.ReadAllLines(folder.Key);
            int stamp = TAuditCommentFile.TAuditStampFind(lines);
            string? commit = null;
            Dictionary<string, string> rows = new(StringComparer.Ordinal);
            for (int index = stamp < 0 ? lines.Length : stamp + 1; index < lines.Length; index++)
            {
                Match commitMatch = TAuditCommitPattern.Match(lines[index]);
                if (commitMatch.Success)
                {
                    commit = commitMatch.Groups["commit"].Value;
                    continue;
                }

                Match rowMatch = TAuditRowPattern.Match(lines[index]);
                if (rowMatch.Success)
                {
                    rows[rowMatch.Groups["name"].Value] = rowMatch.Groups["hash"].Value;
                }
            }

            if (stamp < 0 || commit is null)
            {
                hits.Add($"  {relative} {TAuditMissingProblem}");
                continue;
            }

            Dictionary<string, string> hashes = owners.ToDictionary(
                owner => Path.GetFileName(owner), TAuditHashRead, StringComparer.Ordinal);
            List<string> changes = [];
            string[] changed = [.. hashes.Keys
                .Where(name => rows.TryGetValue(name, out string? hash) && hash != hashes[name])];
            string[] added = [.. hashes.Keys.Where(name => !rows.ContainsKey(name))];
            string[] removed = [.. rows.Keys.Where(name => !hashes.ContainsKey(name))];
            if (changed.Length > 0)
            {
                changes.Add("changed " + string.Join(", ", changed));
            }

            if (added.Length > 0)
            {
                changes.Add("added " + string.Join(", ", added));
            }

            if (removed.Length > 0)
            {
                changes.Add("removed " + string.Join(", ", removed));
            }

            if (changes.Count > 0)
            {
                string? directory = Path.GetDirectoryName(relative);
                string folderPath = string.IsNullOrEmpty(directory) ? "." : directory.Replace('\\', '/');
                hits.Add($"  {relative} {string.Join(" | ", changes)} (git diff {commit} -- {folderPath})");
            }
        }

        return hits;
    }

    private static string TAuditHashRead(string owner)
    {
        string text = File.ReadAllText(owner)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        byte[] digest = SHA256.HashData(new UTF8Encoding(false).GetBytes(text));
        return Convert.ToHexStringLower(digest)[..16];
    }
}
