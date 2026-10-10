<#
.SYNOPSIS
Reports the lexical frequency of the codebase, counted separately for code and for comments.

.DESCRIPTION
Reads the project configuration from StatsWords.json next to this script, then
performs these actions on every run:
  1. Prints the Summary table: files, tokens, vocabulary, type/token ratio and hapax
     for all code, for owned code and for comments.
  2. Prints code and comment tokens for each project under the source roots.
  3. Prints the most frequent code words and comment words.
  4. Prints the verbs and bases of declared types and methods, and the registered ones unused.
  5. Prints comment words that never occur in code, and owned types no comment cites.
  6. Prints the words used exactly once, in code and in comments.
  7. Writes a Markdown report to {report.directory}\{prefix}{version}.md with every list in full.
  8. Writes an HTML page to {report.directory}\{prefix}{version}.html from the template
     StatsWords.html next to this script, and opens it unless -NoOpen is given. The page holds
     every list in full as tables sortable by column and filterable by text, and a log-log
     rank-frequency chart of code, owned code and comments.
A Stats script only reports: it has no gates, no ceilings and no pass or fail. It exits 0
unless the run itself breaks.

The code corpus is every .cs file, with string literals, char literals and comments blanked,
plus the x:Name and Name attribute values of every .xaml file. Identifiers split into words on
PascalCase and acronym boundaries. An identifier is owned when, after one leading underscore,
it starts with a configured prefix followed by an uppercase letter and the first word after
the prefix is a registered base. Only owned identifiers lose their prefix.

The comment corpus is the prose of every comment file. The hash line, fenced code blocks and
backtick spans are left out; identifiers inside backtick spans form the cited set.
Code words merge across case, and a merged word shows its most frequent spelling.

Everything project-specific lives in StatsWords.json. Files come from git: tracked and
untracked files, never ignored ones. Git is the only external tool required. A configured
root or root-level file that does not exist stops the run with an error.

StatsWords.json shape:
  {
    "generation": 1,
    "project": "Cadroue",
    "sources": {
      "roots": ["src", "tests"],
      "files": [],
      "excludeSegments": [".git", "bin", "obj"],
      "excludeSuffixes": [".g.cs", ".Designer.cs"]
    },
    "code": { "extensions": [".cs", ".xaml"] },
    "comments": { "pattern": "*.comment.md" },
    "prefixes": ["PS", "QS", "CS", "LS", "P", "Q", "C", "L", "T"],
    "keywords": ["abstract", "as", "base"],
    "stopwords": ["a", "about", "above"],
    "top": 40,
    "registry": "AuditNames.registry.json",
    "report": {
      "directory": "docs-analysis",
      "versionFile": "version.json",
      "versionKey": "current-version",
      "prefix": "StatsWords-",
      "segments": 1
    }
  }

Files lists root-level sources whose comment files join the comment corpus under the
(root) project. It may be empty. The registry path is relative to this script.

.PARAMETER Top
Overrides top: how many rows each ranking and list shows on the console.

.PARAMETER NoOpen
Write the page without opening it.

.PARAMETER NoPause
Do not stop at each console page. Paging is also off when output or input is redirected.

.PARAMETER Help
Display this help and exit without running. The alias -? is supported.

.EXAMPLE
StatsWords

.EXAMPLE
StatsWords -Top 100 -NoPause
#>
#requires -Version 5.1
# STATSWORDS - STATS GENERATION 1.
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

$ConfigPath = Join-Path $PSScriptRoot "StatsWords.json"

if ($Help) {
    @'
NAME
    StatsWords.ps1

SYNOPSIS
    Report word frequency for code and for comments, and write a Markdown report
    and an HTML page.

SYNTAX
    StatsWords [-Top <number>] [-NoOpen] [-NoPause] [-Help]

CONFIGURATION
    All project-specific values live in StatsWords.json next to the script:
    source roots and root-level files, excluded directory names and suffixes,
    code extensions, comment-file pattern, owned prefixes, C# keywords,
    English stopwords, the console row count, the name registry, report
    directory, version file and key, and the report file-name prefix.

STATISTICS
    Code: identifiers of .cs files, literals and comments blanked, plus the
        x:Name and Name values of .xaml files, split into PascalCase words.
        Keywords are dropped. Owned identifiers lose their prefix.
    Comments: prose words of every comment file, lowercased, stopwords
        dropped. Identifiers in backtick spans form the cited set.
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
    StatsWords
        Report with the configured settings.

    StatsWords -Top 100
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

Write-StatsLine "STATSWORDS - STATS GENERATION $script:StatsGeneration" -ForegroundColor Blue

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
        default { throw "Unknown schema kind: $Kind" }
    }
}

function Read-StatsConfig {
    param([Parameter(Mandatory = $true)][string]$Path)

    $pathFull = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $pathFull -PathType Leaf)) {
        throw "The word-statistics configuration was not found: $pathFull"
    }

    try {
        $config = Get-Content -LiteralPath $pathFull -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The word-statistics configuration is not valid JSON: $pathFull`n$($_.Exception.Message)"
    }

    $problems = [System.Collections.Generic.List[string]]::new()
    $schema = [ordered]@{
        'generation' = 'int'; 'project' = 'string'
        'sources.roots' = 'strings'; 'sources.files' = 'strings'
        'sources.excludeSegments' = 'strings'; 'sources.excludeSuffixes' = 'strings'
        'code.extensions' = 'strings'; 'comments.pattern' = 'string'
        'prefixes' = 'strings'; 'keywords' = 'strings'; 'stopwords' = 'strings'
        'top' = 'positive'; 'registry' = 'string'
        'report.directory' = 'string'; 'report.versionFile' = 'string'; 'report.versionKey' = 'string'; 'report.prefix' = 'string'; 'report.segments' = 'positive'
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
        foreach ($extension in @($config.code.extensions)) {
            if ($extension -notin @('.cs', '.xaml')) { $problems.Add("key 'code.extensions' holds '$extension', but only .cs and .xaml can be read") }
        }
        if (-not ([string]$config.comments.pattern).StartsWith('*.')) { $problems.Add("key 'comments.pattern' must start with '*.'") }
    }

    if ($problems.Count -gt 0) {
        throw "The word-statistics configuration is not valid: $pathFull`n  " + ($problems -join "`n  ")
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
    throw "The word-statistics configuration is generation $($config.generation) but this tooling is generation $script:StatsGeneration : $ConfigPath"
}

if (-not $PSBoundParameters.ContainsKey('Top')) { $Top = [int]$config.top }
$Segments = [int]$config.report.segments
$commentPattern = [string]$config.comments.pattern
$commentSuffix = $commentPattern.TrimStart('*')
$codeExtensions = @($config.code.extensions | ForEach-Object { ([string]$_).ToLowerInvariant() })
$prefixLongest = (@($config.prefixes | ForEach-Object { ([string]$_).Length }) | Measure-Object -Maximum).Maximum
$prefixes = @(for ($length = $prefixLongest; $length -ge 1; $length--) { @($config.prefixes | ForEach-Object { [string]$_ } | Where-Object { $_.Length -eq $length }) })
$keywords = [System.Collections.Generic.HashSet[string]]::new([string[]]@($config.keywords | ForEach-Object { [string]$_ }), [System.StringComparer]::Ordinal)
$stopwords = [hashtable]::new([System.StringComparer]::Ordinal)
foreach ($word in @($config.stopwords)) { $stopwords[([string]$word).ToLowerInvariant()] = $true }
$reportDirectoryFull = Join-StatsPath -Root $repoRootFull -Relative $config.report.directory
$versionPathFull = Join-StatsPath -Root $repoRootFull -Relative $config.report.versionFile
$versionKey = [string]$config.report.versionKey
$reportPrefix = [string]$config.report.prefix

$excludedNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($segment in @($config.sources.excludeSegments)) { [void]$excludedNames.Add([string]$segment) }
$excludedSuffixes = @($config.sources.excludeSuffixes | ForEach-Object { [string]$_ })

# Name registry.
$registryPathFull = Join-StatsPath -Root $PSScriptRoot -Relative $config.registry
if (-not [System.IO.File]::Exists($registryPathFull)) { throw "The name registry was not found: $registryPathFull" }
try {
    $registry = Get-Content -LiteralPath $registryPathFull -Raw -Encoding UTF8 | ConvertFrom-Json
}
catch {
    throw "The name registry is not valid JSON: $registryPathFull`n$($_.Exception.Message)"
}
foreach ($key in @('bases', 'verbs')) {
    if (-not (Test-ConfigValue -Value (Get-ConfigNode -Document $registry -Key $key) -Kind 'strings')) {
        throw "The name registry lacks a valid '$key' list: $registryPathFull"
    }
}
$registeredBases = [System.Collections.Generic.HashSet[string]]::new([string[]]@($registry.bases | ForEach-Object { [string]$_ }), [System.StringComparer]::Ordinal)
$registeredVerbs = [System.Collections.Generic.HashSet[string]]::new([string[]]@($registry.verbs | ForEach-Object { [string]$_ }), [System.StringComparer]::Ordinal)

function ConvertTo-MarkdownCell {
    param([AllowNull()][object]$Value)

    return ([string]$Value).Replace('|', '\|').Replace("`r`n", '<br>').Replace("`n", '<br>').Replace("`r", '<br>')
}

function Format-Integer {
    param([long]$Value)

    return $Value.ToString("N0", [System.Globalization.CultureInfo]::InvariantCulture)
}

function Format-Ratio {
    param([double]$Value)

    return $Value.ToString("0.00", [System.Globalization.CultureInfo]::InvariantCulture)
}

function Format-Percent {
    param([double]$Value)

    return $Value.ToString("0.00", [System.Globalization.CultureInfo]::InvariantCulture) + " %"
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

# File listing.
$rootEntries = [System.Collections.Generic.List[object]]::new()
foreach ($root in @($config.sources.roots)) {
    $rootFull = Join-StatsPath -Root $repoRootFull -Relative $root
    if (-not [System.IO.Directory]::Exists($rootFull)) { throw "The configured source root has no directory: $rootFull" }
    $rootRelative = (Get-RelativePathSafe -BasePath $repoRootFull -Path $rootFull).Trim('/')
    $rootEntries.Add([pscustomobject]@{ Full = $rootFull; Prefix = $(if ($rootRelative.Length -eq 0) { '' } else { $rootRelative + '/' }) })
}

$rootComments = [System.Collections.Generic.List[string]]::new()
foreach ($entry in @($config.sources.files)) {
    $fileFull = Join-StatsPath -Root $repoRootFull -Relative $entry
    if (-not [System.IO.File]::Exists($fileFull)) { throw "The configured source file does not exist: $fileFull" }
    $commentFull = Join-Path ([System.IO.Path]::GetDirectoryName($fileFull)) ([System.IO.Path]::GetFileNameWithoutExtension($fileFull) + $commentSuffix)
    if ([System.IO.File]::Exists($commentFull)) { $rootComments.Add($commentFull) }
}

$patterns = @($commentSuffix) + @($codeExtensions)
$listArguments = @('-C', $repoRootFull, '-c', 'core.quotePath=false', 'ls-files', '--cached', '--others', '--exclude-standard', '--') +
    @($patterns | ForEach-Object { ':(icase)*' + $_ })
$listing = Invoke-StatsGit -Arguments $listArguments
if ($listing.ExitCode -ne 0) {
    throw "Git could not enumerate the files under $repoRootFull, so the statistics cannot be counted."
}

$codeFiles = [System.Collections.Generic.List[object]]::new()
$commentFiles = [System.Collections.Generic.List[object]]::new()
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
    if (-not [System.IO.File]::Exists($full) -or -not $seen.Add($full)) { continue }
    $name = [System.IO.Path]::GetFileName($full)
    $project = Get-FolderKey -RootFull $owner.Full -UnderRoot $relative.Substring($owner.Prefix.Length)
    $item = [pscustomobject]@{ Full = $full; Relative = $relative; Project = $project; Extension = [System.IO.Path]::GetExtension($name).ToLowerInvariant() }
    if ($name.EndsWith($commentSuffix, [System.StringComparison]::OrdinalIgnoreCase)) {
        $commentFiles.Add($item)
    }
    elseif ($item.Extension -in $codeExtensions -and -not (Test-IsExcludedSuffix -Name $name)) {
        $codeFiles.Add($item)
    }
}
foreach ($commentFull in $rootComments) {
    if (-not $seen.Add($commentFull)) { continue }
    $commentFiles.Add([pscustomobject]@{ Full = $commentFull; Relative = (Get-RelativePathSafe -BasePath $repoRootFull -Path $commentFull); Project = '(root)'; Extension = '.md' })
}
$codeFiles = [System.Collections.Generic.List[object]]@(Sort-ByKey -Items @($codeFiles) -Key { $args[0].Relative.ToLowerInvariant() })
$commentFiles = [System.Collections.Generic.List[object]]@(Sort-ByKey -Items @($commentFiles) -Key { $args[0].Relative.ToLowerInvariant() })

if ($codeFiles.Count -eq 0) { throw "No code file was found under the configured roots, so the statistics cannot be counted." }
if ($commentFiles.Count -eq 0) { throw "No comment file was found under the configured roots, so the statistics cannot be counted." }

# Code corpus.
$compiled = [System.Text.RegularExpressions.RegexOptions]::Compiled
$blankPattern = [System.Text.RegularExpressions.Regex]::new('//[^\n]*|/\*[\s\S]*?\*/|"{3,}[\s\S]*?"{3,}|(?:@\$*|\$+@)"(?:[^"]|"")*"|"(?:\\.|[^"\\\n])*"|''(?:\\[^''\n]+|[^''\\\n])''', $compiled)
$identifierPattern = [System.Text.RegularExpressions.Regex]::new('(?<![0-9A-Za-z_])[A-Za-z_][A-Za-z0-9_]*', $compiled)
$wordPattern = [System.Text.RegularExpressions.Regex]::new('[A-Z]+[0-9]*(?=[A-Z][a-z])|[A-Z]?[a-z]+[0-9]*|[A-Z]+[0-9]*|[0-9]+', $compiled)
$xamlNamePattern = [System.Text.RegularExpressions.Regex]::new('(?<![\w.:])(?:x:)?Name\s*=\s*"(?<name>[A-Za-z_][A-Za-z0-9_]*)"', $compiled)
$typePattern = [System.Text.RegularExpressions.Regex]::new('\b(?:class|struct|interface|enum|record(?:\s+(?:class|struct))?)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)', $compiled)
$methodModifiers = 'public|private|protected|internal|static|virtual|override|abstract|sealed|async|extern|unsafe|new|partial|readonly|required|file'
$methodPattern = [System.Text.RegularExpressions.Regex]::new('(?m)^[ \t]*(?:\[[^\]\n]*\][ \t]*)*(?:(?:' + $methodModifiers + ')[ \t]+)*(?<type>[A-Za-z_][\w.]*(?:<[^()\n]*?>)?(?:\[\])*\??)[ \t]+(?<name>[A-Za-z_][A-Za-z0-9_]*)[ \t]*(?:<[^()\n]*?>)?[ \t]*\(', $compiled)
$notReturnTypes = [System.Collections.Generic.HashSet[string]]::new([string[]]@(($methodModifiers -split '\|') + @('return', 'await', 'throw', 'else', 'yield', 'using', 'case', 'goto', 'in', 'is', 'as', 'out', 'ref', 'when', 'and', 'or', 'not', 'var', 'from', 'select', 'where', 'let', 'nameof', 'typeof', 'sizeof', 'default', 'lock', 'fixed', 'delegate', 'event', 'operator', 'implicit', 'explicit', 'const', 'namespace', 'if', 'while', 'for', 'foreach', 'switch', 'catch')), [System.StringComparer]::Ordinal)

function Split-Word {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text)

    $found = $wordPattern.Matches($Text)
    $words = [string[]]::new($found.Count)
    for ($i = 0; $i -lt $found.Count; $i++) { $words[$i] = $found[$i].Value }
    return , $words
}

# Splits one identifier, owned or foreign, with its prefix and base when owned.
function Get-IdentifierShape {
    param([Parameter(Mandatory = $true)][string]$Identifier)

    $core = if ($Identifier.StartsWith('_')) { $Identifier.Substring(1) } else { $Identifier }
    foreach ($prefix in $prefixes) {
        if ($core.Length -gt $prefix.Length -and $core.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase) -and [char]::IsUpper($core[$prefix.Length])) {
            $rest = Split-Word -Text $core.Substring($prefix.Length)
            if ($rest.Count -gt 0 -and $registeredBases.Contains($rest[0])) {
                return [pscustomobject]@{ Owned = $true; Prefixed = $true; Base = $rest[0]; Words = $rest }
            }
        }
    }
    foreach ($prefix in $prefixes) {
        if ($core.Length -gt $prefix.Length -and $core.StartsWith($prefix, [System.StringComparison]::Ordinal) -and [char]::IsUpper($core[$prefix.Length])) {
            $rest = Split-Word -Text $core.Substring($prefix.Length)
            if ($rest.Count -gt 0) {
                return [pscustomobject]@{ Owned = $false; Prefixed = $true; Base = $rest[0]; Words = (Split-Word -Text $Identifier); Rest = $rest }
            }
        }
    }

    return [pscustomobject]@{ Owned = $false; Prefixed = $false; Base = ''; Words = (Split-Word -Text $Identifier) }
}

$identifiers = [hashtable]::new([System.StringComparer]::Ordinal)
$projectRows = @{}

function Get-ProjectRow {
    param([Parameter(Mandatory = $true)][string]$Project)

    if (-not $projectRows.ContainsKey($Project)) {
        $projectRows[$Project] = [pscustomobject]@{ Project = $Project; CodeFiles = 0; CodeTokens = [long]0; CommentFiles = 0; CommentTokens = [long]0 }
    }
    return $projectRows[$Project]
}

function Get-IdentifierInfo {
    param([Parameter(Mandatory = $true)][string]$Identifier)

    $info = $identifiers[$Identifier]
    if ($null -ne $info) { return $info }
    if ($keywords.Contains($Identifier) -or $Identifier.Trim('_').Length -eq 0) {
        $info = [pscustomobject]@{ Words = [string[]]@(); Owned = $false; Count = [long]0; File = -1; Index = -1 }
    }
    else {
        $shape = Get-IdentifierShape -Identifier $Identifier
        $info = [pscustomobject]@{ Words = [string[]]$shape.Words; Owned = $shape.Owned; Count = [long]0; File = -1; Index = -1 }
    }
    $identifiers[$Identifier] = $info
    return $info
}

$declaredTypes = [System.Collections.Generic.List[object]]::new()
$declaredMethods = [System.Collections.Generic.List[object]]::new()
$readErrors = [System.Collections.Generic.List[string]]::new()
[int]$ownedFiles = 0

for ($fileIndex = 0; $fileIndex -lt $codeFiles.Count; $fileIndex++) {
    $file = $codeFiles[$fileIndex]
    try { $text = [System.IO.File]::ReadAllText($file.Full) }
    catch {
        $readErrors.Add("$($file.Relative): $($_.Exception.Message)")
        continue
    }

    $local = [hashtable]::new([System.StringComparer]::Ordinal)
    $firsts = [hashtable]::new([System.StringComparer]::Ordinal)
    if ($file.Extension -eq '.cs') {
        $code = $blankPattern.Replace($text, [System.Text.RegularExpressions.MatchEvaluator]{
            param($match)
            return [System.Text.RegularExpressions.Regex]::Replace($match.Value, '[^\n]', ' ')
        })
        foreach ($match in $identifierPattern.Matches($code)) {
            $value = $match.Value
            $count = $local[$value]
            if ($null -eq $count) { $local[$value] = 1; $firsts[$value] = $match.Index }
            else { $local[$value] = $count + 1 }
        }
        foreach ($match in $typePattern.Matches($code)) {
            $name = $match.Groups['name'].Value
            if (-not $keywords.Contains($name)) { $declaredTypes.Add([pscustomobject]@{ Name = $name; File = $fileIndex; Index = $match.Groups['name'].Index }) }
        }
        foreach ($match in $methodPattern.Matches($code)) {
            $name = $match.Groups['name'].Value
            if (-not $notReturnTypes.Contains($match.Groups['type'].Value) -and -not $keywords.Contains($name)) {
                $declaredMethods.Add([pscustomobject]@{ Name = $name; File = $fileIndex; Index = $match.Groups['name'].Index })
            }
        }
    }
    else {
        foreach ($match in $xamlNamePattern.Matches($text)) {
            $group = $match.Groups['name']
            $count = $local[$group.Value]
            if ($null -eq $count) { $local[$group.Value] = 1; $firsts[$group.Value] = $group.Index }
            else { $local[$group.Value] = $count + 1 }
        }
    }

    $row = Get-ProjectRow -Project $file.Project
    $row.CodeFiles++
    $hasOwned = $false
    [long]$tokens = 0
    foreach ($pair in $local.GetEnumerator()) {
        $info = $identifiers[$pair.Key]
        if ($null -eq $info) { $info = Get-IdentifierInfo -Identifier $pair.Key }
        if ($info.Words.Count -eq 0) { continue }
        if ($info.Count -eq 0) { $info.File = $fileIndex; $info.Index = $firsts[$pair.Key] }
        $info.Count += $pair.Value
        $tokens += [long]$pair.Value * $info.Words.Count
        if ($info.Owned) { $hasOwned = $true }
    }
    $row.CodeTokens += $tokens
    if ($hasOwned) { $ownedFiles++ }
}

# Code words, merged across case; $spellings counts each spelling to pick the shown one.
$codeCounts = [hashtable]::new([System.StringComparer]::OrdinalIgnoreCase)
$ownedCounts = [hashtable]::new([System.StringComparer]::OrdinalIgnoreCase)
$codeFirsts = [hashtable]::new([System.StringComparer]::OrdinalIgnoreCase)
$spellings = [hashtable]::new([System.StringComparer]::OrdinalIgnoreCase)
[long]$codeTokens = 0
[long]$ownedTokens = 0
foreach ($info in $identifiers.get_Values()) {
    if ($info.Count -eq 0) { continue }
    foreach ($word in $info.Words) {
        $current = $codeCounts[$word]
        if ($null -eq $current) {
            $codeCounts[$word] = $info.Count
            $codeFirsts[$word] = $info
            $spellings[$word] = [hashtable]::new([System.StringComparer]::Ordinal)
        }
        else {
            $codeCounts[$word] = $current + $info.Count
            $first = $codeFirsts[$word]
            if ($info.File -lt $first.File -or ($info.File -eq $first.File -and $info.Index -lt $first.Index)) { $codeFirsts[$word] = $info }
        }
        $forms = $spellings[$word]
        $form = $forms[$word]
        if ($null -eq $form) { $forms[$word] = $info.Count }
        else { $forms[$word] = $form + $info.Count }
        $codeTokens += $info.Count
        if ($info.Owned) {
            $current = $ownedCounts[$word]
            if ($null -eq $current) { $ownedCounts[$word] = $info.Count }
            else { $ownedCounts[$word] = $current + $info.Count }
            $ownedTokens += $info.Count
        }
    }
}
# Re-key each merged word to its most frequent spelling, ties to the ordinal-first.
foreach ($key in @($spellings.get_Keys())) {
    $shown = $null
    foreach ($pair in $spellings[$key].GetEnumerator()) {
        if ($null -eq $shown -or $pair.Value -gt $spellings[$key][$shown] -or ($pair.Value -eq $spellings[$key][$shown] -and [string]::CompareOrdinal($pair.Key, $shown) -lt 0)) { $shown = $pair.Key }
    }
    $total = $codeCounts[$key]; $first = $codeFirsts[$key]
    $codeCounts.Remove($key); $codeFirsts.Remove($key)
    $codeCounts[$shown] = $total; $codeFirsts[$shown] = $first
    if ($ownedCounts.ContainsKey($key)) { $owned = $ownedCounts[$key]; $ownedCounts.Remove($key); $ownedCounts[$shown] = $owned }
}

# Comment corpus.
$fencePattern = [System.Text.RegularExpressions.Regex]::new('^\s*(```|~~~)', $compiled)
$spanPattern = [System.Text.RegularExpressions.Regex]::new('`+[^`]*`+', $compiled)
$linkPattern = [System.Text.RegularExpressions.Regex]::new('!?\[(?<text>[^\]]*)\]\([^)]*\)', $compiled)
$markupPattern = [System.Text.RegularExpressions.Regex]::new('^\s*(?:#+|[-*+>]|\d+[.)])\s+|[*_|~]', $compiled)
$proseWordPattern = [System.Text.RegularExpressions.Regex]::new('\p{L}+(?:[''\u2019-]\p{L}+)*', $compiled)
$cited = [hashtable]::new([System.StringComparer]::Ordinal)
$commentCounts = [hashtable]::new([System.StringComparer]::Ordinal)
$commentFirsts = [hashtable]::new([System.StringComparer]::Ordinal)
[long]$commentTokens = 0

foreach ($file in $commentFiles) {
    try { $lines = [System.IO.File]::ReadAllLines($file.Full) }
    catch {
        $readErrors.Add("$($file.Relative): $($_.Exception.Message)")
        continue
    }

    $row = Get-ProjectRow -Project $file.Project
    $row.CommentFiles++
    $inFence = $false
    for ($number = 1; $number -le $lines.Count; $number++) {
        $line = $lines[$number - 1]
        if ($number -eq 2 -and $line.StartsWith('Hash:')) { continue }
        if ($fencePattern.IsMatch($line)) { $inFence = -not $inFence; continue }
        if ($inFence) { continue }
        foreach ($span in $spanPattern.Matches($line)) {
            foreach ($match in $identifierPattern.Matches($span.Value)) { $cited[$match.Value] = $true }
        }
        $prose = $spanPattern.Replace($line, ' ')
        $prose = $linkPattern.Replace($prose, '${text}')
        $prose = $markupPattern.Replace($prose, ' ').Replace([string][char]0x2019, "'").ToLowerInvariant()
        foreach ($match in $proseWordPattern.Matches($prose)) {
            $word = $match.Value
            if ($stopwords[$word]) { continue }
            $current = $commentCounts[$word]
            if ($null -eq $current) { $commentCounts[$word] = [long]1; $commentFirsts[$word] = "$($file.Relative):$number" }
            else { $commentCounts[$word] = $current + 1 }
            $commentTokens++
            $row.CommentTokens++
        }
    }
}

# Line numbers of code locations, counted from the raw text, which blanking left the same length.
$lineStarts = @{}
function Get-CodeLocation {
    param([int]$File, [int]$Index)

    if (-not $lineStarts.ContainsKey($File)) {
        $starts = [System.Collections.Generic.List[int]]::new()
        $starts.Add(0)
        $text = [System.IO.File]::ReadAllText($codeFiles[$File].Full)
        for ($i = $text.IndexOf("`n"); $i -ge 0; $i = $text.IndexOf("`n", $i + 1)) { $starts.Add($i + 1) }
        $lineStarts[$File] = $starts
    }
    $found = $lineStarts[$File].BinarySearch($Index)
    $line = if ($found -ge 0) { $found + 1 } else { -$found - 1 }
    return "$($codeFiles[$File].Relative):$line"
}

# Declarations.
$verbRows = @{}
$baseRows = @{}
$ownedTypes = [System.Collections.Generic.List[object]]::new()
foreach ($type in $declaredTypes) {
    $shape = Get-IdentifierShape -Identifier $type.Name
    if (-not $shape.Prefixed) { continue }
    if (-not $baseRows.ContainsKey($shape.Base)) { $baseRows[$shape.Base] = [pscustomobject]@{ Base = $shape.Base; Types = 0; Methods = 0 } }
    $baseRows[$shape.Base].Types++
    if ($shape.Owned) { $ownedTypes.Add($type) }
}
foreach ($method in $declaredMethods) {
    $shape = Get-IdentifierShape -Identifier $method.Name
    if (-not $shape.Prefixed) { continue }
    if (-not $baseRows.ContainsKey($shape.Base)) { $baseRows[$shape.Base] = [pscustomobject]@{ Base = $shape.Base; Types = 0; Methods = 0 } }
    $baseRows[$shape.Base].Methods++
    $parts = [string[]]$shape.Words
    if (-not $shape.Owned) { $parts = [string[]]$shape.Rest }
    $verb = $parts[$parts.Count - 1]
    if (-not $verbRows.ContainsKey($verb)) { $verbRows[$verb] = [pscustomobject]@{ Verb = $verb; Methods = 0; Owned = 0 } }
    $verbRows[$verb].Methods++
    if ($shape.Owned) { $verbRows[$verb].Owned++ }
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

# The result: every number built once, read by the console and the report alike.
function Get-Ratio {
    param([double]$Part, [double]$Whole)

    if ($Whole -gt 0) { return $Part / $Whole }
    return 0.0
}

function New-CorpusRow {
    param([string]$Corpus, [long]$Files, [long]$Tokens, [System.Collections.IDictionary]$Counts)

    $hapax = 0
    foreach ($value in $Counts.get_Values()) { if ($value -eq 1) { $hapax++ } }
    return [ordered]@{ corpus = $Corpus; files = $Files; tokens = $Tokens; vocabulary = [long]$Counts.get_Count(); typeToken = (Get-Ratio $Counts.get_Count() $Tokens); hapax = [long]$hapax }
}

$codeWordRows = [System.Collections.Generic.List[object]]::new()
$rank = 0
foreach ($word in (Sort-ByKey -Items @($codeCounts.get_Keys()) -Key { (Get-DescendingKey $codeCounts[$args[0]]) + $args[0] })) {
    $rank++
    $owned = [long]0
    if ($ownedCounts.ContainsKey($word)) { $owned = $ownedCounts[$word] }
    $codeWordRows.Add([ordered]@{ rank = $rank; word = $word; count = $codeCounts[$word]; share = (Get-Ratio $codeCounts[$word] $codeTokens) * 100.0; owned = $owned })
}

$commentWordRows = [System.Collections.Generic.List[object]]::new()
$rank = 0
foreach ($word in (Sort-ByKey -Items @($commentCounts.get_Keys()) -Key { (Get-DescendingKey $commentCounts[$args[0]]) + $args[0] })) {
    $rank++
    $commentWordRows.Add([ordered]@{ rank = $rank; word = $word; count = $commentCounts[$word]; share = (Get-Ratio $commentCounts[$word] $commentTokens) * 100.0 })
}

$codeLower = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($word in $codeCounts.get_Keys()) { [void]$codeLower.Add($word.ToLowerInvariant()) }
$commentOnlyRows = @($commentWordRows | Where-Object { -not $codeLower.Contains($_.word) } | ForEach-Object { [ordered]@{ word = $_.word; count = $_.count } })

$verbTable = @(Sort-ByKey -Items @($verbRows.get_Values()) -Key { (Get-DescendingKey $args[0].Methods) + $args[0].Verb } | ForEach-Object {
    [ordered]@{ verb = $_.Verb; methods = [long]$_.Methods; owned = [long]$_.Owned; registered = $registeredVerbs.Contains($_.Verb) }
})
$usedVerbs = [System.Collections.Generic.HashSet[string]]::new([string[]]@($verbRows.get_Values() | Where-Object { $_.Owned -gt 0 } | ForEach-Object { $_.Verb }), [System.StringComparer]::Ordinal)
$verbsUnused = @(Sort-ByKey -Items @($registeredVerbs | Where-Object { -not $usedVerbs.Contains($_) }) -Key { $args[0].ToLowerInvariant() + $args[0] })

$baseTable = @(Sort-ByKey -Items @($baseRows.get_Values()) -Key { (Get-DescendingKey ($args[0].Types + $args[0].Methods)) + $args[0].Base } | ForEach-Object {
    [ordered]@{ base = $_.Base; types = [long]$_.Types; methods = [long]$_.Methods; registered = $registeredBases.Contains($_.Base) }
})
$basesUnused = @(Sort-ByKey -Items @($registeredBases | Where-Object { -not $baseRows.ContainsKey($_) }) -Key { $args[0].ToLowerInvariant() + $args[0] })

$uncitedSeen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$uncitedRows = @(Sort-ByKey -Items @($ownedTypes | Where-Object { -not $cited.ContainsKey($_.Name) -and $uncitedSeen.Add($_.Name) }) -Key { $args[0].Name.ToLowerInvariant() + $args[0].Name } | ForEach-Object {
    [ordered]@{ type = $_.Name; location = (Get-CodeLocation -File $_.File -Index $_.Index) }
})

$codeHapaxRows = @(Sort-ByKey -Items @($codeCounts.get_Keys() | Where-Object { $codeCounts[$_] -eq 1 }) -Key { $args[0].ToLowerInvariant() + $args[0] } | ForEach-Object {
    $info = $codeFirsts[$_]
    [ordered]@{ word = $_; location = (Get-CodeLocation -File $info.File -Index $info.Index) }
})
$commentHapaxRows = @(Sort-ByKey -Items @($commentCounts.get_Keys() | Where-Object { $commentCounts[$_] -eq 1 }) -Key { $args[0] } | ForEach-Object {
    [ordered]@{ word = $_; location = $commentFirsts[$_] }
})

$projectTable = @(Sort-ByKey -Items @($projectRows.get_Values()) -Key { (Get-DescendingKey $args[0].CodeTokens) + $args[0].Project } | ForEach-Object {
    [ordered]@{ project = $_.Project; codeFiles = [long]$_.CodeFiles; codeTokens = [long]$_.CodeTokens; commentFiles = [long]$_.CommentFiles; commentTokens = [long]$_.CommentTokens; ratio = (Get-Ratio $_.CommentTokens $_.CodeTokens) }
})
$projectTotal = [ordered]@{ project = 'Total'; codeFiles = [long]$codeFiles.Count; codeTokens = $codeTokens; commentFiles = [long]$commentFiles.Count; commentTokens = $commentTokens; ratio = (Get-Ratio $commentTokens $codeTokens) }

$stats = [ordered]@{
    project = [string]$config.project
    version = $version
    generation = $script:StatsGeneration
    generated = $generatedAt.ToString('yyyy-MM-dd HH:mm:ss zzz')
    top = $Top
    scope = [ordered]@{
        codeFiles = [long]$codeFiles.Count
        commentFiles = [long]$commentFiles.Count
        projects = [long]$projectRows.get_Count()
        roots = @($rootEntries | ForEach-Object { $(if ($_.Prefix.Length -eq 0) { '.' } else { $_.Prefix.TrimEnd('/') }) })
        files = @($config.sources.files | ForEach-Object { [string]$_ })
        codeExtensions = @($codeExtensions)
        commentPattern = $commentPattern
        prefixes = @($prefixes)
        segments = $Segments
        excluded = @(Sort-ByKey -Items @($excludedNames) -Key { $args[0] })
        excludedSuffixes = @($excludedSuffixes)
        registry = (Get-RelativePathSafe -BasePath $repoRootFull -Path $registryPathFull)
    }
    summary = @(
        (New-CorpusRow -Corpus 'Code all' -Files $codeFiles.Count -Tokens $codeTokens -Counts $codeCounts),
        (New-CorpusRow -Corpus 'Code owned' -Files $ownedFiles -Tokens $ownedTokens -Counts $ownedCounts),
        (New-CorpusRow -Corpus 'Comments' -Files $commentFiles.Count -Tokens $commentTokens -Counts $commentCounts)
    )
    projects = @($projectTable) + @($projectTotal)
    codeWords = @($codeWordRows)
    commentWords = @($commentWordRows)
    verbs = @($verbTable)
    verbsUnused = @($verbsUnused)
    bases = @($baseTable)
    basesUnused = @($basesUnused)
    commentOnly = @($commentOnlyRows)
    uncited = @($uncitedRows)
    codeHapax = @($codeHapaxRows)
    commentHapax = @($commentHapaxRows)
    unreadable = @($readErrors)
    reports = @(
        [ordered]@{ kind = 'markdown'; path = $reportPathFull },
        [ordered]@{ kind = 'page'; path = $pagePathFull }
    )
}

# Console output.
Write-StatsLine ("Scanned: {0} code files, {1} comment files, {2} projects" -f (Format-Integer $stats.scope.codeFiles), (Format-Integer $stats.scope.commentFiles), (Format-Integer $stats.scope.projects)) -ForegroundColor DarkGray

function Select-Head {
    param([AllowEmptyCollection()][object[]]$Rows)

    return , @($Rows | Select-Object -First $Top)
}

function Format-Flag {
    param([bool]$Value)

    if ($Value) { return 'yes' }
    return 'no'
}

Write-SectionTitle 'Summary'
Write-ConsoleTable -Columns @(
    (New-ConsoleColumn -Name 'Corpus' -Right $false -Values @($stats.summary | ForEach-Object { $_.corpus })),
    (New-ConsoleColumn -Name 'Files' -Values @($stats.summary | ForEach-Object { Format-Integer $_.files })),
    (New-ConsoleColumn -Name 'Tokens' -Values @($stats.summary | ForEach-Object { Format-Integer $_.tokens })),
    (New-ConsoleColumn -Name 'Vocabulary' -Values @($stats.summary | ForEach-Object { Format-Integer $_.vocabulary })),
    (New-ConsoleColumn -Name 'Type/token' -Values @($stats.summary | ForEach-Object { Format-Ratio $_.typeToken })),
    (New-ConsoleColumn -Name 'Hapax' -Values @($stats.summary | ForEach-Object { Format-Integer $_.hapax }))
)

Write-SectionTitle 'Projects'
Write-ConsoleTable -Columns @(
    (New-ConsoleColumn -Name 'Project' -Right $false -Values @($stats.projects | ForEach-Object { $_.project })),
    (New-ConsoleColumn -Name 'Code files' -Values @($stats.projects | ForEach-Object { Format-Integer $_.codeFiles })),
    (New-ConsoleColumn -Name 'Code tokens' -Values @($stats.projects | ForEach-Object { Format-Integer $_.codeTokens })),
    (New-ConsoleColumn -Name 'Comment files' -Values @($stats.projects | ForEach-Object { Format-Integer $_.commentFiles })),
    (New-ConsoleColumn -Name 'Comment tokens' -Values @($stats.projects | ForEach-Object { Format-Integer $_.commentTokens })),
    (New-ConsoleColumn -Name 'Ratio' -Values @($stats.projects | ForEach-Object { Format-Ratio $_.ratio }))
)

if ($stats.codeWords.Count -gt 0) {
    $shown = Select-Head $stats.codeWords
    Write-SectionTitle 'Code words'
    Write-ConsoleTable -Columns @(
        (New-ConsoleColumn -Name 'Rank' -Values @($shown | ForEach-Object { Format-Integer $_.rank })),
        (New-ConsoleColumn -Name 'Word' -Right $false -Values @($shown | ForEach-Object { $_.word })),
        (New-ConsoleColumn -Name 'Count' -Values @($shown | ForEach-Object { Format-Integer $_.count })),
        (New-ConsoleColumn -Name 'Share' -Values @($shown | ForEach-Object { Format-Percent $_.share })),
        (New-ConsoleColumn -Name 'Owned' -Values @($shown | ForEach-Object { Format-Integer $_.owned }))
    )
    Write-CutLine -Total $stats.codeWords.Count -Shown $shown.Count
}

if ($stats.commentWords.Count -gt 0) {
    $shown = Select-Head $stats.commentWords
    Write-SectionTitle 'Comment words'
    Write-ConsoleTable -Columns @(
        (New-ConsoleColumn -Name 'Rank' -Values @($shown | ForEach-Object { Format-Integer $_.rank })),
        (New-ConsoleColumn -Name 'Word' -Right $false -Values @($shown | ForEach-Object { $_.word })),
        (New-ConsoleColumn -Name 'Count' -Values @($shown | ForEach-Object { Format-Integer $_.count })),
        (New-ConsoleColumn -Name 'Share' -Values @($shown | ForEach-Object { Format-Percent $_.share }))
    )
    Write-CutLine -Total $stats.commentWords.Count -Shown $shown.Count
}

if ($stats.verbs.Count -gt 0) {
    $shown = Select-Head $stats.verbs
    Write-SectionTitle 'Verbs'
    Write-ConsoleTable -Columns @(
        (New-ConsoleColumn -Name 'Verb' -Right $false -Values @($shown | ForEach-Object { $_.verb })),
        (New-ConsoleColumn -Name 'Methods' -Values @($shown | ForEach-Object { Format-Integer $_.methods })),
        (New-ConsoleColumn -Name 'Registered' -Right $false -Values @($shown | ForEach-Object { Format-Flag $_.registered }))
    )
    Write-CutLine -Total $stats.verbs.Count -Shown $shown.Count
}

if ($stats.verbsUnused.Count -gt 0) {
    $shown = Select-Head $stats.verbsUnused
    Write-SectionTitle ("Verbs unused ({0})" -f (Format-Integer $stats.verbsUnused.Count))
    foreach ($item in $shown) { Write-StatsLine $item }
    Write-CutLine -Total $stats.verbsUnused.Count -Shown $shown.Count
}

if ($stats.bases.Count -gt 0) {
    $shown = Select-Head $stats.bases
    Write-SectionTitle 'Bases'
    Write-ConsoleTable -Columns @(
        (New-ConsoleColumn -Name 'Base' -Right $false -Values @($shown | ForEach-Object { $_.base })),
        (New-ConsoleColumn -Name 'Types' -Values @($shown | ForEach-Object { Format-Integer $_.types })),
        (New-ConsoleColumn -Name 'Methods' -Values @($shown | ForEach-Object { Format-Integer $_.methods })),
        (New-ConsoleColumn -Name 'Registered' -Right $false -Values @($shown | ForEach-Object { Format-Flag $_.registered }))
    )
    Write-CutLine -Total $stats.bases.Count -Shown $shown.Count
}

if ($stats.basesUnused.Count -gt 0) {
    $shown = Select-Head $stats.basesUnused
    Write-SectionTitle ("Bases unused ({0})" -f (Format-Integer $stats.basesUnused.Count))
    foreach ($item in $shown) { Write-StatsLine $item }
    Write-CutLine -Total $stats.basesUnused.Count -Shown $shown.Count
}

if ($stats.commentOnly.Count -gt 0) {
    $shown = Select-Head $stats.commentOnly
    Write-SectionTitle ("Comment-only words ({0})" -f (Format-Integer $stats.commentOnly.Count))
    Write-ConsoleTable -Columns @(
        (New-ConsoleColumn -Name 'Word' -Right $false -Values @($shown | ForEach-Object { $_.word })),
        (New-ConsoleColumn -Name 'Count' -Values @($shown | ForEach-Object { Format-Integer $_.count }))
    )
    Write-CutLine -Total $stats.commentOnly.Count -Shown $shown.Count
}

if ($stats.uncited.Count -gt 0) {
    $shown = Select-Head $stats.uncited
    Write-SectionTitle ("Uncited types ({0})" -f (Format-Integer $stats.uncited.Count))
    Write-ConsoleTable -Columns @(
        (New-ConsoleColumn -Name 'Type' -Right $false -Values @($shown | ForEach-Object { $_.type })),
        (New-ConsoleColumn -Name 'Declared' -Right $false -Values @($shown | ForEach-Object { $_.location }))
    )
    Write-CutLine -Total $stats.uncited.Count -Shown $shown.Count
}

foreach ($hapax in @(@{ Title = 'Code hapax'; Rows = $stats.codeHapax }, @{ Title = 'Comment hapax'; Rows = $stats.commentHapax })) {
    if ($hapax.Rows.Count -eq 0) { continue }
    $shown = Select-Head $hapax.Rows
    Write-SectionTitle ("{0} ({1})" -f $hapax.Title, (Format-Integer $hapax.Rows.Count))
    Write-ConsoleTable -Columns @(
        (New-ConsoleColumn -Name 'Word' -Right $false -Values @($shown | ForEach-Object { $_.word })),
        (New-ConsoleColumn -Name 'Location' -Right $false -Values @($shown | ForEach-Object { $_.location }))
    )
    Write-CutLine -Total $hapax.Rows.Count -Shown $shown.Count
}

if ($stats.unreadable.Count -gt 0) {
    Write-SectionTitle ("Unreadable files ({0})" -f (Format-Integer $stats.unreadable.Count))
    foreach ($item in $stats.unreadable) { Write-StatsLine $item }
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

$md.Add("# Word statistics - $version")
$md.Add('')
$md.Add("- Generated: $($stats.generated)")
$md.Add("- Source roots: $(ConvertTo-MarkdownCell ($stats.scope.roots -join ', '))")
$md.Add("- Root-level files: $(if ($stats.scope.files.Count -gt 0) { ConvertTo-MarkdownCell ($stats.scope.files -join ', ') } else { 'none' })")
$md.Add("- Code extensions: $(ConvertTo-MarkdownCell ($stats.scope.codeExtensions -join ', '))")
$md.Add("- Comment pattern: ``$commentPattern``")
$md.Add("- Owned prefixes: $(ConvertTo-MarkdownCell ($stats.scope.prefixes -join ', '))")
$md.Add("- Name registry: $(ConvertTo-MarkdownCell $stats.scope.registry)")
$md.Add("- Project segments: $Segments")
$md.Add("- Excluded directories: $(ConvertTo-MarkdownCell ($stats.scope.excluded -join ', '))")
$md.Add("- Excluded suffixes: $(ConvertTo-MarkdownCell ($stats.scope.excludedSuffixes -join ', '))")
$md.Add("- Scanned: $(Format-Integer $stats.scope.codeFiles) code files, $(Format-Integer $stats.scope.commentFiles) comment files, $(Format-Integer $stats.scope.projects) projects")

Add-MarkdownTable -Title 'Summary' -Headers @('Corpus', 'Files', 'Tokens', 'Vocabulary', 'Type/token', 'Hapax') -Right @($false, $true, $true, $true, $true, $true) -Rows $stats.summary -Cells {
    param($r) $r.corpus, (Format-Integer $r.files), (Format-Integer $r.tokens), (Format-Integer $r.vocabulary), (Format-Ratio $r.typeToken), (Format-Integer $r.hapax)
}
Add-MarkdownTable -Title 'Projects' -Headers @('Project', 'Code files', 'Code tokens', 'Comment files', 'Comment tokens', 'Ratio') -Right @($false, $true, $true, $true, $true, $true) -Rows $stats.projects -Cells {
    param($r) $r.project, (Format-Integer $r.codeFiles), (Format-Integer $r.codeTokens), (Format-Integer $r.commentFiles), (Format-Integer $r.commentTokens), (Format-Ratio $r.ratio)
}
Add-MarkdownTable -Title 'Code words' -Headers @('Rank', 'Word', 'Count', 'Share', 'Owned') -Right @($true, $false, $true, $true, $true) -Rows $stats.codeWords -Cells {
    param($r) (Format-Integer $r.rank), $r.word, (Format-Integer $r.count), (Format-Percent $r.share), (Format-Integer $r.owned)
}
Add-MarkdownTable -Title 'Comment words' -Headers @('Rank', 'Word', 'Count', 'Share') -Right @($true, $false, $true, $true) -Rows $stats.commentWords -Cells {
    param($r) (Format-Integer $r.rank), $r.word, (Format-Integer $r.count), (Format-Percent $r.share)
}
Add-MarkdownTable -Title 'Verbs' -Headers @('Verb', 'Methods', 'Registered') -Right @($false, $true, $false) -Rows $stats.verbs -Cells {
    param($r) $r.verb, (Format-Integer $r.methods), (Format-Flag $r.registered)
}
Add-MarkdownTable -Title ("Verbs unused ({0})" -f (Format-Integer $stats.verbsUnused.Count)) -Headers @('Verb') -Right @($false) -Rows $stats.verbsUnused -Cells {
    param($r) $r
}
Add-MarkdownTable -Title 'Bases' -Headers @('Base', 'Types', 'Methods', 'Registered') -Right @($false, $true, $true, $false) -Rows $stats.bases -Cells {
    param($r) $r.base, (Format-Integer $r.types), (Format-Integer $r.methods), (Format-Flag $r.registered)
}
Add-MarkdownTable -Title ("Bases unused ({0})" -f (Format-Integer $stats.basesUnused.Count)) -Headers @('Base') -Right @($false) -Rows $stats.basesUnused -Cells {
    param($r) $r
}
Add-MarkdownTable -Title ("Comment-only words ({0})" -f (Format-Integer $stats.commentOnly.Count)) -Headers @('Word', 'Count') -Right @($false, $true) -Rows $stats.commentOnly -Cells {
    param($r) $r.word, (Format-Integer $r.count)
}
Add-MarkdownTable -Title ("Uncited types ({0})" -f (Format-Integer $stats.uncited.Count)) -Headers @('Type', 'Declared') -Right @($false, $false) -Rows $stats.uncited -Cells {
    param($r) $r.type, $r.location
}
Add-MarkdownTable -Title ("Code hapax ({0})" -f (Format-Integer $stats.codeHapax.Count)) -Headers @('Word', 'Location') -Right @($false, $false) -Rows $stats.codeHapax -Cells {
    param($r) $r.word, $r.location
}
Add-MarkdownTable -Title ("Comment hapax ({0})" -f (Format-Integer $stats.commentHapax.Count)) -Headers @('Word', 'Location') -Right @($false, $false) -Rows $stats.commentHapax -Cells {
    param($r) $r.word, $r.location
}
if ($stats.unreadable.Count -gt 0) {
    Add-MarkdownTable -Title ("Unreadable files ({0})" -f (Format-Integer $stats.unreadable.Count)) -Headers @('File') -Right @($false) -Rows $stats.unreadable -Cells {
        param($r) $r
    }
}

[System.IO.Directory]::CreateDirectory($reportDirectoryFull) | Out-Null
[System.IO.File]::WriteAllText($reportPathFull, (($md -join "`n") + "`n"), [System.Text.UTF8Encoding]::new($false))

# Page output: the full result as data inside the template StatsWords.html.
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

$pageTemplatePath = Join-Path $PSScriptRoot 'StatsWords.html'
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
