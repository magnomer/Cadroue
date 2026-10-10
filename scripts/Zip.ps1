#requires -Version 5.1
<#
.SYNOPSIS
    Create the source archive belonging to a snapshot of the project.
.DESCRIPTION
    Uses Git's visible-file set (tracked files plus untracked non-ignored files)
    to create <project>-V<version>.zip inside snapshots/<version>/. The project
    name is read from snapshot.json; this script carries no project-specific
    value, so the file is identical in every project at the same generation.

    When run directly, the script creates snapshots/<version>/ when necessary
    and writes the source ZIP there. When that folder already holds a
    snapshot.json, its version and sourceFingerprint must match the current source.
    Without that metadata the archive is written unchecked.

    No script in scripts/ calls this one. It is run directly, by its bare name
    zip. Run.ps1 delegates to TimeMachine.ps1, which never calls Zip.ps1.
.PARAMETER SnapshotStagingDirectory
    Writes the archive into a staging folder directly below snapshots/ instead.
    The folder must exist, be named .<version>.staging-*, and hold snapshot.json.
    No caller passes it today. It is kept for direct use.
.PARAMETER FingerprintOnly
    Calculate and print the current source fingerprint without creating an archive.
    No caller passes it today. It is kept for direct use.
.PARAMETER Help
    Display this help and exit without reading source files or writing an archive.
    The alias -? is supported.
.EXAMPLE
    zip
    Create the current version's standalone source archive.
.EXAMPLE
    zip -FingerprintOnly
    Print the fingerprint of the current Git-visible source set.
#>
# ZIP - SNAPSHOT GENERATION 1.
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
    [string]$SnapshotStagingDirectory,
    [switch]$FingerprintOnly,
    [Alias('?')]
    [switch]$Help
)

if ($Help) {
    @'
NAME
    Zip.ps1

SYNOPSIS
    Create or fingerprint the source archive for the current project version.

SYNTAX
    zip [-SnapshotStagingDirectory <path>] [-FingerprintOnly] [-Help]

OPTIONS
    -SnapshotStagingDirectory <path>
        Staging directory directly below snapshots/. No caller passes it today.
        Omit it for a standalone archive under snapshots/<version>/.

    -FingerprintOnly
        Print the current Git-visible source fingerprint without creating a ZIP.

    -Help, -?
        Display this help and exit without reading source files or writing a ZIP.

EXAMPLES
    zip
        Create the current version's standalone source archive.

    zip -FingerprintOnly
        Print the fingerprint of the current Git-visible source set.
'@ | Write-Host
    exit 0
}

Write-Host 'ZIP - SNAPSHOT GENERATION 1' -ForegroundColor Blue

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$versionFile = Join-Path $root 'version.json'
$snapshotsRoot = [System.IO.Path]::GetFullPath((Join-Path $root 'snapshots'))
$versionPattern = '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$'

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

function Read-ProjectVersion {
    if (-not (Test-Path -LiteralPath $versionFile -PathType Leaf)) {
        throw "version.json was not found: $versionFile"
    }

    $data = Get-Content -LiteralPath $versionFile -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not ($data.PSObject.Properties.Name -contains 'current-version')) {
        throw 'version.json must contain a current-version property.'
    }

    $version = [string]$data.'current-version'
    if ($version -notmatch $versionPattern) {
        throw "Invalid version format in version.json: $version"
    }

    return $version
}

function Get-GitVisiblePath {
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = 'git'
    $startInfo.Arguments = 'ls-files --cached --others --exclude-standard -z'
    $startInfo.WorkingDirectory = $root
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true

    try {
        $utf8 = New-Object System.Text.UTF8Encoding($false)
        $startInfo.StandardOutputEncoding = $utf8
        $startInfo.StandardErrorEncoding = $utf8
    }
    catch {
        # Older Windows PowerShell/.NET combinations may not expose these
        # setters. Git paths consisting of ordinary project characters still
        # decode through the process default encoding.
    }

    try {
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $startInfo
        if (-not $process.Start()) {
            throw 'Git could not be started.'
        }
    }
    catch [System.ComponentModel.Win32Exception] {
        throw 'Git was not found. Install Git or add git.exe to PATH before running Zip.ps1.'
    }

    try {
        $errorTask = $process.StandardError.ReadToEndAsync()
        $output = $process.StandardOutput.ReadToEnd()
        $errorText = $errorTask.GetAwaiter().GetResult()
        $process.WaitForExit()

        if ($process.ExitCode -ne 0) {
            $detail = $errorText.Trim()
            if ([string]::IsNullOrWhiteSpace($detail)) {
                $detail = "git exited with code $($process.ExitCode)."
            }

            throw "Could not obtain the Git-visible file list. $detail"
        }

        # Do not use String.Split([char]0, StringSplitOptions) here. Under
        # Windows PowerShell 5.1, overload resolution can select the count
        # overload instead of the options overload, leaving the trailing NUL
        # as an empty path entry. Parse the NUL-delimited stream explicitly.
        $paths = New-Object 'System.Collections.Generic.List[string]'
        $start = 0

        for ($index = 0; $index -lt $output.Length; $index++) {
            if ($output[$index] -ne [char]0) {
                continue
            }

            $length = $index - $start
            if ($length -gt 0) {
                $paths.Add($output.Substring($start, $length))
            }

            $start = $index + 1
        }

        # Git normally terminates every entry with NUL, but retain a final
        # unterminated entry defensively. Empty entries are never returned.
        if ($start -lt $output.Length) {
            $paths.Add($output.Substring($start))
        }

        return $paths.ToArray()
    }
    finally {
        $process.Dispose()
    }
}

function Get-SnapshotSourceFile {
    $rootPrefix = $root.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar

    $files = foreach ($gitPath in (Get-GitVisiblePath)) {
        # Belt-and-suspenders guard: a Git path can never be empty, and an
        # empty value must not be resolved to the project root.
        if ($null -eq $gitPath -or $gitPath.Length -eq 0) {
            continue
        }

        $relativePath = $gitPath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        $fullPath = [System.IO.Path]::GetFullPath((Join-Path $root $relativePath))

        if (-not $fullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Git returned a path outside the project root: $gitPath"
        }

        # `git ls-files --cached` includes tracked paths deleted from the
        # working tree. A source snapshot represents the current working tree,
        # so omit those paths; moved files are included separately through the
        # untracked-file portion of the Git query.
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            continue
        }

        [PSCustomObject]@{
            FullPath = $fullPath
            Entry    = $gitPath.Replace('\', '/')
        }
    }

    # Sort-Object compares by culture, which Windows PowerShell 5.1 and pwsh 7 resolve differently
    # ('-' against '/'), so the fingerprint would depend on the edition. The order is ordinal instead.
    $unique = New-Object 'System.Collections.Generic.SortedDictionary[string,object]' ([System.StringComparer]::Ordinal)
    foreach ($file in $files) {
        if (-not $unique.ContainsKey($file.Entry)) {
            $unique.Add($file.Entry, $file)
        }
    }

    $result = @($unique.Values)
    if ($result.Count -eq 0) {
        throw 'Git returned no files to archive.'
    }

    return $result
}

function Get-SnapshotSourceFingerprint {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Files
    )

    # Get-FileHash is left out: Windows PowerShell 5.1 defines it in a script module that fails to
    # load when the session inherits the module path of pwsh 7.
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $records = foreach ($file in $Files) {
            $stream = [System.IO.File]::OpenRead($file.FullPath)
            try {
                $hash = ([System.BitConverter]::ToString($sha256.ComputeHash($stream))).Replace('-', '').ToLowerInvariant()
            }
            finally {
                $stream.Dispose()
            }
            "$($file.Entry)`0$hash"
        }

        $payload = [System.Text.Encoding]::UTF8.GetBytes([string]::Join("`n", $records))
        $digest = $sha256.ComputeHash($payload)
        return ([System.BitConverter]::ToString($digest)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

function Read-SnapshotMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Directory,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedVersion
    )

    $metadataPath = Join-Path $Directory 'snapshot.json'
    if (-not (Test-Path -LiteralPath $metadataPath -PathType Leaf)) {
        throw "Snapshot metadata was not found: $metadataPath`nCreate the snapshot through Run.ps1 so its binary and source state are recorded together."
    }

    $metadata = Get-Content -LiteralPath $metadataPath -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($property in @('version', 'sourceFingerprint')) {
        if (-not ($metadata.PSObject.Properties.Name -contains $property)) {
            throw "snapshot.json must contain a $property property: $metadataPath"
        }
    }

    if ([string]$metadata.version -ne $ExpectedVersion) {
        throw "Snapshot metadata version '$($metadata.version)' does not match version.json '$ExpectedVersion'."
    }

    $fingerprint = [string]$metadata.sourceFingerprint
    if ($fingerprint -notmatch '^[0-9a-fA-F]{64}$') {
        throw "snapshot.json contains an invalid sourceFingerprint: $metadataPath"
    }

    return $metadata
}

$config = Read-SnapshotConfig
$version = Read-ProjectVersion
$initialFiles = @(Get-SnapshotSourceFile)
$initialFingerprint = Get-SnapshotSourceFingerprint -Files $initialFiles

if ($FingerprintOnly) {
    Write-Output $initialFingerprint
    return
}

function Resolve-SnapshotDestination {
    param(
        [string]$StagingDirectory,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedVersion
    )

    $finalDirectory = [System.IO.Path]::GetFullPath((Join-Path $snapshotsRoot $ExpectedVersion))

    # Standalone Zip.ps1 has exactly one destination: snapshots/<version>.
    if ([string]::IsNullOrWhiteSpace($StagingDirectory)) {
        return $finalDirectory
    }

    # The only override is a staging directory named by the caller.
    # It must be an immediate child of snapshots and match the current version.
    if ([System.IO.Path]::IsPathRooted($StagingDirectory)) {
        $candidate = [System.IO.Path]::GetFullPath($StagingDirectory)
    }
    else {
        $candidate = [System.IO.Path]::GetFullPath((Join-Path $root $StagingDirectory))
    }

    $candidate = $candidate.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $snapshots = $snapshotsRoot.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)

    $candidateParent = [System.IO.Path]::GetDirectoryName($candidate)
    $candidateLeaf = [System.IO.Path]::GetFileName($candidate)
    $stagingPrefix = ".$ExpectedVersion.staging-"

    if (-not [string]::Equals($candidateParent, $snapshots, [System.StringComparison]::OrdinalIgnoreCase) -or
        -not $candidateLeaf.StartsWith($stagingPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "-SnapshotStagingDirectory is reserved for Run.ps1 and must name the current version's staging directory directly below: $snapshotsRoot`nRefusing destination: $candidate"
    }

    return $candidate
}

$destination = Resolve-SnapshotDestination `
    -StagingDirectory $SnapshotStagingDirectory `
    -ExpectedVersion $version

# Defensive invariant: the final archive path may never be the project root.
$destinationParent = [System.IO.Path]::GetDirectoryName($destination.TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar,
    [System.IO.Path]::AltDirectorySeparatorChar))
if ([string]::Equals($destination, $root, [System.StringComparison]::OrdinalIgnoreCase) -or
    [string]::Equals($destinationParent, $root, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Internal destination error: Zip.ps1 may not write an archive at the project root. Resolved destination: $destination"
}

$stagingMode = -not [string]::IsNullOrWhiteSpace($SnapshotStagingDirectory)

if (-not (Test-Path -LiteralPath $destination -PathType Container)) {
    if ($stagingMode) {
        throw "Snapshot staging directory was not found: $destination"
    }

    New-Item -ItemType Directory -Path $destination -Force | Out-Null
}

$metadataPath = Join-Path $destination 'snapshot.json'
if (Test-Path -LiteralPath $metadataPath -PathType Leaf) {
    $metadata = Read-SnapshotMetadata -Directory $destination -ExpectedVersion $version
    $expectedFingerprint = ([string]$metadata.sourceFingerprint).ToLowerInvariant()
    if ($initialFingerprint -ne $expectedFingerprint) {
        throw "The current source no longer matches the executable snapshot in $destination.`nIncrease the version and run Run.ps1 again."
    }
}
elseif ($stagingMode) {
    throw "Snapshot metadata was not found in the run staging directory: $metadataPath"
}

$archiveName = "$($config.project)-V$version.zip"
$archivePath = [System.IO.Path]::GetFullPath((Join-Path $destination $archiveName))
$snapshotPrefix = $snapshotsRoot.TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar,
    [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
if (-not $archivePath.StartsWith($snapshotPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Internal destination error: archive path is outside snapshots: $archivePath"
}

$tempPath = Join-Path $destination ('.' + $archiveName + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')

if (Test-Path -LiteralPath $archivePath) {
    throw "Snapshot archive already exists: $archivePath`nVersioned snapshots are immutable."
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$stream = $null
$archive = $null

try {
    try {
        $stream = [System.IO.File]::Open(
            $tempPath,
            [System.IO.FileMode]::CreateNew,
            [System.IO.FileAccess]::Write,
            [System.IO.FileShare]::None)

        $archive = New-Object System.IO.Compression.ZipArchive(
            $stream,
            [System.IO.Compression.ZipArchiveMode]::Create,
            $false)

        foreach ($file in $initialFiles) {
            if (-not (Test-Path -LiteralPath $file.FullPath -PathType Leaf)) {
                throw "A source file disappeared while the archive was being created: $($file.FullPath)"
            }

            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive,
                $file.FullPath,
                $file.Entry,
                [System.IO.Compression.CompressionLevel]::Optimal)
        }
    }
    finally {
        if ($null -ne $archive) {
            $archive.Dispose()
        }

        if ($null -ne $stream) {
            $stream.Dispose()
        }
    }

    $finalFiles = @(Get-SnapshotSourceFile)
    $finalFingerprint = Get-SnapshotSourceFingerprint -Files $finalFiles
    if ($finalFingerprint -ne $initialFingerprint) {
        throw 'The source changed while its snapshot archive was being created. The incomplete archive was discarded.'
    }

    Move-Item -LiteralPath $tempPath -Destination $archivePath
}
catch {
    if (Test-Path -LiteralPath $tempPath) {
        Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
    }

    throw
}

$archiveInfo = Get-Item -LiteralPath $archivePath
Write-Host "Archive: $archivePath" -ForegroundColor Green
Write-Host "Files:   $($initialFiles.Count)"
Write-Host "Size:    $([Math]::Round($archiveInfo.Length / 1MB, 2)) MB"
