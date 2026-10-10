#requires -Version 5.1
<#
.SYNOPSIS
    Record source lines by folder for every version of the repository.
.DESCRIPTION
    Lists version-labelled commits, whose first message line begins with a
    three-part version such as 0.17.7607. The local git history is read first;
    when it is unavailable, the GitHub REST API supplies the list instead.

    Each version without a record is fetched into the system temp folder as a
    ZIP, counted, and the temp folder is deleted before the next version. The
    ZIP comes from git archive when the commit exists locally, and from GitHub
    otherwise.

    Counting follows AuditLines: files under the source roots with the source
    extensions, grouped by the first path segments under their root, skipping
    excluded folder names. Each folder records files, raw, non-blank and blank
    lines, and bytes. Every tally root that holds matching tracked files
    adds one row of its own.

    Each version keeps its record in a file of its own, {version}.json in the
    records folder named in AuditLinesHistory.json, written once that version
    is counted. A record counted under other rules than those of
    AuditLinesHistory.json is counted again, and so is a version whose commit
    changed.

    Lineage links one folder to another where git renamed source files from
    the first into the second between two consecutive versions, and those files
    are at least the lineage share of AuditLinesHistory.json of the files the
    old folder held before or the new folder holds after. The renames come from
    AuditHistory.lineage.ps1 and stay in the lineage folder named in
    AuditLinesHistory.json, apart from the line records, so no lineage change
    counts a version again. The
    page's Lineage checkbox gives every folder linked this way, directly or
    through others, one shared color.

    All records, sorted by version, are then placed into the page template
    AuditLinesHistory.html, written as {prefix}{version}.html into the report folder named in AuditLinesHistory.json,
    and opened in the default browser. The page is self-contained and works
    offline.

    Set GITHUB_TOKEN for authenticated access or a higher API rate limit.
.PARAMETER Rebuild
    Count every version again, ignoring existing records.
.PARAMETER NoOpen
    Write the page without opening it.
.PARAMETER Help
    Display this help and exit. The alias -? is supported.
.EXAMPLE
    AuditLinesHistory
    Count every version that has no record yet, then write and open the page.
.EXAMPLE
    AuditLinesHistory -Rebuild
    Count every version again.
#>
# AUDITLINESHISTORY - AUDIT GENERATION 21.
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

Write-Host 'AUDITLINESHISTORY - AUDIT GENERATION 21' -ForegroundColor Blue

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$configPath = Join-Path $PSScriptRoot 'AuditLinesHistory.json'
$templatePath = Join-Path $PSScriptRoot 'AuditLinesHistory.html'
foreach ($path in @($configPath, $templatePath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "A required file is missing: $path" }
}
$config = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json

function Get-NormalizedExtension {
    param([object[]]$Values)

    $set = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($value in @($Values)) {
        $text = ([string]$value).Trim()
        if ($text.Length -eq 0) { continue }
        if (-not $text.StartsWith('.', [System.StringComparison]::Ordinal)) { $text = '.' + $text }
        [void]$set.Add($text.ToLowerInvariant())
    }
    return , $set
}

function Get-NormalizedRoot {
    param([object[]]$Values)

    return @(@($Values) | ForEach-Object { ([string]$_).Trim().Replace('\', '/').Trim('/') } | Where-Object { $_.Length -gt 0 })
}

$sourceRoots = Get-NormalizedRoot -Values $config.sources.roots
$sourceExtensions = Get-NormalizedExtension -Values $config.sources.extensions
$tallyRoots = Get-NormalizedRoot -Values $config.tally.roots
$tallyExtensions = Get-NormalizedExtension -Values $config.tally.extensions
$excludedNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($segment in @($config.sources.excludeSegments)) {
    if (-not [string]::IsNullOrWhiteSpace([string]$segment)) { [void]$excludedNames.Add(([string]$segment).Trim()) }
}
$segments = [int]$config.segments
if ($sourceRoots.Count -eq 0 -or $sourceExtensions.Count -eq 0 -or $segments -lt 1) {
    throw "AuditLinesHistory.json needs source roots, source extensions, and segments of 1 or more."
}

$repository = ([string]$config.repository).Trim()
$recordsPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ([string]$config.records)))
$lineagePath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ([string]$config.lineage.records)))
$lineageShare = [double]$config.lineage.share
if (-not ($lineageShare -gt 0 -and $lineageShare -le 1)) {
    throw "AuditLinesHistory.json needs a lineage share above 0 and at most 1."
}

function Get-OrdinalList {
    param([object[]]$Values)

    $list = [System.Collections.Generic.List[string]]::new()
    foreach ($value in @($Values)) { $list.Add([string]$value) }
    $list.Sort([System.StringComparer]::Ordinal)
    return , $list
}

$rulesText = @(
    'roots=' + ((Get-OrdinalList -Values $sourceRoots) -join ',')
    'extensions=' + ((Get-OrdinalList -Values @($sourceExtensions)) -join ',')
    'exclude=' + ((Get-OrdinalList -Values @($excludedNames | ForEach-Object { $_.ToLowerInvariant() })) -join ',')
    'segments=' + $segments
    'tallyRoots=' + ((Get-OrdinalList -Values $tallyRoots) -join ',')
    'tallyExtensions=' + ((Get-OrdinalList -Values @($tallyExtensions)) -join ',')
    'record=3'
) -join "`n"
$sha = [System.Security.Cryptography.SHA256]::Create()
try {
    $rules = -join ($sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($rulesText)) | ForEach-Object { $_.ToString('x2') })
}
finally {
    $sha.Dispose()
}

$versionPattern = '^(?<version>(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*))(?:\s|$)'
$dateFormat = 'yyyy-MM-dd HH:mm:ss zzz'

$apiHeaders = @{
    Accept                 = 'application/vnd.github+json'
    'User-Agent'           = 'AuditLinesHistory.ps1'
    'X-GitHub-Api-Version' = '2022-11-28'
}
$downloadHeaders = @{ 'User-Agent' = 'AuditLinesHistory.ps1' }
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
    $wanted = @($sourceRoots) + @($tallyRoots) | ForEach-Object { ($_ -split '/')[0] }
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

function Test-IsExcludedPath {
    param([Parameter(Mandatory = $true)][string]$Relative)

    foreach ($segment in $Relative.Split('/')) {
        if ($excludedNames.Contains($segment)) { return $true }
    }
    return $false
}

function Get-FolderKey {
    param([Parameter(Mandatory = $true)][string]$Relative)

    $cut = $Relative.LastIndexOf('/')
    if ($cut -le 0) { return '(root)' }
    $parts = @($Relative.Substring(0, $cut).Split('/'))
    $take = [Math]::Min($segments, $parts.Count)
    return (($parts | Select-Object -First $take) -join '/')
}

function Find-RootPrefix {
    param(
        [Parameter(Mandatory = $true)][string]$Relative,
        [Parameter(Mandatory = $true)][object[]]$Roots
    )

    foreach ($root in $Roots) {
        $prefix = $root + '/'
        if ($Relative.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) { return $prefix }
    }
    return $null
}

function Measure-Archive {
    param(
        [Parameter(Mandatory = $true)][string]$ZipPath,
        [Parameter(Mandatory = $true)][bool]$StripTop
    )

    $sourceMap = @{}
    $tallyMap = @{}
    $zip = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        foreach ($entry in $zip.Entries) {
            $relative = $entry.FullName.Replace('\', '/')
            if ($relative.EndsWith('/')) { continue }
            if ($StripTop) {
                $cut = $relative.IndexOf('/')
                if ($cut -lt 0) { continue }
                $relative = $relative.Substring($cut + 1)
            }
            if ($relative.Length -eq 0 -or (Test-IsExcludedPath -Relative $relative)) { continue }

            $extension = [System.IO.Path]::GetExtension($relative).ToLowerInvariant()
            $target = $null
            $key = $null
            $root = $null
            $prefix = if ($sourceExtensions.Contains($extension)) { Find-RootPrefix -Relative $relative -Roots $sourceRoots } else { $null }
            if ($null -ne $prefix) {
                $target = $sourceMap
                $key = Get-FolderKey -Relative $relative.Substring($prefix.Length)
                $root = $prefix.TrimEnd('/')
            }
            elseif ($tallyExtensions.Contains($extension)) {
                $prefix = Find-RootPrefix -Relative $relative -Roots $tallyRoots
                if ($null -ne $prefix) {
                    $target = $tallyMap
                    $key = $prefix.TrimEnd('/')
                    $root = $key
                }
            }
            if ($null -eq $target) { continue }

            [long]$lines = 0
            [long]$nonBlank = 0
            $reader = [System.IO.StreamReader]::new($entry.Open(), [System.Text.Encoding]::UTF8, $true)
            try {
                while ($null -ne ($line = $reader.ReadLine())) {
                    $lines++
                    if (-not [string]::IsNullOrWhiteSpace($line)) { $nonBlank++ }
                }
            }
            finally {
                $reader.Dispose()
            }

            $slot = $root + '|' + $key
            if (-not $target.ContainsKey($slot)) {
                $target[$slot] = [pscustomobject]@{ Name = $key; Root = $root; Files = 0; Lines = [long]0; NonBlank = [long]0; Blank = [long]0; Bytes = [long]0 }
            }
            $folder = $target[$slot]
            $folder.Files++
            $folder.Lines += $lines
            $folder.NonBlank += $nonBlank
            $folder.Blank += $lines - $nonBlank
            $folder.Bytes += $entry.Length
        }
    }
    finally {
        $zip.Dispose()
    }

    $folders = [System.Collections.Generic.List[object]]::new()
    foreach ($map in @($sourceMap, $tallyMap)) {
        $names = Get-OrdinalList -Values @($map.Keys)
        foreach ($name in $names) { $folders.Add($map[$name]) }
    }
    return , $folders
}

function ConvertTo-JsonText {
    param([AllowNull()][string]$Value)

    return '"' + $Value.Replace('\', '\\').Replace('"', '\"') + '"'
}

function Add-FolderText {
    param(
        [Parameter(Mandatory = $true)][System.Text.StringBuilder]$Builder,
        [AllowEmptyCollection()][object[]]$Folders,
        [Parameter(Mandatory = $true)][string]$Indent
    )

    [void]$Builder.Append('"folders": [')
    for ($j = 0; $j -lt $Folders.Count; $j++) {
        $folder = $Folders[$j]
        [void]$Builder.Append($(if ($j -eq 0) { "`n" } else { ",`n" }))
        [void]$Builder.Append(($Indent + '  {{ "name": {0}, "root": {1}, "files": {2}, "lines": {3}, "nonBlank": {4}, "blank": {5}, "bytes": {6} }}' -f
            (ConvertTo-JsonText $folder.Name), (ConvertTo-JsonText $folder.Root), $folder.Files, $folder.Lines, $folder.NonBlank, $folder.Blank, $folder.Bytes))
    }
    [void]$Builder.Append($(if ($Folders.Count -eq 0) { "]`n" } else { "`n$Indent]`n" }))
}

. (Join-Path $PSScriptRoot 'AuditHistory.lineage.ps1')

# The folder row a repository path counts in, as root|name, or $null when no source rule takes it.
function Get-LineageSlot {
    param([Parameter(Mandatory = $true)][string]$Relative)

    if (Test-IsExcludedPath -Relative $Relative) { return $null }
    if (-not $sourceExtensions.Contains([System.IO.Path]::GetExtension($Relative).ToLowerInvariant())) { return $null }
    $prefix = Find-RootPrefix -Relative $Relative -Roots $sourceRoots
    if ($null -eq $prefix) { return $null }
    return $prefix.TrimEnd('/') + '|' + (Get-FolderKey -Relative $Relative.Substring($prefix.Length))
}

# The folder links: renamed source files from one folder to another, kept when they are at least the lineage share
# of the files the old folder held at the version before, or of the files the new folder holds at that version.
function Get-LineageLink {
    param(
        [Parameter(Mandatory = $true)][System.Collections.Generic.List[object]]$Records,
        [Parameter(Mandatory = $true)][System.Collections.Generic.List[object]]$Lineage
    )

    $files = @{}
    foreach ($record in $Records) {
        $map = @{}
        foreach ($folder in @($record.Folders)) { $map[$folder.Root + '|' + $folder.Name] = [int]$folder.Files }
        $files[$record.Version] = $map
    }
    $links = [System.Collections.Generic.List[object]]::new()
    foreach ($step in $Lineage) {
        if (-not $files.ContainsKey($step.Version) -or -not $files.ContainsKey($step.Previous)) { continue }
        $counts = @{}
        foreach ($move in $step.Moves) {
            $from = Get-LineageSlot -Relative $move.From
            $to = Get-LineageSlot -Relative $move.To
            if ($null -eq $from -or $null -eq $to -or $from -ceq $to) { continue }
            $key = $from + "`n" + $to
            $counts[$key] = 1 + $(if ($counts.ContainsKey($key)) { $counts[$key] } else { 0 })
        }
        foreach ($key in (Get-OrdinalList -Values @($counts.Keys))) {
            $pair = $key.Split("`n")
            $count = [int]$counts[$key]
            $before = $files[$step.Previous][$pair[0]]
            $after = $files[$step.Version][$pair[1]]
            $kept = ($null -ne $before -and $before -gt 0 -and $count / $before -ge $lineageShare) -or
                ($null -ne $after -and $after -gt 0 -and $count / $after -ge $lineageShare)
            if ($kept) { $links.Add([pscustomobject]@{ Version = $step.Version; From = $pair[0]; To = $pair[1]; Files = $count }) }
        }
    }
    return , $links
}

# The page data: every record sorted by version under the current rules, in the shape the template reads.
function Get-PageRecordText {
    param(
        [Parameter(Mandatory = $true)][System.Collections.Generic.List[object]]$Records,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][System.Collections.Generic.List[object]]$Links
    )

    $sorted = @($Records | Sort-Object -Property @{ Expression = { [version]$_.Version } })
    $builder = [System.Text.StringBuilder]::new()
    [void]$builder.Append("{`n  `"rules`": " + (ConvertTo-JsonText $rules) + ",`n")
    [void]$builder.Append('  "lineage": { "share": ' + $lineageShare.ToString([System.Globalization.CultureInfo]::InvariantCulture) + ', "links": [')
    for ($i = 0; $i -lt $Links.Count; $i++) {
        $link = $Links[$i]
        [void]$builder.Append($(if ($i -eq 0) { "`n" } else { ",`n" }))
        [void]$builder.Append('    { "version": ' + (ConvertTo-JsonText $link.Version) + ', "from": ' + (ConvertTo-JsonText $link.From) +
            ', "to": ' + (ConvertTo-JsonText $link.To) + ', "files": ' + $link.Files + ' }')
    }
    [void]$builder.Append($(if ($Links.Count -eq 0) { "] },`n" } else { "`n  ] },`n" }))
    [void]$builder.Append('  "versions": [')
    for ($i = 0; $i -lt $sorted.Count; $i++) {
        $record = $sorted[$i]
        [void]$builder.Append($(if ($i -eq 0) { "`n" } else { ",`n" }))
        [void]$builder.Append("    {`n")
        [void]$builder.Append("      `"version`": " + (ConvertTo-JsonText $record.Version) + ",`n")
        [void]$builder.Append("      `"commit`": " + (ConvertTo-JsonText $record.Commit) + ",`n")
        [void]$builder.Append("      `"date`": " + (ConvertTo-JsonText $record.Date) + ",`n")
        [void]$builder.Append("      `"source`": " + (ConvertTo-JsonText $record.Source) + ",`n")
        [void]$builder.Append('      ')
        Add-FolderText -Builder $builder -Folders @($record.Folders) -Indent '      '
        [void]$builder.Append('    }')
    }
    [void]$builder.Append($(if ($sorted.Count -eq 0) { "]`n}`n" } else { "`n  ]`n}`n" }))
    return $builder.ToString()
}

# One version's record file, written under a pending name and moved into place, and left alone when unchanged.
function Save-Record {
    param([Parameter(Mandatory = $true)][object]$Record)

    $builder = [System.Text.StringBuilder]::new()
    [void]$builder.Append("{`n")
    [void]$builder.Append("  `"version`": " + (ConvertTo-JsonText $Record.Version) + ",`n")
    [void]$builder.Append("  `"commit`": " + (ConvertTo-JsonText $Record.Commit) + ",`n")
    [void]$builder.Append("  `"date`": " + (ConvertTo-JsonText $Record.Date) + ",`n")
    [void]$builder.Append("  `"source`": " + (ConvertTo-JsonText $Record.Source) + ",`n")
    [void]$builder.Append("  `"rules`": " + (ConvertTo-JsonText $rules) + ",`n")
    [void]$builder.Append('  ')
    Add-FolderText -Builder $builder -Folders @($Record.Folders) -Indent '  '
    [void]$builder.Append("}`n")
    $text = $builder.ToString()

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

# Every record file counted under the current rules; a missing folder holds no records.
function Read-Record {
    $records = [System.Collections.Generic.List[object]]::new()
    if ($Rebuild -or -not [System.IO.Directory]::Exists($recordsPath)) { return , $records }
    $stale = 0
    $paths = @([System.IO.Directory]::GetFiles($recordsPath, '*.json') | Where-Object { $_.EndsWith('.json', [System.StringComparison]::OrdinalIgnoreCase) })
    foreach ($path in $paths) {
        try {
            $item = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
        }
        catch {
            $stale++
            continue
        }
        if ([string]$item.rules -ne $rules) {
            $stale++
            continue
        }
        $folders = @($item.folders | ForEach-Object {
                [pscustomobject]@{ Name = [string]$_.name; Root = [string]$_.root; Files = [int]$_.files; Lines = [long]$_.lines; NonBlank = [long]$_.nonBlank; Blank = [long]$_.blank; Bytes = [long]$_.bytes }
            })
        $records.Add([pscustomobject]@{
                Version = [string]$item.version
                Commit  = [string]$item.commit
                Date    = [string]$item.date
                Source  = [string]$item.source
                Folders = $folders
            })
    }
    if ($stale -gt 0) {
        Write-Host "Records counted under other rules or unreadable: $stale. They are counted again."
    }
    return , $records
}

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

Write-Host "Versions: $($versionMap.Count). Recorded: $($versionMap.Count - $pendingVersions.Count). To count: $($pendingVersions.Count)."

$failures = [System.Collections.Generic.List[string]]::new()
$index = 0
foreach ($versionCommit in $pendingVersions) {
    $index++
    $work = Join-Path ([System.IO.Path]::GetTempPath()) ('AuditLinesHistory-' + [Guid]::NewGuid().ToString('N'))
    [void][System.IO.Directory]::CreateDirectory($work)
    $zipPath = Join-Path $work 'source.zip'
    try {
        $source = 'local'
        $stripTop = $false
        if ($null -eq $localRoot -or -not (Save-LocalArchive -Root $localRoot -Sha $versionCommit.Sha -ZipPath $zipPath)) {
            if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
            Save-GitHubArchive -Sha $versionCommit.Sha -ZipPath $zipPath
            $source = 'github'
            $stripTop = $true
        }

        $folders = Measure-Archive -ZipPath $zipPath -StripTop $stripTop
        for ($i = $records.Count - 1; $i -ge 0; $i--) {
            if ($records[$i].Version -eq $versionCommit.Version) { $records.RemoveAt($i) }
        }
        $record = [pscustomobject]@{
            Version = $versionCommit.Version
            Commit  = $versionCommit.Sha
            Date    = $versionCommit.Date.ToString($dateFormat, [System.Globalization.CultureInfo]::InvariantCulture)
            Source  = $source
            Folders = @($folders)
        }
        Save-Record -Record $record
        $records.Add($record)
        $total = ($folders | Measure-Object -Property Lines -Sum).Sum
        Write-Host ("[{0}/{1}] {2} {3} lines in {4} folders ({5})" -f $index, $pendingVersions.Count, $versionCommit.Version, $total, $folders.Count, $source)
    }
    catch {
        $failures.Add("$($versionCommit.Version): $($_.Exception.Message)")
        Write-Warning "Could not count $($versionCommit.Version): $($_.Exception.Message)"
    }
    finally {
        if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue }
    }
}

Write-Host "Records: $recordsPath"

$versionCount = $records.Count
if ($versionCount -eq 0) {
    throw "The records hold no versions: $recordsPath"
}
$lineage = Update-Lineage -Root $localRoot -Folder $lineagePath -VersionMap $versionMap
$links = Get-LineageLink -Records $records -Lineage $lineage
Write-Host "Lineage links: $($links.Count) at a share of $lineageShare or more."
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
$auditPage = 'AuditLines-' + $version + '.html'
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
