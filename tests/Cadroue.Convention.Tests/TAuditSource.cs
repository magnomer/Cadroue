using System.Diagnostics;
using System.Text.Json;

namespace Convention.Tests;

internal static class TAuditSource
{
    private const string TAuditVersionFile = "version.json";

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

    public static string TAuditVersionRead(string repoRoot)
    {
        using JsonDocument document = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(repoRoot, TAuditVersionFile)));
        return document.RootElement.GetProperty("current-version").GetString() ?? "unknown";
    }

    public static IReadOnlyList<string> TAuditFileRead(string repoRoot, TAuditScope scope)
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

        foreach (string pattern in scope.TAuditScopeInclude)
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
            if (relative.Length == 0 || TAuditExcludedCheck(relative, scope))
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

    private static bool TAuditExcludedCheck(string relativePath, TAuditScope scope)
    {
        string normalized = relativePath.Replace('\\', '/');
        if (scope.TAuditScopeRoots.Count > 0
            && !scope.TAuditScopeRoots.Any(root =>
                normalized.StartsWith(root.Trim('/') + "/", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        string[] segments = normalized.Split('/');

        foreach (string segment in segments)
        {
            if (scope.TAuditScopeSegments.Contains(segment, StringComparer.Ordinal))
            {
                return true;
            }
        }

        string fileName = segments[^1];
        if (scope.TAuditScopeFiles.Contains(fileName, StringComparer.Ordinal))
        {
            return true;
        }

        foreach (string suffix in scope.TAuditScopeSuffixes)
        {
            if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (string prefix in scope.TAuditScopePrefixes)
        {
            if (fileName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
