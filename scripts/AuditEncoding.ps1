<#
.SYNOPSIS
Audits every tracked text file for one encoding form: UTF-8, no mark, LF, a final newline, clean text.

.DESCRIPTION
Reads the project configuration from AuditEncoding.json next to this script, lists the text
files through git, reads each one as raw bytes and reports:

  Invalid UTF-8           a byte sequence that is not UTF-8, at the offset of the first bad byte
  Byte order marks        a byte order mark anywhere in the file, the first three bytes included
  Carriage returns        a carriage return, paired with a line feed or alone
  Missing final newlines  a non-empty file whose last byte is not a line feed
  Control characters      a raw C0 control other than tab, line feed and carriage return, or delete
  Double-encoded text     UTF-8 that was read back through a single-byte page and saved again
  Tabs in sources         a tab in a file whose suffix is listed under tabless
  Non-ASCII scripts       a byte outside ASCII in a file whose suffix is listed under ascii

Files come from git: tracked and untracked files, never ignored ones, as in the convention
tests. Excluded segments and skipped names compare without case. The console follows
scripts\report.md. Every run writes a Markdown report of the scope, the Result table and
every hit list to {report.directory}\{prefix}{version}.md, and the full result as a page to
{report.directory}\{prefix}{version}.html. The page opens in the default browser unless
-NoOpen is given. The version is read from the configured version file and key.

This script carries no project-specific value of its own. Everything a project chooses - the
kinds of file, the skipped names, the suffixes and the patterns - lives in AuditEncoding.json.
Git is the only external tool required.

AuditEncoding.json shape:
  {
    "generation": 23,
    "project": "Cadroue",
    "sources": {
      "include": ["*.cs", "*.md", "*.ps1"],
      "excludeSegments": [".git", "bin", "obj"],
      "skip": ["version.json"]
    },
    "tabless": [".cs", ".xaml"],
    "ascii": [".ps1"],
    "patterns": {
      "control": "<regex>",
      "trail": "<regex>",
      "mojibake": "<regex, where {trail} stands for the trail pattern>"
    },
    "report": {
      "directory": "docs-analysis",
      "versionFile": "version.json",
      "versionKey": "current-version",
      "prefix": "AuditEncoding-"
    }
  }

.PARAMETER Root
Project root to audit. Defaults to the directory containing this script's parent.

.PARAMETER ConfigPath
Path to the JSON configuration. Defaults to AuditEncoding.json next to this script.

.PARAMETER Open
Open the generated Markdown report after the audit finishes.

.PARAMETER NoOpen
Write the page without opening it.

.PARAMETER Help
Display this help and exit. The alias -? is supported.

.EXAMPLE
AuditEncoding
Audit the current checkout.

.EXAMPLE
AuditEncoding -Root D:\temp\sample
Audit another git working tree with this configuration.
#>
#requires -Version 5.1
# AUDITENCODING - AUDIT GENERATION 23.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the audited outcome changes.
# A generation names the set of checks the audit applies. Two projects on the same generation audit
# the same things and their reports compare directly, whatever else differs between the files. A check
# added, removed, or changed in what it reports is a new generation; wording, plumbing, and refactoring
# leave it alone. Every audit script shares one generation number with the convention-test settings,
# and each refuses a configuration written at another generation.
# Generation 12: the first generation of this audit. It reports the eight kinds of the convention test.
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
    [string]$Root,
    [string]$ConfigPath,
    [switch]$Open,
    [switch]$NoOpen,
    [Alias('?')]
    [switch]$Help
)

if ($Help) {
    @'
NAME
    AuditEncoding.ps1

SYNOPSIS
    Audit every tracked text file for valid UTF-8 with no byte order mark,
    LF line breaks, a final newline, no raw control character, no
    double-encoded text, no tab in sources and pure ASCII in scripts.

SYNTAX
    AuditEncoding [-Root <path>] [-ConfigPath <path>] [-Open] [-NoOpen] [-Help]

CONFIGURATION
    All project-specific values live in AuditEncoding.json next to the
    script: the git pathspecs, the excluded segments, the skipped names, the
    tabless and ascii suffixes, the control and mojibake patterns and the
    report folder, version file, version key and file prefix.

REPORTS
    Every run writes a Markdown report of the scope, the Result table and
    every hit list to {report.directory}\{prefix}{version}.md.
    Every run writes the full result as a page next to the Markdown report.
    It opens in the default browser unless -NoOpen is given.

RESULT
    Invalid UTF-8           A byte sequence that is not UTF-8.
    Byte order marks        A byte order mark anywhere in the file.
    Carriage returns        A carriage return, paired or alone.
    Missing final newlines  A non-empty file not ending in a line feed.
    Control characters      A raw control character.
    Double-encoded text     UTF-8 read back through a single-byte page.
    Tabs in sources         A tab in a tabless file.
    Non-ASCII scripts       A byte outside ASCII in a script.

    The exit code is 1 when any Result gate is above 0 and 0 otherwise.

OPTIONS
    -Root <path>
        Project root to audit. Defaults to the parent of the script folder.

    -ConfigPath <path>
        JSON configuration file. Defaults to .\AuditEncoding.json.

    -Open
        Open the generated Markdown report after the audit finishes.

    -NoOpen
        Write the page without opening it.

    -Help, -?
        Display this help and exit without running the audit.

EXAMPLES
    AuditEncoding
        Audit the current checkout.

    AuditEncoding -Root D:\temp\sample
        Audit another git working tree with this configuration.
'@ | Write-Host
    exit 0
}

# Under Windows PowerShell 5.1 an advanced script evaluates a parameter default before
# $PSScriptRoot is available to it, so the defaults are resolved here.
if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Split-Path -Parent $PSScriptRoot
}
if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    $ConfigPath = Join-Path $PSScriptRoot 'AuditEncoding.json'
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding

$script:AuditGeneration = 23
$script:Invariant = [System.Globalization.CultureInfo]::InvariantCulture

Write-Host "AUDITENCODING - AUDIT GENERATION $script:AuditGeneration" -ForegroundColor Blue

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
        throw "The encoding-audit configuration was not found: $pathFull"
    }

    try {
        $config = [System.IO.File]::ReadAllText($pathFull, [System.Text.UTF8Encoding]::new($false)) | ConvertFrom-Json
    }
    catch {
        throw "The encoding-audit configuration is not valid JSON: $pathFull`n$($_.Exception.Message)"
    }

    $problems = [System.Collections.Generic.List[string]]::new()
    foreach ($key in @('generation', 'project', 'sources.include', 'sources.excludeSegments', 'sources.skip',
                       'tabless', 'ascii', 'patterns.control', 'patterns.trail', 'patterns.mojibake',
                       'report.directory', 'report.versionFile', 'report.versionKey', 'report.prefix')) {
        if ($null -eq (Get-ConfigNode -Document $config -Key $key)) {
            $problems.Add("missing key '$key'")
        }
    }

    if ($problems.Count -gt 0) {
        throw "The encoding-audit configuration is not valid: $pathFull`n  " + ($problems -join "`n  ")
    }

    if ([int]$config.generation -ne $script:AuditGeneration) {
        throw "The encoding-audit configuration is generation $($config.generation) but this tooling is generation $script:AuditGeneration.`nA generation names the set of checks applied, so the two must match: $pathFull"
    }

    return $config
}

function Resolve-ProjectRoot {
    param([Parameter(Mandatory = $true)][string]$Path)

    $item = Get-Item -LiteralPath $Path -ErrorAction SilentlyContinue
    if ($null -eq $item -or -not $item.PSIsContainer) {
        throw "The project root is not a directory: $Path"
    }

    return $item.FullName.TrimEnd([char[]]@('\', '/'))
}

function ConvertTo-ArgumentText {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value)

    if ($Value.Length -eq 0 -or $Value -match '[\s"]') {
        return '"' + (($Value -replace '(\\*)"', '$1$1\"') -replace '(\\+)$', '$1$1') + '"'
    }

    return $Value
}

function Get-GitFiles {
    # The same enumeration as the convention tests: git lists tracked and untracked files matching
    # the pathspecs without case, then excluded segments and skipped names drop out, files gone from
    # disk drop out, and the full paths sort ordinally without case.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)][string[]]$Include,
        [AllowEmptyCollection()][string[]]$Segments = @(),
        [AllowEmptyCollection()][string[]]$Skip = @()
    )

    $arguments = @('-c', 'core.quotePath=false', 'ls-files', '--cached', '--others', '--exclude-standard', '--') +
        @($Include | ForEach-Object { ':(icase)' + $_ })
    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = 'git'
    $info.WorkingDirectory = $ProjectRoot
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.UseShellExecute = $false
    $info.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
    $info.StandardErrorEncoding = [System.Text.UTF8Encoding]::new($false)
    $info.Arguments = (@($arguments | ForEach-Object { ConvertTo-ArgumentText $_ })) -join ' '

    $process = [System.Diagnostics.Process]::Start($info)
    $errorTask = $process.StandardError.ReadToEndAsync()
    $output = $process.StandardOutput.ReadToEnd()
    $errorText = $errorTask.GetAwaiter().GetResult()
    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        throw "Git failed to enumerate source files.`n$errorText"
    }

    $segmentSet = [System.Collections.Generic.HashSet[string]]::new([string[]]$Segments, [System.StringComparer]::OrdinalIgnoreCase)
    $skipSet = [System.Collections.Generic.HashSet[string]]::new([string[]]$Skip, [System.StringComparer]::OrdinalIgnoreCase)
    $separator = [System.IO.Path]::DirectorySeparatorChar
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $files = [System.Collections.Generic.List[object]]::new()
    foreach ($line in $output.Split("`n")) {
        $relative = $line.Trim()
        if ($relative.Length -eq 0) { continue }
        $parts = $relative.Split('/')
        $excluded = $false
        foreach ($part in $parts) {
            if ($segmentSet.Contains($part)) { $excluded = $true; break }
        }
        if ($excluded -or $skipSet.Contains($parts[$parts.Length - 1])) { continue }
        $full = [System.IO.Path]::Combine($ProjectRoot, $relative.Replace('/', $separator))
        if (-not $seen.Add($full) -or -not [System.IO.File]::Exists($full)) { continue }
        $files.Add([pscustomobject]@{ Full = $full; Relative = $relative })
    }

    $byFull = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)
    foreach ($file in $files) { $byFull[$file.Full] = $file }
    $keys = [string[]]@($files | ForEach-Object { $_.Full })
    [System.Array]::Sort($keys, [System.StringComparer]::OrdinalIgnoreCase)
    return , [object[]]@($keys | ForEach-Object { $byFull[$_] })
}

function Get-LineAt {
    param([Parameter(Mandatory = $true)][string]$Text, [Parameter(Mandatory = $true)][int]$Index)

    $before = $Text.Substring(0, $Index)
    return $before.Length - $before.Replace("`n", '').Length + 1
}

function Find-PatternLine {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text, [Parameter(Mandatory = $true)][regex]$Pattern, [string]$Label)

    $match = $Pattern.Match($Text)
    if (-not $match.Success) { return $null }
    return '{0} at line {1}' -f $Label, (Get-LineAt -Text $Text -Index $match.Index)
}

function Find-InvalidOffset {
    # The offset of the first ill-formed sequence, as the .NET decoder reports it in the test.
    # The exception index of .NET Framework counts from an internal buffer, so the walk is done here.
    param([Parameter(Mandatory = $true)][byte[]]$Bytes)

    $index = 0
    while ($index -lt $Bytes.Length) {
        $lead = [int]$Bytes[$index]
        if ($lead -lt 0x80) { $index++; continue }
        $low = 0x80
        $high = 0xBF
        if ($lead -ge 0xC2 -and $lead -le 0xDF) { $count = 1 }
        elseif ($lead -eq 0xE0) { $count = 2; $low = 0xA0 }
        elseif ($lead -eq 0xED) { $count = 2; $high = 0x9F }
        elseif ($lead -ge 0xE1 -and $lead -le 0xEF) { $count = 2 }
        elseif ($lead -eq 0xF0) { $count = 3; $low = 0x90 }
        elseif ($lead -eq 0xF4) { $count = 3; $high = 0x8F }
        elseif ($lead -ge 0xF1 -and $lead -le 0xF3) { $count = 3 }
        else { return $index }
        for ($step = 1; $step -le $count; $step++) {
            $position = $index + $step
            if ($position -ge $Bytes.Length) { return $index }
            $trail = [int]$Bytes[$position]
            $floor = if ($step -eq 1) { $low } else { 0x80 }
            $ceiling = if ($step -eq 1) { $high } else { 0xBF }
            if ($trail -lt $floor -or $trail -gt $ceiling) { return $index }
        }
        $index += $count + 1
    }

    return $Bytes.Length
}

function Test-Suffix {
    param([Parameter(Mandatory = $true)][string]$Path, [AllowEmptyCollection()][string[]]$Suffixes)

    foreach ($suffix in $Suffixes) {
        if ($Path.EndsWith($suffix, [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    return $false
}

function Write-SectionTitle {
    param([Parameter(Mandatory = $true)][string]$Text)

    Write-Host ''
    Write-Host $Text -ForegroundColor Blue
    Write-Host ('-' * $Text.Length) -ForegroundColor DarkGray
}

function Write-ResultTable {
    param([Parameter(Mandatory = $true)][object[]]$Rows)

    Write-SectionTitle 'Result'
    $statusWidth = 6
    $countWidth = [Math]::Max(5, ($Rows | ForEach-Object { (Format-Integer $_.Count).Length } | Measure-Object -Maximum).Maximum)
    $gateWidth = [Math]::Max(4, ($Rows | ForEach-Object { $_.Gate.Length } | Measure-Object -Maximum).Maximum)
    $meaningWidth = [Math]::Max(7, ($Rows | ForEach-Object { $_.Meaning.Length } | Measure-Object -Maximum).Maximum)
    Write-Host ('{0}  {1}  {2}  Meaning' -f 'Status'.PadRight($statusWidth), 'Count'.PadLeft($countWidth), 'Gate'.PadRight($gateWidth)) -ForegroundColor Cyan
    Write-Host (@(('-' * $statusWidth), ('-' * $countWidth), ('-' * $gateWidth), ('-' * $meaningWidth)) -join '  ') -ForegroundColor Cyan
    foreach ($row in $Rows) {
        $failing = $row.Count -gt 0
        $status = if ($failing) { 'FAIL' } else { 'OK' }
        $text = '{0}  {1}  {2}  {3}' -f $status.PadRight($statusWidth), (Format-Integer $row.Count).PadLeft($countWidth), $row.Gate.PadRight($gateWidth), $row.Meaning
        Write-Host $status -ForegroundColor $(if ($failing) { 'Red' } else { 'Green' }) -NoNewline
        Write-Host $text.Substring($status.Length)
    }

    $failed = @($Rows | Where-Object { $_.Count -gt 0 })
    Write-Host ''
    if ($failed.Count -eq 0) {
        Write-Host ('PASS: all {0} gates at 0.' -f $Rows.Count) -ForegroundColor Green
    }
    else {
        $sections = ($failed | ForEach-Object { '"' + $_.Section + '"' }) -join ', '
        Write-Host ('FAIL: {0} of {1} gates above 0. See {2}.' -f $failed.Count, $Rows.Count, $sections) -ForegroundColor Red
    }
}

function Format-Integer {
    param([long]$Value)

    return $Value.ToString('N0', $script:Invariant)
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

function Add-Hit {
    # One hit: the console line, the path and detail for the reports, and the kind marked on the file.
    param(
        [Parameter(Mandatory = $true)][object[]]$Kinds,
        [Parameter(Mandatory = $true)][int]$Index,
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Detail,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][System.Collections.Generic.List[int]]$Marks
    )

    $Kinds[$Index].Hits.Add(('{0}: {1}' -f $Path, $Detail))
    $Kinds[$Index].Items.Add([pscustomobject]@{ Path = $Path; Detail = $Detail })
    $Marks.Add($Index)
}

function Format-CodeList {
    param([AllowEmptyCollection()][string[]]$Values)

    if ($null -eq $Values -or $Values.Count -eq 0) { return 'None' }
    return (@($Values | ForEach-Object { '`' + $_ + '`' })) -join ', '
}

# Page output: the full result as data inside the template AuditEncoding.html.
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

function ConvertTo-PageLink {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([string]::Equals([System.IO.Path]::GetDirectoryName($Path), $reportDirectoryFull.TrimEnd('\', '/'), [System.StringComparison]::OrdinalIgnoreCase)) {
        return [System.IO.Path]::GetFileName($Path)
    }
    return [System.Uri]::new($Path).AbsoluteUri
}

$projectRoot = Resolve-ProjectRoot -Path $Root
$config = Read-AuditConfig -Path $ConfigPath
$reportDirectoryFull = Join-AuditPath -Root $projectRoot -Relative ([string]$config.report.directory)
$versionPathFull = Join-AuditPath -Root $projectRoot -Relative ([string]$config.report.versionFile)
$versionKey = [string]$config.report.versionKey
$reportPrefix = [string]$config.report.prefix

# Version, read from the configured version file and key.
$version = "0.0.0"
if ([System.IO.File]::Exists($versionPathFull)) {
    try {
        $versionData = Get-Content -LiteralPath $versionPathFull -Raw -Encoding UTF8 | ConvertFrom-Json
        $versionValue = Get-ConfigNode -Document $versionData -Key $versionKey
        if (-not [string]::IsNullOrWhiteSpace([string]$versionValue)) { $version = [string]$versionValue }
    }
    catch {
        Write-Host "The version file is not valid JSON, using $version : $versionPathFull"
    }
}
else {
    Write-Host "The version file was not found, using $version : $versionPathFull"
}

$include = [string[]]@($config.sources.include | ForEach-Object { [string]$_ })
$segments = [string[]]@($config.sources.excludeSegments | ForEach-Object { [string]$_ })
$skip = [string[]]@($config.sources.skip | ForEach-Object { [string]$_ })
$tabless = [string[]]@($config.tabless | ForEach-Object { [string]$_ })
$ascii = [string[]]@($config.ascii | ForEach-Object { [string]$_ })
$controlPattern = [regex]::new([string]$config.patterns.control)
$mojibakePattern = [regex]::new(([string]$config.patterns.mojibake).Replace('{trail}', [string]$config.patterns.trail))
$tabPattern = [regex]::new("`t")
$highPattern = [regex]::new('[\u0080-\u00FF]')
$mark = [string]::new([char[]]@([char]0xEF, [char]0xBB, [char]0xBF))

$strict = [System.Text.UTF8Encoding]::new($false, $true)
$loose = [System.Text.Encoding]::UTF8
$latin = [System.Text.Encoding]::GetEncoding(28591)

$files = Get-GitFiles -ProjectRoot $projectRoot -Include $include -Segments $segments -Skip $skip
if ($files.Count -eq 0) {
    throw "No tracked text file was enumerated under $projectRoot; the audit would pass vacuously."
}

$kinds = @(
    @{ Label = 'Invalid UTF-8'; Hits = [System.Collections.Generic.List[string]]::new(); Items = [System.Collections.Generic.List[object]]::new() },
    @{ Label = 'Byte order marks'; Hits = [System.Collections.Generic.List[string]]::new(); Items = [System.Collections.Generic.List[object]]::new() },
    @{ Label = 'Carriage returns'; Hits = [System.Collections.Generic.List[string]]::new(); Items = [System.Collections.Generic.List[object]]::new() },
    @{ Label = 'Missing final newlines'; Hits = [System.Collections.Generic.List[string]]::new(); Items = [System.Collections.Generic.List[object]]::new() },
    @{ Label = 'Control characters'; Hits = [System.Collections.Generic.List[string]]::new(); Items = [System.Collections.Generic.List[object]]::new() },
    @{ Label = 'Double-encoded text'; Hits = [System.Collections.Generic.List[string]]::new(); Items = [System.Collections.Generic.List[object]]::new() },
    @{ Label = 'Tabs in sources'; Hits = [System.Collections.Generic.List[string]]::new(); Items = [System.Collections.Generic.List[object]]::new() },
    @{ Label = 'Non-ASCII scripts'; Hits = [System.Collections.Generic.List[string]]::new(); Items = [System.Collections.Generic.List[object]]::new() }
)

$fileRows = [System.Collections.Generic.List[object]]::new()
foreach ($file in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($file.Full)
    $relative = $file.Relative
    $marks = [System.Collections.Generic.List[int]]::new()
    $raw = $latin.GetString($bytes)
    $text = $loose.GetString($bytes)

    $invalid = $false
    try {
        [void]$strict.GetString($bytes)
    }
    catch {
        $invalid = $true
    }
    if ($invalid) {
        Add-Hit -Kinds $kinds -Index 0 -Path $relative -Detail ('invalid byte sequence at offset {0}' -f (Find-InvalidOffset -Bytes $bytes)) -Marks $marks
    }

    $offset = $raw.IndexOf($mark, [System.StringComparison]::Ordinal)
    if ($offset -ge 0) {
        Add-Hit -Kinds $kinds -Index 1 -Path $relative -Detail ('byte order mark at offset {0}' -f $offset) -Marks $marks
    }

    $returns = $raw.Length - $raw.Replace("`r", '').Length
    if ($returns -gt 0) {
        Add-Hit -Kinds $kinds -Index 2 -Path $relative -Detail ('{0} carriage return(s)' -f $returns) -Marks $marks
    }

    if ($bytes.Length -gt 0 -and $bytes[$bytes.Length - 1] -ne 10) {
        Add-Hit -Kinds $kinds -Index 3 -Path $relative -Detail 'no newline at end of file' -Marks $marks
    }

    $reason = Find-PatternLine -Text $text -Pattern $controlPattern -Label 'control character'
    if ($null -ne $reason) { Add-Hit -Kinds $kinds -Index 4 -Path $relative -Detail $reason -Marks $marks }

    $reason = Find-PatternLine -Text $text -Pattern $mojibakePattern -Label 'double-encoded text'
    if ($null -ne $reason) { Add-Hit -Kinds $kinds -Index 5 -Path $relative -Detail $reason -Marks $marks }

    if (Test-Suffix -Path $relative -Suffixes $tabless) {
        $reason = Find-PatternLine -Text $text -Pattern $tabPattern -Label 'tab'
        if ($null -ne $reason) { Add-Hit -Kinds $kinds -Index 6 -Path $relative -Detail $reason -Marks $marks }
    }

    if (Test-Suffix -Path $relative -Suffixes $ascii) {
        $reason = Find-PatternLine -Text $raw -Pattern $highPattern -Label 'non-ASCII byte'
        if ($null -ne $reason) { Add-Hit -Kinds $kinds -Index 7 -Path $relative -Detail $reason -Marks $marks }
    }

    $extension = [System.IO.Path]::GetExtension($relative).ToLowerInvariant()
    if ($extension.Length -eq 0) { $extension = '(none)' }
    $fileRows.Add([pscustomobject]@{ Path = $relative; Extension = $extension; Bytes = [long]$bytes.Length; Kinds = $marks.ToArray() })
}

Write-Host ('Scanned: {0} text files' -f (Format-Integer $files.Count)) -ForegroundColor DarkGray

$meanings = @{
    'Invalid UTF-8' = 'files with a byte sequence that is not UTF-8'
    'Byte order marks' = 'files with a byte order mark'
    'Carriage returns' = 'files with a carriage return'
    'Missing final newlines' = 'non-empty files not ending in a line feed'
    'Control characters' = 'files with a raw control character'
    'Double-encoded text' = 'files with UTF-8 read back through a single-byte page'
    'Tabs in sources' = 'tabless files containing a tab'
    'Non-ASCII scripts' = 'scripts with a byte outside ASCII'
}
$total = 0
$gateRows = foreach ($kind in $kinds) {
    $total += $kind.Hits.Count
    [pscustomobject]@{ Gate = $kind.Label; Count = $kind.Hits.Count; Meaning = $meanings[$kind.Label]; Section = $kind.Label }
}
Write-ResultTable -Rows @($gateRows)

foreach ($kind in $kinds) {
    if ($kind.Hits.Count -eq 0) { continue }
    Write-SectionTitle ('{0} ({1})' -f $kind.Label, (Format-Integer $kind.Hits.Count))
    foreach ($hit in $kind.Hits) {
        Write-Host $hit
    }
}

# Reports: the Markdown report and the page, both under the configured report directory.
$generatedAt = Get-Date
$generatedText = $generatedAt.ToString('yyyy-MM-dd HH:mm:ss zzz')
[System.IO.Directory]::CreateDirectory($reportDirectoryFull) | Out-Null
$markdownPathFull = Join-Path $reportDirectoryFull ('{0}{1}.md' -f $reportPrefix, $version)
$pagePathFull = Join-Path $reportDirectoryFull ('{0}{1}.html' -f $reportPrefix, $version)
$utf8 = [System.Text.UTF8Encoding]::new($false)

$totalBytes = [long]0
$byExtension = [System.Collections.Generic.Dictionary[string, object]]::new([System.StringComparer]::Ordinal)
foreach ($row in $fileRows) {
    $totalBytes += $row.Bytes
    if (-not $byExtension.ContainsKey($row.Extension)) {
        $byExtension[$row.Extension] = [pscustomobject]@{ Extension = $row.Extension; Files = [long]0; Bytes = [long]0 }
    }
    $byExtension[$row.Extension].Files++
    $byExtension[$row.Extension].Bytes += $row.Bytes
}
$extensionKeys = [string[]]@($byExtension.Keys)
[System.Array]::Sort($extensionKeys, [System.StringComparer]::Ordinal)

$failedRows = @($gateRows | Where-Object { $_.Count -gt 0 })
if ($failedRows.Count -eq 0) {
    $verdictText = 'PASS: all {0} gates at 0.' -f @($gateRows).Count
}
else {
    $verdictText = 'FAIL: {0} of {1} gates above 0. See {2}.' -f $failedRows.Count, @($gateRows).Count, (($failedRows | ForEach-Object { '"' + $_.Section + '"' }) -join ', ')
}

$report = [System.Text.StringBuilder]::new()
[void]$report.AppendLine("# Encoding report - $version")
[void]$report.AppendLine()
[void]$report.AppendLine("- Project: $([string]$config.project)")
[void]$report.AppendLine("- Generated: $generatedText")
[void]$report.AppendLine("- Generation: $script:AuditGeneration")
[void]$report.AppendLine("- Project root: ``$projectRoot``")
[void]$report.AppendLine("- Scanned: $(Format-Integer $files.Count) text files, $(Format-Integer $totalBytes) bytes")
[void]$report.AppendLine("- Includes: $(Format-CodeList $include)")
[void]$report.AppendLine("- Excluded segments: $(Format-CodeList $segments)")
[void]$report.AppendLine("- Skipped names: $(Format-CodeList $skip)")
[void]$report.AppendLine("- Tabless suffixes: $(Format-CodeList $tabless)")
[void]$report.AppendLine("- ASCII suffixes: $(Format-CodeList $ascii)")
[void]$report.AppendLine("- Control pattern: ``$([string]$config.patterns.control)``")
[void]$report.AppendLine("- Trail pattern: ``$([string]$config.patterns.trail)``")
[void]$report.AppendLine("- Mojibake pattern: ``$([string]$config.patterns.mojibake)``, where ``{trail}`` stands for the trail pattern")
[void]$report.AppendLine()
[void]$report.AppendLine("## Result")
[void]$report.AppendLine()
[void]$report.AppendLine("| Status | Count | Gate | Meaning |")
[void]$report.AppendLine("|--------|------:|------|---------|")
foreach ($row in $gateRows) {
    $status = if ($row.Count -gt 0) { 'FAIL' } else { 'OK' }
    [void]$report.AppendLine("| $status | $(Format-Integer $row.Count) | $($row.Gate) | $($row.Meaning) |")
}
[void]$report.AppendLine()
[void]$report.AppendLine($verdictText)
foreach ($kind in $kinds) {
    [void]$report.AppendLine()
    [void]$report.AppendLine("## $($kind.Label) ($(Format-Integer $kind.Hits.Count))")
    [void]$report.AppendLine()
    if ($kind.Items.Count -eq 0) {
        [void]$report.AppendLine("None.")
        continue
    }
    foreach ($item in $kind.Items) {
        [void]$report.AppendLine("- ``$($item.Path)``: $($item.Detail)")
    }
}
[System.IO.File]::WriteAllText($markdownPathFull, ($report.ToString() -replace "`r`n", "`n"), $utf8)

$pageTemplatePath = Join-Path $PSScriptRoot 'AuditEncoding.html'
$pageData = [ordered]@{
    project = [string]$config.project
    version = $version
    generation = $script:AuditGeneration
    generated = $generatedText
    root = $projectRoot
    scanned = [long]$files.Count
    bytes = $totalBytes
    gates = @($gateRows | ForEach-Object {
        [ordered]@{ status = $(if ($_.Count -gt 0) { 'FAIL' } else { 'OK' }); gate = [string]$_.Gate; count = [long]$_.Count; meaning = [string]$_.Meaning; section = [string]$_.Section }
    })
    scope = [ordered]@{
        include = @($include)
        excludeSegments = @($segments)
        skip = @($skip)
        tabless = @($tabless)
        ascii = @($ascii)
        control = [string]$config.patterns.control
        trail = [string]$config.patterns.trail
        mojibake = [string]$config.patterns.mojibake
    }
    extensions = @($extensionKeys | ForEach-Object {
        $entry = $byExtension[$_]
        [ordered]@{ extension = [string]$entry.Extension; files = [long]$entry.Files; bytes = [long]$entry.Bytes }
    })
    kinds = @($kinds | ForEach-Object {
        [ordered]@{ label = [string]$_.Label; hits = @($_.Items | ForEach-Object { [ordered]@{ path = [string]$_.Path; detail = [string]$_.Detail } }) }
    })
    files = @($fileRows | ForEach-Object {
        [ordered]@{ path = [string]$_.Path; extension = [string]$_.Extension; bytes = [long]$_.Bytes; kinds = @($_.Kinds | ForEach-Object { [int]$_ }) }
    })
    reports = @(
        [ordered]@{ name = [System.IO.Path]::GetFileName($markdownPathFull); link = ConvertTo-PageLink -Path $markdownPathFull }
    )
}

$pageTemplate = [System.IO.File]::ReadAllText($pageTemplatePath, $utf8)
foreach ($marker in @('/*__DATA__*/', '__TITLE__')) {
    if (-not $pageTemplate.Contains($marker)) { throw "The page template lacks the marker $marker : $pageTemplatePath" }
}
$pageText = $pageTemplate.Replace('__TITLE__', [System.Net.WebUtility]::HtmlEncode([string]$config.project)).Replace('/*__DATA__*/', (ConvertTo-PageJson $pageData))
[System.IO.File]::WriteAllText($pagePathFull, ($pageText -replace "`r`n", "`n"), $utf8)

Write-Host ''
Write-Host "Report: $markdownPathFull"
Write-Host "Report: $pagePathFull"

if ($Open) {
    Start-Process -FilePath $markdownPathFull
}

if (-not $NoOpen) {
    Start-Process -FilePath $pagePathFull
}

if ($total -gt 0) {
    exit 1
}

exit 0
