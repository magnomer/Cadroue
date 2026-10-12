<#
.SYNOPSIS
Runs every audit, prints one progress line per audit, and writes and opens one summary page that links to every audit's page.

.DESCRIPTION
Reads the configuration from Audit.json next to this script, then performs these actions:
  1. Deletes every audit page directly inside the report folder, the summary page and each
     listed audit's own, so the run leaves only fresh output. The folder holds the reports of
     every script family, so a file of another name is never touched. -Keep skips this. The folder must be a direct child of the repository root
     with the configured name, or the run stops. Subfolders are never deleted, moved or
     written, and the folders named in report.keep, such as records, which holds the
     history records, are never touched.
  2. Runs each audit listed in audits, in that order, one at a time, with -NoOpen and
     -NoPause passed to every script that declares them. A missing or throwing script is
     recorded as failed and the run continues.
  3. Collects from each audit its exit code, duration, Result table, verdict line and
     every Report line.
  4. Prints one progress line per audit as it finishes: its position, name, status and
     duration. The results themselves go to the page, not the console.
  5. Writes the summary page {report.directory}\{prefix}{version}.html from the template
     Audit.html next to this script, with the code fingerprint from Audit.fingerprint.ps1,
     prints the verdict, and opens the page unless -NoOpen is given. The individual
     audit pages are never opened. AuditShow.ps1 reads the fingerprint to tell whether
     the page is still current.
The run exits with 1 when any audit failed or threw, else 0.

An audit passes when it exits with 0, its verdict starts with PASS and no Result row
fails. It warns when it passes with warning rows. The history audits print no Result
table: they pass when they exit with 0, and their last console line is their verdict.

Audit.json shape:
  {
    "project": "Cadroue",
    "report": { "directory": "docs-analysis", "versionFile": "version.json", "versionKey": "current-version", "prefix": "Audit-", "keep": ["records"] },
    "audits": [ "AuditLines", "AuditLinesHistory", "AuditEncoding", "AuditFault" ]
  }

Each entry names scripts\<entry>.ps1.

.PARAMETER ConfigPath
Path to the JSON configuration. Defaults to Audit.json next to this script.

.PARAMETER Keep
Keep the existing output instead of clearing it first.

.PARAMETER NoOpen
Write the page without opening it.

.PARAMETER Help
Display this help and exit without running any audit. The alias -? is supported.

.EXAMPLE
Audit

.EXAMPLE
Audit -Keep -NoOpen
#>
#requires -Version 5.1
# AUDIT - AUDIT GENERATION 22.
[CmdletBinding()]
param(
    [string]$ConfigPath,
    [switch]$Keep,
    [switch]$NoOpen,
    [Alias('?')]
    [switch]$Help
)

if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    $ConfigPath = Join-Path $PSScriptRoot 'Audit.json'
}

if ($Help) {
    @'
NAME
    Audit.ps1

SYNOPSIS
    Run every audit and write one summary page that links to every audit's page.

SYNTAX
    Audit [-ConfigPath <path>] [-Keep] [-NoOpen] [-Help]

CONFIGURATION
    All project-specific values live in Audit.json next to the script:
    the project name, report directory, version file and key, summary page
    file-name prefix, the report subfolders that are never touched, and the
    audits to run, in order. Each audit names scripts\<name>.ps1.

RUN
    First every audit page directly inside the report folder is deleted,
    unless -Keep is given: a file named after the summary prefix or a listed
    audit. The folder holds the reports of every script family, so any other
    file stays. The folder must be a direct child of the repository root
    with the configured name. Only files directly inside it are deleted:
    subfolders are never deleted, moved or written, and the subfolders named
    in report.keep, such as records with the history records, are never
    touched. Then each audit runs in order, with -NoOpen and
    -NoPause passed to every script that declares them. A missing or throwing
    script is recorded as failed and the run continues.

PROGRESS
    One line per audit as it finishes: position, name, status and duration.
    OK when the audit passed, WARN when it passed with warning rows, FAIL
    otherwise. The history audits print no Result table: they pass when they
    exit with 0. The results themselves are on the page, not the console.
    The run exits with 1 when any audit failed or threw, else 0.

PAGE
    Every run writes the summary page {report.directory}\{prefix}{version}.html.
    It opens in the default browser unless -NoOpen is given. The individual
    audit pages are never opened. The page stores the code fingerprint, so
    AuditShow can tell whether the code changed since this run.

OPTIONS
    -ConfigPath <path>
        JSON configuration file. Defaults to .\Audit.json.

    -Keep
        Keep the existing output instead of clearing it first.

    -NoOpen
        Write the page without opening it.

    -Help, -?
        Display this help and exit without running any audit.

EXAMPLES
    Audit
        Clear the report folder, run every audit and open the summary page.

    Audit -Keep -NoOpen
        Keep the old reports, run every audit and write the summary page
        without opening it.
'@ | Write-Host
    exit 0
}

Write-Host 'AUDIT - AUDIT GENERATION 22' -ForegroundColor Blue

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding

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
        throw "The audit configuration was not found: $pathFull"
    }

    try {
        $config = Get-Content -LiteralPath $pathFull -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The audit configuration is not valid JSON: $pathFull`n$($_.Exception.Message)"
    }

    $problems = [System.Collections.Generic.List[string]]::new()
    foreach ($key in @('project', 'report.directory', 'report.versionFile', 'report.versionKey', 'report.prefix', 'audits')) {
        if ($null -eq (Get-ConfigNode -Document $config -Key $key)) {
            $problems.Add("missing key '$key'")
        }
    }
    if ($problems.Count -gt 0) {
        throw "The audit configuration is not valid: $pathFull`n  " + ($problems -join "`n  ")
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

function Format-Integer {
    param([long]$Value)

    return $Value.ToString('N0', [System.Globalization.CultureInfo]::InvariantCulture)
}

function Resolve-ReportFolder {
    # The folder must be a direct child of the repository root with the configured name,
    # so clearing it can never reach anything else.
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $folder = Join-AuditPath -Root $Root -Relative $Name
    $folderTrimmed = $folder.TrimEnd('\', '/')
    $rootTrimmed = $Root.TrimEnd('\', '/')
    $parent = [System.IO.Path]::GetDirectoryName($folderTrimmed)
    $leaf = [System.IO.Path]::GetFileName($folderTrimmed)
    $nameTrimmed = $Name.Trim().TrimEnd('\', '/')
    if ([string]::IsNullOrEmpty($parent) -or
        -not [string]::Equals($parent, $rootTrimmed, [System.StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals($leaf, $nameTrimmed, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "The report folder must be a direct child of the repository root named '$Name', but it resolves to: $folder"
    }

    return $folderTrimmed
}

function Clear-ReportFolder {
    # Deletes only the audit pages directly inside the folder: the files whose name starts with one
    # of the prefixes. The folder holds the reports of every script family, so a file of any other
    # name is never touched. Every subfolder is skipped, so nothing under it is ever deleted, moved
    # or written, and an entry named in Keep is never touched.
    param(
        [Parameter(Mandatory = $true)][string]$Folder,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$Keep,
        [Parameter(Mandatory = $true)][string[]]$Prefixes
    )

    if (-not [System.IO.Directory]::Exists($Folder)) { return }
    $kept = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($name in $Keep) { [void]$kept.Add($name) }
    foreach ($entry in [System.IO.Directory]::GetFileSystemEntries($Folder)) {
        $attributes = [System.IO.File]::GetAttributes($entry)
        if (($attributes -band [System.IO.FileAttributes]::Directory) -ne 0) { continue }
        $name = [System.IO.Path]::GetFileName($entry)
        if ($kept.Contains($name)) { continue }
        $owned = @($Prefixes | Where-Object { $name.StartsWith($_, [System.StringComparison]::OrdinalIgnoreCase) })
        if ($owned.Count -eq 0) { continue }
        [System.IO.File]::Delete($entry)
    }
}

function Get-KeptFolders {
    # The report subfolders named in report.keep, each a plain folder name such as records.
    param([Parameter(Mandatory = $true)]$Config)

    $node = Get-ConfigNode -Document $Config -Key 'report.keep'
    if ($null -eq $node) { return @() }
    $names = @($node | ForEach-Object { ([string]$_).Trim() } | Where-Object { $_.Length -gt 0 })
    foreach ($name in $names) {
        if ($name -eq '.' -or $name -eq '..' -or $name.IndexOfAny([char[]]@('\', '/', ':')) -ge 0) {
            throw "Every report.keep entry must be a plain subfolder name, but one is: $name"
        }
    }
    return $names
}

function Invoke-Captured {
    param([scriptblock]$Block)

    # A native stderr line must never end the run, so every audit runs under Continue.
    $ErrorActionPreference = 'Continue'
    $global:LASTEXITCODE = 0
    $lines = [System.Collections.Generic.List[string]]::new()
    $threw = $null
    try {
        $pending = ''
        & $Block *>&1 | ForEach-Object {
            $text = $pending + ($_ | Out-String).TrimEnd("`r", "`n")
            if ($_ -is [System.Management.Automation.InformationRecord] -and $_.MessageData -is [System.Management.Automation.HostInformationMessage] -and $_.MessageData.NoNewLine) {
                $pending = $text
            }
            else {
                $pending = ''
                $lines.Add($text)
            }
        }
        if ($pending -ne '') { $lines.Add($pending) }
    }
    catch {
        $threw = $_.Exception.Message
    }

    # Split multi-line records into lines and drop colour escapes, so the text is plain.
    $plain = [System.Collections.Generic.List[string]]::new()
    foreach ($line in $lines) {
        foreach ($part in ($line -replace "`r`n", "`n" -replace "`r", "`n").Split("`n")) {
            $plain.Add(($part -replace '\x1b\[[0-9;?]*[A-Za-z]', ''))
        }
    }

    return [pscustomobject]@{ Lines = $plain.ToArray(); Exit = $global:LASTEXITCODE; Threw = $threw }
}

function Get-ResultRows {
    # Reads the Result table: heading, underline, header row, column rule, one row per gate.
    param([AllowEmptyCollection()][string[]]$Lines)

    $start = [Array]::IndexOf($Lines, 'Result')
    if ($start -lt 0) { return $null }
    $rows = [System.Collections.Generic.List[object]]::new()
    $index = $start + 1
    while ($index -lt $Lines.Count -and $Lines[$index] -match '^-[- ]*$|^Status\s') { $index++ }
    while ($index -lt $Lines.Count -and $Lines[$index].Trim() -ne '') {
        if ($Lines[$index] -notmatch '^(OK|WARN|FAIL)\s+([\d,]+)\s{2}(.+?)(?:\s{2,}(.*))?$') { return $null }
        $meaning = if ($null -ne $Matches[4]) { $Matches[4].Trim() } else { '' }
        $rows.Add([pscustomobject]@{ Status = $Matches[1]; Count = [long]($Matches[2] -replace ',', ''); Gate = $Matches[3].Trim(); Meaning = $meaning })
        $index++
    }
    if ($rows.Count -eq 0) { return $null }

    $verdict = ''
    for ($next = $index; $next -lt $Lines.Count; $next++) {
        if ($Lines[$next] -match '^(PASS|FAIL): ') {
            $verdict = $Lines[$next].Trim()
            break
        }
    }

    return [pscustomobject]@{ Rows = $rows.ToArray(); Verdict = $verdict }
}

function Get-ReportPaths {
    # Every Report line, and the Page line the history audits print instead.
    param([AllowEmptyCollection()][string[]]$Lines)

    $paths = [System.Collections.Generic.List[string]]::new()
    foreach ($line in $Lines) {
        if ($line -match '^Report: (.+)$') {
            $paths.Add($Matches[1].Trim())
        }
        elseif ($line -match '^Page: (.+?\.html)(?: \(.*\))?$') {
            $paths.Add($Matches[1].Trim())
        }
    }

    return $paths.ToArray()
}

function ConvertTo-PageLink {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Folder
    )

    if ([string]::Equals([System.IO.Path]::GetDirectoryName($Path), $Folder, [System.StringComparison]::OrdinalIgnoreCase)) {
        return [System.IO.Path]::GetFileName($Path)
    }
    return [System.Uri]::new($Path).AbsoluteUri
}

function Get-LastLine {
    param([AllowEmptyCollection()][string[]]$Lines)

    for ($index = $Lines.Count - 1; $index -ge 0; $index--) {
        if ($Lines[$index].Trim() -ne '') { return $Lines[$index].Trim() }
    }
    return ''
}

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

function Invoke-Audit {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Folder
    )

    $path = Join-Path (Join-Path $Root 'scripts') ($Name + '.ps1')
    $result = [ordered]@{
        Name = $Name; Status = 'FAIL'; Exit = $null; Duration = [long]0; Verdict = ''
        Rows = @(); Above = [long]0; Links = @(); Console = ''
    }
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $result.Verdict = 'missing script'
        return [pscustomobject]$result
    }

    $quiet = @{}
    $parameters = (Get-Command -Name $path).Parameters
    if ($parameters.ContainsKey('NoOpen')) { $quiet['NoOpen'] = $true }
    if ($parameters.ContainsKey('NoPause')) { $quiet['NoPause'] = $true }

    Set-Location -LiteralPath $Root
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    $run = Invoke-Captured { & $path @quiet }
    $watch.Stop()
    Set-Location -LiteralPath $Root

    $result.Exit = [int]$run.Exit
    $result.Duration = [long]$watch.ElapsedMilliseconds
    $result.Console = ($run.Lines -join "`n")
    $result.Links = @(@(Get-ReportPaths -Lines $run.Lines) | Where-Object { [System.IO.File]::Exists($_) } | ForEach-Object {
        $full = [System.IO.Path]::GetFullPath($_)
        [ordered]@{ name = [System.IO.Path]::GetFileName($full); link = ConvertTo-PageLink -Path $full -Folder $Folder; kind = [System.IO.Path]::GetExtension($full).TrimStart('.').ToLowerInvariant() }
    })

    if ($null -ne $run.Threw) {
        $result.Verdict = "FAIL: threw: $($run.Threw)"
        return [pscustomobject]$result
    }

    $table = Get-ResultRows -Lines $run.Lines
    if ($null -eq $table) {
        if ($Name -match 'History$') {
            $result.Verdict = Get-LastLine -Lines $run.Lines
            $result.Status = if ($result.Exit -eq 0) { 'OK' } else { 'FAIL' }
        }
        else {
            $result.Verdict = 'FAIL: no Result table in the output.'
        }
        return [pscustomobject]$result
    }

    $result.Rows = @($table.Rows)
    $result.Verdict = $table.Verdict
    $result.Above = [long]@($table.Rows | Where-Object { $_.Count -gt 0 }).Count
    $failing = @($table.Rows | Where-Object { $_.Status -eq 'FAIL' }).Count -gt 0
    $warning = @($table.Rows | Where-Object { $_.Status -eq 'WARN' }).Count -gt 0
    $passed = $result.Exit -eq 0 -and -not $failing -and $table.Verdict.StartsWith('PASS:', [System.StringComparison]::Ordinal)
    $result.Status = if (-not $passed) { 'FAIL' } elseif ($warning) { 'WARN' } else { 'OK' }
    return [pscustomobject]$result
}

$repoRootFull = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')).TrimEnd('\', '/')
$config = Read-AuditConfig -Path $ConfigPath
$audits = @($config.audits | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($audits.Count -eq 0) {
    throw "The audit configuration lists no audit: $ConfigPath"
}

$reportFolder = Resolve-ReportFolder -Root $repoRootFull -Name ([string]$config.report.directory)
$versionPathFull = Join-AuditPath -Root $repoRootFull -Relative ([string]$config.report.versionFile)
$versionKey = [string]$config.report.versionKey
$reportPrefix = [string]$config.report.prefix
$keptFolders = @(Get-KeptFolders -Config $config)

# Version, read from the configured version file and key.
$version = '0.0.0'
if ([System.IO.File]::Exists($versionPathFull)) {
    try {
        $versionData = Get-Content -LiteralPath $versionPathFull -Raw -Encoding UTF8 | ConvertFrom-Json
        $versionValue = Get-ConfigNode -Document $versionData -Key $versionKey
        if (-not [string]::IsNullOrWhiteSpace([string]$versionValue)) {
            $version = [string]$versionValue
        }
    }
    catch {
        Write-Host "The version file is not valid JSON, using $version : $versionPathFull"
    }
}
else {
    Write-Host "The version file was not found, using $version : $versionPathFull"
}

if (-not $Keep) {
    # The summary page and every listed audit name their pages <prefix><version>.
    $auditPrefixes = @($reportPrefix) + @($audits | ForEach-Object { [string]$_ + '-' })
    Clear-ReportFolder -Folder $reportFolder -Keep $keptFolders -Prefixes $auditPrefixes
}
[System.IO.Directory]::CreateDirectory($reportFolder) | Out-Null

Write-Host ('Scanned: {0} audits' -f (Format-Integer $audits.Count)) -ForegroundColor DarkGray

$startedAt = Get-Date
$totalWatch = [System.Diagnostics.Stopwatch]::StartNew()
$results = [System.Collections.Generic.List[object]]::new()
$nameWidth = ($audits | ForEach-Object { $_.Length } | Measure-Object -Maximum).Maximum
$countWidth = ([string]$audits.Count).Length
foreach ($name in $audits) {
    $position = ('[{0}/{1}]' -f ([string]($results.Count + 1)).PadLeft($countWidth), $audits.Count)
    Write-Host ('{0} {1}  ' -f $position, $name.PadRight($nameWidth)) -NoNewline
    $result = Invoke-Audit -Name $name -Root $repoRootFull -Folder $reportFolder
    $results.Add($result)
    $color = switch ($result.Status) { 'FAIL' { 'Red' } 'WARN' { 'Yellow' } default { 'Green' } }
    Write-Host $result.Status.PadRight(4) -ForegroundColor $color -NoNewline
    Write-Host ([string]::Format([System.Globalization.CultureInfo]::InvariantCulture, '  {0:0.0} s', $result.Duration / 1000.0)) -ForegroundColor DarkGray
}
$totalWatch.Stop()
Set-Location -LiteralPath $repoRootFull

. (Join-Path $PSScriptRoot 'Audit.fingerprint.ps1')
$fingerprint = Get-AuditFingerprint -Root $repoRootFull -Excluded ([string]$config.report.directory)

# Page output: the summary as data inside the template Audit.html.
$pageTemplatePath = Join-Path $PSScriptRoot 'Audit.html'
$pagePathFull = Join-Path $reportFolder ('{0}{1}.html' -f $reportPrefix, $version)
$pageData = [ordered]@{
    project = [string]$config.project
    version = $version
    generated = $startedAt.ToString('yyyy-MM-dd HH:mm:ss zzz')
    duration = [long]$totalWatch.ElapsedMilliseconds
    fingerprint = $fingerprint
    audits = @($results | ForEach-Object {
        [ordered]@{
            name = $_.Name
            status = $_.Status
            exit = $_.Exit
            duration = $_.Duration
            verdict = $_.Verdict
            above = $_.Above
            rows = @($_.Rows | ForEach-Object { [ordered]@{ status = $_.Status; count = $_.Count; gate = $_.Gate; meaning = $_.Meaning } })
            links = @($_.Links)
            console = $_.Console
        }
    })
}

$utf8 = [System.Text.UTF8Encoding]::new($false)
$pageTemplate = [System.IO.File]::ReadAllText($pageTemplatePath, $utf8)
foreach ($marker in @('/*__DATA__*/', '__TITLE__')) {
    if (-not $pageTemplate.Contains($marker)) { throw "The page template lacks the marker $marker : $pageTemplatePath" }
}
$pageText = $pageTemplate.Replace('__TITLE__', [System.Net.WebUtility]::HtmlEncode([string]$config.project)).Replace('/*__DATA__*/', (ConvertTo-PageJson $pageData))
[System.IO.File]::WriteAllText($pagePathFull, ($pageText -replace "`r`n", "`n"), $utf8)

$failed = @($results | Where-Object { $_.Status -eq 'FAIL' })
Write-Host ''
if ($failed.Count -eq 0) {
    Write-Host ('PASS: all {0} audits at 0.' -f $results.Count) -ForegroundColor Green
}
else {
    $names = ($failed | ForEach-Object { '"' + $_.Name + '"' }) -join ', '
    Write-Host ('FAIL: {0} of {1} audits above 0. See {2}.' -f $failed.Count, $results.Count, $names) -ForegroundColor Red
}
Write-Host "Report: $pagePathFull"

if (-not $NoOpen) {
    Start-Process -FilePath $pagePathFull
}

if ($failed.Count -gt 0) {
    exit 1
}

exit 0
