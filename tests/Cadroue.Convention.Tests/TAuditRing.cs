using System.Text.RegularExpressions;
using Xunit;

namespace Convention.Tests;

public sealed class TAuditRing
{
    private const string TAuditRingSource = "src/";

    private static readonly Regex TAuditRingReference = new(
        @"<ProjectReference\s+Include=""[^""]*[\\/](?<name>[\w.]+)\.csproj""", RegexOptions.Compiled);

    [Fact]
    public void AuditRing_Projects_ReferenceInward()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new([], [TAuditRingSource + "*.csproj"], [], [], [], []);
        Dictionary<string, string[]> found = TAuditSource.TAuditFileRead(repoRoot, scope)
            .ToDictionary(
                static path => Path.GetFileNameWithoutExtension(path), TAuditEdgeRead, StringComparer.Ordinal);

        List<string> drift = [];
        IEnumerable<string> projects = found.Keys
            .Union(TAuditRingSetting.TAuditRingEdges.Keys, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);
        foreach (string project in projects)
        {
            string[] actual = found.GetValueOrDefault(project, []);
            string[] expected = TAuditRingSetting.TAuditRingEdges.GetValueOrDefault(project, []);
            foreach (string edge in actual.Except(expected, StringComparer.Ordinal))
            {
                drift.Add($"  {project} -> {edge} is referenced but not in the ring table");
            }

            foreach (string edge in expected.Except(actual, StringComparer.Ordinal))
            {
                drift.Add($"  {project} -> {edge} is in the ring table but not referenced");
            }
        }

        Assert.True(drift.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITRING",
            $"{drift.Count} project edge(s) differ from the ring table:\n{string.Join('\n', drift)}"));
    }

    private static string[] TAuditEdgeRead(string csproj)
    {
        return TAuditRingReference.Matches(File.ReadAllText(csproj))
            .Select(match => match.Groups["name"].Value)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
