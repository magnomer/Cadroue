<#
.SYNOPSIS
Audit source names against the project naming rules.

.DESCRIPTION
Reads the project configuration from AuditNames.json and the registered bases,
verbs and exemptions from AuditNames.registry.json, then analyzes the project
source. SyncNames.ps1 alone reads docs-internal and generates that registry, so a
missing or malformed registry fails the run with a request to run it. Every
run writes one Markdown report, the violated names first and the complete
inventory after them, to {report.directory}\{prefix}{version}.md, and the full
result as a page to {report.directory}\{prefix}{version}.html. The page opens in
the default browser unless -NoOpen is given. The version is read from the
configured version file and key. Git and a compatible .NET SDK are required. No project source or test file is written:
SyncNames.ps1 is the separate tool that refreshes the convention tests' registry.

This script carries no project-specific value of its own, so the file is identical
in every project at the same generation.

.PARAMETER Root
Project root to audit. Defaults to the parent of this script's folder.

.PARAMETER NoPause
Do not stop at each console page. Paging is also off when output or input is redirected.

.PARAMETER Open
Open the generated Markdown report after the audit finishes.

.PARAMETER NoOpen
Write the page without opening it.

.PARAMETER Help
Display this help and exit without running the audit. The alias -? is supported.

.EXAMPLE
AuditNames
Audit the current checkout.

.EXAMPLE
AuditNames -Root C:\path\to\project
Audit a specific checkout.
#>
#requires -Version 5.1
# AUDITNAMES - AUDIT GENERATION 21.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the audited outcome changes.
# A generation names the set of checks the audit applies. Two projects on the same generation audit
# the same things and their reports compare directly, whatever else differs between the files. A check
# added, removed, or changed in what it reports is a new generation; wording, plumbing, and refactoring
# leave it alone. Generation 8 checks: missing prefix, component count, base registration, and verb
# ending, plus a descriptive test-method exemption governed by an all-or-nothing test-prefix
# consistency gate, and the generated-name pass that audits a name a source generator emits at the
# declaration it is built from.
# Bases come from AuditNames.registry.json and nothing else, so prose never mints one. Read-AuditRegistry
# validates that file and the helper's ReadRegistry reads it. Its exempt map clears a finding only
# inside the files its row names, or everywhere for a "*" row. A row that matched nothing is
# reported as stale and fails the run, never pruned. The tooling's own files are not audited.
# Generation 7 moved every project-specific value into AuditNames.json, so this file is identical in
# every project at this generation. Generation 8 splits the convention-test settings per audit: the
# hand-written TAuditNameSetting.cs mirrors this configuration, no tooling writes it, and the line
# limit moved to AuditLines. A configuration is total: a missing key is an error, never a default,
# and an unknown key is an error rather than a silent no-op. Generation 9 changes nothing
# the name audit reports; the number rises with the custody and strict audits, which share it.
# Generation 11: nothing the name audit reports changes; the number rises with the truth audit,
# which checks that a deportment field reaches no request, keeps one writer, holds no logic and
# treats no engine data.
# Generation 12: nothing the name audit reports changes; the number rises with the convention tests,
# which bind with no compile error, count chain ceilings in names, count a using or a call on a
# deeper record as a reach, and exempt a contract name only where the type declares the interface.
# Generation 13: nothing this audit reports changes; the number rises with the UI audit, which
# counts every surface markup line that hooks logic into the markup and every surface member
# that is not a constructor.
# Generation 14: nothing this audit reports changes; the number rises with the UI audit, which
# also counts command parameters, member paths and literal tags in surface markup as hooks.
# Generation 15: the name audit also counts every type whose prefix lies outside the turf of its
# project, against a ceiling. The UI audit rises with it.
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
# Generation 21: an out-of-turf type declared public in a sealed turf fails at once, outside the
# prefix ceiling.
[CmdletBinding()]
param(
    [string]$Root,
    [switch]$NoPause,
    [switch]$Open,
    [switch]$NoOpen,
    [Alias('?')]
    [switch]$Help
)

# Under Windows PowerShell 5.1 an advanced script evaluates a parameter default before
# $PSScriptRoot is available to it, so -Root arrives empty there while pwsh 7 resolves it.
# The fallback is applied here, where the automatic variable is always set.
if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Split-Path -Parent $PSScriptRoot
}

. (Join-Path $PSScriptRoot 'AuditNames.config.ps1')

# The registered names come from AuditNames.registry.json, never from docs-internal. The chain is
# docs-internal -> SyncNames.ps1 -> AuditNames.registry.json (this audit and AuditNamesNew.ps1) and
# TAuditNameRegistry.cs and TAuditVerbRegistry.cs (the convention tests). Only SyncNames.ps1 reads docs-internal.

if ($Help) {
    @'
NAME
    AuditNames.ps1

SYNOPSIS
    Audit source names against the project naming rules.

SYNTAX
    AuditNames [-Root <path>] [-NoPause] [-Open] [-NoOpen] [-Help]

OPTIONS
    -Root <path>
        Project root to audit. Defaults to the parent of this script's folder.

    -NoPause
        Do not stop at each console page for a key. Off by itself when output
        or input is redirected.

    -Open
        Open the generated Markdown report after the audit finishes.

    -NoOpen
        Write the page without opening it.

    -Help, -?
        Display this help and exit without running the audit.

OUTPUT
    Five Result gates fail the run: Non-conforming names, one per name with its
    reasons joined, Stale exempt rows, the Turf count above or below its
    ceiling, and Sealed turf. Turf counts every type whose prefix lies outside
    the turf of its project folder, as naming.prefixTurfs maps it. Sealed turf
    takes out of that count every such type in a naming.sealedTurfs folder that
    is public on itself and every containing type, and allows none of them.
    The exit code is 1 when any gate is above 0. Each hit prints as
    path:line [Kind] Name - reason, the
    same line the convention test TAuditName prints. The audit reads the names
    from AuditNames.registry.json and writes no test file. SyncNames.ps1 alone
    reads docs-internal and generates both that registry and the tests'
    TAuditNameRegistry.cs and TAuditVerbRegistry.cs. A missing or malformed registry fails the run with a
    request to run SyncNames.ps1.

REPORTS
    Every run writes one Markdown report to {report.directory}\{prefix}{version}.md:
    the violated names first, then the complete inventory of every name with
    a finding, the exempt registry and the prefix turfs. Every run writes the
    full result as a page next to the Markdown report. It opens in the
    default browser unless -NoOpen is given. The report block of
    AuditNames.json names the folder, the version file and key, and the prefix.

EXAMPLES
    AuditNames
        Audit the current checkout.

    AuditNames -Root C:\path\to\project
        Audit a specific checkout.
'@ | Write-Host
    exit 0
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# dotnet and git write UTF-8. A console still on the OEM code page, as one opened by the dispatcher
# without a profile is, would show every non-ASCII line garbled, so this process reads and writes
# UTF-8. Process-local: the calling console keeps its own code page.
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding

# Console paging. A page is one window of rows; the audit stops at each page boundary and waits
# for a key so the reader can inspect the output before it scrolls away. Any key shows the next
# page, Q shows the rest without stopping. Paging is off when -NoPause is given or when either
# stream is redirected, so a pipeline or a log file never blocks on a key.
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

Write-AuditLine "AUDITNAMES - AUDIT GENERATION $script:AuditGeneration" -ForegroundColor Blue
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

function Read-ProjectVersion {
    # The version file and key come from the report block of the configuration.
    param(
        [string]$ProjectRoot,
        [string]$VersionFile,
        [string]$VersionKey
    )

    $versionPath = Join-AuditPath -ProjectRoot $ProjectRoot -Relative $VersionFile
    if (-not (Test-Path -LiteralPath $versionPath -PathType Leaf)) {
        throw "Version file was not found: $versionPath"
    }

    try {
        $versionData = Get-Content -LiteralPath $versionPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "Version file is not valid JSON: $versionPath`n$($_.Exception.Message)"
    }

    $version = [string]$versionData.($VersionKey)
    if ($version -notmatch '^\d+\.\d+\.\d+$') {
        throw "Version must contain three numeric components: '$version'"
    }

    return $version
}

function Test-ExcludedRelativePath {
    param(
        [string]$RelativePath,
        [Parameter(Mandatory = $true)]$Config
    )

    $segments = $RelativePath -split '[\\/]'
    foreach ($segment in $segments) {
        if ($Config.sources.excludeSegments -contains $segment) {
            return $true
        }
    }

    $fileName = $segments[$segments.Length - 1]

    # The tooling carries names it does not own, and a project that imports it did not choose them,
    # so the audit does not audit itself.
    if ($Config.sources.selfExclude -contains $fileName) {
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

function Get-ProjectSourceFiles {
    param(
        [string]$ProjectRoot,
        [Parameter(Mandatory = $true)]$Config
    )

    $git = Get-Command git -ErrorAction SilentlyContinue
    if ($null -eq $git) {
        throw 'Git is required to enumerate project source files, but git was not found on PATH.'
    }

    if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot '.git'))) {
        throw "The project root is not a Git working tree: $ProjectRoot"
    }

    $lsArguments = @('-c', 'core.quotePath=false', '-C', $ProjectRoot,
        'ls-files', '--cached', '--others', '--exclude-standard', '--') + @($Config.sources.include | ForEach-Object { ':(icase)' + $_ })
    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $gitOutput = & $git.Source @lsArguments 2>&1
    $ErrorActionPreference = $nativePreference
    $gitExitCode = $LASTEXITCODE
    if ($gitExitCode -ne 0) {
        throw "Git could not enumerate source files.`n$($gitOutput -join [Environment]::NewLine)"
    }

    $separator = [string][System.IO.Path]::DirectorySeparatorChar
    $rootPrefix = $ProjectRoot.TrimEnd($script:PathSeparators) + $separator
    $files = New-Object 'System.Collections.Generic.List[string]'
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($entry in $gitOutput) {
        $relativePath = ([string]$entry).Trim()
        if ([string]::IsNullOrWhiteSpace($relativePath) -or (Test-ExcludedRelativePath -RelativePath $relativePath -Config $config)) {
            continue
        }

        $platformRelativePath = $relativePath.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar)
        $fullPath = $rootPrefix + $platformRelativePath
        if (-not $seen.Add($fullPath) -or -not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            continue
        }

        $files.Add($fullPath)
    }

    return @($files.ToArray() | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } })
}

function Write-AuditHelper {
    param(
        [string]$HelperFolder,
        [string]$TargetFramework
    )

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
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" />
  </ItemGroup>
</Project>
'@
    $projectContent = $projectContent.Replace('{TARGET_FRAMEWORK}', $TargetFramework)

    $programContent = @'
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal sealed record NameRecord(
    string Path,
    string Id,
    string? ParentId,
    string Name,
    string Kind,
    int Line,
    int Position,
    int Anchor);

// A name carries one reason text, worded and joined exactly as the convention test words it, and
// one primary category: the first check that failed. The category splits the non-conforming names
// into a whole, so the per-category totals always sum to the one gating count.
internal sealed record NameAnalysis(
    string[] Components,
    int Count,
    string? Reason,
    string? Category)
{
    public static readonly NameAnalysis Ignored = new([], 0, null, null);

    public static NameAnalysis Rejected(string reason, string category) => new([], 0, reason, category);
}

internal sealed record NameHit(
    string Path,
    int Line,
    string Kind,
    string Name,
    string Reason,
    string Category);

internal sealed class AuditNode
{
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required int Line { get; init; }
    public required int Position { get; init; }
    public required string[] Components { get; init; }
    public required int ComponentCount { get; init; }
    public required bool IsFinding { get; init; }
    public required bool IsExempt { get; init; }
    public required string? Reason { get; init; }
    public List<AuditNode> Children { get; } = [];
    public Dictionary<string, AuditNode> ChildMap { get; } = new(StringComparer.Ordinal);
}

internal sealed record FileAudit(
    string Path,
    string Directory,
    string FileName,
    AuditNode[] Roots,
    int NameCount,
    int ReviewCount,
    int HitCount);

// The configuration is read here rather than handed over as arguments, so this implementation and
// the convention tests reach the same document by their own separate routes. The shell script has
// already validated it; a key missing at this point is a defect and throws.
internal sealed class AuditConfig
{
    private readonly JsonDocument document;

    private AuditConfig(JsonDocument document) => this.document = document;

    public static AuditConfig Read(string path) => new(JsonDocument.Parse(File.ReadAllText(path)));

    public JsonElement Node(string key)
    {
        JsonElement node = document.RootElement;
        foreach (string segment in key.Split('.'))
        {
            if (!node.TryGetProperty(segment, out node))
            {
                throw new InvalidOperationException($"The audit configuration has no key '{key}'.");
            }
        }

        return node;
    }

    public string Text(string key) => Node(key).GetString()
        ?? throw new InvalidOperationException($"The audit configuration key '{key}' is not a string.");

    public int Number(string key) => Node(key).GetInt32();

    public string[] List(string key) => Node(key)
        .EnumerateArray()
        .Select(item => item.GetString() ?? string.Empty)
        .ToArray();

    public Dictionary<string, HashSet<string>> Map(string key)
    {
        Dictionary<string, HashSet<string>> map = new(StringComparer.Ordinal);
        foreach (JsonProperty entry in Node(key).EnumerateObject())
        {
            HashSet<string> members = new(StringComparer.Ordinal);
            foreach (JsonElement item in entry.Value.EnumerateArray())
            {
                string? member = item.GetString();
                if (!string.IsNullOrWhiteSpace(member))
                {
                    members.Add(member);
                }
            }

            map[entry.Name] = members;
        }

        return map;
    }
}

internal static class Program
{
    private static readonly CSharpParseOptions ParseOptions = new(
        languageVersion: LanguageVersion.Preview,
        documentationMode: DocumentationMode.None,
        kind: SourceCodeKind.Regular);

    // Assigned once from the configuration before any analysis runs. This is a single-shot console
    // program, so the settings are held statically rather than threaded through every call.
    private static Regex ComponentPattern = null!;
    private static string[] Prefixes = null!;
    private static Dictionary<string, string[]> PrefixTurfs = null!;
    private static int PrefixCeiling;
    private static string[] SealedTurfs = null!;
    private static string TestPrefix = null!;
    private static int ComponentLimit;
    private static int ComponentReview;
    private static string XamlNamespace = null!;
    private static string[] TestAttributes = null!;
    private static string[] GeneratedAttributes = null!;
    private static string[] ExternalAttributes = null!;
    private static string[] CommandAttributes = null!;
    private static string CommandCancelArgument = null!;
    private static string CommandAsyncSuffix = null!;
    private static string CommandSuffix = null!;
    private static string CommandCancelSuffix = null!;
    private static string Project = null!;
    private static int Generation;
    // The names are read from the generated AuditNames.registry.json, never from docs-internal: only
    // SyncNames.ps1 reads docs-internal. The document and marker names below only word the report,
    // telling the reader where a name is registered.
    private const string BasesDocument = "ListObject.md";
    private const string VerbsDocument = "ListVerb.md";
    private const string ExemptMarker = "AUDIT:EXEMPT";
    private const int ItemLimit = 50;
    private const string PrefixReason = "Prefix";
    private const string ShapeReason = "Shape";
    private const string BaseReason = "Base";
    private const string VerbReason = "Verb";
    private const string CountReason = "Count";
    private static readonly string[] ReasonOrder = [PrefixReason, ShapeReason, BaseReason, VerbReason, CountReason];

    private static void ApplyConfig(AuditConfig config)
    {
        ComponentPattern = new Regex(
            config.Text("naming.componentPattern"),
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        Prefixes = config.List("naming.prefixes");
        PrefixTurfs = config.Node("naming.prefixTurfs").EnumerateObject().ToDictionary(
            entry => entry.Name,
            entry => entry.Value.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray(),
            StringComparer.Ordinal);
        PrefixCeiling = config.Number("naming.prefixCeiling");
        SealedTurfs = config.List("naming.sealedTurfs");
        TestPrefix = config.Text("naming.testPrefix");
        ComponentLimit = config.Number("naming.componentLimit");
        ComponentReview = config.Number("naming.componentReview");
        XamlNamespace = config.Text("platform.xamlNamespace");
        TestAttributes = config.List("platform.testAttributes");
        GeneratedAttributes = config.List("platform.generatedAttributes");
        ExternalAttributes = config.List("platform.externalAttributes");
        CommandAttributes = config.List("platform.commandAttributes");
        CommandCancelArgument = config.Text("platform.commandCancelArgument");
        CommandAsyncSuffix = config.Text("platform.commandAsyncSuffix");
        CommandSuffix = config.Text("platform.commandSuffix");
        CommandCancelSuffix = config.Text("platform.commandCancelSuffix");
        Project = config.Text("project");
        Generation = config.Number("generation");
        MethodKinds = new HashSet<string>(config.List("kinds.method"), StringComparer.Ordinal);
        VerbForbiddenKinds = new HashSet<string>(config.List("kinds.verbForbidden"), StringComparer.Ordinal);
        FrameworkContracts = config.Map("platform.frameworkContracts");
    }

    public static int Main(string[] args)
    {
        if (args.Length != 7)
        {
            Console.Error.WriteLine("Expected configuration path, registry path, project root, manifest, version, report path, and page data path.");
            return 2;
        }

        string configPath = args[0];
        string registryPath = args[1];
        string projectRoot = args[2];
        string manifestPath = args[3];
        string version = args[4];
        string outputPath = args[5];
        string pageDataPath = args[6];

        try
        {
            AuditConfig config = AuditConfig.Read(configPath);
            ApplyConfig(config);

            (HashSet<string> bases, HashSet<string> verbs, ExemptRegistry exempt) = ReadRegistry(registryPath);
            List<NameRecord> records = [];
            List<SyntaxTree> trees = [];
            List<string> xamlPaths = [];

            foreach (string sourcePath in File.ReadLines(manifestPath))
            {
                if (string.IsNullOrWhiteSpace(sourcePath))
                {
                    continue;
                }

                if (sourcePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    trees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(sourcePath), ParseOptions, sourcePath));
                }
                else if (sourcePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
                {
                    xamlPaths.Add(sourcePath);
                }
            }

            IndexTypes(trees);
            foreach (SyntaxTree tree in trees)
            {
                records.AddRange(ReadCSharp(tree));
            }

            foreach (string xamlPath in xamlPaths)
            {
                records.AddRange(ReadXaml(xamlPath));
            }

            (List<string> turfs, List<string> sealedHits) = ReadTurfs(trees, projectRoot);
            int failures = WriteReport(
                records, bases, verbs, exempt, turfs, sealedHits, projectRoot, version, outputPath, pageDataPath);
            return failures > 0 ? 3 : 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.ToString());
            return 1;
        }
    }

    // A turf is the project folder a type is declared in. Its prefix must be one the turf allows.
    // A name with no prefix is left to the name check, so it is never counted twice.
    // In a sealed turf, an outside type public on itself and every containing type is a hard hit
    // of its own, kept out of the turf count, so the ceiling never absorbs it.
    private static (List<string> Turfs, List<string> Sealed) ReadTurfs(IEnumerable<SyntaxTree> trees, string projectRoot)
    {
        List<(string Path, int Line, string Name, string Text, bool Sealed)> hits = [];
        foreach (SyntaxTree tree in trees)
        {
            string relative = Path.GetRelativePath(projectRoot, tree.FilePath).Replace('\\', '/');
            string? turf = PrefixTurfs.Keys
                .FirstOrDefault(key => relative.StartsWith(key + "/", StringComparison.OrdinalIgnoreCase));
            if (turf is null)
            {
                continue;
            }

            string[] allowed = PrefixTurfs[turf];
            bool sealedTurf = SealedTurfs.Contains(turf, StringComparer.Ordinal);
            foreach (SyntaxNode node in tree.GetRoot().DescendantNodes())
            {
                (SyntaxToken identifier, string kind) = node switch
                {
                    BaseTypeDeclarationSyntax type => (type.Identifier, type.Kind().ToString()),
                    DelegateDeclarationSyntax shape => (shape.Identifier, "Delegate"),
                    _ => (default, string.Empty),
                };
                string name = identifier.ValueText;
                string? prefix = kind.Length == 0 ? null : ReadPrefix(name);
                if (prefix is null || allowed.Contains(prefix, StringComparer.Ordinal))
                {
                    continue;
                }

                int line = identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                bool exposed = sealedTurf && IsPublicChain(node);
                hits.Add((relative, line, name,
                    $"{relative}:{line} [{kind}] {name} - prefix `{prefix}` is outside the turf of {turf} "
                    + $"({string.Join(", ", allowed.Select(item => $"`{item}`"))})"
                    + (exposed ? " and public in a sealed turf" : ""),
                    exposed));
            }
        }

        List<(string Path, int Line, string Name, string Text, bool Sealed)> ordered = hits
            .OrderBy(hit => hit.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(hit => hit.Line)
            .ThenBy(hit => hit.Name, StringComparer.Ordinal)
            .ToList();
        return (
            ordered.Where(hit => !hit.Sealed).Select(hit => hit.Text).ToList(),
            ordered.Where(hit => hit.Sealed).Select(hit => hit.Text).ToList());
    }

    // Public is read from the syntax: the declaration and every type around it carry the keyword.
    private static bool IsPublicChain(SyntaxNode node) => node.AncestorsAndSelf()
        .OfType<MemberDeclarationSyntax>()
        .Where(member => member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
        .All(member => member.Modifiers.Any(SyntaxKind.PublicKeyword));

    private static IEnumerable<NameRecord> ReadCSharp(SyntaxTree tree)
    {
        string sourcePath = tree.FilePath;
        SyntaxNode root = tree.GetRoot();
        List<(SyntaxNode Node, SyntaxToken Identifier, string Kind, string? Generated)> candidates = [];

        foreach (SyntaxNode node in root.DescendantNodesAndSelf())
        {
            switch (node)
            {
                case BaseTypeDeclarationSyntax typeDeclaration
                    when !HasGeneratedAttribute(typeDeclaration.AttributeLists):
                    candidates.Add((typeDeclaration, typeDeclaration.Identifier, typeDeclaration.Kind().ToString(), null));
                    break;

                case ParameterSyntax parameter
                    when parameter.Parent?.Parent is RecordDeclarationSyntax recordDeclaration &&
                         !HasGeneratedAttribute(recordDeclaration.AttributeLists):
                    candidates.Add((parameter, parameter.Identifier, "RecordProperty", null));
                    break;

                case DelegateDeclarationSyntax delegateDeclaration
                    when !HasGeneratedAttribute(delegateDeclaration.AttributeLists):
                    candidates.Add((delegateDeclaration, delegateDeclaration.Identifier, "Delegate", null));
                    break;

                case MethodDeclarationSyntax methodDeclaration
                    when !IsExternal(
                        methodDeclaration.Modifiers,
                        methodDeclaration.ExplicitInterfaceSpecifier,
                        methodDeclaration.AttributeLists) &&
                        !ImplementsFrameworkContract(methodDeclaration, methodDeclaration.Identifier.ValueText):
                    candidates.Add((
                        methodDeclaration,
                        methodDeclaration.Identifier,
                        HasAnyAttribute(methodDeclaration.AttributeLists, TestAttributes)
                            ? "TestMethod"
                            : "Method", null));
                    AddGeneratedCommands(candidates, methodDeclaration);
                    break;

                case TupleElementSyntax tupleElement:
                    candidates.Add((tupleElement, tupleElement.Identifier, "TupleElement", null));
                    break;

                case TypeParameterSyntax typeParameter:
                    candidates.Add((typeParameter, typeParameter.Identifier, "TypeParameter", null));
                    break;

                case AnonymousObjectMemberDeclaratorSyntax anonymousMember:
                    AddAnonymousMember(candidates, anonymousMember);
                    break;

                case LocalFunctionStatementSyntax localFunction:
                    candidates.Add((localFunction, localFunction.Identifier, "LocalFunction", null));
                    break;

                case PropertyDeclarationSyntax propertyDeclaration
                    when !IsExternal(
                        propertyDeclaration.Modifiers,
                        propertyDeclaration.ExplicitInterfaceSpecifier,
                        propertyDeclaration.AttributeLists) &&
                        !ImplementsFrameworkContract(propertyDeclaration, propertyDeclaration.Identifier.ValueText):
                    candidates.Add((propertyDeclaration, propertyDeclaration.Identifier, "Property", null));
                    break;

                case EventDeclarationSyntax eventDeclaration
                    when !IsExternal(
                        eventDeclaration.Modifiers,
                        eventDeclaration.ExplicitInterfaceSpecifier,
                        eventDeclaration.AttributeLists) &&
                        !ImplementsFrameworkContract(eventDeclaration, eventDeclaration.Identifier.ValueText):
                    candidates.Add((eventDeclaration, eventDeclaration.Identifier, "Event", null));
                    break;

                case VariableDeclaratorSyntax variable:
                    AddVariable(candidates, variable);
                    break;

                case EnumMemberDeclarationSyntax enumMember:
                    candidates.Add((enumMember, enumMember.Identifier, "EnumMember", null));
                    break;
            }
        }

        Dictionary<SyntaxNode, string> ids = new(ReferenceEqualityComparer.Instance);
        foreach (var candidate in candidates)
        {
            if (candidate.Generated is null)
            {
                ids[candidate.Node] = NodeKey(candidate.Node);
            }
        }

        foreach (var candidate in candidates.OrderBy(item => item.Node.SpanStart))
        {
            SyntaxNode node = candidate.Node;
            SyntaxToken identifier = candidate.Identifier;
            string kind = candidate.Kind;
            string name = candidate.Generated ?? identifier.ValueText;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            string ownId = candidate.Generated is null
                ? ids[node]
                : $"{NodeKey(node)}:{candidate.Generated}";

            string? parentId = null;
            if (candidate.Generated is not null)
            {
                ids.TryGetValue(node, out parentId);
            }
            else
            {
                for (SyntaxNode? parent = node.Parent; parent is not null; parent = parent.Parent)
                {
                    if (ids.TryGetValue(parent, out string? governedId))
                    {
                        parentId = governedId;
                        break;
                    }
                }
            }

            SyntaxTree syntaxTree = identifier.SyntaxTree ?? node.SyntaxTree ?? tree;
            FileLinePositionSpan lineSpan = syntaxTree.GetLineSpan(identifier.Span);
            yield return new NameRecord(
                sourcePath,
                ownId,
                parentId,
                name,
                kind,
                lineSpan.StartLinePosition.Line + 1,
                identifier.SpanStart,
                node.SpanStart);
        }
    }

    private static void AddGeneratedCommands(
        ICollection<(SyntaxNode Node, SyntaxToken Identifier, string Kind, string? Generated)> candidates,
        MethodDeclarationSyntax method)
    {
        if (string.IsNullOrWhiteSpace(method.Identifier.ValueText))
        {
            return;
        }

        AttributeSyntax? relayCommand = null;
        foreach (AttributeSyntax attribute in method.AttributeLists.SelectMany(list => list.Attributes))
        {
            string attributeName = attribute.Name.ToString();
            int separator = attributeName.LastIndexOf('.');
            if (separator >= 0)
            {
                attributeName = attributeName[(separator + 1)..];
            }

            if (attributeName.EndsWith("Attribute", StringComparison.Ordinal))
            {
                attributeName = attributeName[..^"Attribute".Length];
            }

            if (CommandAttributes.Contains(attributeName, StringComparer.Ordinal))
            {
                relayCommand = attribute;
                break;
            }
        }

        if (relayCommand is null)
        {
            return;
        }

        // The generator drops a trailing Async and appends Command, so the name that actually
        // exists is never the one written here. It is a data member, so it is governed as one.
        string stem = method.Identifier.ValueText;
        if (stem.EndsWith(CommandAsyncSuffix, StringComparison.Ordinal) && stem.Length > CommandAsyncSuffix.Length)
        {
            stem = stem[..^CommandAsyncSuffix.Length];
        }

        candidates.Add((method, method.Identifier, "GeneratedCommand", stem + CommandSuffix));

        foreach (AttributeArgumentSyntax argument in relayCommand.ArgumentList?.Arguments ?? default)
        {
            if (argument.NameEquals?.Name.Identifier.ValueText == CommandCancelArgument &&
                argument.Expression.IsKind(SyntaxKind.TrueLiteralExpression))
            {
                candidates.Add((method, method.Identifier, "GeneratedCommand", stem + CommandCancelSuffix));
            }
        }
    }

    // An anonymous type declares a property per member. The name is written by a Name = clause when
    // the member carries one, and otherwise inferred from the trailing identifier of the expression.
    // Either way a member exists and is a data member, so it is governed as one.
    private static void AddAnonymousMember(
        ICollection<(SyntaxNode Node, SyntaxToken Identifier, string Kind, string? Generated)> candidates,
        AnonymousObjectMemberDeclaratorSyntax member)
    {
        if (member.NameEquals is not null)
        {
            candidates.Add((member, member.NameEquals.Name.Identifier, "AnonymousMember", null));
            return;
        }

        SyntaxToken? inferred = member.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier,
            _ => null
        };

        if (inferred is not null)
        {
            candidates.Add((member, inferred.Value, "AnonymousMember", null));
        }
    }

    private static void AddVariable(
        ICollection<(SyntaxNode Node, SyntaxToken Identifier, string Kind, string? Generated)> candidates,
        VariableDeclaratorSyntax variable)
    {
        if (variable.Parent is not VariableDeclarationSyntax declaration)
        {
            return;
        }

        switch (declaration.Parent)
        {
            case EventFieldDeclarationSyntax eventField
                when !HasGeneratedAttribute(eventField.AttributeLists) &&
                     !ImplementsFrameworkContract(eventField, variable.Identifier.ValueText):
                candidates.Add((variable, variable.Identifier, "EventField", null));
                break;

            case FieldDeclarationSyntax field
                when !HasGeneratedAttribute(field.AttributeLists):
                candidates.Add((variable, variable.Identifier, "Field", null));
                break;
        }
    }

    private static Dictionary<string, HashSet<string>> FrameworkContracts = null!;

    private static Dictionary<string, List<TypeDeclarationSyntax>> TypeParts = null!;

    private static Dictionary<string, List<string>> TypeKeysByName = null!;

    private static void IndexTypes(IEnumerable<SyntaxTree> trees)
    {
        TypeParts = new(StringComparer.Ordinal);
        TypeKeysByName = new(StringComparer.Ordinal);
        foreach (SyntaxTree tree in trees)
        {
            foreach (TypeDeclarationSyntax type in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                string key = TypeKey(type);
                if (!TypeParts.TryGetValue(key, out List<TypeDeclarationSyntax>? parts))
                {
                    parts = [];
                    TypeParts[key] = parts;
                    string simple = type.Identifier.ValueText;
                    if (!TypeKeysByName.TryGetValue(simple, out List<string>? keys))
                    {
                        keys = [];
                        TypeKeysByName[simple] = keys;
                    }

                    keys.Add(key);
                }

                parts.Add(type);
            }
        }
    }

    private static string TypeKey(TypeDeclarationSyntax type)
    {
        List<string> segments = [];
        for (SyntaxNode? current = type; current is not null; current = current.Parent)
        {
            if (current is TypeDeclarationSyntax declaration)
            {
                segments.Add($"{declaration.Identifier.ValueText}`{declaration.TypeParameterList?.Parameters.Count ?? 0}");
            }
            else if (current is BaseNamespaceDeclarationSyntax space)
            {
                segments.Add(space.Name.ToString());
            }
        }

        segments.Reverse();
        return string.Join(".", segments);
    }

    private static bool ImplementsFrameworkContract(SyntaxNode node, string name)
    {
        TypeDeclarationSyntax? type = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        return type is not null && DeclaresContract(TypeKey(type), name, new HashSet<string>(StringComparer.Ordinal));
    }

    private static bool DeclaresContract(string key, string name, HashSet<string> visited)
    {
        if (!visited.Add(key) || !TypeParts.TryGetValue(key, out List<TypeDeclarationSyntax>? parts))
        {
            return false;
        }

        foreach (TypeDeclarationSyntax part in parts)
        {
            if (part.BaseList is null)
            {
                continue;
            }

            foreach (BaseTypeSyntax baseType in part.BaseList.Types)
            {
                string simple = InterfaceSimpleName(baseType.Type);
                if (FrameworkContracts.TryGetValue(simple, out HashSet<string>? members) && members.Contains(name))
                {
                    return true;
                }

                if (TypeKeysByName.TryGetValue(simple, out List<string>? keys) &&
                    keys.Any(baseKey => DeclaresContract(baseKey, name, visited)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string InterfaceSimpleName(TypeSyntax type) => type switch
    {
        GenericNameSyntax generic => generic.Identifier.ValueText,
        QualifiedNameSyntax qualified => InterfaceSimpleName(qualified.Right),
        AliasQualifiedNameSyntax alias => InterfaceSimpleName(alias.Name),
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        _ => string.Empty
    };

    private static bool IsExternal(
        SyntaxTokenList modifiers,
        ExplicitInterfaceSpecifierSyntax? explicitInterface,
        SyntaxList<AttributeListSyntax> attributes)
    {
        if (explicitInterface is not null ||
            modifiers.Any(token => token.IsKind(SyntaxKind.OverrideKeyword)) ||
            modifiers.Any(token => token.IsKind(SyntaxKind.ExternKeyword)))
        {
            return true;
        }

        return HasGeneratedAttribute(attributes) ||
               HasAnyAttribute(attributes, ExternalAttributes);
    }

    private static bool HasGeneratedAttribute(SyntaxList<AttributeListSyntax> attributes) =>
        HasAnyAttribute(attributes, GeneratedAttributes);

    private static bool HasAnyAttribute(SyntaxList<AttributeListSyntax> attributes, string[] expectedNames)
    {
        foreach (string expectedName in expectedNames)
        {
            if (HasAttribute(attributes, expectedName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributes, string expectedName)
    {
        foreach (AttributeSyntax attribute in attributes.SelectMany(list => list.Attributes))
        {
            string name = attribute.Name.ToString();
            int separator = name.LastIndexOf('.');
            if (separator >= 0)
            {
                name = name[(separator + 1)..];
            }

            if (name.EndsWith("Attribute", StringComparison.Ordinal))
            {
                name = name[..^"Attribute".Length];
            }

            if (string.Equals(name, expectedName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string NodeKey(SyntaxNode node) =>
        $"cs:{node.RawKind}:{node.SpanStart}:{node.Span.Length}";

    private static IEnumerable<NameRecord> ReadXaml(string sourcePath)
    {
        using FileStream stream = File.OpenRead(sourcePath);
        using XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreComments = false,
            IgnoreWhitespace = false
        });

        XDocument document = XDocument.Load(reader, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        if (document.Root is null)
        {
            yield break;
        }

        int position = 0;
        foreach (NameRecord record in ReadXamlElement(document.Root, sourcePath, null, () => ++position))
        {
            yield return record;
        }
    }

    private static IEnumerable<NameRecord> ReadXamlElement(
        XElement element,
        string sourcePath,
        string? parentId,
        Func<int> nextPosition)
    {
        string? currentParentId = parentId;
        XAttribute? nameAttribute = element.Attributes().FirstOrDefault(attribute =>
            attribute.Name.LocalName == "Name" &&
            (string.IsNullOrEmpty(attribute.Name.NamespaceName) ||
             attribute.Name.NamespaceName == XamlNamespace));

        if (nameAttribute is not null && !string.IsNullOrWhiteSpace(nameAttribute.Value))
        {
            int position = nextPosition();
            string id = $"xaml:{position}";
            IXmlLineInfo? lineInfo = nameAttribute as IXmlLineInfo;
            int line = lineInfo is not null && lineInfo.HasLineInfo()
                ? lineInfo.LineNumber
                : 0;

            yield return new NameRecord(
                sourcePath,
                id,
                parentId,
                nameAttribute.Value,
                "XamlName",
                line,
                position,
                position);
            currentParentId = id;
        }

        foreach (XElement child in element.Elements())
        {
            foreach (NameRecord record in ReadXamlElement(child, sourcePath, currentParentId, nextPosition))
            {
                yield return record;
            }
        }
    }

    // The exempt registry is the exempt map of AuditNames.registry.json, read by ReadRegistry: one row
    // per externally imposed name, where the scope is the file names the row grants the name in,
    // or "*" for a mechanism that is universal by spelling. A row clears a finding only inside
    // its scope, so the same word stays a violation everywhere else.
    private sealed class ExemptRegistry
    {
        private readonly Dictionary<string, HashSet<string>> scopes =
            new(StringComparer.Ordinal);

        private readonly HashSet<string> used = new(StringComparer.Ordinal);

        public int RowCount => scopes.Count;

        public void Add(string name, IEnumerable<string> files)
        {
            if (!scopes.TryGetValue(name, out HashSet<string>? set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                scopes[name] = set;
            }

            foreach (string file in files)
            {
                set.Add(file);
            }
        }

        public bool Grants(string name, string sourcePath)
        {
            if (!scopes.TryGetValue(name, out HashSet<string>? files))
            {
                return false;
            }

            string fileName = Path.GetFileName(sourcePath);
            bool granted = files.Contains("*") || files.Contains(fileName);
            if (granted)
            {
                used.Add(name);
            }

            return granted;
        }

        public IReadOnlyList<string> StaleNames() => scopes.Keys
            .Where(name => !used.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    // The registry is generated by SyncNames.ps1 from docs-internal and validated by the shell script
    // before this runs, so a fault found here is a defect, and the message still names the cure.
    private static (HashSet<string> Bases, HashSet<string> Verbs, ExemptRegistry Exempt) ReadRegistry(string path)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement root = document.RootElement;
            HashSet<string> bases = ReadRegistryWords(root.GetProperty("bases"));
            HashSet<string> verbs = ReadRegistryWords(root.GetProperty("verbs"));
            ExemptRegistry exempt = new();
            foreach (JsonProperty row in root.GetProperty("exempt").EnumerateObject())
            {
                string[] files = row.Value.EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .Where(file => file.Length > 0)
                    .ToArray();
                if (files.Length > 0)
                {
                    exempt.Add(row.Name, files);
                }
            }

            if (bases.Count == 0 || verbs.Count == 0)
            {
                throw new InvalidOperationException("The registry holds no base or no verb.");
            }

            return (bases, verbs, exempt);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException or IOException)
        {
            throw new InvalidOperationException(
                $"The name registry '{path}' is missing or malformed: {exception.Message} " +
                "Run SyncNames.ps1 to generate it.", exception);
        }
    }

    private static HashSet<string> ReadRegistryWords(JsonElement list)
    {
        HashSet<string> words = new(StringComparer.Ordinal);
        foreach (JsonElement item in list.EnumerateArray())
        {
            string? word = item.GetString();
            if (!string.IsNullOrEmpty(word))
            {
                words.Add(word);
            }
        }

        return words;
    }

    private static HashSet<string> MethodKinds = null!;

    private static HashSet<string> VerbForbiddenKinds = null!;

    private static NameAnalysis AnalyzeName(
        string name,
        string kind,
        ISet<string> bases,
        ISet<string> verbs,
        bool anyTestPrefixed)
    {
        if (string.Equals(kind, "TestMethod", StringComparison.Ordinal))
        {
            // A test method carries a free-form scenario description, never an object base or
            // verb. While no test method uses the T prefix the whole suite is descriptive and
            // exempt; once any test method adopts T, every test method must carry it.
            if (!anyTestPrefixed)
            {
                return NameAnalysis.Ignored;
            }

            string? testPrefix = ReadPrefix(name.TrimStart('_'));
            if (testPrefix is null)
            {
                return NameAnalysis.Rejected(
                    $"test method carries no prefix while other test methods use the {TestPrefix} prefix",
                    PrefixReason);
            }

            if (!string.Equals(testPrefix, TestPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return NameAnalysis.Rejected(
                    $"test method must use the {TestPrefix} prefix, not `{testPrefix}`",
                    PrefixReason);
            }

            return NameAnalysis.Ignored;
        }

        string working = name.TrimStart('_');
        string? prefix = ReadPrefix(working);
        if (prefix is null)
        {
            // A codebase-owned name that survived the external/generated/framework-contract
            // filters but carries no prefix is itself a violation, not something to skip.
            return NameAnalysis.Rejected("missing required prefix", PrefixReason);
        }

        string remainder = working[prefix.Length..];
        if (string.IsNullOrWhiteSpace(remainder))
        {
            return NameAnalysis.Rejected("no base after the prefix", ShapeReason);
        }

        List<string> components = [];

        foreach (string segment in remainder.Split('_'))
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return NameAnalysis.Rejected("an underscore leaves an empty component", ShapeReason);
            }

            MatchCollection matches = ComponentPattern.Matches(segment);
            string rebuilt = string.Concat(matches.Cast<Match>().Select(match => match.Value));
            if (!string.Equals(rebuilt, segment, StringComparison.Ordinal))
            {
                return NameAnalysis.Rejected($"`{segment}` does not split into PascalCase components", ShapeReason);
            }

            components.AddRange(matches.Cast<Match>().Select(match => match.Value));
        }

        if (components.Count == 0)
        {
            return NameAnalysis.Rejected("no base after the prefix", ShapeReason);
        }

        List<string> reasons = [];
        string? category = null;
        string baseName = components[0];
        if (!bases.Contains(baseName))
        {
            reasons.Add($"unregistered base `{baseName}`");
            category ??= BaseReason;
        }

        string last = components[^1];
        bool lastIsVerb = verbs.Contains(last);
        if (MethodKinds.Contains(kind) && !lastIsVerb)
        {
            reasons.Add($"method does not end in a registered verb (`{last}`)");
            category ??= VerbReason;
        }
        else if (VerbForbiddenKinds.Contains(kind) && lastIsVerb)
        {
            reasons.Add($"data or type name ends in a registered verb (`{last}`)");
            category ??= VerbReason;
        }

        if (components.Count > ComponentLimit)
        {
            reasons.Add($"{components.Count} components after the prefix (limit is {ComponentLimit})");
            category ??= CountReason;
        }

        return new NameAnalysis(
            components.ToArray(),
            components.Count,
            reasons.Count == 0 ? null : string.Join(", ", reasons),
            category);
    }

    private static string PrefixLegend() =>
        string.Join(", ", Prefixes
            .Where(prefix => prefix.All(char.IsUpper))
            .Select(prefix => $"`{prefix}`"));

    private static string? ReadPrefix(string name)
    {
        foreach (string prefix in Prefixes)
        {
            if (!name.StartsWith(prefix, StringComparison.Ordinal) || name.Length == prefix.Length)
            {
                continue;
            }

            char next = name[prefix.Length];
            if (char.IsUpper(next) || char.IsDigit(next) || next == '_')
            {
                return prefix;
            }
        }

        return null;
    }

    private static int WriteReport(
        IEnumerable<NameRecord> records,
        ISet<string> bases,
        ISet<string> verbs,
        ExemptRegistry exempt,
        IReadOnlyList<string> turfs,
        IReadOnlyList<string> sealedHits,
        string projectRoot,
        string version,
        string outputPath,
        string pageDataPath)
    {
        List<FileAudit> files = [];
        List<FileAudit> audits = [];
        List<NameHit> hits = [];
        int totalNames = 0;
        int totalReviews = 0;

        // The test suite either names every test method descriptively without a prefix
        // (all exempt) or commits to the T prefix on every one. A single T-prefixed test
        // method flips the whole suite into the prefix-required mode.
        bool anyTestPrefixed = records.Any(record =>
            string.Equals(record.Kind, "TestMethod", StringComparison.Ordinal) &&
            string.Equals(ReadPrefix(record.Name.TrimStart('_')), TestPrefix, StringComparison.OrdinalIgnoreCase));

        foreach (IGrouping<string, NameRecord> fileGroup in records
                     .GroupBy(record => record.Path, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            FileAudit audit = BuildFileAudit(fileGroup, bases, verbs, exempt, anyTestPrefixed, projectRoot, hits);
            totalNames += audit.NameCount;
            totalReviews += audit.ReviewCount;
            audits.Add(audit);

            if (audit.ReviewCount + audit.HitCount > 0)
            {
                files.Add(audit);
            }
        }

        NameHit[] ordered = hits
            .OrderBy(hit => hit.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(hit => hit.Line)
            .ThenBy(hit => hit.Name, StringComparer.Ordinal)
            .ToArray();
        (string Label, int Value)[] reasonRows = ReasonOrder
            .Select(reason => (reason, hits.Count(hit => string.Equals(hit.Category, reason, StringComparison.Ordinal))))
            .ToArray();
        IReadOnlyList<string> stale = exempt.StaleNames();

        List<string> lines =
        [
            $"# Component Name Audit {version}",
            "",
            $"- Version: `{version}`",
            $"- Source names examined: {totalNames}",
            $"- Files with findings: {files.Count}",
            $"- Non-conforming names: {ordered.Length}",
            $"- Stale exempt rows: {stale.Count}",
            $"- Types outside their prefix turf: {turfs.Count} (ceiling {PrefixCeiling})",
            $"- Public types outside the turf of a sealed folder: {sealedHits.Count}",
            .. reasonRows.Select(row => $"- Primary reason {row.Label}: {row.Value}"),
            $"- At-limit review names: {totalReviews}",
            $"- Counting: {PrefixLegend()} is a prefix and is not counted as a component.",
            "- Non-conforming name: counted once, whatever number of reasons it carries; its primary reason is the first check that failed.",
            $"- Prefix: a codebase-owned name carries no {PrefixLegend()} prefix, or a test method breaks the `{TestPrefix}` test-prefix gate.",
            "- Shape: the name does not split into a prefix and PascalCase components.",
            $"- Base: the first component after the prefix is not registered in `{BasesDocument}`.",
            $"- Verb: a method not ending in a registered verb, or data/type ending in one (`{VerbsDocument}`).",
            $"- Count: more than {ComponentLimit} components after the prefix.",
            $"- Review: at least {ComponentReview} components after the prefix.",
            "- Implicit framework-interface implementations (for example `IDisposable.Dispose`) are exempt, like explicit ones.",
            "- Hierarchy: findings remain under the nearest governed declaration that contains them.",
            $"- Exemption authority: **only the user** adds a name to the Exempt sections of `{BasesDocument}`. A contributor or agent may propose one; it is not granted by working the report.",
            "- A proposal needs a named binding mechanism (Win32/COM v-table or `[DllImport]`/`[ComImport]`, a `: IFoo` implicit implementation, an `[ObservableProperty]` `partial void` hook, or a `PART_` name inside a replacement `ControlTemplate`) and the `file:line` that was read. Name shape, proximity to an exempt name, and a wish for a zero count are not evidence.",
            ""
        ];

        string? currentDirectory = null;
        foreach (FileAudit file in files
                     .OrderBy(file => file.Directory, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(file => file.FileName, StringComparer.OrdinalIgnoreCase))
        {
            if (!string.Equals(currentDirectory, file.Directory, StringComparison.OrdinalIgnoreCase))
            {
                if (currentDirectory is not null)
                {
                    lines.Add("");
                }

                currentDirectory = file.Directory;
                lines.Add($"## `{file.Directory}`");
                lines.Add("");
            }

            lines.Add($"### `{file.FileName}`");
            lines.Add("");
            foreach (AuditNode root in file.Roots)
            {
                AddNodeLines(root, 0, lines);
            }
            lines.Add("");
        }

        if (files.Count == 0)
        {
            lines.Add("No prefix, shape, base, verb, or count findings were found.");
        }

        List<string> violationLines =
        [
            $"# Violated Names {version}",
            "",
            $"- Version: `{version}`",
            $"- Non-conforming names: {ordered.Length}",
            .. reasonRows.Select(row => $"- Primary reason {row.Label}: {row.Value}"),
            "",
            "Every line below is a rename until the user says otherwise. **Only the user grants an",
            $"exemption** by adding the name to the Exempt sections of `{BasesDocument}`; working this",
            "report does not grant one, and a finding is never cleared by declaring it external.",
            "A proposal must name the binding mechanism - a Win32/COM v-table slot or",
            "`[DllImport]`/`[ComImport]` declaration, an implicit implementation of a declared `: IFoo`,",
            "an `[ObservableProperty]` `partial void On...Changed`/`Changing` hook, or a `PART_` name inside",
            "a replacement `ControlTemplate` - and cite the `file:line` that was read. A name's shape, its",
            "neighbours, and the size of this list are not evidence.",
            "",
            "A name a source generator emits is audited at the declaration it is built from, so a",
            "`[RelayCommand]` method is reported under the `{Name}Command` property that actually",
            "exists. That suffix is the generator's, not a component you may spend: the method keeps",
            "one slot beside its verb, and an over-limit command name is a missing owner like any",
            "other. It is not one of the four exemption mechanisms above.",
            ""
        ];

        foreach (NameHit hit in ordered)
        {
            violationLines.Add($"- {HitFormat(hit)}");
        }

        if (ordered.Length == 0)
        {
            violationLines.Add("No prefix, shape, base, verb, or count violations were found.");
        }

        lines.Add("");
        lines.Add("## Exempt registry");
        lines.Add("");
        lines.Add($"- Rows in the `{ExemptMarker}` block of `{BasesDocument}`: {exempt.RowCount}");
        lines.Add($"- Rows that matched a name this run: {exempt.RowCount - stale.Count}");
        lines.Add($"- Stale rows (matched nothing): {stale.Count}");
        lines.Add("");

        if (stale.Count == 0)
        {
            lines.Add("Every registered exemption still corresponds to a name in the source.");
        }
        else
        {
            lines.Add("A stale row fails the run: the name it grants is gone from the scope it names,");
            lines.Add("so the row must be pruned by the user. It is never pruned by the tooling.");
            lines.Add("");
            foreach (string name in stale)
            {
                lines.Add($"- `{name}`");
            }
        }

        int turfOver = Math.Max(0, turfs.Count - PrefixCeiling);
        int turfStale = Math.Max(0, PrefixCeiling - turfs.Count);
        lines.Add("");
        lines.Add("## Prefix turfs");
        lines.Add("");
        lines.Add($"- Types whose prefix is outside the turf of their project: {turfs.Count}");
        lines.Add($"- Ceiling: {PrefixCeiling}");
        lines.Add("");
        lines.AddRange(turfs.Select(turf => $"- {turf}"));
        lines.Add("");
        lines.Add("## Sealed turfs");
        lines.Add("");
        lines.Add($"- Sealed folders: {string.Join(", ", SealedTurfs.Select(item => $"`{item}`"))}");
        lines.Add($"- Public types whose prefix is outside the turf of a sealed folder: {sealedHits.Count}");
        lines.Add("");
        lines.AddRange(sealedHits.Select(hit => $"- {hit}"));

        // One report holds both: the violated names first, then the complete inventory, each under its
        // own heading one level beneath the report title.
        List<string> reportLines =
        [
            $"# AuditNames {version}",
            "",
            .. violationLines.Select(DemoteHeading),
            "",
            .. lines.Select(DemoteHeading)
        ];
        WriteLinesAtomic(outputPath, reportLines);

        (string Gate, int Count, string Meaning, string Section)[] gates =
        [
            ("Non-conforming names", ordered.Length, "names breaking the naming rules", "Non-conforming names"),
            ("Stale exempt rows", stale.Count, "exempt rows whose name is gone from source", "Stale exempt rows"),
            ("Turf over ceiling", turfOver, "types outside their prefix turf above the ceiling", "Turf"),
            ("Turf stale ceiling", turfStale, "ceiling points above the types outside their turf", "Turf"),
            ("Sealed turf", sealedHits.Count, "public types outside the turf of a sealed folder", "Sealed turf"),
        ];
        WritePageData(
            pageDataPath,
            projectRoot,
            version,
            outputPath,
            gates,
            reasonRows,
            ordered,
            stale,
            turfs,
            sealedHits,
            audits,
            [
                ("names", totalNames),
                ("files", files.Count),
                ("reviews", totalReviews),
                ("exempt", exempt.RowCount),
                ("turf", turfs.Count),
                ("ceiling", PrefixCeiling),
            ]);

        WriteResult(gates);

        if (ordered.Length > 0)
        {
            WriteTable("Reasons", "Primary reason", "Names", [.. reasonRows, ("Total", ordered.Length)]);
        }

        WriteRows("Names",
        [
            ("Names examined", totalNames),
            ("Files with findings", files.Count),
            ("At-limit review names", totalReviews),
            ("Exempt rows", exempt.RowCount),
            ("Turf", turfs.Count),
            ("Turf ceiling", PrefixCeiling),
        ]);

        WriteList("Non-conforming names", ordered.Select(HitFormat).ToArray());
        WriteList("Stale exempt rows", stale.ToArray());
        WriteList("Turf", turfs.ToArray());
        WriteList("Sealed turf", sealedHits.ToArray());

        Console.WriteLine();
        Console.WriteLine($"Report: {outputPath}");

        return ordered.Length + stale.Count + turfOver + turfStale + sealedHits.Count;
    }

    private static string DemoteHeading(string line) =>
        line.StartsWith('#') ? "#" + line : line;

    // The page data is everything this run computed, for the template AuditNames.html to draw. The
    // default encoder escapes every character outside ASCII and the <, > and & characters, so the
    // text drops into the page's script block unchanged. Each name is one row of the inventory:
    // file index, line, kind, name, owner, status, component count, components, and reason.
    private static void WritePageData(
        string pageDataPath,
        string projectRoot,
        string version,
        string outputPath,
        (string Gate, int Count, string Meaning, string Section)[] gates,
        (string Label, int Value)[] reasonRows,
        NameHit[] ordered,
        IReadOnlyList<string> stale,
        IReadOnlyList<string> turfs,
        IReadOnlyList<string> sealedHits,
        IReadOnlyList<FileAudit> audits,
        (string Key, int Value)[] totals)
    {
        List<string> paths = [];
        List<object?[]> names = [];
        foreach (FileAudit audit in audits)
        {
            int index = paths.Count;
            paths.Add(Path.GetRelativePath(projectRoot, audit.Path).Replace('\\', '/'));
            foreach (AuditNode root in audit.Roots)
            {
                AddPageRows(root, null, index, names);
            }
        }

        var data = new
        {
            project = Project,
            version,
            generation = Generation,
            generated = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", System.Globalization.CultureInfo.InvariantCulture),
            root = projectRoot,
            report = new { name = Path.GetFileName(outputPath), link = Path.GetFileName(outputPath) },
            gates = gates.Select(gate => new
            {
                status = gate.Count > 0 ? "FAIL" : "OK",
                gate = gate.Gate,
                count = gate.Count,
                meaning = gate.Meaning,
                section = gate.Section,
            }),
            totals = totals.ToDictionary(total => total.Key, total => total.Value, StringComparer.Ordinal),
            limit = ComponentLimit,
            review = ComponentReview,
            reasons = reasonRows.Select(row => new { label = row.Label, count = row.Value }),
            hits = ordered.Select(hit => new
            {
                path = hit.Path,
                line = hit.Line,
                kind = hit.Kind,
                name = hit.Name,
                reason = hit.Reason,
                category = hit.Category,
                text = HitFormat(hit),
            }),
            stale,
            turfs,
            sealedTurfs = sealedHits,
            files = paths,
            names,
        };

        string? folder = Path.GetDirectoryName(pageDataPath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }

        File.WriteAllText(pageDataPath, JsonSerializer.Serialize(data), new UTF8Encoding(false));
    }

    private static void AddPageRows(AuditNode node, string? owner, int file, ICollection<object?[]> rows)
    {
        string status = node.Reason is not null
            ? "Violation"
            : node.IsExempt
                ? "Exempt"
                : node.ComponentCount >= ComponentReview ? "Review" : "Clear";
        rows.Add([file, node.Line, node.Kind, node.Name, owner, status, node.ComponentCount,
            string.Join(" + ", node.Components), node.Reason]);
        foreach (AuditNode child in node.Children)
        {
            AddPageRows(child, node.Name, file, rows);
        }
    }

    private static string HitFormat(NameHit hit) =>
        $"{hit.Path}:{hit.Line} [{hit.Kind}] {hit.Name} - {hit.Reason}";

    private static void WriteTitle(string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(title);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(new string('-', title.Length));
        Console.ResetColor();
    }

    private static void WriteResult((string Gate, int Count, string Meaning, string Section)[] rows)
    {
        WriteTitle("Result");
        int statusWidth = 6;
        int countWidth = Math.Max(5, rows.Max(row => row.Count.ToString("N0").Length));
        int gateWidth = Math.Max(4, rows.Max(row => row.Gate.Length));
        int meaningWidth = Math.Max(7, rows.Max(row => row.Meaning.Length));
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{"Status".PadRight(statusWidth)}  {"Count".PadLeft(countWidth)}  {"Gate".PadRight(gateWidth)}  Meaning");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{new string('-', statusWidth)}  {new string('-', countWidth)}  {new string('-', gateWidth)}  {new string('-', meaningWidth)}");
        Console.ResetColor();
        foreach ((string gate, int count, string meaning, string _) in rows)
        {
            bool failing = count > 0;
            string status = failing ? "FAIL" : "OK";
            Console.ForegroundColor = failing ? ConsoleColor.Red : ConsoleColor.Green;
            Console.Write(status);
            Console.ResetColor();
            Console.WriteLine($"{new string(' ', statusWidth - status.Length)}  {count.ToString("N0").PadLeft(countWidth)}  {gate.PadRight(gateWidth)}  {meaning}");
        }

        var failed = rows.Where(row => row.Count > 0).ToArray();
        Console.WriteLine();
        if (failed.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"PASS: all {rows.Length} gates at 0.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAIL: {failed.Length} of {rows.Length} gates above 0. See {string.Join(", ", failed.Select(row => "\"" + row.Section + "\""))}.");
        }

        Console.ResetColor();
    }

    private static void WriteRows(string title, (string Label, int Value)[] rows)
    {
        int labelWidth = rows.Max(row => row.Label.Length);
        int valueWidth = rows.Max(row => row.Value.ToString("N0").Length);
        WriteTitle(title);
        foreach ((string label, int value) in rows)
        {
            Console.WriteLine($"{label.PadRight(labelWidth)}  {value.ToString("N0").PadLeft(valueWidth)}");
        }
    }

    private static void WriteTable(string title, string labelHeader, string valueHeader, (string Label, int Value)[] rows)
    {
        int labelWidth = Math.Max(labelHeader.Length, rows.Max(row => row.Label.Length));
        int valueWidth = Math.Max(valueHeader.Length, rows.Max(row => row.Value.ToString("N0").Length));
        WriteTitle(title);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{labelHeader.PadRight(labelWidth)}  {valueHeader.PadLeft(valueWidth)}");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{new string('-', labelWidth)}  {new string('-', valueWidth)}");
        Console.ResetColor();
        foreach ((string label, int value) in rows)
        {
            Console.WriteLine($"{label.PadRight(labelWidth)}  {value.ToString("N0").PadLeft(valueWidth)}");
        }
    }

    private static void WriteList(string title, string[] items)
    {
        if (items.Length == 0)
        {
            return;
        }

        string heading = $"{title} ({items.Length:N0})";
        WriteTitle(heading);
        foreach (string item in items.Take(ItemLimit))
        {
            Console.WriteLine(item);
        }

        if (items.Length > ItemLimit)
        {
            Console.WriteLine($"... and {items.Length - ItemLimit:N0} more in the report.");
        }
    }

    private static FileAudit BuildFileAudit(
        IEnumerable<NameRecord> records,
        ISet<string> bases,
        ISet<string> verbs,
        ExemptRegistry exempt,
        bool anyTestPrefixed,
        string projectRoot,
        ICollection<NameHit> hits)
    {
        NameRecord[] ordered = records
            .OrderBy(record => record.Anchor)
            .ThenBy(record => record.Position)
            .ThenBy(record => record.Line)
            .ThenBy(record => record.Name, StringComparer.Ordinal)
            .ToArray();

        List<AuditNode> roots = [];
        Dictionary<string, AuditNode> rootMap = new(StringComparer.Ordinal);
        Dictionary<string, AuditNode> idToNode = new(StringComparer.Ordinal);
        int nameCount = 0;
        int reviewCount = 0;
        int hitCount = 0;

        foreach (NameRecord record in ordered)
        {
            if (string.IsNullOrWhiteSpace(record.Name))
            {
                continue;
            }

            AuditNode? parent = null;
            if (!string.IsNullOrWhiteSpace(record.ParentId))
            {
                idToNode.TryGetValue(record.ParentId, out parent);
            }

            List<AuditNode> siblings = parent?.Children ?? roots;
            Dictionary<string, AuditNode> siblingMap = parent?.ChildMap ?? rootMap;

            if (!siblingMap.TryGetValue(record.Id, out AuditNode? node))
            {
                bool exempted = exempt.Grants(record.Name, record.Path);
                NameAnalysis analysis = exempted
                    ? NameAnalysis.Ignored
                    : AnalyzeName(record.Name, record.Kind, bases, verbs, anyTestPrefixed);
                bool review = analysis.Count >= ComponentReview && analysis.Count <= ComponentLimit;
                node = new AuditNode
                {
                    Name = record.Name,
                    Kind = record.Kind,
                    Line = record.Line,
                    Position = record.Position,
                    Components = analysis.Components,
                    ComponentCount = analysis.Count,
                    IsFinding = analysis.Reason is not null || analysis.Count >= ComponentReview,
                    IsExempt = exempted,
                    Reason = analysis.Reason
                };

                siblings.Add(node);
                siblingMap[record.Id] = node;
                nameCount++;

                if (review)
                {
                    reviewCount++;
                }

                if (analysis.Reason is not null)
                {
                    hitCount++;
                    hits.Add(new NameHit(
                        Path.GetRelativePath(projectRoot, record.Path).Replace('\\', '/'),
                        record.Line,
                        record.Kind,
                        record.Name,
                        analysis.Reason,
                        analysis.Category!));
                }
            }

            if (!string.IsNullOrWhiteSpace(record.Id))
            {
                idToNode[record.Id] = node;
            }
        }

        string path = ordered.Length > 0 ? ordered[0].Path : string.Empty;
        return new FileAudit(
            path,
            Path.GetDirectoryName(path) ?? string.Empty,
            Path.GetFileName(path),
            roots.ToArray(),
            nameCount,
            reviewCount,
            hitCount);
    }

    private static bool HasFinding(AuditNode node) =>
        node.IsFinding || node.Children.Any(HasFinding);

    private static void AddNodeLines(AuditNode node, int depth, ICollection<string> lines)
    {
        if (!HasFinding(node))
        {
            return;
        }

        string indent = new(' ', depth * 2);
        string line = $"{indent}- `{node.Name}`";

        if (node.Reason is not null)
        {
            line += $" - **Violation: {node.Reason}**";
        }
        else if (node.ComponentCount >= ComponentReview)
        {
            line += $" - Review: {node.ComponentCount} components: {FormatComponents(node.Components)}";
        }

        if (node.IsFinding && node.Line > 0)
        {
            line += $" - line {node.Line}";
        }

        lines.Add(line);
        foreach (AuditNode child in node.Children)
        {
            AddNodeLines(child, depth + 1, lines);
        }
    }

    private static string FormatComponents(IEnumerable<string> components) =>
        string.Join(" + ", components.Select(component => $"`{component}`"));

    private static void WriteLinesAtomic(string outputPath, IEnumerable<string> lines)
    {
        string? outputDirectory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new InvalidOperationException($"The output directory could not be resolved: {outputPath}");
        }

        Directory.CreateDirectory(outputDirectory);
        string temporaryPath = outputPath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporaryPath, string.Join("\n", lines) + "\n", new UTF8Encoding(false));
            File.Move(temporaryPath, outputPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
'@

    $projectPath = Join-Path $HelperFolder 'AuditNames.Helper.csproj'
    $programPath = Join-Path $HelperFolder 'Program.cs'
    [System.IO.File]::WriteAllText($projectPath, $projectContent, [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText($programPath, $programContent, [System.Text.UTF8Encoding]::new($false))
    return $projectPath
}

# Page output: the helper writes the page data as JSON, and this fills the template AuditNames.html
# with it. The page goes next to the Markdown report with no byte order mark and LF line breaks.
function Write-AuditPage {
    param(
        [Parameter(Mandatory = $true)][string]$DataPath,
        [Parameter(Mandatory = $true)][string]$PagePath,
        [Parameter(Mandatory = $true)][string]$Title
    )

    $utf8 = [System.Text.UTF8Encoding]::new($false)
    $templatePath = Join-Path $PSScriptRoot 'AuditNames.html'
    if (-not (Test-Path -LiteralPath $templatePath -PathType Leaf)) {
        throw "The page template was not found: $templatePath"
    }

    $template = [System.IO.File]::ReadAllText($templatePath, $utf8)
    foreach ($marker in @('/*__DATA__*/', '__TITLE__')) {
        if (-not $template.Contains($marker)) {
            throw "The page template lacks the marker $marker : $templatePath"
        }
    }

    $data = [System.IO.File]::ReadAllText($DataPath, $utf8)
    $page = $template.Replace('__TITLE__', [System.Net.WebUtility]::HtmlEncode($Title)).Replace('/*__DATA__*/', $data)
    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($PagePath)) | Out-Null
    [System.IO.File]::WriteAllText($PagePath, ($page -replace "`r`n", "`n"), $utf8)
}

$projectRoot = Resolve-ProjectRoot -Path $Root
$config = Read-AuditConfig -ProjectRoot $projectRoot
# The registered names are validated here, before any build, so a missing or stale registry fails at
# once with the request to run SyncNames.ps1. The helper reads the same file by its path.
$registry = Read-AuditRegistry
$version = Read-ProjectVersion -ProjectRoot $projectRoot -VersionFile ([string]$config.report.versionFile) -VersionKey ([string]$config.report.versionKey)

[string[]]$sourceFiles = @(Get-ProjectSourceFiles -ProjectRoot $projectRoot -Config $config)

if ($sourceFiles.Length -eq 0) {
    throw "No C# or XAML source files were found under: $projectRoot"
}
Write-AuditLine ("Scanned: {0:N0} source files" -f $sourceFiles.Length) -ForegroundColor DarkGray

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    throw 'The .NET SDK is required to parse project declarations, but dotnet was not found on PATH.'
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
$reportDirectoryFull = Join-AuditPath -ProjectRoot $projectRoot -Relative ([string]$config.report.directory)
$markdownPathFull = Join-Path $reportDirectoryFull ('{0}{1}.md' -f [string]$config.report.prefix, $version)
$pagePathFull = Join-Path $reportDirectoryFull ('{0}{1}.html' -f [string]$config.report.prefix, $version)
$temporaryFolder = Join-Path ([System.IO.Path]::GetTempPath()) ($config.project + '-AuditNames-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($temporaryFolder) | Out-Null
$auditExitCode = 0

try {
    $manifestPath = Join-Path $temporaryFolder 'sources.txt'
    $pageDataPath = Join-Path $temporaryFolder 'page.json'
    [System.IO.File]::WriteAllLines($manifestPath, $sourceFiles, [System.Text.UTF8Encoding]::new($false))

    $stagingFolder = Join-Path $temporaryFolder 'helper'
    [System.IO.Directory]::CreateDirectory($stagingFolder) | Out-Null
    $stagedProject = Write-AuditHelper -HelperFolder $stagingFolder -TargetFramework $targetFramework

    $hasher = [System.Security.Cryptography.SHA256]::Create()
    try {
        $helperText = $sdkVersion + "`n" + [System.IO.File]::ReadAllText($stagedProject) + "`n" + [System.IO.File]::ReadAllText((Join-Path $stagingFolder 'Program.cs'))
        $helperHash = [System.BitConverter]::ToString($hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($helperText))).Replace('-', '').Substring(0, 16)
    }
    finally {
        $hasher.Dispose()
    }

    $cacheFolder = Join-Path ([System.IO.Path]::GetTempPath()) ($config.project + '-AuditNames-Helper-' + $helperHash)
    $cacheOutput = Join-Path $cacheFolder 'out'
    $cacheMarker = Join-Path $cacheFolder 'ready'
    $helperAssembly = Join-Path $cacheOutput 'AuditNames.Helper.dll'

    $previousNoLogo = $env:DOTNET_NOLOGO
    $previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
    $env:DOTNET_NOLOGO = '1'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

    try {
        if (-not (Test-Path -LiteralPath $cacheMarker -PathType Leaf) -or -not (Test-Path -LiteralPath $helperAssembly -PathType Leaf)) {
            if (Test-Path -LiteralPath $cacheFolder) {
                Remove-Item -LiteralPath $cacheFolder -Recurse -Force
            }

            [System.IO.Directory]::CreateDirectory($cacheFolder) | Out-Null
            Copy-Item -Path (Join-Path $stagingFolder '*') -Destination $cacheFolder
            $buildArguments = @('build', (Join-Path $cacheFolder 'AuditNames.Helper.csproj'), '--configuration', 'Release', '--output', $cacheOutput, '-nologo')
            $nativePreference = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            $buildOutput = & $dotnet.Source @buildArguments 2>&1
            $ErrorActionPreference = $nativePreference
            if ($LASTEXITCODE -ne 0) {
                throw "The component-name audit helper did not build.`n$($buildOutput -join [Environment]::NewLine)"
            }

            [System.IO.File]::WriteAllText($cacheMarker, $helperHash, [System.Text.UTF8Encoding]::new($false))
        }

        $arguments = @(
            $helperAssembly,
            (Join-Path $PSScriptRoot 'AuditNames.json'),
            $registry.Path,
            $projectRoot,
            $manifestPath,
            $version,
            $markdownPathFull,
            $pageDataPath
        )

        $nativePreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        $auditOutput = & $dotnet.Source @arguments 2>&1
        $ErrorActionPreference = $nativePreference
        $auditExitCode = $LASTEXITCODE
    }
    finally {
        $env:DOTNET_NOLOGO = $previousNoLogo
        $env:DOTNET_CLI_TELEMETRY_OPTOUT = $previousTelemetry
    }

    if ($auditExitCode -ne 0 -and $auditExitCode -ne 3) {
        throw "The component-name audit failed.`n$($auditOutput -join [Environment]::NewLine)"
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

    Write-AuditPage -DataPath $pageDataPath -PagePath $pagePathFull -Title ([string]$config.project)
    Write-AuditLine "Report: $pagePathFull"
}
finally {
    if (Test-Path -LiteralPath $temporaryFolder) {
        Remove-Item -LiteralPath $temporaryFolder -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if ($Open) {
    Start-Process -FilePath $markdownPathFull
}

if (-not $NoOpen) {
    Start-Process -FilePath $pagePathFull
}

if ($auditExitCode -ne 0) {
    exit 1
}

exit 0
