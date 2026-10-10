<#
.SYNOPSIS
    Build a source snapshot without launching it.
.DESCRIPTION
    Delegates to TimeMachine.ps1 with -NoLaunch and the build folder as its
    work root. With no version, the current working-tree source is built there,
    installed into the build folder and copied, with all publish dependencies,
    into snapshots/<version>/. With a version, the historical source archive is
    built in isolation and installed into snapshots/<version>/. Use stable to
    select the stable version recorded in the version file.

    Every project-specific value - folder names, version file, project name -
    lives in Execution.json. This script carries none, so the file is identical
    in every project at the same generation.
.PARAMETER Version
    Version to build. Omit it to build the current working tree, pass stable to
    use the stable version from the version file, or pass a major.minor.revision value.
.PARAMETER Rebuild
    Build a historical version from its commit even when its snapshot already holds an executable.
.PARAMETER Help
    Display this help and exit without building. The alias -? is supported.
.EXAMPLE
    build
    Build the current working tree into the build folder and mirror its snapshot.
.EXAMPLE
    build stable
    Build the source archive identified by the stable version.
.EXAMPLE
    build 2.9.9991
    Build a specific historical source archive.
#>
#requires -Version 5.1
# BUILD - EXECUTION GENERATION 2.
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
    [Parameter(Position = 0)]
    [ValidatePattern('^(stable|(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*))$')]
    [string]$Version,
    [switch]$Rebuild,
    [Alias('?')]
    [switch]$Help
)

if ($Help) {
    @'
NAME
    Build.ps1

SYNOPSIS
    Build a source snapshot without launching it.

SYNTAX
    build [<version>|stable] [-Rebuild] [-Help]

OPTIONS
    <version>|stable
        Build the current working tree when omitted, the stable version when
        set to stable, or a specific major.minor.revision source archive.

    -Rebuild
        Build a historical version from its commit even when its snapshot
        already holds an executable.

    -Help, -?
        Display this help and exit without building.

EXAMPLES
    build
        Build the current working tree and mirror its snapshot.

    build stable
        Build the source archive identified by the stable version.

    build 2.9.9991
        Build a specific historical source archive.
'@ | Write-Host
    exit 0
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $PSScriptRoot 'Execution.config.ps1')

Write-ExecutionGeneration -Script 'Build.ps1'
$config = Read-ExecutionConfig -ProjectRoot $root

$timeMachine = Join-Path $PSScriptRoot 'TimeMachine.ps1'
if (-not (Test-Path -LiteralPath $timeMachine -PathType Leaf)) {
    throw "TimeMachine.ps1 was not found: $timeMachine"
}

if ($Version -eq 'stable') {
    $Version = Read-ExecutionVersion -ProjectRoot $root -Config $config -Key $config.version.stableKey
}

if ([string]::IsNullOrEmpty($Version)) {
    & $timeMachine -CurrentDestination build -WorkRoot build -Invoker Build.ps1 -MirrorCurrentSnapshot -NoLaunch
}
else {
    & $timeMachine -Version $Version -WorkRoot build -Invoker Build.ps1 -Rebuild:$Rebuild -NoLaunch
}
