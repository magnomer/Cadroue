using System.Text.RegularExpressions;

using Xunit;

namespace Cadroue.Tests;

public sealed class InterfaceBoundaryTests
{
    private static readonly Regex DirectProductionCall = new(
        @"\bL[A-Z][A-Za-z0-9_]*\s*\.\s*L[A-Z][A-Za-z0-9_]*\s*\(|\.\s*L[A-Z][A-Za-z0-9_]*\s*\(",
        RegexOptions.Compiled);

    private static readonly Regex DirectProductionConstruction = new(
        @"\bnew\s+L[A-Z][A-Za-z0-9_]*\s*[({]",
        RegexOptions.Compiled);

    [Fact]
    public void Tests_ReachProductionOperationsOnlyThroughInterface()
    {
        string projectRoot = ProjectRootRead();
        string[] offenders = Directory
            .EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(projectRoot, path)
                .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(part => part is "Interface" or "bin" or "obj"))
            .Where(path =>
            {
                string source = File.ReadAllText(path);
                return DirectProductionCall.IsMatch(source) || DirectProductionConstruction.IsMatch(source);
            })
            .Select(path => Path.GetRelativePath(projectRoot, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            "Production operations must be relayed by tests/Cadroue.Tests/Interface. Direct usage: " +
            string.Join(", ", offenders));
    }

    private static string ProjectRootRead()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "tests", "Cadroue.Tests");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate tests/Cadroue.Tests.");
    }
}
