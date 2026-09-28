using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditPlatform
{
    private const string TAuditProjectExtension = ".csproj";

    private static readonly string[] TAuditImportNames = ["Directory.Build.props", "Directory.Build.targets"];

    private static readonly string[] TAuditConfigNames = [".editorconfig", ".globalconfig"];

    private static readonly SortedDictionary<string, string> TAuditUnreadable = new(StringComparer.Ordinal);

    private static readonly Lazy<IReadOnlyList<TAuditHit>> TAuditPlatformHits = new(TAuditPlatformRead);

    private static readonly Regex TAuditPragmaPattern = new(
        @"^\s*#\s*pragma\s+warning\s+disable\b", RegexOptions.Compiled);

    private static readonly Regex TAuditBarePattern = new(@"disable\s*$", RegexOptions.Compiled);

    private static readonly Regex TAuditSectionPattern = new(@"^\s*\[(?<glob>.+)\]\s*$", RegexOptions.Compiled);

    private static readonly Regex TAuditPairPattern = new(
        @"^\s*(?<key>[^=#;]+?)\s*=\s*(?<value>[^#;]*?)\s*$", RegexOptions.Compiled);

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
        _tAuditOutput.WriteLine($"AUDITPLATFORM Unreadable files: {TAuditUnreadable.Count}");
        IEnumerable<string> unreadable = TAuditUnreadable.Select(pair => $"    {pair.Key}: {pair.Value}");
        over.AddRange(unreadable.Any() ? unreadable.Prepend($"  Unreadable files: {TAuditUnreadable.Count}") : []);

        Assert.True(held && TAuditUnreadable.Count == 0, TAuditConvention.TAuditReportFormat(
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
                .Select(path => TAuditRelativeRead(repoRoot, path)).Order(StringComparer.Ordinal)))
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
            TAuditRelativeRead(repoRoot, pair.Value), 0, pair.Key, "Unmapped", "",
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
            string relative = TAuditRelativeRead(repoRoot, path);
            XDocument document = XDocument.Load(path);
            string[] frameworks = TAuditFrameworkRead(document);
            string[] references = TAuditIncludeRead(document, "ProjectReference")
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
                             .OrderBy(source => TAuditRelativeRead(repoRoot, source), StringComparer.OrdinalIgnoreCase)
                             .ThenBy(source => TAuditRelativeRead(repoRoot, source), StringComparer.Ordinal)
                             .Where(source => !TAuditAnalyzerCheck(repoRoot, path, source))
                             .Take(1))
                {
                    hits.Add(new TAuditHit(
                        TAuditRelativeRead(repoRoot, source), 0, name, "Analyzer", "",
                        $"{TAuditPlatformSetting.TAuditPlatformRule} is not an error here"));
                }

                hits.AddRange(TAuditSuppressRead(repoRoot, path, held, name));

                hits.AddRange(TAuditPlatformSetting.TAuditPlatformProperties
                    .Where(property => TAuditValueRead(document, property).Any(TAuditTrueCheck))
                    .Select(property => new TAuditHit(relative, 0, name, "Windows", property, $"enables {property}")));
                hits.AddRange(TAuditIncludeRead(document, "PackageReference")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(package => TAuditPlatformSetting.TAuditPlatformPackages
                        .Contains(package, StringComparer.OrdinalIgnoreCase))
                    .Select(package => new TAuditHit(
                        relative, 0, name, "Windows", package, $"references the Windows package {package}")));
                hits.AddRange(held.SelectMany(source => TAuditSourceScan(repoRoot, source, name)));
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

        foreach (string settings in projects.Values.Concat(TAuditImportRead(repoRoot)))
        {
            if (TAuditValueRead(XDocument.Load(settings), "ImplicitUsings")
                .Any(value => TAuditTrueCheck(value) || value.Equals("enable", StringComparison.OrdinalIgnoreCase)))
            {
                hits.Add(new TAuditHit(
                    TAuditRelativeRead(repoRoot, settings), 0, Path.GetFileNameWithoutExtension(settings), "Implicit",
                    "", "turns implicit usings on, so the binder would miss their global usings"));
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

    private static IEnumerable<string> TAuditImportRead(string repoRoot) => TAuditSource.TAuditFileRead(
        repoRoot, new TAuditScope([], TAuditImportNames.Select(name => "*" + name).ToArray(), [], [], [], []));

    private static IEnumerable<TAuditHit> TAuditSuppressRead(
        string repoRoot, string project, IEnumerable<string> held, string name)
    {
        string rule = TAuditPlatformSetting.TAuditPlatformRule;
        XDocument[] documents = TAuditChainRead(repoRoot, project, TAuditImportNames)
            .Append(project)
            .Select(path => XDocument.Load(path))
            .ToArray();
        string relative = TAuditRelativeRead(repoRoot, project);
        if (documents.Any(document => TAuditListCheck(document, "NoWarn", rule)))
        {
            yield return new TAuditHit(relative, 0, name, "Suppress", rule, $"NoWarn holds {rule}");
        }

        foreach ((string property, string value) in TAuditPlatformSetting.TAuditPlatformSilencers)
        {
            if (documents.Any(document => TAuditValueRead(document, property)
                    .Any(found => found.Equals(value, StringComparison.OrdinalIgnoreCase))))
            {
                yield return new TAuditHit(relative, 0, name, "Suppress", property, $"{property} is {value}");
            }
        }

        foreach (string source in held.Where(source => source.EndsWith(".cs", StringComparison.Ordinal)))
        {
            string[] lines = TAuditTextRead(source);
            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                bool pragma = TAuditPragmaPattern.IsMatch(line)
                              && (line.Contains(rule, StringComparison.Ordinal) || TAuditBarePattern.IsMatch(line));
                bool attribute = line.Contains("SuppressMessage", StringComparison.Ordinal)
                                 && line.Contains(rule, StringComparison.Ordinal);
                if (pragma || attribute)
                {
                    yield return new TAuditHit(
                        TAuditRelativeRead(repoRoot, source), index + 1, name, "Suppress", rule, line.Trim());
                }
            }
        }
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
                if (TAuditBinder.TAuditSymbolRead(declared) is not INamedTypeSymbol type)
                {
                    continue;
                }

                IEnumerable<INamedTypeSymbol> bases = type.AllInterfaces;
                for (INamedTypeSymbol? current = type.BaseType; current is not null; current = current.BaseType)
                {
                    bases = bases.Append(current);
                }

                if (!bases.Any(held => TAuditBinder.TAuditSourceRead(held.OriginalDefinition)
                        ?.StartsWith(portable, StringComparison.OrdinalIgnoreCase) == true))
                {
                    int line = declared.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    yield return new TAuditHit(TAuditRelativeRead(repoRoot, tree.FilePath), line, name, "Domain",
                        type.Name,
                        $"{type.Name} implements no port of {column}, so it holds more than a Windows adaptation");
                }
            }
        }
    }

    private static IEnumerable<TAuditHit> TAuditSourceScan(string repoRoot, string source, string project)
    {
        Regex[] patterns = TAuditPlatformSetting.TAuditPlatformPatterns.Select(pattern => new Regex(pattern)).ToArray();
        string relative = TAuditRelativeRead(repoRoot, source);
        string[] lines = TAuditTextRead(source);
        for (int index = 0; index < lines.Length; index++)
        {
            Match? match = patterns.Select(pattern => pattern.Match(lines[index]))
                .FirstOrDefault(found => found.Success);
            if (match is not null)
            {
                yield return new TAuditHit(
                    relative, index + 1, project, "Windows", match.Value, $"names {match.Value}");
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
            && TAuditFrameworkRead(XDocument.Load(path))
                .Any(framework => framework.Contains("-windows", StringComparison.OrdinalIgnoreCase));
    }

    private static bool TAuditAnalyzerCheck(string repoRoot, string project, string source)
    {
        string rule = TAuditPlatformSetting.TAuditPlatformRule;
        List<string> configs = TAuditChainRead(repoRoot, source, TAuditConfigNames).ToList();
        List<string> editors = configs.Where(config => config.EndsWith(TAuditConfigNames[0], StringComparison.Ordinal))
            .ToList();
        int top = editors.FindIndex(config => TAuditTextRead(config).Any(line =>
            TAuditPairPattern.Match(line) is { Success: true } pair
            && pair.Groups["key"].Value.Equals("root", StringComparison.OrdinalIgnoreCase)
            && pair.Groups["value"].Value.Equals("true", StringComparison.OrdinalIgnoreCase)));
        IEnumerable<string> ordered = configs
            .Where(config => config.EndsWith(TAuditConfigNames[1], StringComparison.Ordinal))
            .Reverse()
            .Concat(editors.Take(top < 0 ? editors.Count : top + 1).Reverse());
        string? level = null;
        foreach (string config in ordered)
        {
            level = TAuditLevelRead(config, source, $"dotnet_diagnostic.{rule}.severity") ?? level;
        }

        XDocument[] documents = TAuditChainRead(repoRoot, project, TAuditImportNames)
            .Append(project)
            .Select(path => XDocument.Load(path))
            .ToArray();
        bool listed = documents.Any(document => TAuditListCheck(document, "WarningsAsErrors", rule));
        bool spared = documents.Any(document => TAuditListCheck(document, "WarningsNotAsErrors", rule));
        bool every = documents.Any(document => TAuditValueRead(document, "TreatWarningsAsErrors").Any(TAuditTrueCheck));
        bool escalated = listed || (every && !spared);
        return level switch
        {
            null => escalated,
            _ when level.Equals("error", StringComparison.OrdinalIgnoreCase) => true,
            _ when level.Equals("warning", StringComparison.OrdinalIgnoreCase) => escalated,
            _ => false,
        };
    }

    private static bool TAuditTrueCheck(string value) => value.Equals("true", StringComparison.OrdinalIgnoreCase);

    private static string[] TAuditTextRead(string path)
    {
        try
        {
            return File.ReadAllLines(path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            TAuditUnreadable[path] = error.Message;
            return [];
        }
    }

    private static string? TAuditLevelRead(string config, string source, string key)
    {
        string relative = Path.GetRelativePath(Path.GetDirectoryName(config)!, source).Replace('\\', '/');
        bool applies = config.EndsWith(TAuditConfigNames[1], StringComparison.Ordinal);
        string? level = null;
        foreach (string line in TAuditTextRead(config))
        {
            Match section = TAuditSectionPattern.Match(line);
            if (section.Success)
            {
                applies = TAuditGlobCheck(section.Groups["glob"].Value, relative);
                continue;
            }

            Match pair = TAuditPairPattern.Match(line);
            if (applies && pair.Success && pair.Groups["key"].Value.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                level = pair.Groups["value"].Value;
            }
        }

        return level;
    }

    private static bool TAuditGlobCheck(string glob, string relative)
    {
        string pattern = glob.Contains('/') ? glob.TrimStart('/') : "**/" + glob;
        System.Text.StringBuilder text = new("^");
        for (int index = 0; index < pattern.Length; index++)
        {
            char next = pattern[index];
            if (next == '*' && index + 1 < pattern.Length && pattern[index + 1] == '*')
            {
                text.Append(index + 2 < pattern.Length && pattern[index + 2] == '/' ? "(?:.*/)?" : ".*");
                index += index + 2 < pattern.Length && pattern[index + 2] == '/' ? 2 : 1;
            }
            else
            {
                text.Append(next switch
                {
                    '*' => "[^/]*",
                    '?' => "[^/]",
                    '{' => "(?:",
                    '}' => ")",
                    ',' => "|",
                    '[' or ']' => next.ToString(),
                    _ => Regex.Escape(next.ToString())
                });
            }
        }

        return Regex.IsMatch(relative, text.Append('$').ToString(), RegexOptions.IgnoreCase);
    }

    private static bool TAuditListCheck(XDocument document, string element, string rule) =>
        TAuditValueRead(document, element)
            .SelectMany(value => Regex.Split(value, @"[;,\s]+"))
            .Contains(rule, StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<string> TAuditChainRead(string repoRoot, string project, string[] names)
    {
        string root = Path.TrimEndingDirectorySeparator(repoRoot);
        DirectoryInfo? folder = new FileInfo(project).Directory;
        while (folder is not null)
        {
            foreach (string name in names)
            {
                string candidate = Path.Combine(folder.FullName, name);
                if (File.Exists(candidate))
                {
                    yield return candidate;
                }
            }

            string current = Path.TrimEndingDirectorySeparator(folder.FullName);
            if (string.Equals(current, root, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            folder = folder.Parent;
        }
    }

    private static string[] TAuditFrameworkRead(XDocument document) => TAuditValueRead(document, "TargetFramework")
        .Concat(TAuditValueRead(document, "TargetFrameworks"))
        .SelectMany(value => value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .ToArray();

    private static IEnumerable<string> TAuditValueRead(XDocument document, string element) => document.Descendants()
        .Where(node => node.Name.LocalName.Equals(element, StringComparison.OrdinalIgnoreCase))
        .Select(node => node.Value.Trim());

    private static IEnumerable<string> TAuditIncludeRead(XDocument document, string element) => document.Descendants()
        .Where(node => node.Name.LocalName.Equals(element, StringComparison.OrdinalIgnoreCase))
        .Select(node => (string?)node.Attribute("Include") ?? "")
        .Where(include => include.Length > 0);

    private static string TAuditRelativeRead(string repoRoot, string path) =>
        Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
}
