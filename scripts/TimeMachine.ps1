<#
.SYNOPSIS
    Build and launch the application, either from the current source or a versioned snapshot.
.DESCRIPTION
    With no version, the current working-tree source (the configured project
    file) is published inside <work root>/temp and installed into the selected
    current destination: the build folder, the run folder, or
    snapshots/<current-version>/. It can also mirror the complete publish output
    into snapshots/<version>/ before launching.

    With a version, one ladder is climbed. A snapshot folder already holding
    an executable is launched as it is. Otherwise the commit whose subject
    carries the version is exported inside <work root>/temp, published there,
    and installed into snapshots/<version>/. When no local commit carries it,
    the configured remote is fetched once and the commit looked for again; a
    fetch the remote refuses stops the run with the remote's own words. When
    the exact version exists nowhere reached, the nearest lower version is
    used and announced. -Rebuild skips the snapshot rung so the commit is
    built afresh.

    Every intermediate file, the MSBuild artifacts included, lives under
    <work root>/temp, so two scripts with different work roots never share a
    folder. The temp folder is removed once the install succeeds and kept for
    inspection when it fails.

    The checked-out source tree is never used as build output. A snapshot
    folder keeps every file the publish does not produce.

    Every project-specific value - project file, executable, historical project
    names, runtime, publish properties, folder names, git remote and commit
    subject shape, version file - lives in Execution.json. This script carries
    none, so the file is identical in every project at the same generation.
.PARAMETER Version
    Historical major.minor.revision to build. Omit it to use the current working tree.
.PARAMETER CurrentDestination
    Destination for a current-source build, named by its layout key: run
    (default), build, or snapshots, the last meaning snapshots/<current-version>/.
    Ignored for historical versions, which are installed under snapshots/<version>/.
.PARAMETER WorkRoot
    Layout key of the folder whose temp subfolder holds every intermediate file
    of this invocation: run (default) or build.
.PARAMETER MirrorCurrentSnapshot
    Also copy a current-source publish into snapshots/<current-version>/.
.PARAMETER Rebuild
    Build a historical version from its commit even when its snapshot already holds an executable.
.PARAMETER NoLaunch
    Build and install without starting the application.
.PARAMETER Invoker
    Name of the script that delegated here, written into the record. Defaults to TimeMachine.ps1.
.PARAMETER Help
    Display this help and exit without building or launching. The alias -? is supported.
.EXAMPLE
    TimeMachine
    Build the current source into the run folder and launch it.
.EXAMPLE
    TimeMachine -NoLaunch
    Build the current source without launching it.
.EXAMPLE
    TimeMachine 2.9.9991
    Launch the 2.9.9991 snapshot, building it from its commit first when needed.
.EXAMPLE
    TimeMachine 2.9.9991 -Rebuild -NoLaunch
    Rebuild the 2.9.9991 snapshot from its commit without launching it.
#>
#requires -Version 5.1
# TIMEMACHINE - EXECUTION GENERATION 2.
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

    [ValidateSet('build', 'run', 'snapshots')]
    [string]$CurrentDestination = 'run',

    [ValidateSet('build', 'run')]
    [string]$WorkRoot = 'run',

    [switch]$MirrorCurrentSnapshot,

    [switch]$Rebuild,

    [switch]$NoLaunch,

    [ValidatePattern('^[A-Za-z][A-Za-z0-9_.-]*$')]
    [string]$Invoker = 'TimeMachine.ps1',

    [Alias('?')]
    [switch]$Help
)

if ($Help) {
    @'
NAME
    TimeMachine.ps1

SYNOPSIS
    Build and optionally launch the application from current or historical source.

SYNTAX
    TimeMachine [<version>] [-CurrentDestination <build|run|snapshots>]
        [-WorkRoot <build|run>] [-MirrorCurrentSnapshot] [-Rebuild] [-NoLaunch]
        [-Invoker <script>] [-Help]

OPTIONS
    <version>
        Historical major.minor.revision. Omit for current source. A snapshot
        holding an executable is launched as it is; otherwise the commit
        carrying the version is built, fetching the remote once when needed.
        A version found nowhere falls back to the nearest lower one, announced.

    -CurrentDestination <build|run|snapshots>
        Current-source destination by layout key. Defaults to run; snapshots
        means snapshots/<current-version>/.

    -WorkRoot <build|run>
        Layout key of the folder whose temp subfolder holds every intermediate
        file. Defaults to run.

    -MirrorCurrentSnapshot
        Also copy a current-source publish into its versioned snapshot.

    -Rebuild
        Build a historical version from its commit even when its snapshot
        already holds an executable.

    -NoLaunch
        Build and install without starting the application.

    -Invoker <script>
        Name of the script that delegated here, written into the record.
        Defaults to TimeMachine.ps1.

    -Help, -?
        Display this help and exit without building or launching.

EXAMPLES
    TimeMachine
        Build current source into the run folder and launch it.

    TimeMachine -NoLaunch
        Build current source without launching it.

    TimeMachine 2.9.9991
        Launch the 2.9.9991 snapshot, building it from its commit first when needed.

    TimeMachine 2.9.9991 -Rebuild -NoLaunch
        Rebuild the 2.9.9991 snapshot from its commit without launching it.
'@ | Write-Host
    exit 0
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $PSScriptRoot 'Execution.config.ps1')

Write-ExecutionGeneration -Script 'TimeMachine.ps1'
$config = Read-ExecutionConfig -ProjectRoot $root

$projectName = $config.project
$runtime = $config.publish.runtime
$selfContained = $config.publish.selfContained.ToString().ToLowerInvariant()
$publishProperties = @($config.publish.properties | ForEach-Object { "-p:$_" })
$snapshotsRoot = Join-ExecutionPath -ProjectRoot $root -Relative $config.layout.snapshots

function Start-Application {
    # Launches detached and returns the process id. The console never waits for the application.
    param(
        [Parameter(Mandatory = $true)][string]$Executable,
        [Parameter(Mandatory = $true)][string]$Folder
    )

    # A freshly installed executable at the same path can leave Explorer with a stale or
    # missing icon-cache entry, and a launch that races that first extraction shows the
    # default icon in the taskbar. Tell the shell the item changed and extract once here.
    Add-Type -Namespace Execution -Name Shell -MemberDefinition @'
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public struct SHFILEINFO {
            public IntPtr hIcon; public int iIcon; public uint dwAttributes;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
        }
        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern IntPtr SHGetFileInfo(string path, uint attributes, ref SHFILEINFO info, uint size, uint flags);
        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern void SHChangeNotify(int eventId, uint flags, string item1, string item2);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr icon);
'@
    [Execution.Shell]::SHChangeNotify(0x00002000, 0x0005, $Executable, $null)
    $info = New-Object Execution.Shell+SHFILEINFO
    $size = [System.Runtime.InteropServices.Marshal]::SizeOf($info)
    if ([Execution.Shell]::SHGetFileInfo($Executable, 0, [ref] $info, $size, 0x0100) -ne [IntPtr]::Zero -and $info.hIcon -ne [IntPtr]::Zero) {
        [Execution.Shell]::DestroyIcon($info.hIcon) | Out-Null
    }

    $process = Start-Process -FilePath $Executable -WorkingDirectory $Folder -PassThru
    Add-ExecutionLaunch -ProjectRoot $root -Config $config -ProcessId $process.Id -Version $Version -Script $Invoker -Folder $Folder -Executable $Executable
    return $process.Id
}

function Get-SummaryFields {
    # The fields the console block and the record share, so the two never disagree.
    param(
        [string]$Folder,
        [string]$Executable,
        [int]$ProcessId = 0
    )

    $fields = [ordered]@{}
    if ($askedVersion -ne $Version) {
        $fields['Asked'] = "$askedVersion -> nearest lower $Version"
    }

    $fields['Source'] = $source
    $fields['Build'] = "Release $runtime" + $(if ($config.publish.selfContained) { ' self-contained' } else { '' })
    if ($Folder) {
        $fields['Folder'] = $Folder
    }

    if ($Executable -and (Test-Path -LiteralPath $Executable -PathType Leaf)) {
        $fields['Executable'] = Format-ExecutionExecutable -Path $Executable
    }

    if ($ProcessId -gt 0) {
        $fields['PID'] = $ProcessId
    }

    $fields['Duration'] = Format-ExecutionDuration -Elapsed ((Get-Date) - $started)
    if ($null -ne $restore) {
        $fields['Restore'] = $restore.ExitCode
    }

    if ($null -ne $publish) {
        $fields['Publish'] = $publish.ExitCode
    }

    return $fields
}

function Install-Payload {
    # Moves every item of the staging folder into the destination. A full install first clears the
    # destination except the temp root and the slot folders that may live inside it; a snapshot
    # install overwrites only the names the publish produced, so whatever else the folder holds is kept.
    param(
        [Parameter(Mandatory = $true)][string]$Staging,
        [Parameter(Mandatory = $true)][string]$Destination,
        [Parameter(Mandatory = $true)][string]$Keep,
        [switch]$Snapshot
    )

    if (Test-Path -LiteralPath $Destination) {
        $stale = Get-ChildItem -LiteralPath $Destination -Force | Where-Object {
            $_.FullName -ne $Keep -and -not ($_.PSIsContainer -and $_.Name -match $script:ExecutionSlotPattern)
        }
        if ($Snapshot) {
            $produced = @(Get-ChildItem -LiteralPath $Staging -Force | ForEach-Object { $_.Name })
            $stale = $stale | Where-Object { $_.Name -in $produced }
        }

        try {
            $stale | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
        }
        catch {
            throw "The installed build could not be replaced, most likely because $projectName is still running from it. Run Stop.ps1 or close $projectName and run again: $Destination"
        }
    }
    else {
        New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    }

    Get-ChildItem -LiteralPath $Staging -Force | ForEach-Object {
        Move-Item -LiteralPath $_.FullName -Destination $Destination
    }
}

$started = Get-Date
$useCurrentSource = [string]::IsNullOrEmpty($Version)
$askedVersion = $Version
$resolved = $null
$source = ''
$restore = $null
$publish = $null
$workDir = $null
$destinationDir = $null
$destinationExe = $null
$failure = $null
$outcome = $null
$processId = 0

try {
    if ($useCurrentSource) {
        $Version = Read-ExecutionVersion -ProjectRoot $root -Config $config -Key $config.version.currentKey
        $askedVersion = $Version
        $source = 'working tree, built now'
    }
    else {
        $resolved = Resolve-ExecutionVersion -ProjectRoot $root -Config $config -Version $Version -Rebuild:$Rebuild
        $Version = $resolved.Version
        $source = if ($resolved.Rung -eq 'snapshot') { 'snapshot, already built' } else { "commit $($resolved.Commit.Substring(0, 12)), built now" }
    }

    if ($useCurrentSource -and $CurrentDestination -ne 'snapshots') {
        $destinationDir = Join-ExecutionPath -ProjectRoot $root -Relative $config.layout.$CurrentDestination
        $destinationIsSnapshot = $false
    }
    else {
        $destinationDir = Join-Path $snapshotsRoot $Version
        $destinationIsSnapshot = $true
    }

    if ($null -ne $resolved -and $resolved.Rung -eq 'snapshot') {
        $destinationExe = $resolved.Executable
    }
    else {
        Set-ExecutionDotnetQuiet

        $workRootDir = Join-ExecutionPath -ProjectRoot $root -Relative $config.layout.$WorkRoot
        $tempRoot = Join-ExecutionPath -ProjectRoot $workRootDir -Relative $config.layout.temp
        $workDir = Join-Path $tempRoot ("timemachine-$Version-" + [Guid]::NewGuid().ToString('N'))
        $sourceDir = Join-Path $workDir 'source'
        $sourceArchive = Join-Path $workDir 'source.zip'
        $publishDir = Join-Path $workDir 'publish'
        $artifactsDir = Join-Path $workDir 'artifacts'

        $executableName = $config.source.executable
        $destinationExe = Join-Path $destinationDir $executableName
        $stagingDir = Join-Path $tempRoot ('staging-' + [Guid]::NewGuid().ToString('N'))
        $snapshotStagingDir = $null
        $installed = $false

        New-Item -ItemType Directory -Path $workDir -Force | Out-Null

        try {
            if ($useCurrentSource) {
                $project = Join-ExecutionPath -ProjectRoot $root -Relative $config.source.project
                if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
                    throw "The project file was not found: $project"
                }

                Write-Host "Building current source: $Version" -ForegroundColor Cyan
            }
            else {
                $commit = $resolved.Commit
                Write-Host "Exporting commit $($commit.Substring(0, 12)) for version $Version" -ForegroundColor Cyan
                $export = Invoke-ExecutionGit -ProjectRoot $root -Arguments @('archive', '--format=zip', '--output', $sourceArchive, $commit)
                if ($export.ExitCode -ne 0) {
                    throw "git archive failed for commit $commit`:`n$($export.Error.Trim())"
                }

                # Expand-Archive is left out: Windows PowerShell 5.1 loads it from a script module,
                # which resolves to the pwsh 7 copy when the session inherits the pwsh module path.
                New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null
                Add-Type -AssemblyName System.IO.Compression.FileSystem
                [System.IO.Compression.ZipFile]::ExtractToDirectory($sourceArchive, $sourceDir)

                # A historical commit may predate the current project name, so every configured
                # historical project name is accepted, matched on both the file and its folder.
                $historicalNames = @($config.source.historicalProjects)
                $historicalFiles = @($historicalNames | ForEach-Object { "$_.csproj" })
                # Windows PowerShell 5.1 ignores -Include beside -LiteralPath, so the name is matched here.
                $projects = @(
                    Get-ChildItem -LiteralPath $sourceDir -File -Recurse |
                        Where-Object { $_.Name -in $historicalFiles -and $_.Directory.Name -in $historicalNames }
                )

                if ($projects.Count -eq 0) {
                    throw "A $projectName UI project was not found in commit $commit."
                }

                if ($projects.Count -gt 1) {
                    $projectList = ($projects.FullName | ForEach-Object { "  $_" }) -join [Environment]::NewLine
                    throw "More than one $projectName UI project was found in commit $commit`:$([Environment]::NewLine)$projectList"
                }

                $project = $projects[0].FullName
                Write-Host "Historical source: $sourceDir" -ForegroundColor Cyan
            }

            New-Item -ItemType Directory -Path $publishDir | Out-Null

            $restore = Invoke-ExecutionDotnet -Arguments @('restore', $project, '--runtime', $runtime, '--artifacts-path', $artifactsDir)
            if ($restore.ExitCode -ne 0) {
                throw "dotnet restore failed with exit code $($restore.ExitCode)."
            }

            $publish = Invoke-ExecutionDotnet -Arguments (@(
                'publish', $project,
                '--no-restore',
                '--configuration', 'Release',
                '--runtime', $runtime,
                '--self-contained', $selfContained,
                '--output', $publishDir,
                '--artifacts-path', $artifactsDir) +
                $publishProperties +
                @('-p:DebugType=none', '-p:DebugSymbols=false'))
            if ($publish.ExitCode -ne 0) {
                throw "dotnet publish failed with exit code $($publish.ExitCode)."
            }

            # A historical commit may predate the configured assembly name, so the project's own name
            # is accepted when the configured executable is not what its publish produced.
            $publishedExe = Join-Path $publishDir $executableName
            if (-not $useCurrentSource -and -not (Test-Path -LiteralPath $publishedExe -PathType Leaf)) {
                $executableName = [System.IO.Path]::GetFileNameWithoutExtension($project) + '.exe'
                $publishedExe = Join-Path $publishDir $executableName
            }

            if (-not (Test-Path -LiteralPath $publishedExe -PathType Leaf)) {
                throw "Publish reported success but $executableName is missing: $publishedExe"
            }

            # Stage the whole publish output beside its final destination. This keeps
            # the executable and every version-matched dependency together.
            Move-Item -LiteralPath $publishDir -Destination $stagingDir

            # Decided only now, after the build, so the folder's state is as fresh as it can be.
            $destinationDir = Resolve-ExecutionSlot -ProjectRoot $root -Config $config -Folder $destinationDir
            $destinationExe = Join-Path $destinationDir $executableName

            Install-Payload -Staging $stagingDir -Destination $destinationDir -Keep $tempRoot -Snapshot:$destinationIsSnapshot

            if (-not (Test-Path -LiteralPath $destinationExe -PathType Leaf)) {
                throw "$executableName is missing from the installed build: $destinationExe"
            }

            $installed = $true

            if ($useCurrentSource -and $MirrorCurrentSnapshot -and $CurrentDestination -ne 'snapshots') {
                New-Item -ItemType Directory -Path $snapshotsRoot -Force | Out-Null

                $snapshotDir = Join-Path $snapshotsRoot $Version
                $snapshotStagingDir = Join-Path $tempRoot ('mirror-' + [Guid]::NewGuid().ToString('N'))
                New-Item -ItemType Directory -Path $snapshotStagingDir | Out-Null

                # Mirror the complete publish payload so the executable and all of its
                # version-matched dependencies stay together.
                Get-ChildItem -LiteralPath $destinationDir -Force | Where-Object {
                    $_.FullName -ne $tempRoot -and -not ($_.PSIsContainer -and $_.Name -match $script:ExecutionSlotPattern)
                } | ForEach-Object {
                    Copy-Item -LiteralPath $_.FullName -Destination $snapshotStagingDir -Recurse -Force
                }

                $snapshotDir = Resolve-ExecutionSlot -ProjectRoot $root -Config $config -Folder $snapshotDir
                Install-Payload -Staging $snapshotStagingDir -Destination $snapshotDir -Keep $tempRoot -Snapshot
                Remove-Item -LiteralPath $snapshotStagingDir -Recurse -Force -ErrorAction SilentlyContinue
                $snapshotStagingDir = $null

                Write-Host "Snapshot: $snapshotDir" -ForegroundColor Green
            }
        }
        finally {
            if ($null -ne $snapshotStagingDir -and (Test-Path -LiteralPath $snapshotStagingDir)) {
                Remove-Item -LiteralPath $snapshotStagingDir -Recurse -Force -ErrorAction SilentlyContinue
            }

            if (Test-Path -LiteralPath $stagingDir) {
                Remove-Item -LiteralPath $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
            }

            if ($installed -and (Test-Path -LiteralPath $tempRoot)) {
                Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
            }
            elseif (-not $installed) {
                Write-Host "Intermediate files were kept for inspection: $workDir" -ForegroundColor Yellow
            }
        }
    }

    if ($NoLaunch) {
        $outcome = 'installed'
    }
    else {
        $processId = Start-Application -Executable $destinationExe -Folder $destinationDir
        $outcome = 'launched'
    }
}
catch {
    $failure = $_
    $outcome = 'failed'
}

$fields = Get-SummaryFields -Folder $destinationDir -Executable $destinationExe -ProcessId $processId
$tail = @()
if ($null -ne $failure) {
    $fields['Failure'] = $failure.Exception.Message
    if ($null -ne $workDir -and (Test-Path -LiteralPath $workDir)) {
        $fields['Kept'] = $workDir
    }

    # The last dotnet call that ran is the one that explains a build failure.
    $last = if ($null -ne $publish) { $publish } elseif ($null -ne $restore) { $restore } else { $null }
    if ($null -ne $last -and $last.ExitCode -ne 0) {
        $tail = @($last.Lines | Select-Object -Last 40)
    }
}

Write-ExecutionRecord -ProjectRoot $root -Config $config -Version $Version -Script $Invoker -Outcome $outcome -Fields $fields -Tail $tail

if ($null -ne $failure) {
    throw $failure
}

$heading = if ($outcome -eq 'launched') { "Launched $projectName $Version" } else { "Installed $projectName $Version" }
Write-ExecutionSummary -Heading $heading -Fields $fields
