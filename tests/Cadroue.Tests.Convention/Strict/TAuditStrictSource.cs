using System.Text.RegularExpressions;

namespace Convention.Tests;

internal static class TAuditStrictSource
{
    private static readonly Regex TAuditLiteralPattern = new(
        @"@?""(?:[^""\\]|\\.)*""|//.*$",
        RegexOptions.Compiled);

    public static IEnumerable<string> TAuditExemptRead(
        IReadOnlyList<string> include, IReadOnlyList<string> forbidden, IReadOnlyList<string> exempt)
    {
        HashSet<string> used = TAuditSourceScan(include, forbidden, [])
            .Select(hit => Path.GetFileName(hit.Trim().Split(':')[0]))
            .ToHashSet(StringComparer.Ordinal);
        return exempt.Where(name => !used.Contains(name)).Select(name => $"  {name}");
    }

    public static List<string> TAuditSourceScan(
        IReadOnlyList<string> include, IReadOnlyList<string> forbidden, IReadOnlyList<string> exempt)
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            [],
            include,
            TAuditNameSetting.TAuditExcludedSegments,
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            []);
        List<string> hits = [];
        foreach (string path in TAuditSource.TAuditFileRead(repoRoot, scope))
        {
            if (exempt.Contains(Path.GetFileName(path), StringComparer.Ordinal))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(path);
            for (int index = 0; index < lines.Length; index++)
            {
                string bare = TAuditLiteralPattern.Replace(lines[index], string.Empty);
                foreach (string pattern in forbidden)
                {
                    if (Regex.IsMatch(bare, pattern))
                    {
                        string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                        hits.Add($"  {relative}:{index + 1} {pattern}");
                    }
                }
            }
        }

        return hits;
    }
}
