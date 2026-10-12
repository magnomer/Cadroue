<#
.SYNOPSIS
    Record the object audit for every version of the repository.
.DESCRIPTION
    Lists version-labelled commits, whose first message line begins with a
    three-part version such as 0.17.7607. The local git history is read first;
    when it is unavailable, the GitHub REST API supplies the list instead.

    Each version without a record is fetched into the system temp folder as a
    ZIP, measured, and the temp folder is deleted before the next version. The
    ZIP comes from git archive when the commit exists locally, and from GitHub
    otherwise.

    Measuring follows AuditObject: every partial type is merged and measured
    for Parts, Lines, Members, Mutable, Outgoing, Incoming, Shared, Crossings,
    Glued, Fused and Density. Each type keeps every flag it hits, worst first:
    Hydra, Kraken, Spider, Chameleon, Octopus, Centipede and Serpent. Its
    verdict is its first flag, else Colony for several parts and Hermit for
    one. A hub is a state slot reached from thresholds.hub.parts or more parts;
    it is counted per slot and never changes a verdict. The
    sources and thresholds are those of the current AuditObject.json, so every
    version is graded by one rule set. Its ceilings and parts ledger are copied
    into the page data and grade nothing.

    Known limit: no version is built. The sources of the version bind into one
    Roslyn compilation against the shared frameworks in packs, without the
    generated code or the package references of a build, and compile errors
    are tolerated. A name that binds only as a candidate still counts, as it
    does in AuditObject. A name that reaches the codebase only through
    generated code or a missing package binds to nothing, so Outgoing and Incoming
    of old versions are best-effort and may fall below what AuditObject would
    report on a built checkout of the same commit.

    Each version records the verdict counts, the flag counts with the hub
    count, and every type with a flag, several parts or a hub, with
    its flags and metrics. Each version keeps its record in a file of its own,
    {version}.json in the records folder named in AuditObjectHistory.json,
    written once that version is measured. A record measured under other
    sources or thresholds of AuditObject.json, other packs, or another
    measuring program is measured again, and so is a version whose commit
    changed.

    Lineage links one type to another where git renamed a source file between
    two consecutive versions and the project folder or the file stem changed:
    the listed type the old stem names in the old project, listed before that
    version, links to the listed type the new stem names in the new project,
    listed from that version on. A stem names the longest listed type name
    that begins it and ends at its end or before a capital, so PCorpusBrowse
    names PCorpus. The renames come from
    AuditHistory.lineage.ps1 and stay in the lineage folder named in
    AuditObjectHistory.json, apart from the measure records, so no lineage
    change measures a version again. The page's Lineage checkbox gives every
    type linked this way, directly or through others, one shared color.

    All records, sorted by version, are then placed into the page template
    AuditObjectHistory.html, written as {prefix}{version}.html into the report
    folder named in AuditObjectHistory.json, and opened in the default browser. The page charts every flag count and
    every verdict count over the versions. It is self-contained and works
    offline. Page and progress lines number the terms as AuditObject does,
    (1) Hydra to (7) Serpent, (8) Hub and (9) Colony, and Hermit stays plain.

    Set GITHUB_TOKEN for authenticated access or a higher API rate limit.
.PARAMETER Rebuild
    Measure every version again, ignoring existing records.
.PARAMETER NoOpen
    Write the page without opening it.
.PARAMETER Help
    Display this help and exit. The alias -? is supported.
.EXAMPLE
    AuditObjectHistory
    Measure every version that has no record yet, then write and open the page.
.EXAMPLE
    AuditObjectHistory -Rebuild
    Measure every version again.
#>
#requires -Version 5.1
# AUDITOBJECTHISTORY - AUDIT GENERATION 22.
[CmdletBinding()]
param(
    [switch]$Rebuild,
    [switch]$NoOpen,
    [Alias('?')]
    [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Help) {
    Get-Help -Name $PSCommandPath -Detailed
    return
}

Write-Host 'AUDITOBJECTHISTORY - AUDIT GENERATION 22' -ForegroundColor Blue

[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding

$configPath = Join-Path $PSScriptRoot 'AuditObjectHistory.json'
$objectConfigPath = Join-Path $PSScriptRoot 'AuditObject.json'
$templatePath = Join-Path $PSScriptRoot 'AuditObjectHistory.html'
foreach ($path in @($configPath, $objectConfigPath, $templatePath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "A required file is missing: $path" }
}
$config = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
$objectConfig = Get-Content -LiteralPath $objectConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json

$sourceRoots = @(@($objectConfig.sources.roots) | ForEach-Object { ([string]$_).Trim().Replace('\', '/').Trim('/') } | Where-Object { $_.Length -gt 0 })
$packs = @(@($config.packs) | ForEach-Object { ([string]$_).Trim() } | Where-Object { $_.Length -gt 0 })
if ($sourceRoots.Count -eq 0 -or $packs.Count -eq 0) {
    throw 'AuditObject.json needs source roots and AuditObjectHistory.json needs packs.'
}

$repository = ([string]$config.repository).Trim()
$recordsPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ([string]$config.records)))
$lineagePath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ([string]$config.lineage.records)))
$framework = [string]$objectConfig.helper.framework
$projectName = [string]$objectConfig.project
$objectTerms = @('Hydra', 'Kraken', 'Spider', 'Chameleon', 'Octopus', 'Centipede', 'Serpent', 'Hub', 'Colony')

$script:HelperProject = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{TARGET_FRAMEWORK}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" />
  </ItemGroup>
</Project>
'@

$script:HelperProgram = @'
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length != 5)
{
    Console.Error.WriteLine("usage: <config> <zip> <strip> <output> <packs>");
    return 2;
}

JsonElement config = JsonDocument.Parse(File.ReadAllText(args[0])).RootElement;
string zipPath = args[1];
bool strip = args[2] == "1";
string outputPath = args[3];
string[] packs = args[4].Split(',', StringSplitOptions.RemoveEmptyEntries);

JsonElement sources = config.GetProperty("sources");
string[] roots = List(sources, "roots").Select(root => root.Replace('\\', '/').Trim('/') + "/").ToArray();
string[] extensions = List(sources, "extensions");
string[] segments = List(sources, "excludeSegments");
string[] suffixes = List(sources, "excludeSuffixes");
string[] prefixes = List(sources, "excludePrefixes");
JsonElement thresholds = config.GetProperty("thresholds");
JsonElement hydra = thresholds.GetProperty("hydra");
Limits limits = new(
    hydra.GetProperty("parts").GetInt32(),
    hydra.GetProperty("lines").GetInt32(),
    hydra.GetProperty("fused").GetDouble(),
    hydra.GetProperty("density").GetDouble(),
    thresholds.GetProperty("spider").GetProperty("outgoing").GetInt32(),
    thresholds.GetProperty("spider").GetProperty("incoming").GetInt32(),
    thresholds.GetProperty("chameleon").GetProperty("mutable").GetInt32(),
    thresholds.GetProperty("octopus").GetProperty("outgoing").GetInt32(),
    thresholds.GetProperty("centipede").GetProperty("members").GetInt32(),
    thresholds.GetProperty("serpent").GetProperty("lines").GetInt32(),
    thresholds.GetProperty("hub").GetProperty("parts").GetInt32());
string[] ladder = ["Hydra", "Kraken", "Spider", "Chameleon", "Octopus", "Centipede", "Serpent"];
string[] verdicts = [.. ladder, "Colony", "Hermit"];

CSharpParseOptions options = new(LanguageVersion.Preview, DocumentationMode.None, SourceCodeKind.Regular);
List<SyntaxTree> trees = [];
using (ZipArchive zip = ZipFile.OpenRead(zipPath))
{
    foreach (ZipArchiveEntry entry in zip.Entries)
    {
        string relative = entry.FullName.Replace('\\', '/');
        if (relative.EndsWith('/'))
        {
            continue;
        }

        if (strip)
        {
            int cut = relative.IndexOf('/');
            if (cut < 0)
            {
                continue;
            }

            relative = relative[(cut + 1)..];
        }

        if (!Chosen(relative))
        {
            continue;
        }

        using StreamReader reader = new(entry.Open(), Encoding.UTF8, true);
        trees.Add(CSharpSyntaxTree.ParseText(reader.ReadToEnd(), options, relative));
    }
}

if (trees.Count == 0)
{
    Console.Error.WriteLine("No source file matched the sources of the object audit.");
    return 2;
}

CSharpCompilation compilation = CSharpCompilation.Create(
    "AuditObjectHistory",
    trees,
    References(packs),
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));

Dictionary<INamedTypeSymbol, TypeRecord> types = new(SymbolEqualityComparer.Default);
Dictionary<SyntaxTree, SemanticModel> models = trees.ToDictionary(tree => tree, tree => compilation.GetSemanticModel(tree, true));

foreach (SyntaxTree tree in trees)
{
    SemanticModel model = models[tree];
    foreach (TypeDeclarationSyntax declaration in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
    {
        if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol typeSymbol)
        {
            continue;
        }

        if (!types.TryGetValue(typeSymbol, out TypeRecord? type))
        {
            type = new TypeRecord(typeSymbol);
            types.Add(typeSymbol, type);
        }

        FileLinePositionSpan span = declaration.GetLocation().GetLineSpan();
        PartRecord part = new(span.EndLinePosition.Line - span.StartLinePosition.Line + 1);
        type.Parts.Add(part);
        foreach (MemberDeclarationSyntax member in declaration.Members)
        {
            if (member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
            {
                continue;
            }

            foreach ((ISymbol symbol, bool state, bool mutable) in DeclaredMembers(model, member))
            {
                if (!type.Members.ContainsKey(symbol))
                {
                    type.Members.Add(symbol, new MemberRecord(type.Members.Count, symbol, part, state, mutable));
                }
            }
        }
    }
}

foreach (SyntaxTree tree in trees)
{
    SemanticModel model = models[tree];
    foreach (TypeDeclarationSyntax declaration in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
    {
        if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol typeSymbol
            || !types.TryGetValue(typeSymbol, out TypeRecord? type))
        {
            continue;
        }

        foreach (MemberDeclarationSyntax member in declaration.Members)
        {
            if (member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
            {
                continue;
            }

            MemberRecord[] owners = DeclaredMembers(model, member)
                .Select(pair => type.Members.TryGetValue(pair.Symbol, out MemberRecord? found) ? found : null)
                .Where(found => found is not null)
                .Select(found => found!)
                .ToArray();
            if (owners.Length == 0)
            {
                continue;
            }

            foreach (SimpleNameSyntax name in member.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                SymbolInfo info = model.GetSymbolInfo(name);
                ISymbol? bound = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
                if (bound is IMethodSymbol { AssociatedSymbol: not null } accessor)
                {
                    bound = accessor.AssociatedSymbol;
                }

                if (bound is not (IMethodSymbol or IFieldSymbol or IPropertySymbol or IEventSymbol))
                {
                    continue;
                }

                if (!type.Members.TryGetValue(bound.OriginalDefinition, out MemberRecord? target))
                {
                    continue;
                }

                foreach (MemberRecord owner in owners.Where(owner => !ReferenceEquals(owner, target)))
                {
                    owner.Uses.Add(target);
                    target.UsedBy.Add(owner);
                }
            }
        }
    }
}

HashSet<INamedTypeSymbol> codebase = new(SymbolEqualityComparer.Default);
foreach (SyntaxTree tree in trees)
{
    SemanticModel model = models[tree];
    foreach (SyntaxNode node in tree.GetRoot().DescendantNodes().Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax))
    {
        if (model.GetDeclaredSymbol(node) is INamedTypeSymbol declared)
        {
            codebase.Add(declared);
        }
    }
}

Dictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>> uses = new(SymbolEqualityComparer.Default);
foreach (SyntaxTree tree in trees)
{
    SemanticModel model = models[tree];
    foreach (SyntaxNode declaration in tree.GetRoot().DescendantNodes().Where(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax))
    {
        if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol user)
        {
            continue;
        }

        if (!uses.TryGetValue(user, out HashSet<INamedTypeSymbol>? used))
        {
            used = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            uses.Add(user, used);
        }

        IEnumerable<SimpleNameSyntax> names = declaration
            .DescendantNodes(node => node == declaration || node is not (BaseTypeDeclarationSyntax or DelegateDeclarationSyntax))
            .OfType<SimpleNameSyntax>();
        foreach (SimpleNameSyntax name in names)
        {
            SymbolInfo info = model.GetSymbolInfo(name);
            used.UnionWith(TypesOf(info.Symbol ?? info.CandidateSymbols.FirstOrDefault()).Where(target =>
                codebase.Contains(target)
                && !SymbolEqualityComparer.Default.Equals(target, user)
                && !Nested(user, target)));
        }
    }
}

Dictionary<INamedTypeSymbol, int> incoming = new(SymbolEqualityComparer.Default);
foreach (INamedTypeSymbol used in uses.Values.SelectMany(used => used))
{
    incoming[used] = incoming.GetValueOrDefault(used) + 1;
}

List<TypeSummary> summaries = types.Values
    .Where(type => type.Members.Count > 0)
    .Select(type => Summarize(
        type,
        limits,
        uses.GetValueOrDefault(type.Symbol)?.Count ?? 0,
        incoming.GetValueOrDefault(type.Symbol)))
    .OrderByDescending(summary => summary.Lines)
    .ThenBy(summary => summary.Name, StringComparer.Ordinal)
    .ToList();

using (FileStream stream = File.Create(outputPath))
using (Utf8JsonWriter writer = new(stream))
{
    writer.WriteStartObject();
    writer.WriteNumber("files", trees.Count);
    writer.WriteNumber("types", summaries.Count);
    writer.WriteStartObject("verdicts");
    foreach (string verdict in verdicts)
    {
        writer.WriteNumber(verdict.ToLowerInvariant(), summaries.Count(summary => summary.Verdict == verdict));
    }

    writer.WriteEndObject();
    writer.WriteStartObject("flags");
    foreach (string flag in ladder)
    {
        writer.WriteNumber(flag.ToLowerInvariant(), summaries.Count(summary => summary.Flags.Contains(flag)));
    }

    writer.WriteNumber("hub", summaries.Sum(summary => summary.Shared));
    writer.WriteEndObject();
    writer.WriteStartArray("list");
    foreach (TypeSummary summary in summaries.Where(summary => summary.Verdict != "Hermit" || summary.Shared > 0))
    {
        writer.WriteStartObject();
        writer.WriteString("name", summary.Name);
        writer.WriteString("verdict", summary.Verdict.ToLowerInvariant());
        writer.WriteStartArray("flags");
        foreach (string flag in summary.Flags)
        {
            writer.WriteStringValue(flag.ToLowerInvariant());
        }

        writer.WriteEndArray();
        writer.WriteNumber("parts", summary.Parts);
        writer.WriteNumber("lines", summary.Lines);
        writer.WriteNumber("members", summary.Members);
        writer.WriteNumber("mutable", summary.Mutable);
        writer.WriteNumber("outgoing", summary.Outgoing);
        writer.WriteNumber("incoming", summary.Incoming);
        writer.WriteNumber("shared", summary.Shared);
        writer.WriteNumber("crossings", summary.Crossings);
        writer.WriteNumber("glued", Math.Round(summary.Glued, 2));
        writer.WriteNumber("fused", Math.Round(summary.Fused, 2));
        writer.WriteNumber("density", Math.Round(summary.Density, 2));
        writer.WriteEndObject();
    }

    writer.WriteEndArray();
    writer.WriteEndObject();
}

return 0;

bool Chosen(string relative)
{
    if (!roots.Any(root => relative.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        || !extensions.Any(extension => relative.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
    {
        return false;
    }

    string[] pieces = relative.Split('/');
    string file = pieces[^1];
    return !pieces.Any(piece => segments.Contains(piece, StringComparer.OrdinalIgnoreCase))
        && !suffixes.Any(suffix => file.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        && !prefixes.Any(prefix => file.StartsWith(prefix, StringComparison.Ordinal));
}

static string[] List(JsonElement node, string key) =>
    node.GetProperty(key).EnumerateArray().Select(item => item.GetString()!).ToArray();

static List<MetadataReference> References(string[] packs)
{
    Dictionary<string, (Version Version, string Path)> chosen = new(StringComparer.OrdinalIgnoreCase);
    string runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
    string shared = Path.GetDirectoryName(Path.GetDirectoryName(runtime)!)!;
    foreach (string pack in packs)
    {
        string folder = Path.Combine(shared, pack, Path.GetFileName(runtime));
        if (!Directory.Exists(folder))
        {
            Console.Error.WriteLine($"The shared framework '{pack}' is not installed at {folder}, so it is left out.");
            continue;
        }

        foreach (string path in Directory.EnumerateFiles(folder, "*.dll"))
        {
            Version version;
            try
            {
                version = AssemblyName.GetAssemblyName(path).Version ?? new Version();
            }
            catch (BadImageFormatException)
            {
                continue;
            }

            string name = Path.GetFileNameWithoutExtension(path);
            if (!chosen.TryGetValue(name, out (Version Version, string Path) known) || known.Version < version)
            {
                chosen[name] = (version, path);
            }
        }
    }

    return chosen.Values.Select(entry => (MetadataReference)MetadataReference.CreateFromFile(entry.Path)).ToList();
}

static IEnumerable<(ISymbol Symbol, bool State, bool Mutable)> DeclaredMembers(SemanticModel model, MemberDeclarationSyntax member)
{
    switch (member)
    {
        case FieldDeclarationSyntax field:
            foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
            {
                if (model.GetDeclaredSymbol(variable) is IFieldSymbol fieldSymbol)
                {
                    yield return (fieldSymbol, !fieldSymbol.IsConst, !fieldSymbol.IsConst && !fieldSymbol.IsReadOnly);
                }
            }
            break;
        case EventFieldDeclarationSyntax eventField:
            foreach (VariableDeclaratorSyntax variable in eventField.Declaration.Variables)
            {
                if (model.GetDeclaredSymbol(variable) is IEventSymbol eventSymbol)
                {
                    yield return (eventSymbol, true, false);
                }
            }
            break;
        case PropertyDeclarationSyntax property:
            if (model.GetDeclaredSymbol(property) is IPropertySymbol { PartialImplementationPart: null } propertySymbol)
            {
                bool auto = property.ExpressionBody is null
                    && property.AccessorList is not null
                    && property.AccessorList.Accessors.All(accessor => accessor.Body is null && accessor.ExpressionBody is null);
                bool backed = propertySymbol.ContainingType.GetMembers().OfType<IFieldSymbol>()
                    .Any(backing => SymbolEqualityComparer.Default.Equals(backing.AssociatedSymbol, propertySymbol));
                bool settable = propertySymbol.SetMethod is { IsInitOnly: false };
                yield return (propertySymbol.PartialDefinitionPart ?? propertySymbol, auto, backed && settable);
            }
            break;
        default:
            if (model.GetDeclaredSymbol(member) is ISymbol symbol and not IMethodSymbol { PartialImplementationPart: not null })
            {
                yield return (symbol is IMethodSymbol { PartialDefinitionPart: { } definition } ? definition : symbol, false, false);
            }
            break;
    }
}

static IEnumerable<INamedTypeSymbol> TypesOf(ISymbol? symbol) => symbol switch
{
    null => [],
    IAliasSymbol alias => TypesOf(alias.Target),
    IArrayTypeSymbol array => TypesOf(array.ElementType),
    IPointerTypeSymbol pointer => TypesOf(pointer.PointedAtType),
    INamedTypeSymbol named => (named.IsAnonymousType ? [] : new[] { named.OriginalDefinition })
        .Concat(named.TypeArguments.SelectMany(TypesOf)),
    ITypeSymbol or INamespaceSymbol or ILocalSymbol or IParameterSymbol => [],
    IRangeVariableSymbol or IDiscardSymbol or ILabelSymbol or IPreprocessingSymbol => [],
    _ => symbol.ContainingType is null ? [] : [symbol.ContainingType.OriginalDefinition],
};

static bool Nested(INamedTypeSymbol first, INamedTypeSymbol second)
{
    for (INamedTypeSymbol? outer = first.ContainingType; outer is not null; outer = outer.ContainingType)
    {
        if (SymbolEqualityComparer.Default.Equals(outer.OriginalDefinition, second))
        {
            return true;
        }
    }

    for (INamedTypeSymbol? outer = second.ContainingType; outer is not null; outer = outer.ContainingType)
    {
        if (SymbolEqualityComparer.Default.Equals(outer.OriginalDefinition, first))
        {
            return true;
        }
    }

    return false;
}

static TypeSummary Summarize(TypeRecord type, Limits limits, int outgoing, int incoming)
{
    MemberRecord[] members = type.Members.Values.ToArray();
    int crossings = members.Sum(member => member.Uses.Count(target => !ReferenceEquals(member.Part, target.Part)));
    HashSet<MemberRecord> hubs = [];
    foreach (MemberRecord member in members.Where(member => member.IsState))
    {
        HashSet<PartRecord> touching = [member.Part];
        touching.UnionWith(member.UsedBy.Select(user => user.Part));
        if (touching.Count >= limits.HubParts)
        {
            hubs.Add(member);
        }
    }

    int lines = type.Parts.Sum(part => part.Lines);
    int parts = type.Parts.Count;
    double glued = Glued(members, parts, []);
    double fused = Glued(members, parts, hubs);
    double density = members.Length == 0 ? 0 : crossings / (double)members.Length;
    int memberCount = members.Length;
    int mutable = members.Count(member => member.IsMutable);
    bool serpent = lines >= limits.SerpentLines;
    bool centipede = memberCount >= limits.CentipedeMembers;
    bool octopus = outgoing >= limits.OctopusOutgoing;
    bool spider = outgoing >= limits.SpiderOutgoing && incoming >= limits.SpiderIncoming;
    List<string> flags = [];
    if (parts >= limits.HydraParts
        && lines >= limits.HydraLines
        && (fused >= limits.HydraFused || density >= limits.HydraDensity))
    {
        flags.Add("Hydra");
    }

    if ((serpent || centipede) && (octopus || spider))
    {
        flags.Add("Kraken");
    }

    if (spider)
    {
        flags.Add("Spider");
    }

    if (mutable >= limits.ChameleonMutable)
    {
        flags.Add("Chameleon");
    }

    if (octopus)
    {
        flags.Add("Octopus");
    }

    if (centipede)
    {
        flags.Add("Centipede");
    }

    if (serpent)
    {
        flags.Add("Serpent");
    }

    return new TypeSummary(
        type.Symbol.ToDisplayString(),
        parts,
        lines,
        memberCount,
        mutable,
        outgoing,
        incoming,
        hubs.Count,
        crossings,
        glued,
        fused,
        density,
        flags,
        flags.Count > 0 ? flags[0] : parts > 1 ? "Colony" : "Hermit");
}

static double Glued(MemberRecord[] members, int partCount, HashSet<MemberRecord> excluded)
{
    if (partCount == 0 || members.Length == 0)
    {
        return 0;
    }

    int[] parent = Enumerable.Range(0, members.Length).ToArray();
    int Find(int index)
    {
        while (parent[index] != index)
        {
            parent[index] = parent[parent[index]];
            index = parent[index];
        }
        return index;
    }

    foreach (MemberRecord member in members.Where(member => !excluded.Contains(member)))
    {
        foreach (MemberRecord target in member.Uses.Where(target => !excluded.Contains(target)))
        {
            parent[Find(member.Index)] = Find(target.Index);
        }
    }

    int widest = members
        .Where(member => !excluded.Contains(member))
        .GroupBy(member => Find(member.Index))
        .Select(group => group.Select(member => member.Part).Distinct().Count())
        .DefaultIfEmpty(0)
        .Max();
    return widest / (double)partCount;
}

internal sealed class TypeRecord(INamedTypeSymbol symbol)
{
    public INamedTypeSymbol Symbol { get; } = symbol;
    public List<PartRecord> Parts { get; } = [];
    public Dictionary<ISymbol, MemberRecord> Members { get; } = new(SymbolEqualityComparer.Default);
}

internal sealed class PartRecord(int lines)
{
    public int Lines { get; } = lines;
}

internal sealed class MemberRecord(int index, ISymbol symbol, PartRecord part, bool isState, bool isMutable)
{
    public int Index { get; } = index;
    public ISymbol Symbol { get; } = symbol;
    public PartRecord Part { get; } = part;
    public bool IsState { get; } = isState;
    public bool IsMutable { get; } = isMutable;
    public HashSet<MemberRecord> Uses { get; } = [];
    public HashSet<MemberRecord> UsedBy { get; } = [];
}

internal sealed record TypeSummary(
    string Name,
    int Parts,
    int Lines,
    int Members,
    int Mutable,
    int Outgoing,
    int Incoming,
    int Shared,
    int Crossings,
    double Glued,
    double Fused,
    double Density,
    List<string> Flags,
    string Verdict);

internal sealed record Limits(
    int HydraParts,
    int HydraLines,
    double HydraFused,
    double HydraDensity,
    int SpiderOutgoing,
    int SpiderIncoming,
    int ChameleonMutable,
    int OctopusOutgoing,
    int CentipedeMembers,
    int SerpentLines,
    int HubParts);
'@

$sha = [System.Security.Cryptography.SHA256]::Create()
try {
    $rulesText = @(
        'sources=' + ($objectConfig.sources | ConvertTo-Json -Compress -Depth 4)
        'thresholds=' + ($objectConfig.thresholds | ConvertTo-Json -Compress -Depth 4)
        'packs=' + ($packs -join ',')
        'program=' + $script:HelperProgram
        'record=3'
    ) -join "`n"
    $rules = -join ($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($rulesText)) | ForEach-Object { $_.ToString('x2') })
}
finally {
    $sha.Dispose()
}

$versionPattern = '^(?<version>(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*))(?:\s|$)'
$dateFormat = 'yyyy-MM-dd HH:mm:ss zzz'

$apiHeaders = @{
    Accept                 = 'application/vnd.github+json'
    'User-Agent'           = 'AuditObjectHistory.ps1'
    'X-GitHub-Api-Version' = '2022-11-28'
}
$downloadHeaders = @{ 'User-Agent' = 'AuditObjectHistory.ps1' }
if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_TOKEN)) {
    $apiHeaders['Authorization'] = "Bearer $($env:GITHUB_TOKEN.Trim())"
}

function Invoke-Git {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = @(& git @Arguments 2>$null)
        return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Lines = $output }
    }
    catch {
        return [pscustomobject]@{ ExitCode = -1; Lines = @() }
    }
    finally {
        $ErrorActionPreference = $previous
    }
}

function Get-LocalRepositoryRoot {
    if ($null -eq (Get-Command -Name git -CommandType Application -ErrorAction SilentlyContinue)) { return $null }
    $result = Invoke-Git -Arguments @('-C', $PSScriptRoot, 'rev-parse', '--show-toplevel')
    if ($result.ExitCode -ne 0 -or $result.Lines.Count -eq 0) { return $null }
    return ([string]$result.Lines[0]).Trim()
}

function Add-VersionCommit {
    param(
        [Parameter(Mandatory = $true)][hashtable]$Map,
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][string]$Sha,
        [Parameter(Mandatory = $true)][DateTimeOffset]$Date
    )

    $match = [System.Text.RegularExpressions.Regex]::Match($Title.Trim(), $versionPattern)
    if (-not $match.Success) { return }
    $version = $match.Groups['version'].Value
    if ($Map.ContainsKey($version)) {
        Write-Warning "More than one commit begins with version $version. Keeping the newest commit $($Map[$version].Sha)."
        return
    }
    $Map[$version] = [pscustomobject]@{ Version = $version; Sha = $Sha; Date = $Date }
}

function Get-LocalVersionCommit {
    param([Parameter(Mandatory = $true)][string]$Root)

    $result = Invoke-Git -Arguments @('-C', $Root, '-c', 'i18n.logOutputEncoding=UTF-8', 'log', '--format=%H%x1f%cI%x1f%s', 'HEAD')
    if ($result.ExitCode -ne 0) { return $null }
    $map = @{}
    foreach ($line in $result.Lines) {
        $parts = ([string]$line).Split([char]0x1f)
        if ($parts.Count -lt 3) { continue }
        $date = [DateTimeOffset]::Parse($parts[1], [System.Globalization.CultureInfo]::InvariantCulture)
        Add-VersionCommit -Map $map -Title $parts[2] -Sha $parts[0] -Date $date
    }
    return , $map
}

function Get-GitHubVersionCommit {
    $map = @{}
    $page = 1
    while ($true) {
        Write-Host "Reading GitHub commit page $page..."
        $response = Invoke-RestMethod -Uri "https://api.github.com/repos/$repository/commits?per_page=100&page=$page" -Method Get -Headers $apiHeaders -UseBasicParsing
        $commits = @($response)
        if ($commits.Count -eq 0) { break }
        foreach ($commit in $commits) {
            $title = ([string]$commit.commit.message -split '\r?\n', 2)[0]
            $dateValue = $commit.commit.committer.date
            $date = [DateTimeOffset]::MinValue
            if ($dateValue -is [datetime]) {
                $date = [DateTimeOffset]::new($dateValue)
            }
            elseif (-not [string]::IsNullOrWhiteSpace([string]$dateValue)) {
                $date = [DateTimeOffset]::Parse([string]$dateValue, [System.Globalization.CultureInfo]::InvariantCulture)
            }
            Add-VersionCommit -Map $map -Title $title -Sha ([string]$commit.sha) -Date $date
        }
        if ($commits.Count -lt 100) { break }
        $page++
    }
    return , $map
}

function Save-LocalArchive {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Sha,
        [Parameter(Mandatory = $true)][string]$ZipPath
    )

    if ((Invoke-Git -Arguments @('-C', $Root, 'cat-file', '-e', "$Sha^{commit}")).ExitCode -ne 0) { return $false }
    $tree = Invoke-Git -Arguments @('-C', $Root, '-c', 'core.quotePath=false', 'ls-tree', '--name-only', $Sha)
    if ($tree.ExitCode -ne 0) { return $false }
    $wanted = @($sourceRoots | ForEach-Object { ($_ -split '/')[0] })
    $present = @($tree.Lines | Where-Object { $name = [string]$_; @($wanted | Where-Object { $_ -ieq $name }).Count -gt 0 })
    if ($present.Count -eq 0) { return $false }
    $archive = Invoke-Git -Arguments (@('-C', $Root, 'archive', '--format=zip', "--output=$ZipPath", $Sha, '--') + $present)
    return $archive.ExitCode -eq 0 -and (Test-Path -LiteralPath $ZipPath -PathType Leaf)
}

function Save-GitHubArchive {
    param(
        [Parameter(Mandatory = $true)][string]$Sha,
        [Parameter(Mandatory = $true)][string]$ZipPath
    )

    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri "https://codeload.github.com/$repository/zip/$Sha" -Method Get -Headers $downloadHeaders -UseBasicParsing -OutFile $ZipPath
}

function Get-HelperBinary {
    param([Parameter(Mandatory = $true)][string]$DotnetPath)

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $sdkOutput = & $DotnetPath --version 2>&1
    $ErrorActionPreference = $previous
    if ($LASTEXITCODE -ne 0) {
        throw "The .NET SDK version could not be read.`n$($sdkOutput -join [Environment]::NewLine)"
    }

    $sdkVersion = ([string]($sdkOutput | Select-Object -First 1)).Trim()
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $seed = $script:HelperProject + "`n" + $script:HelperProgram + "`n" + $framework + "`n" + $sdkVersion
        $digest = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($seed))
    }
    finally {
        $sha.Dispose()
    }

    $hash = ([System.BitConverter]::ToString($digest) -replace '-', '').Substring(0, 16).ToLowerInvariant()
    $cacheParent = Join-Path ([System.IO.Path]::GetTempPath()) ($projectName + '-AuditObjectHistory')
    $cacheFolder = Join-Path $cacheParent $hash
    $binaryPath = Join-Path (Join-Path $cacheFolder 'bin') 'AuditObjectHistory.Helper.dll'
    if (Test-Path -LiteralPath $binaryPath -PathType Leaf) { return $binaryPath }

    Write-Host 'Compiling the object measurer once for this SDK...'
    if (Test-Path -LiteralPath $cacheParent) {
        Get-ChildItem -LiteralPath $cacheParent -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }
    $helperFolder = Join-Path $cacheFolder 'helper'
    [void][System.IO.Directory]::CreateDirectory($helperFolder)
    $encoding = [System.Text.UTF8Encoding]::new($false)
    $projectPath = Join-Path $helperFolder 'AuditObjectHistory.Helper.csproj'
    [System.IO.File]::WriteAllText($projectPath, $script:HelperProject.Replace('{TARGET_FRAMEWORK}', $framework), $encoding)
    [System.IO.File]::WriteAllText((Join-Path $helperFolder 'Program.cs'), $script:HelperProgram, $encoding)
    $ErrorActionPreference = 'Continue'
    $buildOutput = & $DotnetPath build $projectPath --configuration Release --nologo --verbosity quiet --output (Join-Path $cacheFolder 'bin') 2>&1
    $ErrorActionPreference = $previous
    if ($LASTEXITCODE -ne 0) {
        Remove-Item -LiteralPath $cacheFolder -Recurse -Force -ErrorAction SilentlyContinue
        throw "The object measurer could not be built.`n$($buildOutput -join [Environment]::NewLine)"
    }
    return $binaryPath
}

function ConvertTo-JsonText {
    param([AllowNull()][string]$Value)

    return '"' + $Value.Replace('\', '\\').Replace('"', '\"') + '"'
}

. (Join-Path $PSScriptRoot 'AuditHistory.lineage.ps1')

# Whether a type namespace belongs to a project folder name: the same name, or one nested in the other.
function Test-LineageNamespace {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Namespace,
        [Parameter(Mandatory = $true)][string]$Project
    )

    return $Namespace -ceq $Project -or $Namespace.StartsWith($Project + '.', [System.StringComparison]::Ordinal) -or
        $Project.StartsWith($Namespace + '.', [System.StringComparison]::Ordinal)
}

# Whether a project folder such as src/Cadroue.Core lies under a source root.
function Test-LineageSource {
    param([Parameter(Mandatory = $true)][string]$Project)

    foreach ($root in $sourceRoots) {
        if ($Project.StartsWith($root + '/', [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    return $false
}

# The listed types a file stem names: the longest listed type name that begins the stem and ends at its end or
# before a capital, in a namespace of the project, kept when Keep accepts it. PCorpusBrowse names PCorpus.
function Find-LineageType {
    param(
        [Parameter(Mandatory = $true)][hashtable]$ByShort,
        [Parameter(Mandatory = $true)][string]$Stem,
        [Parameter(Mandatory = $true)][string]$Project,
        [Parameter(Mandatory = $true)][scriptblock]$Keep
    )

    for ($length = $Stem.Length; $length -gt 0; $length--) {
        if ($length -lt $Stem.Length -and -not [char]::IsUpper($Stem[$length])) { continue }
        $short = $Stem.Substring(0, $length)
        if (-not $ByShort.ContainsKey($short)) { continue }
        $found = @($ByShort[$short] | Where-Object { (Test-LineageNamespace -Namespace $_.Namespace -Project $Project) -and (& $Keep $_) })
        if ($found.Count -gt 0) { return , $found }
    }
    return , @()
}

# The type links: a renamed source file whose project or stem changed links the listed type its old stem names in
# the old project, listed before that version, to the listed type its new stem names in the new project, listed
# from that version on.
function Get-LineageLink {
    param(
        [Parameter(Mandatory = $true)][System.Collections.Generic.List[object]]$Records,
        [Parameter(Mandatory = $true)][System.Collections.Generic.List[object]]$Lineage
    )

    $sorted = @($Records | Sort-Object -Property @{ Expression = { [version]$_.Version } })
    $position = @{}
    $types = @{}
    for ($i = 0; $i -lt $sorted.Count; $i++) {
        $position[$sorted[$i].Version] = $i
        foreach ($match in [System.Text.RegularExpressions.Regex]::Matches($sorted[$i].Measure, '"name":"(?<name>[^"]+)"')) {
            $name = $match.Groups['name'].Value
            if ($types.ContainsKey($name)) { $types[$name].Last = $i; continue }
            $cut = $name.LastIndexOf('.')
            $types[$name] = [pscustomobject]@{ Name = $name; Namespace = $(if ($cut -lt 0) { '' } else { $name.Substring(0, $cut) }); Short = $name.Substring($cut + 1); First = $i; Last = $i }
        }
    }
    $byShort = @{}
    foreach ($type in $types.Values) {
        if (-not $byShort.ContainsKey($type.Short)) { $byShort[$type.Short] = [System.Collections.Generic.List[object]]::new() }
        $byShort[$type.Short].Add($type)
    }

    $links = [System.Collections.Generic.List[object]]::new()
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($step in $Lineage) {
        if (-not $position.ContainsKey($step.Version)) { continue }
        $at = [int]$position[$step.Version]
        foreach ($move in $step.Moves) {
            if (-not $move.From.EndsWith('.cs', [System.StringComparison]::OrdinalIgnoreCase) -or -not $move.To.EndsWith('.cs', [System.StringComparison]::OrdinalIgnoreCase)) { continue }
            $fromProject = Get-LineageProject -Relative $move.From
            $toProject = Get-LineageProject -Relative $move.To
            if ($null -eq $fromProject -or $null -eq $toProject -or -not (Test-LineageSource -Project $fromProject) -or -not (Test-LineageSource -Project $toProject)) { continue }
            $fromStem = Get-LineageStem -Relative $move.From
            $toStem = Get-LineageStem -Relative $move.To
            $fromName = $fromProject.Substring($fromProject.IndexOf('/') + 1)
            $toName = $toProject.Substring($toProject.IndexOf('/') + 1)
            $olds = Find-LineageType -ByShort $byShort -Stem $fromStem -Project $fromName -Keep { param($type) $type.First -lt $at }
            if ($olds.Count -eq 0) { continue }
            $news = Find-LineageType -ByShort $byShort -Stem $toStem -Project $toName -Keep { param($type) $type.Last -ge $at }
            foreach ($old in $olds) {
                foreach ($new in $news) {
                    if ($new.Name -ceq $old.Name) { continue }
                    if (-not $seen.Add($old.Name + "`n" + $new.Name)) { continue }
                    $links.Add([pscustomobject]@{ Version = $step.Version; From = $old.Name; To = $new.Name })
                }
            }
        }
    }
    return , $links
}

# The page data: every record sorted by version, under the current rules, thresholds, ceilings and parts ledger.
function Get-PageRecordText {
    param(
        [Parameter(Mandatory = $true)][System.Collections.Generic.List[object]]$Records,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][System.Collections.Generic.List[object]]$Links
    )

    $sorted = @($Records | Sort-Object -Property @{ Expression = { [version]$_.Version } })
    $builder = [System.Text.StringBuilder]::new()
    [void]$builder.Append("{`n  `"rules`": " + (ConvertTo-JsonText $rules) + ",`n")
    [void]$builder.Append('  "lineage": { "links": [')
    for ($i = 0; $i -lt $Links.Count; $i++) {
        $link = $Links[$i]
        [void]$builder.Append($(if ($i -eq 0) { "`n" } else { ",`n" }))
        [void]$builder.Append('    { "version": ' + (ConvertTo-JsonText $link.Version) + ', "from": ' + (ConvertTo-JsonText $link.From) + ', "to": ' + (ConvertTo-JsonText $link.To) + ' }')
    }
    [void]$builder.Append($(if ($Links.Count -eq 0) { "] },`n" } else { "`n  ] },`n" }))
    [void]$builder.Append('  "thresholds": ' + ($objectConfig.thresholds | ConvertTo-Json -Compress -Depth 4) + ",`n")
    [void]$builder.Append('  "ceiling": ' + ($objectConfig.ceiling | ConvertTo-Json -Compress -Depth 4) + ",`n")
    [void]$builder.Append('  "parts": ' + ($objectConfig.parts | ConvertTo-Json -Compress -Depth 4) + ",`n")
    [void]$builder.Append('  "versions": [')
    for ($i = 0; $i -lt $sorted.Count; $i++) {
        $record = $sorted[$i]
        [void]$builder.Append($(if ($i -eq 0) { "`n" } else { ",`n" }))
        [void]$builder.Append('    { "version": ' + (ConvertTo-JsonText $record.Version) + ', "commit": ' + (ConvertTo-JsonText $record.Commit) +
            ', "date": ' + (ConvertTo-JsonText $record.Date) + ', "source": ' + (ConvertTo-JsonText $record.Source) + ', "measure": ' + $record.Measure + ' }')
    }
    [void]$builder.Append($(if ($sorted.Count -eq 0) { "]`n}`n" } else { "`n  ]`n}`n" }))
    return $builder.ToString()
}

# One version's record file, written under a pending name and moved into place, and left alone when unchanged.
# The measure stays on one line of its own, as the measurer wrote it, so reading takes it back verbatim.
function Save-Record {
    param([Parameter(Mandatory = $true)][object]$Record)

    $text = "{`n" +
        '  "version": ' + (ConvertTo-JsonText $Record.Version) + ",`n" +
        '  "commit": ' + (ConvertTo-JsonText $Record.Commit) + ",`n" +
        '  "date": ' + (ConvertTo-JsonText $Record.Date) + ",`n" +
        '  "source": ' + (ConvertTo-JsonText $Record.Source) + ",`n" +
        '  "rules": ' + (ConvertTo-JsonText $rules) + ",`n" +
        '  "measure": ' + $Record.Measure + "`n}`n"

    $encoding = [System.Text.UTF8Encoding]::new($false)
    $path = Join-Path $recordsPath ($Record.Version + '.json')
    if ([System.IO.File]::Exists($path) -and [System.IO.File]::ReadAllText($path, $encoding) -ceq $text) { return }
    [void][System.IO.Directory]::CreateDirectory($recordsPath)
    $pending = $path + '.pending'
    [System.IO.File]::WriteAllText($pending, $text, $encoding)
    if ([System.IO.File]::Exists($path)) {
        [System.IO.File]::Replace($pending, $path, [NullString]::Value)
    }
    else {
        [System.IO.File]::Move($pending, $path)
    }
}

# Every record file measured under the current rules; a missing folder holds no records.
function Read-Record {
    $records = [System.Collections.Generic.List[object]]::new()
    if ($Rebuild -or -not [System.IO.Directory]::Exists($recordsPath)) { return , $records }
    $stale = 0
    $measurePrefix = '  "measure": '
    $paths = @([System.IO.Directory]::GetFiles($recordsPath, '*.json') | Where-Object { $_.EndsWith('.json', [System.StringComparison]::OrdinalIgnoreCase) })
    foreach ($path in $paths) {
        $text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
        $measure = @($text.Split("`n") | Where-Object { $_.StartsWith($measurePrefix, [System.StringComparison]::Ordinal) })
        try {
            $item = $text | ConvertFrom-Json
        }
        catch {
            $item = $null
        }
        if ($null -eq $item -or $measure.Count -ne 1 -or [string]$item.rules -ne $rules) {
            $stale++
            continue
        }
        $records.Add([pscustomobject]@{
                Version = [string]$item.version
                Commit  = [string]$item.commit
                Date    = [string]$item.date
                Source  = [string]$item.source
                Measure = $measure[0].Substring($measurePrefix.Length).Trim()
            })
    }
    if ($stale -gt 0) {
        Write-Host "Records measured under other rules or unreadable: $stale. They are measured again."
    }
    return , $records
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    throw 'The .NET SDK is required, but dotnet was not found on PATH.'
}
$previousNoLogo = $env:DOTNET_NOLOGO
$previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

$failures = [System.Collections.Generic.List[string]]::new()
try {
    $binaryPath = Get-HelperBinary -DotnetPath $dotnet.Source

    $localRoot = Get-LocalRepositoryRoot
    $versionMap = $null
    if ($null -ne $localRoot) {
        $versionMap = Get-LocalVersionCommit -Root $localRoot
    }
    if ($null -eq $versionMap -or $versionMap.Count -eq 0) {
        Write-Host 'Local git history is unavailable, so GitHub supplies the version list.'
        $versionMap = Get-GitHubVersionCommit
    }
    if ($versionMap.Count -eq 0) {
        throw 'No version-labelled commits were found.'
    }

    $records = Read-Record
    $pendingVersions = @($versionMap.Values | Where-Object {
            $candidate = $_
            @($records | Where-Object { $_.Version -eq $candidate.Version -and $_.Commit -eq $candidate.Sha }).Count -eq 0
        } | Sort-Object -Property @{ Expression = { [version]$_.Version } })

    Write-Host "Versions: $($versionMap.Count). Recorded: $($versionMap.Count - $pendingVersions.Count). To measure: $($pendingVersions.Count)."

    $index = 0
    foreach ($versionCommit in $pendingVersions) {
        $index++
        $work = Join-Path ([System.IO.Path]::GetTempPath()) ('AuditObjectHistory-' + [Guid]::NewGuid().ToString('N'))
        [void][System.IO.Directory]::CreateDirectory($work)
        $zipPath = Join-Path $work 'source.zip'
        $measurePath = Join-Path $work 'measure.json'
        try {
            $source = 'local'
            $strip = '0'
            if ($null -eq $localRoot -or -not (Save-LocalArchive -Root $localRoot -Sha $versionCommit.Sha -ZipPath $zipPath)) {
                if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
                Save-GitHubArchive -Sha $versionCommit.Sha -ZipPath $zipPath
                $source = 'github'
                $strip = '1'
            }

            $previous = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            $helperOutput = & $dotnet.Source $binaryPath $objectConfigPath $zipPath $strip $measurePath ($packs -join ',') 2>&1
            $ErrorActionPreference = $previous
            if ($LASTEXITCODE -ne 0) { throw "The object measurer failed.`n$($helperOutput -join [Environment]::NewLine)" }

            $measureText = [System.IO.File]::ReadAllText($measurePath, [System.Text.Encoding]::UTF8).Trim()
            $measure = $measureText | ConvertFrom-Json
            for ($i = $records.Count - 1; $i -ge 0; $i--) {
                if ($records[$i].Version -eq $versionCommit.Version) { $records.RemoveAt($i) }
            }
            $record = [pscustomobject]@{
                Version = $versionCommit.Version
                Commit  = $versionCommit.Sha
                Date    = $versionCommit.Date.ToString($dateFormat, [System.Globalization.CultureInfo]::InvariantCulture)
                Source  = $source
                Measure = $measureText
            }
            Save-Record -Record $record
            $records.Add($record)
            $flags = $measure.flags
            $flagText = @($objectTerms | Where-Object { $null -ne $flags.PSObject.Properties[$_.ToLowerInvariant()] } | ForEach-Object {
                    '({0}) {1} {2}' -f ([array]::IndexOf($objectTerms, $_) + 1), $_, $flags.($_.ToLowerInvariant())
                }) -join ', '
            Write-Host ("[{0}/{1}] {2} {3} types; flags: {4} ({5})" -f $index, $pendingVersions.Count, $versionCommit.Version, $measure.types, $flagText, $source)
        }
        catch {
            $failures.Add("$($versionCommit.Version): $($_.Exception.Message)")
            Write-Warning "Could not measure $($versionCommit.Version): $($_.Exception.Message)"
        }
        finally {
            if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue }
        }
    }
}
finally {
    $env:DOTNET_NOLOGO = $previousNoLogo
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = $previousTelemetry
}

Write-Host "Records: $recordsPath"

$versionCount = $records.Count
if ($versionCount -eq 0) {
    throw "The records hold no versions: $recordsPath"
}
$lineage = Update-Lineage -Root $localRoot -Folder $lineagePath -VersionMap $versionMap
$links = Get-LineageLink -Records $records -Lineage $lineage
Write-Host "Lineage links: $($links.Count) between types."
$utf8 = [System.Text.UTF8Encoding]::new($false)
$recordText = Get-PageRecordText -Records $records -Links $links
$pageTitle = $repository.Split('/')[-1]
$template = [System.IO.File]::ReadAllText($templatePath, $utf8)
foreach ($marker in @('/*__DATA__*/', '__TITLE__', '__AUDITPAGE__')) {
    if (-not $template.Contains($marker)) { throw "The template lacks the marker $marker : $templatePath" }
}
$versionPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ([string]$config.report.versionFile)))
$version = [string](Get-Content -LiteralPath $versionPath -Raw -Encoding UTF8 | ConvertFrom-Json).([string]$config.report.versionKey)
if ([string]::IsNullOrWhiteSpace($version)) { throw "The version file lacks the key $($config.report.versionKey): $versionPath" }
$visualDirectory = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ([string]$config.report.directory)))
$visualPath = Join-Path $visualDirectory ([string]$config.report.prefix + $version + '.html')
$auditPage = 'AuditObject-' + $version + '.html'
$pageText = $template.Replace('__TITLE__', [System.Net.WebUtility]::HtmlEncode($pageTitle)).Replace('__AUDITPAGE__', $auditPage).Replace('/*__DATA__*/', $recordText.Trim().Replace('</', '<\/'))
[void][System.IO.Directory]::CreateDirectory($visualDirectory)
[System.IO.File]::WriteAllText($visualPath, $pageText, $utf8)
Write-Host "Page: $visualPath ($versionCount versions)"
if (-not $NoOpen) {
    Start-Process -FilePath $visualPath
}

if ($failures.Count -gt 0) {
    Write-Host "Failed versions: $($failures.Count)"
    exit 1
}
