<#
.SYNOPSIS
Reports which C# language features and grammar the codebase uses, how often and where.

.DESCRIPTION
Reads the project configuration from StatsSyntax.json next to this script, then
performs these actions on every run:
  1. Parses every .cs file under the source roots with Roslyn, syntax only: nothing is
     bound and the solution is never built.
  2. Counts lines, nodes, every syntax kind, each catalog feature, each side of each
     style pair and attribute names, in total and per project.
  3. Prints the Summary table: files, lines, nodes, syntax kinds used, features used,
     the minimum C# version the used features need, and parse errors.
A Stats script only reports: it has no gates, no ceilings and no pass or fail. It exits 0
unless the run itself breaks.

The parser is a small C# helper embedded in this script. It is built once into the
temporary folder, cached under a hash of its text, the target framework and the SDK
version, and reused on later runs. The .NET SDK and git are the external tools required.

A feature is a language construct with the C# version that introduced it. A style is a
pair of competing ways to write the same thing, counted side by side. The catalog of both
lives in StatsSyntax.json; the helper holds one detector for each entry, and an id present
on one side only stops the run with an error.

Everything project-specific lives in StatsSyntax.json. Files come from git: tracked and
untracked files, never ignored ones. A configured root or root-level file that does not
exist stops the run with an error.

StatsSyntax.json shape:
  {
    "generation": 1,
    "project": "Cadroue",
    "sources": {
      "roots": ["src", "tests"],
      "files": [],
      "excludeSegments": [".git", "bin", "obj"],
      "excludeSuffixes": [".g.cs", ".Designer.cs"]
    },
    "helper": { "framework": "net10.0" },
    "top": 40,
    "report": {
      "directory": "docs-analysis",
      "versionFile": "version.json",
      "versionKey": "current-version",
      "prefix": "StatsSyntax-",
      "segments": 1
    },
    "features": [{ "id": "class", "label": "Class", "family": "Types", "version": "1" }],
    "styles": [{ "id": "branch", "label": "Branch", "a": "Switch expression", "b": "Switch statement" }]
  }

Files lists root-level .cs files counted under the (root) project. It may be empty.

.PARAMETER Top
Overrides top: how many rows each ranking and list shows on the console.

.PARAMETER NoOpen
Write the page without opening it.

.PARAMETER NoPause
Do not stop at each console page. Paging is also off when output or input is redirected.

.PARAMETER Help
Display this help and exit without running. The alias -? is supported.

.EXAMPLE
StatsSyntax

.EXAMPLE
StatsSyntax -Top 100 -NoPause
#>
#requires -Version 5.1
# STATSSYNTAX - STATS GENERATION 1.
# A generation names what the statistics count. Wording, plumbing and refactoring leave it alone.
[CmdletBinding()]
param(
    [ValidateRange(1, [int]::MaxValue)]
    [int]$Top,
    [switch]$NoOpen,
    [switch]$NoPause,
    [Alias('?')]
    [switch]$Help
)

$ConfigPath = Join-Path $PSScriptRoot "StatsSyntax.json"

if ($Help) {
    @'
NAME
    StatsSyntax.ps1

SYNOPSIS
    Report which C# language features and grammar the codebase uses, how
    often, where, and which of two competing styles it prefers.

SYNTAX
    StatsSyntax [-Top <number>] [-NoOpen] [-NoPause] [-Help]

CONFIGURATION
    All project-specific values live in StatsSyntax.json next to the script:
    source roots and root-level files, excluded directory names and suffixes,
    the helper target framework, the console row count, report directory,
    version file and key, the report file-name prefix, and the catalog of
    features and styles.

STATISTICS
    Every .cs file is parsed with Roslyn, syntax only. Nothing is bound and
    the solution is never built. The parser helper is built once and cached.
    No gate, no ceiling, no pass or fail. The exit code is 0 unless the
    run itself breaks.

OPTIONS
    -Top <number>
        Rows each ranking and list shows on the console. Overrides top.
        The report and the page always hold every row.

    -NoOpen
        Write the page without opening it.

    -NoPause
        Do not stop at each console page for a key. Off by itself when output
        or input is redirected.

    -Help, -?
        Display this help and exit without running.

EXAMPLES
    StatsSyntax
        Report with the configured settings.

    StatsSyntax -Top 100
        Show 100 rows per ranking and list on the console.
'@ | Write-Host
    exit 0
}

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding

$script:StatsGeneration = 1

# Console paging, as in the audits.
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

function Write-StatsLine {
    param(
        [Parameter(Position = 0)][AllowEmptyString()][string]$Text = '',
        [ConsoleColor]$ForegroundColor
    )

    if ($script:PageLimit -gt 0) {
        $rows = [Math]::Max(1, [Math]::Ceiling($Text.Length / [double]$script:PageWidth))
        if ($script:PageCount + $rows -gt $script:PageLimit -and $script:PageCount -gt 0) {
            $prompt = '-- More -- (any key: next page, Q: no more pauses)'
            Write-Host $prompt -ForegroundColor DarkGray -NoNewline
            $key = [Console]::ReadKey($true)
            Write-Host ("`r" + (' ' * $prompt.Length) + "`r") -NoNewline
            if ($key.Key -eq [ConsoleKey]::Q) {
                $script:PageLimit = 0
            }
            $script:PageCount = 0
        }
        $script:PageCount += $rows
    }

    if ($PSBoundParameters.ContainsKey('ForegroundColor')) {
        Write-Host $Text -ForegroundColor $ForegroundColor
    }
    else {
        Write-Host $Text
    }
}

Write-StatsLine "STATSSYNTAX - STATS GENERATION $script:StatsGeneration" -ForegroundColor Blue

function Get-ConfigNode {
    param(
        [Parameter(Mandatory = $true)]$Document,
        [Parameter(Mandatory = $true)][string]$Key
    )

    $node = $Document
    foreach ($segment in ($Key -split '\.')) {
        if ($null -eq $node -or -not ($node.PSObject.Properties.Name -contains $segment)) {
            return $null
        }
        $node = $node.$segment
    }

    return , $node
}

function Get-ConfigLeaves {
    param(
        [Parameter(Mandatory = $true)]$Node,
        [AllowEmptyString()][string]$Path = ''
    )

    $leaves = [System.Collections.Generic.List[string]]::new()
    foreach ($property in $Node.PSObject.Properties) {
        $child = if ($Path.Length -eq 0) { $property.Name } else { $Path + '.' + $property.Name }
        if ($property.Value -is [System.Management.Automation.PSCustomObject]) {
            foreach ($leaf in (Get-ConfigLeaves -Node $property.Value -Path $child)) { $leaves.Add($leaf) }
        }
        else {
            $leaves.Add($child)
        }
    }

    return , $leaves
}

# True when every item of an array is an object holding exactly the given text properties.
function Test-ConfigRecords {
    param(
        $Value,
        [Parameter(Mandatory = $true)][string[]]$Names
    )

    if ($null -eq $Value -or -not ($Value -is [System.Array]) -or $Value.Count -eq 0) { return $false }
    foreach ($item in $Value) {
        if (-not ($item -is [System.Management.Automation.PSCustomObject])) { return $false }
        $present = @($item.PSObject.Properties | ForEach-Object { $_.Name })
        if ($present.Count -ne $Names.Count) { return $false }
        foreach ($name in $Names) {
            if ($name -notin $present) { return $false }
            $text = $item.$name
            if (-not ($text -is [string]) -or [string]::IsNullOrWhiteSpace($text)) { return $false }
        }
    }

    return $true
}

function Test-ConfigValue {
    param(
        $Value,
        [Parameter(Mandatory = $true)][string]$Kind
    )

    switch ($Kind) {
        'string' { return $Value -is [string] -and -not [string]::IsNullOrWhiteSpace($Value) }
        'int' { return ($Value -is [int] -or $Value -is [long]) -and $Value -ge 0 }
        'positive' { return ($Value -is [int] -or $Value -is [long]) -and $Value -ge 1 }
        'strings' {
            if ($null -eq $Value -or -not ($Value -is [System.Array])) { return $false }
            foreach ($item in $Value) {
                if (-not ($item -is [string]) -or [string]::IsNullOrWhiteSpace($item)) { return $false }
            }
            return $true
        }
        'features' { return Test-ConfigRecords -Value $Value -Names @('id', 'label', 'family', 'version') }
        'styles' { return Test-ConfigRecords -Value $Value -Names @('id', 'label', 'a', 'b') }
        default { throw "Unknown schema kind: $Kind" }
    }
}

function Read-StatsConfig {
    param([Parameter(Mandatory = $true)][string]$Path)

    $pathFull = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $pathFull -PathType Leaf)) {
        throw "The syntax-statistics configuration was not found: $pathFull"
    }

    try {
        $config = Get-Content -LiteralPath $pathFull -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The syntax-statistics configuration is not valid JSON: $pathFull`n$($_.Exception.Message)"
    }

    $problems = [System.Collections.Generic.List[string]]::new()
    $schema = [ordered]@{
        'generation' = 'int'; 'project' = 'string'
        'sources.roots' = 'strings'; 'sources.files' = 'strings'
        'sources.excludeSegments' = 'strings'; 'sources.excludeSuffixes' = 'strings'
        'helper.framework' = 'string'; 'top' = 'positive'
        'report.directory' = 'string'; 'report.versionFile' = 'string'; 'report.versionKey' = 'string'; 'report.prefix' = 'string'; 'report.segments' = 'positive'
        'features' = 'features'; 'styles' = 'styles'
    }

    $present = Get-ConfigLeaves -Node $config
    foreach ($key in $schema.Keys) {
        if ($key -notin $present) {
            $problems.Add("missing key '$key'")
        }
        elseif (-not (Test-ConfigValue -Value (Get-ConfigNode -Document $config -Key $key) -Kind $schema[$key])) {
            $problems.Add("key '$key' is not a valid $($schema[$key])")
        }
    }

    foreach ($key in $present) {
        if (-not $schema.Contains($key)) {
            $problems.Add("unknown key '$key'")
        }
    }

    if ($problems.Count -eq 0) {
        foreach ($list in @('features', 'styles')) {
            $ids = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
            foreach ($entry in @($config.$list)) {
                if ([string]$entry.id -cnotmatch '^[a-z0-9]+(-[a-z0-9]+)*$') { $problems.Add("key '$list' holds the id '$($entry.id)', which is not lowercase-hyphen") }
                if (-not $ids.Add([string]$entry.id)) { $problems.Add("key '$list' holds the id '$($entry.id)' twice") }
            }
        }
        foreach ($entry in @($config.features)) {
            if ([string]$entry.version -notmatch '^[0-9]+(\.[0-9]+)?$') { $problems.Add("feature '$($entry.id)' has the version '$($entry.version)', which is not a C# version such as 7 or 7.3") }
        }
    }

    if ($problems.Count -gt 0) {
        throw "The syntax-statistics configuration is not valid: $pathFull`n  " + ($problems -join "`n  ")
    }

    return $config
}

function Join-StatsPath {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Relative
    )

    if ([System.IO.Path]::IsPathRooted($Relative)) {
        return [System.IO.Path]::GetFullPath($Relative)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $Root $Relative))
}

$repoRootFull = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$config = Read-StatsConfig -Path $ConfigPath

if ([int]$config.generation -ne $script:StatsGeneration) {
    throw "The syntax-statistics configuration is generation $($config.generation) but this tooling is generation $script:StatsGeneration : $ConfigPath"
}

if (-not $PSBoundParameters.ContainsKey('Top')) { $Top = [int]$config.top }
$Segments = [int]$config.report.segments
$reportDirectoryFull = Join-StatsPath -Root $repoRootFull -Relative $config.report.directory
$versionPathFull = Join-StatsPath -Root $repoRootFull -Relative $config.report.versionFile
$versionKey = [string]$config.report.versionKey
$reportPrefix = [string]$config.report.prefix
$helperFramework = [string]$config.helper.framework

$excludedNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($segment in @($config.sources.excludeSegments)) { [void]$excludedNames.Add([string]$segment) }
$excludedSuffixes = @($config.sources.excludeSuffixes | ForEach-Object { [string]$_ })

function ConvertTo-MarkdownCell {
    param([AllowNull()][object]$Value)

    return ([string]$Value).Replace('|', '\|').Replace("`r`n", '<br>').Replace("`n", '<br>').Replace("`r", '<br>')
}

function Format-Integer {
    param([long]$Value)

    return $Value.ToString("N0", [System.Globalization.CultureInfo]::InvariantCulture)
}

function Format-Percent {
    param([double]$Value)

    return $Value.ToString("0.00", [System.Globalization.CultureInfo]::InvariantCulture) + " %"
}

function Format-Rate {
    param([double]$Value)

    return $Value.ToString("N2", [System.Globalization.CultureInfo]::InvariantCulture)
}

function Get-RelativePathSafe {
    param(
        [Parameter(Mandatory = $true)][string]$BasePath,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $baseFull = [System.IO.Path]::GetFullPath($BasePath)
    if (-not $baseFull.EndsWith([System.IO.Path]::DirectorySeparatorChar.ToString())) {
        $baseFull += [System.IO.Path]::DirectorySeparatorChar
    }

    $baseUri = [System.Uri]::new($baseFull)
    $pathUri = [System.Uri]::new([System.IO.Path]::GetFullPath($Path))
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($pathUri).ToString()).Replace('\', '/')
}

function Test-IsExcludedPath {
    param(
        [Parameter(Mandatory = $true)][string]$Relative,
        [Parameter(Mandatory = $true)][System.Collections.Generic.HashSet[string]]$ExcludedNames
    )

    foreach ($segment in $Relative.Split('/')) {
        if ($ExcludedNames.Contains($segment)) {
            return $true
        }
    }

    return $false
}

function Test-IsExcludedSuffix {
    param([Parameter(Mandatory = $true)][string]$Name)

    foreach ($suffix in $excludedSuffixes) {
        if ($Name.EndsWith($suffix, [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
    }

    return $false
}

function Get-FolderKey {
    param(
        [Parameter(Mandatory = $true)][string]$RootFull,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$UnderRoot
    )

    $cut = $UnderRoot.LastIndexOf('/')
    if ($cut -le 0) { return [System.IO.Path]::GetFileName($RootFull) }
    $parts = @($UnderRoot.Substring(0, $cut).Split('/'))
    $take = [Math]::Min($Segments, $parts.Count)
    return (($parts | Select-Object -First $take) -join '/')
}

function Write-SectionTitle {
    param([Parameter(Mandatory = $true)][string]$Text)

    Write-StatsLine ""
    Write-StatsLine $Text -ForegroundColor Blue
    Write-StatsLine ('-' * $Text.Length) -ForegroundColor DarkGray
}

function Format-Cell {
    param(
        [string]$Text,
        [int]$Width,
        [bool]$Right
    )

    if ($Right) { return $Text.PadLeft($Width) }
    return $Text.PadRight($Width)
}

# Renders a table: a cyan header row and dashed rule, then the rows in the default colour.
function Write-ConsoleTable {
    param([Parameter(Mandatory = $true)][object[]]$Columns)

    $gap = "  "
    foreach ($column in $Columns) {
        $width = $column.Name.Length
        foreach ($value in $column.Values) {
            if ($value.Length -gt $width) { $width = $value.Length }
        }
        $column.Width = $width
    }

    $header = [System.Text.StringBuilder]::new()
    $rule = [System.Text.StringBuilder]::new()
    foreach ($column in $Columns) {
        if ($header.Length -gt 0) { [void]$header.Append($gap); [void]$rule.Append($gap) }
        [void]$header.Append((Format-Cell -Text $column.Name -Width $column.Width -Right $column.Right))
        [void]$rule.Append('-' * $column.Width)
    }
    Write-StatsLine $header.ToString().TrimEnd() -ForegroundColor Cyan
    Write-StatsLine $rule.ToString() -ForegroundColor Cyan

    $rowCount = $Columns[0].Values.Count
    for ($row = 0; $row -lt $rowCount; $row++) {
        $line = [System.Text.StringBuilder]::new()
        foreach ($column in $Columns) {
            if ($line.Length -gt 0) { [void]$line.Append($gap) }
            [void]$line.Append((Format-Cell -Text $column.Values[$row] -Width $column.Width -Right $column.Right))
        }
        Write-StatsLine $line.ToString().TrimEnd()
    }
}

function New-ConsoleColumn {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [bool]$Right = $true,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$Values
    )

    return [pscustomobject]@{ Name = $Name; Right = $Right; Values = $Values; Width = 0 }
}

function Write-CutLine {
    param([int]$Total, [int]$Shown)

    if ($Total -gt $Shown) { Write-StatsLine ("... and {0} more in the report." -f (Format-Integer ($Total - $Shown))) }
}

function Invoke-StatsGit {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    # Windows PowerShell 5.1 turns any git stderr line into a terminating error under Stop, so git runs under Continue.
    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = @(& git @Arguments 2>$null)
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $nativePreference
    }

    return [pscustomobject]@{ Lines = $output; ExitCode = $exitCode }
}

# Sorts items by a text key under ordinal comparison, alike on 5.1 and 7.
function Sort-ByKey {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Items,
        [Parameter(Mandatory = $true)][scriptblock]$Key
    )

    if ($Items.Count -lt 2) { return $Items }
    $keys = [System.Collections.Generic.List[string]]::new($Items.Count)
    for ($i = 0; $i -lt $Items.Count; $i++) { $keys.Add([string](& $Key $Items[$i]) + [char]0 + $i.ToString('D9')) }
    $keys.Sort([System.StringComparer]::Ordinal)
    foreach ($entry in $keys) { $Items[[int]$entry.Substring($entry.Length - 9)] }
}

function Get-DescendingKey {
    param([long]$Count)

    return ([long]::MaxValue - $Count).ToString('D19')
}

# A C# version as text ("7.3") turned into a comparable number.
function ConvertTo-LanguageVersion {
    param([Parameter(Mandatory = $true)][string]$Text)

    $parts = $Text.Split('.')
    $minor = if ($parts.Count -gt 1) { [int]$parts[1] } else { 0 }
    return [System.Version]::new([int]$parts[0], $minor)
}

# File listing.
$rootEntries = [System.Collections.Generic.List[object]]::new()
foreach ($root in @($config.sources.roots)) {
    $rootFull = Join-StatsPath -Root $repoRootFull -Relative $root
    if (-not [System.IO.Directory]::Exists($rootFull)) { throw "The configured source root has no directory: $rootFull" }
    $rootRelative = (Get-RelativePathSafe -BasePath $repoRootFull -Path $rootFull).Trim('/')
    $rootEntries.Add([pscustomobject]@{ Full = $rootFull; Prefix = $(if ($rootRelative.Length -eq 0) { '' } else { $rootRelative + '/' }) })
}

$listArguments = @('-C', $repoRootFull, '-c', 'core.quotePath=false', 'ls-files', '--cached', '--others', '--exclude-standard', '--', ':(icase)*.cs')
$listing = Invoke-StatsGit -Arguments $listArguments
if ($listing.ExitCode -ne 0) {
    throw "Git could not enumerate the files under $repoRootFull, so the statistics cannot be counted."
}

$codeFiles = [System.Collections.Generic.List[object]]::new()
$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $listing.Lines) {
    $relative = ([string]$entry).Trim()
    if ($relative.Length -eq 0 -or (Test-IsExcludedPath -Relative $relative -ExcludedNames $excludedNames)) { continue }

    $owner = $null
    foreach ($candidate in $rootEntries) {
        if ($candidate.Prefix.Length -eq 0 -or $relative.StartsWith($candidate.Prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            $owner = $candidate
            break
        }
    }
    if ($null -eq $owner) { continue }

    $full = [System.IO.Path]::Combine($repoRootFull, $relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
    $name = [System.IO.Path]::GetFileName($full)
    if (-not $name.EndsWith('.cs', [System.StringComparison]::OrdinalIgnoreCase) -or (Test-IsExcludedSuffix -Name $name)) { continue }
    if (-not [System.IO.File]::Exists($full) -or -not $seen.Add($full)) { continue }
    $project = Get-FolderKey -RootFull $owner.Full -UnderRoot $relative.Substring($owner.Prefix.Length)
    $codeFiles.Add([pscustomobject]@{ Full = $full; Relative = $relative; Project = $project })
}
foreach ($entry in @($config.sources.files)) {
    $fileFull = Join-StatsPath -Root $repoRootFull -Relative $entry
    if (-not [System.IO.File]::Exists($fileFull)) { throw "The configured source file does not exist: $fileFull" }
    if (-not $fileFull.EndsWith('.cs', [System.StringComparison]::OrdinalIgnoreCase) -or -not $seen.Add($fileFull)) { continue }
    $codeFiles.Add([pscustomobject]@{ Full = $fileFull; Relative = (Get-RelativePathSafe -BasePath $repoRootFull -Path $fileFull); Project = '(root)' })
}
$codeFiles = [System.Collections.Generic.List[object]]@(Sort-ByKey -Items @($codeFiles) -Key { $args[0].Relative })

if ($codeFiles.Count -eq 0) { throw "No .cs file was found under the configured roots, so the statistics cannot be counted." }

# The parser helper: Roslyn syntax only, no binder.
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
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Reads a file list (relativePath TAB project per line), parses every file without binding,
// and writes one JSON file of syntax counts: node kinds, catalog features, style sides,
// attribute names and parse diagnostics, in total and per project.
internal static class Program
{
    internal static readonly string[] FeatureIds =
    {
        "class", "struct", "interface", "enum", "delegate", "record-class", "record-struct", "partial-type",
        "static-class", "sealed-class", "abstract-class", "primary-constructor", "file-local-type", "nested-type", "generic-type",
        "file-scoped-namespace", "block-namespace", "global-using", "using-static", "using-alias", "alias-any-type",
        "auto-property", "full-property", "init-accessor", "required-member", "expression-bodied-member", "readonly-struct-member",
        "field-like-event", "event-accessors", "indexer", "operator-overload", "conversion-operator", "extension-method",
        "extension-block", "field-keyword", "static-abstract-interface-member", "default-interface-method", "constructor",
        "static-constructor", "finalizer", "const-field", "readonly-field", "params-collection", "optional-parameter",
        "named-argument", "ref-out-parameter", "in-parameter", "ref-return-local", "local-function", "static-local-function",
        "type-pattern", "is-type", "declaration-pattern", "constant-pattern", "is-null", "not-pattern", "and-or-pattern", "relational-pattern",
        "property-pattern", "positional-pattern", "list-pattern", "var-pattern", "discard-pattern", "switch-expression",
        "switch-statement", "case-guard",
        "lambda", "static-lambda", "anonymous-method", "target-typed-new", "collection-expression", "spread-element",
        "with-expression", "tuple-literal", "deconstruction", "discard", "range", "index-from-end", "null-conditional",
        "null-conditional-assignment", "null-coalescing", "null-coalescing-assignment", "null-forgiving", "nameof",
        "interpolated-string", "raw-string", "utf8-literal", "verbatim-string", "default-literal", "throw-expression",
        "conditional-operator", "checked-expression", "typeof", "sizeof", "stackalloc", "anonymous-type", "object-initializer",
        "collection-initializer", "query-expression", "await", "as-cast", "cast-expression",
        "if", "for", "foreach", "while", "do", "try", "catch", "finally", "exception-filter", "using-statement",
        "using-declaration", "await-foreach", "await-using", "lock", "yield", "goto", "fixed", "unsafe-block", "local-const",
        "checked-block",
        "generic-method", "constraint-clause", "notnull-constraint", "unmanaged-constraint", "variance", "default-of-t",
        "async-method", "async-lambda",
        "nullable-value-annotation", "nullable-reference-annotation", "nullable-directive",
        "pointer-type", "function-pointer",
        "if-directive", "region-directive", "pragma-directive"
    };

    internal static readonly string[] StyleIds =
    {
        "null-check", "object-creation", "collection-creation", "member-body", "local-type", "using", "namespace",
        "text-building", "branch", "lambda-body", "type-test", "constructor"
    };

    // Well-known BCL structs, and after the first pass also every enum, struct and record struct declared in the sources.
    internal static readonly HashSet<string> ValueTypeNames = new(StringComparer.Ordinal)
    {
        "DateTime", "DateTimeOffset", "TimeSpan", "Guid", "CancellationToken", "KeyValuePair", "Point", "Size", "Rect", "Thickness", "Color"
    };

    internal static readonly Dictionary<string, int> FeatureIndex = FeatureIds.Select((id, i) => (id, i)).ToDictionary(p => p.id, p => p.i, StringComparer.Ordinal);
    internal static readonly Dictionary<string, int> StyleIndex = StyleIds.Select((id, i) => (id, i)).ToDictionary(p => p.id, p => p.i, StringComparer.Ordinal);

    private static int Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Usage: StatsSyntax.Helper <file-list> <repository-root> <output-json>");
            return 2;
        }

        var entries = File.ReadAllLines(args[0], new UTF8Encoding(false))
            .Where(line => line.Length > 0)
            .Select(line =>
            {
                int cut = line.IndexOf('\t');
                if (cut <= 0) throw new InvalidDataException("A file-list line lacks a tab: " + line);
                return (Path: line.Substring(0, cut), Project: line.Substring(cut + 1));
            })
            .OrderBy(entry => entry.Path, StringComparer.Ordinal)
            .ToArray();

        var options = new CSharpParseOptions(LanguageVersion.Preview, DocumentationMode.None);
        var texts = new string[entries.Length];
        var trees = new SyntaxTree[entries.Length];
        Parallel.For(0, entries.Length, i =>
        {
            string full = Path.Combine(args[1], entries[i].Path.Replace('/', Path.DirectorySeparatorChar));
            texts[i] = File.ReadAllText(full, Encoding.UTF8);
            trees[i] = CSharpSyntaxTree.ParseText(texts[i], options, entries[i].Path);
        });

        // First pass: the names of the enums, structs and record structs declared in the scanned sources.
        foreach (var tree in trees)
        {
            foreach (var declared in tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                if (declared is EnumDeclarationSyntax || declared is StructDeclarationSyntax || declared.IsKind(SyntaxKind.RecordStructDeclaration))
                {
                    ValueTypeNames.Add(declared.Identifier.ValueText);
                }
            }
        }

        var results = new FileScan[entries.Length];
        Parallel.For(0, entries.Length, i =>
        {
            var scan = new FileScan(entries[i].Path, entries[i].Project, trees[i]);
            scan.Run(texts[i]);
            results[i] = scan;
        });

        WriteResult(args[2], results, options);
        return 0;
    }

    private static void WriteResult(string outputPath, FileScan[] results, CSharpParseOptions options)
    {
        var total = new Tally("");
        var projects = new SortedDictionary<string, Tally>(StringComparer.Ordinal);
        foreach (var scan in results)
        {
            if (!projects.TryGetValue(scan.Project, out var project))
            {
                project = new Tally(scan.Project);
                projects[scan.Project] = project;
            }
            total.Add(scan);
            project.Add(scan);
        }

        using var stream = File.Create(outputPath);
        using var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });
        json.WriteStartObject();
        var assembly = typeof(CSharpSyntaxTree).Assembly;
        string roslyn = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>().Select(a => a.InformationalVersion.Split('+')[0]).FirstOrDefault()
            ?? assembly.GetName().Version?.ToString() ?? "";
        json.WriteString("roslyn", roslyn);
        json.WriteString("languageVersion", options.SpecifiedLanguageVersion.ToString());
        json.WriteNumber("files", total.Files);
        json.WriteNumber("lines", total.Lines);
        json.WriteNumber("nodes", total.Nodes);

        json.WriteStartArray("projects");
        foreach (var project in projects.Values)
        {
            json.WriteStartObject();
            json.WriteString("project", project.Name);
            json.WriteNumber("files", project.Files);
            json.WriteNumber("lines", project.Lines);
            json.WriteNumber("nodes", project.Nodes);
            json.WriteEndObject();
        }
        json.WriteEndArray();

        json.WriteStartArray("kinds");
        foreach (var pair in total.Kinds.OrderByDescending(p => p.Value.Count).ThenBy(p => p.Key, StringComparer.Ordinal))
        {
            json.WriteStartObject();
            json.WriteString("kind", pair.Key);
            json.WriteNumber("count", pair.Value.Count);
            json.WriteNumber("files", pair.Value.Files);
            json.WriteStartArray("projects");
            foreach (var project in projects.Values)
            {
                if (!project.Kinds.TryGetValue(pair.Key, out var cell)) continue;
                json.WriteStartObject();
                json.WriteString("project", project.Name);
                json.WriteNumber("count", cell.Count);
                json.WriteNumber("files", cell.Files);
                json.WriteEndObject();
            }
            json.WriteEndArray();
            json.WriteEndObject();
        }
        json.WriteEndArray();

        json.WriteStartArray("features");
        for (int f = 0; f < FeatureIds.Length; f++)
        {
            json.WriteStartObject();
            json.WriteString("id", FeatureIds[f]);
            json.WriteNumber("count", total.FeatureCount[f]);
            json.WriteNumber("files", total.FeatureFiles[f]);
            json.WriteString("first", total.FeatureFirst[f] ?? "");
            json.WriteStartArray("projects");
            foreach (var project in projects.Values)
            {
                if (project.FeatureCount[f] == 0) continue;
                json.WriteStartObject();
                json.WriteString("project", project.Name);
                json.WriteNumber("count", project.FeatureCount[f]);
                json.WriteNumber("files", project.FeatureFiles[f]);
                json.WriteEndObject();
            }
            json.WriteEndArray();
            json.WriteEndObject();
        }
        json.WriteEndArray();

        json.WriteStartArray("styles");
        for (int s = 0; s < StyleIds.Length; s++)
        {
            json.WriteStartObject();
            json.WriteString("id", StyleIds[s]);
            json.WriteNumber("a", total.StyleCount[s, 0]);
            json.WriteNumber("b", total.StyleCount[s, 1]);
            json.WriteNumber("filesA", total.StyleFiles[s, 0]);
            json.WriteNumber("filesB", total.StyleFiles[s, 1]);
            json.WriteString("firstA", total.StyleFirst[s, 0] ?? "");
            json.WriteString("firstB", total.StyleFirst[s, 1] ?? "");
            json.WriteStartArray("projects");
            foreach (var project in projects.Values)
            {
                if (project.StyleCount[s, 0] == 0 && project.StyleCount[s, 1] == 0) continue;
                json.WriteStartObject();
                json.WriteString("project", project.Name);
                json.WriteNumber("a", project.StyleCount[s, 0]);
                json.WriteNumber("b", project.StyleCount[s, 1]);
                json.WriteEndObject();
            }
            json.WriteEndArray();
            json.WriteEndObject();
        }
        json.WriteEndArray();

        json.WriteStartArray("attributes");
        foreach (var pair in total.Attributes.OrderByDescending(p => p.Value.Count).ThenBy(p => p.Key, StringComparer.Ordinal))
        {
            json.WriteStartObject();
            json.WriteString("name", pair.Key);
            json.WriteNumber("count", pair.Value.Count);
            json.WriteNumber("files", pair.Value.Files);
            json.WriteEndObject();
        }
        json.WriteEndArray();

        json.WriteStartArray("diagnostics");
        foreach (var scan in results)
        {
            foreach (var diagnostic in scan.Diagnostics)
            {
                var span = diagnostic.Location.GetLineSpan();
                json.WriteStartObject();
                json.WriteString("path", scan.Path);
                json.WriteNumber("line", span.StartLinePosition.Line + 1);
                json.WriteNumber("column", span.StartLinePosition.Character + 1);
                json.WriteString("id", diagnostic.Id);
                json.WriteString("message", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture));
                json.WriteEndObject();
            }
        }
        json.WriteEndArray();

        json.WriteEndObject();
    }
}

internal sealed class Cell
{
    public long Count;
    public long Files;
}

// Sums file scans in ordinal path order, so each first location is the first in that order.
internal sealed class Tally
{
    public Tally(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public long Files;
    public long Lines;
    public long Nodes;
    public readonly Dictionary<string, Cell> Kinds = new(StringComparer.Ordinal);
    public readonly Dictionary<string, Cell> Attributes = new(StringComparer.Ordinal);
    public readonly long[] FeatureCount = new long[Program.FeatureIds.Length];
    public readonly long[] FeatureFiles = new long[Program.FeatureIds.Length];
    public readonly string?[] FeatureFirst = new string?[Program.FeatureIds.Length];
    public readonly long[,] StyleCount = new long[Program.StyleIds.Length, 2];
    public readonly long[,] StyleFiles = new long[Program.StyleIds.Length, 2];
    public readonly string?[,] StyleFirst = new string?[Program.StyleIds.Length, 2];

    public void Add(FileScan scan)
    {
        Files++;
        Lines += scan.Lines;
        Nodes += scan.Nodes;
        Merge(Kinds, scan.Kinds);
        Merge(Attributes, scan.Attributes);
        for (int f = 0; f < FeatureCount.Length; f++)
        {
            if (scan.FeatureCount[f] == 0) continue;
            FeatureCount[f] += scan.FeatureCount[f];
            FeatureFiles[f]++;
            FeatureFirst[f] ??= scan.Path + ":" + scan.FeatureLine[f];
        }
        for (int s = 0; s < StyleCount.GetLength(0); s++)
        {
            for (int side = 0; side < 2; side++)
            {
                if (scan.StyleCount[s, side] == 0) continue;
                StyleCount[s, side] += scan.StyleCount[s, side];
                StyleFiles[s, side]++;
                StyleFirst[s, side] ??= scan.Path + ":" + scan.StyleLine[s, side];
            }
        }
    }

    private static void Merge(Dictionary<string, Cell> target, Dictionary<string, int> source)
    {
        foreach (var pair in source)
        {
            if (!target.TryGetValue(pair.Key, out var cell))
            {
                cell = new Cell();
                target[pair.Key] = cell;
            }
            cell.Count += pair.Value;
            cell.Files++;
        }
    }
}

// Counts one syntax tree. Every detector reads bare syntax: nothing is bound.
internal sealed class FileScan
{
    private readonly SyntaxTree _tree;

    public FileScan(string path, string project, SyntaxTree tree)
    {
        Path = path;
        Project = project;
        _tree = tree;
    }

    public string Path { get; }
    public string Project { get; }
    public long Lines;
    public long Nodes;
    public readonly Dictionary<string, int> Kinds = new(StringComparer.Ordinal);
    public readonly Dictionary<string, int> Attributes = new(StringComparer.Ordinal);
    public readonly int[] FeatureCount = new int[Program.FeatureIds.Length];
    public readonly int[] FeatureLine = new int[Program.FeatureIds.Length];
    public readonly int[,] StyleCount = new int[Program.StyleIds.Length, 2];
    public readonly int[,] StyleLine = new int[Program.StyleIds.Length, 2];
    public readonly List<Diagnostic> Diagnostics = new();

    public void Run(string text)
    {
        Lines = CountLines(text);
        Diagnostics.AddRange(_tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        var root = _tree.GetRoot();
        foreach (var node in root.DescendantNodesAndSelf(descendIntoTrivia: true))
        {
            Nodes++;
            string kind = node.Kind().ToString();
            Kinds[kind] = Kinds.TryGetValue(kind, out int seen) ? seen + 1 : 1;
            Detect(node, kind);
        }
    }

    private static long CountLines(string text)
    {
        if (text.Length == 0) return 0;
        long lines = 0;
        foreach (char c in text) if (c == '\n') lines++;
        if (text[text.Length - 1] != '\n') lines++;
        return lines;
    }

    private int LineOf(SyntaxNode node) => _tree.GetLineSpan(node.Span).StartLinePosition.Line + 1;

    private void Hit(string id, SyntaxNode node)
    {
        int f = Program.FeatureIndex[id];
        if (FeatureCount[f]++ == 0) FeatureLine[f] = LineOf(node);
    }

    private void Side(string id, bool a, SyntaxNode node)
    {
        int s = Program.StyleIndex[id];
        int side = a ? 0 : 1;
        if (StyleCount[s, side]++ == 0) StyleLine[s, side] = LineOf(node);
    }

    private static bool Has(SyntaxTokenList modifiers, SyntaxKind kind) => modifiers.Any(kind);

    private static bool InInterface(SyntaxNode node) => node.Parent is InterfaceDeclarationSyntax;

    private static bool InStruct(SyntaxNode node)
    {
        for (var parent = node.Parent; parent != null; parent = parent.Parent)
        {
            if (parent is TypeDeclarationSyntax type)
            {
                return type.IsKind(SyntaxKind.StructDeclaration) || type.IsKind(SyntaxKind.RecordStructDeclaration);
            }
        }
        return false;
    }

    private static bool IsNull(ExpressionSyntax? expression) => expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.NullLiteralExpression);

    // A static abstract or static virtual member of an interface.
    private static bool IsStaticAbstract(MemberDeclarationSyntax member) =>
        InInterface(member) && Has(member.Modifiers, SyntaxKind.StaticKeyword) &&
        (Has(member.Modifiers, SyntaxKind.AbstractKeyword) || Has(member.Modifiers, SyntaxKind.VirtualKeyword));

    private void MemberCommon(MemberDeclarationSyntax member, bool hasBody)
    {
        if (IsStaticAbstract(member)) Hit("static-abstract-interface-member", member);
        if (InInterface(member) && hasBody) Hit("default-interface-method", member);
        if (Has(member.Modifiers, SyntaxKind.ReadOnlyKeyword) && member is not BaseFieldDeclarationSyntax && InStruct(member)) Hit("readonly-struct-member", member);
        if (Has(member.Modifiers, SyntaxKind.RequiredKeyword)) Hit("required-member", member);
    }

    private void TypeCommon(BaseTypeDeclarationSyntax type)
    {
        if (Has(type.Modifiers, SyntaxKind.PartialKeyword)) Hit("partial-type", type);
        if (Has(type.Modifiers, SyntaxKind.FileKeyword)) Hit("file-local-type", type);
        if (type.Parent is BaseTypeDeclarationSyntax) Hit("nested-type", type);
        if (type is TypeDeclarationSyntax generic && generic.TypeParameterList != null) Hit("generic-type", type);
    }

    private void BodyStyle(SyntaxNode node, BlockSyntax? body, ArrowExpressionClauseSyntax? arrow)
    {
        if (arrow != null) Side("member-body", true, node);
        else if (body != null) Side("member-body", false, node);
    }

    private void Detect(SyntaxNode node, string kind)
    {
        switch (kind)
        {
            case "ExtensionBlockDeclaration":
                Hit("extension-block", node);
                return;
            case "FieldExpression":
                Hit("field-keyword", node);
                return;
        }

        switch (node)
        {
            // Types.
            case ClassDeclarationSyntax type:
                Hit("class", type);
                TypeCommon(type);
                if (Has(type.Modifiers, SyntaxKind.StaticKeyword)) Hit("static-class", type);
                if (Has(type.Modifiers, SyntaxKind.SealedKeyword)) Hit("sealed-class", type);
                if (Has(type.Modifiers, SyntaxKind.AbstractKeyword)) Hit("abstract-class", type);
                ConstructorStyle(type);
                return;
            case StructDeclarationSyntax type:
                Hit("struct", type);
                TypeCommon(type);
                ConstructorStyle(type);
                return;
            case InterfaceDeclarationSyntax type:
                Hit("interface", type);
                TypeCommon(type);
                return;
            case RecordDeclarationSyntax type:
                if (type.IsKind(SyntaxKind.RecordStructDeclaration))
                {
                    Hit("record-struct", type);
                }
                else
                {
                    Hit("record-class", type);
                    if (Has(type.Modifiers, SyntaxKind.SealedKeyword)) Hit("sealed-class", type);
                    if (Has(type.Modifiers, SyntaxKind.AbstractKeyword)) Hit("abstract-class", type);
                }
                TypeCommon(type);
                return;
            case EnumDeclarationSyntax type:
                Hit("enum", type);
                TypeCommon(type);
                return;
            case DelegateDeclarationSyntax type:
                Hit("delegate", type);
                if (Has(type.Modifiers, SyntaxKind.FileKeyword)) Hit("file-local-type", type);
                if (type.Parent is BaseTypeDeclarationSyntax) Hit("nested-type", type);
                if (type.TypeParameterList != null) Hit("generic-type", type);
                return;

            // Namespaces.
            case FileScopedNamespaceDeclarationSyntax:
                Hit("file-scoped-namespace", node);
                Side("namespace", true, node);
                return;
            case NamespaceDeclarationSyntax:
                Hit("block-namespace", node);
                Side("namespace", false, node);
                return;
            case UsingDirectiveSyntax directive:
                if (directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword)) Hit("global-using", directive);
                if (directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)) Hit("using-static", directive);
                if (directive.Alias != null)
                {
                    Hit("using-alias", directive);
                    if (directive.NamespaceOrType is not NameSyntax) Hit("alias-any-type", directive);
                }
                return;

            // Members.
            case PropertyDeclarationSyntax property:
            {
                bool hasBody = property.ExpressionBody != null ||
                    (property.AccessorList?.Accessors.Any(a => a.Body != null || a.ExpressionBody != null) ?? false);
                if (hasBody) Hit("full-property", property);
                else if (!InInterface(property) && !Has(property.Modifiers, SyntaxKind.AbstractKeyword) && !Has(property.Modifiers, SyntaxKind.ExternKeyword)) Hit("auto-property", property);
                MemberCommon(property, hasBody);
                if (property.ExpressionBody != null) Side("member-body", true, property);
                return;
            }
            case IndexerDeclarationSyntax indexer:
            {
                bool hasBody = indexer.ExpressionBody != null ||
                    (indexer.AccessorList?.Accessors.Any(a => a.Body != null || a.ExpressionBody != null) ?? false);
                Hit("indexer", indexer);
                MemberCommon(indexer, hasBody);
                if (indexer.ExpressionBody != null) Side("member-body", true, indexer);
                return;
            }
            case AccessorDeclarationSyntax accessor:
                if (accessor.IsKind(SyntaxKind.InitAccessorDeclaration)) Hit("init-accessor", accessor);
                if (Has(accessor.Modifiers, SyntaxKind.ReadOnlyKeyword)) Hit("readonly-struct-member", accessor);
                BodyStyle(accessor, accessor.Body, accessor.ExpressionBody);
                return;
            case EventFieldDeclarationSyntax eventField:
                Hit("field-like-event", eventField);
                MemberCommon(eventField, false);
                return;
            case EventDeclarationSyntax eventDeclaration:
                Hit("event-accessors", eventDeclaration);
                MemberCommon(eventDeclaration, eventDeclaration.AccessorList?.Accessors.Any(a => a.Body != null || a.ExpressionBody != null) ?? false);
                return;
            case FieldDeclarationSyntax field:
                if (Has(field.Modifiers, SyntaxKind.ConstKeyword)) Hit("const-field", field);
                if (Has(field.Modifiers, SyntaxKind.ReadOnlyKeyword)) Hit("readonly-field", field);
                MemberCommon(field, false);
                return;
            case MethodDeclarationSyntax method:
                if (method.ParameterList.Parameters.Count > 0 && Has(method.ParameterList.Parameters[0].Modifiers, SyntaxKind.ThisKeyword)) Hit("extension-method", method);
                if (method.TypeParameterList != null) Hit("generic-method", method);
                if (Has(method.Modifiers, SyntaxKind.AsyncKeyword)) Hit("async-method", method);
                MemberCommon(method, method.Body != null || method.ExpressionBody != null);
                BodyStyle(method, method.Body, method.ExpressionBody);
                return;
            case OperatorDeclarationSyntax op:
                Hit("operator-overload", op);
                MemberCommon(op, op.Body != null || op.ExpressionBody != null);
                BodyStyle(op, op.Body, op.ExpressionBody);
                return;
            case ConversionOperatorDeclarationSyntax conversion:
                Hit("conversion-operator", conversion);
                MemberCommon(conversion, conversion.Body != null || conversion.ExpressionBody != null);
                BodyStyle(conversion, conversion.Body, conversion.ExpressionBody);
                return;
            case ConstructorDeclarationSyntax constructor:
                Hit(Has(constructor.Modifiers, SyntaxKind.StaticKeyword) ? "static-constructor" : "constructor", constructor);
                BodyStyle(constructor, constructor.Body, constructor.ExpressionBody);
                return;
            case DestructorDeclarationSyntax destructor:
                Hit("finalizer", destructor);
                BodyStyle(destructor, destructor.Body, destructor.ExpressionBody);
                return;
            case ArrowExpressionClauseSyntax arrow:
                if (arrow.Parent is not LocalFunctionStatementSyntax) Hit("expression-bodied-member", arrow);
                return;
            case ParameterSyntax parameter:
                if (Has(parameter.Modifiers, SyntaxKind.ParamsKeyword) && parameter.Type is not ArrayTypeSyntax) Hit("params-collection", parameter);
                if (parameter.Default != null) Hit("optional-parameter", parameter);
                if (Has(parameter.Modifiers, SyntaxKind.RefKeyword) || Has(parameter.Modifiers, SyntaxKind.OutKeyword)) Hit("ref-out-parameter", parameter);
                if (Has(parameter.Modifiers, SyntaxKind.InKeyword)) Hit("in-parameter", parameter);
                return;
            case ArgumentSyntax argument:
                if (argument.NameColon != null && argument.Parent is not TupleExpressionSyntax) Hit("named-argument", argument);
                if (argument.Expression is IdentifierNameSyntax { Identifier.ValueText: "_" } &&
                    (argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword) || (argument.Parent is TupleExpressionSyntax target && IsDeconstructionTarget(target)))) Hit("discard", argument);
                return;
            case AttributeArgumentSyntax attributeArgument:
                if (attributeArgument.NameColon != null) Hit("named-argument", attributeArgument);
                return;
            case RefTypeSyntax:
                Hit("ref-return-local", node);
                return;
            case LocalFunctionStatementSyntax local:
                Hit("local-function", local);
                if (Has(local.Modifiers, SyntaxKind.StaticKeyword)) Hit("static-local-function", local);
                if (local.TypeParameterList != null) Hit("generic-method", local);
                if (Has(local.Modifiers, SyntaxKind.AsyncKeyword)) Hit("async-method", local);
                return;

            // Patterns.
            case TypePatternSyntax:
                Hit("type-pattern", node);
                return;
            case DeclarationPatternSyntax:
                Hit("declaration-pattern", node);
                return;
            case ConstantPatternSyntax constant:
                if (IsNull(constant.Expression) &&
                    (constant.Parent is IsPatternExpressionSyntax || (constant.Parent is UnaryPatternSyntax && constant.Parent.Parent is IsPatternExpressionSyntax)))
                {
                    Hit("is-null", constant);
                    Side("null-check", true, constant);
                }
                else
                {
                    Hit("constant-pattern", constant);
                }
                return;
            case UnaryPatternSyntax:
                Hit("not-pattern", node);
                return;
            case BinaryPatternSyntax:
                Hit("and-or-pattern", node);
                return;
            case RelationalPatternSyntax:
                Hit("relational-pattern", node);
                return;
            case PropertyPatternClauseSyntax:
                Hit("property-pattern", node);
                return;
            case PositionalPatternClauseSyntax:
                Hit("positional-pattern", node);
                return;
            case ListPatternSyntax:
                Hit("list-pattern", node);
                return;
            case VarPatternSyntax:
                Hit("var-pattern", node);
                return;
            case DiscardPatternSyntax:
                Hit("discard-pattern", node);
                return;
            case SwitchExpressionSyntax:
                Hit("switch-expression", node);
                Side("branch", true, node);
                return;
            case SwitchStatementSyntax:
                Hit("switch-statement", node);
                Side("branch", false, node);
                return;
            case WhenClauseSyntax:
                Hit("case-guard", node);
                return;
            case IsPatternExpressionSyntax isPattern:
                if (isPattern.Pattern is DeclarationPatternSyntax { Designation: SingleVariableDesignationSyntax } ||
                    isPattern.Pattern is RecursivePatternSyntax { Designation: SingleVariableDesignationSyntax })
                {
                    Side("type-test", true, isPattern);
                }
                return;

            // Expressions.
            case AnonymousMethodExpressionSyntax anonymous:
                Hit("anonymous-method", anonymous);
                if (Has(anonymous.Modifiers, SyntaxKind.StaticKeyword)) Hit("static-lambda", anonymous);
                if (anonymous.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword)) Hit("async-lambda", anonymous);
                return;
            case LambdaExpressionSyntax lambda:
                Hit("lambda", lambda);
                if (Has(lambda.Modifiers, SyntaxKind.StaticKeyword)) Hit("static-lambda", lambda);
                if (lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword)) Hit("async-lambda", lambda);
                if (lambda.ExpressionBody != null) Side("lambda-body", true, lambda);
                else if (lambda.Block != null) Side("lambda-body", false, lambda);
                return;
            case ImplicitObjectCreationExpressionSyntax implicitCreation:
                Hit("target-typed-new", implicitCreation);
                Side("object-creation", true, implicitCreation);
                if (implicitCreation.Initializer is { } implicitInitializer && implicitInitializer.IsKind(SyntaxKind.CollectionInitializerExpression)) Side("collection-creation", false, implicitCreation);
                return;
            case ObjectCreationExpressionSyntax creation:
                Side("object-creation", false, creation);
                if (creation.Initializer is { } initializer && initializer.IsKind(SyntaxKind.CollectionInitializerExpression)) Side("collection-creation", false, creation);
                return;
            case ArrayCreationExpressionSyntax arrayCreation:
                if (arrayCreation.Initializer != null) Side("collection-creation", false, arrayCreation);
                return;
            case ImplicitArrayCreationExpressionSyntax:
                Side("collection-creation", false, node);
                return;
            case CollectionExpressionSyntax:
                Hit("collection-expression", node);
                Side("collection-creation", true, node);
                return;
            case SpreadElementSyntax:
                Hit("spread-element", node);
                return;
            case WithExpressionSyntax:
                Hit("with-expression", node);
                return;
            case TupleExpressionSyntax tuple:
                if (!IsDeconstructionTarget(tuple)) Hit("tuple-literal", tuple);
                return;
            case TupleTypeSyntax:
                Hit("tuple-literal", node);
                return;
            case AssignmentExpressionSyntax assignment:
                if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    if (assignment.Left is TupleExpressionSyntax || assignment.Left is DeclarationExpressionSyntax) Hit("deconstruction", assignment);
                    if (assignment.Left is IdentifierNameSyntax { Identifier.ValueText: "_" }) Hit("discard", assignment);
                }
                else if (assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression))
                {
                    Hit("null-coalescing-assignment", assignment);
                }
                return;
            case ForEachVariableStatementSyntax forEachVariable:
                Hit("deconstruction", forEachVariable);
                Hit("foreach", forEachVariable);
                if (forEachVariable.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword)) Hit("await-foreach", forEachVariable);
                return;
            case DiscardDesignationSyntax:
                Hit("discard", node);
                return;
            case RangeExpressionSyntax:
                Hit("range", node);
                return;
            case ConditionalAccessExpressionSyntax access:
                Hit(access.WhenNotNull is AssignmentExpressionSyntax ? "null-conditional-assignment" : "null-conditional", access);
                return;
            case PostfixUnaryExpressionSyntax postfix:
                if (postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression)) Hit("null-forgiving", postfix);
                return;
            case PrefixUnaryExpressionSyntax prefix:
                if (prefix.IsKind(SyntaxKind.IndexExpression)) Hit("index-from-end", prefix);
                return;
            case InvocationExpressionSyntax invocation:
                if (invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" }) Hit("nameof", invocation);
                if (invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Format" } member &&
                    (member.Expression is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.StringKeyword } || member.Expression is IdentifierNameSyntax { Identifier.ValueText: "String" }))
                {
                    Side("text-building", false, invocation);
                }
                return;
            case InterpolatedStringExpressionSyntax interpolated:
                Hit("interpolated-string", interpolated);
                Side("text-building", true, interpolated);
                if (interpolated.StringStartToken.IsKind(SyntaxKind.InterpolatedVerbatimStringStartToken)) Hit("verbatim-string", interpolated);
                if (interpolated.StringStartToken.IsKind(SyntaxKind.InterpolatedSingleLineRawStringStartToken) ||
                    interpolated.StringStartToken.IsKind(SyntaxKind.InterpolatedMultiLineRawStringStartToken)) Hit("raw-string", interpolated);
                return;
            case LiteralExpressionSyntax literal:
                if (literal.IsKind(SyntaxKind.Utf8StringLiteralExpression)) Hit("utf8-literal", literal);
                else if (literal.IsKind(SyntaxKind.DefaultLiteralExpression)) Hit("default-literal", literal);
                else if (literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    if (literal.Token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken) || literal.Token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken)) Hit("raw-string", literal);
                    else if (literal.Token.Text.StartsWith("@", StringComparison.Ordinal)) Hit("verbatim-string", literal);
                }
                return;
            case ThrowExpressionSyntax:
                Hit("throw-expression", node);
                return;
            case ConditionalExpressionSyntax:
                Hit("conditional-operator", node);
                return;
            case CheckedExpressionSyntax:
                Hit("checked-expression", node);
                return;
            case TypeOfExpressionSyntax:
                Hit("typeof", node);
                return;
            case SizeOfExpressionSyntax:
                Hit("sizeof", node);
                return;
            case StackAllocArrayCreationExpressionSyntax:
            case ImplicitStackAllocArrayCreationExpressionSyntax:
                Hit("stackalloc", node);
                return;
            case AnonymousObjectCreationExpressionSyntax:
                Hit("anonymous-type", node);
                return;
            case InitializerExpressionSyntax initializerExpression:
                if (initializerExpression.IsKind(SyntaxKind.ObjectInitializerExpression)) Hit("object-initializer", initializerExpression);
                else if (initializerExpression.IsKind(SyntaxKind.CollectionInitializerExpression)) Hit("collection-initializer", initializerExpression);
                return;
            case QueryExpressionSyntax:
                Hit("query-expression", node);
                return;
            case AwaitExpressionSyntax:
                Hit("await", node);
                return;
            case CastExpressionSyntax:
                Hit("cast-expression", node);
                return;
            case DefaultExpressionSyntax:
                Hit("default-of-t", node);
                return;
            case BinaryExpressionSyntax binary:
                DetectBinary(binary);
                return;
            case VariableDeclarationSyntax declaration:
                if (declaration.Parent is LocalDeclarationStatementSyntax localOwner && Has(localOwner.Modifiers, SyntaxKind.ConstKeyword)) return;
                if (declaration.Parent is LocalDeclarationStatementSyntax || declaration.Parent is ForStatementSyntax || declaration.Parent is UsingStatementSyntax)
                {
                    Side("local-type", declaration.Type.IsVar, declaration);
                }
                return;
            case DeclarationExpressionSyntax declarationExpression:
                Side("local-type", declarationExpression.Type.IsVar, declarationExpression);
                return;

            // Statements.
            case IfStatementSyntax:
                Hit("if", node);
                return;
            case ForStatementSyntax:
                Hit("for", node);
                return;
            case ForEachStatementSyntax forEach:
                Hit("foreach", forEach);
                if (forEach.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword)) Hit("await-foreach", forEach);
                Side("local-type", forEach.Type.IsVar, forEach);
                return;
            case WhileStatementSyntax:
                Hit("while", node);
                return;
            case DoStatementSyntax:
                Hit("do", node);
                return;
            case TryStatementSyntax:
                Hit("try", node);
                return;
            case CatchClauseSyntax:
                Hit("catch", node);
                return;
            case FinallyClauseSyntax:
                Hit("finally", node);
                return;
            case CatchFilterClauseSyntax:
                Hit("exception-filter", node);
                return;
            case UsingStatementSyntax usingStatement:
                Hit("using-statement", usingStatement);
                Side("using", false, usingStatement);
                if (usingStatement.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword)) Hit("await-using", usingStatement);
                return;
            case LocalDeclarationStatementSyntax localDeclaration:
                if (localDeclaration.UsingKeyword.IsKind(SyntaxKind.UsingKeyword))
                {
                    Hit("using-declaration", localDeclaration);
                    Side("using", true, localDeclaration);
                    if (localDeclaration.AwaitKeyword.IsKind(SyntaxKind.AwaitKeyword)) Hit("await-using", localDeclaration);
                }
                if (Has(localDeclaration.Modifiers, SyntaxKind.ConstKeyword)) Hit("local-const", localDeclaration);
                return;
            case LockStatementSyntax:
                Hit("lock", node);
                return;
            case YieldStatementSyntax:
                Hit("yield", node);
                return;
            case GotoStatementSyntax:
                Hit("goto", node);
                return;
            case FixedStatementSyntax:
                Hit("fixed", node);
                return;
            case UnsafeStatementSyntax:
                Hit("unsafe-block", node);
                return;
            case CheckedStatementSyntax:
                Hit("checked-block", node);
                return;

            // Generics.
            case TypeParameterConstraintClauseSyntax:
                Hit("constraint-clause", node);
                return;
            case TypeConstraintSyntax constraint:
                if (constraint.Type is IdentifierNameSyntax { Identifier.ValueText: "notnull" }) Hit("notnull-constraint", constraint);
                else if (constraint.Type is IdentifierNameSyntax { Identifier.ValueText: "unmanaged" }) Hit("unmanaged-constraint", constraint);
                return;
            case TypeParameterSyntax typeParameter:
                if (!typeParameter.VarianceKeyword.IsKind(SyntaxKind.None)) Hit("variance", typeParameter);
                return;

            // Nullability and unsafe types.
            case NullableTypeSyntax nullable:
                Hit(IsValueType(nullable.ElementType) ? "nullable-value-annotation" : "nullable-reference-annotation", nullable);
                return;
            case PointerTypeSyntax:
                Hit("pointer-type", node);
                return;
            case FunctionPointerTypeSyntax:
                Hit("function-pointer", node);
                return;

            // Directives.
            case NullableDirectiveTriviaSyntax:
                Hit("nullable-directive", node);
                return;
            case IfDirectiveTriviaSyntax:
                Hit("if-directive", node);
                return;
            case RegionDirectiveTriviaSyntax:
                Hit("region-directive", node);
                return;
            case PragmaWarningDirectiveTriviaSyntax:
            case PragmaChecksumDirectiveTriviaSyntax:
                Hit("pragma-directive", node);
                return;

            // Attributes.
            case AttributeSyntax attribute:
                AddAttribute(attribute);
                return;
        }
    }

    private void DetectBinary(BinaryExpressionSyntax binary)
    {
        switch (binary.Kind())
        {
            case SyntaxKind.IsExpression:
                Hit("is-type", binary);
                return;
            case SyntaxKind.AsExpression:
                Hit("as-cast", binary);
                Side("type-test", false, binary);
                return;
            case SyntaxKind.CoalesceExpression:
                Hit("null-coalescing", binary);
                return;
            case SyntaxKind.EqualsExpression:
            case SyntaxKind.NotEqualsExpression:
                if (IsNull(binary.Left) || IsNull(binary.Right)) Side("null-check", false, binary);
                return;
            case SyntaxKind.AddExpression:
                if (binary.Parent is BinaryExpressionSyntax outer && outer.IsKind(SyntaxKind.AddExpression)) return;
                if (HasStringOperand(binary)) Side("text-building", false, binary);
                return;
        }
    }

    // True when a tuple, alone or nested in tuples, is the left side of a deconstruction or the variable of a foreach.
    private static bool IsDeconstructionTarget(TupleExpressionSyntax tuple)
    {
        SyntaxNode top = tuple;
        while (top.Parent is ArgumentSyntax && top.Parent.Parent is TupleExpressionSyntax outer) top = outer;
        return (top.Parent is AssignmentExpressionSyntax owner && owner.Left == top) || (top.Parent is ForEachVariableStatementSyntax loop && loop.Variable == top);
    }

    // T? is a value annotation when T is a predefined value type, a tuple, a listed BCL struct or a declared enum, struct or record struct; else a reference annotation.
    private static bool IsValueType(TypeSyntax type)
    {
        switch (type)
        {
            case PredefinedTypeSyntax predefined:
                return !predefined.Keyword.IsKind(SyntaxKind.StringKeyword) && !predefined.Keyword.IsKind(SyntaxKind.ObjectKeyword);
            case TupleTypeSyntax:
                return true;
            case QualifiedNameSyntax qualified:
                return IsValueType(qualified.Right);
            case AliasQualifiedNameSyntax aliased:
                return IsValueType(aliased.Name);
            case SimpleNameSyntax simple:
                return Program.ValueTypeNames.Contains(simple.Identifier.ValueText);
            default:
                return false;
        }
    }

    // True when a chain of + holds a string literal operand.
    private static bool HasStringOperand(ExpressionSyntax expression)
    {
        if (expression is BinaryExpressionSyntax add && add.IsKind(SyntaxKind.AddExpression)) return HasStringOperand(add.Left) || HasStringOperand(add.Right);
        return expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression);
    }

    private void ConstructorStyle(TypeDeclarationSyntax type)
    {
        if (type.ParameterList != null)
        {
            Hit("primary-constructor", type);
            Side("constructor", true, type);
        }
        else if (type.Members.OfType<ConstructorDeclarationSyntax>().Any(c => !Has(c.Modifiers, SyntaxKind.StaticKeyword)))
        {
            Side("constructor", false, type);
        }
    }

    private void AddAttribute(AttributeSyntax attribute)
    {
        NameSyntax name = attribute.Name;
        if (name is QualifiedNameSyntax qualified) name = qualified.Right;
        else if (name is AliasQualifiedNameSyntax aliased) name = aliased.Name;
        string text = name is SimpleNameSyntax simple ? simple.Identifier.ValueText : name.ToString();
        if (text.Length > "Attribute".Length && text.EndsWith("Attribute", StringComparison.Ordinal)) text = text.Substring(0, text.Length - "Attribute".Length);
        Attributes[text] = Attributes.TryGetValue(text, out int seen) ? seen + 1 : 1;
    }
}
'@

function Invoke-StatsNative {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = @(& $FilePath @Arguments 2>&1 | ForEach-Object { [string]$_ })
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $nativePreference
    }

    return [pscustomobject]@{ Lines = $output; ExitCode = $exitCode }
}

# Builds the helper once into the temporary folder, keyed by its text, the framework and the SDK version.
function Get-HelperBinary {
    param([string]$DotnetPath, [string]$ProjectName, [string]$TargetFramework)

    $sdk = Invoke-StatsNative -FilePath $DotnetPath -Arguments @('--version')
    if ($sdk.ExitCode -ne 0) {
        throw "The .NET SDK version could not be read.`n$($sdk.Lines -join [Environment]::NewLine)"
    }

    $sdkVersion = ([string]($sdk.Lines | Select-Object -First 1)).Trim()
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $seed = $script:HelperProject + "`n" + $script:HelperProgram + "`n" + $TargetFramework + "`n" + $sdkVersion
        $digest = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($seed))
    }
    finally {
        $sha.Dispose()
    }

    $hash = ([System.BitConverter]::ToString($digest) -replace '-', '').Substring(0, 16).ToLowerInvariant()
    $cacheParent = Join-Path ([System.IO.Path]::GetTempPath()) ($ProjectName + '-StatsSyntax')
    $cacheFolder = Join-Path $cacheParent $hash
    $binaryPath = Join-Path (Join-Path $cacheFolder 'bin') 'StatsSyntax.Helper.dll'
    if (Test-Path -LiteralPath $binaryPath -PathType Leaf) {
        return [pscustomobject]@{ Path = $binaryPath; Built = $false; Sdk = $sdkVersion }
    }

    Write-StatsLine 'Compiling the syntax parser once for this SDK...' -ForegroundColor DarkGray
    if (Test-Path -LiteralPath $cacheParent) {
        Get-ChildItem -LiteralPath $cacheParent -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }

    $helperFolder = Join-Path $cacheFolder 'helper'
    [System.IO.Directory]::CreateDirectory($helperFolder) | Out-Null
    $encoding = [System.Text.UTF8Encoding]::new($false)
    $projectPath = Join-Path $helperFolder 'StatsSyntax.Helper.csproj'
    [System.IO.File]::WriteAllText($projectPath, $script:HelperProject.Replace('{TARGET_FRAMEWORK}', $TargetFramework), $encoding)
    [System.IO.File]::WriteAllText((Join-Path $helperFolder 'Program.cs'), $script:HelperProgram, $encoding)
    $build = Invoke-StatsNative -FilePath $DotnetPath -Arguments @('build', $projectPath, '--configuration', 'Release', '--nologo', '--verbosity', 'quiet', '--output', (Join-Path $cacheFolder 'bin'))
    if ($build.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $binaryPath -PathType Leaf)) {
        Remove-Item -LiteralPath $cacheFolder -Recurse -Force -ErrorAction SilentlyContinue
        throw "The syntax parser could not be built.`n$($build.Lines -join [Environment]::NewLine)"
    }

    return [pscustomobject]@{ Path = $binaryPath; Built = $true; Sdk = $sdkVersion }
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    throw 'The .NET SDK is required, but dotnet was not found on PATH.'
}

$previousNoLogo = $env:DOTNET_NOLOGO
$previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$temporaryFolder = Join-Path ([System.IO.Path]::GetTempPath()) ([string]$config.project + '-StatsSyntax-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($temporaryFolder) | Out-Null

try {
    $helper = Get-HelperBinary -DotnetPath $dotnet.Source -ProjectName ([string]$config.project) -TargetFramework $helperFramework
    $listPath = Join-Path $temporaryFolder 'files.txt'
    $resultPath = Join-Path $temporaryFolder 'syntax.json'
    $listText = (@($codeFiles | ForEach-Object { $_.Relative + "`t" + $_.Project }) -join "`n") + "`n"
    [System.IO.File]::WriteAllText($listPath, $listText, [System.Text.UTF8Encoding]::new($false))

    $run = Invoke-StatsNative -FilePath $dotnet.Source -Arguments @($helper.Path, $listPath, $repoRootFull, $resultPath)
    if ($run.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        throw "The syntax parser failed.`n$($run.Lines -join [Environment]::NewLine)"
    }

    $raw = Get-Content -LiteralPath $resultPath -Raw -Encoding UTF8 | ConvertFrom-Json
}
finally {
    $env:DOTNET_NOLOGO = $previousNoLogo
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = $previousTelemetry
    Remove-Item -LiteralPath $temporaryFolder -Recurse -Force -ErrorAction SilentlyContinue
}

# The catalog and the helper must name the same features and styles.
function Assert-SameIds {
    param(
        [Parameter(Mandatory = $true)][string]$What,
        [Parameter(Mandatory = $true)][string[]]$Catalog,
        [Parameter(Mandatory = $true)][string[]]$Helper
    )

    $missingInCatalog = @($Helper | Where-Object { $_ -cnotin $Catalog })
    $missingInHelper = @($Catalog | Where-Object { $_ -cnotin $Helper })
    if ($missingInCatalog.Count -gt 0 -or $missingInHelper.Count -gt 0) {
        $lines = @()
        if ($missingInCatalog.Count -gt 0) { $lines += "in the helper but not in StatsSyntax.json: " + ($missingInCatalog -join ', ') }
        if ($missingInHelper.Count -gt 0) { $lines += "in StatsSyntax.json but not in the helper: " + ($missingInHelper -join ', ') }
        throw "The $What catalog and the helper disagree.`n  " + ($lines -join "`n  ")
    }
}

Assert-SameIds -What 'feature' -Catalog @($config.features | ForEach-Object { [string]$_.id }) -Helper @($raw.features | ForEach-Object { [string]$_.id })
Assert-SameIds -What 'style' -Catalog @($config.styles | ForEach-Object { [string]$_.id }) -Helper @($raw.styles | ForEach-Object { [string]$_.id })
if ([long]$raw.files -ne $codeFiles.Count) {
    throw "The syntax parser read $($raw.files) files but $($codeFiles.Count) were listed."
}

# Version, read from the configured version file and key.
$version = "0.0.0"
if ([System.IO.File]::Exists($versionPathFull)) {
    try {
        $versionData = Get-Content -LiteralPath $versionPathFull -Raw -Encoding UTF8 | ConvertFrom-Json
        $versionValue = Get-ConfigNode -Document $versionData -Key $versionKey
        if (-not [string]::IsNullOrWhiteSpace([string]$versionValue)) { $version = [string]$versionValue }
    }
    catch {
        Write-StatsLine "The version file is not valid JSON, using $version : $versionPathFull"
    }
}
else {
    Write-StatsLine "The version file was not found, using $version : $versionPathFull"
}

$generatedAt = Get-Date
$reportPathFull = Join-Path $reportDirectoryFull ("{0}{1}.md" -f $reportPrefix, $version)
$pagePathFull = Join-Path $reportDirectoryFull ("{0}{1}.html" -f $reportPrefix, $version)

function ConvertTo-ProjectCounts {
    param([AllowNull()][object[]]$Rows)

    return , @($Rows | Where-Object { $null -ne $_ } | ForEach-Object { [ordered]@{ project = [string]$_.project; count = [long]$_.count; files = [long]$_.files } })
}

# The result: every number built once, read by the console and the report alike.
$helperFeatures = @{}
foreach ($row in @($raw.features)) { $helperFeatures[[string]$row.id] = $row }
$helperStyles = @{}
foreach ($row in @($raw.styles)) { $helperStyles[[string]$row.id] = $row }

$featureTable = @(foreach ($entry in @($config.features)) {
    $row = $helperFeatures[[string]$entry.id]
    [ordered]@{
        id = [string]$entry.id
        label = [string]$entry.label
        family = [string]$entry.family
        version = [string]$entry.version
        count = [long]$row.count
        perThousand = $(if ([long]$raw.lines -gt 0) { 1000.0 * [long]$row.count / [long]$raw.lines } else { 0.0 })
        files = [long]$row.files
        first = [string]$row.first
        projects = (ConvertTo-ProjectCounts @($row.projects))
    }
})

$styleTable = @(foreach ($entry in @($config.styles)) {
    $row = $helperStyles[[string]$entry.id]
    $sum = [long]$row.a + [long]$row.b
    [ordered]@{
        id = [string]$entry.id
        label = [string]$entry.label
        a = [ordered]@{ label = [string]$entry.a; count = [long]$row.a; files = [long]$row.filesA; first = [string]$row.firstA }
        b = [ordered]@{ label = [string]$entry.b; count = [long]$row.b; files = [long]$row.filesB; first = [string]$row.firstB }
        shareA = $(if ($sum -gt 0) { 100.0 * [long]$row.a / $sum } else { 0.0 })
        projects = @(@($row.projects) | Where-Object { $null -ne $_ } | ForEach-Object { [ordered]@{ project = [string]$_.project; a = [long]$_.a; b = [long]$_.b } })
    }
})

$familyOrder = [System.Collections.Generic.List[string]]::new()
foreach ($entry in $featureTable) { if (-not $familyOrder.Contains($entry.family)) { $familyOrder.Add($entry.family) } }
$familyTable = @(foreach ($family in $familyOrder) {
    $members = @($featureTable | Where-Object { $_.family -eq $family })
    [ordered]@{
        family = $family
        features = [long]$members.Count
        used = [long]@($members | Where-Object { $_.count -gt 0 }).Count
        count = [long](($members | ForEach-Object { $_.count } | Measure-Object -Sum).Sum)
        perThousand = $(if ([long]$raw.lines -gt 0) { 1000.0 * [long](($members | ForEach-Object { $_.count } | Measure-Object -Sum).Sum) / [long]$raw.lines } else { 0.0 })
    }
})

$usedFeatures = @($featureTable | Where-Object { $_.count -gt 0 })
$minimumVersion = [System.Version]::new(1, 0)
$minimumFeatures = [System.Collections.Generic.List[string]]::new()
foreach ($entry in $usedFeatures) {
    $entryVersion = ConvertTo-LanguageVersion -Text $entry.version
    if ($entryVersion -gt $minimumVersion) { $minimumVersion = $entryVersion; $minimumFeatures.Clear() }
    if ($entryVersion -eq $minimumVersion) { $minimumFeatures.Add($entry.id) }
}
$minimumText = if ($minimumVersion.Minor -eq 0) { [string]$minimumVersion.Major } else { [string]$minimumVersion }

$kindTable = @(Sort-ByKey -Items @(@($raw.kinds) | Where-Object { $null -ne $_ } | ForEach-Object {
    [ordered]@{ kind = [string]$_.kind; count = [long]$_.count; files = [long]$_.files; projects = (ConvertTo-ProjectCounts @($_.projects)) }
}) -Key { (Get-DescendingKey $args[0].count) + $args[0].kind })

# The newest C# version among the given features, as the text the catalog gives; '-' when there are none.
function Get-NewestVersion {
    param([AllowEmptyCollection()][object[]]$Entries)

    $newest = '-'
    foreach ($entry in $Entries) {
        if ($newest -eq '-' -or (ConvertTo-LanguageVersion -Text $entry.version) -gt (ConvertTo-LanguageVersion -Text $newest)) { $newest = $entry.version }
    }
    return $newest
}

$projectTable = @(Sort-ByKey -Items @(@($raw.projects) | Where-Object { $null -ne $_ }) -Key { (Get-DescendingKey ([long]$args[0].lines)) + [string]$args[0].project } | ForEach-Object {
    $projectName = [string]$_.project
    $projectFeatures = @($featureTable | Where-Object { $entry = $_; @($entry.projects | Where-Object { $_.project -eq $projectName -and $_.count -gt 0 }).Count -gt 0 })
    [ordered]@{
        project = $projectName
        files = [long]$_.files
        lines = [long]$_.lines
        nodes = [long]$_.nodes
        featuresUsed = [long]$projectFeatures.Count
        newestVersion = (Get-NewestVersion $projectFeatures)
    }
})
$attributeTable = @(Sort-ByKey -Items @(@($raw.attributes) | Where-Object { $null -ne $_ } | ForEach-Object { [ordered]@{ name = [string]$_.name; count = [long]$_.count; files = [long]$_.files } }) -Key { (Get-DescendingKey $args[0].count) + $args[0].name })
$diagnosticTable = @(@($raw.diagnostics) | Where-Object { $null -ne $_ } | ForEach-Object {
    [ordered]@{ location = ("{0}:{1}:{2}" -f $_.path, $_.line, $_.column); id = [string]$_.id; message = [string]$_.message }
})

$versionTable = @(Sort-ByKey -Items @($featureTable | ForEach-Object { $_.version } | Select-Object -Unique) -Key { $parsed = ConvertTo-LanguageVersion -Text $args[0]; '{0:D5}.{1:D5}' -f $parsed.Major, $parsed.Minor } | ForEach-Object {
    $versionText = $_
    $members = @($featureTable | Where-Object { $_.version -eq $versionText })
    [ordered]@{
        version = $versionText
        features = [long]$members.Count
        used = [long]@($members | Where-Object { $_.count -gt 0 }).Count
        count = [long](($members | ForEach-Object { $_.count } | Measure-Object -Sum).Sum)
    }
})
$rankedTable = @(Sort-ByKey -Items $usedFeatures -Key { (Get-DescendingKey $args[0].count) + $args[0].id })
$unusedTable = @($featureTable | Where-Object { $_.count -eq 0 })
$totals = [ordered]@{
    families = [ordered]@{
        features = [long](($familyTable | ForEach-Object { $_.features } | Measure-Object -Sum).Sum)
        used = [long](($familyTable | ForEach-Object { $_.used } | Measure-Object -Sum).Sum)
        count = [long](($familyTable | ForEach-Object { $_.count } | Measure-Object -Sum).Sum)
        perThousand = $(if ([long]$raw.lines -gt 0) { 1000.0 * [long](($familyTable | ForEach-Object { $_.count } | Measure-Object -Sum).Sum) / [long]$raw.lines } else { 0.0 })
    }
    projects = [ordered]@{
        files = [long](($projectTable | ForEach-Object { $_.files } | Measure-Object -Sum).Sum)
        lines = [long](($projectTable | ForEach-Object { $_.lines } | Measure-Object -Sum).Sum)
        nodes = [long](($projectTable | ForEach-Object { $_.nodes } | Measure-Object -Sum).Sum)
        featuresUsed = [long]$usedFeatures.Count
        newestVersion = (Get-NewestVersion $usedFeatures)
    }
}

$stats = [ordered]@{
    project = [string]$config.project
    version = $version
    generation = $script:StatsGeneration
    generated = $generatedAt.ToString('yyyy-MM-dd HH:mm:ss zzz')
    top = $Top
    scope = [ordered]@{
        files = [long]$raw.files
        projects = [long]$projectTable.Count
        lines = [long]$raw.lines
        roots = @($rootEntries | ForEach-Object { $(if ($_.Prefix.Length -eq 0) { '.' } else { $_.Prefix.TrimEnd('/') }) })
        rootFiles = @($config.sources.files | ForEach-Object { [string]$_ })
        segments = $Segments
        excluded = @(Sort-ByKey -Items @($excludedNames) -Key { $args[0] })
        excludedSuffixes = @($excludedSuffixes)
        roslyn = [string]$raw.roslyn
        languageVersion = [string]$raw.languageVersion
        framework = $helperFramework
        sdk = [string]$helper.Sdk
        helperBuilt = [bool]$helper.Built
    }
    summary = [ordered]@{
        files = [long]$raw.files
        lines = [long]$raw.lines
        nodes = [long]$raw.nodes
        kindsUsed = [long]$kindTable.Count
        featuresUsed = [long]$usedFeatures.Count
        featuresTotal = [long]$featureTable.Count
        minimumVersion = $minimumText
        minimumFeatures = @($minimumFeatures)
        parseErrors = [long]$diagnosticTable.Count
    }
    projects = @($projectTable)
    families = @($familyTable)
    versions = @($versionTable)
    totals = $totals
    features = @($featureTable)
    ranked = @($rankedTable)
    unused = @($unusedTable)
    styles = @($styleTable)
    kinds = @($kindTable)
    attributes = @($attributeTable)
    diagnostics = @($diagnosticTable)
    reports = @(
        [ordered]@{ kind = 'markdown'; path = $reportPathFull },
        [ordered]@{ kind = 'page'; path = $pagePathFull }
    )
}

# Console output.
Write-StatsLine ("Scanned: {0} files, {1} projects, {2} lines" -f (Format-Integer $stats.scope.files), (Format-Integer $stats.scope.projects), (Format-Integer $stats.scope.lines)) -ForegroundColor DarkGray

Write-SectionTitle 'Summary'
$summaryRows = @(
    @('Files', (Format-Integer $stats.summary.files)),
    @('Lines', (Format-Integer $stats.summary.lines)),
    @('Nodes', (Format-Integer $stats.summary.nodes)),
    @('Syntax kinds used', (Format-Integer $stats.summary.kindsUsed)),
    @('Features used', ("{0} of {1}" -f (Format-Integer $stats.summary.featuresUsed), (Format-Integer $stats.summary.featuresTotal))),
    @('Minimum C# version', $stats.summary.minimumVersion),
    @('Parse errors', (Format-Integer $stats.summary.parseErrors))
)
Write-ConsoleTable -Columns @(
    (New-ConsoleColumn -Name 'Measure' -Right $false -Values @($summaryRows | ForEach-Object { $_[0] })),
    (New-ConsoleColumn -Name 'Value' -Values @($summaryRows | ForEach-Object { $_[1] }))
)

function Select-Head {
    param([AllowEmptyCollection()][object[]]$Rows)

    return , @($Rows | Select-Object -First $Top)
}

Write-SectionTitle 'Families'
Write-ConsoleTable -Columns @(
    (New-ConsoleColumn -Name 'Family' -Right $false -Values @(@($stats.families | ForEach-Object { $_.family }) + 'Total')),
    (New-ConsoleColumn -Name 'Features' -Values @(@($stats.families | ForEach-Object { Format-Integer $_.features }) + (Format-Integer $stats.totals.families.features))),
    (New-ConsoleColumn -Name 'Used' -Values @(@($stats.families | ForEach-Object { Format-Integer $_.used }) + (Format-Integer $stats.totals.families.used))),
    (New-ConsoleColumn -Name 'Count' -Values @(@($stats.families | ForEach-Object { Format-Integer $_.count }) + (Format-Integer $stats.totals.families.count))),
    (New-ConsoleColumn -Name 'Per 1k lines' -Values @(@($stats.families | ForEach-Object { Format-Rate $_.perThousand }) + (Format-Rate $stats.totals.families.perThousand)))
)

Write-SectionTitle 'Projects'
Write-ConsoleTable -Columns @(
    (New-ConsoleColumn -Name 'Project' -Right $false -Values @(@($stats.projects | ForEach-Object { $_.project }) + 'Total')),
    (New-ConsoleColumn -Name 'Files' -Values @(@($stats.projects | ForEach-Object { Format-Integer $_.files }) + (Format-Integer $stats.totals.projects.files))),
    (New-ConsoleColumn -Name 'Lines' -Values @(@($stats.projects | ForEach-Object { Format-Integer $_.lines }) + (Format-Integer $stats.totals.projects.lines))),
    (New-ConsoleColumn -Name 'Nodes' -Values @(@($stats.projects | ForEach-Object { Format-Integer $_.nodes }) + (Format-Integer $stats.totals.projects.nodes))),
    (New-ConsoleColumn -Name 'Features used' -Values @(@($stats.projects | ForEach-Object { Format-Integer $_.featuresUsed }) + (Format-Integer $stats.summary.featuresUsed))),
    (New-ConsoleColumn -Name 'Newest C#' -Values @(@($stats.projects | ForEach-Object { $_.newestVersion }) + $stats.totals.projects.newestVersion))
)

Write-SectionTitle 'Versions'
Write-ConsoleTable -Columns @(
    (New-ConsoleColumn -Name 'C# version' -Right $false -Values @($stats.versions | ForEach-Object { $_.version })),
    (New-ConsoleColumn -Name 'Features' -Values @($stats.versions | ForEach-Object { Format-Integer $_.features })),
    (New-ConsoleColumn -Name 'Used' -Values @($stats.versions | ForEach-Object { Format-Integer $_.used })),
    (New-ConsoleColumn -Name 'Count' -Values @($stats.versions | ForEach-Object { Format-Integer $_.count }))
)

if ($stats.ranked.Count -gt 0) {
    $shown = Select-Head $stats.ranked
    Write-SectionTitle 'Features'
    Write-ConsoleTable -Columns @(
        (New-ConsoleColumn -Name 'Feature' -Right $false -Values @($shown | ForEach-Object { $_.label })),
        (New-ConsoleColumn -Name 'Family' -Right $false -Values @($shown | ForEach-Object { $_.family })),
        (New-ConsoleColumn -Name 'C#' -Right $false -Values @($shown | ForEach-Object { $_.version })),
        (New-ConsoleColumn -Name 'Count' -Values @($shown | ForEach-Object { Format-Integer $_.count })),
        (New-ConsoleColumn -Name 'Per 1k lines' -Values @($shown | ForEach-Object { Format-Rate $_.perThousand })),
        (New-ConsoleColumn -Name 'Files' -Values @($shown | ForEach-Object { Format-Integer $_.files })),
        (New-ConsoleColumn -Name 'First' -Right $false -Values @($shown | ForEach-Object { $_.first }))
    )
    Write-CutLine -Total $stats.ranked.Count -Shown $shown.Count
}

Write-SectionTitle 'Styles'
Write-ConsoleTable -Columns @(
    (New-ConsoleColumn -Name 'Style' -Right $false -Values @($stats.styles | ForEach-Object { $_.label })),
    (New-ConsoleColumn -Name 'A' -Right $false -Values @($stats.styles | ForEach-Object { $_.a.label })),
    (New-ConsoleColumn -Name 'A count' -Values @($stats.styles | ForEach-Object { Format-Integer $_.a.count })),
    (New-ConsoleColumn -Name 'B' -Right $false -Values @($stats.styles | ForEach-Object { $_.b.label })),
    (New-ConsoleColumn -Name 'B count' -Values @($stats.styles | ForEach-Object { Format-Integer $_.b.count })),
    (New-ConsoleColumn -Name 'A share' -Values @($stats.styles | ForEach-Object { Format-Percent $_.shareA }))
)

if ($stats.unused.Count -gt 0) {
    $shown = Select-Head $stats.unused
    Write-SectionTitle ("Features unused ({0})" -f (Format-Integer $stats.unused.Count))
    Write-ConsoleTable -Columns @(
        (New-ConsoleColumn -Name 'Feature' -Right $false -Values @($shown | ForEach-Object { $_.label })),
        (New-ConsoleColumn -Name 'Family' -Right $false -Values @($shown | ForEach-Object { $_.family })),
        (New-ConsoleColumn -Name 'C#' -Right $false -Values @($shown | ForEach-Object { $_.version }))
    )
    Write-CutLine -Total $stats.unused.Count -Shown $shown.Count
}

if ($stats.kinds.Count -gt 0) {
    $shown = Select-Head $stats.kinds
    Write-SectionTitle 'Syntax kinds'
    Write-ConsoleTable -Columns @(
        (New-ConsoleColumn -Name 'Kind' -Right $false -Values @($shown | ForEach-Object { $_.kind })),
        (New-ConsoleColumn -Name 'Count' -Values @($shown | ForEach-Object { Format-Integer $_.count })),
        (New-ConsoleColumn -Name 'Files' -Values @($shown | ForEach-Object { Format-Integer $_.files }))
    )
    Write-CutLine -Total $stats.kinds.Count -Shown $shown.Count
}

if ($stats.attributes.Count -gt 0) {
    $shown = Select-Head $stats.attributes
    Write-SectionTitle 'Attributes'
    Write-ConsoleTable -Columns @(
        (New-ConsoleColumn -Name 'Attribute' -Right $false -Values @($shown | ForEach-Object { $_.name })),
        (New-ConsoleColumn -Name 'Count' -Values @($shown | ForEach-Object { Format-Integer $_.count })),
        (New-ConsoleColumn -Name 'Files' -Values @($shown | ForEach-Object { Format-Integer $_.files }))
    )
    Write-CutLine -Total $stats.attributes.Count -Shown $shown.Count
}

if ($stats.diagnostics.Count -gt 0) {
    Write-SectionTitle ("Parse errors ({0})" -f (Format-Integer $stats.diagnostics.Count))
    Write-ConsoleTable -Columns @(
        (New-ConsoleColumn -Name 'Location' -Right $false -Values @($stats.diagnostics | ForEach-Object { $_.location })),
        (New-ConsoleColumn -Name 'Id' -Right $false -Values @($stats.diagnostics | ForEach-Object { $_.id })),
        (New-ConsoleColumn -Name 'Message' -Right $false -Values @($stats.diagnostics | ForEach-Object { $_.message }))
    )
}

# Markdown output.
$md = [System.Collections.Generic.List[string]]::new()

function Add-MarkdownTable {
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][string[]]$Headers,
        [Parameter(Mandatory = $true)][bool[]]$Right,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Rows,
        [Parameter(Mandatory = $true)][scriptblock]$Cells
    )

    $md.Add('')
    $md.Add("## $Title")
    $md.Add('')
    if ($Rows.Count -eq 0) {
        $md.Add('None.')
        return
    }
    $md.Add('| ' + ($Headers -join ' | ') + ' |')
    $rules = for ($i = 0; $i -lt $Headers.Count; $i++) { if ($Right[$i]) { '---:' } else { '---' } }
    $md.Add('|' + ($rules -join '|') + '|')
    foreach ($row in $Rows) {
        $md.Add('| ' + ((@(& $Cells $row) | ForEach-Object { ConvertTo-MarkdownCell $_ }) -join ' | ') + ' |')
    }
}

$md.Add("# Syntax statistics - $version")
$md.Add('')
$md.Add("- Generated: $($stats.generated)")
$md.Add("- Source roots: $(ConvertTo-MarkdownCell ($stats.scope.roots -join ', '))")
$md.Add("- Root-level files: $(if ($stats.scope.rootFiles.Count -gt 0) { ConvertTo-MarkdownCell ($stats.scope.rootFiles -join ', ') } else { 'none' })")
$md.Add("- Project segments: $($stats.scope.segments)")
$md.Add("- Excluded directories: $(ConvertTo-MarkdownCell ($stats.scope.excluded -join ', '))")
$md.Add("- Excluded suffixes: $(ConvertTo-MarkdownCell ($stats.scope.excludedSuffixes -join ', '))")
$md.Add("- Roslyn: $(ConvertTo-MarkdownCell $stats.scope.roslyn)")
$md.Add("- Language version: $(ConvertTo-MarkdownCell $stats.scope.languageVersion)")
$md.Add("- Scanned: $(Format-Integer $stats.scope.files) files, $(Format-Integer $stats.scope.projects) projects, $(Format-Integer $stats.scope.lines) lines")

Add-MarkdownTable -Title 'Summary' -Headers @('Measure', 'Value') -Right @($false, $true) -Rows $summaryRows -Cells {
    param($r) $r[0], $r[1]
}
Add-MarkdownTable -Title 'Families' -Headers @('Family', 'Features', 'Used', 'Count', 'Per 1k lines') -Right @($false, $true, $true, $true, $true) -Rows (@($stats.families) + [ordered]@{ family = 'Total'; features = $stats.totals.families.features; used = $stats.totals.families.used; count = $stats.totals.families.count; perThousand = $stats.totals.families.perThousand }) -Cells {
    param($r) $r.family, (Format-Integer $r.features), (Format-Integer $r.used), (Format-Integer $r.count), (Format-Rate $r.perThousand)
}
Add-MarkdownTable -Title 'Projects' -Headers @('Project', 'Files', 'Lines', 'Nodes', 'Features used', 'Newest C#') -Right @($false, $true, $true, $true, $true, $true) -Rows (@($stats.projects) + [ordered]@{ project = 'Total'; files = $stats.totals.projects.files; lines = $stats.totals.projects.lines; nodes = $stats.totals.projects.nodes; featuresUsed = $stats.summary.featuresUsed; newestVersion = $stats.totals.projects.newestVersion }) -Cells {
    param($r) $r.project, (Format-Integer $r.files), (Format-Integer $r.lines), (Format-Integer $r.nodes), (Format-Integer $r.featuresUsed), $r.newestVersion
}
Add-MarkdownTable -Title 'Versions' -Headers @('C# version', 'Features', 'Used', 'Count') -Right @($false, $true, $true, $true) -Rows $stats.versions -Cells {
    param($r) $r.version, (Format-Integer $r.features), (Format-Integer $r.used), (Format-Integer $r.count)
}
Add-MarkdownTable -Title 'Features' -Headers @('Feature', 'Family', 'C#', 'Count', 'Per 1k lines', 'Files', 'First') -Right @($false, $false, $false, $true, $true, $true, $false) -Rows $stats.ranked -Cells {
    param($r) $r.label, $r.family, $r.version, (Format-Integer $r.count), (Format-Rate $r.perThousand), (Format-Integer $r.files), $r.first
}
Add-MarkdownTable -Title 'Styles' -Headers @('Style', 'A', 'A count', 'B', 'B count', 'A share') -Right @($false, $false, $true, $false, $true, $true) -Rows $stats.styles -Cells {
    param($r) $r.label, $r.a.label, (Format-Integer $r.a.count), $r.b.label, (Format-Integer $r.b.count), (Format-Percent $r.shareA)
}
Add-MarkdownTable -Title ("Features unused ({0})" -f (Format-Integer $stats.unused.Count)) -Headers @('Feature', 'Family', 'C#') -Right @($false, $false, $false) -Rows $stats.unused -Cells {
    param($r) $r.label, $r.family, $r.version
}
Add-MarkdownTable -Title 'Syntax kinds' -Headers @('Kind', 'Count', 'Files') -Right @($false, $true, $true) -Rows $stats.kinds -Cells {
    param($r) $r.kind, (Format-Integer $r.count), (Format-Integer $r.files)
}
Add-MarkdownTable -Title 'Attributes' -Headers @('Attribute', 'Count', 'Files') -Right @($false, $true, $true) -Rows $stats.attributes -Cells {
    param($r) $r.name, (Format-Integer $r.count), (Format-Integer $r.files)
}
Add-MarkdownTable -Title ("Parse errors ({0})" -f (Format-Integer $stats.diagnostics.Count)) -Headers @('Location', 'Id', 'Message') -Right @($false, $false, $false) -Rows $stats.diagnostics -Cells {
    param($r) $r.location, $r.id, $r.message
}

$projectNames = @($stats.projects | ForEach-Object { $_.project })
$projectRight = @($false) + @($projectNames | ForEach-Object { $true })
Add-MarkdownTable -Title 'Feature counts per project' -Headers (@('Feature') + $projectNames) -Right $projectRight -Rows $stats.ranked -Cells {
    param($r)
    $r.label
    foreach ($name in $projectNames) {
        $hit = @($r.projects | Where-Object { $_.project -eq $name })
        if ($hit.Count -gt 0 -and $hit[0].count -gt 0) { Format-Integer $hit[0].count } else { '' }
    }
}
Add-MarkdownTable -Title 'Style A share per project' -Headers (@('Style') + $projectNames) -Right $projectRight -Rows $stats.styles -Cells {
    param($r)
    $r.label
    foreach ($name in $projectNames) {
        $hit = @($r.projects | Where-Object { $_.project -eq $name })
        if ($hit.Count -gt 0 -and ($hit[0].a + $hit[0].b) -gt 0) { Format-Percent (100.0 * $hit[0].a / ($hit[0].a + $hit[0].b)) } else { '-' }
    }
}

[System.IO.Directory]::CreateDirectory($reportDirectoryFull) | Out-Null
[System.IO.File]::WriteAllText($reportPathFull, (($md -join "`n") + "`n"), [System.Text.UTF8Encoding]::new($false))

# Page output: the full result as data inside the template StatsSyntax.html.
# The JSON is written by hand, so Windows PowerShell 5.1 and pwsh 7 produce the same bytes.
# Dictionaries are read through get_Keys(): "Keys" can be a stored word.
function ConvertTo-PageJson {
    param([AllowNull()][object]$Value)

    if ($null -eq $Value) { return 'null' }
    if ($Value -is [bool]) { return $(if ($Value) { 'true' } else { 'false' }) }
    if ($Value -is [string]) {
        if ($Value -notmatch '[^ -~]|["<>&]|\\') { return '"' + $Value + '"' }
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
    if ($Value -is [double] -or $Value -is [single] -or $Value -is [decimal]) {
        $number = [double]$Value
        if ([double]::IsNaN($number) -or [double]::IsInfinity($number)) { return 'null' }
        # Ten fixed decimals: 'R' prints differently on .NET Framework and .NET.
        return $number.ToString('0.##########', [System.Globalization.CultureInfo]::InvariantCulture)
    }
    if ($Value -is [System.Collections.IDictionary]) {
        $pairs = @(foreach ($key in $Value.get_Keys()) { (ConvertTo-PageJson ([string]$key)) + ':' + (ConvertTo-PageJson $Value[$key]) })
        return '{' + ($pairs -join ',') + '}'
    }
    if ($Value -is [System.Collections.IEnumerable]) {
        $items = @(foreach ($item in $Value) { ConvertTo-PageJson $item })
        return '[' + ($items -join ",`n") + ']'
    }
    throw "The page data holds a value of an unexpected type: $($Value.GetType().FullName)"
}

$pageTemplatePath = Join-Path $PSScriptRoot 'StatsSyntax.html'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$pageTemplate = [System.IO.File]::ReadAllText($pageTemplatePath, $utf8)
foreach ($marker in @('/*__DATA__*/', '__TITLE__')) {
    if (-not $pageTemplate.Contains($marker)) { throw "The page template lacks the marker $marker : $pageTemplatePath" }
}
$pageText = $pageTemplate.Replace('__TITLE__', [System.Net.WebUtility]::HtmlEncode([string]$config.project)).Replace('/*__DATA__*/', (ConvertTo-PageJson $stats))
[System.IO.File]::WriteAllText($pagePathFull, ($pageText -replace "`r`n", "`n"), $utf8)

Write-StatsLine ""
foreach ($report in $stats.reports) { Write-StatsLine "Report: $($report.path)" }

if (-not $NoOpen) {
    Start-Process -FilePath $pagePathFull
}

exit 0
