namespace Convention.Tests;

internal static class TAuditPlatformFile
{
    public static readonly string[] TAuditImportNames = ["Directory.Build.props", "Directory.Build.targets"];

    public static readonly SortedDictionary<string, string> TAuditUnreadable = new(StringComparer.Ordinal);

    public static IEnumerable<string> TAuditImportRead(string repoRoot) => TAuditSource.TAuditFileRead(
        repoRoot, new TAuditScope([], TAuditImportNames.Select(name => "*" + name).ToArray(), [], [], [], []));

    public static string[] TAuditTextRead(string path)
    {
        try
        {
            return File.ReadAllLines(path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            TAuditUnreadable[path] = error.Message;
            return [];
        }
    }

    public static IEnumerable<string> TAuditChainRead(string repoRoot, string project, string[] names)
    {
        string root = Path.TrimEndingDirectorySeparator(repoRoot);
        DirectoryInfo? folder = new FileInfo(project).Directory;
        while (folder is not null)
        {
            foreach (string name in names)
            {
                string candidate = Path.Combine(folder.FullName, name);
                if (File.Exists(candidate))
                {
                    yield return candidate;
                }
            }

            string current = Path.TrimEndingDirectorySeparator(folder.FullName);
            if (string.Equals(current, root, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            folder = folder.Parent;
        }
    }

    public static string TAuditRelativeRead(string repoRoot, string path) =>
        Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
}
