<#
.SYNOPSIS
    Run the project test suite.
.DESCRIPTION
    Runs every test project under the tests folder from the repository root.
    The test projects are discovered on disk, not through the solution. By
    default every project runs, the convention test project included. -Main
    skips the convention project and -Convention runs it alone.
    -Platform picks the test projects mapped to one platform in Test.json; the
    convention project still runs beside them unless -Main is given. A Windows
    project on a host that is not Windows is skipped with a notice line.
    Given symbol names, only the tests a change inside those symbols can affect
    run: TraceSymbol.ps1 finds them, and each test project runs with a filter that
    selects them. A project with no affected test is skipped. The convention
    project runs whole, since its audits read every source file.
    Supports VSTest filters, test discovery, repeated runs for investigating
    intermittent failures, and additional arguments passed to dotnet test.

    Every project builds first, one by one, then all run at once, each in its
    own process; a project's output prints whole when it ends. Every failing
    project is named. The script stops after the first failed run and returns
    the exit code of the first failing project in discovery order.

    Every project-specific value - project name, tests folder, convention
    project, platform projects - lives in Test.json. This script carries none, so the file is
    identical in every project at the same generation.
.PARAMETER Name
    One or more symbol names. Runs only the tests a change inside them can affect. Positional.
.PARAMETER Configuration
    Build configuration used by dotnet test: Debug (default) or Release.
.PARAMETER Filter
    VSTest filter expression used to select tests.
.PARAMETER Project
    Name of a single test project to run. Defaults to all. Overrides the scope switches and -Platform.
.PARAMETER Platform
    All (default), Internal or Windows. A platform other than All runs only the test projects Test.json
    maps to it, and the convention project unless -Main is given. Cannot be combined with -Convention.
.PARAMETER All
    Run every test project, the convention project included. This is the default. Alias: -a.
.PARAMETER Convention
    Run the convention test project only. Alias: -c.
.PARAMETER Main
    Run every test project except the convention project. Alias: -m.
.PARAMETER NoBuild
    Pass --no-build to dotnet test. This also prevents restore.
.PARAMETER NoRestore
    Pass --no-restore while still allowing the test project to build.
.PARAMETER List
    Discover and list matching tests without running them. Cannot be combined with Repeat.
.PARAMETER Repeat
    Number of test runs. Execution stops on the first failure. Defaults to 1.
.PARAMETER Verbosity
    dotnet test logging verbosity. Defaults to minimal.
.PARAMETER AdditionalArguments
    Additional arguments forwarded unchanged to dotnet test.
.PARAMETER Help
    Display this help and exit without running tests. The alias -? is supported.
.EXAMPLE
    test
    Run the full Debug test suite once, the convention tests included.
.EXAMPLE
    test LTwin
    Run the tests a change inside LTwin can affect, and the convention tests.
.EXAMPLE
    test LTwin -m
    Run the tests a change inside LTwin can affect, without the convention tests.
.EXAMPLE
    test -m
    Run the Debug test suite once, without the convention tests.
.EXAMPLE
    test -c
    Run the convention tests only.
.EXAMPLE
    test -Platform Internal
    Run the portable tests and the convention tests.
.EXAMPLE
    test -Platform Windows -m
    Run the Windows tests only.
.EXAMPLE
    test -Configuration Release
    Run the Release test suite.
.EXAMPLE
    test -Project <name>
    Run one test project.
.EXAMPLE
    test -Filter 'FullyQualifiedName~Tests'
    Run tests whose fully-qualified names match the filter.
.EXAMPLE
    test -Repeat 10 -NoBuild
    Reuse existing build output and stop at the first failure across ten runs.
.EXAMPLE
    test -List
    List discovered tests without executing them.
.EXAMPLE
    test -AdditionalArguments '--logger', 'trx', '--results-directory', 'TestResults'
    Forward logger and results-directory options to dotnet test.
#>
#requires -Version 5.1
# TEST - TEST GENERATION 5.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the executed outcome changes.
# A generation names how the family finds and runs the tests. Two projects on the same generation
# run the same projects the same way from the same tree layout.
# Generation 2: every *.csproj one level under the tests folder is a test project, discovered on disk
# and run one by one through dotnet test; by default every project runs, the convention project
# included; -Main skips the convention project, -Convention runs it alone, -Project picks one by name;
# the first failing project ends the run and its exit code is returned.
# Generation 3: everything in generation 2; given symbol names, TraceSymbol.ps1 names the affected tests,
# each test project runs with a filter selecting them - by method, by class when the method filter
# grows too long, whole when the class filter does too - a project with none is skipped, and the
# convention project runs whole.
# Generation 4: everything in generation 3; Test.json maps each platform to one test project or a list, -Platform
# runs only those projects beside the convention project, -Main still drops the convention project, and a
# Windows project on a host that is not Windows is skipped with a notice line instead of failing.
# Generation 5: everything in generation 4, except that every project builds first, one by one, and then
# all run at once in their own processes; every failing project is named, not only the first, and the
# first failing project in discovery order gives the exit code.
# Every project-specific value lives in Test.json, so this file is identical in every project at
# this generation.
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string[]]$Name,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [string]$Filter,

    [string]$Project,

    [ValidateSet('All', 'Internal', 'Windows')]
    [string]$Platform = 'All',

    [Alias('a')]
    [switch]$All,

    [Alias('c')]
    [switch]$Convention,

    [Alias('m')]
    [switch]$Main,

    [switch]$NoBuild,

    [switch]$NoRestore,

    [switch]$List,

    [ValidateRange(1, [int]::MaxValue)]
    [int]$Repeat = 1,

    [ValidateSet('quiet', 'minimal', 'normal', 'detailed', 'diagnostic')]
    [string]$Verbosity = 'minimal',

    [Parameter(ValueFromRemainingArguments)]
    [string[]]$AdditionalArguments,

    [Alias('?')]
    [switch]$Help
)

if ($Help) {
    @'
NAME
    Test.ps1

SYNOPSIS
    Run the project test suite.

SYNTAX
    test [<name> ...] [-Configuration <Debug|Release>] [-Filter <expression>]
        [-Project <name>] [-Platform <All|Internal|Windows>]
        [-All | -Convention | -Main]
        [-NoBuild] [-NoRestore] [-List]
        [-Repeat <count>] [-Verbosity <level>]
        [-AdditionalArguments <arguments[]>] [-Help]

OPTIONS
    <name>
        Symbol to start from: a type, method, property or field. Several are
        allowed. Runs only the tests a change inside them can affect, found by
        TraceSymbol.ps1, and the convention project whole.

    -Configuration <Debug|Release>
        Build configuration. Defaults to Debug.

    -Filter <expression>
        VSTest filter expression used to select tests.

    -Project <name>
        Run one test project under the tests folder. Defaults to all.
        Overrides the scope switches and -Platform.

    -Platform <All|Internal|Windows>
        Run the test projects Test.json maps to the platform, and the
        convention project unless -Main is given. Defaults to All, every
        project. A Windows project on a host that is not Windows is skipped
        with a notice line. Cannot be combined with -Convention.

    -All, -a
        Run every test project, the convention project included. Default.

    -Convention, -c
        Run the convention test project only.

    -Main, -m
        Run every test project except the convention project.

    -NoBuild
        Reuse existing build output and skip restore.

    -NoRestore
        Skip restore while still allowing the test project to build.

    -List
        List matching tests without running them. Cannot be used with -Repeat.

    -Repeat <count>
        Number of runs, stopping at the first failure. Defaults to 1.

    -Verbosity <level>
        dotnet test logging verbosity. Defaults to minimal.

    -AdditionalArguments <arguments[]>
        Additional arguments forwarded unchanged to dotnet test.

    -Help, -?
        Display this help and exit without running tests.

EXAMPLES
    test
        Run the full Debug test suite once, the convention tests included.

    test LTwin
        Run the tests a change inside LTwin can affect, and the convention tests.

    test LTwin -m
        Run the tests a change inside LTwin can affect, without the convention tests.

    test -m
        Run the Debug test suite once, without the convention tests.

    test -c
        Run the convention tests only.

    test -Platform Internal
        Run the portable tests and the convention tests.

    test -Platform Windows -m
        Run the Windows tests only.

    test -Configuration Release
        Run the Release test suite.

    test -Project <name>
        Run one test project.

    test -Filter 'FullyQualifiedName~Tests'
        Run tests whose fully-qualified names match the filter.

    test -Repeat 10 -NoBuild
        Reuse build output and stop at the first failure across ten runs.

    test -List
        List discovered tests without executing them.

    test -AdditionalArguments '--logger', 'trx'
        Forward additional options to dotnet test.
'@ | Write-Host
    exit 0
}

Write-Host 'TEST - TEST GENERATION 5' -ForegroundColor Blue

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# dotnet and git write UTF-8. A console still on the OEM code page, as one opened by the dispatcher
# without a profile is, would show every non-ASCII line garbled, so this process reads and writes
# UTF-8. Process-local: the calling console keeps its own code page.
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding

function Get-OrdinalKey {
    # Sort-Object compares text by culture, which Windows PowerShell 5.1 and pwsh 7 order differently.
    # Uppercase hexadecimal UTF-16 code units compare alike under every culture, so this key sorts ordinally.
    param([string]$Text)
    return [System.BitConverter]::ToString([System.Text.Encoding]::BigEndianUnicode.GetBytes($Text)).Replace('-', '')
}

function Read-TestConfig {
    $configPath = Join-Path $PSScriptRoot 'Test.json'
    if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
        throw "The test configuration was not found: $configPath"
    }

    try {
        $config = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The test configuration is not valid JSON: $configPath`n$($_.Exception.Message)"
    }

    foreach ($key in @('generation', 'tests', 'convention', 'platforms')) {
        if (-not ($config.PSObject.Properties.Name -contains $key)) {
            throw "The test configuration must contain a $key property: $configPath"
        }
    }

    if ([int]$config.generation -ne 5) {
        throw "The test configuration is generation $($config.generation); this script is generation 5."
    }

    return $config
}

if (@($All, $Convention, $Main | Where-Object { $_ }).Count -gt 1) {
    throw '-All, -Convention and -Main cannot be combined.'
}

if ($Convention -and $Platform -ne 'All') {
    throw '-Convention and -Platform cannot be combined.'
}

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$config = Read-TestConfig
$testsRoot = Join-Path $root ([string]$config.tests)

if (-not (Test-Path -LiteralPath $testsRoot -PathType Container)) {
    throw "The test folder was not found: $testsRoot"
}

$projects = @(
    Get-ChildItem -LiteralPath $testsRoot -Directory |
        ForEach-Object { Get-ChildItem -LiteralPath $_.FullName -Filter '*.csproj' -File } |
        Sort-Object -Property @{ Expression = { Get-OrdinalKey $_.Name } }
)

if (-not [string]::IsNullOrWhiteSpace($Project)) {
    $projects = @($projects | Where-Object { $_.BaseName -eq $Project })
    if ($projects.Count -eq 0) {
        throw "Test project was not found under tests: $Project"
    }
}
elseif ($Convention) {
    $projects = @($projects | Where-Object { $_.BaseName -eq [string]$config.convention })
    if ($projects.Count -eq 0) {
        throw "The convention test project was not found under tests: $($config.convention)"
    }
}
else {
    if ($Platform -ne 'All') {
        $mapped = $config.platforms.PSObject.Properties[$Platform]
        if ($null -eq $mapped) {
            throw "The test configuration maps no project to the platform: $Platform"
        }

        $platformProjects = @($mapped.Value | ForEach-Object { [string]$_ })
        foreach ($platformProject in $platformProjects) {
            if (@($projects | Where-Object { $_.BaseName -eq $platformProject }).Count -eq 0) {
                throw "The $Platform test project was not found under tests: $platformProject"
            }
        }

        $projects = @($projects | Where-Object { $platformProjects -contains $_.BaseName -or $_.BaseName -eq [string]$config.convention })
    }

    if ($Main) {
        $projects = @($projects | Where-Object { $_.BaseName -ne [string]$config.convention })
    }
}

# A Windows test project targets a Windows framework, which builds and runs on Windows alone.
$windowsMapped = $config.platforms.PSObject.Properties['Windows']
if ($null -ne $windowsMapped -and [System.Environment]::OSVersion.Platform -ne [System.PlatformID]::Win32NT) {
    $windowsProjects = @($windowsMapped.Value | ForEach-Object { [string]$_ })
    if (@($projects | Where-Object { $windowsProjects -contains $_.BaseName }).Count -gt 0) {
        Write-Host "$($windowsProjects -join ', ') is skipped: this host is not Windows."
        $projects = @($projects | Where-Object { $windowsProjects -notcontains $_.BaseName })
        if ($projects.Count -eq 0) {
            exit 0
        }
    }
}

if ($projects.Count -eq 0) {
    throw "No test project was found under: $testsRoot"
}

# A filter travels on the dotnet command line, whose length Windows caps near 32K characters, so a
# selection longer than this falls back from methods to classes, and from classes to the whole project.
$selectionLimit = 20000
$selections = @{}

if ($null -ne $Name -and $Name.Count -gt 0) {
    $detector = Join-Path $PSScriptRoot 'TraceSymbol.ps1'
    if (-not (Test-Path -LiteralPath $detector -PathType Leaf)) {
        throw "TraceSymbol.ps1 was not found: $detector"
    }

    $detection = & $detector -Name $Name -Json | Out-String | ConvertFrom-Json
    $detected = @($detection.tests)
    Write-Host ("{0}: {1} affected symbol(s), {2} affected test(s)." -f ($Name -join ', '), @($detection.symbols).Count, $detected.Count) -ForegroundColor Cyan

    $projects = @(
        foreach ($testProject in $projects) {
            if ($testProject.BaseName -eq [string]$config.convention) {
                $testProject
                continue
            }

            $selected = @($detected | Where-Object { $_.project -eq $testProject.BaseName })
            if ($selected.Count -eq 0) {
                continue
            }

            $selection = @($selected | ForEach-Object { "FullyQualifiedName~.$($_.class).$($_.method)" } | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } } -Unique) -join '|'
            if ($selection.Length -gt $selectionLimit) {
                $selection = @($selected | ForEach-Object { "FullyQualifiedName~.$($_.class)." } | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } } -Unique) -join '|'
            }

            if ($selection.Length -gt $selectionLimit) {
                Write-Host "$($testProject.BaseName): the selection is too long for a filter, so the whole project runs." -ForegroundColor Yellow
            }
            else {
                $selections[$testProject.BaseName] = $selection
            }

            $testProject
        }
    )

    if ($projects.Count -eq 0) {
        Write-Host 'No affected test was found.' -ForegroundColor Green
        exit 0
    }
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    throw 'The .NET SDK is required to run tests, but dotnet was not found on PATH.'
}

if ($List -and $Repeat -ne 1) {
    throw '-List cannot be combined with -Repeat.'
}

$commonArguments = @(
    '--configuration'
    $Configuration
    '--verbosity'
    $Verbosity
    '--nologo'
)

# The projects run at once, and two builds of one shared reference collide on its output files, so
# every project builds first, one by one, and each run then reuses that output.
if (-not $NoBuild) {
    $buildArguments = $commonArguments
    if ($NoRestore) {
        $buildArguments += '--no-restore'
    }

    foreach ($testProject in $projects) {
        & $dotnet.Source build $testProject.FullName @buildArguments
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build failed in $($testProject.BaseName) (exit code $LASTEXITCODE)." -ForegroundColor Red
            exit $LASTEXITCODE
        }
    }
}

$commonArguments += '--no-build'

if ($List) {
    $commonArguments += '--list-tests'
}

if ($null -ne $AdditionalArguments -and $AdditionalArguments.Count -gt 0) {
    $commonArguments += $AdditionalArguments
}

function ConvertTo-Argument {
    # Start-Process takes one command line, so each argument is quoted the way the C runtime splits it.
    param([string]$Text)
    if ($Text -ne '' -and $Text -notmatch '[\s"]') {
        return $Text
    }

    return '"' + (($Text -replace '(\\*)"', '$1$1\"') -replace '(\\+)$', '$1$1') + '"'
}

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$exitCode = 0
$running = @()
$outputRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('Test-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $outputRoot

Push-Location $root
try {
    for ($run = 1; $run -le $Repeat; $run++) {
        if ($Repeat -gt 1) {
            Write-Host "Test run $run of $Repeat" -ForegroundColor Cyan
        }

        # Every project runs at once in its own process. Its output goes to a file and is printed whole
        # when the process ends, so the lines of two projects never interleave.
        $running = @(foreach ($testProject in $projects) {
            $projectFilter = $Filter
            if ($selections.ContainsKey($testProject.BaseName)) {
                $selection = $selections[$testProject.BaseName]
                $projectFilter = if ([string]::IsNullOrWhiteSpace($Filter)) { $selection } else { "($Filter)&($selection)" }
            }

            $filterArguments = @()
            if (-not [string]::IsNullOrWhiteSpace($projectFilter)) {
                $filterArguments = @('--filter', $projectFilter)
            }

            $arguments = @('test', $testProject.FullName) + $filterArguments + $commonArguments
            $outputPath = Join-Path $outputRoot "$($testProject.BaseName).out"
            $errorPath = Join-Path $outputRoot "$($testProject.BaseName).err"
            $process = Start-Process -FilePath $dotnet.Source -ArgumentList (@($arguments | ForEach-Object { ConvertTo-Argument $_ }) -join ' ') `
                -WorkingDirectory $root -NoNewWindow -PassThru -RedirectStandardOutput $outputPath -RedirectStandardError $errorPath
            # Windows PowerShell 5.1 reports no exit code for a process whose handle was never read.
            $null = $process.Handle
            [pscustomobject]@{ Project = $testProject; Process = $process; Output = $outputPath; Error = $errorPath; Shown = $false }
        })

        while (@($running | Where-Object { -not $_.Shown }).Count -gt 0) {
            foreach ($entry in @($running | Where-Object { -not $_.Shown -and $_.Process.HasExited })) {
                $entry.Process.WaitForExit()
                $entry.Shown = $true
                Write-Host ('{0} ({1:N1}s)' -f $entry.Project.BaseName, ($entry.Process.ExitTime - $entry.Process.StartTime).TotalSeconds) -ForegroundColor Cyan
                foreach ($path in @($entry.Output, $entry.Error)) {
                    [System.IO.File]::ReadAllLines($path, [Console]::OutputEncoding)
                }
            }

            Start-Sleep -Milliseconds 200
        }

        # Every failing project is named, and the first in discovery order gives the exit code.
        foreach ($entry in $running) {
            if ($entry.Process.ExitCode -ne 0) {
                Write-Host "Tests failed in $($entry.Project.BaseName) on run $run of $Repeat (exit code $($entry.Process.ExitCode))." -ForegroundColor Red
                if ($exitCode -eq 0) {
                    $exitCode = $entry.Process.ExitCode
                }
            }
        }

        if ($exitCode -ne 0) {
            break
        }
    }
}
finally {
    foreach ($entry in $running) {
        if (-not $entry.Process.HasExited) {
            $entry.Process.Kill()
        }
    }

    Remove-Item -LiteralPath $outputRoot -Recurse -Force -ErrorAction SilentlyContinue
    Pop-Location
    $stopwatch.Stop()
}

if ($exitCode -eq 0) {
    if ($List) {
        Write-Host ("Test discovery completed in {0:N1}s." -f $stopwatch.Elapsed.TotalSeconds) -ForegroundColor Green
    }
    else {
        Write-Host ("Tests passed ({0} project(s), {1} run(s), {2:N1}s)." -f $projects.Count, $Repeat, $stopwatch.Elapsed.TotalSeconds) -ForegroundColor Green
    }
}

exit $exitCode
