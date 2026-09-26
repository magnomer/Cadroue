using System.Text.RegularExpressions;

using Xunit;

namespace Cadroue.Tests;

public sealed class TAuditWait
{
    private sealed record TAuditKind(
        string TAuditLabel, Regex TAuditPattern, int TAuditCeiling, string? TAuditOwner = null);

    private static readonly Regex TAuditLiteral = new(
        @"@""(?:[^""]|"""")*""|\$?""(?:\\.|[^""\\\r\n])*""|'(?:\\.|[^'\\\r\n])'",
        RegexOptions.Compiled);

    private static readonly TAuditKind[] TAuditKinds =
    [
        new("process exit waited synchronously", new Regex(@"\.WaitForExit\s*\(", RegexOptions.Compiled), 0),
        new(
            "task result blocked on",
            new Regex(@"\.GetAwaiter\s*\(\s*\)\s*\.GetResult\s*\(|\.Result\b(?!\s*\.)", RegexOptions.Compiled),
            0),
        new("task or event waited synchronously", new Regex(@"\.Wait\s*\(", RegexOptions.Compiled), 2),
        new("thread put to sleep", new Regex(@"\bThread\s*\.\s*Sleep\s*\(", RegexOptions.Compiled), 2),
        new("wait handle waited", new Regex(@"\.WaitOne\s*\(", RegexOptions.Compiled), 1),
        new(
            "process started outside LEmployer",
            new Regex(@"\bProcess\s*\.\s*Start\s*\(|\bnew\s+Process\b", RegexOptions.Compiled),
            8,
            "LEmployer.cs")
    ];

    [Fact]
    public void Sources_HoldNoThreadWaiting_BeyondTheirCeiling()
    {
        string sourceRoot = TAuditRootRead();
        var sources = Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(sourceRoot, path)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part is "bin" or "obj"))
            .Select(path => (Path: Path.GetRelativePath(sourceRoot, path), Text: TAuditLiteral.Replace(
                File.ReadAllText(path), string.Empty)))
            .ToArray();

        var failures = new List<string>();
        foreach (TAuditKind kind in TAuditKinds)
        {
            string[] hits = sources
                .Where(source => kind.TAuditOwner is null
                    || !string.Equals(Path.GetFileName(source.Path), kind.TAuditOwner, StringComparison.Ordinal))
                .SelectMany(source => kind.TAuditPattern.Matches(source.Text)
                    .Select(match => $"{source.Path}:{source.Text[..match.Index].Count(c => c == '\n') + 1}"))
                .ToArray();
            if (hits.Length != kind.TAuditCeiling)
            {
                failures.Add(
                    $"{kind.TAuditLabel}: {hits.Length} hit(s), ceiling {kind.TAuditCeiling}"
                    + (hits.Length < kind.TAuditCeiling ? " (lower the ceiling)" : string.Empty)
                    + $" [{string.Join(", ", hits)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static string TAuditRootRead()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "src");
            if (File.Exists(Path.Combine(directory.FullName, "Cadroue.sln")) && Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the src folder beside Cadroue.sln.");
    }
}
