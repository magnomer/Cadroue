using System.Text.RegularExpressions;

using Xunit;

namespace Cadroue.Tests.Interface;

public sealed class TInterfaceBoundary
{
    private static readonly Regex TInterfaceDirectCall = new(
        @"\bL[A-Z][A-Za-z0-9_]*\s*\.\s*L[A-Z][A-Za-z0-9_]*\s*\(|\.\s*L[A-Z][A-Za-z0-9_]*\s*\(",
        RegexOptions.Compiled);

    private static readonly Regex TInterfaceDirectConstruct = new(
        @"\bnew\s+L[A-Z][A-Za-z0-9_]*\s*[({]",
        RegexOptions.Compiled);

    [Fact]
    public void Tests_ReachProductionOperationsOnlyThroughInterface()
    {
        string testsRoot = TInterfaceRootRead();
        string[] offenders = Directory
            .EnumerateDirectories(testsRoot, "Cadroue.Tests.*")
            .Where(project => Path.GetFileName(project)
                is not ("Cadroue.Tests.Interface" or "Cadroue.Tests.Convention"))
            .SelectMany(project => Directory
                .EnumerateFiles(project, "*.cs", SearchOption.AllDirectories)
                .Where(path => !Path.GetRelativePath(project, path)
                    .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(part => part is "Interface" or "bin" or "obj")))
            .Where(path =>
            {
                string source = File.ReadAllText(path);
                return TInterfaceDirectCall.IsMatch(source) || TInterfaceDirectConstruct.IsMatch(source);
            })
            .Select(path => Path.GetRelativePath(testsRoot, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "Production operations must be relayed by tests/Cadroue.Tests.Interface. Direct usage: " +
            string.Join(", ", offenders));
    }

    private static string TInterfaceRootRead()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "tests");
            if (Directory.Exists(Path.Combine(candidate, "Cadroue.Tests.Interface")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate tests/Cadroue.Tests.Interface.");
    }
}
