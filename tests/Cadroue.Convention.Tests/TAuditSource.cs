using System.Diagnostics;

namespace Convention.Tests;

internal static class TAuditSource
{
    public static string TAuditRootRead()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) ||
                File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"No Git working tree was found above '{AppContext.BaseDirectory}'.");
    }

    public static IReadOnlyList<string> TAuditFileRead(string repoRoot)
    {
        ProcessStartInfo info = new("git")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string argument in new[]
                 {
                     "-c", "core.quotePath=false", "ls-files",
                     "--cached", "--others", "--exclude-standard", "--"
                 })
        {
            info.ArgumentList.Add(argument);
        }

        foreach (string pattern in TAuditSetting.TAuditSourceInclude)
        {
            info.ArgumentList.Add(pattern);
        }

        using Process process = Process.Start(info)
            ?? throw new InvalidOperationException("Git could not be started to enumerate source files.");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git failed to enumerate source files.\n{error}");
        }

        List<string> files = [];
        foreach (string line in output.Split('\n'))
        {
            string relative = line.Trim();
            if (relative.Length == 0 || TAuditExcludedCheck(relative))
            {
                continue;
            }

            string full = Path.Combine(repoRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(full))
            {
                continue;
            }

            files.Add(full);
        }

        files.Sort(StringComparer.OrdinalIgnoreCase);
        return files;
    }

    private static bool TAuditExcludedCheck(string relativePath)
    {
        string[] segments = relativePath.Split('/', '\\');
        foreach (string segment in segments)
        {
            if (TAuditSetting.TAuditExcludedSegments.Contains(segment, StringComparer.Ordinal))
            {
                return true;
            }
        }

        string fileName = segments[^1];

        // The tooling carries names it does not own, and a project that imports it did not choose
        // them, so the audit does not audit itself.
        if (TAuditSetting.TAuditSelfExcluded.Contains(fileName, StringComparer.Ordinal))
        {
            return true;
        }

        foreach (string suffix in TAuditSetting.TAuditExcludedSuffixes)
        {
            if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (string prefix in TAuditSetting.TAuditExcludedPrefixes)
        {
            if (fileName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
