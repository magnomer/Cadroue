using System.Diagnostics;

namespace Cadroue.Convention.Tests;

internal static class TAuditSource
{
    private static readonly string[] TAuditExcludedSegments =
    {
        ".git", "bin", "obj", "out", "publish", "snapshots", "TestResults"
    };

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
                     "--cached", "--others", "--exclude-standard", "--", "*.cs", "*.xaml"
                 })
        {
            info.ArgumentList.Add(argument);
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

            files.Add(Path.Combine(repoRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
        }

        files.Sort(StringComparer.OrdinalIgnoreCase);
        return files;
    }

    private static bool TAuditExcludedCheck(string relativePath)
    {
        string[] segments = relativePath.Split('/', '\\');
        foreach (string segment in segments)
        {
            if (TAuditExcludedSegments.Contains(segment, StringComparer.Ordinal))
            {
                return true;
            }
        }

        string fileName = segments[^1];
        return fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".GlobalUsings.g.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.StartsWith("TemporaryGeneratedFile_", StringComparison.Ordinal) ||
               fileName.StartsWith("GeneratedInternalTypeHelper", StringComparison.Ordinal);
    }
}
