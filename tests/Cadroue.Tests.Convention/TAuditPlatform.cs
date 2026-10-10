using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditPlatform
{
    private const string TAuditProjectExtension = ".csproj";

    private static readonly Lazy<IReadOnlyList<TAuditHit>> TAuditPlatformHits = new(TAuditPlatformRead);

    private readonly ITestOutputHelper _tAuditOutput;

    public TAuditPlatform(ITestOutputHelper output) => _tAuditOutput = output;

    [Fact]
    public void AuditPlatform_Projects_HoldWithinCeiling()
    {
        List<string> over = [];
        foreach (string kind in TAuditPlatformSetting.TAuditPlatformKinds)
        {
            TAuditHit[] hits = TAuditPlatformHits.Value.Where(hit => hit.TAuditHitKind == kind)
                .OrderBy(hit => hit.TAuditHitRing, StringComparer.Ordinal)
                .ThenBy(hit => hit.TAuditHitPath, StringComparer.Ordinal)
                .ThenBy(hit => hit.TAuditHitLine)
                .ThenBy(hit => hit.TAuditHitName, StringComparer.Ordinal)
                .ToArray();
            int ceiling = TAuditPlatformSetting.TAuditPlatformCeiling.GetValueOrDefault(kind);
            _tAuditOutput.WriteLine($"AUDITPLATFORM {kind}: {hits.Length} hit(s), ceiling {ceiling}");
            if (hits.Length <= ceiling)
            {
                continue;
            }

            over.Add($"  {kind}: {hits.Length} hit(s), ceiling {ceiling}");
            over.AddRange(hits.Select(hit => "    " + TAuditRowRead(hit)));
        }

        bool held = !TAuditPlatformSetting.TAuditPlatformEnforced || over.Count == 0;
        _tAuditOutput.WriteLine($"AUDITPLATFORM Unreadable files: {TAuditPlatformFile.TAuditUnreadable.Count}");
        IEnumerable<string> unreadable = TAuditPlatformFile.TAuditUnreadable
            .Select(pair => $"    {pair.Key}: {pair.Value}");
        over.AddRange(unreadable.Any()
            ? unreadable.Prepend($"  Unreadable files: {TAuditPlatformFile.TAuditUnreadable.Count}")
            : []);

        Assert.True(held && TAuditPlatformFile.TAuditUnreadable.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITPLATFORM",
            $"Platform kind(s) count above their ceiling or files are unreadable:\n{string.Join('\n', over)}"));
    }

    [Fact]
    public void AuditPlatform_Ceiling_MatchesHits()
    {
        List<string> stale = TAuditPlatformSetting.TAuditPlatformCeiling
            .Select(pair => (pair.Key, pair.Value,
                Count: TAuditPlatformHits.Value.Count(hit => hit.TAuditHitKind == pair.Key)))
            .Where(pair => pair.Count < pair.Value)
            .Select(pair => $"  {pair.Key}: {pair.Count} hit(s), ceiling {pair.Value}")
            .ToList();

        Assert.True(stale.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITPLATFORM",
            $"{stale.Count} ceiling(s) sit above the count and must be lowered.\n{string.Join('\n', stale)}"));
    }

    private static IReadOnlyList<TAuditHit> TAuditPlatformRead()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        string root = TAuditPlatformSetting.TAuditPlatformRoot;
        TAuditScope scope = new(
            [],
            [root + "*" + TAuditProjectExtension, root + "*.cs", root + "*.xaml"],
            TAuditNameSetting.TAuditExcludedSegments,
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            []);
        IReadOnlyList<string> files = TAuditSource.TAuditFileRead(repoRoot, scope);
        string[] projectFiles = files
            .Where(path => path.EndsWith(TAuditProjectExtension, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        string[] twice = projectFiles
            .GroupBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => $"{group.Key}: " + string.Join(", ", group
                .Select(path => TAuditPlatformFile.TAuditRelativeRead(repoRoot, path)).Order(StringComparer.Ordinal)))
            .ToArray();
        if (twice.Length > 0)
        {
            throw new InvalidOperationException(
                $"Project files share one name, so the audit cannot tell them apart:\n{string.Join('\n', twice)}");
        }

        Dictionary<string, string> projects = projectFiles
            .ToDictionary(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase);
        Assert.True(projects.Count > 0, TAuditConvention.TAuditReportFormat(
            "AUDITPLATFORM", "No project file was enumerated; the audit would pass vacuously."));
        string[] sources = files
            .Where(path => !path.EndsWith(TAuditProjectExtension, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Dictionary<string, string> table = new(
            TAuditPlatformSetting.TAuditPlatformColumn, StringComparer.OrdinalIgnoreCase);

        List<TAuditHit> hits = [];
        hits.AddRange(projects.Where(pair => !table.ContainsKey(pair.Key)).Select(pair => new TAuditHit(
            TAuditPlatformFile.TAuditRelativeRead(repoRoot, pair.Value), 0, pair.Key, "Unmapped", "",
            "the platform table does not name this project")));

        foreach ((string name, string column) in table.Where(pair => !projects.ContainsKey(pair.Key)))
        {
            string role = column.Length == 0 ? "host"
                : column.Equals(name, StringComparison.OrdinalIgnoreCase) ? "portable" : "twin";
            hits.Add(new TAuditHit(
                "", 0, name, "Absent", "", $"the table names this {role}, but no project file exists"));
        }

        foreach ((string name, string path) in projects.Where(pair => table.ContainsKey(pair.Key)))
        {
            string column = table[name];
            string relative = TAuditPlatformFile.TAuditRelativeRead(repoRoot, path);
            XDocument document = XDocument.Load(path);
            string[] frameworks = TAuditPlatformProject.TAuditFrameworkRead(document);
            string[] references = TAuditPlatformProject.TAuditIncludeRead(document, "ProjectReference")
                .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', '/').Split('/')[^1]))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string folder = Path.GetDirectoryName(path)! + Path.DirectorySeparatorChar;
            string[] held = sources.Where(source => source.StartsWith(folder, StringComparison.Ordinal)).ToArray();
            string targets = $"targets '{string.Join(';', frameworks)}'";

            if (column.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                string portable = TAuditPlatformSetting.TAuditPlatformPortable;
                if (frameworks.Length != 1 || !frameworks[0].Equals(portable, StringComparison.OrdinalIgnoreCase))
                {
                    hits.Add(new TAuditHit(relative, 0, name, "Framework", "", $"{targets}, not exactly '{portable}'"));
                }

                hits.AddRange(references
                    .Where(reference => TAuditWindowsCheck(reference, table, projects))
                    .Select(reference => new TAuditHit(
                        relative, 0, name, "Reference", reference, $"references the Windows project {reference}")));

                foreach (string source in held.Where(source => source.EndsWith(".cs", StringComparison.Ordinal))
                             .OrderBy(source => TAuditPlatformFile.TAuditRelativeRead(repoRoot, source),
                                 StringComparer.OrdinalIgnoreCase)
                             .ThenBy(source => TAuditPlatformFile.TAuditRelativeRead(repoRoot, source),
                                 StringComparer.Ordinal)
                             .Where(source => !TAuditPlatformAnalyzer.TAuditAnalyzerCheck(repoRoot, path, source))
                             .Take(1))
                {
                    hits.Add(new TAuditHit(
                        TAuditPlatformFile.TAuditRelativeRead(repoRoot, source), 0, name, "Analyzer", "",
                        $"{TAuditPlatformSetting.TAuditPlatformRule} is not an error here"));
                }

                hits.AddRange(TAuditPlatformPortable.TAuditSuppressRead(repoRoot, path, held, name));

                hits.AddRange(TAuditPlatformSetting.TAuditPlatformProperties
                    .Where(property => TAuditPlatformProject.TAuditValueRead(document, property)
                        .Any(TAuditPlatformProject.TAuditTrueCheck))
                    .Select(property => new TAuditHit(relative, 0, name, "Windows", property, $"enables {property}")));
                hits.AddRange(TAuditPlatformProject.TAuditIncludeRead(document, "PackageReference")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(package => TAuditPlatformSetting.TAuditPlatformPackages
                        .Contains(package, StringComparer.OrdinalIgnoreCase))
                    .Select(package => new TAuditHit(
                        relative, 0, name, "Windows", package, $"references the Windows package {package}")));
                hits.AddRange(held.SelectMany(source =>
                    TAuditPlatformPortable.TAuditSourceScan(repoRoot, source, name)));
            }
            else if (column.Length > 0)
            {
                string twin = TAuditPlatformSetting.TAuditPlatformTwin;
                if (frameworks.Length != 1 || !frameworks[0].StartsWith(twin, StringComparison.OrdinalIgnoreCase))
                {
                    hits.Add(new TAuditHit(relative, 0, name, "Framework", "", $"{targets}, not '{twin}'"));
                }

                hits.AddRange(references
                    .Where(reference => table.GetValueOrDefault(reference) != column
                        && TAuditPlatformSetting.TAuditPlatformCapsule.GetValueOrDefault(name) != reference)
                    .Select(reference => new TAuditHit(
                        relative, 0, name, "Column", reference,
                        $"references {reference} outside the {column} column")));

                if (held.Length == 0)
                {
                    hits.Add(new TAuditHit(relative, 0, name, "Empty", "", "the twin holds no source file"));
                }

                hits.AddRange(TAuditDomainRead(repoRoot, column, held, name));
            }
        }

        foreach (string settings in projects.Values.Concat(TAuditPlatformFile.TAuditImportRead(repoRoot)))
        {
            if (TAuditPlatformProject.TAuditValueRead(XDocument.Load(settings), "ImplicitUsings")
                .Any(value => TAuditPlatformProject.TAuditTrueCheck(value)
                    || value.Equals("enable", StringComparison.OrdinalIgnoreCase)))
            {
                hits.Add(new TAuditHit(
                    TAuditPlatformFile.TAuditRelativeRead(repoRoot, settings), 0,
                    Path.GetFileNameWithoutExtension(settings), "Implicit", "",
                    "turns implicit usings on, so the binder would miss their global usings"));
            }
        }

        return hits;
    }

    private static string TAuditRowRead(TAuditHit hit)
    {
        string where = hit.TAuditHitPath.Length == 0 ? ""
            : hit.TAuditHitLine > 0 ? $" {hit.TAuditHitPath}:{hit.TAuditHitLine}" : $" {hit.TAuditHitPath}";
        return $"{hit.TAuditHitRing}{where} - {hit.TAuditHitName}";
    }

    private static IEnumerable<TAuditHit> TAuditDomainRead(
        string repoRoot, string column, IEnumerable<string> held, string name)
    {
        if (TAuditPlatformSetting.TAuditPlatformShell.Contains(column, StringComparer.OrdinalIgnoreCase))
        {
            yield break;
        }

        string portable = TAuditPlatformSetting.TAuditPlatformRoot + column + "/";
        HashSet<string> files = new(held.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
        foreach (SyntaxTree tree in TAuditBinder.TAuditTrees.Where(tree =>
                     files.Contains(Path.GetFullPath(tree.FilePath))))
        {
            foreach (TypeDeclarationSyntax declared in tree.GetRoot()
                         .DescendantNodes().OfType<TypeDeclarationSyntax>()
                         .Where(type => type.Parent is not TypeDeclarationSyntax))
            {
                if (TAuditBinderSymbol.TAuditSymbolRead(declared) is not INamedTypeSymbol type)
                {
                    continue;
                }

                IEnumerable<INamedTypeSymbol> bases = type.AllInterfaces;
                for (INamedTypeSymbol? current = type.BaseType; current is not null; current = current.BaseType)
                {
                    bases = bases.Append(current);
                }

                if (!bases.Any(held => TAuditBinderSymbol.TAuditSourceRead(held.OriginalDefinition)
                        ?.StartsWith(portable, StringComparison.OrdinalIgnoreCase) == true))
                {
                    int line = declared.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    yield return new TAuditHit(
                        TAuditPlatformFile.TAuditRelativeRead(repoRoot, tree.FilePath), line, name, "Domain", type.Name,
                        $"{type.Name} implements no port of {column}, so it holds more than a Windows adaptation");
                }
            }
        }
    }

    private static bool TAuditWindowsCheck(
        string project, IReadOnlyDictionary<string, string> table, IReadOnlyDictionary<string, string> projects)
    {
        string column = table.GetValueOrDefault(project, project);
        if (column.Length > 0 && !column.Equals(project, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return projects.TryGetValue(project, out string? path)
            && TAuditPlatformProject.TAuditFrameworkRead(XDocument.Load(path))
                .Any(framework => framework.Contains("-windows", StringComparison.OrdinalIgnoreCase));
    }
}
