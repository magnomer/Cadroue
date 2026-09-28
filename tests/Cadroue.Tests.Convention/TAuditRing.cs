using System.Xml.Linq;
using Xunit;

namespace Convention.Tests;

public sealed class TAuditRing
{
    private const string TAuditRingAudit = "AUDITRING";

    private const string TAuditRingSource = "src/";

    private const string TAuditTransitiveKind = "Transitive";

    private static readonly string[] TAuditImportNames = ["Directory.Build.props", "Directory.Build.targets"];

    private static readonly string[] TAuditItemNames = ["ProjectReference", "Reference", "Compile"];

    [Fact]
    public void AuditRing_Projects_ReferenceInward()
    {
        Dictionary<string, string[]> found = TAuditProjectRead()
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
            TAuditRingAudit,
            $"{drift.Count} project edge(s) differ from the ring table:\n{string.Join('\n', drift)}"));
    }

    [Fact]
    public void AuditRing_Projects_HideNoReference()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        List<string> hidden = [];
        foreach (string project in TAuditProjectRead())
        {
            string folder = Path.GetDirectoryName(project)!;
            foreach (XElement item in XDocument.Load(project).Descendants())
            {
                string include = (string?)item.Attribute("Include") ?? string.Empty;
                bool linked = item.Name.LocalName == "Compile"
                              && (item.Attribute("Link") is not null
                                  || !Path.GetFullPath(Path.Combine(folder, include)).StartsWith(
                                      folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
                bool binary = item.Name.LocalName == "Reference" && item.Elements().Any(child =>
                    child.Name.LocalName == "HintPath");
                if (linked || binary)
                {
                    string owner = Path.GetRelativePath(repoRoot, project).Replace('\\', '/');
                    hidden.Add($"  {owner} {item.Name.LocalName} {include}");
                }
            }
        }

        TAuditScope imports = new([], TAuditImportNames.Select(name => "*" + name).ToArray(), [], [], [], []);
        foreach (string import in TAuditSource.TAuditFileRead(repoRoot, imports))
        {
            string owner = Path.GetRelativePath(repoRoot, import).Replace('\\', '/');
            hidden.AddRange(XDocument.Load(import).Descendants()
                .Where(item => TAuditItemNames.Contains(item.Name.LocalName, StringComparer.Ordinal))
                .Select(item => $"  {owner} {item.Name.LocalName}"));
        }

        Assert.True(hidden.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditRingAudit,
            $"{hidden.Count} reference(s) or source link(s) bypass the ring table:\n{string.Join('\n', hidden)}"));
    }

    [Fact]
    public void AuditRing_CutProjects_CompileAgainstNeighbour()
    {
        List<string> open = TAuditOpenRead();
        int ceiling = TAuditRingSetting.TAuditRingCeiling.GetValueOrDefault(TAuditTransitiveKind);
        Assert.True(open.Count <= ceiling, TAuditConvention.TAuditReportFormat(
            TAuditRingAudit,
            $"{open.Count} cut project(s) compile against rings past their neighbour, ceiling {ceiling}:\n"
            + string.Join('\n', open)));
    }

    [Fact]
    public void AuditRing_Ceiling_MatchesHits()
    {
        int count = TAuditOpenRead().Count;
        int ceiling = TAuditRingSetting.TAuditRingCeiling.GetValueOrDefault(TAuditTransitiveKind);
        Assert.True(count >= ceiling, TAuditConvention.TAuditReportFormat(
            TAuditRingAudit,
            $"The {TAuditTransitiveKind} ceiling {ceiling} sits above the count {count} and must be lowered."));
    }

    private static List<string> TAuditOpenRead()
    {
        return TAuditProjectRead()
            .Where(project => TAuditChainSetting.TAuditChainCut.Contains(
                Path.GetFileNameWithoutExtension(project), StringComparer.Ordinal))
            .Where(project => !XDocument.Load(project).Descendants()
                .Where(node => node.Name.LocalName == "DisableTransitiveProjectReferences")
                .Any(node => node.Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)))
            .Select(project => $"  {Path.GetFileNameWithoutExtension(project)}")
            .ToList();
    }

    private static IReadOnlyList<string> TAuditProjectRead()
    {
        TAuditScope scope = new([], [TAuditRingSource + "*.csproj"], [], [], [], []);
        return TAuditSource.TAuditFileRead(TAuditSource.TAuditRootRead(), scope);
    }

    private static string[] TAuditEdgeRead(string csproj)
    {
        return XDocument.Load(csproj).Descendants()
            .Where(node => node.Name.LocalName == "ProjectReference")
            .Select(node => ((string?)node.Attribute("Include") ?? string.Empty).Replace('\\', '/'))
            .Select(include => Path.GetFileNameWithoutExtension(include[(include.LastIndexOf('/') + 1)..]))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
