using System.Xml.Linq;
using Xunit;

namespace Convention.Tests;

public sealed class TAuditCharter
{
    private const string TAuditCharterAudit = "AUDITCHARTER";

    private const string TAuditCharterSource = "src/";

    private const string TAuditPiggybackingKind = "Piggybacking";

    private static readonly string[] TAuditImportNames = ["Directory.Build.props", "Directory.Build.targets"];

    private static readonly string[] TAuditItemNames = ["ProjectReference", "Reference", "Compile"];

    [Fact]
    public void AuditCharter_Projects_HoldNoRerouting()
    {
        Dictionary<string, string[]> found = TAuditProjectRead()
            .ToDictionary(
                static path => Path.GetFileNameWithoutExtension(path), TAuditEdgeRead, StringComparer.Ordinal);

        List<string> drift = [];
        IEnumerable<string> projects = found.Keys
            .Union(TAuditCharterSetting.TAuditCharterEdges.Keys, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);
        foreach (string project in projects)
        {
            string[] actual = found.GetValueOrDefault(project, []);
            string[] expected = TAuditCharterSetting.TAuditCharterEdges.GetValueOrDefault(project, []);
            foreach (string edge in actual.Except(expected, StringComparer.Ordinal))
            {
                drift.Add($"  {project} -> {edge} is referenced but not in the charter");
            }

            foreach (string edge in expected.Except(actual, StringComparer.Ordinal))
            {
                drift.Add($"  {project} -> {edge} is in the charter but not referenced");
            }
        }

        Assert.True(drift.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditCharterAudit,
            $"{drift.Count} Rerouting project edge(s) differ from the charter:\n{string.Join('\n', drift)}"));
    }

    [Fact]
    public void AuditCharter_Neighbours_MatchProjects()
    {
        List<string> drift = [];
        foreach ((string ring, string[] edges) in TAuditCharterSetting.TAuditCharterEdges.Where(pair =>
                     pair.Key != TAuditBorderSetting.TAuditBorderHost))
        {
            string[] neighbour = TAuditBorderSetting.TAuditBorderNeighbour.GetValueOrDefault(ring) ?? ["(absent)"];
            if (TAuditBorderSetting.TAuditBorderCapsule.GetValueOrDefault(ring) is string capsule)
            {
                neighbour = [.. neighbour, capsule];
            }

            if (!neighbour.Order(StringComparer.Ordinal).SequenceEqual(edges.Order(StringComparer.Ordinal)))
            {
                drift.Add($"  {ring} names [{string.Join(", ", neighbour)}] "
                          + $"but references [{string.Join(", ", edges)}]");
            }
        }

        drift.AddRange(TAuditBorderSetting.TAuditBorderNeighbour.Keys
            .Where(ring => !TAuditCharterSetting.TAuditCharterEdges.ContainsKey(ring))
            .Select(ring => $"  {ring} is walked but is no project in the charter"));
        Assert.True(drift.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditCharterAudit,
            $"{drift.Count} ring(s) walk a neighbour that differs from their project references:\n"
            + string.Join('\n', drift)));
    }

    [Fact]
    public void AuditCharter_Projects_HoldNoBackdooring()
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
            TAuditCharterAudit,
            $"{hidden.Count} Backdooring reference(s) or source link(s) bypass the charter:\n"
            + string.Join('\n', hidden)));
    }

    [Fact]
    public void AuditCharter_CutProjects_HoldNoPiggybacking()
    {
        List<string> open = TAuditOpenRead();
        int ceiling = TAuditCharterSetting.TAuditCharterCeiling.GetValueOrDefault(TAuditPiggybackingKind);
        Assert.True(open.Count <= ceiling, TAuditConvention.TAuditReportFormat(
            TAuditCharterAudit,
            $"{open.Count} {TAuditPiggybackingKind} cut project(s) compile past their neighbour, ceiling {ceiling}:\n"
            + string.Join('\n', open)));
    }

    [Fact]
    public void AuditCharter_Ceiling_MatchesHits()
    {
        int count = TAuditOpenRead().Count;
        int ceiling = TAuditCharterSetting.TAuditCharterCeiling.GetValueOrDefault(TAuditPiggybackingKind);
        Assert.True(count >= ceiling, TAuditConvention.TAuditReportFormat(
            TAuditCharterAudit,
            $"The {TAuditPiggybackingKind} ceiling {ceiling} sits above the count {count} and must be lowered."));
    }

    private static List<string> TAuditOpenRead()
    {
        return TAuditProjectRead()
            .Where(project => TAuditBorderSetting.TAuditBorderCut.Contains(
                Path.GetFileNameWithoutExtension(project), StringComparer.Ordinal))
            .Where(project => !XDocument.Load(project).Descendants()
                .Where(node => node.Name.LocalName == "DisableTransitiveProjectReferences")
                .Any(node => node.Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)))
            .Select(project => $"  {Path.GetFileNameWithoutExtension(project)}")
            .ToList();
    }

    private static IReadOnlyList<string> TAuditProjectRead()
    {
        TAuditScope scope = new([], [TAuditCharterSource + "*.csproj"], [], [], [], []);
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
