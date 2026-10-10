using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Convention.Tests;

internal static class TAuditCommentSnapshot
{
    private const string TAuditSnapshotFile = "tests/Cadroue.Tests.Convention/obj/TAuditCommentSnapshot.json";

    private const string TAuditRestampProblem = "restamped, prose unchanged";

    private static readonly Regex TAuditProsePattern = new(@"\s+", RegexOptions.Compiled);

    private static readonly Regex TAuditStampPattern = new("^Hash: `[0-9a-f]{16}`\\s*$", RegexOptions.Compiled);

    public static List<string> TAuditRestampRead(
        string repoRoot,
        IEnumerable<(string, string, string)> states)
    {
        string snapshotPath = Path.Combine(repoRoot, TAuditSnapshotFile);
        Dictionary<string, string[]> snapshots = TAuditSnapshotRead(snapshotPath);
        SortedDictionary<string, string[]> kept = new(StringComparer.Ordinal);
        List<string> hits = [];
        foreach ((string path, string second, string expected) in states)
        {
            if (!TAuditStampPattern.IsMatch(second))
            {
                continue;
            }

            string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
            string text = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
            bool current = second.Contains($"`{expected}`", StringComparison.Ordinal);
            string[]? entry = snapshots.GetValueOrDefault(relative);
            if (!current)
            {
                kept[relative] = entry is not null && entry[0] == second ? entry : [second, text];
                continue;
            }

            if (entry is null || entry[0] == second || TAuditProseRead(text) != TAuditProseRead(entry[1]))
            {
                continue;
            }

            string? committed = TAuditRatchetFile.TAuditCommittedRead(relative)?.Split('\n').ElementAtOrDefault(1);
            if (committed?.TrimEnd() == second.TrimEnd())
            {
                continue;
            }

            kept[relative] = entry;
            hits.Add($"  {relative}:2 {TAuditRestampProblem}");
        }

        TAuditSnapshotSave(snapshotPath, kept);
        return hits;
    }

    private static Dictionary<string, string[]> TAuditSnapshotRead(string snapshotPath)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(snapshotPath)) ?? [];
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            return [];
        }
    }

    private static void TAuditSnapshotSave(string snapshotPath, SortedDictionary<string, string[]> snapshots)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
        string text = JsonSerializer.Serialize(snapshots, new JsonSerializerOptions { WriteIndented = true });
        string written = text.Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
        File.WriteAllText(snapshotPath, written, new UTF8Encoding(false));
    }

    private static string TAuditProseRead(string text)
    {
        IEnumerable<string> lines = text.Split('\n')
            .Where((line, index) => index != 1 || !TAuditStampPattern.IsMatch(line))
            .Select(line => TAuditProsePattern.Replace(line.Trim(), " "))
            .Where(line => line.Length > 0);
        return string.Join('\n', lines);
    }
}
