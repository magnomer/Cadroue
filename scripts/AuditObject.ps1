<#
.SYNOPSIS
Grades every type on a ladder of creature verdicts: merges every partial type and measures
its size, its coupling to other codebase types, the state it rebinds and how tightly its parts
are glued.

.DESCRIPTION
A type split over several partial files can look tidy on disk while the compiler still sees one
object. This audit parses the source with Roslyn, merges the parts of every type, and measures:

  - Parts, Lines and Members of the merged type: its declarations, the lines they span and the
    members they declare. A part is one declaration of the type, so one file may hold several.
    Lines sums the declaration spans, so the lines of a nested type count toward it. Members
    leaves nested types out, since each is merged as its own type. A partial method or property
    counts once, on the part that implements it.
  - Mutable: the slots the type can rebind after construction. These are the fields neither const
    nor readonly, static ones included, and the backed properties with a non-init setter. A
    readonly field of any type, an event, and a get-only or init-only property are not Mutable.
    Known limit: a captured primary-constructor parameter is neither Mutable nor a hub slot,
    though the compiler stores it, since it declares no member of the type.
  - Outgoing: the distinct codebase types the type names, its fan-out. Incoming: the distinct
    codebase types that name it, its fan-in. Both count types, not members or references. A
    codebase type is any class, struct, interface, record, enum or delegate in a walked file.
    Every name counts, so a get-only, init-only or event declaration still adds its type. Names
    inside a nested type belong to it, and a type never uses itself or a type nested in or
    around it.
  - Crossings: member links that cross from one part into another. Density: Crossings per member.
  - Glued: the share of parts the widest component of the member graph spans, members being
    linked by "reads, writes or calls". Fused is the same share once hub state is removed.
  - Shared: the hub slots of the type.

The helper reads the bound sources in three passes. The first registers every part and member,
so a reference bound later finds its target whatever file declares it. The second turns every
simple name in a member body that binds to another member of the same type into an edge from
the enclosing member, never to itself. The third binds every simple name under a type
declaration, nested types excluded, to the codebase types it stands for, which Outgoing and
Incoming count.

Every flag is measured on the merged type, whatever its part count, so a partial split escapes
none. Each flag below is hit when its condition holds with every comparison at the limit or
above. A type keeps every flag it hits. Its verdict is the worst of them, in this order, else
Colony or Hermit:
  Hydra      parts and lines at thresholds.hydra, and fused or density at thresholds.hydra
  Kraken     a Serpent or Centipede that is also an Octopus or Spider, with no threshold of its own
  Spider     outgoing and incoming at thresholds.spider
  Chameleon  mutable at thresholds.chameleon
  Octopus    outgoing at thresholds.octopus
  Centipede  members at thresholds.centipede
  Serpent    lines at thresholds.serpent
  Colony     several parts, no flag
  Hermit     one part, no flag
Kraken joins the size axis (Serpent or Centipede) and the coupling axis (Octopus or Spider).
Mutable stays its own axis, so a Chameleon condition never makes a Kraken.
A hub is a state slot reached from thresholds.hub.parts or more parts, its declaring part
included. It is a finding on the slot, outside the ladder, never a flag or a verdict. A type whose
only finding is a hub stays a Colony, and the verdict table shows each type's hub count. While
enforced is true, the count of every flag and of hubs must stay within its ceiling, and every type
declared in several parts within the part count its row in parts names, an unnamed type holding one.
A ceiling above its count is stale and always fails.

The convention test TAuditObject is the counterpart of this script, and neither reads the other.
On the same tree both report the same results, while presentation is each side's own. The only
allowed difference in results is a config fault. This script throws on any missing key in
AuditObject.json, a missing ceiling key included, and refuses a configuration at another
generation. The test reads a missing ceiling key as 0, so that flag's fact fails once the flag
has a hit. A missing limit key throws on both sides.

Binding goes through the shared binder of AuditBinder.cs and AuditBinder.json: the tracked
sources, the generated code and the host build output, with no compile error allowed. The
solution must be built first. The scope line counts the listed source files the binder walked.
The helper targets helper.framework, the framework of the convention tests, and is compiled
once per text and SDK into the temp folder. The console shows the result, the hits by flag, the
types by verdict, the types declared in several parts, every flagged type with its flags and hubs,
and every hit list. The split, flagged and hit lists are cut at console.top, while the Markdown
report written to {report.directory}\{prefix}{version}.md holds every row. A page with the same
result is written beside it as {report.directory}\{prefix}{version}.html, filled from the template
AuditObject.html, with every measured type in one filterable table. The page opens in the default
browser unless -NoOpen is given.
Console and report number every term by one list: (1) Hydra to (7) Serpent on the ladder,
(8) Hub and (9) Colony. Hermit, the clean state, carries no number.

Everything project-specific lives in AuditObject.json next to this script. No project source
is modified. Git and the .NET SDK are required.

.PARAMETER Root
Project root to audit. Defaults to the directory containing this script's parent.

.PARAMETER ReportDirectory
Overrides report.directory for this run, for the Markdown report and the page. Relative paths
resolve against the project root.

.PARAMETER Top
Overrides console.top: how many rows the split, flagged and hit lists show on the console.

.PARAMETER Open
Open the Markdown report after the audit finishes.

.PARAMETER NoOpen
Write the page without opening it.

.PARAMETER NoPause
Do not stop at each console page. Paging is also off when output or input is redirected.

.PARAMETER Help
Display this help and exit. The alias -? is supported.

.EXAMPLE
AuditObject
Audit the current checkout.

.EXAMPLE
AuditObject -ReportDirectory D:\temp\audit -Top 10 -Open -NoOpen
Write the reports elsewhere, show ten rows per list, open the Markdown report but not the page.
#>
#requires -Version 5.1
# AUDITOBJECT - AUDIT GENERATION 21.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the audited outcome changes.
# A generation names the set of checks the audit applies. Two projects on the same generation audit
# the same things and their reports compare directly, whatever else differs between the files. A check
# added, removed, or changed in what it reports is a new generation; wording, plumbing, and refactoring
# leave it alone. Every audit script shares one generation number with the convention-test settings,
# and each refuses a configuration written at another generation.
# Generation 9 merges partial types, counts parts, lines, members and state, measures shared state
# and cross-part references, and computes member-graph components to grade each split type.
# Generation 11: nothing the object audit reports changes; the number rises with the truth audit,
# which checks that a deportment field reaches no request, keeps one writer, holds no logic and
# treats no engine data.
# Generation 12: the audit applies the rules of the convention test: the monolith rule with hubs and
# density, the hub and large counts, and a part ceiling per split type, bound with no compile error.
# Generation 13: nothing this audit reports changes; the number rises with the UI audit, which
# counts every surface markup line that hooks logic into the markup and every surface member
# that is not a constructor.
# Generation 14: nothing this audit reports changes; the number rises with the UI audit, which
# also counts command parameters, member paths and literal tags in surface markup as hooks.
# Generation 15: nothing this audit reports changes; the number rises with the name audit, which
# counts prefix turfs, and the UI audit, which counts pack URIs, scaffold types and contract IDs.
# Generation 16: nothing this audit reports changes; the number rises with the structure audit, whose
# Unsealing kind counts engine types on the public members of sealed Deportment types.
# Generation 17: findings take one vocabulary of -ing kinds and plain measure names, and the
# configuration keys follow. What the audit counts is unchanged.
# Generation 18: nothing this audit reports changes; the number rises with the UI audit, whose
# truth detector stops five false findings.
# Generation 19: nothing this audit reports changes; the number rises with the comment audit, whose
# hash line ties every comment file to the sources it describes.
# Generation 20: nothing this audit reports changes; the number rises with the UI audit, whose
# Mismatching kind only informs while a driver folder it waits on holds no source.
# Generation 21: nothing this audit reports changes; the number rises with the structure, name and
# UI audits, which gained kinds, and with the new fault audit.
[CmdletBinding()]
param(
    [string]$Root,
    [string]$ReportDirectory,
    [int]$Top = 0,
    [switch]$Open,
    [switch]$NoOpen,
    [switch]$NoPause,
    [Alias('?')]
    [switch]$Help
)

if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Split-Path -Parent $PSScriptRoot
}

if ($Help) {
    @'
NAME
    AuditObject.ps1

SYNOPSIS
    Merge partial types and grade every type on a ladder of creature verdicts.

SYNTAX
    AuditObject [-Root <path>] [-ReportDirectory <path>] [-Top <n>] [-Open] [-NoOpen] [-NoPause] [-Help]

OPTIONS
    -Root <path>             Project root. Defaults to the parent of the scripts folder.
    -ReportDirectory <path>  Overrides report.directory for the report and the page.
    -Top <n>                 Overrides console.top, the rows each console list shows.
    -Open                    Open the Markdown report when done.
    -NoOpen                  Write the page without opening it.
    -NoPause                 No console paging.
    -Help                    Show this help.

VERDICTS
    (1) Hydra, (2) Kraken, (3) Spider, (4) Chameleon, (5) Octopus, (6) Centipede, (7) Serpent,
    (9) Colony, Hermit, worst first. A hub, numbered (8), is a finding per state slot, never a
    verdict. See the script header for definitions.

COUNTERS
    Above ceiling   flag, hub or part counts above their ceiling while enforced is true.
    Stale ceilings  ceilings above their count, enforced or not.

REPORTS
    {report.directory}\{prefix}{version}.md    every row of every list.
    {report.directory}\{prefix}{version}.html  the same result as a page, opened unless -NoOpen.
'@ | Write-Host
    exit 0
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:AuditGeneration = 21

[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding

$script:PageLimit = 0
$script:PageWidth = 0
$script:PageCount = 0
if (-not $NoPause) {
    try {
        if (-not [Console]::IsOutputRedirected -and -not [Console]::IsInputRedirected) {
            $script:PageLimit = [Math]::Max(0, $Host.UI.RawUI.WindowSize.Height - 2)
            $script:PageWidth = [Math]::Max(1, $Host.UI.RawUI.WindowSize.Width)
        }
    }
    catch {
        $script:PageLimit = 0
    }
}

function Get-OrdinalKey {
    # Sort-Object compares text by culture, which Windows PowerShell 5.1 and pwsh 7 order differently.
    # Uppercase hexadecimal UTF-16 code units compare alike under every culture, so this key sorts ordinally.
    param([string]$Text)
    return [System.BitConverter]::ToString([System.Text.Encoding]::BigEndianUnicode.GetBytes($Text)).Replace('-', '')
}

function Write-AuditLine {
    param(
        [Parameter(Position = 0)][AllowEmptyString()][string]$Text = '',
        [ConsoleColor]$ForegroundColor,
        [string]$Lead = '',
        [ConsoleColor]$LeadColor = [ConsoleColor]::Gray
    )

    if ($script:PageLimit -gt 0) {
        $rows = [Math]::Max(1, [Math]::Ceiling($Text.Length / [double]$script:PageWidth))
        if ($script:PageCount + $rows -gt $script:PageLimit -and $script:PageCount -gt 0) {
            $prompt = '-- More -- (any key: next page, Q: no more pauses)'
            Write-Host $prompt -ForegroundColor Yellow -NoNewline
            $key = [Console]::ReadKey($true)
            Write-Host ("`r" + (' ' * $prompt.Length) + "`r") -NoNewline
            if ($key.Key -eq [ConsoleKey]::Q) {
                $script:PageLimit = 0
            }
            $script:PageCount = 0
        }
        $script:PageCount += $rows
    }

    if ($Lead -ne '') {
        Write-Host $Lead -ForegroundColor $LeadColor -NoNewline
        Write-Host $Text.Substring([Math]::Min($Lead.Length, $Text.Length))
    }
    elseif ($PSBoundParameters.ContainsKey('ForegroundColor')) {
        Write-Host $Text -ForegroundColor $ForegroundColor
    }
    else {
        Write-Host $Text
    }
}

Write-AuditLine "AUDITOBJECT - AUDIT GENERATION $script:AuditGeneration" -ForegroundColor Blue
$script:PathSeparators = [char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)

function Resolve-ProjectRoot {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw 'The project root is empty.'
    }

    $item = Get-Item -LiteralPath $Path -ErrorAction Stop
    if (-not $item.PSIsContainer) {
        throw "The project root is not a directory: $Path"
    }

    return $item.FullName.TrimEnd($script:PathSeparators)
}

function Read-AuditConfig {
    param([string]$ConfigPath)

    if (-not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) {
        throw "The audit configuration was not found: $ConfigPath"
    }

    try {
        $config = Get-Content -LiteralPath $ConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The audit configuration is not valid JSON: $ConfigPath`n$($_.Exception.Message)"
    }

    $required = @(
        'generation', 'project', 'enforced', 'helper.framework',
        'sources.roots', 'sources.extensions', 'sources.excludeSegments',
        'sources.excludeSuffixes', 'sources.excludePrefixes',
        'thresholds.hydra.parts', 'thresholds.hydra.lines', 'thresholds.hydra.fused', 'thresholds.hydra.density',
        'thresholds.kraken', 'thresholds.spider.outgoing', 'thresholds.spider.incoming',
        'thresholds.chameleon.mutable', 'thresholds.octopus.outgoing', 'thresholds.centipede.members',
        'thresholds.serpent.lines', 'thresholds.hub.parts',
        'ceiling.hydra', 'ceiling.kraken', 'ceiling.spider', 'ceiling.chameleon',
        'ceiling.octopus', 'ceiling.centipede', 'ceiling.serpent', 'ceiling.hub', 'parts',
        'console.top',
        'history.records', 'history.pagePrefix',
        'report.directory', 'report.versionFile', 'report.versionKey', 'report.prefix'
    )

    foreach ($key in $required) {
        $node = $config
        foreach ($segment in $key.Split('.')) {
            if ($null -eq $node -or -not (@($node.PSObject.Properties | ForEach-Object { $_.Name }) -contains $segment)) {
                throw "The audit configuration has no key '$key': $ConfigPath"
            }
            $node = $node.$segment
        }
    }

    if ([int]$config.generation -ne $script:AuditGeneration) {
        throw "The object-audit configuration is generation $($config.generation) but this tooling is generation $script:AuditGeneration.`nA generation names the set of checks applied, so the two must match: $ConfigPath"
    }

    return $config
}

function Read-ProjectVersion {
    param([string]$ProjectRoot, $Config)

    $versionPath = Join-Path $ProjectRoot $Config.report.versionFile
    if (-not (Test-Path -LiteralPath $versionPath -PathType Leaf)) {
        throw "Version file was not found: $versionPath"
    }

    try {
        $versionData = Get-Content -LiteralPath $versionPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "Version file is not valid JSON: $versionPath`n$($_.Exception.Message)"
    }

    $version = [string]$versionData.($Config.report.versionKey)
    if ([string]::IsNullOrWhiteSpace($version)) {
        return 'unknown'
    }

    return $version
}

function Test-ExcludedRelativePath {
    param([string]$RelativePath, $Config)

    $segments = $RelativePath -split '[\\/]'
    foreach ($segment in $segments) {
        if ($Config.sources.excludeSegments -contains $segment) {
            return $true
        }
    }

    $fileName = $segments[$segments.Length - 1]
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

function Get-ProjectSourceFiles {
    param([string]$ProjectRoot, $Config)

    $git = Get-Command git -ErrorAction SilentlyContinue
    if ($null -eq $git) {
        throw 'Git is required to enumerate project source files, but git was not found on PATH.'
    }

    if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot '.git'))) {
        throw "The project root is not a Git working tree: $ProjectRoot"
    }

    $pathspecs = @(foreach ($folder in $Config.sources.roots) {
            foreach ($extension in $Config.sources.extensions) {
                ':(icase)' + ([string]$folder).TrimEnd('/') + '/*' + $extension
            }
        })
    $lsArguments = @('-c', 'core.quotePath=false', '-C', $ProjectRoot,
        'ls-files', '--cached', '--others', '--exclude-standard', '--') + $pathspecs
    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $gitOutput = & $git.Source @lsArguments 2>&1
    $ErrorActionPreference = $nativePreference
    if ($LASTEXITCODE -ne 0) {
        throw "Git could not enumerate source files.`n$($gitOutput -join [Environment]::NewLine)"
    }

    $separator = [string][System.IO.Path]::DirectorySeparatorChar
    $rootPrefix = $ProjectRoot.TrimEnd($script:PathSeparators) + $separator
    $files = New-Object 'System.Collections.Generic.List[string]'

    foreach ($entry in $gitOutput) {
        $relativePath = ([string]$entry).Trim()
        if ([string]::IsNullOrWhiteSpace($relativePath) -or (Test-ExcludedRelativePath -RelativePath $relativePath -Config $Config)) {
            continue
        }

        $fullPath = $rootPrefix + $relativePath.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
            $files.Add($fullPath)
        }
    }

    return @($files.ToArray() | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } } -Unique)
}

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
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length != 8)
{
    Console.Error.WriteLine("usage: <config> <root> <manifest> <version> <report> <top> <binder> <page>");
    return 2;
}

string configPath = args[0];
string projectRoot = args[1];
string manifestPath = args[2];
string version = args[3];
string reportPath = args[4];
int top = int.Parse(args[5]);
string binderPath = args[6];
string pagePath = args[7];

JsonElement config = JsonDocument.Parse(File.ReadAllText(configPath)).RootElement;
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
int generation = config.GetProperty("generation").GetInt32();
bool enforced = config.GetProperty("enforced").GetBoolean();
JsonElement ceiling = config.GetProperty("ceiling");
string[] ladder = ["Hydra", "Kraken", "Spider", "Chameleon", "Octopus", "Centipede", "Serpent"];
string[] verdicts = [.. ladder, "Colony", "Hermit"];
string[] terms = [.. ladder, "Hub", "Colony"];
Dictionary<string, int> ceilings = ladder.Append("Hub")
    .ToDictionary(kind => kind, kind => ceiling.GetProperty(kind.ToLowerInvariant()).GetInt32(), StringComparer.Ordinal);
Dictionary<string, int> partsCeilings = config.GetProperty("parts").EnumerateObject()
    .ToDictionary(item => item.Name, item => item.Value.GetInt32(), StringComparer.Ordinal);

HashSet<string> chosen = File.ReadAllLines(manifestPath)
    .Where(line => line.Length > 0)
    .Select(line => Path.GetFullPath(Path.Combine(projectRoot, line)))
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
LAuditBinder binder = LAuditBinder.LAuditBinderRead(projectRoot, binderPath);
CSharpCompilation compilation = binder.LAuditCompilation;
List<SyntaxTree> trees = binder.LAuditTrees.Where(tree => chosen.Contains(Path.GetFullPath(tree.FilePath))).ToList();
if (trees.Count == 0)
{
    Console.Error.WriteLine("No listed source file is bound, so the audit cannot judge.");
    return 2;
}

Dictionary<INamedTypeSymbol, TypeRecord> types = new(SymbolEqualityComparer.Default);

foreach (SyntaxTree tree in trees)
{
    SemanticModel model = compilation.GetSemanticModel(tree, true);
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
                if (type.Members.ContainsKey(symbol))
                {
                    continue;
                }

                type.Members.Add(symbol, new MemberRecord(type.Members.Count, symbol, part, state, mutable));
            }
        }
    }
}

foreach (SyntaxTree tree in trees)
{
    SemanticModel model = compilation.GetSemanticModel(tree, true);
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

            MemberRecord[] sources = DeclaredMembers(model, member)
                .Select(pair => type.Members.TryGetValue(pair.Symbol, out MemberRecord? found) ? found : null)
                .Where(found => found is not null)
                .Select(found => found!)
                .ToArray();
            if (sources.Length == 0)
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

                bound = bound.OriginalDefinition;
                if (!type.Members.TryGetValue(bound, out MemberRecord? target))
                {
                    continue;
                }

                foreach (MemberRecord source in sources)
                {
                    if (!ReferenceEquals(source, target))
                    {
                        source.Uses.Add(target);
                        target.UsedBy.Add(source);
                    }
                }
            }
        }
    }
}

HashSet<INamedTypeSymbol> codebase = new(SymbolEqualityComparer.Default);
foreach (SyntaxTree tree in trees)
{
    SemanticModel model = compilation.GetSemanticModel(tree, true);
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
    SemanticModel model = compilation.GetSemanticModel(tree, true);
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

List<TypeSummary> split = summaries.Where(summary => summary.Parts > 1).ToList();
Dictionary<string, List<string>> hits = new(StringComparer.Ordinal);
Dictionary<string, List<(string Type, Measure[] Measures)>> cited = new(StringComparer.Ordinal);
foreach (string flag in ladder)
{
    cited[flag] = summaries
        .Where(summary => summary.Flags.Contains(flag))
        .Select(summary => (summary.Name, Cited(flag, summary)))
        .ToList();
    hits[flag] = cited[flag]
        .Select(row => $"{row.Type}: " + string.Join(", ", row.Measures.Select(measure => $"{measure.Name} {measure.Text}")))
        .ToList();
}

hits["Hub"] = summaries
    .SelectMany(summary => summary.Hubs.Select(hub => $"{summary.Name}: {hub}"))
    .ToList();
cited["Hub"] = summaries
    .SelectMany(summary => summary.HubSlots.Select(slot => (summary.Name, new Measure[] { new("slot", slot.Slot), new("parts", slot.Parts) })))
    .ToList();
Dictionary<string, int> partCounts = split.ToDictionary(summary => summary.Name, summary => summary.Parts, StringComparer.Ordinal);
List<(string Term, string Line)> partsOver = partCounts
    .Where(pair => pair.Value > partsCeilings.GetValueOrDefault(pair.Key, 1))
    .Select(pair => (pair.Key, $"parts {pair.Value}, ceiling {partsCeilings.GetValueOrDefault(pair.Key, 1)}"))
    .ToList();
List<(string Term, string Line)> aboveTerms = enforced
    ? ceilings
        .Where(pair => hits[pair.Key].Count > pair.Value)
        .Select(pair => (Numbered(pair.Key), $"{hits[pair.Key].Count} hit(s), ceiling {pair.Value}"))
        .Concat(partsOver)
        .ToList()
    : [];
List<string> above = aboveTerms.Select(row => $"{row.Term}: {row.Line}").ToList();
Dictionary<string, int> counts = new(partCounts, StringComparer.Ordinal);
foreach (string kind in ceilings.Keys)
{
    counts[kind] = hits[kind].Count;
}

List<(string Term, string Line)> staleTerms = ceilings
    .Concat(partsCeilings)
    .Where(pair => counts.GetValueOrDefault(pair.Key, 1) < pair.Value)
    .Select(pair => (Numbered(pair.Key), $"{counts.GetValueOrDefault(pair.Key, 1)} hit(s), ceiling {pair.Value}"))
    .ToList();
List<string> stale = staleTerms.Select(row => $"{row.Term}: {row.Line}").ToList();
List<TypeSummary> shown = summaries
    .Where(summary => summary.Verdict != "Hermit" || summary.Hubs.Count > 0)
    .OrderBy(summary => Array.IndexOf(verdicts, summary.Verdict))
    .ThenBy(summary => summary.Name, StringComparer.Ordinal)
    .ToList();

Console.WriteLine($"Scanned: {trees.Count:N0} source files, ceilings {(enforced ? "enforced" : "not enforced")}");
List<(string Gate, int Count, string Meaning)> gates =
[
    ("Above ceiling", above.Count, "flags or types above their enforced ceiling"),
    ("Stale ceilings", stale.Count, "ceilings set above their current hits"),
];
WriteResult(gates);

WriteHeading("Hits by flag");
List<string[]> kindRows = ceilings
    .Select(pair => new[] { Numbered(pair.Key), hits[pair.Key].Count.ToString("N0"), pair.Value.ToString("N0") })
    .ToList();
WriteTable(TextTable(["Flag", "Hits", "Ceiling"], kindRows));

WriteHeading("Types by verdict");
List<string[]> verdictRows = verdicts
    .Select(verdict => new[] { Numbered(verdict), summaries.Count(summary => summary.Verdict == verdict).ToString("N0") })
    .Append(["Total", summaries.Count.ToString("N0")])
    .ToList();
WriteTable(TextTable(["Verdict", "Types"], verdictRows));

string[] header =
[
    "Type", "Parts", "Lines", "Members", "Mutable", "Outgoing", "Incoming", "Shared", "Crossings", "Glued", "Fused", "Density", "Verdict",
];
string[] verdictHeader = ["Type", "Verdict", "Flags", "Hubs", "Parts", "Lines", "Members", "Mutable", "Outgoing", "Incoming"];
if (split.Count > 0)
{
    string splitHeading = $"Types declared in several parts ({split.Count:N0})";
    WriteHeading(splitHeading);
    WriteTable(TextTable(header, split.Take(top).Select(Row).ToList()));

    if (split.Count > top)
    {
        Console.WriteLine($"... and {split.Count - top:N0} more in the report.");
    }
}

if (shown.Count > 0)
{
    WriteHeading($"Flagged types and hubs ({shown.Count:N0})");
    WriteTable(TextTable(verdictHeader, shown.Take(top).Select(VerdictRow).ToList()));

    if (shown.Count > top)
    {
        Console.WriteLine($"... and {shown.Count - top:N0} more in the report.");
    }
}

List<(string Title, List<string> Rows)> sections = ceilings.Keys
    .Select(kind => (kind, hits[kind]))
    .Append(("Above ceiling", above))
    .Append(("Stale ceilings", stale))
    .ToList();
foreach ((string title, List<string> rows) in sections.Where(section => section.Rows.Count > 0))
{
    string heading = terms.Contains(title) ? $"{Numbered(title)}: {rows.Count:N0}" : $"{title} ({rows.Count:N0})";
    WriteHeading(heading);
    rows.Take(top).ToList().ForEach(Console.WriteLine);
    if (rows.Count > top)
    {
        Console.WriteLine($"... and {rows.Count - top:N0} more in the report.");
    }
}

Console.WriteLine();
Console.WriteLine($"Report: {reportPath}");

List<string> lines =
[
    $"# Object audit {version}",
    string.Empty,
    $"- Generation: {generation}",
    $"- Enforced: {enforced}",
    $"- Types: {summaries.Count}, declared in several parts: {split.Count}",
    "- Verdicts: " + string.Join(", ", verdicts.Select(verdict => $"{Numbered(verdict)} {summaries.Count(summary => summary.Verdict == verdict)}")),
];
lines.AddRange(ceilings.Select(pair => $"- {Numbered(pair.Key)}: {hits[pair.Key].Count}, ceiling {pair.Value}"));
lines.Add($"- Above ceiling: {above.Count}");
lines.Add($"- Stale ceilings: {stale.Count}");
lines.Add(string.Empty);
lines.Add($"A Hydra has Parts {limits.HydraParts} and Lines {limits.HydraLines}, "
    + $"and Fused {limits.HydraFused:0.00} or Density {limits.HydraDensity:0.00}. "
    + "A Kraken is a Serpent or Centipede that is also an Octopus or Spider. "
    + $"A Spider has Outgoing {limits.SpiderOutgoing} and Incoming {limits.SpiderIncoming}. "
    + $"A Chameleon has Mutable {limits.ChameleonMutable}. "
    + $"An Octopus has Outgoing {limits.OctopusOutgoing}. "
    + $"A Centipede has Members {limits.CentipedeMembers}. "
    + $"A Serpent has Lines {limits.SerpentLines}. "
    + "Every value is reached at the limit or above, on the merged type. "
    + "A type's verdict is its worst flag, else Colony for several parts and Hermit for one. "
    + $"A hub is a state slot reached from {limits.HubParts} or more parts. "
    + "It is a finding on the slot, never a verdict, so the verdict table shows the hub count beside it.");
lines.Add(string.Empty);
lines.Add("## Verdicts");
lines.Add(string.Empty);
lines.Add("| " + string.Join(" | ", verdictHeader) + " |");
lines.Add("|---|---|---|---:|---:|---:|---:|---:|---:|---:|");
lines.AddRange(shown.Select(summary => $"| `{summary.Name}` | {Numbered(summary.Verdict)} "
    + $"| {FlagText(summary)} "
    + $"| {summary.Hubs.Count} | {summary.Parts} | {summary.Lines} | {summary.Members} | {summary.Mutable} "
    + $"| {summary.Outgoing} | {summary.Incoming} |"));
lines.Add(string.Empty);
lines.Add($"Every other type ({summaries.Count - shown.Count}) is a Hermit: one part, no flag and no hub.");
lines.Add(string.Empty);
lines.Add("## Split types");
lines.Add(string.Empty);
lines.Add("| " + string.Join(" | ", header) + " |");
lines.Add("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
lines.AddRange(split.Select(summary => $"| `{summary.Name}` | {summary.Parts} | {summary.Lines} | {summary.Members} | {summary.Mutable} "
    + $"| {summary.Outgoing} | {summary.Incoming} | {summary.Hubs.Count} | {summary.Crossings} | {summary.Glued:0.00} "
    + $"| {summary.Fused:0.00} | {summary.Density:0.00} | {Numbered(summary.Verdict)} |"));
foreach ((string title, List<string> rows) in sections)
{
    lines.Add(string.Empty);
    lines.Add($"## {Numbered(title)}");
    if (rows.Count > 0)
    {
        lines.Add(string.Empty);
        lines.AddRange(rows.Select(row => "- " + row));
    }
}

string? reportFolder = Path.GetDirectoryName(reportPath);
if (!string.IsNullOrWhiteSpace(reportFolder))
{
    Directory.CreateDirectory(reportFolder);
}

File.WriteAllText(reportPath, string.Join("\n", lines) + "\n", new UTF8Encoding(false));

string[] Strings(JsonElement list) => list.EnumerateArray().Select(item => item.GetString()!).ToArray();
string State(int count, int limit) => count < limit ? "STALE" : count <= limit ? "OK" : enforced ? "ABOVE" : "OVER";
JsonElement sourceConfig = config.GetProperty("sources");
var page = new
{
    project = config.GetProperty("project").GetString(),
    version,
    generation,
    enforced,
    generated = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
    report = Path.GetFileName(reportPath),
    scanned = new { sources = trees.Count, types = summaries.Count, split = split.Count, flagged = shown.Count },
    gates = gates.Select(row => new { status = row.Count > 0 ? "FAIL" : "OK", gate = row.Gate, count = row.Count, meaning = row.Meaning }),
    ceilings = ceilings
        .Select(pair => new { kind = Numbered(pair.Key), key = "ceiling." + pair.Key.ToLowerInvariant(), count = hits[pair.Key].Count, ceiling = pair.Value })
        .Concat(partsCeilings.Select(pair => new { kind = pair.Key, key = "parts." + pair.Key, count = counts.GetValueOrDefault(pair.Key, 1), ceiling = pair.Value }))
        .Select(row => new { row.kind, row.key, row.count, row.ceiling, state = State(row.count, row.ceiling) }),
    verdicts = verdicts.Select(verdict => new { verdict = Numbered(verdict), count = summaries.Count(summary => summary.Verdict == verdict) }),
    findings = sections.Select(section => new
    {
        title = Numbered(section.Title),
        gate = !terms.Contains(section.Title),
        rows = section.Rows,
        cited = cited.TryGetValue(section.Title, out List<(string Type, Measure[] Measures)>? citedRows)
            ? citedRows.Select(row => (object)new
            {
                type = row.Type,
                measures = row.Measures.Select(measure => new { name = measure.Name, value = measure.Value, places = measure.Places }),
            }).ToList()
            : (section.Title == "Above ceiling" ? aboveTerms : staleTerms)
                .Select(row => (object)new { term = row.Term, line = row.Line })
                .ToList(),
    }),
    limits,
    scope = new
    {
        roots = Strings(sourceConfig.GetProperty("roots")),
        extensions = Strings(sourceConfig.GetProperty("extensions")),
        excludeSegments = Strings(sourceConfig.GetProperty("excludeSegments")),
        excludeSuffixes = Strings(sourceConfig.GetProperty("excludeSuffixes")),
        excludePrefixes = Strings(sourceConfig.GetProperty("excludePrefixes")),
        framework = config.GetProperty("helper").GetProperty("framework").GetString(),
        top,
    },
    types = summaries.Select(summary => new
    {
        name = summary.Name,
        parts = summary.Parts,
        lines = summary.Lines,
        members = summary.Members,
        mutable = summary.Mutable,
        outgoing = summary.Outgoing,
        incoming = summary.Incoming,
        shared = summary.Hubs.Count,
        hubs = summary.Hubs,
        crossings = summary.Crossings,
        glued = summary.Glued,
        fused = summary.Fused,
        density = summary.Density,
        verdict = Numbered(summary.Verdict),
    }),
};
File.WriteAllText(pagePath, JsonSerializer.Serialize(page), new UTF8Encoding(false));
return above.Count + stale.Count > 0 ? 3 : 0;

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

static Measure[] Cited(string flag, TypeSummary summary) => flag switch
{
    "Hydra" => [new("parts", summary.Parts), new("lines", summary.Lines), new("fused", summary.Fused), new("density", summary.Density)],
    "Kraken" => [new("lines", summary.Lines), new("members", summary.Members), new("outgoing", summary.Outgoing), new("incoming", summary.Incoming)],
    "Spider" => [new("outgoing", summary.Outgoing), new("incoming", summary.Incoming)],
    "Chameleon" => [new("mutable", summary.Mutable)],
    "Octopus" => [new("outgoing", summary.Outgoing)],
    "Centipede" => [new("members", summary.Members)],
    _ => [new("lines", summary.Lines)],
};

static TypeSummary Summarize(TypeRecord type, Limits limits, int outgoing, int incoming)
{
    MemberRecord[] members = type.Members.Values.ToArray();
    int crossings = members.Sum(member => member.Uses.Count(target => !ReferenceEquals(member.Part, target.Part)));

    HashSet<MemberRecord> hubs = [];
    List<string> hubNames = [];
    List<(string Slot, int Parts)> hubSlots = [];
    foreach (MemberRecord member in members.Where(member => member.IsState))
    {
        HashSet<PartRecord> touching = [member.Part];
        touching.UnionWith(member.UsedBy.Select(user => user.Part));
        if (touching.Count >= limits.HubParts)
        {
            hubs.Add(member);
            hubNames.Add($"`{member.Symbol.Name}` reaches {touching.Count} parts");
            hubSlots.Add((member.Symbol.Name, touching.Count));
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
        hubNames,
        hubSlots,
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

string Numbered(string term) => terms.Contains(term) ? $"({Array.IndexOf(terms, term) + 1}) {term}" : term;

string FlagText(TypeSummary summary) => summary.Flags.Count > 0 ? string.Join(", ", summary.Flags.Select(Numbered)) : "none";

string[] Row(TypeSummary summary) =>
[
    summary.Name,
    summary.Parts.ToString("N0"),
    summary.Lines.ToString("N0"),
    summary.Members.ToString("N0"),
    summary.Mutable.ToString("N0"),
    summary.Outgoing.ToString("N0"),
    summary.Incoming.ToString("N0"),
    summary.Hubs.Count.ToString("N0"),
    summary.Crossings.ToString("N0"),
    summary.Glued.ToString("0.00"),
    summary.Fused.ToString("0.00"),
    summary.Density.ToString("0.00"),
    Numbered(summary.Verdict),
];

string[] VerdictRow(TypeSummary summary) =>
[
    summary.Name,
    Numbered(summary.Verdict),
    FlagText(summary),
    summary.Hubs.Count.ToString("N0"),
    summary.Parts.ToString("N0"),
    summary.Lines.ToString("N0"),
    summary.Members.ToString("N0"),
    summary.Mutable.ToString("N0"),
    summary.Outgoing.ToString("N0"),
    summary.Incoming.ToString("N0"),
];

static void WriteHeading(string title)
{
    Console.WriteLine();
    Console.WriteLine(title);
    Console.WriteLine(new string('-', title.Length));
}

static void WriteTable(IEnumerable<string> lines)
{
    foreach (string line in lines)
    {
        Console.WriteLine(line);
    }
}

static void WriteResult(List<(string Gate, int Count, string Meaning)> rows)
{
    WriteHeading("Result");
    int countWidth = Math.Max(5, rows.Max(row => row.Count.ToString("N0").Length));
    int gateWidth = Math.Max(4, rows.Max(row => row.Gate.Length));
    int meaningWidth = Math.Max(7, rows.Max(row => row.Meaning.Length));
    Console.WriteLine($"{"Status",-6}  {"Count".PadLeft(countWidth)}  {"Gate".PadRight(gateWidth)}  Meaning");
    Console.WriteLine($"{new string('-', 6)}  {new string('-', countWidth)}  {new string('-', gateWidth)}  {new string('-', meaningWidth)}");
    foreach ((string gate, int count, string meaning) in rows)
    {
        Console.WriteLine($"{(count > 0 ? "FAIL" : "OK"),-6}  {count.ToString("N0").PadLeft(countWidth)}  {gate.PadRight(gateWidth)}  {meaning}");
    }

    List<string> failed = rows.Where(row => row.Count > 0).Select(row => $"\"{row.Gate}\"").ToList();
    Console.WriteLine();
    Console.WriteLine(failed.Count == 0
        ? $"PASS: all {rows.Count} gates at 0."
        : $"FAIL: {failed.Count} of {rows.Count} gates above 0. See {string.Join(", ", failed)}.");
}

static IEnumerable<string> TextTable(string[] header, List<string[]> rows)
{
    int[] widths = header.Select((cell, column) => Math.Max(cell.Length, rows.Count == 0 ? 0 : rows.Max(row => row[column].Length))).ToArray();
    bool[] numeric = header.Select((cell, column) => rows.Count > 0 && rows.All(row => row[column] == "-" || double.TryParse(row[column], out _))).ToArray();
    string Line(string[] cells) => string.Join("  ", cells.Select((cell, column) => numeric[column] ? cell.PadLeft(widths[column]) : cell.PadRight(widths[column]))).TrimEnd();
    yield return Line(header);
    yield return string.Join("  ", widths.Select(width => new string('-', width)));
    foreach (string[] row in rows)
    {
        yield return Line(row);
    }
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
    List<string> Hubs,
    List<(string Slot, int Parts)> HubSlots,
    int Crossings,
    double Glued,
    double Fused,
    double Density,
    List<string> Flags,
    string Verdict);

internal sealed record Measure(string Name, object Value)
{
    public int Places => Value is double ? 2 : 0;

    public string Text => Value is double number ? number.ToString("0.00") : Convert.ToString(Value)!;
}

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

function Get-HelperBinary {
    param([string]$DotnetPath, [string]$ProjectName, [string]$TargetFramework)

    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $sdkOutput = & $DotnetPath --version 2>&1
    $ErrorActionPreference = $nativePreference
    if ($LASTEXITCODE -ne 0) {
        throw "The .NET SDK version could not be read.`n$($sdkOutput -join [Environment]::NewLine)"
    }

    $sdkVersion = ([string]($sdkOutput | Select-Object -First 1)).Trim()
    $binderSource = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'AuditBinder.cs'))
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $seed = $script:HelperProject + "`n" + $script:HelperProgram + "`n" + $binderSource + "`n" + $TargetFramework + "`n" + $sdkVersion
        $digest = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($seed))
    }
    finally {
        $sha.Dispose()
    }

    $hash = ([System.BitConverter]::ToString($digest) -replace '-', '').Substring(0, 16).ToLowerInvariant()
    $cacheParent = Join-Path ([System.IO.Path]::GetTempPath()) ($ProjectName + '-AuditObject')
    $cacheFolder = Join-Path $cacheParent $hash
    $binaryPath = Join-Path (Join-Path $cacheFolder 'bin') 'AuditObject.Helper.dll'
    if (Test-Path -LiteralPath $binaryPath -PathType Leaf) {
        return $binaryPath
    }

    Write-AuditLine 'Compiling the object binder once for this SDK...'
    if (Test-Path -LiteralPath $cacheParent) {
        Get-ChildItem -LiteralPath $cacheParent -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }

    $helperFolder = Join-Path $cacheFolder 'helper'
    [System.IO.Directory]::CreateDirectory($helperFolder) | Out-Null
    $encoding = New-Object System.Text.UTF8Encoding($false)
    $projectPath = Join-Path $helperFolder 'AuditObject.Helper.csproj'
    [System.IO.File]::WriteAllText($projectPath, $script:HelperProject.Replace('{TARGET_FRAMEWORK}', $TargetFramework), $encoding)
    [System.IO.File]::WriteAllText((Join-Path $helperFolder 'Program.cs'), $script:HelperProgram, $encoding)
    [System.IO.File]::WriteAllText((Join-Path $helperFolder 'AuditBinder.cs'), $binderSource, $encoding)
    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $buildOutput = & $DotnetPath build $projectPath --configuration Release --nologo --verbosity quiet --output (Join-Path $cacheFolder 'bin') 2>&1
    $ErrorActionPreference = $nativePreference
    if ($LASTEXITCODE -ne 0) {
        Remove-Item -LiteralPath $cacheFolder -Recurse -Force -ErrorAction SilentlyContinue
        throw "The object binder could not be built.`n$($buildOutput -join [Environment]::NewLine)"
    }

    return $binaryPath
}

$projectRoot = Resolve-ProjectRoot -Path $Root
$configPath = Join-Path $PSScriptRoot 'AuditObject.json'
$config = Read-AuditConfig -ConfigPath $configPath
$version = Read-ProjectVersion -ProjectRoot $projectRoot -Config $config

if ($Top -le 0) {
    $Top = [int]$config.console.top
}

$reportFolder = if ([string]::IsNullOrWhiteSpace($ReportDirectory)) { [string]$config.report.directory } else { $ReportDirectory }
if (-not [System.IO.Path]::IsPathRooted($reportFolder)) {
    $reportFolder = Join-Path $projectRoot $reportFolder
}
$reportPath = Join-Path $reportFolder ([string]$config.report.prefix + $version + '.md')

[string[]]$sourceFiles = @(Get-ProjectSourceFiles -ProjectRoot $projectRoot -Config $config)
if ($sourceFiles.Length -eq 0) {
    throw "No tracked source file was enumerated, so the audit would pass vacuously: $projectRoot"
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    throw 'The .NET SDK is required, but dotnet was not found on PATH.'
}

$previousNoLogo = $env:DOTNET_NOLOGO
$previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$temporaryFolder = Join-Path ([System.IO.Path]::GetTempPath()) ($config.project + '-AuditObject-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($temporaryFolder) | Out-Null

try {
    $binaryPath = Get-HelperBinary -DotnetPath $dotnet.Source -ProjectName ([string]$config.project) -TargetFramework ([string]$config.helper.framework)
    $manifestPath = Join-Path $temporaryFolder 'sources.txt'
    $pageDataPath = Join-Path $temporaryFolder 'page.json'
    [System.IO.File]::WriteAllLines($manifestPath, $sourceFiles, [System.Text.UTF8Encoding]::new($false))

    $arguments = @(
        $binaryPath,
        $configPath,
        $projectRoot,
        $manifestPath,
        $version,
        $reportPath,
        $Top,
        (Join-Path $PSScriptRoot 'AuditBinder.json'),
        $pageDataPath
    )

    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $auditOutput = & $dotnet.Source @arguments 2>&1
    $ErrorActionPreference = $nativePreference
    $auditExitCode = $LASTEXITCODE

    if ($auditExitCode -ne 0 -and $auditExitCode -ne 3) {
        throw "The object audit failed.`n$($auditOutput -join [Environment]::NewLine)"
    }

    $relayLines = @($auditOutput | ForEach-Object { [string]$_ })
    $headerRows = @{}
    for ($index = 1; $index -lt $relayLines.Count; $index++) {
        $rule = $relayLines[$index]
        $above = $relayLines[$index - 1]
        if ($rule -notmatch '^-+(  -+)*$' -or $above -eq '' -or $above -match '^-+(  -+)*$') { continue }
        if ($rule -match '^-+$' -and $above.Length -eq $rule.Length) { continue }
        $headerRows[$index - 1] = $true
        if ($index -ge 2) {
            $crown = $relayLines[$index - 2]
            $prior = if ($index -ge 3) { $relayLines[$index - 3] } else { '' }
            if ($crown -ne '' -and $crown -notmatch '^-+(  -+)*$' -and $prior -eq '') { $headerRows[$index - 2] = $true }
        }
    }
    $inResult = $false
    for ($index = 0; $index -lt $relayLines.Count; $index++) {
        $text = $relayLines[$index]
        $next = if ($index + 1 -lt $relayLines.Count) { $relayLines[$index + 1] } else { '' }
        if ($text.StartsWith('Scanned: ', [System.StringComparison]::Ordinal)) {
            Write-AuditLine $text -ForegroundColor DarkGray
        }
        elseif ($text -ne '' -and $next -match '^-+$' -and $next.Length -eq $text.Length) {
            $inResult = $text -eq 'Result'
            Write-AuditLine $text -ForegroundColor Blue
        }
        elseif ($text -match '^-+(  -+)*$') {
            Write-AuditLine $text -ForegroundColor $(if ($headerRows.ContainsKey($index - 1)) { 'Cyan' } else { 'DarkGray' })
        }
        elseif ($headerRows.ContainsKey($index)) {
            Write-AuditLine $text -ForegroundColor Cyan
        }
        elseif ($inResult -and $text -match '^(OK|FAIL) ') {
            Write-AuditLine $text -Lead $Matches[1] -LeadColor $(if ($Matches[1] -eq 'OK') { 'Green' } else { 'Red' })
        }
        elseif ($text.StartsWith('PASS: ', [System.StringComparison]::Ordinal)) {
            Write-AuditLine $text -ForegroundColor Green
        }
        elseif ($text.StartsWith('FAIL: ', [System.StringComparison]::Ordinal)) {
            Write-AuditLine $text -ForegroundColor Red
        }
        else {
            if ($text -eq '') { $inResult = $false }
            Write-AuditLine $text
        }
    }

    # Page output: the helper's page data inside the template AuditObject.html.
    # The helper writes the JSON, so Windows PowerShell 5.1 and pwsh 7 write the same bytes.
    $pageTemplatePath = Join-Path $PSScriptRoot 'AuditObject.html'
    $pagePath = Join-Path $reportFolder ([string]$config.report.prefix + $version + '.html')
    [System.IO.Directory]::CreateDirectory($reportFolder) | Out-Null
    $utf8 = [System.Text.UTF8Encoding]::new($false)
    if (-not (Test-Path -LiteralPath $pageDataPath -PathType Leaf)) {
        throw "The object audit wrote no page data: $pageDataPath"
    }
    $pageData = [System.IO.File]::ReadAllText($pageDataPath, $utf8)
    $pageTemplate = [System.IO.File]::ReadAllText($pageTemplatePath, $utf8)
    foreach ($marker in @('/*__DATA__*/', '/*__HISTORY__*/', '__TITLE__')) {
        if (-not $pageTemplate.Contains($marker)) { throw "The page template lacks the marker $marker : $pageTemplatePath" }
    }

    # The history link and the newest earlier record, written by hand so both PowerShell editions write the same bytes.
    function ConvertTo-HistoryJson {
        param([AllowNull()][object]$Value)

        if ($null -eq $Value) { return 'null' }
        if ($Value -is [string]) {
            $builder = [System.Text.StringBuilder]::new('"')
            foreach ($character in $Value.ToCharArray()) {
                $code = [int]$character
                if ($character -eq '"') { [void]$builder.Append('\"') }
                elseif ($character -eq '\') { [void]$builder.Append('\\') }
                elseif ($code -lt 0x20 -or $code -gt 0x7E -or $character -eq '<' -or $character -eq '>' -or $character -eq '&') { [void]$builder.Append('\u' + $code.ToString('x4')) }
                else { [void]$builder.Append($character) }
            }
            return $builder.Append('"').ToString()
        }
        if ($Value -is [int] -or $Value -is [long]) { return $Value.ToString([System.Globalization.CultureInfo]::InvariantCulture) }
        $pairs = @(foreach ($key in $Value.Keys) { (ConvertTo-HistoryJson ([string]$key)) + ':' + (ConvertTo-HistoryJson $Value[$key]) })
        return '{' + ($pairs -join ',') + '}'
    }

    function ConvertTo-CountMap {
        param([AllowNull()][object]$Counts)

        $map = [ordered]@{}
        if ($null -ne $Counts) {
            foreach ($property in $Counts.PSObject.Properties) { $map[[string]$property.Name] = [long]$property.Value }
        }
        return $map
    }

    $previous = $null
    # The newest history record file other than this version; each is named {version}.json and only that one is read.
    $historyRecordsPath = Join-Path $projectRoot ([string]$config.history.records)
    if ([System.IO.Directory]::Exists($historyRecordsPath)) {
        try {
            $newestPath = $null
            $newestVersion = $null
            foreach ($candidate in [System.IO.Directory]::GetFiles($historyRecordsPath, '*.json')) {
                if (-not $candidate.EndsWith('.json', [System.StringComparison]::OrdinalIgnoreCase)) { continue }
                $name = [System.IO.Path]::GetFileNameWithoutExtension($candidate)
                $candidateVersion = $null
                if ($name -eq $version -or -not [version]::TryParse($name, [ref]$candidateVersion)) { continue }
                if ($null -eq $newestVersion -or $candidateVersion -gt $newestVersion) {
                    $newestPath = $candidate
                    $newestVersion = $candidateVersion
                }
            }
            $newest = $null
            if ($null -ne $newestPath) { $newest = Get-Content -LiteralPath $newestPath -Raw -Encoding UTF8 | ConvertFrom-Json }
            if ($null -ne $newest) {
                $previous = [ordered]@{
                    version = [string]$newest.version
                    date = [string]$newest.date
                    types = [long]$newest.measure.types
                    flags = ConvertTo-CountMap $newest.measure.flags
                    verdicts = ConvertTo-CountMap $newest.measure.verdicts
                }
            }
        }
        catch {
            $previous = $null
        }
    }
    $historyPage = @([System.IO.Directory]::GetFiles($reportFolder, [string]$config.history.pagePrefix + '*.html') |
        Sort-Object -Property @{ Expression = { [System.IO.File]::GetLastWriteTimeUtc($_) }; Descending = $true } | Select-Object -First 1)
    $historyData = [ordered]@{
        page = $(if ($historyPage.Count -gt 0) { [System.IO.Path]::GetFileName($historyPage[0]) } else { '' })
        previous = $previous
    }
    $pageText = $pageTemplate.Replace('__TITLE__', [System.Net.WebUtility]::HtmlEncode([string]$config.project)).Replace('/*__DATA__*/', $pageData.Trim()).Replace('/*__HISTORY__*/', (ConvertTo-HistoryJson $historyData))
    [System.IO.File]::WriteAllText($pagePath, ($pageText -replace "`r`n", "`n"), $utf8)
    Write-AuditLine "Report: $pagePath"
}
finally {
    $env:DOTNET_NOLOGO = $previousNoLogo
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = $previousTelemetry
    if (Test-Path -LiteralPath $temporaryFolder) {
        Remove-Item -LiteralPath $temporaryFolder -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if ($Open -and (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    Invoke-Item -LiteralPath $reportPath
}

if (-not $NoOpen) {
    Start-Process -FilePath $pagePath
}

exit ([int]($auditExitCode -ne 0))
