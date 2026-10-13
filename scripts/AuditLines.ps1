<#
.SYNOPSIS
Counts source lines under the configured roots, prints the results, and writes a Markdown report and a page.

.DESCRIPTION
Reads the project configuration from AuditLines.json next to this script, then
performs these actions on every run:
  1. Prints the result: each gate with its status and meaning, then the verdict.
  2. Prints the hits and the ceiling of each kind.
  3. Prints line and size totals for each folder under the source roots, and one row per tally root.
  4. Prints every counted file over the line limit, and every file in the warning band below it.
  5. Prints every line over the width limit of its extension; the band below it goes to the report only.
  6. Prints the files that gained the most lines over the last commits.
  7. Writes a Markdown source-line report to {report.directory}\{prefix}{version}.md.
  8. Writes the full result as a page to {report.directory}\{prefix}{version}.html.
     The page opens in the default browser unless -NoOpen is given.
The console follows scripts\report.md: widest view first, empty lists left out.
The page holds everything the console and the report hold, plus every counted file.
It fills the template AuditLines.html next to this script, and works offline.
It compares the folder totals with the newest record file in the
history.records folder other than the current version, and links the newest
history page.
Comment-file totals print in AuditComments only.

Everything project-specific - source roots, counted extensions, excluded
directory names, thresholds, ceilings, report location, version file -
lives in AuditLines.json. The script itself carries no project knowledge.
Files come from git: tracked and untracked files, never ignored ones, as in the
convention tests. Segments and extensions compare without case, and every path
prints with forward slashes. A configured root without a directory fails the run.
Git is the only external tool required.

AuditLines.json shape:
  {
    "generation": 25,
    "project": "Cadroue",
    "enforced": true,
    "ceilings": { "Length": 0, "Width": 0 },
    "sources": {
      "roots": ["src", "tests"],
      "extensions": [".cs", ".xaml", ".csproj"],
      "excludeSegments": [".git", ".vs", "bin", "obj", "artifacts", "packages", "node_modules", "publish"]
    },
    "thresholds": { "limit": 500, "warning": 400, "band": 5 },
    "width": { ".cs": 120, ".xaml": 200 },
    "hotspot": { "commits": 30, "top": 15 },
    "tally": { "roots": ["scripts"], "extensions": [".ps1", ".cs"] },
    "report": {
      "directory": "docs-analysis",
      "versionFile": "version.json",
      "versionKey": "current-version",
      "prefix": "AuditLines-",
      "segments": 1
    },
    "history": {
      "records": "docs-analysis/records/AuditLinesHistory",
      "pagePrefix": "AuditLinesHistory-"
    }
  }

A file over thresholds.limit lines is a Length hit, and one over thresholds.warning a warning.
Width maps an extension to the longest line it allows, 0 included; extensions left
out are not checked. A line over its limit is a Width hit, and a line within
thresholds.band columns of it a warning. Warnings never gate.
Enforced, a kind above its ceiling fails the run. A ceiling above its count is
stale and fails the run even when the rules are not enforced.
Hotspot reads git history for the files that gained the most lines.
Tally counts its roots with its own extensions into the folder totals only,
one row per root, never into the length or width gates or the extension totals.

.PARAMETER ConfigPath
Path to the JSON configuration. Defaults to AuditLines.json next to this script.

.PARAMETER SourceRoots
Overrides sources.roots for this run.

.PARAMETER Extensions
Overrides sources.extensions for this run.

.PARAMETER LimitThreshold
Overrides thresholds.limit for this run.

.PARAMETER WarningThreshold
Overrides thresholds.warning for this run.

.PARAMETER Segments
Overrides report.segments: how many path segments under a root form a folder row.

.PARAMETER Commits
Overrides hotspot.commits: how many recent commits feed the hotspot table. 0 skips it.

.PARAMETER OutputPath
Overrides the Markdown line-count report path for this run.

.PARAMETER Open
Open the generated report after the audit finishes.

.PARAMETER NoOpen
Write the page without opening it.

.PARAMETER NoPause
Do not stop at each console page. Paging is also off when output or input is redirected.

.PARAMETER Help
Display this help and exit without running the audit. The alias -? is supported.

.EXAMPLE
AuditLines

.EXAMPLE
AuditLines -LimitThreshold 600 -WarningThreshold 500

.EXAMPLE
AuditLines -Extensions .cs, .xaml

.EXAMPLE
AuditLines -SourceRoots .\src, .\tests
#>
#requires -Version 5.1
# AUDITLINES - AUDIT GENERATION 25.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the audited outcome changes.
# A generation names the set of checks the audit applies. Two projects on the same generation audit
# the same things and their reports compare directly, whatever else differs between the files. A check
# added, removed, or changed in what it reports is a new generation; wording, plumbing, and refactoring
# leave it alone. Every audit script shares one generation number with the convention-test settings,
# and each refuses a configuration written at another generation.
# Generation 11: nothing the line audit reports changes; the number rises with the truth audit,
# which checks that a deportment field reaches no request, keeps one writer, holds no logic and
# treats no engine data.
# Generation 12: nothing the line audit reports changes; the number rises with the convention tests,
# which bind with no compile error, count chain ceilings in names, count a using or a call on a
# deeper record as a reach, and exempt a contract name only where the type declares the interface.
# Generation 13: nothing this audit reports changes; the number rises with the UI audit, which
# counts every surface markup line that hooks logic into the markup and every surface member
# that is not a constructor.
# Generation 14: nothing this audit reports changes; the number rises with the UI audit, which
# also counts command parameters, member paths and literal tags in surface markup as hooks.
# Generation 15: nothing this audit reports changes; the number rises with the name audit, which
# counts prefix turfs, and the UI audit, which counts pack URIs, scaffold types and contract IDs.
# Generation 16: nothing this audit reports changes; the number rises with the structure audit, whose
# Unsealing kind counts engine types on the public members of sealed Deportment types.
# Generation 17: nothing this audit reports changes; the number rises with the vocabulary revision
# of the UI, object, structure and name audits.
# Generation 18: nothing this audit reports changes; the number rises with the UI audit, whose
# truth detector stops five false findings.
# Generation 19: nothing this audit reports changes; the number rises with the comment audit, whose
# hash line ties every comment file to the sources it describes.
# Generation 20: nothing this audit reports changes; the number rises with the UI audit, whose
# Mismatching kind only informs while a driver folder it waits on holds no source.
# Generation 21: nothing this audit reports changes; the number rises with the structure, name and
# UI audits, which gained kinds, and with the new fault audit.
# Generation 22: nothing this audit reports changes; the number rises with the structure audit,
# whose Unsealing kind walks every public type of a sealed ring, whatever its prefix.
[CmdletBinding()]
param(
    [string]$ConfigPath,
    [string[]]$SourceRoots,
    [string[]]$Extensions,
    [ValidateRange(1, [int]::MaxValue)]
    [int]$LimitThreshold,
    [ValidateRange(0, [int]::MaxValue)]
    [int]$WarningThreshold,
    [ValidateRange(1, 8)]
    [int]$Segments,
    [ValidateRange(0, [int]::MaxValue)]
    [int]$Commits,
    [string]$OutputPath,
    [switch]$Open,
    [switch]$NoOpen,
    [switch]$NoPause,
    [Alias('?')]
    [switch]$Help
)

if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    $ConfigPath = Join-Path $PSScriptRoot "AuditLines.json"
}

if ($Help) {
    @'
NAME
    AuditLines.ps1

SYNOPSIS
    Count source lines under the configured roots and write a Markdown report and a page.

SYNTAX
    AuditLines [-ConfigPath <path>] [-SourceRoots <path[]>]
        [-Extensions <extension[]>] [-LimitThreshold <number>]
        [-WarningThreshold <number>] [-Segments <number>] [-Commits <number>]
        [-OutputPath <path>] [-Open] [-NoOpen] [-NoPause] [-Help]

CONFIGURATION
    All project-specific values live in AuditLines.json next to the script:
    source roots, counted extensions, excluded directory names, thresholds,
    width limits, enforcement and ceilings, tally roots and extensions,
    report directory, version file and key, report file-name prefix, and
    the history records folder and page prefix the page compares with.
    Parameters below override it per run.

PAGE
    Every run writes the full result as a page next to the Markdown report.
    It opens in the default browser unless -NoOpen is given.

CHECKS
    Length: a file over thresholds.limit lines is a hit, and a file over
        thresholds.warning lines is a warning.
    Width: a line over the width limit of its extension is a hit, and a
        line within thresholds.band columns of it is a warning. A limit
        of 0 is a real limit, and an unlisted extension is not checked.
    Enforced, a kind above its ceiling fails. A ceiling above its count
    is stale and fails even when the rules are not enforced. Warnings,
    folder totals, tally rows and hotspots never fail. A configured root without a
    directory, or a failing git, stops the audit with an error.

OPTIONS
    -ConfigPath <path>
        JSON configuration file. Defaults to .\AuditLines.json.

    -SourceRoots <path[]>
        Source directories to count. Overrides sources.roots.

    -Extensions <extension[]>
        Extensions to count. Overrides sources.extensions.

    -LimitThreshold <number>
        Last line count that passes. Overrides thresholds.limit.

    -WarningThreshold <number>
        Last line count below the warning band. Overrides
        thresholds.warning.

    -Segments <number>
        Path segments under a root that form a folder row. Overrides
        report.segments. 1 lists projects, 2 lists their first-level folders.

    -Commits <number>
        Recent commits that feed the hotspot table. Overrides
        hotspot.commits. 0 skips the table.

    -OutputPath <path>
        Markdown line-count report path. By default, the project version is
        used to create a path under report.directory.

    -Open
        Open the generated line-count report after the audit finishes.

    -NoOpen
        Write the page without opening it.

    -NoPause
        Do not stop at each console page for a key. Off by itself when output
        or input is redirected.

    -Help, -?
        Display this help and exit without running the audit.

EXAMPLES
    AuditLines
        Audit with the configured settings.

    AuditLines -LimitThreshold 600 -WarningThreshold 500
        Report files over 600 lines, and warn on files from 501 to 600 lines.

    AuditLines -Extensions .cs, .xaml
        Count only C# and XAML files.

    AuditLines -SourceRoots .\src, .\tests
        Count both the src and tests source roots.
'@ | Write-Host
    exit 0
}

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Git writes UTF-8, so this process reads and writes UTF-8 and a non-ASCII path decodes alike on 5.1 and 7.
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding

$script:AuditGeneration = 25
$script:ItemLimit = 40

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

Write-AuditLine "AUDITLINES - AUDIT GENERATION $script:AuditGeneration" -ForegroundColor Blue


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

function Read-AuditConfig {
    param([Parameter(Mandatory = $true)][string]$Path)

    $pathFull = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $pathFull -PathType Leaf)) {
        throw "The line-audit configuration was not found: $pathFull"
    }

    try {
        $config = Get-Content -LiteralPath $pathFull -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The line-audit configuration is not valid JSON: $pathFull`n$($_.Exception.Message)"
    }

    $problems = [System.Collections.Generic.List[string]]::new()
    foreach ($key in @('generation', 'project', 'enforced', 'ceilings', 'sources.roots', 'sources.extensions', 'sources.excludeSegments',
                       'thresholds.limit', 'thresholds.warning', 'thresholds.band', 'width', 'hotspot.commits', 'hotspot.top',
                       'report.directory', 'report.versionFile', 'report.versionKey', 'report.prefix', 'report.segments',
                       'history.records', 'history.pagePrefix', 'tally.roots', 'tally.extensions')) {
        if ($null -eq (Get-ConfigNode -Document $config -Key $key)) {
            $problems.Add("missing key '$key'")
        }
    }

    if ($problems.Count -eq 0) {
        if (-not ($config.enforced -is [bool])) {
            $problems.Add("key 'enforced' is not true or false")
        }
        foreach ($property in $config.ceilings.PSObject.Properties) {
            if (-not (($property.Value -is [int] -or $property.Value -is [long]) -and $property.Value -ge 0)) {
                $problems.Add("ceiling '$($property.Name)' is not a whole number of at least 0")
            }
        }
        if (-not (($config.thresholds.band -is [int] -or $config.thresholds.band -is [long]) -and $config.thresholds.band -ge 0)) {
            $problems.Add("key 'thresholds.band' is not a whole number of at least 0")
        }
    }

    if ($problems.Count -gt 0) {
        throw "The line-audit configuration is not valid: $pathFull`n  " + ($problems -join "`n  ")
    }

    return $config
}

function Join-AuditPath {
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
$config = Read-AuditConfig -Path $ConfigPath

if ([int]$config.generation -ne $script:AuditGeneration) {
    throw "The line-audit configuration is generation $($config.generation) but this tooling is generation $script:AuditGeneration.`nA generation names the set of checks applied, so the two must match: $ConfigPath"
}

if (-not $PSBoundParameters.ContainsKey('SourceRoots')) { $SourceRoots = @($config.sources.roots) }
if (-not $PSBoundParameters.ContainsKey('Extensions')) { $Extensions = @($config.sources.extensions) }
if (-not $PSBoundParameters.ContainsKey('LimitThreshold')) { $LimitThreshold = [int]$config.thresholds.limit }
if (-not $PSBoundParameters.ContainsKey('WarningThreshold')) { $WarningThreshold = [int]$config.thresholds.warning }
if (-not $PSBoundParameters.ContainsKey('Segments')) { $Segments = [int]$config.report.segments }
if (-not $PSBoundParameters.ContainsKey('Commits')) { $Commits = [int]$config.hotspot.commits }
$hotspotCommits = $Commits
$hotspotTop = [int]$config.hotspot.top

$widthLimits = @{}
foreach ($property in $config.width.PSObject.Properties) {
    $widthLimits[([string]$property.Name).ToLowerInvariant()] = [int]$property.Value
}

$widthBand = [int]$config.thresholds.band
$enforced = [bool]$config.enforced
$ceilings = [ordered]@{}
foreach ($property in $config.ceilings.PSObject.Properties) {
    $ceilings[[string]$property.Name] = [int]$property.Value
}

$reportDirectoryFull = Join-AuditPath -Root $repoRootFull -Relative $config.report.directory
$versionPathFull = Join-AuditPath -Root $repoRootFull -Relative $config.report.versionFile
$versionKey = [string]$config.report.versionKey
$reportPrefix = [string]$config.report.prefix

if ($LimitThreshold -lt 1) {
    throw "thresholds.limit ($LimitThreshold) must be at least 1."
}

if ($WarningThreshold -ge $LimitThreshold) {
    throw "WarningThreshold ($WarningThreshold) must be lower than LimitThreshold ($LimitThreshold)."
}

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

    return $Value.ToString("0.0", [System.Globalization.CultureInfo]::InvariantCulture) + " %"
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

function Write-SectionTitle {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [ConsoleColor]$Color = [ConsoleColor]::Blue
    )

    Write-AuditLine ""
    Write-AuditLine $Text -ForegroundColor $Color
    Write-AuditLine ('-' * $Text.Length) -ForegroundColor DarkGray
}

function Write-ResultTable {
    param([Parameter(Mandatory = $true)][object[]]$Rows)

    Write-SectionTitle "Result"
    $statusWidth = 6
    $countWidth = [Math]::Max(5, ($Rows | ForEach-Object { (Format-Integer $_.Count).Length } | Measure-Object -Maximum).Maximum)
    $gateWidth = [Math]::Max(4, ($Rows | ForEach-Object { $_.Gate.Length } | Measure-Object -Maximum).Maximum)
    $meaningWidth = [Math]::Max(7, ($Rows | ForEach-Object { $_.Meaning.Length } | Measure-Object -Maximum).Maximum)
    Write-AuditLine ("{0}  {1}  {2}  Meaning" -f 'Status'.PadRight($statusWidth), 'Count'.PadLeft($countWidth), 'Gate'.PadRight($gateWidth)) -ForegroundColor Cyan
    Write-AuditLine (@(('-' * $statusWidth), ('-' * $countWidth), ('-' * $gateWidth), ('-' * $meaningWidth)) -join '  ') -ForegroundColor Cyan
    foreach ($row in $Rows) {
        $failing = $row.Count -gt 0
        $status = if ($failing) { 'FAIL' } else { 'OK' }
        $text = "{0}  {1}  {2}  {3}" -f $status.PadRight($statusWidth), (Format-Integer $row.Count).PadLeft($countWidth), $row.Gate.PadRight($gateWidth), $row.Meaning
        Write-AuditLine $text -Lead $status -LeadColor $(if ($failing) { 'Red' } else { 'Green' })
    }

    $failed = @($Rows | Where-Object { $_.Count -gt 0 })
    Write-AuditLine ""
    if ($failed.Count -eq 0) {
        Write-AuditLine ("PASS: all {0} gates at 0." -f $Rows.Count) -ForegroundColor Green
    }
    else {
        $sections = ($failed | ForEach-Object { '"' + $_.Section + '"' }) -join ', '
        Write-AuditLine ("FAIL: {0} of {1} gates above 0. See {2}." -f $failed.Count, $Rows.Count, $sections) -ForegroundColor Red
    }
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

# Renders a table whose header has two rows: group names above, column names below.
# Each column is @{ Group = ''; Name = ''; Right = $true; Values = @() }.
# A column without a group prints its name on the upper row and leaves the lower row blank.
function Write-GroupedConsoleTable {
    param(
        [Parameter(Mandatory = $true)][object[]]$Columns
    )

    $gap = "  "
    foreach ($column in $Columns) {
        $width = [Math]::Max($column.Width, $column.Name.Length)
        foreach ($value in $column.Values) {
            if ($value.Length -gt $width) { $width = $value.Length }
        }
        $column.Width = $width
    }

    $groups = [System.Collections.Generic.List[object]]::new()
    $index = 0
    while ($index -lt $Columns.Count) {
        $column = $Columns[$index]
        $last = $index
        if (-not [string]::IsNullOrEmpty($column.Group)) {
            while ($last + 1 -lt $Columns.Count -and $Columns[$last + 1].Group -eq $column.Group) { $last++ }
        }
        $groups.Add([pscustomobject]@{ First = $index; Last = $last; Label = if ([string]::IsNullOrEmpty($column.Group)) { $column.Name } else { $column.Group } })
        $index = $last + 1
    }

    foreach ($group in $groups) {
        $span = 0
        for ($i = $group.First; $i -le $group.Last; $i++) {
            $span += $Columns[$i].Width
            if ($i -lt $group.Last) { $span += $gap.Length }
        }
        if ($group.Label.Length -gt $span) {
            $Columns[$group.Last].Width += $group.Label.Length - $span
        }
    }

    $upper = [System.Text.StringBuilder]::new()
    foreach ($group in $groups) {
        $span = 0
        for ($i = $group.First; $i -le $group.Last; $i++) {
            $span += $Columns[$i].Width
            if ($i -lt $group.Last) { $span += $gap.Length }
        }
        if ($upper.Length -gt 0) { [void]$upper.Append($gap) }
        [void]$upper.Append($group.Label.PadRight($span))
    }

    $lower = [System.Text.StringBuilder]::new()
    $rule = [System.Text.StringBuilder]::new()
    foreach ($column in $Columns) {
        if ($lower.Length -gt 0) { [void]$lower.Append($gap); [void]$rule.Append($gap) }
        $label = if ([string]::IsNullOrEmpty($column.Group)) { "" } else { $column.Name }
        [void]$lower.Append((Format-Cell -Text $label -Width $column.Width -Right $column.Right))
        [void]$rule.Append('-' * $column.Width)
    }

    Write-AuditLine $upper.ToString().TrimEnd() -ForegroundColor Cyan
    if (@($Columns | Where-Object { -not [string]::IsNullOrEmpty($_.Group) }).Count -gt 0) {
        Write-AuditLine $lower.ToString().TrimEnd() -ForegroundColor Cyan
    }
    Write-AuditLine $rule.ToString() -ForegroundColor Cyan

    $rowCount = $Columns[0].Values.Count
    for ($row = 0; $row -lt $rowCount; $row++) {
        $line = [System.Text.StringBuilder]::new()
        foreach ($column in $Columns) {
            if ($line.Length -gt 0) { [void]$line.Append($gap) }
            [void]$line.Append((Format-Cell -Text $column.Values[$row] -Width $column.Width -Right $column.Right))
        }
        Write-AuditLine $line.ToString().TrimEnd()
    }
}

function New-ConsoleColumn {
    param(
        [string]$Group,
        [Parameter(Mandatory = $true)][string]$Name,
        [bool]$Right = $true,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$Values
    )

    return [pscustomobject]@{ Group = $Group; Name = $Name; Right = $Right; Values = $Values; Width = 0 }
}

# Gives columns that share a group and name the same width across every table passed in.
function Sync-ConsoleColumnWidth {
    param(
        [Parameter(Mandatory = $true)][object[][]]$Tables
    )

    $widths = @{}
    foreach ($table in $Tables) {
        foreach ($column in $table) {
            $key = "$($column.Group)|$($column.Name)"
            $width = $column.Name.Length
            foreach ($value in $column.Values) {
                if ($value.Length -gt $width) { $width = $value.Length }
            }
            if (-not $widths.ContainsKey($key) -or $widths[$key] -lt $width) { $widths[$key] = $width }
        }
    }

    foreach ($table in $Tables) {
        foreach ($column in $table) {
            $column.Width = $widths["$($column.Group)|$($column.Name)"]
        }
    }
}

function Invoke-AuditGit {
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

$sourceRootFulls = [System.Collections.Generic.List[string]]::new()
$rootEntries = [System.Collections.Generic.List[object]]::new()
foreach ($root in $SourceRoots) {
    if ([string]::IsNullOrWhiteSpace($root)) {
        continue
    }

    $rootFull = Join-AuditPath -Root $repoRootFull -Relative $root
    if (-not [System.IO.Directory]::Exists($rootFull)) {
        throw "The configured source root has no directory: $rootFull"
    }

    $rootRelative = (Get-RelativePathSafe -BasePath $repoRootFull -Path $rootFull).Trim('/')
    [void]$sourceRootFulls.Add($rootFull)
    $rootEntries.Add([pscustomobject]@{ Full = $rootFull; Prefix = $(if ($rootRelative.Length -eq 0) { '' } else { $rootRelative + '/' }) })
}

if ($sourceRootFulls.Count -eq 0) {
    throw "At least one source root must be supplied."
}

$normalizedExtensions = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($extension in $Extensions) {
    if ([string]::IsNullOrWhiteSpace($extension)) {
        continue
    }

    $normalized = $extension.Trim()
    if (-not $normalized.StartsWith('.', [System.StringComparison]::Ordinal)) {
        $normalized = "." + $normalized
    }

    [void]$normalizedExtensions.Add($normalized)
}

if ($normalizedExtensions.Count -eq 0) {
    throw "At least one file extension must be supplied."
}

$excludedNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($segment in @($config.sources.excludeSegments)) {
    if (-not [string]::IsNullOrWhiteSpace($segment)) {
        [void]$excludedNames.Add([string]$segment)
    }
}

$folderResults = [System.Collections.Generic.List[object]]::new()
$fileResults = [System.Collections.Generic.List[object]]::new()
$wideHits = [System.Collections.Generic.List[object]]::new()
$wideWarnings = [System.Collections.Generic.List[object]]::new()
$extensionTotals = @{}
$readErrors = [System.Collections.Generic.List[string]]::new()

$solutionFile = @([System.IO.Directory]::GetFiles($repoRootFull, '*.slnx') + [System.IO.Directory]::GetFiles($repoRootFull, '*.sln')) | Select-Object -First 1
$projectPrefix = if ($null -eq $solutionFile) { '' } else { [System.IO.Path]::GetFileNameWithoutExtension($solutionFile) + '.' }

function Get-FolderKey {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Root,
        [Parameter(Mandatory = $true)][string]$Relative
    )

    $cut = $Relative.LastIndexOf('/')
    $parts = @(if ($cut -gt 0) { $Relative.Substring(0, $cut).Split("/") })
    $take = [Math]::Min($Segments, $parts.Count)
    $labels = @($parts | Select-Object -First $take | ForEach-Object {
        if ($projectPrefix.Length -gt 0 -and $_.Length -gt $projectPrefix.Length -and $_.StartsWith($projectPrefix, [System.StringComparison]::OrdinalIgnoreCase)) { $_.Substring($projectPrefix.Length) } else { $_ }
    })
    if ($Root.Length -gt 0) { $labels = @($Root) + $labels }
    if ($labels.Count -eq 0) { return "(root)" }
    return ($labels -join ' / ')
}

function New-FolderResult {
    param([Parameter(Mandatory = $true)][string]$Name)

    return [pscustomobject]@{
        Name = $Name
        Files = 0
        Lines = [long]0
        NonBlankLines = [long]0
        BlankLines = [long]0
        Bytes = [long]0
    }
}

# One listing from the repository root, filtered by root, so overlapping roots never count a file twice.
$listArguments = @('-C', $repoRootFull, '-c', 'core.quotePath=false', 'ls-files', '--cached', '--others', '--exclude-standard', '--') +
    @($normalizedExtensions | ForEach-Object { ':(icase)*' + $_ })
$listing = Invoke-AuditGit -Arguments $listArguments
if ($listing.ExitCode -ne 0) {
    throw "Git could not enumerate the source files under $repoRootFull, so the audit cannot judge."
}

$sourcePaths = [System.Collections.Generic.List[string]]::new()
$sourceRelatives = @{}
$sourceUnderRoots = @{}
$sourceOwners = @{}
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
    if (-not [System.IO.File]::Exists($full)) { continue }

    $sourcePaths.Add($full)
    $sourceRelatives[$full] = $relative
    $sourceUnderRoots[$full] = $relative.Substring($owner.Prefix.Length)
    $sourceOwners[$full] = $owner.Prefix.TrimEnd('/')
}
$sourcePaths.Sort([System.StringComparer]::OrdinalIgnoreCase)

$folderMap = [ordered]@{}
$order = 0
foreach ($fileFull in $sourcePaths) {
    $file = [System.IO.FileInfo]::new($fileFull)
    $relative = [string]$sourceRelatives[$fileFull]
    $folderKey = Get-FolderKey -Root ([string]$sourceOwners[$fileFull]) -Relative ([string]$sourceUnderRoots[$fileFull])
    $extensionKey = $file.Extension.ToLowerInvariant()
    $hasWidth = $widthLimits.ContainsKey($extensionKey)
    $widthLimit = if ($hasWidth) { $widthLimits[$extensionKey] } else { 0 }
    [long]$fileLines = 0
    [long]$fileNonBlankLines = 0
    $fileWide = [System.Collections.Generic.List[object]]::new()

    try {
        foreach ($line in [System.IO.File]::ReadLines($fileFull)) {
            $fileLines++
            if (-not [string]::IsNullOrWhiteSpace($line)) { $fileNonBlankLines++ }
            if ($hasWidth -and $line.Length -gt $widthLimit - $widthBand) {
                $fileWide.Add([pscustomobject]@{ Width = $line.Length; Limit = $widthLimit; Location = "${relative}:$fileLines"; Text = $line })
            }
        }
    }
    catch {
        $readErrors.Add("${relative}: $($_.Exception.Message)")
        continue
    }

    foreach ($wide in $fileWide) {
        if ($wide.Width -gt $wide.Limit) { $wideHits.Add($wide) } else { $wideWarnings.Add($wide) }
    }

    if (-not $folderMap.Contains($folderKey)) { $folderMap[$folderKey] = New-FolderResult -Name $folderKey }
    $folder = $folderMap[$folderKey]
    $cut = $relative.LastIndexOf('/')
    $fileResults.Add([pscustomobject]@{
        Order = $order
        Folder = $folderKey
        Name = $file.Name
        RelativePath = $relative
        Location = $(if ($cut -lt 0) { '' } else { $relative.Substring(0, $cut) })
        Extension = $extensionKey
        Lines = $fileLines
        NonBlankLines = $fileNonBlankLines
        BlankLines = $fileLines - $fileNonBlankLines
        Bytes = $file.Length
    })
    $order++

    $folder.Files++
    $folder.Lines += $fileLines
    $folder.NonBlankLines += $fileNonBlankLines
    $folder.BlankLines += $fileLines - $fileNonBlankLines
    $folder.Bytes += $file.Length

    if (-not $extensionTotals.ContainsKey($extensionKey)) {
        $extensionTotals[$extensionKey] = [pscustomobject]@{ Extension = $extensionKey; Files = 0; Lines = [long]0; NonBlankLines = [long]0 }
    }
    $extensionTotals[$extensionKey].Files++
    $extensionTotals[$extensionKey].Lines += $fileLines
    $extensionTotals[$extensionKey].NonBlankLines += $fileNonBlankLines
}

# Tally roots join the folder totals as one row each, and never reach the gates or the extension totals.
$tallyNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$tallyExtensions = @($config.tally.extensions | ForEach-Object { ([string]$_).Trim() } | Where-Object { $_.Length -gt 0 } |
    ForEach-Object { if ($_.StartsWith('.', [System.StringComparison]::Ordinal)) { $_ } else { '.' + $_ } })
foreach ($tallyRoot in @($config.tally.roots)) {
    if ([string]::IsNullOrWhiteSpace($tallyRoot) -or $tallyExtensions.Count -eq 0) { continue }

    $tallyFull = Join-AuditPath -Root $repoRootFull -Relative $tallyRoot
    if (-not [System.IO.Directory]::Exists($tallyFull)) {
        throw "The configured tally root has no directory: $tallyFull"
    }

    $tallyName = (Get-RelativePathSafe -BasePath $repoRootFull -Path $tallyFull).Trim('/')
    [void]$tallyNames.Add($tallyName)
    $tallyListing = Invoke-AuditGit -Arguments (@('-C', $repoRootFull, '-c', 'core.quotePath=false', 'ls-files', '--cached', '--others', '--exclude-standard', '--') +
        @($tallyExtensions | ForEach-Object { ':(icase)' + $tallyName + '/*' + $_ }))
    if ($tallyListing.ExitCode -ne 0) {
        throw "Git could not enumerate the tally files under $tallyFull, so the audit cannot judge."
    }

    foreach ($entry in $tallyListing.Lines) {
        $relative = ([string]$entry).Trim()
        if ($relative.Length -eq 0 -or (Test-IsExcludedPath -Relative $relative -ExcludedNames $excludedNames)) { continue }

        $full = [System.IO.Path]::Combine($repoRootFull, $relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
        if (-not [System.IO.File]::Exists($full) -or $sourceRelatives.ContainsKey($full)) { continue }

        $file = [System.IO.FileInfo]::new($full)
        [long]$fileLines = 0
        [long]$fileNonBlankLines = 0
        try {
            foreach ($line in [System.IO.File]::ReadLines($full)) {
                $fileLines++
                if (-not [string]::IsNullOrWhiteSpace($line)) { $fileNonBlankLines++ }
            }
        }
        catch {
            $readErrors.Add("${relative}: $($_.Exception.Message)")
            continue
        }

        if (-not $folderMap.Contains($tallyName)) { $folderMap[$tallyName] = New-FolderResult -Name $tallyName }
        $folder = $folderMap[$tallyName]
        $folder.Files++
        $folder.Lines += $fileLines
        $folder.NonBlankLines += $fileNonBlankLines
        $folder.BlankLines += $fileLines - $fileNonBlankLines
        $folder.Bytes += $file.Length
    }
}
foreach ($entry in $folderMap.Values) { $folderResults.Add($entry) }

if ($sourcePaths.Count -eq 0) {
    throw "No source file was scanned under the configured roots, so the audit cannot judge."
}

$folderResults = @(
    $folderResults |
        Sort-Object -Property @{ Expression = 'Lines'; Descending = $true }, @{ Expression = { Get-OrdinalKey $_.Name } }
)

# Largest first; ties keep the enumeration order, which sorts paths without case like the convention test.
$fileResults = @(
    $fileResults |
        Sort-Object -Property @{ Expression = 'Lines'; Descending = $true }, @{ Expression = 'Order'; Descending = $false }
)

$overLimitFiles = @($fileResults | Where-Object { $_.Lines -gt $LimitThreshold })
$warningFiles = @($fileResults | Where-Object { $_.Lines -gt $WarningThreshold -and $_.Lines -le $LimitThreshold })

$overLimitTitle = "Files over $LimitThreshold lines"
$warningTitle = "Files at $($WarningThreshold + 1)-$LimitThreshold lines"
$wideTitle = "Lines over the width limit"
$wideWarningTitle = "Lines within $widthBand columns of the width limit"

# Ceilings, judged like the convention test: a kind gates above its ceiling when enforced, a ceiling above its count is stale.
$kindCounts = [ordered]@{ Length = $overLimitFiles.Count; Width = $wideHits.Count }
$ceilingRows = [System.Collections.Generic.List[object]]::new()
[long]$lengthAbove = 0
[long]$widthAbove = 0
foreach ($kind in $kindCounts.Keys) {
    $count = [int]$kindCounts[$kind]
    $ceiling = if ($ceilings.Contains($kind)) { [int]$ceilings[$kind] } else { 0 }
    $above = if ($enforced -and $count -gt $ceiling) { $count - $ceiling } else { 0 }
    if ($kind -eq 'Length') { $lengthAbove = $above } else { $widthAbove = $above }
    $ceilingRows.Add([pscustomobject]@{ Kind = $kind; Hits = $count; Ceiling = $ceiling })
}
foreach ($kind in $ceilings.Keys) {
    if (-not $kindCounts.Contains($kind)) {
        $ceilingRows.Add([pscustomobject]@{ Kind = $kind; Hits = 0; Ceiling = [int]$ceilings[$kind] })
    }
}
$staleCeilings = @($ceilingRows | Where-Object { $_.Ceiling -gt $_.Hits })

[long]$totalLines = 0
[long]$totalNonBlankLines = 0
[long]$totalBytes = 0
[int]$totalFiles = 0
foreach ($item in $folderResults) {
    $totalLines += $item.Lines
    $totalNonBlankLines += $item.NonBlankLines
    $totalBytes += $item.Bytes
    $totalFiles += $item.Files
}

$largestFolder = if ($folderResults.Count -gt 0) { $folderResults[0] } else { $null }
$generatedAt = Get-Date
$extensionLabel = (($normalizedExtensions | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } }) -join ", ")
$blankLines = $totalLines - $totalNonBlankLines

# Version, read from the configured version file and key.
$version = "0.0.0"
if ([System.IO.File]::Exists($versionPathFull)) {
    try {
        $versionData = Get-Content -LiteralPath $versionPathFull -Raw -Encoding UTF8 | ConvertFrom-Json
        $versionValue = Get-ConfigNode -Document $versionData -Key $versionKey
        if (-not [string]::IsNullOrWhiteSpace([string]$versionValue)) {
            $version = [string]$versionValue
        }
    }
    catch {
        Write-AuditLine "The version file is not valid JSON, using $version : $versionPathFull"
    }
}
else {
    Write-AuditLine "The version file was not found, using $version : $versionPathFull"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $reportDirectoryFull ("{0}{1}.md" -f $reportPrefix, $version)
}

$outputPathFull = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = [System.IO.Path]::GetDirectoryName($outputPathFull)
if ([string]::IsNullOrWhiteSpace($outputDirectory)) {
    throw "The output path must include a valid directory: $outputPathFull"
}

[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null

function New-FileConsoleColumns {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Items
    )

    return @(
        (New-ConsoleColumn -Group 'Lines' -Name 'Raw' -Values @($Items | ForEach-Object { Format-Integer $_.Lines })),
        (New-ConsoleColumn -Group 'Lines' -Name 'Non-blank' -Values @($Items | ForEach-Object { Format-Integer $_.NonBlankLines })),
        (New-ConsoleColumn -Group 'Sizes' -Name 'Raw' -Values @($Items | ForEach-Object { Format-Integer $_.Bytes })),
        (New-ConsoleColumn -Name 'Location' -Right $false -Values @($Items | ForEach-Object { $_.Location })),
        (New-ConsoleColumn -Name 'File' -Right $false -Values @($Items | ForEach-Object { $_.Name }))
    )
}

function Write-WideConsoleTable {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Items
    )

    $shown = @($Items | Select-Object -First $script:ItemLimit)
    Write-GroupedConsoleTable -Columns @(
        (New-ConsoleColumn -Name 'Width' -Values @($shown | ForEach-Object { Format-Integer $_.Width })),
        (New-ConsoleColumn -Name 'Limit' -Values @($shown | ForEach-Object { Format-Integer $_.Limit })),
        (New-ConsoleColumn -Name 'Line' -Right $false -Values @($shown | ForEach-Object { $_.Location }))
    )
    if ($Items.Count -gt $shown.Count) {
        Write-AuditLine ("... and {0:N0} more in the report." -f ($Items.Count - $shown.Count))
    }
}

function Add-WideMarkdownTable {
    param(
        [Parameter(Mandatory = $true)][System.Text.StringBuilder]$Builder,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Items
    )

    if ($Items.Count -eq 0) {
        [void]$Builder.AppendLine("None.")
        return
    }

    [void]$Builder.AppendLine("| Width | Limit | Line |")
    [void]$Builder.AppendLine("|------:|------:|------|")
    foreach ($item in $Items) {
        [void]$Builder.AppendLine("| $(Format-Integer $item.Width) | $(Format-Integer $item.Limit) | $(ConvertTo-MarkdownCell $item.Location) |")
    }
}

function Write-FileConsoleTable {
    param(
        [Parameter(Mandatory = $true)][object[]]$Columns
    )

    Write-GroupedConsoleTable -Columns $Columns
}

function Add-FileMarkdownTable {
    param(
        [Parameter(Mandatory = $true)][System.Text.StringBuilder]$Builder,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Items
    )

    if ($Items.Count -eq 0) {
        [void]$Builder.AppendLine("None.")
        return
    }

    [void]$Builder.AppendLine("| Lines | Non-blank | Folder | File |")
    [void]$Builder.AppendLine("|------:|----------:|--------|------|")
    foreach ($item in $Items) {
        [void]$Builder.AppendLine("| $(Format-Integer $item.Lines) | $(Format-Integer $item.NonBlankLines) | $(ConvertTo-MarkdownCell $item.Folder) | $(ConvertTo-MarkdownCell $item.RelativePath) |")
    }
}

[System.IO.Directory]::CreateDirectory($reportDirectoryFull) | Out-Null

$widthLabel = (($widthLimits.Keys | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } } | ForEach-Object { "$_ $($widthLimits[$_])" }) -join ", ")

# Hotspots: files that gained the most lines over the last commits, read from git.
$hotspots = @()
$hotspotNote = ''
$hotspotTitle = "Hotspots over the last $hotspotCommits commit(s)"
if ($hotspotCommits -gt 0) {
    $gitArguments = @('-C', $repoRootFull, '-c', 'core.quotepath=false', 'log', '--numstat', '--format=', "-n", "$hotspotCommits", '--') + @($sourceRootFulls)
    $history = Invoke-AuditGit -Arguments $gitArguments
    if ($history.ExitCode -eq 0) {
        $growth = @{}
        foreach ($row in $history.Lines) {
            $parts = [string]$row -split "`t", 3
            if ($parts.Count -lt 3 -or $parts[0] -eq '-' ) { continue }
            # A rename reads "dir/{old => new}/file" or "old => new"; the file now lives at the new side.
            $path = ([string]$parts[2] -replace '\{[^{}]* => ([^{}]*)\}', '$1') -replace '//+', '/'
            if ($path.Contains(' => ')) { $path = ($path -split ' => ', 2)[1] }
            $extension = [System.IO.Path]::GetExtension($path)
            if (-not $normalizedExtensions.Contains($extension)) { continue }
            if (-not $growth.ContainsKey($path)) { $growth[$path] = [pscustomobject]@{ Path = $path; Added = [long]0; Deleted = [long]0 } }
            $growth[$path].Added += [long]$parts[0]
            $growth[$path].Deleted += [long]$parts[1]
        }
        $hotspots = @($growth.Values | Sort-Object -Property @{ Expression = 'Added'; Descending = $true }, @{ Expression = { Get-OrdinalKey $_.Path } } | Select-Object -First $hotspotTop)
    }
    else {
        $hotspotNote = "Git history could not be read, so hotspots are skipped."
    }

}

# Console output, from the widest view to the narrowest.
Write-AuditLine ("Scanned: {0:N0} source files" -f $fileResults.Count) -ForegroundColor DarkGray

$gateRows = @(
    [pscustomobject]@{ Gate = 'Length above ceiling'; Count = $lengthAbove; Meaning = "files over $LimitThreshold lines beyond the Length ceiling"; Section = $overLimitTitle },
    [pscustomobject]@{ Gate = 'Width above ceiling'; Count = $widthAbove; Meaning = 'lines over the width limit beyond the Width ceiling'; Section = $wideTitle },
    [pscustomobject]@{ Gate = 'Stale ceilings'; Count = $staleCeilings.Count; Meaning = 'ceilings set above their hits'; Section = 'Ceilings' },
    [pscustomobject]@{ Gate = 'Unreadable files'; Count = $readErrors.Count; Meaning = 'files the audit could not read'; Section = 'Unreadable files' }
)
Write-ResultTable -Rows $gateRows

Write-SectionTitle "Ceilings"
Write-GroupedConsoleTable -Columns @(
    (New-ConsoleColumn -Name 'Kind' -Right $false -Values @($ceilingRows | ForEach-Object { $_.Kind })),
    (New-ConsoleColumn -Name 'Hits' -Values @($ceilingRows | ForEach-Object { Format-Integer $_.Hits })),
    (New-ConsoleColumn -Name 'Ceiling' -Values @($ceilingRows | ForEach-Object { Format-Integer $_.Ceiling }))
)
if (-not $enforced) {
    Write-AuditLine "The line rules are not enforced, so hits above a ceiling do not fail the run." -ForegroundColor Yellow
}

$tableRows = @($folderResults) + @([pscustomobject]@{
    Name = 'Total'
    Files = $totalFiles
    Lines = $totalLines
    NonBlankLines = [long](($folderResults | Measure-Object -Property NonBlankLines -Sum).Sum)
    Bytes = $totalBytes
})
Write-SectionTitle "Source lines by folder"
Write-GroupedConsoleTable -Columns @(
    (New-ConsoleColumn -Name 'Folder' -Right $false -Values @($tableRows | ForEach-Object { $_.Name })),
    (New-ConsoleColumn -Name 'Files' -Values @($tableRows | ForEach-Object { Format-Integer $_.Files })),
    (New-ConsoleColumn -Group 'Lines' -Name 'Raw' -Values @($tableRows | ForEach-Object { Format-Integer $_.Lines })),
    (New-ConsoleColumn -Group 'Lines' -Name 'Non-blank' -Values @($tableRows | ForEach-Object { Format-Integer $_.NonBlankLines })),
    (New-ConsoleColumn -Group 'Lines' -Name 'Share' -Values @($tableRows | ForEach-Object { Format-Percent $(if ($totalLines -gt 0) { ($_.Lines / [double]$totalLines) * 100.0 } else { 0.0 }) })),
    (New-ConsoleColumn -Group 'Lines' -Name 'Average' -Values @($tableRows | ForEach-Object { Format-Integer $(if ($_.Files -gt 0) { [Math]::Round($_.Lines / [double]$_.Files) } else { 0 }) })),
    (New-ConsoleColumn -Group 'Sizes' -Name 'Total' -Values @($tableRows | ForEach-Object { Format-Integer $_.Bytes })),
    (New-ConsoleColumn -Group 'Sizes' -Name 'Share' -Values @($tableRows | ForEach-Object { Format-Percent $(if ($totalBytes -gt 0) { ($_.Bytes / [double]$totalBytes) * 100.0 } else { 0.0 }) })),
    (New-ConsoleColumn -Group 'Sizes' -Name 'Average' -Values @($tableRows | ForEach-Object { Format-Integer $(if ($_.Files -gt 0) { [Math]::Round($_.Bytes / [double]$_.Files) } else { 0 }) }))
)

$overLimitColumns = New-FileConsoleColumns -Items $overLimitFiles
$warningColumns = New-FileConsoleColumns -Items $warningFiles
Sync-ConsoleColumnWidth -Tables @($overLimitColumns, $warningColumns)

if ($overLimitFiles.Count -gt 0) {
    Write-SectionTitle ("{0} ({1:N0})" -f $overLimitTitle, $overLimitFiles.Count)
    Write-FileConsoleTable -Columns $overLimitColumns
}

if ($warningFiles.Count -gt 0) {
    Write-SectionTitle ("{0} ({1:N0})" -f $warningTitle, $warningFiles.Count) -Color Yellow
    Write-FileConsoleTable -Columns $warningColumns
}

if ($wideHits.Count -gt 0) {
    Write-SectionTitle ("{0} ({1:N0})" -f $wideTitle, $wideHits.Count)
    Write-WideConsoleTable -Items @($wideHits)
}

if ($hotspotCommits -gt 0) {
    Write-SectionTitle $hotspotTitle
    if ($hotspotNote -ne '') {
        Write-AuditLine $hotspotNote
    }
    elseif ($hotspots.Count -gt 0) {
        $currentLines = @{}
        foreach ($item in $fileResults) { $currentLines[$item.RelativePath] = $item.Lines }
        Write-GroupedConsoleTable -Columns @(
            (New-ConsoleColumn -Name 'Added' -Values @($hotspots | ForEach-Object { Format-Integer $_.Added })),
            (New-ConsoleColumn -Name 'Deleted' -Values @($hotspots | ForEach-Object { Format-Integer $_.Deleted })),
            (New-ConsoleColumn -Name 'Net' -Values @($hotspots | ForEach-Object { Format-Integer ($_.Added - $_.Deleted) })),
            (New-ConsoleColumn -Name 'Now' -Values @($hotspots | ForEach-Object { if ($currentLines.ContainsKey($_.Path)) { Format-Integer $currentLines[$_.Path] } else { "-" } })),
            (New-ConsoleColumn -Name 'File' -Right $false -Values @($hotspots | ForEach-Object { $_.Path }))
        )
    }
}

if ($readErrors.Count -gt 0) {
    Write-SectionTitle ("Unreadable files ({0:N0})" -f $readErrors.Count)
    foreach ($readError in $readErrors) { Write-AuditLine $readError }
}

# Markdown output: repository summary, large files, folder totals, and extension totals.
$reportBuilder = [System.Text.StringBuilder]::new()
[void]$reportBuilder.AppendLine("# Source line report - $version")
[void]$reportBuilder.AppendLine()
[void]$reportBuilder.AppendLine("- Generated: $($generatedAt.ToString('yyyy-MM-dd HH:mm:ss zzz'))")
[void]$reportBuilder.AppendLine("- Source roots: $(ConvertTo-MarkdownCell ($sourceRootFulls -join '; '))")
[void]$reportBuilder.AppendLine("- Counted extensions: $(ConvertTo-MarkdownCell $extensionLabel)")
[void]$reportBuilder.AppendLine("- Excluded directories: $(ConvertTo-MarkdownCell (($excludedNames | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } }) -join ', '))")
[void]$reportBuilder.AppendLine("- Folder segments: $Segments")
[void]$reportBuilder.AppendLine()
[void]$reportBuilder.AppendLine("## Summary")
[void]$reportBuilder.AppendLine()
[void]$reportBuilder.AppendLine("| Metric | Value |")
[void]$reportBuilder.AppendLine("|--------|------:|")
[void]$reportBuilder.AppendLine("| Folders | $(Format-Integer $folderResults.Count) |")
[void]$reportBuilder.AppendLine("| Counted files | $(Format-Integer $totalFiles) |")
[void]$reportBuilder.AppendLine("| Physical lines | $(Format-Integer $totalLines) |")
[void]$reportBuilder.AppendLine("| Non-blank lines | $(Format-Integer $totalNonBlankLines) |")
[void]$reportBuilder.AppendLine("| Blank lines | $(Format-Integer $blankLines) |")
[void]$reportBuilder.AppendLine("| Total bytes | $(Format-Integer $totalBytes) |")
[void]$reportBuilder.AppendLine("| $overLimitTitle | $(Format-Integer $overLimitFiles.Count) |")
[void]$reportBuilder.AppendLine("| $warningTitle | $(Format-Integer $warningFiles.Count) |")
[void]$reportBuilder.AppendLine("| $wideTitle | $(Format-Integer $wideHits.Count) |")
[void]$reportBuilder.AppendLine("| $wideWarningTitle | $(Format-Integer $wideWarnings.Count) |")
[void]$reportBuilder.AppendLine("| Enforced | $(if ($enforced) { 'yes' } else { 'no' }) |")
[void]$reportBuilder.AppendLine("| Length above ceiling | $(Format-Integer $lengthAbove) |")
[void]$reportBuilder.AppendLine("| Width above ceiling | $(Format-Integer $widthAbove) |")
[void]$reportBuilder.AppendLine("| Stale ceilings | $(Format-Integer $staleCeilings.Count) |")
if ($null -ne $largestFolder) {
    [void]$reportBuilder.AppendLine("| Largest folder | $(ConvertTo-MarkdownCell $largestFolder.Name) ($(Format-Integer $largestFolder.Lines) lines) |")
}
[void]$reportBuilder.AppendLine()
[void]$reportBuilder.AppendLine("## $overLimitTitle")
[void]$reportBuilder.AppendLine()
Add-FileMarkdownTable -Builder $reportBuilder -Items $overLimitFiles
[void]$reportBuilder.AppendLine()
[void]$reportBuilder.AppendLine("## $warningTitle")
[void]$reportBuilder.AppendLine()
Add-FileMarkdownTable -Builder $reportBuilder -Items $warningFiles
[void]$reportBuilder.AppendLine()
[void]$reportBuilder.AppendLine("## $wideTitle ($widthLabel)")
[void]$reportBuilder.AppendLine()
Add-WideMarkdownTable -Builder $reportBuilder -Items @($wideHits)
[void]$reportBuilder.AppendLine()
[void]$reportBuilder.AppendLine("## $wideWarningTitle")
[void]$reportBuilder.AppendLine()
Add-WideMarkdownTable -Builder $reportBuilder -Items @($wideWarnings)
[void]$reportBuilder.AppendLine()
[void]$reportBuilder.AppendLine("## Ceilings")
[void]$reportBuilder.AppendLine()
[void]$reportBuilder.AppendLine("| Kind | Hits | Ceiling |")
[void]$reportBuilder.AppendLine("|------|-----:|--------:|")
foreach ($item in $ceilingRows) {
    [void]$reportBuilder.AppendLine("| $(ConvertTo-MarkdownCell $item.Kind) | $(Format-Integer $item.Hits) | $(Format-Integer $item.Ceiling) |")
}
[void]$reportBuilder.AppendLine()
if ($hotspotCommits -gt 0) {
    [void]$reportBuilder.AppendLine("## $hotspotTitle")
    [void]$reportBuilder.AppendLine()
    if ($hotspots.Count -eq 0) { [void]$reportBuilder.AppendLine("None.") }
    else {
        [void]$reportBuilder.AppendLine("| Added | Deleted | Net | File |")
        [void]$reportBuilder.AppendLine("|------:|--------:|----:|------|")
        foreach ($item in $hotspots) {
            [void]$reportBuilder.AppendLine("| $(Format-Integer $item.Added) | $(Format-Integer $item.Deleted) | $(Format-Integer ($item.Added - $item.Deleted)) | $(ConvertTo-MarkdownCell $item.Path) |")
        }
    }
    [void]$reportBuilder.AppendLine()
}
[void]$reportBuilder.AppendLine("## Lines by source folder")
[void]$reportBuilder.AppendLine()
[void]$reportBuilder.AppendLine("| Folder | Files | Lines | Non-blank | Share | Average lines | Bytes | Size share | Average bytes |")
[void]$reportBuilder.AppendLine("|--------|------:|------:|----------:|------:|--------------:|------:|-----------:|--------------:|")
foreach ($item in $folderResults) {
    $share = if ($totalLines -gt 0) { ($item.Lines / [double]$totalLines) * 100.0 } else { 0.0 }
    $sizeShare = if ($totalBytes -gt 0) { ($item.Bytes / [double]$totalBytes) * 100.0 } else { 0.0 }
    $averageSize = if ($item.Files -gt 0) { $item.Bytes / [double]$item.Files } else { 0.0 }
    $averageLines = if ($item.Files -gt 0) { $item.Lines / [double]$item.Files } else { 0.0 }
    [void]$reportBuilder.AppendLine("| $(ConvertTo-MarkdownCell $item.Name) | $(Format-Integer $item.Files) | $(Format-Integer $item.Lines) | $(Format-Integer $item.NonBlankLines) | $(Format-Percent $share) | $([Math]::Round($averageLines).ToString('N0', [System.Globalization.CultureInfo]::InvariantCulture)) | $(Format-Integer $item.Bytes) | $(Format-Percent $sizeShare) | $([Math]::Round($averageSize).ToString('N0', [System.Globalization.CultureInfo]::InvariantCulture)) |")
}
[void]$reportBuilder.AppendLine()
[void]$reportBuilder.AppendLine("## Lines by extension")
[void]$reportBuilder.AppendLine()
[void]$reportBuilder.AppendLine("| Extension | Files | Lines | Non-blank | Blank | Share |")
[void]$reportBuilder.AppendLine("|-----------|------:|------:|----------:|------:|------:|")
$sortedExtensionTotals = @(
    $extensionTotals.Values |
        Sort-Object -Property @{ Expression = 'Lines'; Descending = $true }, @{ Expression = { Get-OrdinalKey $_.Extension } }
)
foreach ($item in $sortedExtensionTotals) {
    $share = if ($totalLines -gt 0) { ($item.Lines / [double]$totalLines) * 100.0 } else { 0.0 }
    [void]$reportBuilder.AppendLine("| $(ConvertTo-MarkdownCell $item.Extension) | $(Format-Integer $item.Files) | $(Format-Integer $item.Lines) | $(Format-Integer $item.NonBlankLines) | $(Format-Integer ($item.Lines - $item.NonBlankLines)) | $(Format-Percent $share) |")
}

if ($readErrors.Count -gt 0) {
    [void]$reportBuilder.AppendLine()
    [void]$reportBuilder.AppendLine("## Unreadable files")
    [void]$reportBuilder.AppendLine()
    foreach ($readError in $readErrors) {
        [void]$reportBuilder.AppendLine("- $(ConvertTo-MarkdownCell $readError)")
    }
}

[System.IO.File]::WriteAllText($outputPathFull, ($reportBuilder.ToString() -replace "`r`n", "`n"), [System.Text.UTF8Encoding]::new($false))

# Page output: the full result as data inside the template AuditLines.html.
# The JSON is written by hand, so Windows PowerShell 5.1 and pwsh 7 produce the same bytes.
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
    if ($Value -is [System.Collections.IDictionary]) {
        $pairs = @(foreach ($key in $Value.Keys) { (ConvertTo-PageJson ([string]$key)) + ':' + (ConvertTo-PageJson $Value[$key]) })
        return '{' + ($pairs -join ',') + '}'
    }
    if ($Value -is [System.Collections.IEnumerable]) {
        $items = @(foreach ($item in $Value) { ConvertTo-PageJson $item })
        return '[' + ($items -join ",`n") + ']'
    }
    throw "The page data holds a value of an unexpected type: $($Value.GetType().FullName)"
}

# The newest history record file other than this version, with its folders renamed to this run's folder keys.
# Each record file is named {version}.json; only the chosen one is read.
function Read-PageHistory {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not [System.IO.Directory]::Exists($Path)) { return $null }
    $newestPath = $null
    $newestVersion = $null
    foreach ($candidate in [System.IO.Directory]::GetFiles($Path, '*.json')) {
        if (-not $candidate.EndsWith('.json', [System.StringComparison]::OrdinalIgnoreCase)) { continue }
        $name = [System.IO.Path]::GetFileNameWithoutExtension($candidate)
        $candidateVersion = $null
        if ($name -eq $version -or -not [version]::TryParse($name, [ref]$candidateVersion)) { continue }
        if ($null -eq $newestVersion -or $candidateVersion -gt $newestVersion) {
            $newestPath = $candidate
            $newestVersion = $candidateVersion
        }
    }
    if ($null -eq $newestPath) { return $null }
    try {
        $record = Get-Content -LiteralPath $newestPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        return $null
    }

    $folders = [ordered]@{}
    foreach ($folder in @($record.folders)) {
        $name = [string]$folder.name
        $root = [string]$folder.root
        $key = if ($name -eq $root) { $name } else { Get-FolderKey -Root $root -Relative ($name + '/file') }
        if (-not $folders.Contains($key)) {
            $folders[$key] = [ordered]@{ name = $key; files = [long]0; lines = [long]0; nonBlank = [long]0; bytes = [long]0 }
        }
        $folders[$key].files += [long]$folder.files
        $folders[$key].lines += [long]$folder.lines
        $folders[$key].nonBlank += [long]$folder.nonBlank
        $folders[$key].bytes += [long]$folder.bytes
    }

    return [ordered]@{ version = [string]$record.version; date = [string]$record.date; folders = @($folders.Values) }
}

function ConvertTo-PageLink {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([string]::Equals([System.IO.Path]::GetDirectoryName($Path), $reportDirectoryFull.TrimEnd('\', '/'), [System.StringComparison]::OrdinalIgnoreCase)) {
        return [System.IO.Path]::GetFileName($Path)
    }
    return [System.Uri]::new($Path).AbsoluteUri
}

$pageTemplatePath = Join-Path $PSScriptRoot 'AuditLines.html'
$pagePathFull = Join-Path $reportDirectoryFull ("{0}{1}.html" -f $reportPrefix, $version)
$historyPrefix = [string]$config.history.pagePrefix
$historyPage = @([System.IO.Directory]::GetFiles($reportDirectoryFull, $historyPrefix + '*.html') |
    Sort-Object -Property @{ Expression = { [System.IO.File]::GetLastWriteTimeUtc($_) }; Descending = $true } | Select-Object -First 1)

$pageData = [ordered]@{
    project = [string]$config.project
    version = $version
    generation = $script:AuditGeneration
    generated = $generatedAt.ToString('yyyy-MM-dd HH:mm:ss zzz')
    enforced = $enforced
    scope = [ordered]@{
        roots = @($rootEntries | ForEach-Object { $(if ($_.Prefix.Length -eq 0) { '.' } else { $_.Prefix.TrimEnd('/') }) })
        extensions = @($normalizedExtensions | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } })
        excluded = @($excludedNames | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } })
        segments = $Segments
        widths = @($widthLimits.Keys | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } } | ForEach-Object { [ordered]@{ extension = $_; limit = $widthLimits[$_] } })
        tallyRoots = @($tallyNames | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } })
        tallyExtensions = @($tallyExtensions)
    }
    thresholds = [ordered]@{ limit = $LimitThreshold; warning = $WarningThreshold; band = $widthBand }
    gates = @($gateRows | ForEach-Object { [ordered]@{ gate = $_.Gate; count = [long]$_.Count; meaning = $_.Meaning; section = $_.Section } })
    ceilings = @($ceilingRows | ForEach-Object { [ordered]@{ kind = $_.Kind; hits = $_.Hits; ceiling = $_.Ceiling } })
    folders = @($folderResults | ForEach-Object { [ordered]@{ name = $_.Name; tally = $tallyNames.Contains($_.Name); files = $_.Files; lines = $_.Lines; nonBlank = $_.NonBlankLines; bytes = $_.Bytes } })
    extensions = @($sortedExtensionTotals | ForEach-Object { [ordered]@{ extension = $_.Extension; files = $_.Files; lines = $_.Lines; nonBlank = $_.NonBlankLines } })
    files = @($fileResults | ForEach-Object { [ordered]@{ path = $_.RelativePath; folder = $_.Folder; extension = $_.Extension; lines = $_.Lines; nonBlank = $_.NonBlankLines; bytes = $_.Bytes } })
    wide = @(@($wideHits) + @($wideWarnings) | ForEach-Object { [ordered]@{ location = $_.Location; width = $_.Width; limit = $_.Limit; text = $_.Text } })
    hotspots = [ordered]@{
        commits = $hotspotCommits
        note = $hotspotNote
        rows = @($hotspots | ForEach-Object { [ordered]@{ path = $_.Path; added = $_.Added; deleted = $_.Deleted } })
    }
    unreadable = @($readErrors)
    previous = Read-PageHistory -Path (Join-AuditPath -Root $repoRootFull -Relative ([string]$config.history.records))
    history = $(if ($historyPage.Count -gt 0) { ConvertTo-PageLink -Path $historyPage[0] } else { '' })
    reports = @(
        [ordered]@{ name = [System.IO.Path]::GetFileName($outputPathFull); link = ConvertTo-PageLink -Path $outputPathFull }
    )
}

$utf8 = [System.Text.UTF8Encoding]::new($false)
$pageTemplate = [System.IO.File]::ReadAllText($pageTemplatePath, $utf8)
foreach ($marker in @('/*__DATA__*/', '__TITLE__')) {
    if (-not $pageTemplate.Contains($marker)) { throw "The page template lacks the marker $marker : $pageTemplatePath" }
}
$pageText = $pageTemplate.Replace('__TITLE__', [System.Net.WebUtility]::HtmlEncode([string]$config.project)).Replace('/*__DATA__*/', (ConvertTo-PageJson $pageData))
[System.IO.File]::WriteAllText($pagePathFull, ($pageText -replace "`r`n", "`n"), $utf8)

Write-AuditLine ""
Write-AuditLine "Report: $outputPathFull"
Write-AuditLine "Report: $pagePathFull"

if ($Open) {
    Start-Process -FilePath $outputPathFull
}

if (-not $NoOpen) {
    Start-Process -FilePath $pagePathFull
}

if (($lengthAbove + $widthAbove + $staleCeilings.Count + $readErrors.Count) -gt 0) {
    exit 1
}

exit 0
