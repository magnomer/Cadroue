<#
.SYNOPSIS
    Follow a value from the method it is injected into until nothing carries it on.
.DESCRIPTION
    Takes a value - text, a number, true or false, null, or a file - and an entry
    method. Every parameter of the entry that can take the value receives it. A
    parameter that cannot take it is a failure, reported and not explained.

    The code is read, never run. Binding uses the .NET SDK's own Roslyn on one
    compilation of every configured source, the generated markup code, the shared
    frameworks and the package assemblies of the latest build output. Nothing in
    the project is executed or modified.

    Inside a method the value is carried by locals, parameters, fields,
    properties, the receiver and the results of calls. Each move of the value is a
    hop, and every hop is logged with its kind:
      pass      the value itself moves on: an argument, an assignment, a receiver
      derived   a new value computed from it moves on
      store     it is written to a field or property
      read      a method reads the field or property that holds it
      return    a method hands it back to its caller
      control   it decides a branch; every call inside the branch is listed and
                not followed, because the value does not travel into it
    A known value prunes branches: false skips every branch that needs true, and
    text or a number skips cases and comparisons it cannot meet.

    Interfaces and overrides resolve to every implementation in the source. A call
    into code outside the source ends that path as Opaque. A path also ends as Drop
    when nothing carries the value further, Control when the call is only
    influenced, Depth at the hop limit, and Fail when the value is refused.

    The trace is a graph, written as Markdown. Each symbol has one section. Its
    header carries the id of the group it was first listed in; its children form
    the next group. A symbol met again points back to its group instead of
    repeating.

    Every project-specific value lives in TraceValue.json. This script carries none.
.PARAMETER Value
    The value to inject. true, false, null, a number, file:<path>, or any text.
.PARAMETER Entry
    The method that takes the value: Type.Method, or Method alone. A property name
    injects into its setter. Positional.
.PARAMETER Parameter
    The parameter of the entry that takes the value. Defaults to every parameter
    that can take it.
.PARAMETER Depth
    Maximum number of hops to walk. Defaults to 0, which walks until nothing new turns up.
.PARAMETER OutputPath
    Write the Markdown to this file instead of the console.
.PARAMETER Help
    Display this help and exit. The alias -? is supported.
.EXAMPLE
    tracevalue LCardClerkField.LCardValidate -Value false
    Follow false from every bool parameter of LCardClerkField.LCardValidate.
.EXAMPLE
    tracevalue LScriptVault.LScriptSave -Value 'file:D:\sample.png' -OutputPath trace.md
    Follow an image file from LScriptSave and write the trace to trace.md.
#>
#requires -Version 5.1
# TRACEVALUE - TRACE GENERATION 1.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the executed outcome changes.
# Generation 1: one Roslyn compilation of sources, generated code, frameworks and build packages;
# the value enters the entry parameters that take it; pass, derived, store, read, return and
# control hops are logged; known values prune branches and switch cases; interfaces and overrides
# resolve to source implementations; the graph is written as Markdown, one section per symbol.
# Every project-specific value lives in TraceValue.json, so this file is identical in every project at
# this generation.
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Entry,

    [AllowEmptyString()]
    [string]$Value,

    [string]$Parameter = '',

    [ValidateRange(0, [int]::MaxValue)]
    [int]$Depth = 0,

    [string]$OutputPath = '',

    [Alias('?')]
    [switch]$Help
)

if ($Help) {
    @'
NAME
    TraceValue.ps1

SYNOPSIS
    Follow a value from the method it is injected into until nothing carries it on.

SYNTAX
    tracevalue <entry> -Value <value> [-Parameter <name>] [-Depth <hops>] [-OutputPath <file>] [-Help]

OPTIONS
    <entry>
        Method that takes the value: Type.Method or Method. A property injects into its setter.

    -Value <value>
        true, false, null, a number, file:<path>, or any text.

    -Parameter <name>
        Parameter of the entry that takes the value. Defaults to every one that can.

    -Depth <hops>
        Maximum number of hops to walk. Defaults to 0, no limit.

    -OutputPath <file>
        Write the Markdown to this file instead of the console.

    -Help, -?
        Display this help and exit.

HOPS
    pass, derived, store, read, return, control.

ENDS
    Drop, Opaque, Control, Depth, Fail.

EXAMPLES
    tracevalue LCardClerkField.LCardValidate -Value false
        Follow false from every bool parameter of LCardClerkField.LCardValidate.

    tracevalue LScriptVault.LScriptSave -Value 'file:D:\sample.png' -OutputPath trace.md
        Follow an image file from LScriptSave and write the trace to trace.md.
'@ | Write-Host
    exit 0
}

Write-Host 'TRACEVALUE - TRACE GENERATION 1' -ForegroundColor Blue

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding
$script:PathSeparators = [char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)

function Get-OrdinalKey {
    # Sort-Object compares text by culture, which Windows PowerShell 5.1 and pwsh 7 order differently.
    # Uppercase hexadecimal UTF-16 code units compare alike under every culture, so this key sorts ordinally.
    param([string]$Text)
    return [System.BitConverter]::ToString([System.Text.Encoding]::BigEndianUnicode.GetBytes($Text)).Replace('-', '')
}

function Read-TracerConfig {
    param([string]$ConfigPath)

    if (-not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) {
        throw "The TraceValue configuration was not found: $ConfigPath"
    }

    try {
        $config = Get-Content -LiteralPath $ConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The TraceValue configuration is not valid JSON: $ConfigPath`n$($_.Exception.Message)"
    }

    $required = @(
        'generation', 'project', 'layer',
        'sources.roots', 'sources.extensions', 'sources.excludeSegments',
        'sources.excludeSuffixes', 'sources.excludePrefixes',
        'generated.roots', 'generated.configuration',
        'references.frameworks', 'references.build'
    )

    foreach ($key in $required) {
        $node = $config
        foreach ($segment in $key.Split('.')) {
            if ($null -eq $node -or -not ($node.PSObject.Properties.Name -contains $segment)) {
                throw "The TraceValue configuration has no key '$key': $ConfigPath"
            }
            $node = $node.$segment
        }
    }

    if ([int]$config.generation -ne 1) {
        throw "The TraceValue configuration is generation $($config.generation); this script is generation 1."
    }

    return $config
}

function Test-TracerExcluded {
    param([string]$RelativePath, $Config)

    $segments = $RelativePath -split '[\\/]'
    foreach ($segment in $segments) {
        if ($Config.sources.excludeSegments -contains $segment) {
            return $true
        }
    }

    $fileName = $segments[$segments.Length - 1]
    if (-not ($Config.sources.extensions -contains [System.IO.Path]::GetExtension($fileName))) {
        return $true
    }

    foreach ($suffix in $Config.sources.excludeSuffixes) {
        if ($fileName.EndsWith($suffix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    foreach ($prefix in $Config.sources.excludePrefixes) {
        if ($fileName.StartsWith($prefix, [System.StringComparison]::Ordinal)) {
            return $true
        }
    }

    return $false
}

function Get-TracerSources {
    param([string]$ProjectRoot, $Config)

    $git = Get-Command git -ErrorAction SilentlyContinue
    if ($null -eq $git) {
        throw 'Git is required to enumerate project source files, but git was not found on PATH.'
    }

    $lsArguments = @('-c', 'core.quotePath=false', '-C', $ProjectRoot,
        'ls-files', '--cached', '--others', '--exclude-standard', '--') + @($Config.sources.roots)
    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $gitOutput = & $git.Source @lsArguments 2>&1
    $ErrorActionPreference = $nativePreference
    if ($LASTEXITCODE -ne 0) {
        throw "Git could not enumerate source files.`n$($gitOutput -join [Environment]::NewLine)"
    }

    $rootPrefix = $ProjectRoot.TrimEnd($script:PathSeparators) + [System.IO.Path]::DirectorySeparatorChar
    $files = New-Object 'System.Collections.Generic.List[string]'
    foreach ($entry in $gitOutput) {
        $relativePath = ([string]$entry).Trim()
        if ([string]::IsNullOrWhiteSpace($relativePath) -or (Test-TracerExcluded -RelativePath $relativePath -Config $Config)) {
            continue
        }

        $fullPath = $rootPrefix + $relativePath.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
            $files.Add($fullPath)
        }
    }

    return @($files.ToArray() | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } } -Unique)
}

function Get-TracerLatest {
    param([string]$Folder)

    if (-not (Test-Path -LiteralPath $Folder -PathType Container)) {
        return $null
    }

    return Get-ChildItem -LiteralPath $Folder -Directory |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
}

function Get-TracerGenerated {
    param([string]$ProjectRoot, $Config)

    $files = New-Object 'System.Collections.Generic.List[string]'
    foreach ($root in $Config.generated.roots) {
        $folder = Join-Path (Join-Path (Join-Path $ProjectRoot $root) 'obj') $Config.generated.configuration
        $target = Get-TracerLatest -Folder $folder
        if ($null -eq $target) {
            continue
        }

        Get-ChildItem -LiteralPath $target.FullName -Filter '*.cs' -File -Recurse |
            Where-Object { $_.Name -notlike '*_wpftmp*' -and $_.Name -notlike '*.g.i.cs' } |
            ForEach-Object { $files.Add($_.FullName) }
    }

    return @($files.ToArray())
}

function Get-TracerReferences {
    param([string]$ProjectRoot, $Config)

    $folder = Join-Path (Join-Path (Join-Path $ProjectRoot $Config.references.build) 'bin') $Config.generated.configuration
    $target = Get-TracerLatest -Folder $folder
    if ($null -eq $target) {
        Write-Host "No build output under $folder; package types stay unbound." -ForegroundColor DarkYellow
        return @()
    }

    $project = [string]$Config.project
    return @(Get-ChildItem -LiteralPath $target.FullName -Filter '*.dll' -File |
        Where-Object {
            $name = [System.IO.Path]::GetFileNameWithoutExtension($_.Name)
            -not ($name -ieq $project -or $name.StartsWith($project + '.', [System.StringComparison]::OrdinalIgnoreCase))
        } |
        ForEach-Object { $_.FullName })
}

function Write-TracerHelper {
    param([string]$HelperFolder, [string]$TargetFramework)

    $projectContent = @'
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
    <Reference Include="Microsoft.CodeAnalysis">
      <HintPath>$(MSBuildSDKsPath)/../Roslyn/bincore/Microsoft.CodeAnalysis.dll</HintPath>
      <Private>true</Private>
    </Reference>
    <Reference Include="Microsoft.CodeAnalysis.CSharp">
      <HintPath>$(MSBuildSDKsPath)/../Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll</HintPath>
      <Private>true</Private>
    </Reference>
  </ItemGroup>
</Project>
'@
    $projectContent = $projectContent.Replace('{TARGET_FRAMEWORK}', $TargetFramework)

    $programContent = @'
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

if (args.Length != 4)
{
    Console.Error.WriteLine("usage: <config> <root> <manifest> <request>");
    return 2;
}

int exit = 0;
Thread worker = new(() =>
{
    try
    {
        exit = Tracer.Run(args);
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception.Message);
        exit = 1;
    }
}, 512 * 1024 * 1024);
worker.Start();
worker.Join();
return exit;

static class Tracer
{
    public static int Run(string[] args)
    {
        string configPath = args[0];
        string projectRoot = args[1];
        string manifestPath = args[2];
        JsonElement request = JsonDocument.Parse(File.ReadAllText(args[3])).RootElement;
        string entry = request.GetProperty("entry").GetString()!;
        string parameter = request.GetProperty("parameter").GetString() ?? "";
        Probe probe = Probe.Parse(request.GetProperty("value").GetString() ?? "");
        int depth = request.GetProperty("depth").GetInt32();
        string outPath = request.GetProperty("out").GetString()!;

        JsonElement config = JsonDocument.Parse(File.ReadAllText(configPath)).RootElement;
        string[] frameworks = config.GetProperty("references").GetProperty("frameworks").EnumerateArray().Select(item => item.GetString()!).ToArray();
        Regex layer = new(config.GetProperty("layer").GetString()!, RegexOptions.Compiled);

        Dictionary<char, List<string>> manifest = File.ReadAllLines(manifestPath)
            .Where(line => line.Length > 2)
            .GroupBy(line => line[0])
            .ToDictionary(group => group.Key, group => group.Select(line => line[2..]).ToList());
        List<string> Take(char tag) => manifest.TryGetValue(tag, out List<string>? list) ? list : [];

        Dictionary<string, string> chosen = new(StringComparer.OrdinalIgnoreCase);
        string runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        string shared = Path.GetDirectoryName(Path.GetDirectoryName(runtime)!)!;
        foreach (string pack in frameworks)
        {
            string folder = Path.Combine(shared, pack, Path.GetFileName(runtime));
            if (!Directory.Exists(folder))
            {
                Console.Error.WriteLine($"The shared framework '{pack}' is not installed at {folder}; its members stay unbound.");
                continue;
            }

            foreach (string path in Directory.EnumerateFiles(folder, "*.dll"))
            {
                chosen.TryAdd(Path.GetFileNameWithoutExtension(path), path);
            }
        }

        foreach (string path in Take('R'))
        {
            chosen[Path.GetFileNameWithoutExtension(path)] = path;
        }

        List<MetadataReference> references = [];
        foreach (string path in chosen.Values)
        {
            try
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
            catch (Exception exception) when (exception is IOException or BadImageFormatException)
            {
            }
        }

        CSharpParseOptions parseOptions = new(LanguageVersion.Preview, DocumentationMode.None, SourceCodeKind.Regular);
        List<SyntaxTree> trees = Take('S').Concat(Take('G')).AsParallel().AsOrdered()
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), parseOptions, path))
            .ToList();
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Tracer",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));

        Graph graph = new(compilation, projectRoot, layer);
        graph.Index(trees);

        List<IMethodSymbol> entries = graph.Find(entry);
        if (entries.Count == 0)
        {
            Console.Error.WriteLine($"The entry was not found in the source: {entry}");
            return 1;
        }

        Node root = graph.Inject(probe, entries, parameter, depth);
        string markdown = graph.Render(root, probe, entry);
        File.WriteAllText(outPath, markdown, new UTF8Encoding(false));
        return 0;
    }
}

enum Mention
{
    None,
    Direct,
    Derived,
}

readonly record struct Known(bool Has, object? Value)
{
    public static readonly Known Unknown = new(false, null);

    public string Text => !Has ? "" : Value switch
    {
        null => "null",
        bool flag => flag ? "true" : "false",
        string text => "\"" + (text.Length > 24 ? text[..24] + "\u2026" : text) + "\"",
        IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
        _ => Value.ToString() ?? "",
    };
}

sealed record Probe(string Raw, string Kind, Known Constant)
{
    public static Probe Parse(string raw)
    {
        if (raw == "true" || raw == "false")
        {
            return new Probe(raw, "bool", new Known(true, raw == "true"));
        }

        if (raw == "null")
        {
            return new Probe(raw, "null", new Known(true, null));
        }

        if (raw.StartsWith("file:", StringComparison.Ordinal))
        {
            string path = raw[5..];
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The value file was not found.", path);
            }

            return new Probe(raw, "file", Known.Unknown);
        }

        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long whole))
        {
            return new Probe(raw, "number", new Known(true, whole));
        }

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double real))
        {
            return new Probe(raw, "number", new Known(true, real));
        }

        return new Probe(raw, "text", new Known(true, raw));
    }

    public bool Accepts(ITypeSymbol type)
    {
        bool nullable = false;
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } wrapper)
        {
            type = wrapper.TypeArguments[0];
            nullable = true;
        }

        if (type.SpecialType == SpecialType.System_Object || type.TypeKind is TypeKind.TypeParameter or TypeKind.Dynamic)
        {
            return true;
        }

        return Kind switch
        {
            "bool" => type.SpecialType == SpecialType.System_Boolean,
            "number" => type.SpecialType is >= SpecialType.System_SByte and <= SpecialType.System_Double,
            "text" => type.SpecialType == SpecialType.System_String || (type.SpecialType == SpecialType.System_Char && Raw.Length == 1),
            "null" => nullable || type.IsReferenceType,
            "file" => type.SpecialType == SpecialType.System_String || Pictured(type),
            _ => false,
        };
    }

    static bool Pictured(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Byte })
        {
            return true;
        }

        if (type is INamedTypeSymbol { IsGenericType: true } generic
            && generic.Name is "ReadOnlyMemory" or "Memory" or "ReadOnlySpan" or "Span"
            && generic.TypeArguments[0].SpecialType == SpecialType.System_Byte)
        {
            return true;
        }

        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.Name is "Stream" or "Image" or "ImageSource" or "BitmapSource" or "Bitmap"
                || current.Name.EndsWith("Image", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

sealed class Node(int id, string label, string layer)
{
    public int Id { get; } = id;
    public string Label { get; } = label;
    public string Layer { get; } = layer;
    public string? End { get; set; }
    public List<Edge> Out { get; } = [];
}

sealed record Edge(Node To, string Kind);

sealed class Seed
{
    public Dictionary<ISymbol, Known> Symbols { get; } = new(SymbolEqualityComparer.Default);
    public HashSet<ISymbol> Results { get; } = new(SymbolEqualityComparer.Default);
    public bool This { get; set; }
    public bool Bound { get; set; }
}

sealed class Frame(IMethodSymbol method, Node node, int depth, Seed seed)
{
    public IMethodSymbol Method { get; } = method;
    public Node Node { get; } = node;
    public int Depth { get; } = depth;
    public Dictionary<ISymbol, Known> Tracked { get; } = new(seed.Symbols, SymbolEqualityComparer.Default);
    public HashSet<ISymbol> Results { get; } = seed.Results;
    public bool This { get; } = seed.This;
    public bool Bound { get; } = seed.Bound;
    public Dictionary<IOperation, Mention> Calls { get; } = [];
    public bool Changed { get; set; }
    public bool Returns { get; set; }
    public int Nested { get; set; }
}

sealed class Graph(CSharpCompilation compilation, string projectRoot, Regex layer)
{
    readonly Dictionary<IMethodSymbol, List<IOperation>> bodies = new(SymbolEqualityComparer.Default);
    readonly Dictionary<ISymbol, HashSet<IMethodSymbol>> readers = new(SymbolEqualityComparer.Default);
    readonly Dictionary<ISymbol, HashSet<IMethodSymbol>> callers = new(SymbolEqualityComparer.Default);
    readonly Dictionary<ISymbol, HashSet<ISymbol>> implementations = new(SymbolEqualityComparer.Default);
    readonly Dictionary<ISymbol, HashSet<ISymbol>> abstractions = new(SymbolEqualityComparer.Default);
    readonly Dictionary<string, Node> nodes = new(StringComparer.Ordinal);
    readonly Dictionary<string, bool> summaries = new(StringComparer.Ordinal);
    readonly Dictionary<string, int> depths = new(StringComparer.Ordinal);
    readonly HashSet<(int, int, string)> edges = [];
    readonly HashSet<string> active = new(StringComparer.Ordinal);
    readonly HashSet<string> guesses = new(StringComparer.Ordinal);
    bool stale;
    int limit;

    static ISymbol Norm(ISymbol symbol)
    {
        symbol = symbol.OriginalDefinition;
        return symbol is IMethodSymbol { PartialDefinitionPart: { } definition } ? definition : symbol;
    }

    static void Add(Dictionary<ISymbol, HashSet<IMethodSymbol>> map, ISymbol key, IMethodSymbol value)
    {
        key = Norm(key);
        if (!map.TryGetValue(key, out HashSet<IMethodSymbol>? set))
        {
            set = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            map[key] = set;
        }

        set.Add(value);
    }

    static void Link(Dictionary<ISymbol, HashSet<ISymbol>> map, ISymbol key, ISymbol value)
    {
        key = Norm(key);
        if (!map.TryGetValue(key, out HashSet<ISymbol>? set))
        {
            set = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            map[key] = set;
        }

        set.Add(Norm(value));
    }

    public void Index(List<SyntaxTree> trees)
    {
        foreach (SyntaxTree tree in trees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree, true);
            foreach (SyntaxNode syntax in tree.GetRoot().DescendantNodes())
            {
                IMethodSymbol? owner = null;
                IOperation? body = null;
                switch (syntax)
                {
                    case BaseMethodDeclarationSyntax method:
                        owner = model.GetDeclaredSymbol(method) as IMethodSymbol;
                        body = model.GetOperation(method);
                        break;
                    case AccessorDeclarationSyntax accessor:
                        owner = model.GetDeclaredSymbol(accessor) as IMethodSymbol;
                        body = model.GetOperation(accessor);
                        break;
                    case ArrowExpressionClauseSyntax arrow when arrow.Parent is BasePropertyDeclarationSyntax property:
                        owner = model.GetDeclaredSymbol(property) switch
                        {
                            IPropertySymbol symbol => symbol.GetMethod,
                            _ => null,
                        };
                        body = model.GetOperation(arrow);
                        break;
                }

                if (owner is null || body is null)
                {
                    continue;
                }

                IMethodSymbol key = (IMethodSymbol)Norm(owner);
                if (!bodies.TryGetValue(key, out List<IOperation>? list))
                {
                    list = [];
                    bodies[key] = list;
                }

                list.Add(body);
            }
        }

        foreach ((IMethodSymbol owner, List<IOperation> list) in bodies)
        {
            foreach (IOperation operation in list.SelectMany(body => body.DescendantsAndSelf()))
            {
                if (operation.Parent is ISimpleAssignmentOperation assignment && assignment.Target == operation)
                {
                    continue;
                }

                switch (operation)
                {
                    case IFieldReferenceOperation field:
                        Add(readers, field.Field, owner);
                        break;
                    case IPropertyReferenceOperation property:
                        Add(readers, property.Property, owner);
                        break;
                    case IParameterReferenceOperation reference
                        when reference.Parameter.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor
                        && !SymbolEqualityComparer.Default.Equals(Norm(constructor), owner):
                        Add(readers, reference.Parameter, owner);
                        break;
                    case IInvocationOperation invocation:
                        Add(callers, invocation.TargetMethod, owner);
                        break;
                }
            }
        }

        foreach (INamedTypeSymbol type in compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type).OfType<INamedTypeSymbol>())
        {
            foreach (INamedTypeSymbol contract in type.AllInterfaces)
            {
                foreach (ISymbol member in contract.GetMembers())
                {
                    if (type.FindImplementationForInterfaceMember(member) is { } implementation && Sourced(implementation))
                    {
                        Link(implementations, member, implementation);
                        Link(abstractions, implementation, member);
                    }
                }
            }

            foreach (ISymbol member in type.GetMembers())
            {
                ISymbol? overridden = member switch
                {
                    IMethodSymbol method => method.OverriddenMethod,
                    IPropertySymbol property => property.OverriddenProperty,
                    _ => null,
                };

                while (overridden is not null)
                {
                    Link(implementations, overridden, member);
                    Link(abstractions, member, overridden);
                    overridden = overridden switch
                    {
                        IMethodSymbol method => method.OverriddenMethod,
                        IPropertySymbol property => property.OverriddenProperty,
                        _ => null,
                    };
                }
            }
        }
    }

    static bool Sourced(ISymbol symbol) => symbol.Locations.Any(location => location.IsInSource);

    public List<IMethodSymbol> Find(string entry)
    {
        string[] parts = entry.Split('.');
        string name = parts[^1];
        string? owner = parts.Length > 1 ? parts[^2] : null;
        List<IMethodSymbol> found = [];
        foreach (ISymbol symbol in compilation.GetSymbolsWithName(_ => true, SymbolFilter.Member))
        {
            if (owner is not null && symbol.ContainingType?.Name != owner)
            {
                continue;
            }

            IMethodSymbol? method = symbol switch
            {
                IMethodSymbol { MethodKind: MethodKind.Constructor } constructor when constructor.ContainingType.Name == name => constructor,
                IMethodSymbol { MethodKind: MethodKind.Ordinary } ordinary when ordinary.Name == name => ordinary,
                IPropertySymbol property when property.Name == name => property.SetMethod,
                _ => null,
            };

            if (method is not null)
            {
                found.Add((IMethodSymbol)Norm(method));
            }
        }

        return found.Distinct<IMethodSymbol>(SymbolEqualityComparer.Default).ToList();
    }

    string Place(ISymbol symbol)
    {
        Location? location = symbol.Locations.FirstOrDefault(item => item.IsInSource);
        if (location?.SourceTree is null)
        {
            return "external";
        }

        string relative = Path.GetRelativePath(projectRoot, location.SourceTree.FilePath).Replace('\\', '/');
        Match match = layer.Match(relative);
        return match.Success ? match.Groups["layer"].Value : "source";
    }

    static string Named(ISymbol symbol)
    {
        string type = symbol.ContainingType?.Name ?? "";
        return symbol switch
        {
            IMethodSymbol { MethodKind: MethodKind.Constructor } method => "new " + method.ContainingType.Name,
            IMethodSymbol { MethodKind: MethodKind.PropertySet or MethodKind.PropertyGet } accessor => type + "." + accessor.AssociatedSymbol!.Name,
            IParameterSymbol parameter => type + "." + parameter.Name,
            IMethodSymbol method when method.ContainingType is { } owner && owner.GetMembers(method.Name).Length > 1 =>
                type + "." + method.Name + "[" + string.Join(", ", method.Parameters.Select(item => item.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))) + "]",
            _ => type.Length == 0 ? symbol.Name : type + "." + symbol.Name,
        };
    }

    Node Make(string key, Func<Node> create)
    {
        if (!nodes.TryGetValue(key, out Node? node))
        {
            node = create();
            nodes[key] = node;
        }

        return node;
    }

    Node Lone(string label, string layerName, string end)
    {
        Node node = new(nodes.Count, label, layerName) { End = end };
        nodes["!" + nodes.Count] = node;
        return node;
    }

    void Connect(Node from, Node to, string kind)
    {
        if (edges.Add((from.Id, to.Id, kind)))
        {
            from.Out.Add(new Edge(to, kind));
        }
    }

    static string Id(ISymbol symbol) => symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString();

    public Node Inject(Probe probe, List<IMethodSymbol> entries, string parameter, int depth)
    {
        limit = depth;
        Node root;
        do
        {
            nodes.Clear();
            edges.Clear();
            depths.Clear();
            active.Clear();
            guesses.Clear();
            stale = false;
            root = Pass(probe, entries, parameter);
        }
        while (stale);

        return root;
    }

    Node Pass(Probe probe, List<IMethodSymbol> entries, string parameter)
    {
        Node root = Lone($"value {probe.Raw} ({probe.Kind})", "input", "Drop");
        root.End = null;
        foreach (IMethodSymbol method in entries.SelectMany(item => Resolve(item) is { Count: > 0 } found ? found : [item]).Distinct<IMethodSymbol>(SymbolEqualityComparer.Default))
        {
            List<IParameterSymbol> slots = method.Parameters
                .Where(slot => parameter.Length == 0 || slot.Name == parameter)
                .ToList();
            List<IParameterSymbol> taking = slots.Where(slot => probe.Accepts(slot.Type)).ToList();
            if (taking.Count == 0)
            {
                string refused = slots.Count == 0
                    ? $"{Named(method)} has no parameter{(parameter.Length == 0 ? "" : " " + parameter)}"
                    : $"{Named(method)}({string.Join(", ", slots.Select(slot => slot.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) + " " + slot.Name))}) does not take {probe.Kind}";
                Connect(root, Lone(refused, Place(method), "Fail"), "pass");
                continue;
            }

            foreach (IParameterSymbol slot in taking)
            {
                Seed seed = new();
                seed.Symbols[slot] = probe.Constant;
                Node child = Enter(method, slot.Name, probe.Constant, seed, "F", 1);
                Connect(root, child, "pass");
            }
        }

        return root;
    }

    Node Enter(IMethodSymbol method, string slot, Known known, Seed seed, string mode, int depth, string? tag = null)
    {
        string text = known.Has ? "=" + known.Text : "";
        string key = $"M|{mode}|{Id(method)}|{tag ?? slot}|{text}";
        Node node = Make(key, () => new Node(nodes.Count, $"{Named(method)}({slot}{text})", Place(method)));
        Analyze(method, node, seed, key, depth);
        return node;
    }

    bool Analyze(IMethodSymbol method, Node node, Seed seed, string key, int depth)
    {
        bool known = summaries.GetValueOrDefault(key);
        if (depths.TryGetValue(key, out int seen) && (limit == 0 || seen <= depth))
        {
            if (active.Contains(key))
            {
                guesses.Add(key);
            }

            return known;
        }

        summaries[key] = known;
        depths[key] = depth;
        if (limit > 0 && depth > limit)
        {
            node.End = "Depth";
            return false;
        }

        Frame frame = new(method, node, depth, seed);
        if (!bodies.TryGetValue(method, out List<IOperation>? list))
        {
            Primary(frame);
            node.End = node.Out.Count == 0 ? "Drop" : null;
            return false;
        }

        bool entered = active.Add(key);
        do
        {
            frame.Changed = false;
            foreach (IOperation body in list)
            {
                Visit(frame, body);
            }
        }
        while (frame.Changed);

        if (entered)
        {
            active.Remove(key);
        }

        frame.Returns |= known;
        if (frame.Returns && !frame.Bound)
        {
            Ascend(frame);
        }

        if (frame.Returns != known && guesses.Contains(key))
        {
            stale = true;
        }

        summaries[key] = frame.Returns;
        node.End = node.Out.Count == 0 ? "Drop" : null;
        return frame.Returns;
    }

    void Primary(Frame frame)
    {
        foreach (ISymbol symbol in frame.Tracked.Keys.ToList())
        {
            if (symbol is not IParameterSymbol parameter)
            {
                continue;
            }

            if (readers.ContainsKey(parameter))
            {
                Hold(frame, parameter);
            }

            IPropertySymbol? property = frame.Method.ContainingType.GetMembers(parameter.Name).OfType<IPropertySymbol>().FirstOrDefault();
            if (frame.Method.ContainingType.IsRecord && property is not null)
            {
                Hold(frame, property);
            }
        }
    }

    void Ascend(Frame frame)
    {
        IMethodSymbol method = frame.Method;
        if (method.MethodKind == MethodKind.PropertyGet && method.AssociatedSymbol is IPropertySymbol property)
        {
            Store(frame, property, "return");
            return;
        }

        HashSet<ISymbol> targets = new(SymbolEqualityComparer.Default) { method };
        if (abstractions.TryGetValue(method, out HashSet<ISymbol>? above))
        {
            targets.UnionWith(above);
        }

        foreach (ISymbol target in targets)
        {
            if (!callers.TryGetValue(target, out HashSet<IMethodSymbol>? set))
            {
                continue;
            }

            foreach (IMethodSymbol caller in set)
            {
                Seed seed = new();
                seed.Results.UnionWith(targets);
                Node child = Enter(caller, "result of " + Named(method), Known.Unknown, seed, "R", frame.Depth + 1, "result of " + Id(method));
                Connect(frame.Node, child, "return");
            }
        }
    }

    void Hold(Frame frame, ISymbol member)
    {
        member = Norm(member);
        Connect(frame.Node, Make("H|" + Id(member), () => new Node(nodes.Count, Named(member), Place(member)) { End = "Held" }), "store");
    }

    bool Owned(Frame frame, IOperation? instance) => Strip(instance) switch
    {
        IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ImplicitReceiver } => true,
        IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance } => frame.Method.MethodKind == MethodKind.Constructor,
        _ => false,
    };

    void Store(Frame frame, ISymbol member, string kind)
    {
        member = Norm(member);
        if (!Sourced(member))
        {
            Connect(frame.Node, Make("X|" + Named(member), () => new Node(nodes.Count, Named(member), "external") { End = "Opaque" }), kind);
            return;
        }

        string key = "S|" + Id(member);
        Node node = Make(key, () => new Node(nodes.Count, Named(member), Place(member)));
        Connect(frame.Node, node, kind);
        int depth = frame.Depth + 1;
        if (depths.TryGetValue(key, out int before) && (limit == 0 || before <= depth))
        {
            return;
        }

        depths[key] = depth;
        if (limit > 0 && depth > limit)
        {
            node.End = "Depth";
            return;
        }

        HashSet<ISymbol> held = new(SymbolEqualityComparer.Default) { member };
        if (abstractions.TryGetValue(member, out HashSet<ISymbol>? above))
        {
            held.UnionWith(above);
        }

        HashSet<IMethodSymbol> reading = new(SymbolEqualityComparer.Default);
        foreach (ISymbol symbol in held)
        {
            if (readers.TryGetValue(symbol, out HashSet<IMethodSymbol>? set))
            {
                reading.UnionWith(set);
            }
        }

        foreach (IMethodSymbol reader in reading.OrderBy(item => Id(item), StringComparer.Ordinal))
        {
            Seed seed = new();
            foreach (ISymbol symbol in held)
            {
                seed.Symbols[symbol] = Known.Unknown;
            }

            Node child = Enter(reader, "reads " + member.Name, Known.Unknown, seed, "D", frame.Depth + 2, "reads " + Id(member));
            Connect(node, child, "read");
        }

        node.End = node.Out.Count == 0 ? "Drop" : null;
    }

    void Track(Frame frame, ISymbol symbol, Known known)
    {
        symbol = Norm(symbol);
        if (!frame.Tracked.TryGetValue(symbol, out Known current))
        {
            frame.Tracked[symbol] = known;
            frame.Changed = true;
            return;
        }

        if (current.Has && (!known.Has || !Equals(current.Value, known.Value)))
        {
            frame.Tracked[symbol] = Known.Unknown;
            frame.Changed = true;
        }
    }

    void Visit(Frame frame, IOperation? operation)
    {
        if (operation is null)
        {
            return;
        }

        switch (operation)
        {
            case IConditionalOperation conditional:
            {
                Visit(frame, conditional.Condition);
                Known test = Evaluate(frame, conditional.Condition);
                List<IOperation?> taken = !test.Has || test.Value is not bool flag
                    ? [conditional.WhenTrue, conditional.WhenFalse]
                    : [flag ? conditional.WhenTrue : conditional.WhenFalse];
                bool deciding = Mentions(frame, conditional.Condition) != Mention.None;
                foreach (IOperation? branch in taken)
                {
                    if (deciding)
                    {
                        Influence(frame, branch);
                    }

                    Visit(frame, branch);
                }

                return;
            }
            case IWhileLoopOperation loop:
            {
                Visit(frame, loop.Condition);
                if (Mentions(frame, loop.Condition) != Mention.None)
                {
                    Influence(frame, loop.Body);
                }

                Visit(frame, loop.Body);
                return;
            }
            case ISwitchOperation choice:
            {
                Visit(frame, choice.Value);
                Known test = Evaluate(frame, choice.Value);
                bool deciding = Mentions(frame, choice.Value) != Mention.None;
                List<ISwitchCaseOperation> taken = choice.Cases.ToList();
                if (test.Has)
                {
                    List<ISwitchCaseOperation> matched = taken
                        .Where(item => item.Clauses.Any(clause => clause is ISingleValueCaseClauseOperation single && Same(test, Evaluate(frame, single.Value))))
                        .ToList();
                    bool open = taken.Any(item => item.Clauses.Any(clause => clause is not ISingleValueCaseClauseOperation and not IDefaultCaseClauseOperation));
                    if (matched.Count > 0)
                    {
                        taken = matched;
                    }
                    else if (!open)
                    {
                        taken = taken.Where(item => item.Clauses.Any(clause => clause is IDefaultCaseClauseOperation)).ToList();
                    }
                }

                foreach (ISwitchCaseOperation item in taken)
                {
                    foreach (IOperation statement in item.Body)
                    {
                        if (deciding)
                        {
                            Influence(frame, statement);
                        }

                        Visit(frame, statement);
                    }
                }

                return;
            }
            case IForEachLoopOperation each:
            {
                Visit(frame, each.Collection);
                Mention mention = Mentions(frame, each.Collection);
                if (mention != Mention.None)
                {
                    foreach (ILocalSymbol local in each.Locals)
                    {
                        Track(frame, local, Known.Unknown);
                    }
                }

                Visit(frame, each.LoopControlVariable);
                Visit(frame, each.Body);
                return;
            }
            case IAnonymousFunctionOperation or ILocalFunctionOperation:
            {
                frame.Nested++;
                Children(frame, operation);
                frame.Nested--;
                return;
            }
        }

        Children(frame, operation);

        switch (operation)
        {
            case ISimpleAssignmentOperation assignment:
            {
                Mention mention = Mentions(frame, assignment.Value);
                if (mention != Mention.None)
                {
                    Assign(frame, assignment.Target, mention, mention == Mention.Direct ? Evaluate(frame, assignment.Value) : Known.Unknown);
                }

                break;
            }
            case ICompoundAssignmentOperation compound when Mentions(frame, compound.Value) != Mention.None:
                Assign(frame, compound.Target, Mention.Derived, Known.Unknown);
                break;
            case ICoalesceAssignmentOperation coalesce when Mentions(frame, coalesce.Value) != Mention.None:
                Assign(frame, coalesce.Target, Mention.Derived, Known.Unknown);
                break;
            case IVariableDeclaratorOperation declarator when declarator.GetVariableInitializer() is { } initializer:
            {
                Mention mention = Mentions(frame, initializer.Value);
                if (mention != Mention.None)
                {
                    Track(frame, declarator.Symbol, mention == Mention.Direct ? Evaluate(frame, initializer.Value) : Known.Unknown);
                }

                break;
            }
            case IReturnOperation { ReturnedValue: { } returned } when frame.Nested == 0 && Mentions(frame, returned) != Mention.None:
                frame.Returns = true;
                break;
            case IInvocationOperation invocation:
                Call(frame, invocation, invocation.TargetMethod, invocation.Instance, invocation.Arguments);
                break;
            case IObjectCreationOperation creation when creation.Constructor is not null:
                Call(frame, creation, creation.Constructor, null, creation.Arguments);
                break;
        }
    }

    void Children(Frame frame, IOperation operation)
    {
        foreach (IOperation child in operation.ChildOperations)
        {
            Visit(frame, child);
        }
    }

    void Influence(Frame frame, IOperation? branch)
    {
        if (branch is null)
        {
            return;
        }

        foreach (IOperation operation in branch.DescendantsAndSelf())
        {
            IMethodSymbol? target = operation switch
            {
                IInvocationOperation invocation => invocation.TargetMethod,
                IObjectCreationOperation creation => creation.Constructor,
                _ => null,
            };

            if (target is null || !Sourced(Norm(target)))
            {
                continue;
            }

            ISymbol symbol = Norm(target);
            Connect(frame.Node, Make("C|" + Id(symbol), () => new Node(nodes.Count, Named(symbol), Place(symbol)) { End = "Control" }), "control");
        }
    }

    void Assign(Frame frame, IOperation target, Mention mention, Known known)
    {
        string kind = mention == Mention.Direct ? "pass" : "derived";
        switch (target)
        {
            case ILocalReferenceOperation local:
                Track(frame, local.Local, known);
                break;
            case IParameterReferenceOperation parameter:
                Track(frame, parameter.Parameter, known);
                break;
            case IFieldReferenceOperation field when Owned(frame, field.Instance):
                Hold(frame, field.Field);
                break;
            case IFieldReferenceOperation field:
                Store(frame, field.Field, "store");
                break;
            case IPropertyReferenceOperation property when Owned(frame, property.Instance):
                Hold(frame, property.Property);
                break;
            case IPropertyReferenceOperation property:
            {
                IMethodSymbol? setter = property.Property.SetMethod is { } set ? (IMethodSymbol)Norm(set) : null;
                if (setter is not null && bodies.ContainsKey(setter))
                {
                    Seed seed = new() { Bound = true };
                    seed.Symbols[setter.Parameters[^1]] = known;
                    Connect(frame.Node, Enter(setter, "value", known, seed, "B", frame.Depth + 1), kind);
                }
                else
                {
                    Store(frame, property.Property, "store");
                }

                break;
            }
            case IArrayElementReferenceOperation element when Strip(element.ArrayReference) is ILocalReferenceOperation array:
                Track(frame, array.Local, Known.Unknown);
                break;
        }
    }

    void Call(Frame frame, IOperation site, IMethodSymbol target, IOperation? instance, IEnumerable<IArgumentOperation> arguments)
    {
        List<(int Ordinal, Mention Mention, Known Known)> carried = [];
        foreach (IArgumentOperation argument in arguments)
        {
            if (argument.Parameter is null)
            {
                continue;
            }

            Mention mention = Mentions(frame, argument.Value);
            if (mention != Mention.None)
            {
                carried.Add((argument.Parameter.Ordinal, mention, mention == Mention.Direct ? Evaluate(frame, argument.Value) : Known.Unknown));
            }
        }

        Mention receiver = Mentions(frame, instance);
        if (carried.Count == 0 && receiver == Mention.None)
        {
            return;
        }

        IMethodSymbol definition = (IMethodSymbol)Norm(target);
        if (definition.MethodKind == MethodKind.LocalFunction)
        {
            foreach ((int ordinal, _, Known known) in carried)
            {
                Track(frame, definition.Parameters[ordinal], known);
            }

            if (!frame.Calls.ContainsKey(site))
            {
                frame.Calls[site] = Mention.Derived;
                frame.Changed = true;
            }

            return;
        }

        List<IMethodSymbol> targets = Resolve(definition);
        string kind = receiver == Mention.Direct || carried.Any(item => item.Mention == Mention.Direct) ? "pass" : "derived";
        if (targets.Count == 0)
        {
            string label = definition.ContainingType?.TypeKind == TypeKind.Delegate ? "delegate " + definition.ContainingType.Name : Named(definition);
            Connect(frame.Node, Make("X|" + label, () => new Node(nodes.Count, label, "external") { End = "Opaque" }), kind);
            if (!frame.Calls.ContainsKey(site))
            {
                frame.Calls[site] = Mention.Derived;
                frame.Changed = true;
            }

            return;
        }

        bool returns = site is IObjectCreationOperation;
        foreach (IMethodSymbol callee in targets)
        {
            foreach ((int ordinal, Mention mention, Known known) in carried)
            {
                if (ordinal >= callee.Parameters.Length)
                {
                    continue;
                }

                IParameterSymbol slot = callee.Parameters[ordinal];
                Seed seed = new() { Bound = true };
                seed.Symbols[slot] = known;
                Node child = Enter(callee, slot.Name, known, seed, "B", frame.Depth + 1);
                Connect(frame.Node, child, mention == Mention.Direct ? "pass" : "derived");
                if (summaries.TryGetValue(NodeKey(callee, slot.Name, known), out bool back) && back)
                {
                    Connect(child, frame.Node, "return");
                    returns = true;
                }
            }

            if (receiver == Mention.Direct && !callee.IsStatic)
            {
                Seed seed = new() { Bound = true, This = true };
                Node child = Enter(callee, "this", Known.Unknown, seed, "B", frame.Depth + 1);
                Connect(frame.Node, child, "pass");
                if (summaries.TryGetValue(NodeKey(callee, "this", Known.Unknown), out bool back) && back)
                {
                    Connect(child, frame.Node, "return");
                    returns = true;
                }
            }
        }

        Mention result = returns ? Mention.Derived : Mention.None;
        if (!frame.Calls.TryGetValue(site, out Mention before) || before != result)
        {
            frame.Calls[site] = result;
            frame.Changed = true;
        }
    }

    static string NodeKey(IMethodSymbol method, string slot, Known known) =>
        $"M|B|{Id(method)}|{slot}|{(known.Has ? "=" + known.Text : "")}";

    List<IMethodSymbol> Resolve(IMethodSymbol definition)
    {
        List<IMethodSymbol> found = [];
        if (bodies.ContainsKey(definition) || (definition.MethodKind == MethodKind.Constructor && Sourced(definition)))
        {
            found.Add(definition);
        }

        if (implementations.TryGetValue(definition, out HashSet<ISymbol>? set))
        {
            found.AddRange(set.OfType<IMethodSymbol>().Where(item => bodies.ContainsKey(item)));
        }

        return found.Distinct<IMethodSymbol>(SymbolEqualityComparer.Default).ToList();
    }

    static IOperation? Strip(IOperation? operation)
    {
        while (operation is IConversionOperation or IParenthesizedOperation)
        {
            operation = operation switch
            {
                IConversionOperation conversion => conversion.Operand,
                IParenthesizedOperation parenthesized => parenthesized.Operand,
                _ => operation,
            };
        }

        return operation;
    }

    bool Holds(Frame frame, ISymbol symbol) => frame.Tracked.ContainsKey(Norm(symbol));

    Mention Mentions(Frame frame, IOperation? operation)
    {
        operation = Strip(operation);
        switch (operation)
        {
            case null:
                return Mention.None;
            case ILocalReferenceOperation local:
                return Holds(frame, local.Local) ? Mention.Direct : Mention.None;
            case IParameterReferenceOperation parameter:
                return Holds(frame, parameter.Parameter) ? Mention.Direct : Mention.None;
            case IFieldReferenceOperation field:
                return Holds(frame, field.Field) ? Mention.Direct : Weaker(Mentions(frame, field.Instance));
            case IPropertyReferenceOperation property:
                if (Holds(frame, property.Property))
                {
                    return Mention.Direct;
                }

                return Weaker(property.Arguments.Aggregate(Mentions(frame, property.Instance), (sum, item) => Max(sum, Mentions(frame, item.Value))));
            case IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance }:
                return frame.This ? Mention.Direct : Mention.None;
            case IInvocationOperation invocation:
                if (frame.Results.Contains(Norm(invocation.TargetMethod)))
                {
                    return Mention.Direct;
                }

                return frame.Calls.TryGetValue(invocation, out Mention called) ? called : Mention.None;
            case IObjectCreationOperation creation:
                return frame.Calls.TryGetValue(creation, out Mention created) ? created : Weaker(Mentions(frame, creation.Initializer));
        }

        Mention found = Mention.None;
        foreach (IOperation child in operation.ChildOperations)
        {
            found = Max(found, Mentions(frame, child));
        }

        return Weaker(found);
    }

    static Mention Weaker(Mention mention) => mention == Mention.None ? Mention.None : Mention.Derived;

    static Mention Max(Mention left, Mention right) => (Mention)Math.Max((int)left, (int)right);

    Known Evaluate(Frame frame, IOperation? operation)
    {
        if (operation is null)
        {
            return Known.Unknown;
        }

        if (operation.ConstantValue.HasValue)
        {
            return new Known(true, operation.ConstantValue.Value);
        }

        operation = Strip(operation);
        switch (operation)
        {
            case ILocalReferenceOperation local when frame.Tracked.TryGetValue(Norm(local.Local), out Known held):
                return held;
            case IParameterReferenceOperation parameter when frame.Tracked.TryGetValue(Norm(parameter.Parameter), out Known held):
                return held;
            case IUnaryOperation { OperatorKind: UnaryOperatorKind.Not } unary when Evaluate(frame, unary.Operand) is { Has: true, Value: bool flag }:
                return new Known(true, !flag);
            case IBinaryOperation binary:
            {
                Known left = Evaluate(frame, binary.LeftOperand);
                Known right = Evaluate(frame, binary.RightOperand);
                return binary.OperatorKind switch
                {
                    BinaryOperatorKind.Equals when left.Has && right.Has => new Known(true, Same(left, right)),
                    BinaryOperatorKind.NotEquals when left.Has && right.Has => new Known(true, !Same(left, right)),
                    BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.And when left.Value is false || right.Value is false => new Known(true, false),
                    BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.And when left.Value is true && right.Value is true => new Known(true, true),
                    BinaryOperatorKind.ConditionalOr or BinaryOperatorKind.Or when left.Value is true || right.Value is true => new Known(true, true),
                    BinaryOperatorKind.ConditionalOr or BinaryOperatorKind.Or when left.Value is false && right.Value is false => new Known(true, false),
                    _ => Known.Unknown,
                };
            }
            case IIsPatternOperation pattern:
                return Matches(frame, Evaluate(frame, pattern.Value), pattern.Pattern);
        }

        return Known.Unknown;
    }

    Known Matches(Frame frame, Known value, IPatternOperation pattern)
    {
        if (!value.Has)
        {
            return Known.Unknown;
        }

        return pattern switch
        {
            IConstantPatternOperation constant when Evaluate(frame, constant.Value) is { Has: true } expected => new Known(true, Same(value, expected)),
            INegatedPatternOperation negated when Matches(frame, value, negated.Pattern) is { Has: true, Value: bool flag } => new Known(true, !flag),
            _ => Known.Unknown,
        };
    }

    static bool Same(Known left, Known right)
    {
        if (!left.Has || !right.Has)
        {
            return false;
        }

        if (left.Value is null || right.Value is null)
        {
            return left.Value is null && right.Value is null;
        }

        if (left.Value is IConvertible && right.Value is IConvertible
            && left.Value is not (string or bool or char) && right.Value is not (string or bool or char))
        {
            try
            {
                return Convert.ToDecimal(left.Value, CultureInfo.InvariantCulture) == Convert.ToDecimal(right.Value, CultureInfo.InvariantCulture);
            }
            catch (Exception exception) when (exception is OverflowException or InvalidCastException)
            {
                return false;
            }
        }

        return left.Value.Equals(right.Value);
    }

    public string Render(Node root, Probe probe, string entry)
    {
        Dictionary<int, int> groups = new() { [root.Id] = 1 };
        Queue<Node> queue = new();
        queue.Enqueue(root);
        int next = 2;
        StringBuilder body = new();
        Dictionary<string, int> kinds = new(StringComparer.Ordinal);
        Dictionary<string, int> ends = new(StringComparer.Ordinal);
        int sections = 0;

        while (queue.Count > 0)
        {
            Node node = queue.Dequeue();
            if (node.Out.Count == 0)
            {
                continue;
            }

            sections++;
            int group = next++;
            List<string> items = [];
            foreach (Edge edge in node.Out)
            {
                kinds[edge.Kind] = kinds.GetValueOrDefault(edge.Kind) + 1;
                Node to = edge.To;
                string item = $"{to.Label} -{edge.Kind} [{to.Layer}]";
                if (groups.TryGetValue(to.Id, out int seen))
                {
                    item += $" \u2192 ({seen})";
                }
                else
                {
                    groups[to.Id] = group;
                    if (to.Out.Count > 0)
                    {
                        queue.Enqueue(to);
                    }
                    else
                    {
                        string end = to.End ?? "Drop";
                        ends[end] = ends.GetValueOrDefault(end) + 1;
                        item += $" {{{end}}}";
                    }
                }

                items.Add(item);
            }

            body.Append($"### ({groups[node.Id]}) {node.Label} [{node.Layer}]\n");
            body.Append($"({group}) {string.Join(", ", items)}\n\n");
        }

        StringBuilder text = new();
        text.Append($"# Trace: {probe.Raw} into {entry}\n\n");
        text.Append($"Symbols: {groups.Count}, sections: {sections}, hops: {kinds.Values.Sum()}.\n\n");
        text.Append("Hops: " + (kinds.Count == 0 ? "none" : string.Join(", ", kinds.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key} {pair.Value}"))) + ".\n\n");
        text.Append("Ends: " + (ends.Count == 0 ? "none" : string.Join(", ", ends.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key} {pair.Value}"))) + ".\n\n");
        text.Append("Each header is a symbol and the group it was first listed in. The line below lists its hops as the next group.\n");
        text.Append("An entry is `symbol -hop [layer]`. `\u2192 (n)` points back to a symbol already listed. `{End}` closes a path.\n\n");
        if (root.Out.Count == 0)
        {
            text.Append("The value found no parameter to enter.\n");
        }

        text.Append(body);
        return text.ToString().TrimEnd('\n') + "\n";
    }
}
'@

    $projectPath = Join-Path $HelperFolder 'Tracer.Helper.csproj'
    $programPath = Join-Path $HelperFolder 'Program.cs'
    [System.IO.File]::WriteAllText($projectPath, $projectContent, [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText($programPath, $programContent, [System.Text.UTF8Encoding]::new($false))
    return $projectPath
}

if ([string]::IsNullOrWhiteSpace($Entry)) {
    throw 'An entry method is required. Run tracevalue -Help for usage.'
}

if (-not $PSBoundParameters.ContainsKey('Value')) {
    throw 'A value is required. Run tracevalue -Help for usage.'
}

$projectRoot = (Get-Item -LiteralPath (Split-Path -Parent $PSScriptRoot)).FullName.TrimEnd($script:PathSeparators)
$configPath = Join-Path $PSScriptRoot 'TraceValue.json'
$config = Read-TracerConfig -ConfigPath $configPath

[string[]]$sourceFiles = @(Get-TracerSources -ProjectRoot $projectRoot -Config $config)
if ($sourceFiles.Length -eq 0) {
    throw "No source files were found under: $projectRoot"
}
[string[]]$generatedFiles = @(Get-TracerGenerated -ProjectRoot $projectRoot -Config $config)
[string[]]$referenceFiles = @(Get-TracerReferences -ProjectRoot $projectRoot -Config $config)

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    throw 'The .NET SDK is required, but dotnet was not found on PATH.'
}

$nativePreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
$sdkOutput = & $dotnet.Source --version 2>&1
$ErrorActionPreference = $nativePreference
if ($LASTEXITCODE -ne 0) {
    throw "The .NET SDK version could not be read.`n$($sdkOutput -join [Environment]::NewLine)"
}

$sdkVersion = ([string]($sdkOutput | Select-Object -First 1)).Trim()
if ($sdkVersion -notmatch '^(\d+)\.') {
    throw "The .NET SDK version is not recognized: '$sdkVersion'"
}

$targetFramework = "net$($Matches[1]).0"
$temporaryFolder = Join-Path ([System.IO.Path]::GetTempPath()) ($config.project + '-Tracer-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($temporaryFolder) | Out-Null

try {
    $manifestPath = Join-Path $temporaryFolder 'sources.txt'
    $manifest = @($sourceFiles | ForEach-Object { "S|$_" }) +
        @($generatedFiles | ForEach-Object { "G|$_" }) +
        @($referenceFiles | ForEach-Object { "R|$_" })
    [System.IO.File]::WriteAllLines($manifestPath, [string[]]$manifest, [System.Text.UTF8Encoding]::new($false))

    $helperFolder = Join-Path $temporaryFolder 'helper'
    [System.IO.Directory]::CreateDirectory($helperFolder) | Out-Null
    $projectPath = Write-TracerHelper -HelperFolder $helperFolder -TargetFramework $targetFramework

    $tracePath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        Join-Path $temporaryFolder 'trace.md'
    }
    elseif ([System.IO.Path]::IsPathRooted($OutputPath)) {
        $OutputPath
    }
    else {
        Join-Path (Get-Location).Path $OutputPath
    }

    $requestPath = Join-Path $temporaryFolder 'request.json'
    $request = [ordered]@{
        entry     = $Entry
        parameter = $Parameter
        value     = $Value
        depth     = $Depth
        out       = $tracePath
    } | ConvertTo-Json
    [System.IO.File]::WriteAllText($requestPath, $request, [System.Text.UTF8Encoding]::new($false))

    $arguments = @(
        'run',
        '--project', $projectPath,
        '--configuration', 'Release',
        '--no-launch-profile',
        '--',
        $configPath,
        $projectRoot,
        $manifestPath,
        $requestPath
    )

    $previousNoLogo = $env:DOTNET_NOLOGO
    $previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
    $env:DOTNET_NOLOGO = '1'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

    try {
        $nativePreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        $traceOutput = & $dotnet.Source @arguments 2>&1
        $ErrorActionPreference = $nativePreference
        $traceExitCode = $LASTEXITCODE
    }
    finally {
        $env:DOTNET_NOLOGO = $previousNoLogo
        $env:DOTNET_CLI_TELEMETRY_OPTOUT = $previousTelemetry
    }

    if ($traceExitCode -ne 0) {
        throw "The trace failed.`n$($traceOutput -join [Environment]::NewLine)"
    }

    if ([string]::IsNullOrWhiteSpace($OutputPath)) {
        [System.IO.File]::ReadAllText($tracePath, [System.Text.Encoding]::UTF8) | Write-Host
    }
    else {
        Write-Host "Trace written: $tracePath" -ForegroundColor Cyan
    }
}
finally {
    if (Test-Path -LiteralPath $temporaryFolder) {
        Remove-Item -LiteralPath $temporaryFolder -Recurse -Force -ErrorAction SilentlyContinue
    }
}

exit 0
