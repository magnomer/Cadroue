<#
.SYNOPSIS
    Publish a Debug build and launch it from the debug folder.
.DESCRIPTION
    Debug runs are repeatable development runs and are not preserved under
    snapshots/. Every intermediate file, the MSBuild artifacts included, lives
    under <debug>/temp, so a debug build never shares a folder with Build.ps1 or
    Run.ps1. The temp folder is removed once the publish succeeds and kept for
    inspection when it fails. The executable is launched detached with the debug
    folder as its working directory; the console returns at once.

    Every project-specific value - project file, executable, runtime, publish
    properties, folder names - lives in Execution.json. This script carries
    none, so the file is identical in every project at the same generation.
.PARAMETER Runtime
    Runtime identifier for the .NET publish. Defaults to the configured publish runtime.
.PARAMETER Help
    Display this help and exit without publishing or launching. The alias -? is supported.
.EXAMPLE
    debug
    Publish and launch the Debug build on the configured runtime.
.EXAMPLE
    debug -Runtime win-arm64
    Publish and launch a Windows ARM64 Debug build.
#>
#requires -Version 5.1
# DEBUG - EXECUTION GENERATION 2.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the executed outcome changes.
# A generation names what the family builds, where it installs, and what it launches. Two projects on
# the same generation publish the same way and their folders lay out the same, whatever else differs
# between the files. A publish flag, a destination, a temp isolation rule, or a launch step added,
# removed, or changed is a new generation; wording, plumbing, and refactoring leave it alone.
# Generation 2: the family lives in scripts/ with Execution.json beside it and is invoked by bare name
# through the dispatchers Install.ps1 installs; current source publishes Release, self-contained, with
# the configured properties into run/, build/, or snapshots/<version>/; a historical version climbs
# the ladder - snapshot executable, local commit, fetched commit, nearest lower - and a commit is
# exported and published into snapshots/<version>/; debug publishes Debug into debug/; when a live
# process runs from the install folder the build goes into an eight-digit slot inside it instead, and
# slots nothing runs from are reaped first; every launch is written to the ledger so Stop.ps1 can end
# it; every intermediate file lives under <work root>/temp, removed on success and kept on failure.
# Every project-specific value lives in Execution.json, so this file is identical in every project at
# this generation.
[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z]+-[A-Za-z0-9_-]+$')]
    [string]$Runtime,
    [Alias('?')]
    [switch]$Help
)

if ($Help) {
    @'
NAME
    Debug.ps1

SYNOPSIS
    Publish a Debug build and launch it from the debug folder.

SYNTAX
    debug [-Runtime <runtime>] [-Help]

OPTIONS
    -Runtime <runtime>
        .NET runtime identifier. Defaults to the configured publish runtime.

    -Help, -?
        Display this help and exit without publishing or launching.

EXAMPLES
    debug
        Publish and launch the Debug build on the configured runtime.

    debug -Runtime win-arm64
        Publish and launch a Windows ARM64 Debug build.
'@ | Write-Host
    exit 0
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $PSScriptRoot 'Execution.config.ps1')

Write-ExecutionGeneration -Script 'Debug.ps1'
$config = Read-ExecutionConfig -ProjectRoot $root

if ([string]::IsNullOrEmpty($Runtime)) {
    $Runtime = $config.publish.runtime
}

$started = Get-Date
$projectName = $config.project
$version = Read-ExecutionVersion -ProjectRoot $root -Config $config -Key $config.version.currentKey
$clean = Join-Path $PSScriptRoot 'Clean.ps1'
$project = Join-ExecutionPath -ProjectRoot $root -Relative $config.source.project
$debugDir = Join-ExecutionPath -ProjectRoot $root -Relative $config.layout.debug
$tempDir = Join-ExecutionPath -ProjectRoot $debugDir -Relative $config.layout.temp
$publishDir = Join-Path $tempDir 'publish'
$artifactsDir = Join-Path $tempDir 'artifacts'
$executableName = $config.source.executable
$installDir = $debugDir
$exe = Join-Path $debugDir $executableName
$publishProperties = @($config.publish.properties | ForEach-Object { "-p:$_" })
$selfContained = $config.publish.selfContained.ToString().ToLowerInvariant()
$publish = $null
$failure = $null
$outcome = $null
$processId = 0

try {
    if (-not (Test-Path -LiteralPath $clean -PathType Leaf)) {
        throw "Clean.ps1 was not found: $clean"
    }

    if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
        throw "The project file was not found: $project"
    }

    if (Test-Path -LiteralPath $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    Set-ExecutionDotnetQuiet
    try {
        $publish = Invoke-ExecutionDotnet -Arguments (@(
            'publish', $project,
            '--configuration', 'Debug',
            '--runtime', $Runtime,
            '--self-contained', $selfContained,
            '--output', $publishDir,
            '--artifacts-path', $artifactsDir) +
            $publishProperties)
    }
    finally {
        try {
            & $clean -TempOnly
        }
        catch {
            Write-Warning "Temporary cleanup failed: $($_.Exception.Message)"
        }
    }

    if ($publish.ExitCode -ne 0) {
        Write-Host "Intermediate files were kept for inspection: $tempDir" -ForegroundColor Yellow
        throw "dotnet publish failed with exit code $($publish.ExitCode)."
    }

    $publishedExe = Join-Path $publishDir $executableName
    if (-not (Test-Path -LiteralPath $publishedExe -PathType Leaf)) {
        throw "Publish reported success but the debug executable is missing: $publishedExe"
    }

    # A live debug instance keeps its folder; the fresh build then goes into a slot beside it.
    $installDir = Resolve-ExecutionSlot -ProjectRoot $root -Config $config -Folder $debugDir
    $exe = Join-Path $installDir $executableName
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null

    # Replace only the installed payload, never the temp folder holding the new publish nor a slot.
    Get-ChildItem -LiteralPath $installDir -Force | Where-Object {
        $_.FullName -ne $tempDir -and -not ($_.PSIsContainer -and $_.Name -match $script:ExecutionSlotPattern)
    } | ForEach-Object {
        try {
            Remove-Item -LiteralPath $_.FullName -Recurse -Force
        }
        catch {
            throw "The debug build could not be replaced, most likely because $projectName is still running from it. Run Stop.ps1 or close $projectName and run again: $installDir"
        }
    }

    Get-ChildItem -LiteralPath $publishDir -Force | ForEach-Object {
        Move-Item -LiteralPath $_.FullName -Destination $installDir
    }

    Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue

    $processId = (Start-Process -FilePath $exe -WorkingDirectory $installDir -PassThru).Id
    Add-ExecutionLaunch -ProjectRoot $root -Config $config -ProcessId $processId -Version $version -Script 'Debug.ps1' -Folder $installDir -Executable $exe
    $outcome = 'launched'
}
catch {
    $failure = $_
    $outcome = 'failed'
}

$fields = [ordered]@{
    'Source' = 'working tree, built now'
    'Build'  = "Debug $Runtime" + $(if ($config.publish.selfContained) { ' self-contained' } else { '' })
    'Folder' = $installDir
}
if (Test-Path -LiteralPath $exe -PathType Leaf) {
    $fields['Executable'] = Format-ExecutionExecutable -Path $exe
}

if ($processId -gt 0) {
    $fields['PID'] = $processId
}

$fields['Duration'] = Format-ExecutionDuration -Elapsed ((Get-Date) - $started)
if ($null -ne $publish) {
    $fields['Publish'] = $publish.ExitCode
}

$tail = @()
if ($null -ne $failure) {
    $fields['Failure'] = $failure.Exception.Message
    if (Test-Path -LiteralPath $tempDir) {
        $fields['Kept'] = $tempDir
    }

    if ($null -ne $publish -and $publish.ExitCode -ne 0) {
        $tail = @($publish.Lines | Select-Object -Last 40)
    }
}

Write-ExecutionRecord -ProjectRoot $root -Config $config -Version $version -Script 'Debug.ps1' -Outcome $outcome -Fields $fields -Tail $tail

if ($null -ne $failure) {
    throw $failure
}

Write-ExecutionSummary -Heading "Launched $projectName $version (debug)" -Fields $fields
