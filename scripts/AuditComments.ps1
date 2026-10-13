<#
.SYNOPSIS
Audits the folder comment files that accompany source folders and the sources themselves for stray comments.

.DESCRIPTION
Every folder holding a source carries exactly one comment file, the folder sidecar, named
{folder name}{comment suffix} inside that folder. It carries the intent of the folder's code,
which no check judges. The repository root is the one exception: no file directly inside it
is audited, and it needs no sidecar.

Reads the project configuration from AuditComments.json next to this script, then
performs these actions on every run:
  1. Prints the Result table, one gate per finding kind below.
  2. Prints comment-file line totals for each folder under the source roots.
  3. Prints folders that have no sidecar, and comment files that are no folder's sidecar.
  4. Prints comment lines that break the line rules: too many words, a forbidden
     character, or more than one sentence.
  5. Prints in-code comment lines found inside sources, every line of a block comment included.
  6. Prints signature headings that name no identifier of the folder's sources, and file
     headings that name no file of the folder.
  7. Prints sidecars whose stamp no longer matches their folder, as advice, and those with no stamp.
  8. Prints folders over the file limit and folder ceilings set above their count, as warnings.
  9. Prints the exempt comment files, which the line rules, headings and stamps skip.
 10. Writes a Markdown report to {report.directory}\{prefix}{version}.md.
 11. Writes the full result as a page to {report.directory}\{prefix}{version}.html.
     The page opens in the default browser unless -NoOpen is given.
The console follows scripts\report.md: widest view first, empty lists left out.
The page holds everything the console and the report hold, plus every comment file read.
It fills the template AuditComments.html next to this script, and works offline.

A sidecar ends with its stamp, a section that StampComment.ps1 writes:
  ## Stamp

  Commit: `<commit the stamp was taken at>`
  - `<file name>` `<16 hex digits>`
One row per source of the folder. The digits are the first 16 of the lowercase SHA-256 of the
UTF-8 text of that source, its byte order mark dropped and CRLF or CR turned into LF. The stamp
section is reserved: the line rules and the heading check stop at it.
A row whose source changed, a source with no row, and a row whose source is gone are advice:
the sidecar is listed with the diff command that shows what changed since its stamp, and the
gate only warns. A sidecar with no stamp section warns while the count sits at or below
ceilings.unstamped and fails above it. A ceiling above the count is stale and fails.

While enforced is false, every finding gate warns instead of failing: folders without a sidecar,
comment files without a folder, line rules, in-code comments, headings and unstamped sidecars.
Stale ceilings and unreadable files fail either way. A project sets it false while it migrates.

A folder holding more sources than rules.folderFiles warns, unless its count sits at or below
its row in ceilings.folders. A row above its folder's count is stale, and warns.

Exempt comment files, listed by path under exempt.comments, belong to the developer alone. Only
the developer edits them, so the audit never judges them: no line rule, heading or stamp check
reads them, and StampComment.ps1 refuses them. They count in the folder totals and print under
Exempt comment files. A listed path that does not exist stops the audit with an error.

Everything project-specific lives in AuditComments.json. The script itself
carries no project knowledge. Files come from git: tracked and untracked files,
never ignored ones, as in the convention tests. Git is the only external tool required.
Every path prints with forward slashes, and every list keeps the order of the
convention tests: paths under the roots sorted without case.
A configured root that does not exist stops the audit with an error.
An unreadable file is reported once, under Unreadable files, and never as a missing pair.

AuditComments.json shape:
  {
    "generation": 25,
    "project": "Cadroue",
    "enforced": true,
    "sources": {
      "roots": ["localization", "src", "tests"],
      "commentPattern": "*.comment.md",
      "suffixes": [".cs", ".xaml", ".csproj", ".json", ".props", ".slnx"],
      "excludeSegments": [".git", "bin", "obj"],
      "excludeSuffixes": [".g.cs", ".Designer.cs"]
    },
    "rules": { "maxWords": 20, "forbidden": [";"], "sentenceMarks": [".", "!", "?"],
               "abbreviations": ["e.g", "i.e", "etc", "vs", "cf"], "folderFiles": 10 },
    "remark": {
      "markers": { ".cs": ["//", "/*"], ".xaml": ["<!--"], ".props": ["<!--"],
                   ".csproj": ["<!--"], ".slnx": ["<!--"] },
      "closers": { "/*": "*/", "<!--": "-->" },
      "exemptFiles": ["TAuditNameRegistry.cs", "TAuditVerbRegistry.cs"]
    },
    "exempt": { "comments": ["<comment file path>"] },
    "ceilings": { "unstamped": <n>, "folders": { "<folder path>": <n> } },
    "report": {
      "directory": "docs-analysis",
      "versionFile": "version.json",
      "versionKey": "current-version",
      "prefix": "AuditComments-",
      "segments": 1
    }
  }

Suffixes name the files that count as sources. A file under a root ending in one of them is a
source of its folder, the excluded suffixes aside. Folder ceilings are keyed by the folder's path
from the repository root, with forward slashes.
Markers are looked for in every file whose extension declares them, source or not,
except the exempt files and the excluded suffixes. Headings are checked in every
comment file the line rules read.

Report.prefix starts the file name of both the Markdown report and the page.

.PARAMETER ConfigPath
Path to the JSON configuration. Defaults to AuditComments.json next to this script.

.PARAMETER SourceRoots
Overrides sources.roots for this run.

.PARAMETER Segments
Overrides report.segments: how many path segments under a root form a folder row.

.PARAMETER MaxWords
Overrides rules.maxWords for this run.

.PARAMETER OutputPath
Overrides the Markdown report path for this run.

.PARAMETER Open
Open the generated Markdown report after the audit finishes.

.PARAMETER NoOpen
Write the page without opening it.

.PARAMETER NoPause
Do not stop at each console page. Paging is also off when output or input is redirected.

.PARAMETER Help
Display this help and exit without running the audit. The alias -? is supported.

.EXAMPLE
AuditComments

.EXAMPLE
AuditComments -Segments 2

.EXAMPLE
AuditComments -SourceRoots .\src -MaxWords 25
#>
#requires -Version 5.1
# AUDITCOMMENTS - AUDIT GENERATION 25.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the audited outcome changes.
# A generation names the set of checks the audit applies. Two projects on the same generation audit
# the same things and their reports compare directly, whatever else differs between the files. A check
# added, removed, or changed in what it reports is a new generation; wording, plumbing, and refactoring
# leave it alone. Every audit script shares one generation number with the convention-test settings,
# and each refuses a configuration written at another generation.
# Generation 11: nothing the comment audit reports changes; the number rises with the truth audit,
# which checks that a deportment field reaches no request, keeps one writer, holds no logic and
# treats no engine data.
# Generation 12: nothing the comment audit reports changes; the number rises with the convention tests,
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
# Generation 19: every paired comment file carries a hash of its sources on its second line. A hash
# its sources no longer match is a finding, and a missing hash is a warning under a falling ceiling.
# Exempt comment files belong to the developer alone, and no line rule, heading or hash check reads them.
# Generation 20: nothing this audit reports changes; the number rises with the UI audit, whose
# Mismatching kind only informs while a driver folder it waits on holds no source.
# Generation 21: nothing this audit reports changes; the number rises with the structure, name and
# UI audits, which gained kinds, and with the new fault audit.
# Generation 22: nothing this audit reports changes; the number rises with the structure audit,
# whose Unsealing kind walks every public type of a sealed ring, whatever its prefix.
# Generation 24: one sidecar per folder replaces one per source file. A stamp section records each
# source's hash and the commit, and a changed folder is advice, never a failure. A folder over the
# file limit warns above its own ceiling. The restamped-unrevised warning and its snapshot are gone.
# Generation 25: the repository root is exempt. No file directly inside it is audited, and the
# root-level files and root sidecar settings are gone.
[CmdletBinding()]
param(
    [string]$ConfigPath,
    [string[]]$SourceRoots,
    [ValidateRange(1, 8)]
    [int]$Segments,
    [ValidateRange(1, [int]::MaxValue)]
    [int]$MaxWords,
    [string]$OutputPath,
    [switch]$Open,
    [switch]$NoOpen,
    [switch]$NoPause,
    [Alias('?')]
    [switch]$Help
)

if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    $ConfigPath = Join-Path $PSScriptRoot "AuditComments.json"
}

if ($Help) {
    @'
NAME
    AuditComments.ps1

SYNOPSIS
    Audit comment files and stray in-code comments, and write a Markdown report and a page.

SYNTAX
    AuditComments [-ConfigPath <path>] [-SourceRoots <path[]>] [-Segments <number>]
        [-MaxWords <number>] [-OutputPath <path>] [-Open] [-NoOpen] [-NoPause] [-Help]

CONFIGURATION
    All project-specific values live in AuditComments.json next to the script:
    source roots, comment-file pattern, root sidecar, source suffixes, excluded
    directory names and suffixes, line rules, in-code comment markers and
    exempt files, exempt comment files, report directory, version file and key, and the report
    file-name prefix. Parameters below override it per run.

PAGE
    Every run writes the full result as a page next to the Markdown report.
    It opens in the default browser unless -NoOpen is given.

CHECKS
    Sidecars: every folder holding a source needs its sidecar,
        {folder}.comment.md. Every other comment file under the roots is a
        finding. Files directly inside the repository root are never audited.
    Line rules: every comment line holds one sentence, at most the word
        limit, and none of the forbidden characters. The stamp section is
        not read.
    In-code comments: every file whose extension declares markers, source
        or not, carries none, exempt files and excluded suffixes aside.
    Headings: every signature heading names an identifier of its folder's
        sources, and every file heading names a file of the folder.
    Stamps: a sidecar ends with ## Stamp, the commit, and one hash row per
        source. A changed, added or removed source is a WARN that lists the
        diff to reread. A missing stamp is a WARN while the count sits at or
        below ceilings.unstamped, a finding above it, and a ceiling above
        the count is stale and a finding. Stamp a sidecar with
        StampComment.ps1.
    Folder size: a folder with more sources than rules.folderFiles is a
        WARN unless ceilings.folders holds it. A row above its folder's
        count is a WARN.
    Exempt: the comment files under exempt.comments belong to the developer
        alone. Line rules, headings and stamps skip them, and they are
        listed under Exempt comment files.
    A configured root that does not exist, or a failing
    git, stops the audit with an error. Paths print with forward slashes
    in the order of the convention tests.

OPTIONS
    -ConfigPath <path>
        JSON configuration file. Defaults to .\AuditComments.json.

    -SourceRoots <path[]>
        Source directories to audit. Overrides sources.roots.

    -Segments <number>
        Path segments under a root that form a folder row. Overrides
        report.segments. 1 lists projects, 2 lists their first-level folders.

    -MaxWords <number>
        Word limit per comment line. Overrides rules.maxWords.

    -OutputPath <path>
        Markdown report path. By default, the project version is used to
        create a path under report.directory.

    -Open
        Open the generated Markdown report after the audit finishes.

    -NoOpen
        Write the page without opening it.

    -NoPause
        Do not stop at each console page for a key. Off by itself when output
        or input is redirected.

    -Help, -?
        Display this help and exit without running the audit.

EXAMPLES
    AuditComments
        Audit with the configured settings.

    AuditComments -Segments 2
        Break folder totals down one level further.

    AuditComments -SourceRoots .\src -MaxWords 25
        Audit only src, allowing 25 words per line.
'@ | Write-Host
    exit 0
}

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Git writes UTF-8, so this process reads and writes UTF-8 and a non-ASCII path decodes alike on 5.1 and 7.
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding

$script:AuditGeneration = 25

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

Write-AuditLine "AUDITCOMMENTS - AUDIT GENERATION $script:AuditGeneration" -ForegroundColor Blue


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
        if ($property.Value -is [System.Management.Automation.PSCustomObject] -and $child -notin @('ceilings.folders', 'remark.markers', 'remark.closers')) {
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
        'bool' { return $Value -is [bool] }
        'strings' {
            if ($null -eq $Value -or -not ($Value -is [System.Array])) { return $false }
            foreach ($item in $Value) {
                if (-not ($item -is [string]) -or [string]::IsNullOrWhiteSpace($item)) { return $false }
            }
            return $true
        }
        'map' {
            if ($null -eq $Value -or -not ($Value -is [System.Management.Automation.PSCustomObject])) { return $false }
            foreach ($property in $Value.PSObject.Properties) {
                if (-not ($property.Value -is [string]) -or [string]::IsNullOrWhiteSpace($property.Value)) { return $false }
            }
            return $true
        }
        'mapStrings' {
            if ($null -eq $Value -or -not ($Value -is [System.Management.Automation.PSCustomObject])) { return $false }
            foreach ($property in $Value.PSObject.Properties) {
                if (-not (Test-ConfigValue -Value $property.Value -Kind 'strings')) { return $false }
            }
            return $true
        }
        'mapInts' {
            if ($null -eq $Value -or -not ($Value -is [System.Management.Automation.PSCustomObject])) { return $false }
            foreach ($property in $Value.PSObject.Properties) {
                if (-not (Test-ConfigValue -Value $property.Value -Kind 'int')) { return $false }
            }
            return $true
        }
        default { throw "Unknown schema kind: $Kind" }
    }
}

function Resolve-UserPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    return $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
}

function Read-AuditConfig {
    param([Parameter(Mandatory = $true)][string]$Path)

    $pathFull = Resolve-UserPath -Path $Path
    if (-not (Test-Path -LiteralPath $pathFull -PathType Leaf)) {
        throw "The comment-audit configuration was not found: $pathFull"
    }

    try {
        $config = Get-Content -LiteralPath $pathFull -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The comment-audit configuration is not valid JSON: $pathFull`n$($_.Exception.Message)"
    }

    $problems = [System.Collections.Generic.List[string]]::new()
    $schema = [ordered]@{
        'generation' = 'int'; 'project' = 'string'; 'enforced' = 'bool'
        'sources.roots' = 'strings'; 'sources.commentPattern' = 'string'; 'sources.suffixes' = 'strings'
        'sources.excludeSegments' = 'strings'; 'sources.excludeSuffixes' = 'strings'
        'rules.maxWords' = 'int'; 'rules.forbidden' = 'strings'; 'rules.sentenceMarks' = 'strings'; 'rules.abbreviations' = 'strings'; 'rules.folderFiles' = 'int'
        'remark.markers' = 'mapStrings'; 'remark.closers' = 'map'; 'remark.exemptFiles' = 'strings'
        'exempt.comments' = 'strings'
        'ceilings.unstamped' = 'int'; 'ceilings.folders' = 'mapInts'
        'report.directory' = 'string'; 'report.versionFile' = 'string'; 'report.versionKey' = 'string'; 'report.prefix' = 'string'; 'report.segments' = 'int'
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

    if ($problems.Count -gt 0) {
        throw "The comment-audit configuration is not valid: $pathFull`n  " + ($problems -join "`n  ")
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
    throw "The comment-audit configuration is generation $($config.generation) but this tooling is generation $script:AuditGeneration.`nA generation names the set of checks applied, so the two must match: $ConfigPath"
}

if (-not $PSBoundParameters.ContainsKey('SourceRoots')) { $SourceRoots = @($config.sources.roots) }
if (-not $PSBoundParameters.ContainsKey('Segments')) { $Segments = [int]$config.report.segments }
if (-not $PSBoundParameters.ContainsKey('MaxWords')) { $MaxWords = [int]$config.rules.maxWords }

$commentPattern = [string]$config.sources.commentPattern
$commentSuffix = $commentPattern.TrimStart('*')
$forbidden = @($config.rules.forbidden | ForEach-Object { [string]$_ })
$sentenceMarks = @($config.rules.sentenceMarks | ForEach-Object { [string]$_ })
$exemptFiles = [System.Collections.Generic.HashSet[string]]::new([string[]]@($config.remark.exemptFiles | ForEach-Object { [string]$_ }), [System.StringComparer]::OrdinalIgnoreCase)
$reportDirectoryFull = Join-AuditPath -Root $repoRootFull -Relative $config.report.directory
$versionPathFull = Join-AuditPath -Root $repoRootFull -Relative $config.report.versionFile
$versionKey = [string]$config.report.versionKey
$reportPrefix = [string]$config.report.prefix

$folderFileLimit = [int]$config.rules.folderFiles
$pairs = [System.Collections.Generic.List[object]]::new()
foreach ($suffix in @($config.sources.suffixes)) {
    $pairs.Add([pscustomobject]@{ Suffix = [string]$suffix })
}
$pairs = @($pairs | Sort-Object -Property @{ Expression = { $_.Suffix.Length }; Descending = $true })
$folderCeilings = [ordered]@{}
foreach ($property in $config.ceilings.folders.PSObject.Properties) {
    $folderCeilings[[string]$property.Name] = [int]$property.Value
}

$markers = @{}
foreach ($property in $config.remark.markers.PSObject.Properties) {
    $markers[[string]$property.Name] = @($property.Value | ForEach-Object { [string]$_ })
}

$excludedNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($segment in @($config.sources.excludeSegments)) {
    if (-not [string]::IsNullOrWhiteSpace($segment)) { [void]$excludedNames.Add([string]$segment) }
}
$excludedSuffixes = @($config.sources.excludeSuffixes | ForEach-Object { [string]$_ })

if ($MaxWords -lt 1) {
    throw "rules.maxWords ($MaxWords) must be at least 1."
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
        [System.ConsoleColor]$Color = [System.ConsoleColor]::Blue
    )

    Write-AuditLine ""
    Write-AuditLine $Text -ForegroundColor $Color
    Write-AuditLine ('-' * $Text.Length) -ForegroundColor DarkGray
}

function Write-ResultTable {
    param([Parameter(Mandatory = $true)][object[]]$Rows)

    Write-SectionTitle 'Result'
    $statusWidth = 6
    $countWidth = [Math]::Max(5, ($Rows | ForEach-Object { (Format-Integer $_.Count).Length } | Measure-Object -Maximum).Maximum)
    $gateWidth = [Math]::Max(4, ($Rows | ForEach-Object { $_.Gate.Length } | Measure-Object -Maximum).Maximum)
    $meaningWidth = [Math]::Max(7, ($Rows | ForEach-Object { $_.Meaning.Length } | Measure-Object -Maximum).Maximum)
    Write-AuditLine ('{0}  {1}  {2}  Meaning' -f 'Status'.PadRight($statusWidth), 'Count'.PadLeft($countWidth), 'Gate'.PadRight($gateWidth)) -ForegroundColor Cyan
    Write-AuditLine (@(('-' * $statusWidth), ('-' * $countWidth), ('-' * $gateWidth), ('-' * $meaningWidth)) -join '  ') -ForegroundColor Cyan
    foreach ($row in $Rows) {
        $status = Get-GateStatus -Row $row
        $text = '{0}  {1}  {2}  {3}' -f $status.PadRight($statusWidth), (Format-Integer $row.Count).PadLeft($countWidth), $row.Gate.PadRight($gateWidth), $row.Meaning
        Write-AuditLine $text -Lead $status -LeadColor $(switch ($status) { 'FAIL' { 'Red' } 'WARN' { 'Yellow' } default { 'Green' } })
    }

    $failed = @($Rows | Where-Object { (Get-GateStatus -Row $_) -eq 'FAIL' })
    $warned = @($Rows | Where-Object { (Get-GateStatus -Row $_) -eq 'WARN' })
    Write-AuditLine ''
    if ($failed.Count -eq 0 -and $warned.Count -eq 0) {
        Write-AuditLine ('PASS: all {0} gates at 0.' -f $Rows.Count) -ForegroundColor Green
    }
    elseif ($failed.Count -eq 0) {
        $sections = ($warned | ForEach-Object { '"' + $_.Section + '"' }) -join ', '
        Write-AuditLine ('PASS: no gate fails, {0} of {1} gates warn. See {2}.' -f $warned.Count, $Rows.Count, $sections) -ForegroundColor Yellow
    }
    else {
        $sections = ($failed | ForEach-Object { '"' + $_.Section + '"' }) -join ', '
        Write-AuditLine ('FAIL: {0} of {1} gates above 0. See {2}.' -f $failed.Count, $Rows.Count, $sections) -ForegroundColor Red
    }
}

# A gate with a Ceiling warns while its count sits at or below it, and fails above it.
function Get-GateStatus {
    param([Parameter(Mandatory = $true)]$Row)

    if ($Row.Count -eq 0) { return 'OK' }
    if ($null -ne $Row.PSObject.Properties['Warn'] -and $Row.Warn) { return 'WARN' }
    if ($null -ne $Row.PSObject.Properties['Ceiling'] -and $Row.Count -le $Row.Ceiling) { return 'WARN' }
    return 'FAIL'
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

function Get-PairFor {
    param([Parameter(Mandatory = $true)][string]$Name)

    foreach ($pair in $pairs) {
        if ($Name.EndsWith($pair.Suffix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return [pscustomobject]@{ Suffix = $pair.Suffix }
        }
    }

    return $null
}

# The sidecar a folder expects: its own name plus the comment suffix.
function Get-SidecarName {
    param([Parameter(Mandatory = $true)][string]$Directory)

    return [System.IO.Path]::GetFileName($Directory.TrimEnd('\', '/')) + $commentSuffix
}

$stringLiteralPattern = [System.Text.RegularExpressions.Regex]::new('(?:"{3,})[\s\S]*?"{3,}|(?:@\$?|\$@)"(?:[^"]|"")*"|"(?:\\.|[^"\\\r\n])*"|''(?:\\.|[^''\\\r\n])''', [System.Text.RegularExpressions.RegexOptions]::Compiled)
$codeSpanPattern = [System.Text.RegularExpressions.Regex]::new('`[^`]*`', [System.Text.RegularExpressions.RegexOptions]::Compiled)
$abbreviationAlternatives = (@($config.rules.abbreviations | ForEach-Object { [System.Text.RegularExpressions.Regex]::Escape([string]$_) }) -join '|')
$abbreviationPattern = [System.Text.RegularExpressions.Regex]::new('(?<![\p{L}.])(?:' + $abbreviationAlternatives + ')\.', [System.Text.RegularExpressions.RegexOptions]::Compiled -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
$sentencePattern = [System.Text.RegularExpressions.Regex]::new('[' + [System.Text.RegularExpressions.Regex]::Escape(-join $sentenceMarks) + ']\s+\p{L}', [System.Text.RegularExpressions.RegexOptions]::Compiled)

function Remove-StringLiteral {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text)

    return $stringLiteralPattern.Replace($Text, [System.Text.RegularExpressions.MatchEvaluator]{
        param($match)
        $breaks = @($match.Value.ToCharArray() | Where-Object { $_ -eq "`n" }).Count
        return '""' + ("`n" * $breaks)
    })
}

function Test-CommentLine {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Line)

    $text = $Line.Trim()
    $heading = $text -match '^#+\s*'
    if ($heading) { $text = $text.Substring($Matches[0].Length) }
    if ($text.Length -eq 0) { return @() }
    if ($text -match '^[-*>]\s+') { $text = $text.Substring($Matches[0].Length) }

    $prose = $codeSpanPattern.Replace($text, '')
    $counted = if ($heading) { $prose } else { $text }
    $problems = [System.Collections.Generic.List[string]]::new()
    $words = @(@($counted -split '\s+') | Where-Object { $_.Length -gt 0 }).Count
    if ($words -gt $MaxWords) { $problems.Add("$words words") }

    foreach ($token in $forbidden) {
        if ($prose.Contains($token)) { $problems.Add("forbidden '$token'") }
    }

    $sentences = 1 + $sentencePattern.Matches($abbreviationPattern.Replace($prose, 'abbr')).Count
    if ($sentences -gt 1) { $problems.Add("$sentences sentences") }

    return @($problems)
}

# JSON written by hand, so Windows PowerShell 5.1 and pwsh 7 produce the same bytes.
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
    if ([string]::IsNullOrWhiteSpace($root)) { continue }
    $rootFull = Join-AuditPath -Root $repoRootFull -Relative $root
    if (-not [System.IO.Directory]::Exists($rootFull)) { throw "The configured source root has no directory: $rootFull" }
    $rootRelative = (Get-RelativePathSafe -BasePath $repoRootFull -Path $rootFull).Trim('/')
    [void]$sourceRootFulls.Add($rootFull)
    $rootEntries.Add([pscustomobject]@{ Full = $rootFull; Prefix = $(if ($rootRelative.Length -eq 0) { '' } else { $rootRelative + '/' }) })
}

if ($sourceRootFulls.Count -eq 0) { throw "At least one source root must be supplied." }

# Comment files only the developer edits, so no check judges them.
$exemptComments = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($entry in @($config.exempt.comments)) {
    if ([string]::IsNullOrWhiteSpace($entry)) { continue }
    $exemptFull = Join-AuditPath -Root $repoRootFull -Relative $entry
    if (-not [System.IO.File]::Exists($exemptFull)) { throw "The configured exempt comment file does not exist: $exemptFull" }
    [void]$exemptComments.Add($exemptFull)
}

$sourceFiles = [System.Collections.Generic.List[object]]::new()
$commentFiles = [System.Collections.Generic.List[object]]::new()
$remarkFiles = [System.Collections.Generic.List[object]]::new()
$readErrors = [System.Collections.Generic.List[string]]::new()
$unreadable = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

function Measure-Lines {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Relative
    )

    [long]$lines = 0
    [long]$nonBlank = 0
    try {
        foreach ($line in [System.IO.File]::ReadLines($Path)) {
            $lines++
            if (-not [string]::IsNullOrWhiteSpace($line)) { $nonBlank++ }
        }
    }
    catch {
        if ($unreadable.Add($Path)) { $readErrors.Add("${Relative}: $($_.Exception.Message)") }
        return [pscustomobject]@{ Readable = $false; Lines = [long]0; NonBlank = [long]0 }
    }

    return [pscustomobject]@{ Readable = $true; Lines = $lines; NonBlank = $nonBlank }
}

function Add-CommentFile {
    param(
        [Parameter(Mandatory = $true)][System.IO.FileInfo]$File,
        [Parameter(Mandatory = $true)][string]$Relative,
        [Parameter(Mandatory = $true)][string]$Folder
    )

    $count = Measure-Lines -Path $File.FullName -Relative $Relative
    $commentFiles.Add([pscustomobject]@{
        Full = $File.FullName; Relative = $Relative; Folder = $Folder; Name = $File.Name; Directory = $File.DirectoryName
        Readable = $count.Readable; Lines = $count.Lines; NonBlank = $count.NonBlank; Bytes = $File.Length
    })
}

function Add-SourceFile {
    param(
        [Parameter(Mandatory = $true)][System.IO.FileInfo]$File,
        [Parameter(Mandatory = $true)][string]$Relative,
        [Parameter(Mandatory = $true)][string]$Folder
    )

    if (-not (Test-IsExcludedSuffix -Name $File.Name)) {
        $pair = Get-PairFor -Name $File.Name
        if ($null -ne $pair) {
            $count = Measure-Lines -Path $File.FullName -Relative $Relative
            $sourceFiles.Add([pscustomobject]@{
                Full = $File.FullName; Relative = $Relative; Folder = $Folder; Name = $File.Name; Directory = $File.DirectoryName
                Suffix = $pair.Suffix; Expected = (Get-SidecarName -Directory $File.DirectoryName); Lines = $count.Lines; NonBlank = $count.NonBlank
            })
        }
    }

    $extension = $File.Extension.ToLowerInvariant()
    if ($markers.ContainsKey($extension) -and -not (Test-IsExcludedSuffix -Name $File.Name) -and -not $exemptFiles.Contains($File.Name)) {
        $remarkFiles.Add([pscustomobject]@{ Full = $File.FullName; Relative = $Relative; Extension = $extension })
    }
}

# One listing from the repository root, filtered by root, so overlapping roots never count a file twice.
$patterns = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
[void]$patterns.Add($commentSuffix)
foreach ($pair in $pairs) { [void]$patterns.Add($pair.Suffix) }
foreach ($extension in $markers.Keys) { [void]$patterns.Add([string]$extension) }
$listArguments = @('-C', $repoRootFull, '-c', 'core.quotePath=false', 'ls-files', '--cached', '--others', '--exclude-standard', '--') +
    @($patterns | ForEach-Object { ':(icase)*' + $_ })
$listing = Invoke-AuditGit -Arguments $listArguments
if ($listing.ExitCode -ne 0) {
    throw "Git could not enumerate the files under $repoRootFull, so the audit cannot judge."
}

$scanPaths = [System.Collections.Generic.List[string]]::new()
$scanRelatives = @{}
$scanFolders = @{}
foreach ($entry in $listing.Lines) {
    $relative = ([string]$entry).Trim()
    if ($relative.Length -eq 0 -or (Test-IsExcludedPath -Relative $relative -ExcludedNames $excludedNames)) { continue }
    # The repository root is exempt: a file directly inside it is never audited.
    if ($relative.IndexOf('/') -lt 0) { continue }

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

    $scanPaths.Add($full)
    $scanRelatives[$full] = $relative
    $scanFolders[$full] = Get-FolderKey -RootFull $owner.Full -UnderRoot $relative.Substring($owner.Prefix.Length)
}
$scanPaths.Sort([System.StringComparer]::OrdinalIgnoreCase)

foreach ($fileFull in $scanPaths) {
    $file = [System.IO.FileInfo]::new($fileFull)
    $relative = [string]$scanRelatives[$fileFull]
    $folder = [string]$scanFolders[$fileFull]
    if ($file.Name.EndsWith($commentSuffix, [System.StringComparison]::OrdinalIgnoreCase)) {
        Add-CommentFile -File $file -Relative $relative -Folder $folder
    }
    else {
        Add-SourceFile -File $file -Relative $relative -Folder $folder
    }
}

if ($sourceFiles.Count -eq 0) {
    throw "No source file was scanned under the configured roots, so the audit cannot judge."
}

# Folder totals.
$folderMap = [ordered]@{}
foreach ($source in $sourceFiles) {
    if (-not $folderMap.Contains($source.Folder)) {
        $folderMap[$source.Folder] = [pscustomobject]@{ Name = $source.Folder; SourceFiles = 0; SourceNonBlank = [long]0; Files = 0; Lines = [long]0; NonBlank = [long]0; Bytes = [long]0 }
    }
    $folderMap[$source.Folder].SourceFiles++
    $folderMap[$source.Folder].SourceNonBlank += $source.NonBlank
}
foreach ($comment in $commentFiles) {
    if (-not $folderMap.Contains($comment.Folder)) {
        $folderMap[$comment.Folder] = [pscustomobject]@{ Name = $comment.Folder; SourceFiles = 0; SourceNonBlank = [long]0; Files = 0; Lines = [long]0; NonBlank = [long]0; Bytes = [long]0 }
    }
    $folderMap[$comment.Folder].Files++
    $folderMap[$comment.Folder].Lines += $comment.Lines
    $folderMap[$comment.Folder].NonBlank += $comment.NonBlank
    $folderMap[$comment.Folder].Bytes += $comment.Bytes
}
$folderResults = @($folderMap.Values | Sort-Object -Property @{ Expression = 'Lines'; Descending = $true }, @{ Expression = { Get-OrdinalKey $_.Name } })

[long]$totalLines = 0
[long]$totalBytes = 0
[int]$totalFiles = 0
foreach ($item in $folderResults) {
    $totalLines += $item.Lines
    $totalBytes += $item.Bytes
    $totalFiles += $item.Files
}

# Pairing in both directions.
$expectedByDirectory = @{}
foreach ($source in $sourceFiles) {
    if (-not $expectedByDirectory.ContainsKey($source.Directory)) {
        $expectedByDirectory[$source.Directory] = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    }
    [void]$expectedByDirectory[$source.Directory].Add($source.Expected)
}

# Every list keeps the enumeration order: paths sorted without case, like the convention test.
# A folder without its sidecar lists once, under its first source.
$missingSeen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$missingComments = @($sourceFiles | Where-Object { -not [System.IO.File]::Exists((Join-Path $_.Directory $_.Expected)) -and $missingSeen.Add($_.Directory) })
$orphanComments = @($commentFiles | Where-Object {
    -not $exemptComments.Contains($_.Full) -and
    -not ($expectedByDirectory.ContainsKey($_.Directory) -and $expectedByDirectory[$_.Directory].Contains($_.Name))
})

# Line rules inside comment files. The reserved stamp section ends what they read.
$stampHeadingPattern = [System.Text.RegularExpressions.Regex]::new('^##\s+Stamp\s*$')
$ruleHits = [System.Collections.Generic.List[object]]::new()
foreach ($comment in $commentFiles) {
    if (-not $comment.Readable -or $exemptComments.Contains($comment.Full)) { continue }
    $number = 0
    foreach ($line in [System.IO.File]::ReadLines($comment.Full)) {
        $number++
        if ($stampHeadingPattern.IsMatch($line)) { break }
        $problems = @(Test-CommentLine -Line $line)
        if ($problems.Count -gt 0) {
            $ruleHits.Add([pscustomobject]@{ Relative = $comment.Relative; Line = $number; Problem = ($problems -join ', '); Text = $line.Trim() })
        }
    }
}

# In-code comments inside sources.
$remarkHits = [System.Collections.Generic.List[object]]::new()
$blockClosers = @{}
foreach ($property in $config.remark.closers.PSObject.Properties) { $blockClosers[[string]$property.Name] = [string]$property.Value }
foreach ($source in $remarkFiles) {
    if ($unreadable.Contains($source.Full)) { continue }
    $tokens = $markers[$source.Extension]
    $number = 0
    try { $content = [System.IO.File]::ReadAllText($source.Full) }
    catch {
        if ($unreadable.Add($source.Full)) { $readErrors.Add("$($source.Relative): $($_.Exception.Message)") }
        continue
    }
    $stripped = if ($source.Extension -eq '.cs') { Remove-StringLiteral -Text $content } else { $content }
    $raw = $content -split "`n"
    $openToken = $null
    foreach ($code in ($stripped -split "`n")) {
        $number++
        $line = if ($number -le $raw.Count) { $raw[$number - 1] } else { '' }
        if ($null -ne $openToken) {
            $remarkHits.Add([pscustomobject]@{ Relative = $source.Relative; Line = $number; Marker = $openToken; Text = $line.Trim() })
            if ($code.IndexOf($blockClosers[$openToken], [System.StringComparison]::Ordinal) -ge 0) { $openToken = $null }
            continue
        }
        foreach ($token in $tokens) {
            $at = $code.IndexOf($token, [System.StringComparison]::Ordinal)
            if ($at -ge 0) {
                $remarkHits.Add([pscustomobject]@{ Relative = $source.Relative; Line = $number; Marker = $token; Text = $line.Trim() })
                if ($blockClosers.ContainsKey($token) -and $code.IndexOf($blockClosers[$token], $at + $token.Length, [System.StringComparison]::Ordinal) -lt 0) {
                    $openToken = $token
                }
                break
            }
        }
    }
}

$headingHits = [System.Collections.Generic.List[object]]::new()
$headingPattern = [System.Text.RegularExpressions.Regex]::new('^##\s+`(?<span>[^`]+)`\s*$')
$fileNamePattern = [System.Text.RegularExpressions.Regex]::new('^[\w.]+\.(cs|xaml|json|csproj|props|slnx|md)$')
$genericPattern = [System.Text.RegularExpressions.Regex]::new('<[^<>]*>')
$identifierPattern = [System.Text.RegularExpressions.Regex]::new('@?[A-Za-z_][A-Za-z0-9_]*')
# The sources each sidecar describes: every source of its folder.
$ownersByComment = @{}
foreach ($source in $sourceFiles) {
    $pairedFull = [System.IO.Path]::Combine($source.Directory, $source.Expected)
    if (-not $ownersByComment.ContainsKey($pairedFull)) { $ownersByComment[$pairedFull] = [System.Collections.Generic.List[object]]::new() }
    $ownersByComment[$pairedFull].Add($source)
}
foreach ($comment in $commentFiles) {
    if (-not $comment.Readable -or $exemptComments.Contains($comment.Full) -or -not $ownersByComment.ContainsKey($comment.Full)) { continue }
    $owners = @($ownersByComment[$comment.Full] | Where-Object { -not $exemptFiles.Contains($_.Name) -and -not $unreadable.Contains($_.Full) })
    if ($owners.Count -eq 0) { continue }
    $ownerNames = [System.Collections.Generic.HashSet[string]]::new([string[]]@($ownersByComment[$comment.Full] | ForEach-Object { $_.Name }), [System.StringComparer]::OrdinalIgnoreCase)
    $ownerText = -join @($owners | ForEach-Object { [System.IO.File]::ReadAllText($_.Full) })
    $number = 0
    foreach ($line in [System.IO.File]::ReadLines($comment.Full)) {
        $number++
        if ($stampHeadingPattern.IsMatch($line)) { break }
        $heading = $headingPattern.Match($line)
        if (-not $heading.Success) { continue }
        $span = $heading.Groups['span'].Value
        if ($span.StartsWith('<', [System.StringComparison]::Ordinal)) { continue }
        if ($fileNamePattern.IsMatch($span)) {
            if (-not $ownerNames.Contains($span)) {
                $headingHits.Add([pscustomobject]@{ Relative = $comment.Relative; Line = $number; Problem = $span; Text = $line.Trim() })
            }
            continue
        }
        $stop = $span.IndexOfAny([char[]]@('(', '=', ';', '{', ':'))
        $head = if ($stop -lt 0) { $span } else { $span.Substring(0, $stop) }
        while ($genericPattern.IsMatch($head)) { $head = $genericPattern.Replace($head, '') }
        $identifiers = $identifierPattern.Matches($head)
        if ($identifiers.Count -eq 0) { continue }
        $name = $identifiers[$identifiers.Count - 1].Value
        if (-not [System.Text.RegularExpressions.Regex]::IsMatch($ownerText, '\b' + [System.Text.RegularExpressions.Regex]::Escape($name) + '\b')) {
            $headingHits.Add([pscustomobject]@{ Relative = $comment.Relative; Line = $number; Problem = $name; Text = $line.Trim() })
        }
    }
}

# The stamp section that ends every sidecar: the commit, then one hash row per source of the folder.
# A changed, added or removed source is advice, listed with the diff that shows the change.
$hashHits = [System.Collections.Generic.List[object]]::new()
$unstampedHits = [System.Collections.Generic.List[object]]::new()
$unstampedCeiling = [int]$config.ceilings.unstamped
# While enforced is false every finding gate only warns, so a project mid-migration stays green.
$enforced = [bool]$config.enforced
$commitLinePattern = [System.Text.RegularExpressions.Regex]::new('^Commit: `(?<commit>[0-9a-f]{7,40})`\s*$')
$rowLinePattern = [System.Text.RegularExpressions.Regex]::new('^- `(?<name>[^`]+)` `(?<hash>[0-9a-f]{16})`\s*$')
$hashEncoding = [System.Text.UTF8Encoding]::new($false)
$hashAlgorithm = [System.Security.Cryptography.SHA256]::Create()
foreach ($comment in $commentFiles) {
    if (-not $comment.Readable -or $exemptComments.Contains($comment.Full) -or -not $ownersByComment.ContainsKey($comment.Full)) { continue }
    $owners = @($ownersByComment[$comment.Full] | Where-Object { -not $exemptFiles.Contains($_.Name) } | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_.Name } })
    if ($owners.Count -eq 0) { continue }
    $commentLines = [System.IO.File]::ReadAllLines($comment.Full)
    $stampAt = -1
    for ($index = 0; $index -lt $commentLines.Count; $index++) {
        if ($stampHeadingPattern.IsMatch($commentLines[$index])) { $stampAt = $index; break }
    }
    $commit = $null
    $rows = [ordered]@{}
    if ($stampAt -ge 0) {
        for ($index = $stampAt + 1; $index -lt $commentLines.Count; $index++) {
            $commitMatch = $commitLinePattern.Match($commentLines[$index])
            if ($commitMatch.Success) { $commit = $commitMatch.Groups['commit'].Value; continue }
            $rowMatch = $rowLinePattern.Match($commentLines[$index])
            if ($rowMatch.Success) { $rows[$rowMatch.Groups['name'].Value] = $rowMatch.Groups['hash'].Value }
        }
    }
    if ($stampAt -lt 0 -or $null -eq $commit) {
        $unstampedHits.Add([pscustomobject]@{ Relative = $comment.Relative; Line = $(if ($stampAt -lt 0) { $commentLines.Count } else { $stampAt + 1 }); Problem = 'no stamp'; Text = '' })
        continue
    }
    $changed = [System.Collections.Generic.List[string]]::new()
    $added = [System.Collections.Generic.List[string]]::new()
    $readable = $true
    foreach ($owner in $owners) {
        try { $ownerText = [System.IO.File]::ReadAllText($owner.Full) }
        catch {
            if ($unreadable.Add($owner.Full)) { $readErrors.Add("$($owner.Relative): $($_.Exception.Message)") }
            $readable = $false
            break
        }
        $digest = $hashAlgorithm.ComputeHash($hashEncoding.GetBytes($ownerText.Replace("`r`n", "`n").Replace("`r", "`n")))
        $hash = (-join @($digest | ForEach-Object { $_.ToString('x2') })).Substring(0, 16)
        if (-not $rows.Contains($owner.Name)) { $added.Add($owner.Name) }
        elseif ([string]$rows[$owner.Name] -cne $hash) { $changed.Add($owner.Name) }
    }
    if (-not $readable) { continue }
    $ownerNameSet = [System.Collections.Generic.HashSet[string]]::new([string[]]@($owners | ForEach-Object { $_.Name }), [System.StringComparer]::Ordinal)
    $removed = @($rows.Keys | Where-Object { -not $ownerNameSet.Contains([string]$_) })
    if ($changed.Count + $added.Count + $removed.Count -eq 0) { continue }
    $parts = @()
    if ($changed.Count -gt 0) { $parts += 'changed ' + ($changed -join ', ') }
    if ($added.Count -gt 0) { $parts += 'added ' + ($added -join ', ') }
    if ($removed.Count -gt 0) { $parts += 'removed ' + ($removed -join ', ') }
    $folderRelative = [System.IO.Path]::GetDirectoryName($comment.Relative)
    if ([string]::IsNullOrEmpty($folderRelative)) { $folderRelative = '.' }
    $hashHits.Add([pscustomobject]@{ Relative = $comment.Relative; Line = $stampAt + 1; Problem = ($parts -join '; ').Replace(';', ' |'); Text = "git diff $commit -- $($folderRelative.Replace('\', '/'))" })
}
$hashAlgorithm.Dispose()
$staleCeilings = if ($unstampedCeiling -gt $unstampedHits.Count) { 1 } else { 0 }

# Folders over the file limit warn unless their own ceiling holds them. A ceiling above its folder's count is stale and warns.
$folderHits = [System.Collections.Generic.List[object]]::new()
$folderStaleHits = [System.Collections.Generic.List[object]]::new()
$folderCounts = [ordered]@{}
foreach ($source in $sourceFiles) {
    $folderRelative = (Get-RelativePathSafe -BasePath $repoRootFull -Path $source.Directory).Trim('/')
    if ($folderRelative.Length -eq 0) { $folderRelative = '.' }
    if (-not $folderCounts.Contains($folderRelative)) { $folderCounts[$folderRelative] = 0 }
    $folderCounts[$folderRelative]++
}
foreach ($folderRelative in @($folderCounts.Keys | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } })) {
    $count = [int]$folderCounts[$folderRelative]
    if ($count -le $folderFileLimit) { continue }
    if ($folderCeilings.Contains($folderRelative) -and $count -le [int]$folderCeilings[$folderRelative]) { continue }
    $ceilingText = if ($folderCeilings.Contains($folderRelative)) { "ceiling $($folderCeilings[$folderRelative])" } else { "limit $folderFileLimit" }
    $folderHits.Add([pscustomobject]@{ Relative = $folderRelative; Line = $count; Problem = "$count files, $ceilingText"; Text = '' })
}
foreach ($folderRelative in @($folderCeilings.Keys | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } })) {
    $count = if ($folderCounts.Contains($folderRelative)) { [int]$folderCounts[$folderRelative] } else { 0 }
    $ceiling = [int]$folderCeilings[$folderRelative]
    if ($ceiling -gt $count -and $ceiling -gt $folderFileLimit) {
        $folderStaleHits.Add([pscustomobject]@{ Relative = $folderRelative; Line = $count; Problem = "ceiling $ceiling, count $count"; Text = '' })
    }
    elseif ($ceiling -le $folderFileLimit) {
        $folderStaleHits.Add([pscustomobject]@{ Relative = $folderRelative; Line = $count; Problem = "ceiling $ceiling at or under the limit $folderFileLimit"; Text = '' })
    }
}

$exemptListed = @($commentFiles | Where-Object { $exemptComments.Contains($_.Full) })

# Version, read from the configured version file and key.
$version = "0.0.0"
if ([System.IO.File]::Exists($versionPathFull)) {
    try {
        $versionData = Get-Content -LiteralPath $versionPathFull -Raw -Encoding UTF8 | ConvertFrom-Json
        $versionValue = Get-ConfigNode -Document $versionData -Key $versionKey
        if (-not [string]::IsNullOrWhiteSpace([string]$versionValue)) { $version = [string]$versionValue }
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
$outputPathFull = Resolve-UserPath -Path $OutputPath
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($outputPathFull)) | Out-Null

$generatedAt = Get-Date

# Advice printed under the hash findings, in the console and the report alike.
$staleAdvice = @(
    'Better to check: the folder changed since its sidecar was stamped.',
    'Run the listed diff and reread the sidecar where the change touches its intent.',
    'Then run StampComment.ps1 on the sidecar.'
)

# Console output.
Write-AuditLine ("Scanned: {0:N0} source files, {1:N0} comment files" -f $sourceFiles.Count, $commentFiles.Count) -ForegroundColor DarkGray
if (-not $enforced) {
    Write-AuditLine 'Not enforced: every finding gate warns until enforced is true.' -ForegroundColor Yellow
}

$gateRows = @(
    [pscustomobject]@{ Gate = 'Folders without a sidecar'; Count = $missingComments.Count; Warn = -not $enforced; Meaning = 'folders holding a source but no sidecar'; Section = 'Folders without a sidecar' },
    [pscustomobject]@{ Gate = 'Comment files without a folder'; Count = $orphanComments.Count; Warn = -not $enforced; Meaning = 'comment files that are no folder''s sidecar'; Section = 'Comment files without a folder' },
    [pscustomobject]@{ Gate = 'Comment lines breaking the line rules'; Count = $ruleHits.Count; Warn = -not $enforced; Meaning = 'lines too long, multi-sentence or with a forbidden char'; Section = 'Comment lines breaking the line rules' },
    [pscustomobject]@{ Gate = 'In-code comments'; Count = $remarkHits.Count; Warn = -not $enforced; Meaning = 'comment lines left inside sources'; Section = 'In-code comments' },
    [pscustomobject]@{ Gate = 'Headings naming nothing in their folder'; Count = $headingHits.Count; Warn = -not $enforced; Meaning = 'headings naming no file or identifier of their folder'; Section = 'Headings naming nothing in their folder' },
    [pscustomobject]@{ Gate = 'Sidecars with a changed folder'; Count = $hashHits.Count; Warn = $true; Meaning = 'folders changed since the stamp, advice only'; Section = 'Sidecars with a changed folder' },
    [pscustomobject]@{ Gate = 'Sidecars with no stamp'; Count = $unstampedHits.Count; Warn = -not $enforced; Ceiling = $unstampedCeiling; Meaning = "unstamped sidecars, warn up to ceiling $(Format-Integer $unstampedCeiling)"; Section = 'Sidecars with no stamp' },
    [pscustomobject]@{ Gate = 'Folders over the file limit'; Count = $folderHits.Count; Warn = $true; Meaning = "folders over $(Format-Integer $folderFileLimit) sources and their ceiling, warn only"; Section = 'Folders over the file limit' },
    [pscustomobject]@{ Gate = 'Stale folder ceilings'; Count = $folderStaleHits.Count; Warn = $true; Meaning = 'folder ceilings above their count, warn only'; Section = 'Stale folder ceilings' },
    [pscustomobject]@{ Gate = 'Stale ceilings'; Count = $staleCeilings; Meaning = 'ceilings set above their count'; Section = 'Stale ceilings' },
    [pscustomobject]@{ Gate = 'Unreadable files'; Count = $readErrors.Count; Meaning = 'files the audit could not read'; Section = 'Unreadable files' }
)
Write-ResultTable -Rows $gateRows

$tableRows = @($folderResults) + @([pscustomobject]@{
    Name = 'Total'
    Files = $totalFiles
    Lines = $totalLines
    NonBlank = [long](($folderResults | Measure-Object -Property NonBlank -Sum).Sum)
    Bytes = $totalBytes
    SourceNonBlank = [long](($folderResults | Measure-Object -Property SourceNonBlank -Sum).Sum)
})
$folderColumns = @(
    (New-ConsoleColumn -Name 'Folder' -Right $false -Values @($tableRows | ForEach-Object { $_.Name })),
    (New-ConsoleColumn -Name 'Files' -Values @($tableRows | ForEach-Object { Format-Integer $_.Files })),
    (New-ConsoleColumn -Group 'Lines' -Name 'Raw' -Values @($tableRows | ForEach-Object { Format-Integer $_.Lines })),
    (New-ConsoleColumn -Group 'Lines' -Name 'Non-blank' -Values @($tableRows | ForEach-Object { Format-Integer $_.NonBlank })),
    (New-ConsoleColumn -Group 'Lines' -Name 'Share' -Values @($tableRows | ForEach-Object { Format-Percent $(if ($totalLines -gt 0) { ($_.Lines / [double]$totalLines) * 100.0 } else { 0.0 }) })),
    (New-ConsoleColumn -Group 'Sizes' -Name 'Total' -Values @($tableRows | ForEach-Object { Format-Integer $_.Bytes })),
    (New-ConsoleColumn -Group 'Sizes' -Name 'Share' -Values @($tableRows | ForEach-Object { Format-Percent $(if ($totalBytes -gt 0) { ($_.Bytes / [double]$totalBytes) * 100.0 } else { 0.0 }) })),
    (New-ConsoleColumn -Group 'Sizes' -Name 'Average' -Values @($tableRows | ForEach-Object { Format-Integer $(if ($_.Files -gt 0) { [Math]::Round($_.Bytes / [double]$_.Files) } else { 0 }) })),
    (New-ConsoleColumn -Name 'Density' -Values @($tableRows | ForEach-Object { Format-Ratio $(if ($_.SourceNonBlank -gt 0) { $_.NonBlank / [double]$_.SourceNonBlank } else { 0.0 }) }))
)

Write-SectionTitle "Comment lines by folder"
Write-GroupedConsoleTable -Columns $folderColumns

function Write-HitTable {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Items,
        [Parameter(Mandatory = $true)][string]$Kind
    )

    $columns = @(
        (New-ConsoleColumn -Name 'Line' -Values @($Items | ForEach-Object { [string]$_.Line })),
        (New-ConsoleColumn -Name $Kind -Right $false -Values @($Items | ForEach-Object { [string]$_.($Kind) })),
        (New-ConsoleColumn -Name 'File' -Right $false -Values @($Items | ForEach-Object { $_.Relative }))
    )
    Write-GroupedConsoleTable -Columns $columns
}

if ($missingComments.Count -gt 0) {
    Write-SectionTitle ("Folders without a sidecar ({0:N0})" -f $missingComments.Count)
    foreach ($item in $missingComments) { Write-AuditLine "$($item.Relative) -> $($item.Expected)" }
}

if ($orphanComments.Count -gt 0) {
    Write-SectionTitle ("Comment files without a folder ({0:N0})" -f $orphanComments.Count)
    foreach ($item in $orphanComments) { Write-AuditLine $item.Relative }
}

if ($ruleHits.Count -gt 0) {
    Write-SectionTitle ("Comment lines breaking the line rules ({0:N0})" -f $ruleHits.Count)
    Write-HitTable -Items @($ruleHits) -Kind 'Problem'
}

if ($headingHits.Count -gt 0) {
    Write-SectionTitle ("Headings naming nothing in their folder ({0:N0})" -f $headingHits.Count)
    Write-HitTable -Items @($headingHits) -Kind 'Problem'
}

if ($hashHits.Count -gt 0) {
    Write-SectionTitle ("Sidecars with a changed folder ({0:N0})" -f $hashHits.Count) -Color Yellow
    foreach ($item in $hashHits) {
        Write-AuditLine "$($item.Relative): $($item.Problem)"
        Write-AuditLine "  $($item.Text)" -ForegroundColor DarkGray
    }
    foreach ($advice in $staleAdvice) { Write-AuditLine $advice -ForegroundColor Yellow }
}

if ($unstampedHits.Count -gt $unstampedCeiling) {
    Write-SectionTitle ("Sidecars with no stamp ({0:N0}, ceiling {1:N0})" -f $unstampedHits.Count, $unstampedCeiling)
    Write-HitTable -Items @($unstampedHits) -Kind 'Problem'
}
elseif ($unstampedHits.Count -gt 0) {
    # The backlog list is long, so a warning prints its count here and the report keeps the full list.
    Write-SectionTitle ("Sidecars with no stamp ({0:N0}, ceiling {1:N0})" -f $unstampedHits.Count, $unstampedCeiling) -Color Yellow
    Write-AuditLine ("WARNING: {0:N0} sidecars carry no stamp. Reread each one, stamp it with StampComment.ps1, and lower ceilings.unstamped. The report lists them all." -f $unstampedHits.Count) -ForegroundColor Yellow
}

if ($staleCeilings -gt 0) {
    Write-SectionTitle "Stale ceilings (1)"
    Write-AuditLine ("ceilings.unstamped is {0:N0} but only {1:N0} sidecars carry no stamp, so lower it." -f $unstampedCeiling, $unstampedHits.Count)
}

if ($folderHits.Count -gt 0) {
    Write-SectionTitle ("Folders over the file limit ({0:N0})" -f $folderHits.Count) -Color Yellow
    foreach ($item in $folderHits) { Write-AuditLine "$($item.Relative): $($item.Problem)" }
    Write-AuditLine 'Split the folder by concern, or hold its count under a ceilings.folders row.' -ForegroundColor Yellow
}

if ($folderStaleHits.Count -gt 0) {
    Write-SectionTitle ("Stale folder ceilings ({0:N0})" -f $folderStaleHits.Count) -Color Yellow
    foreach ($item in $folderStaleHits) { Write-AuditLine "$($item.Relative): $($item.Problem)" }
    Write-AuditLine 'Lower each row to its folder''s count, or drop it at or under the limit.' -ForegroundColor Yellow
}

if ($remarkHits.Count -gt 0) {
    Write-SectionTitle ("In-code comments ({0:N0})" -f $remarkHits.Count)
    Write-HitTable -Items @($remarkHits) -Kind 'Marker'
}

if ($exemptListed.Count -gt 0) {
    Write-SectionTitle ("Exempt comment files ({0:N0})" -f $exemptListed.Count)
    Write-AuditLine 'Only the developer edits these. Line rules, headings and stamps skip them.' -ForegroundColor DarkGray
    foreach ($item in $exemptListed) { Write-AuditLine $item.Relative }
}

if ($readErrors.Count -gt 0) {
    Write-SectionTitle ("Unreadable files ({0:N0})" -f $readErrors.Count)
    foreach ($readError in $readErrors) { Write-AuditLine $readError }
}

# Markdown output.
$report = [System.Text.StringBuilder]::new()
[void]$report.AppendLine("# Comment report - $version")
[void]$report.AppendLine()
[void]$report.AppendLine("- Generated: $($generatedAt.ToString('yyyy-MM-dd HH:mm:ss zzz'))")
[void]$report.AppendLine("- Source roots: $(ConvertTo-MarkdownCell ($sourceRootFulls -join '; '))")
[void]$report.AppendLine("- Comment pattern: ``$commentPattern``")
[void]$report.AppendLine("- Folder segments: $Segments")
[void]$report.AppendLine("- Line rules: at most $MaxWords words, one sentence, none of $(ConvertTo-MarkdownCell (($forbidden | ForEach-Object { '`' + $_ + '`' }) -join ' '))")
[void]$report.AppendLine("- Excluded directories: $(ConvertTo-MarkdownCell (($excludedNames | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } }) -join ', '))")
[void]$report.AppendLine()
[void]$report.AppendLine("## Summary")
[void]$report.AppendLine()
[void]$report.AppendLine("| Metric | Value |")
[void]$report.AppendLine("|--------|------:|")
[void]$report.AppendLine("| Folders | $(Format-Integer $folderResults.Count) |")
[void]$report.AppendLine("| Source files | $(Format-Integer $sourceFiles.Count) |")
[void]$report.AppendLine("| Comment files | $(Format-Integer $totalFiles) |")
[void]$report.AppendLine("| Comment lines | $(Format-Integer $totalLines) |")
[void]$report.AppendLine("| Folders without a sidecar | $(Format-Integer $missingComments.Count) |")
[void]$report.AppendLine("| Comment files without a folder | $(Format-Integer $orphanComments.Count) |")
[void]$report.AppendLine("| Lines breaking the line rules | $(Format-Integer $ruleHits.Count) |")
[void]$report.AppendLine("| In-code comments | $(Format-Integer $remarkHits.Count) |")
[void]$report.AppendLine("| Headings naming nothing in their folder | $(Format-Integer $headingHits.Count) |")
[void]$report.AppendLine("| Sidecars with a changed folder (advice) | $(Format-Integer $hashHits.Count) |")
[void]$report.AppendLine("| Sidecars with no stamp (warning) | $(Format-Integer $unstampedHits.Count) |")
[void]$report.AppendLine("| Ceiling for sidecars with no stamp | $(Format-Integer $unstampedCeiling) |")
[void]$report.AppendLine("| Folders over the file limit (warning) | $(Format-Integer $folderHits.Count) |")
[void]$report.AppendLine("| Stale folder ceilings (warning) | $(Format-Integer $folderStaleHits.Count) |")
[void]$report.AppendLine("| Exempt comment files | $(Format-Integer $exemptListed.Count) |")
[void]$report.AppendLine()
[void]$report.AppendLine("## Comment lines by folder")
[void]$report.AppendLine()
[void]$report.AppendLine("Density is the number of non-blank comment lines written per non-blank source line.")
[void]$report.AppendLine()
[void]$report.AppendLine("| Folder | Sources | Files | Lines | Non-blank | Share | Bytes | Average bytes | Density |")
[void]$report.AppendLine("|--------|--------:|------:|------:|----------:|------:|------:|--------------:|--------:|")
foreach ($item in $folderResults) {
    $share = if ($totalLines -gt 0) { ($item.Lines / [double]$totalLines) * 100.0 } else { 0.0 }
    $density = if ($item.SourceNonBlank -gt 0) { $item.NonBlank / [double]$item.SourceNonBlank } else { 0.0 }
    $average = if ($item.Files -gt 0) { [Math]::Round($item.Bytes / [double]$item.Files) } else { 0 }
    [void]$report.AppendLine("| $(ConvertTo-MarkdownCell $item.Name) | $(Format-Integer $item.SourceFiles) | $(Format-Integer $item.Files) | $(Format-Integer $item.Lines) | $(Format-Integer $item.NonBlank) | $(Format-Percent $share) | $(Format-Integer $item.Bytes) | $(Format-Integer $average) | $(Format-Ratio $density) |")
}
[void]$report.AppendLine()
[void]$report.AppendLine("## Folders without a sidecar")
[void]$report.AppendLine()
if ($missingComments.Count -eq 0) { [void]$report.AppendLine("None.") }
else {
    [void]$report.AppendLine("| First source | Expected |")
    [void]$report.AppendLine("|--------------|----------|")
    foreach ($item in $missingComments) { [void]$report.AppendLine("| $(ConvertTo-MarkdownCell $item.Relative) | $(ConvertTo-MarkdownCell $item.Expected) |") }
}
[void]$report.AppendLine()
[void]$report.AppendLine("## Comment files without a folder")
[void]$report.AppendLine()
if ($orphanComments.Count -eq 0) { [void]$report.AppendLine("None.") }
else { foreach ($item in $orphanComments) { [void]$report.AppendLine("- $(ConvertTo-MarkdownCell $item.Relative)") } }
[void]$report.AppendLine()
[void]$report.AppendLine("## Comment lines breaking the line rules")
[void]$report.AppendLine()
if ($ruleHits.Count -eq 0) { [void]$report.AppendLine("None.") }
else {
    [void]$report.AppendLine("| File | Line | Problem | Text |")
    [void]$report.AppendLine("|------|-----:|---------|------|")
    foreach ($item in $ruleHits) { [void]$report.AppendLine("| $(ConvertTo-MarkdownCell $item.Relative) | $(Format-Integer $item.Line) | $(ConvertTo-MarkdownCell $item.Problem) | $(ConvertTo-MarkdownCell $item.Text) |") }
}
[void]$report.AppendLine()
[void]$report.AppendLine("## In-code comments")
[void]$report.AppendLine()
if ($remarkHits.Count -eq 0) { [void]$report.AppendLine("None.") }
else {
    [void]$report.AppendLine("| File | Line | Marker | Text |")
    [void]$report.AppendLine("|------|-----:|--------|------|")
    foreach ($item in $remarkHits) { [void]$report.AppendLine("| $(ConvertTo-MarkdownCell $item.Relative) | $(Format-Integer $item.Line) | $(ConvertTo-MarkdownCell $item.Marker) | $(ConvertTo-MarkdownCell $item.Text) |") }
}
[void]$report.AppendLine()
[void]$report.AppendLine("## Sidecars with a changed folder")
[void]$report.AppendLine()
if ($hashHits.Count -eq 0) { [void]$report.AppendLine("None.") }
else {
    [void]$report.AppendLine("| File | Change | Diff |")
    [void]$report.AppendLine("|------|--------|------|")
    foreach ($item in $hashHits) { [void]$report.AppendLine("| $(ConvertTo-MarkdownCell $item.Relative) | $(ConvertTo-MarkdownCell $item.Problem) | ``$($item.Text)`` |") }
    [void]$report.AppendLine()
    foreach ($advice in $staleAdvice) { [void]$report.AppendLine($advice) }
}
[void]$report.AppendLine()
[void]$report.AppendLine("## Folders over the file limit")
[void]$report.AppendLine()
if ($folderHits.Count -eq 0) { [void]$report.AppendLine("None.") }
else { foreach ($item in $folderHits) { [void]$report.AppendLine("- $(ConvertTo-MarkdownCell $item.Relative): $(ConvertTo-MarkdownCell $item.Problem)") } }
[void]$report.AppendLine()
[void]$report.AppendLine("## Stale folder ceilings")
[void]$report.AppendLine()
if ($folderStaleHits.Count -eq 0) { [void]$report.AppendLine("None.") }
else { foreach ($item in $folderStaleHits) { [void]$report.AppendLine("- $(ConvertTo-MarkdownCell $item.Relative): $(ConvertTo-MarkdownCell $item.Problem)") } }
[void]$report.AppendLine()
[void]$report.AppendLine("## Sidecars with no stamp")
[void]$report.AppendLine()
[void]$report.AppendLine("A warning while the count sits at or below the ceiling of $(Format-Integer $unstampedCeiling), a failure above it.")
[void]$report.AppendLine("Reread each sidecar, then stamp it with StampComment.ps1 and lower the ceiling.")
[void]$report.AppendLine()
if ($unstampedHits.Count -eq 0) { [void]$report.AppendLine("None.") }
else {
    [void]$report.AppendLine("| File | Problem |")
    [void]$report.AppendLine("|------|---------|")
    foreach ($item in $unstampedHits) { [void]$report.AppendLine("| $(ConvertTo-MarkdownCell $item.Relative) | $(ConvertTo-MarkdownCell $item.Problem) |") }
}
[void]$report.AppendLine()
[void]$report.AppendLine("## Exempt comment files")
[void]$report.AppendLine()
[void]$report.AppendLine("Only the developer edits these. Line rules, headings and stamps skip them.")
[void]$report.AppendLine()
if ($exemptListed.Count -eq 0) { [void]$report.AppendLine("None.") }
else { foreach ($item in $exemptListed) { [void]$report.AppendLine("- $(ConvertTo-MarkdownCell $item.Relative)") } }
if ($readErrors.Count -gt 0) {
    [void]$report.AppendLine()
    [void]$report.AppendLine("## Read errors")
    [void]$report.AppendLine()
    foreach ($readError in $readErrors) { [void]$report.AppendLine("- $(ConvertTo-MarkdownCell $readError)") }
}

[System.IO.File]::WriteAllText($outputPathFull, ($report.ToString() -replace "`r`n", "`n"), [System.Text.UTF8Encoding]::new($false))

# Page output: the full result as data inside the template AuditComments.html.
function ConvertTo-PageLink {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([string]::Equals([System.IO.Path]::GetDirectoryName($Path), $reportDirectoryFull.TrimEnd('\', '/'), [System.StringComparison]::OrdinalIgnoreCase)) {
        return [System.IO.Path]::GetFileName($Path)
    }
    return [System.Uri]::new($Path).AbsoluteUri
}

# One hit list as page rows: path, line, the kind column and the trimmed text.
function ConvertTo-PageHit {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Items,
        [Parameter(Mandatory = $true)][string]$Kind
    )

    return , @($Items | ForEach-Object { [ordered]@{ path = [string]$_.Relative; line = [int]$_.Line; kind = [string]$_.($Kind); text = [string]$_.Text } })
}

# The state of every comment file, as the pair and hash checks judged it.
$staleRelatives = [System.Collections.Generic.HashSet[string]]::new([string[]]@($hashHits | ForEach-Object { $_.Relative }), [System.StringComparer]::OrdinalIgnoreCase)
$unstampedRelatives = [System.Collections.Generic.HashSet[string]]::new([string[]]@($unstampedHits | ForEach-Object { $_.Relative }), [System.StringComparer]::OrdinalIgnoreCase)
$pageComments = [System.Collections.Generic.List[object]]::new()
foreach ($comment in $commentFiles) {
    $owners = @()
    if ($ownersByComment.ContainsKey($comment.Full)) { $owners = @($ownersByComment[$comment.Full] | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_.Name } }) }
    $state = 'stamped'
    if (-not $comment.Readable) { $state = 'unreadable' }
    elseif ($owners.Count -eq 0) { $state = 'no source' }
    elseif ($exemptComments.Contains($comment.Full)) { $state = 'exempt' }
    elseif (@($owners | Where-Object { $exemptFiles.Contains($_.Name) }).Count -gt 0) { $state = 'exempt' }
    elseif ($staleRelatives.Contains($comment.Relative)) { $state = 'changed' }
    elseif ($unstampedRelatives.Contains($comment.Relative)) { $state = 'no stamp' }
    elseif (@($owners | Where-Object { $unreadable.Contains($_.Full) }).Count -gt 0) { $state = 'unreadable' }
    $pageComments.Add([ordered]@{
        path = [string]$comment.Relative; folder = [string]$comment.Folder; state = $state
        sources = @($owners | ForEach-Object { [string]$_.Relative })
        lines = [long]$comment.Lines; nonBlank = [long]$comment.NonBlank; bytes = [long]$comment.Bytes
    })
}

$pageTemplatePath = Join-Path $PSScriptRoot 'AuditComments.html'
$pagePathFull = Join-Path $reportDirectoryFull ("{0}{1}.html" -f $reportPrefix, $version)
[System.IO.Directory]::CreateDirectory($reportDirectoryFull) | Out-Null

$pageData = [ordered]@{
    project = [string]$config.project
    version = $version
    generation = $script:AuditGeneration
    enforced = $enforced
    generated = $generatedAt.ToString('yyyy-MM-dd HH:mm:ss zzz')
    scanned = [ordered]@{ sources = [long]$sourceFiles.Count; comments = [long]$commentFiles.Count }
    gates = @($gateRows | ForEach-Object {
        $ceiling = $null
        if ($null -ne $_.PSObject.Properties['Ceiling']) { $ceiling = [long]$_.Ceiling }
        [ordered]@{ status = Get-GateStatus -Row $_; gate = $_.Gate; count = [long]$_.Count; ceiling = $ceiling; meaning = $_.Meaning; section = $_.Section }
    })
    ceilings = @(@([ordered]@{ key = 'ceilings.unstamped'; kind = 'Sidecars with no stamp'; count = [long]$unstampedHits.Count; ceiling = [long]$unstampedCeiling }) + @($folderCeilings.Keys | ForEach-Object {
        [ordered]@{ key = 'ceilings.folders.' + [string]$_; kind = 'Folder sources'; count = [long]$(if ($folderCounts.Contains([string]$_)) { $folderCounts[[string]$_] } else { 0 }); ceiling = [long]$folderCeilings[[string]$_] }
    }))
    scope = [ordered]@{
        roots = @($rootEntries | ForEach-Object { $(if ($_.Prefix.Length -eq 0) { '.' } else { $_.Prefix.TrimEnd('/') }) })
        commentPattern = $commentPattern
        segments = $Segments
        pairs = @($pairs | ForEach-Object { [ordered]@{ suffix = [string]$_.Suffix; template = '{folder}' + $commentSuffix } })
        folderFiles = $folderFileLimit
        excluded = @($excludedNames | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } })
        excludedSuffixes = @($excludedSuffixes)
        maxWords = $MaxWords
        forbidden = @($forbidden)
        sentenceMarks = @($sentenceMarks)
        abbreviations = @($config.rules.abbreviations | ForEach-Object { [string]$_ })
        markers = @($config.remark.markers.PSObject.Properties | ForEach-Object { [ordered]@{ extension = [string]$_.Name; tokens = @($_.Value | ForEach-Object { [string]$_ }) } })
        closers = @($config.remark.closers.PSObject.Properties | ForEach-Object { [ordered]@{ open = [string]$_.Name; close = [string]$_.Value } })
        exemptFiles = @($config.remark.exemptFiles | ForEach-Object { [string]$_ })
        exemptComments = @($exemptListed | ForEach-Object { [string]$_.Relative })
    }
    folders = @($folderResults | ForEach-Object {
        [ordered]@{ name = [string]$_.Name; sourceFiles = [long]$_.SourceFiles; sourceNonBlank = [long]$_.SourceNonBlank; files = [long]$_.Files; lines = [long]$_.Lines; nonBlank = [long]$_.NonBlank; bytes = [long]$_.Bytes }
    })
    missing = @($missingComments | ForEach-Object { [ordered]@{ path = [string]$_.Relative; expected = [string]$_.Expected } })
    orphans = @($orphanComments | ForEach-Object { [string]$_.Relative })
    rules = ConvertTo-PageHit -Items @($ruleHits) -Kind 'Problem'
    headings = ConvertTo-PageHit -Items @($headingHits) -Kind 'Problem'
    stale = ConvertTo-PageHit -Items @($hashHits) -Kind 'Problem'
    staleAdvice = @($staleAdvice)
    folderLimit = ConvertTo-PageHit -Items @($folderHits) -Kind 'Problem'
    folderStale = ConvertTo-PageHit -Items @($folderStaleHits) -Kind 'Problem'
    unstamped = ConvertTo-PageHit -Items @($unstampedHits) -Kind 'Problem'
    remarks = ConvertTo-PageHit -Items @($remarkHits) -Kind 'Marker'
    unreadable = @($readErrors)
    comments = @($pageComments)
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

# Changed folders, folders over the limit and stale folder ceilings only warn, so they never fail the run.
# While enforced is false the finding gates warn too, and only stale ceilings and unreadable files fail.
$findings = $missingComments.Count + $orphanComments.Count + $ruleHits.Count + $remarkHits.Count + $headingHits.Count
$held = $findings -eq 0 -and $unstampedHits.Count -le $unstampedCeiling
if ((-not $held -and $enforced) -or ($staleCeilings + $readErrors.Count) -gt 0) {
    exit 1
}

exit 0
