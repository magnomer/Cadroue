<#
.SYNOPSIS
Read and validate Execution.json for the execution tooling.

.DESCRIPTION
Dot-sourced by Run.ps1, Build.ps1, Debug.ps1, Stop.ps1 and TimeMachine.ps1. Holds the
generation number, the configuration schema, and the strict reader every script uses. No
project-specific value appears here: this file is identical in every project at this
generation.
#>
#requires -Version 5.1
# EXECUTION.CONFIG - EXECUTION GENERATION 2.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the executed outcome changes.
# A configuration is total: every key in the schema must be present, a missing key is an error rather
# than a default, and an unknown key is an error rather than a silent no-op. A typo that quietly does
# nothing is the worst failure mode a configuration like this has, so it is made loud.
# Execution.json holds every project-specific value the family reads: the project name, the project
# file and executable, the historical project names, the publish runtime and properties, the folder
# layout, the git remote and commit-subject shape, and the version file with its keys. The scripts
# hold none of them.
# A historical version is resolved by one ladder shared by every script: a snapshot folder holding an
# executable is launched as it is; otherwise the commit whose subject carries the version is built;
# otherwise the remote is fetched once and the commit looked for again; a fetch that fails stops the
# run with the remote's own words. When the exact version exists nowhere reached, the nearest lower
# version is used and said aloud. -Rebuild skips the snapshot rung so the commit is built afresh.
# Every run leaves a record: one Markdown file per version under the configured record folder, an
# entry appended per run with the same fields the console block shows, and on failure the error, the
# kept temp folder, and the tail of the dotnet output. A record changes nothing that is built or
# launched, so it does not move the generation.
# Generation 2: the family lives in <project root>/scripts with its configuration beside it, only the
# version file staying at the root. A running instance never blocks the next install: when a live
# process runs from the install folder, the fresh build goes into an eight-digit slot folder inside it
# and launches from there; slots no process runs from are reaped before the next install. Every launch
# is written to the ledger named by layout.ledger with its process id and start instant, so Stop.ps1
# can end exactly what the family launched and a reused process id is never mistaken for a live one.

$script:ExecutionGeneration = 2

# dotnet and git write UTF-8. A console still on the OEM code page, as one opened by the dispatcher
# without a profile is, would show every non-ASCII line garbled, so this process reads and writes
# UTF-8. Process-local: the calling console keeps its own code page.
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding

# Each entry is a path through the document and the kind of value that must be found there.
# 'string', 'int', 'bool', or 'string[]'.
$script:ExecutionSchema = [ordered]@{
    'generation'                = 'int'
    'project'                   = 'string'
    'source.project'            = 'string'
    'source.executable'         = 'string'
    'source.historicalProjects' = 'string[]'
    'publish.runtime'           = 'string'
    'publish.selfContained'     = 'bool'
    'publish.properties'        = 'string[]'
    'layout.run'                = 'string'
    'layout.build'              = 'string'
    'layout.debug'              = 'string'
    'layout.snapshots'          = 'string'
    'layout.temp'               = 'string'
    'layout.ledger'             = 'string'
    'git.remote'                = 'string'
    'git.subjectPattern'        = 'string'
    'version.file'              = 'string'
    'version.currentKey'        = 'string'
    'version.stableKey'         = 'string'
    'record.directory'          = 'string'
    'record.filePattern'        = 'string'
}

$script:ExecutionVersionPattern = '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$'
$script:ExecutionSlotPattern = '^[1-9][0-9]{7}$'

function Join-ExecutionPath {
    # A configured path is written with forward slashes and is always relative to the project root.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)][string]$Relative
    )

    $native = $Relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar).Replace('\', [System.IO.Path]::DirectorySeparatorChar)
    return Join-Path $ProjectRoot $native
}

function Get-ExecutionNode {
    param(
        [Parameter(Mandatory = $true)]$Document,
        [Parameter(Mandatory = $true)][string]$Key
    )

    $node = $Document
    foreach ($segment in $Key.Split('.')) {
        if ($null -eq $node -or -not ($node.PSObject.Properties.Name -contains $segment)) {
            return @{ Found = $false; Value = $null }
        }

        $node = $node.$segment
    }

    return @{ Found = $true; Value = $node }
}

function Get-ExecutionKeyPaths {
    # Every leaf path present in the document, so an unknown key is reported rather than ignored.
    param(
        [Parameter(Mandatory = $true)]$Node,
        [string]$Prefix = ''
    )

    $paths = New-Object 'System.Collections.Generic.List[string]'
    foreach ($property in $Node.PSObject.Properties) {
        $path = if ($Prefix.Length -eq 0) { $property.Name } else { $Prefix + '.' + $property.Name }

        $isBranch = $null -ne $property.Value -and
            $property.Value -is [System.Management.Automation.PSCustomObject] -and
            -not $script:ExecutionSchema.Contains($path)

        if ($isBranch) {
            foreach ($child in (Get-ExecutionKeyPaths -Node $property.Value -Prefix $path)) {
                [void]$paths.Add($child)
            }
        }
        else {
            [void]$paths.Add($path)
        }
    }

    return , $paths.ToArray()
}

function Test-ExecutionValue {
    param(
        [Parameter(Mandatory = $true)][string]$Kind,
        $Value
    )

    switch ($Kind) {
        'string' { return $Value -is [string] -and -not [string]::IsNullOrWhiteSpace($Value) }
        'int' { return $Value -is [int] -or $Value -is [long] }
        'bool' { return $Value -is [bool] }
        'string[]' {
            if ($null -eq $Value -or -not ($Value -is [System.Array])) { return $false }
            foreach ($item in $Value) {
                if (-not ($item -is [string])) { return $false }
            }

            return $true
        }
        default { throw "Unknown schema kind: $Kind" }
    }
}

function Read-ExecutionConfig {
    param([Parameter(Mandatory = $true)][string]$ProjectRoot)

    $configPath = Join-Path $PSScriptRoot 'Execution.json'
    if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
        throw "The execution configuration was not found: $configPath"
    }

    try {
        $config = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The execution configuration is not valid JSON: $configPath`n$($_.Exception.Message)"
    }

    $problems = New-Object 'System.Collections.Generic.List[string]'

    foreach ($key in $script:ExecutionSchema.Keys) {
        $node = Get-ExecutionNode -Document $config -Key $key
        if (-not $node.Found) {
            [void]$problems.Add("missing key: $key")
            continue
        }

        if (-not (Test-ExecutionValue -Kind $script:ExecutionSchema[$key] -Value $node.Value)) {
            [void]$problems.Add("key '$key' must be $($script:ExecutionSchema[$key])")
        }
    }

    foreach ($path in (Get-ExecutionKeyPaths -Node $config)) {
        if (-not $script:ExecutionSchema.Contains($path)) {
            [void]$problems.Add("unknown key: $path")
        }
    }

    if ($problems.Count -gt 0) {
        throw "The execution configuration is not valid: $configPath`n  " + ($problems -join "`n  ")
    }

    if ($config.generation -ne $script:ExecutionGeneration) {
        throw "The execution configuration is generation $($config.generation) but this tooling is generation $script:ExecutionGeneration.`nA generation names what the family builds, installs and launches, so the two must match: $configPath"
    }

    return $config
}

function Read-ExecutionVersion {
    # Reads one version key from the configured version file and validates its major.minor.revision shape.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)]$Config,
        [Parameter(Mandatory = $true)][string]$Key
    )

    $versionFile = Join-ExecutionPath -ProjectRoot $ProjectRoot -Relative $Config.version.file
    if (-not (Test-Path -LiteralPath $versionFile -PathType Leaf)) {
        throw "The version file was not found: $versionFile"
    }

    $versionData = Get-Content -LiteralPath $versionFile -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not ($versionData.PSObject.Properties.Name -contains $Key)) {
        throw "The version file must contain a $Key property: $versionFile"
    }

    $version = [string]$versionData.$Key
    if ($version -notmatch $script:ExecutionVersionPattern) {
        throw "Invalid $Key format in the version file: $version"
    }

    return $version
}

function Invoke-ExecutionGit {
    # Runs git in the project root and returns its exit code with both streams captured, so a
    # failure can be reported in git's own words instead of being guessed at.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = 'git'
    $info.WorkingDirectory = $ProjectRoot
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.UseShellExecute = $false
    # Git must never wait for a person: a missing credential is reported as a refusal instead.
    $info.EnvironmentVariables['GIT_TERMINAL_PROMPT'] = '0'
    $info.EnvironmentVariables['GCM_INTERACTIVE'] = 'never'
    # Windows PowerShell 5.1 has no ArgumentList, so each argument is quoted into one line.
    # CommandLineToArgvW rules: backslashes before a quote are doubled and the quote escaped,
    # and trailing backslashes are doubled so they cannot escape the closing quote.
    $info.Arguments = ($Arguments | ForEach-Object {
        if ($_.Length -eq 0 -or $_ -match '[\s"]') {
            '"' + (($_ -replace '(\\*)"', '$1$1\"') -replace '(\\+)$', '$1$1') + '"'
        }
        else { $_ }
    }) -join ' '

    $process = [System.Diagnostics.Process]::Start($info)
    # Both pipes drain at once. Reading them one after the other deadlocks when git fills the
    # second pipe (fetch progress goes to stderr) before it has finished writing the first.
    $errorTask = $process.StandardError.ReadToEndAsync()
    $output = $process.StandardOutput.ReadToEnd()
    $errorText = $errorTask.GetAwaiter().GetResult()
    $process.WaitForExit()

    return @{ ExitCode = $process.ExitCode; Output = $output; Error = $errorText }
}

function Set-ExecutionDotnetQuiet {
    # Keeps the dotnet CLI from printing its first-run welcome and telemetry notice ahead of the
    # lines that matter. Process scope only; nothing outlives this script.
    $env:DOTNET_NOLOGO = '1'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
}

function Write-ExecutionSummary {
    # The block every script prints last, so the key facts sit right above the returned prompt.
    param(
        [Parameter(Mandatory = $true)][string]$Heading,
        [Parameter(Mandatory = $true)][System.Collections.Specialized.OrderedDictionary]$Fields
    )

    Write-Host $Heading -ForegroundColor Green
    foreach ($key in $Fields.Keys) {
        $value = $Fields[$key]
        if ($null -eq $value -or $value -eq '') {
            continue
        }

        Write-Host ("  {0,-11}{1}" -f $key, $value)
    }

    Write-Host ("  {0,-11}EXECUTION {1}" -f 'Generation', $script:ExecutionGeneration)
}

function Invoke-ExecutionDotnet {
    # Runs dotnet with its output shown live and kept, so a failure can put the tail in the record.
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    # Under Windows PowerShell 5.1 a redirected stderr line becomes an error record, which a Stop
    # preference would turn into a halt mid-build. Continue is scoped to this call.
    $ErrorActionPreference = 'Continue'
    $lines = New-Object 'System.Collections.Generic.List[string]'
    & dotnet @Arguments 2>&1 | ForEach-Object {
        $text = if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.Exception.Message } else { "$_" }
        Write-Host $text
        [void]$lines.Add($text)
    }

    return @{ ExitCode = $LASTEXITCODE; Lines = $lines.ToArray() }
}

function Format-ExecutionDuration {
    param([Parameter(Mandatory = $true)][timespan]$Elapsed)

    if ($Elapsed.TotalMinutes -ge 1) {
        return '{0}m {1:00}s' -f [int][math]::Floor($Elapsed.TotalMinutes), $Elapsed.Seconds
    }

    return '{0:0.0}s' -f $Elapsed.TotalSeconds
}

function Write-ExecutionRecord {
    # Appends one entry to the version's record file, creating the file with its header on first use.
    # Never throws: a record that cannot be written is said on the console and the run goes on.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)]$Config,
        [Parameter(Mandatory = $true)][string]$Version,
        [Parameter(Mandatory = $true)][string]$Script,
        [Parameter(Mandatory = $true)][string]$Outcome,
        [Parameter(Mandatory = $true)][System.Collections.Specialized.OrderedDictionary]$Fields,
        [string[]]$Tail = @()
    )

    try {
        $folder = Join-ExecutionPath -ProjectRoot $ProjectRoot -Relative $Config.record.directory
        $path = Join-Path $folder $Config.record.filePattern.Replace('{version}', $Version)
        New-Item -ItemType Directory -Path $folder -Force | Out-Null

        $lines = New-Object 'System.Collections.Generic.List[string]'
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            [void]$lines.Add("# Execution record $Version")
            [void]$lines.Add('')
            [void]$lines.Add("- Generation: $script:ExecutionGeneration")
            [void]$lines.Add('')
        }

        [void]$lines.Add(('## {0:yyyy-MM-dd HH:mm:ss zzz} - {1} - {2}' -f (Get-Date), $Script, $Outcome))
        [void]$lines.Add('')
        foreach ($key in $Fields.Keys) {
            $value = $Fields[$key]
            if ($null -ne $value -and $value -ne '') {
                [void]$lines.Add("- ${key}: $value")
            }
        }

        if ($Tail.Count -gt 0) {
            [void]$lines.Add('- Output tail:')
            [void]$lines.Add('')
            [void]$lines.Add('  ```')
            foreach ($line in $Tail) {
                [void]$lines.Add("  $line")
            }

            [void]$lines.Add('  ```')
        }

        [void]$lines.Add('')
        [System.IO.File]::AppendAllText($path, (($lines -join "`n") + "`n"), [System.Text.UTF8Encoding]::new($false))
        Write-Host "Recorded: $path" -ForegroundColor DarkGray
    }
    catch {
        Write-Host "The execution record could not be written: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

function Format-ExecutionExecutable {
    # Name, size and last write of an executable, for the summary block.
    param([Parameter(Mandatory = $true)][string]$Path)

    $file = Get-Item -LiteralPath $Path
    return '{0}  {1:N1} MB  {2:yyyy-MM-dd HH:mm}' -f $file.Name, ($file.Length / 1MB), $file.LastWriteTime
}

function Get-ExecutionSnapshotVersions {
    # Every snapshot folder named as a version and holding an executable, keyed by version.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)]$Config
    )

    $found = @{}
    $snapshotsRoot = Join-ExecutionPath -ProjectRoot $ProjectRoot -Relative $Config.layout.snapshots
    if (-not (Test-Path -LiteralPath $snapshotsRoot -PathType Container)) {
        return $found
    }

    foreach ($folder in Get-ChildItem -LiteralPath $snapshotsRoot -Directory) {
        if ($folder.Name -notmatch $script:ExecutionVersionPattern) {
            continue
        }

        $executables = @(Get-ChildItem -LiteralPath $folder.FullName -Filter '*.exe' -File)
        if ($executables.Count -eq 1) {
            $found[$folder.Name] = $executables[0].FullName
        }
    }

    return $found
}

function Get-ExecutionGitVersions {
    # Every version a commit subject carries, keyed by version, valued by the commit hash.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)]$Config
    )

    $found = @{}
    $log = Invoke-ExecutionGit -ProjectRoot $ProjectRoot -Arguments @('log', '--all', '--format=%H%x09%s')
    if ($log.ExitCode -ne 0) {
        throw "git log failed in the project root:`n$($log.Error.Trim())"
    }

    $pattern = [regex]$Config.git.subjectPattern
    foreach ($line in $log.Output -split "`n") {
        $parts = $line.TrimEnd("`r").Split("`t", 2)
        if ($parts.Count -ne 2) {
            continue
        }

        $match = $pattern.Match($parts[1])
        if ($match.Success -and -not $found.ContainsKey($match.Groups['version'].Value)) {
            $found[$match.Groups['version'].Value] = $parts[0]
        }
    }

    return $found
}

function Resolve-ExecutionVersion {
    # The ladder every script climbs for a historical version. Returns the version to use, the
    # rung it was found on ('snapshot' launches as is, 'git' builds the commit), and the commit.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)]$Config,
        [Parameter(Mandatory = $true)][string]$Version,
        [switch]$Rebuild
    )

    $snapshots = if ($Rebuild) { @{} } else { Get-ExecutionSnapshotVersions -ProjectRoot $ProjectRoot -Config $Config }
    $commits = Get-ExecutionGitVersions -ProjectRoot $ProjectRoot -Config $Config

    if ($snapshots.ContainsKey($Version)) {
        return @{ Version = $Version; Rung = 'snapshot'; Commit = $null; Executable = $snapshots[$Version] }
    }

    if ($commits.ContainsKey($Version)) {
        return @{ Version = $Version; Rung = 'git'; Commit = $commits[$Version]; Executable = $null }
    }

    $remote = $Config.git.remote
    Write-Host "Version $Version is not held locally. Fetching $remote..." -ForegroundColor Cyan
    $fetch = Invoke-ExecutionGit -ProjectRoot $ProjectRoot -Arguments @('fetch', $remote)
    if ($fetch.ExitCode -ne 0) {
        throw "The remote $remote refused the fetch, so version $Version cannot be obtained:`n$($fetch.Error.Trim())"
    }

    $commits = Get-ExecutionGitVersions -ProjectRoot $ProjectRoot -Config $Config
    if ($commits.ContainsKey($Version)) {
        return @{ Version = $Version; Rung = 'git'; Commit = $commits[$Version]; Executable = $null }
    }

    # The exact version exists nowhere reached. Fall back to the nearest lower one, a snapshot
    # winning over a commit at the same version because it needs no build.
    $wanted = [version]$Version
    $nearest = $null
    foreach ($candidate in @($snapshots.Keys) + @($commits.Keys)) {
        $number = [version]$candidate
        if ($number -lt $wanted -and ($null -eq $nearest -or $number -gt $nearest)) {
            $nearest = $number
        }
    }

    if ($null -eq $nearest) {
        throw "Version $Version was not found in snapshots, in git history, or at $remote, and no lower version exists."
    }

    $lower = $nearest.ToString()
    if ($snapshots.ContainsKey($lower)) {
        Write-Host "Version $Version was not found anywhere; using the nearest lower version $lower (snapshot)." -ForegroundColor Yellow
        return @{ Version = $lower; Rung = 'snapshot'; Commit = $null; Executable = $snapshots[$lower] }
    }

    Write-Host "Version $Version was not found anywhere; using the nearest lower version $lower (commit)." -ForegroundColor Yellow
    return @{ Version = $lower; Rung = 'git'; Commit = $commits[$lower]; Executable = $null }
}

function Write-ExecutionGeneration {
    param([Parameter(Mandatory = $true)][string]$Script)

    Write-Host "$([System.IO.Path]::GetFileNameWithoutExtension($Script).ToUpperInvariant()) - EXECUTION GENERATION $script:ExecutionGeneration" -ForegroundColor Cyan
}

function Get-ExecutionLedgerPath {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)]$Config
    )

    return Join-ExecutionPath -ProjectRoot $ProjectRoot -Relative $Config.layout.ledger
}

function Test-ExecutionLaunchAlive {
    # A ledger entry is alive when its process id still exists and started at the recorded instant.
    # The instant guards against Windows handing a finished process's id to an unrelated program.
    param([Parameter(Mandatory = $true)]$Entry)

    $process = Get-Process -Id ([int]$Entry.pid) -ErrorAction SilentlyContinue
    if ($null -eq $process) {
        return $false
    }

    try {
        $started = if ($Entry.started -is [datetime]) {
            $Entry.started
        }
        else {
            [datetime]::Parse([string]$Entry.started, $null, [System.Globalization.DateTimeStyles]::RoundtripKind)
        }
        return [math]::Abs(($process.StartTime - $started).TotalSeconds) -lt 1
    }
    catch {
        return $false
    }
}

function Save-ExecutionLaunches {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Entries
    )

    $folder = Split-Path -Parent $Path
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    $json = ConvertTo-Json -InputObject @($Entries) -Depth 3
    [System.IO.File]::WriteAllText($Path, ($json -replace "`r`n", "`n") + "`n", [System.Text.UTF8Encoding]::new($false))
}

function Get-ExecutionLaunches {
    # Every launch the family made that is still running. Entries whose process has gone are
    # dropped and the ledger rewritten, so the file never needs a separate prune.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)]$Config
    )

    $path = Get-ExecutionLedgerPath -ProjectRoot $ProjectRoot -Config $Config
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        return @()
    }

    try {
        # Windows PowerShell 5.1 emits a JSON array as one object, so ForEach-Object unrolls it.
        $entries = @(Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json | ForEach-Object { $_ })
    }
    catch {
        Write-Host "The launch ledger is not valid JSON and is being reset: $path" -ForegroundColor Yellow
        $entries = @()
    }

    $alive = @($entries | Where-Object { $null -ne $_ -and (Test-ExecutionLaunchAlive -Entry $_) })
    if ($alive.Count -ne $entries.Count) {
        Save-ExecutionLaunches -Path $path -Entries $alive
    }

    return $alive
}

function Add-ExecutionLaunch {
    # Records one launched process in the ledger. Never throws: a ledger that cannot be written is
    # said on the console and the launch stands.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)]$Config,
        [Parameter(Mandatory = $true)][int]$ProcessId,
        [Parameter(Mandatory = $true)][string]$Version,
        [Parameter(Mandatory = $true)][string]$Script,
        [Parameter(Mandatory = $true)][string]$Folder,
        [Parameter(Mandatory = $true)][string]$Executable
    )

    try {
        $process = Get-Process -Id $ProcessId -ErrorAction Stop
        $entry = [ordered]@{
            pid        = $ProcessId
            started    = $process.StartTime.ToString('o')
            version    = $Version
            script     = $Script
            folder     = $Folder
            executable = $Executable
            recorded   = (Get-Date).ToString('o')
        }

        $path = Get-ExecutionLedgerPath -ProjectRoot $ProjectRoot -Config $Config
        $entries = @(Get-ExecutionLaunches -ProjectRoot $ProjectRoot -Config $Config) + @([pscustomobject]$entry)
        Save-ExecutionLaunches -Path $path -Entries $entries
    }
    catch {
        Write-Host "The launch ledger could not be written: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

function Get-ExecutionStrays {
    # Processes running the project's executable from under the project root that the ledger does
    # not know: launched by hand from Explorer or a shell.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)]$Config
    )

    $name = [System.IO.Path]::GetFileNameWithoutExtension($Config.source.executable)
    $known = @(Get-ExecutionLaunches -ProjectRoot $ProjectRoot -Config $Config | ForEach-Object { [int]$_.pid })
    $prefix = $ProjectRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar

    return @(
        Get-Process -Name $name -ErrorAction SilentlyContinue | Where-Object {
            $_.Id -notin $known -and $null -ne $_.Path -and $_.Path.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)
        }
    )
}

function Test-ExecutionFolderBusy {
    # A folder is busy when a live process runs the executable that sits directly in it, whether the
    # ledger knows the process or not.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)]$Config,
        [Parameter(Mandatory = $true)][string]$Folder
    )

    $wanted = [System.IO.Path]::GetFullPath($Folder).TrimEnd('\', '/')
    foreach ($entry in Get-ExecutionLaunches -ProjectRoot $ProjectRoot -Config $Config) {
        $known = [System.IO.Path]::GetFullPath([string]$entry.folder).TrimEnd('\', '/')
        if ([string]::Equals($known, $wanted, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    foreach ($stray in Get-ExecutionStrays -ProjectRoot $ProjectRoot -Config $Config) {
        $parent = (Split-Path -Parent $stray.Path).TrimEnd('\', '/')
        if ([string]::Equals($parent, $wanted, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Get-ExecutionSlots {
    # The numbered slot folders inside an install folder, each a parallel copy of the same build.
    param([Parameter(Mandatory = $true)][string]$Folder)

    if (-not (Test-Path -LiteralPath $Folder -PathType Container)) {
        return @()
    }

    return @(Get-ChildItem -LiteralPath $Folder -Directory -Force | Where-Object { $_.Name -match $script:ExecutionSlotPattern })
}

function Remove-ExecutionDeadSlots {
    # Deletes every slot folder no live process runs from. A slot that will not go is left in place.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)]$Config,
        [Parameter(Mandatory = $true)][string]$Folder
    )

    $removed = 0
    foreach ($slot in Get-ExecutionSlots -Folder $Folder) {
        if (Test-ExecutionFolderBusy -ProjectRoot $ProjectRoot -Config $Config -Folder $slot.FullName) {
            continue
        }

        Remove-Item -LiteralPath $slot.FullName -Recurse -Force -ErrorAction SilentlyContinue
        if (-not (Test-Path -LiteralPath $slot.FullName)) {
            $removed++
        }
    }

    return $removed
}

function Resolve-ExecutionSlot {
    # The folder a fresh install goes to: the folder itself when nothing runs from it, otherwise a
    # new eight-digit slot inside it, so a running instance never blocks the next build.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)]$Config,
        [Parameter(Mandatory = $true)][string]$Folder
    )

    $reaped = Remove-ExecutionDeadSlots -ProjectRoot $ProjectRoot -Config $Config -Folder $Folder
    if ($reaped -gt 0) {
        Write-Host "Reaped $reaped finished slot(s) under $Folder" -ForegroundColor DarkGray
    }

    if (-not (Test-ExecutionFolderBusy -ProjectRoot $ProjectRoot -Config $Config -Folder $Folder)) {
        return $Folder
    }

    do {
        $slot = Join-Path $Folder ('{0:00000000}' -f (Get-Random -Minimum 10000000 -Maximum 100000000))
    } while (Test-Path -LiteralPath $slot)

    Write-Host "$($Config.project) is running from $Folder; installing beside it in slot $(Split-Path -Leaf $slot)" -ForegroundColor Yellow
    return $slot
}

function Stop-ExecutionLaunch {
    # Asks the main window to close first so the application saves what it holds, then ends the
    # process when it lingers. Returns closed, killed, gone or lingering.
    # lingering means the process still runs after the forced stop and its wait.
    param(
        [Parameter(Mandatory = $true)][int]$ProcessId,
        [switch]$Force,
        [int]$TimeoutSeconds = 5
    )

    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if ($null -eq $process) {
        return 'gone'
    }

    if (-not $Force -and $process.MainWindowHandle -ne [IntPtr]::Zero) {
        [void]$process.CloseMainWindow()
        if ($process.WaitForExit($TimeoutSeconds * 1000)) {
            return 'closed'
        }
    }

    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
    if ($process.WaitForExit($TimeoutSeconds * 1000)) {
        return 'killed'
    }

    return 'lingering'
}
