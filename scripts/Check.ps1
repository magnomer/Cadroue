#requires -Version 5.1
# CHECK - CHECK GENERATION 1.
[CmdletBinding()]
param(
    [string[]]$Grep = @(),
    [switch]$NoBuild,
    [switch]$NoTest,
    [switch]$NoAudit,
    [switch]$Open,
    [Alias('?')]
    [switch]$Help
)

if ($Help) {
    @'
NAME
    Check.ps1

SYNOPSIS
    Run the whole done gate - build, full tests and every audit in $audits - and print one tally line.

SYNTAX
    check [-Grep <pattern>[,<pattern>...]] [-NoBuild] [-NoTest] [-NoAudit] [-Open] [-Verbose] [-Help]

OPTIONS
    -Grep <pattern>[,<pattern>...]
        Search the .cs and .xaml files under src and tests for leftover names.
        Every hit counts as a failure.

    -NoBuild
        Skip the build. A run with any skip flag never counts as done.

    -NoTest
        Skip the full Test.ps1 run.

    -NoAudit
        Skip every audit.

    -Open
        Let each audit that writes a page open it. By default an audit
        with a -NoOpen parameter receives it, so a check run opens nothing.

    -Verbose
        Print up to 200 lines of an audit's output. Only an audit that
        fails a counter or has unparsable output prints them.

    -Help
        Display this help and exit without building, testing or auditing.

OUTPUT
    The last line reads OK or FAIL, then one counter per stage.
    An audit with WARN rows adds WARN and their sum after its counter.
    A warning never fails the run.
    The exit code is 0 on OK and 1 on FAIL.

EXAMPLES
    check
        Run the full gate.

    check -Grep 'LOldName','POldName'
        Run the full gate and fail on any leftover of the two names.
'@ | Write-Host
    exit 0
}

Write-Host 'CHECK - CHECK GENERATION 1' -ForegroundColor Blue

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$kWarn = [string]::new([char[]](0xACBD, 0xACE0))
$kErr = [string]::new([char[]](0xC624, 0xB958))
$kPass = [string]::new([char[]](0xD1B5, 0xACFC))
$kFail = [string]::new([char[]](0xC2E4, 0xD328))

$audits = @('AuditNames', 'AuditLines', 'AuditComments', 'AuditFake', 'AuditFault', 'AuditObject', 'AuditPlatform', 'AuditStructure', 'AuditUI', 'AuditEncoding')

$tally = [System.Collections.Generic.List[string]]::new()
$failed = $false

function Get-OrdinalKey {
    # Sort-Object compares text by culture, which Windows PowerShell 5.1 and pwsh 7 order differently.
    # Uppercase hexadecimal UTF-16 code units compare alike under every culture, so this key sorts ordinally.
    param([string]$Text)
    return [System.BitConverter]::ToString([System.Text.Encoding]::BigEndianUnicode.GetBytes($Text)).Replace('-', '')
}

function Invoke-Captured {
    param([scriptblock]$Block)
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
    } catch {
        $threw = $_.Exception.Message
    }
    [pscustomobject]@{ Lines = $lines.ToArray(); Exit = $LASTEXITCODE; Threw = $threw }
}

function Get-Counters {
    param([string[]]$Lines)
    $start = [Array]::IndexOf($Lines, 'Result')
    if ($start -lt 0) { return $null }
    $rows = [System.Collections.Generic.List[object]]::new()
    $index = $start + 1
    while ($index -lt $Lines.Count -and $Lines[$index] -match '^-[- ]*$|^Status\s') { $index++ }
    while ($index -lt $Lines.Count -and $Lines[$index].Trim() -ne '') {
        if ($Lines[$index] -notmatch '^(OK|FAIL|WARN)\s+([\d,]+)\s{2}(.+?)(\s{2,}|$)') { return $null }
        $rows.Add([pscustomobject]@{ Status = $Matches[1]; Label = $Matches[3].Trim(); Count = [int]($Matches[2] -replace ',', '') })
        $index++
    }
    if ($rows.Count -eq 0) { return $null }
    return $rows.ToArray()
}

function Show-Lines {
    param([string[]]$Lines, [int]$Max = 40)
    $Lines | Select-Object -First $Max | ForEach-Object { "    $_" }
    if ($Lines.Count -gt $Max) { "    ... ($($Lines.Count - $Max) more)" }
}

if (-not $NoBuild) {
    "=== build"
    $solutions = @(Get-ChildItem -LiteralPath $root -File | Where-Object { $_.Extension -in '.slnx', '.sln' })
    if ($solutions.Count -ne 1) { throw "The root must hold exactly one solution file, but it holds $($solutions.Count): $root" }
    $solution = $solutions[0].FullName
    $r = Invoke-Captured { dotnet build $solution --no-incremental }
    $warn = @($r.Lines | Where-Object { $_ -match "(warning|$kWarn)\s+[A-Z]+\d+" } | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } } -Unique)
    $err = @($r.Lines | Where-Object { $_ -match "(error|$kErr)\s+[A-Z]+\d+" } | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } } -Unique)
    $ok = ($r.Exit -eq 0) -and -not $r.Threw -and $err.Count -eq 0 -and $warn.Count -eq 0
    if (-not $ok) {
        $failed = $true
        Show-Lines ($err + $warn)
        if ($r.Threw) { "    threw: $($r.Threw)" }
        if ($err.Count -eq 0 -and $warn.Count -eq 0) { Show-Lines ($r.Lines | Select-Object -Last 15) }
    }
    $tally.Add("build $($warn.Count)w/$($err.Count)e" + $(if ($r.Exit -ne 0) { " exit$($r.Exit)" } else { '' }))
}

if (-not $NoTest) {
    "=== test"
    $r = Invoke-Captured { & "$root/scripts/Test.ps1" }
    $summary = @($r.Lines | Where-Object { $_ -match "$kPass!|$kFail!|\[FAIL\]|Tests failed|Passed!|Failed!" })
    $bad = ($r.Exit -ne 0) -or $r.Threw -or @($summary | Where-Object { $_ -match "$kFail!|\[FAIL\]|Tests failed|Failed!" }).Count -gt 0
    $noSummary = $summary.Count -eq 0
    Show-Lines $summary
    if ($bad -or $noSummary) {
        $failed = $true
        if ($r.Threw) { "    threw: $($r.Threw)" }
        if ($noSummary) { "    no pass/fail summary found; tail:"; Show-Lines ($r.Lines | Select-Object -Last 15) }
    }
    $tally.Add('test ' + $(if ($bad) { "FAIL(exit$($r.Exit))" } elseif ($noSummary) { 'UNPARSED' } else { 'pass' }))
}

if (-not $NoAudit) {
    foreach ($name in $audits) {
        "=== $name"
        $path = "$root/scripts/$name.ps1"
        if (-not (Test-Path $path)) {
            $failed = $true
            "    missing script"
            $tally.Add("$name MISSING")
            continue
        }
        # An audit that writes a page opens it unless told otherwise, and a check run opens none unless -Open is given.
        $quiet = @{}
        if (-not $Open -and (Get-Command -Name $path).Parameters.ContainsKey('NoOpen')) { $quiet['NoOpen'] = $true }
        $r = Invoke-Captured { & $path @quiet }
        $counters = Get-Counters $r.Lines
        $sum = 0
        $warnSum = 0
        if ($null -eq $counters) {
            $failed = $true
            "    UNPARSED (no Result section in output)"
            $state = 'UNPARSED'
        }
        else {
            foreach ($counter in $counters) {
                if ($counter.Status -eq 'WARN') {
                    $warnSum += $counter.Count
                    "    WARNING $($counter.Label): $($counter.Count)"
                    continue
                }
                $sum += $counter.Count
                if ($counter.Count -gt 0) { "    $($counter.Label): $($counter.Count)" }
            }
            $state = "$sum" + $(if ($warnSum -gt 0) { " WARN $warnSum" } else { '' })
        }
        $exitBad = $null -ne $r.Exit -and $r.Exit -ne 0 -and -not ($r.Exit -eq 1 -and $sum -gt 0)
        if ($r.Threw -or $exitBad) {
            $failed = $true
            "    script error (exit $($r.Exit)) $($r.Threw)"
            Show-Lines ($r.Lines | Select-Object -Last 15)
            $state += " ERR"
        }
        if ($sum -gt 0) {
            $failed = $true
            if ($VerbosePreference -ne 'Continue') { "    rerun with -Verbose or open the audit report for the list" }
        }
        if ($sum -gt 0 -or $null -eq $counters) { Show-Lines $r.Lines 200 | ForEach-Object { Write-Verbose $_ } }
        $tally.Add("$($name -replace '^audit') $state")
    }
}

if ($Grep.Count -gt 0) {
    "=== grep"
    $files = Get-ChildItem -Recurse -File -Path "$root/src", "$root/tests" -Include *.cs, *.xaml -ErrorAction SilentlyContinue
    $hits = 0
    foreach ($pattern in $Grep) {
        $found = @($files | Select-String -Pattern $pattern -Encoding UTF8)
        $hits += $found.Count
        "  /$pattern/ $($found.Count)"
        $found | ForEach-Object { "    $(Resolve-Path -Relative $_.Path):$($_.LineNumber): $($_.Line.Trim())" }
    }
    if ($hits -gt 0) { $failed = $true }
    $tally.Add("grep $hits")
}

""
$verdict = if ($failed) { 'FAIL' } else { 'OK' }
"$verdict | $($tally -join ' | ')"
exit $(if ($failed) { 1 } else { 0 })
