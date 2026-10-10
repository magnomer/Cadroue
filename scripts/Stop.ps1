<#
.SYNOPSIS
    End every instance the execution family launched.
.DESCRIPTION
    Reads the launch ledger that Run.ps1, Build.ps1, Debug.ps1 and TimeMachine.ps1
    write, and ends every process still alive in it: the main window is asked to
    close first so the application saves what it holds, and a process that lingers
    past the timeout is killed. Ledger entries whose process is gone are dropped.
    Slot folders nothing runs from any more are reaped afterwards.

    With a version, only the instances of that version are ended. With -All, the
    processes running the project's executable from under the project root that
    the ledger does not know - launched by hand - are ended too.

    Every project-specific value - executable name, ledger path, folder names -
    lives in Execution.json. This script carries none, so the file is identical
    in every project at the same generation.
.PARAMETER Version
    End only the instances of this major.minor.revision version. Omit it to end every instance.
.PARAMETER All
    Also end instances the ledger does not know, found by executable name under the project root. Alias: -a.
.PARAMETER Force
    Kill at once instead of asking the main window to close first.
.PARAMETER TimeoutSeconds
    Seconds to wait for a window to close before the process is killed. Defaults to 5.
.PARAMETER Help
    Display this help and exit without ending anything. The alias -? is supported.
.EXAMPLE
    stop
    Close every instance the family launched.
.EXAMPLE
    stop 2.9.9991
    Close only the instances of version 2.9.9991.
.EXAMPLE
    stop -All -Force
    Kill every instance under the project root, known or not, without asking it to close.
#>
#requires -Version 5.1
# STOP - EXECUTION GENERATION 2.
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
    [ValidatePattern('^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$')]
    [string]$Version,
    [Alias('a')]
    [switch]$All,
    [switch]$Force,
    [ValidateRange(1, 300)]
    [int]$TimeoutSeconds = 5,
    [Alias('?')]
    [switch]$Help
)

if ($Help) {
    @'
NAME
    Stop.ps1

SYNOPSIS
    End every instance the execution family launched.

SYNTAX
    stop [<version>] [-All] [-Force] [-TimeoutSeconds <n>] [-Help]

OPTIONS
    <version>
        End only the instances of this major.minor.revision version.

    -All, -a
        Also end instances the ledger does not know, found by executable
        name under the project root.

    -Force
        Kill at once instead of asking the main window to close first.

    -TimeoutSeconds <n>
        Seconds to wait for a window to close before killing. Defaults to 5.

    -Help, -?
        Display this help and exit without ending anything.

EXAMPLES
    stop
        Close every instance the family launched.

    stop 2.9.9991
        Close only the instances of version 2.9.9991.

    stop -All -Force
        Kill every instance under the project root, known or not, without asking it to close.
'@ | Write-Host
    exit 0
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $PSScriptRoot 'Execution.config.ps1')

Write-ExecutionGeneration -Script 'Stop.ps1'
$config = Read-ExecutionConfig -ProjectRoot $root

$started = Get-Date
$projectName = $config.project
$ended = New-Object 'System.Collections.Generic.List[object]'
$folders = New-Object 'System.Collections.Generic.List[string]'

$launches = @(Get-ExecutionLaunches -ProjectRoot $root -Config $config)
if (-not [string]::IsNullOrEmpty($Version)) {
    $launches = @($launches | Where-Object { $_.version -eq $Version })
}

foreach ($entry in $launches) {
    $how = Stop-ExecutionLaunch -ProcessId ([int]$entry.pid) -Force:$Force -TimeoutSeconds $TimeoutSeconds
    [void]$ended.Add([pscustomobject]@{ PID = [int]$entry.pid; Version = $entry.version; Script = $entry.script; Folder = $entry.folder; How = $how })
    [void]$folders.Add((Split-Path -Parent $entry.folder))
    [void]$folders.Add($entry.folder)
}

if ($All) {
    foreach ($stray in Get-ExecutionStrays -ProjectRoot $root -Config $config) {
        $folder = Split-Path -Parent $stray.Path
        $how = Stop-ExecutionLaunch -ProcessId $stray.Id -Force:$Force -TimeoutSeconds $TimeoutSeconds
        [void]$ended.Add([pscustomobject]@{ PID = $stray.Id; Version = '-'; Script = 'by hand'; Folder = $folder; How = $how })
        [void]$folders.Add((Split-Path -Parent $folder))
        [void]$folders.Add($folder)
    }
}

# Rereading the ledger drops the entries whose process has now gone.
$remaining = @(Get-ExecutionLaunches -ProjectRoot $root -Config $config)

function Get-OrdinalKey {
    # Sort-Object compares text by culture, which Windows PowerShell 5.1 and pwsh 7 order differently.
    # Uppercase hexadecimal UTF-16 code units compare alike under every culture, so this key sorts ordinally.
    param([string]$Text)
    return [System.BitConverter]::ToString([System.Text.Encoding]::BigEndianUnicode.GetBytes($Text)).Replace('-', '')
}

$reaped = 0
foreach ($folder in ($folders | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } } -Unique)) {
    if (Test-Path -LiteralPath $folder -PathType Container) {
        $reaped += Remove-ExecutionDeadSlots -ProjectRoot $root -Config $config -Folder $folder
    }
}

if ($ended.Count -eq 0) {
    $scope = if ([string]::IsNullOrEmpty($Version)) { "No $projectName instance" } else { "No $projectName $Version instance" }
    Write-Host "$scope was running from the ledger$(if ($All) { ' or the project root' })." -ForegroundColor Yellow
}
else {
    Write-Host "Ended $($ended.Count) $projectName instance(s):" -ForegroundColor Cyan
    $ended | Format-Table -AutoSize PID, Version, Script, How, Folder | Out-String -Width 4096 | ForEach-Object { Write-Host $_.TrimEnd() }
}

$fields = [ordered]@{}
$fields['Ended'] = $ended.Count
$fields['Closed'] = @($ended | Where-Object { $_.How -eq 'closed' }).Count
$fields['Killed'] = @($ended | Where-Object { $_.How -eq 'killed' }).Count
$lingering = @($ended | Where-Object { $_.How -eq 'lingering' })
if ($lingering.Count -gt 0) {
    $fields['Lingering'] = ($lingering | ForEach-Object { $_.PID }) -join ', '
}

$fields['Remaining'] = $remaining.Count
if ($reaped -gt 0) {
    $fields['Reaped'] = "$reaped slot(s)"
}

$fields['Duration'] = Format-ExecutionDuration -Elapsed ((Get-Date) - $started)

foreach ($stoppedVersion in @($ended | Where-Object { $_.Version -ne '-' } | ForEach-Object { $_.Version } | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } } -Unique)) {
    $versionFields = [ordered]@{}
    $versionFields['PIDs'] = (@($ended | Where-Object { $_.Version -eq $stoppedVersion } | ForEach-Object { "$($_.PID) ($($_.How))" })) -join ', '
    foreach ($key in $fields.Keys) {
        $versionFields[$key] = $fields[$key]
    }

    Write-ExecutionRecord -ProjectRoot $root -Config $config -Version $stoppedVersion -Script 'Stop.ps1' -Outcome 'stopped' -Fields $versionFields
}

Write-ExecutionSummary -Heading "$projectName stop" -Fields $fields

if ($lingering.Count -gt 0) {
    exit 1
}
