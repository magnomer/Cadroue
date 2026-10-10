#requires -Version 5.1
<#
.SYNOPSIS
    Download selected version-labelled source snapshots from GitHub.
.DESCRIPTION
    Reads the complete commit history of the configured repository through the
    GitHub REST API. A commit is treated as a version when the first line of
    its commit message begins with a three-part version such as 2.0.6920.

    Each matching commit is downloaded as a GitHub source ZIP and stored as:

        snapshots/<version>/<project>-V<version>.zip

    Existing valid archives are skipped. A corrupt existing archive is
    replaced automatically. Use -Rebuild to replace every existing archive.

    Set GITHUB_TOKEN in the environment when authenticated API access or a
    higher GitHub API rate limit is needed.

    Every project-specific value - project name, repository, threshold - lives
    in snapshot.json. This script carries none, so the file is identical in
    every project at the same generation.
.PARAMETER Repository
    GitHub repository in owner/name form. Defaults to the repository in snapshot.json.
.PARAMETER SnapshotDirectory
    Destination for version directories and source ZIPs. Defaults to snapshots/.
.PARAMETER Rebuild
    Replace every existing archive, including archives that are already valid.
.PARAMETER Threshold
    Save the exclusive version threshold to snapshot.json. Use 0 to download
    every version. The saved threshold is used by later runs.
.PARAMETER Help
    Display this help and exit without contacting GitHub. The alias -? is supported.
.EXAMPLE
    GitDownload
    Download missing or invalid archives from the default repository.
.EXAMPLE
    GitDownload -Rebuild
    Redownload every version archive.
.EXAMPLE
    GitDownload -Threshold 2.0.6920
    Save the threshold and download versions newer than 2.0.6920.
#>
# GITDOWNLOAD - SNAPSHOT GENERATION 1.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the executed outcome changes.
# A generation names how the family archives the source, fetches archives from GitHub, cleans the
# tree and maintains the code index. Two projects on the same generation produce the same archive
# from the same tree and lay out snapshots/ the same, whatever else differs between the files.
# Generation 1: Zip.ps1 archives the Git-visible file set as snapshots/<version>/<project>-V<version>.zip,
# refuses to overwrite and refuses a tree that no longer matches an executable snapshot; GitDownload.ps1
# fetches every version-labelled commit above the threshold from GitHub into the same layout;
# Clean.ps1 removes obj/, bin/, TestResults/ and WPF temp projects outside the protected trees and
# maintains the editor exclusions.
# Every project-specific value lives in snapshot.json, so this file is identical in every project at
# this generation.
[CmdletBinding()]
param(
    [string]$Repository,
    [string]$SnapshotDirectory,
    [string]$Threshold,
    [switch]$Rebuild,
    [Alias('?')]
    [switch]$Help
)

if ($Help) {
    @'
NAME
    GitDownload.ps1

SYNOPSIS
    Download version-labelled source snapshots from GitHub.

SYNTAX
    GitDownload [-Repository <owner/name>] [-SnapshotDirectory <path>]
                      [-Threshold <version|0>] [-Rebuild] [-Help]

OPTIONS
    -Repository <owner/name>
        GitHub repository to scan. Default: the repository in snapshot.json.

    -SnapshotDirectory <path>
        Destination for version directories and source ZIPs. Default: snapshots/.

    -Rebuild
        Replace every archive, including archives that are already valid.

    -Threshold <version|0>
        Save an exclusive version threshold in snapshot.json. Only versions
        newer than the threshold are downloaded. Use 0 to download all versions.

    -Help, -?
        Display this help and exit without contacting GitHub.

EXAMPLES
    GitDownload
        Download missing or invalid archives from the default repository.

    GitDownload -Rebuild
        Redownload every version archive.

    GitDownload -Threshold 2.0.6920
        Save the threshold and download versions newer than 2.0.6920.

NOTES
    Set GITHUB_TOKEN for authenticated access or a higher API rate limit.
'@ | Write-Host
    exit 0
}

Write-Host 'GITDOWNLOAD - SNAPSHOT GENERATION 1' -ForegroundColor Blue

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# GitHub requires TLS 1.2. Windows PowerShell 5.1 may otherwise negotiate an
# older protocol on machines whose .NET defaults have not been updated.
[System.Net.ServicePointManager]::SecurityProtocol =
    [System.Net.ServicePointManager]::SecurityProtocol -bor
    [System.Net.SecurityProtocolType]::Tls12

function Read-SnapshotConfig {
    $configPath = Join-Path $PSScriptRoot 'snapshot.json'
    if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
        throw "The snapshot configuration was not found: $configPath"
    }

    try {
        $config = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The snapshot configuration is not valid JSON: $configPath`n$($_.Exception.Message)"
    }

    foreach ($key in @('generation', 'project', 'solution', 'repository', 'threshold')) {
        if (-not ($config.PSObject.Properties.Name -contains $key)) {
            throw "The snapshot configuration must contain a $key property: $configPath"
        }
    }

    if ([int]$config.generation -ne 1) {
        throw "The snapshot configuration is generation $($config.generation); this script is generation 1."
    }

    return $config
}

$config = Read-SnapshotConfig
$configurationPath = Join-Path $PSScriptRoot 'snapshot.json'
if ([string]::IsNullOrWhiteSpace($Repository)) {
    $Repository = [string]$config.repository
}

if ($Repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') {
    throw "Repository must use owner/name form: $Repository"
}

$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($SnapshotDirectory)) {
    $SnapshotDirectory = Join-Path $projectRoot 'snapshots'
}

if ([System.IO.Path]::IsPathRooted($SnapshotDirectory)) {
    $snapshotRoot = [System.IO.Path]::GetFullPath($SnapshotDirectory)
}
else {
    $snapshotRoot = [System.IO.Path]::GetFullPath((Join-Path $projectRoot $SnapshotDirectory))
}

$versionPattern = '^(?<version>(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*))(?:\s|$)'
$thresholdPattern = '^(?:0|(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*))$'
$pageSize = 100
$repositoryParts = $Repository.Split('/')
$repositoryOwner = $repositoryParts[0]
$repositoryName = $repositoryParts[1]

$headers = @{
    Accept                 = 'application/vnd.github+json'
    'User-Agent'           = "$($config.project)-GitDownload.ps1"
    'X-GitHub-Api-Version' = '2022-11-28'
}
$downloadHeaders = @{
    'User-Agent' = "$($config.project)-GitDownload.ps1"
}

if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_TOKEN)) {
    $headers['Authorization'] = "Bearer $($env:GITHUB_TOKEN.Trim())"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Test-ThresholdValue {
    param([string]$Value)

    return -not [string]::IsNullOrWhiteSpace($Value) -and $Value -match $thresholdPattern
}

function Save-ThresholdConfiguration {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    # Written by hand because ConvertTo-Json lays the file out differently in Windows PowerShell 5.1
    # and in pwsh 7. Every value in snapshot.json is a string or a whole number.
    $config.threshold = $Value
    $lines = foreach ($property in $config.PSObject.Properties) {
        $text = if ($property.Value -is [string]) {
            '"' + $property.Value.Replace('\', '\\').Replace('"', '\"') + '"'
        }
        else {
            [string]$property.Value
        }

        '  "{0}": {1}' -f $property.Name, $text
    }

    [System.IO.File]::WriteAllText(
        $configurationPath,
        "{`n" + (@($lines) -join ",`n") + "`n}`n",
        (New-Object System.Text.UTF8Encoding($false)))
}

if ($PSBoundParameters.ContainsKey('Threshold')) {
    $Threshold = $Threshold.Trim()
    if (-not (Test-ThresholdValue -Value $Threshold)) {
        throw "Threshold must be 0 or a three-part version such as 2.0.6920: $Threshold"
    }

    Save-ThresholdConfiguration -Value $Threshold
    Write-Host "Saved threshold $Threshold to $configurationPath"
}
else {
    $Threshold = ([string]$config.threshold).Trim()
    if (-not (Test-ThresholdValue -Value $Threshold)) {
        throw "The threshold in $configurationPath must be 0 or a three-part version such as 2.0.6920."
    }
}

function Get-ExceptionDetail {
    param(
        [Parameter(Mandatory = $true)]
        [System.Management.Automation.ErrorRecord]$ErrorRecord
    )

    $detail = $ErrorRecord.Exception.Message
    $response = $null
    if ($ErrorRecord.Exception.PSObject.Properties.Name -contains 'Response') {
        $response = $ErrorRecord.Exception.Response
    }

    if ($null -eq $response) {
        return $detail
    }

    try {
        $statusCode = [int]$response.StatusCode
        $statusText = [string]$response.StatusDescription
        if (-not [string]::IsNullOrWhiteSpace($statusText)) {
            $detail = "HTTP $statusCode $statusText. $detail"
        }
        else {
            $detail = "HTTP $statusCode. $detail"
        }
    }
    catch {
        # Preserve the original exception message when the response object does
        # not expose the usual HTTP status properties.
    }

    try {
        $remaining = [string]$response.Headers['X-RateLimit-Remaining']
        $reset = [string]$response.Headers['X-RateLimit-Reset']
        if ($remaining -eq '0' -and $reset -match '^\d+$') {
            $resetTime = [DateTimeOffset]::FromUnixTimeSeconds([long]$reset).ToLocalTime()
            $detail += " GitHub API rate limit resets at $($resetTime.ToString('yyyy-MM-dd HH:mm:ss zzz'))."
        }
    }
    catch {
        # Rate-limit headers are optional.
    }

    return $detail
}

function Get-VersionCommit {
    $versionByName = @{}
    $page = 1
    $unversionedCount = 0
    $duplicateCount = 0

    while ($true) {
        $uri = "https://api.github.com/repos/$Repository/commits?per_page=$pageSize&page=$page"
        Write-Host "Reading GitHub commit page $page..."

        try {
            $response = Invoke-RestMethod `
                -Uri $uri `
                -Method Get `
                -Headers $headers `
                -UseBasicParsing
        }
        catch {
            $detail = Get-ExceptionDetail -ErrorRecord $_
            throw "Could not read commits from $Repository. $detail"
        }

        $commits = @($response)
        if ($commits.Count -eq 0) {
            break
        }

        foreach ($commit in $commits) {
            $message = [string]$commit.commit.message
            $title = ($message -split '\r?\n', 2)[0].Trim()
            $match = [System.Text.RegularExpressions.Regex]::Match($title, $versionPattern)

            if (-not $match.Success) {
                $unversionedCount++
                continue
            }

            $version = $match.Groups['version'].Value
            if ($versionByName.ContainsKey($version)) {
                $duplicateCount++
                Write-Warning "More than one commit begins with version $version. Keeping the newest commit $($versionByName[$version].Sha)."
                continue
            }

            # pwsh 7 reads an ISO date in JSON as a DateTime, while Windows PowerShell 5.1 keeps the text.
            $dateValue = $commit.commit.committer.date
            if ([string]::IsNullOrWhiteSpace([string]$dateValue)) {
                $dateValue = $commit.commit.author.date
            }

            $commitDate = [DateTimeOffset]::MinValue
            if ($dateValue -is [datetime]) {
                $commitDate = [DateTimeOffset]::new($dateValue)
            }
            elseif (-not [string]::IsNullOrWhiteSpace([string]$dateValue)) {
                [void][DateTimeOffset]::TryParse([string]$dateValue, [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::AssumeUniversal, [ref]$commitDate)
            }

            $versionByName[$version] = [PSCustomObject]@{
                Version = $version
                Sha     = [string]$commit.sha
                Date    = $commitDate
                Title   = $title
            }
        }

        if ($commits.Count -lt $pageSize) {
            break
        }

        $page++
    }

    $versions = @($versionByName.Values | Sort-Object -Property Date, @{ Expression = { [version]$_.Version } })
    if ($versions.Count -eq 0) {
        throw "No version-labelled commits were found in $Repository."
    }

    return [PSCustomObject]@{
        Versions          = $versions
        UnversionedCount  = $unversionedCount
        DuplicateCount    = $duplicateCount
    }
}

function Test-ZipArchive {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $false
    }

    try {
        $file = Get-Item -LiteralPath $Path
        if ($file.Length -le 0) {
            return $false
        }

        $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
        try {
            return $archive.Entries.Count -gt 0
        }
        finally {
            $archive.Dispose()
        }
    }
    catch {
        return $false
    }
}

function Save-VersionArchive {
    param(
        [Parameter(Mandatory = $true)]
        [object]$VersionCommit
    )

    $versionDirectory = Join-Path $snapshotRoot $VersionCommit.Version
    $archiveName = "$($config.project)-V$($VersionCommit.Version).zip"
    $archivePath = Join-Path $versionDirectory $archiveName

    if ((Test-Path -LiteralPath $archivePath -PathType Leaf) -and -not $Rebuild) {
        if (Test-ZipArchive -Path $archivePath) {
            return 'Existing'
        }

        Write-Warning "Existing archive is invalid and will be replaced: $archivePath"
    }

    [System.IO.Directory]::CreateDirectory($versionDirectory) | Out-Null

    $temporaryPath = Join-Path $versionDirectory (".$archiveName.download-$PID-$([Guid]::NewGuid().ToString('N'))")
    # Codeload does not consume one REST API request per archive, so an
    # unauthenticated run can download more than GitHub's 60-request hourly
    # REST limit. The REST API is used only to enumerate commit history.
    $archiveUri = "https://codeload.github.com/$repositoryOwner/$repositoryName/zip/$($VersionCommit.Sha)"

    # Windows PowerShell 5.1 redraws its progress bar per received block, which slows a large
    # download many times over. The run's own progress bar is kept; only this call is silenced.
    $ProgressPreference = 'SilentlyContinue'
    try {
        Invoke-WebRequest `
            -Uri $archiveUri `
            -Method Get `
            -Headers $downloadHeaders `
            -UseBasicParsing `
            -OutFile $temporaryPath

        if (-not (Test-ZipArchive -Path $temporaryPath)) {
            throw 'GitHub returned an empty or invalid ZIP archive.'
        }

        if (Test-Path -LiteralPath $archivePath) {
            Remove-Item -LiteralPath $archivePath -Force
        }

        Move-Item -LiteralPath $temporaryPath -Destination $archivePath
        return 'Downloaded'
    }
    catch {
        if (Test-Path -LiteralPath $temporaryPath) {
            Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
        }

        $detail = Get-ExceptionDetail -ErrorRecord $_
        throw $detail
    }
}

[System.IO.Directory]::CreateDirectory($snapshotRoot) | Out-Null

$result = Get-VersionCommit
$allVersions = @($result.Versions)
if ($Threshold -eq '0') {
    $versions = $allVersions
}
else {
    $thresholdVersion = [version]$Threshold
    $versions = @($allVersions | Where-Object { [version]$_.Version -gt $thresholdVersion })
}
$total = $versions.Count
$thresholdSkippedCount = $allVersions.Count - $total
$downloadedCount = 0
$existingCount = 0
$failures = New-Object 'System.Collections.Generic.List[string]'

Write-Host ""
Write-Host "Repository: $Repository"
Write-Host "Threshold:  $Threshold"
Write-Host "Versions:   $total"
Write-Host "Destination: $snapshotRoot"
Write-Host ""

$activity = "Downloading $($config.project) Git snapshots"
for ($index = 0; $index -lt $total; $index++) {
    $versionCommit = $versions[$index]
    $position = $index + 1
    $status = "$position of $total - $($versionCommit.Version)"
    $percent = [int](($position / [double]$total) * 100)

    Write-Progress -Activity $activity -Status $status -PercentComplete $percent
    Write-Host "[$position/$total] $($versionCommit.Version)  $($versionCommit.Sha.Substring(0, 10))"

    try {
        $state = Save-VersionArchive -VersionCommit $versionCommit
        if ($state -eq 'Existing') {
            $existingCount++
            Write-Host '  Existing archive kept.'
        }
        else {
            $downloadedCount++
            $savedPath = Join-Path (Join-Path $snapshotRoot $versionCommit.Version) "$($config.project)-V$($versionCommit.Version).zip"
            Write-Host "  Saved: $savedPath"
        }
    }
    catch {
        $message = "$($versionCommit.Version): $($_.Exception.Message)"
        $failures.Add($message)
        Write-Warning $message
    }
}

Write-Progress -Activity $activity -Completed

Write-Host ""
Write-Host "Downloaded: $downloadedCount"
Write-Host "Existing:   $existingCount"
Write-Host "Versions at or below threshold skipped: $thresholdSkippedCount"
Write-Host "Unversioned commits skipped: $($result.UnversionedCount)"
if ($result.DuplicateCount -gt 0) {
    Write-Host "Duplicate version commits skipped: $($result.DuplicateCount)"
}

if ($failures.Count -gt 0) {
    $failureText = [string]::Join("`n - ", $failures)
    throw "$($failures.Count) version archive(s) could not be downloaded:`n - $failureText"
}

Write-Host "Complete: $snapshotRoot"
