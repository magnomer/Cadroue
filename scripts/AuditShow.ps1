<#
.SYNOPSIS
Opens the audit summary page, or offers to rerun the audit when the code changed since it was written.

.DESCRIPTION
Reads the configuration from Audit.json next to this script, then performs these actions:
  1. Finds the summary page {report.directory}\{prefix}{version}.html of the current version.
  2. Compares the code fingerprint stored in the page with the current one, both computed
     by Audit.fingerprint.ps1. The report folder is left out of the fingerprint.
  3. When they match, opens the page.
  4. When they differ, or the page is missing or holds no fingerprint, asks whether to
     rerun the audit. Yes runs Audit.ps1, which opens its new page itself. No opens the
     old page, if there is one. Without an interactive console the answer is no.
The run exits with Audit.ps1's exit code after a rerun, else 0.

.PARAMETER ConfigPath
Path to the JSON configuration. Defaults to Audit.json next to this script.

.PARAMETER NoOpen
Report the page and its state without opening it. A rerun is passed -NoOpen too.

.PARAMETER Help
Display this help and exit. The alias -? is supported.

.EXAMPLE
AuditShow
#>
#requires -Version 5.1
# AUDITSHOW - AUDIT GENERATION 21.
[CmdletBinding()]
param(
    [string]$ConfigPath,
    [switch]$NoOpen,
    [Alias('?')]
    [switch]$Help
)

if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    $ConfigPath = Join-Path $PSScriptRoot 'Audit.json'
}

if ($Help) {
    @'
NAME
    AuditShow.ps1

SYNOPSIS
    Open the audit summary page, or offer to rerun the audit when the code
    changed since the page was written.

SYNTAX
    AuditShow [-ConfigPath <path>] [-NoOpen] [-Help]

RUN
    Finds the summary page of the current version, as configured in
    Audit.json. Audit stores a code fingerprint in the page: the HEAD commit
    and the content of every changed or untracked file, outside the report
    folder. When the current fingerprint matches, the page opens. When it
    differs, or the page is missing, AuditShow asks whether to rerun the
    audit. Yes runs Audit, which opens its new page. No opens the old page.
    Without an interactive console the answer is no.

OPTIONS
    -ConfigPath <path>
        JSON configuration file. Defaults to .\Audit.json.

    -NoOpen
        Report the page and its state without opening it. A rerun is passed
        -NoOpen too.

    -Help, -?
        Display this help and exit.

EXAMPLES
    AuditShow
        Open the summary page, or ask to rerun the audit first.
'@ | Write-Host
    exit 0
}

Write-Host 'AUDITSHOW - AUDIT GENERATION 21' -ForegroundColor Blue

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding

. (Join-Path $PSScriptRoot 'Audit.fingerprint.ps1')

$repoRootFull = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')).TrimEnd('\', '/')
$configPathFull = [System.IO.Path]::GetFullPath($ConfigPath)
if (-not (Test-Path -LiteralPath $configPathFull -PathType Leaf)) {
    throw "The audit configuration was not found: $configPathFull"
}
$config = Get-Content -LiteralPath $configPathFull -Raw -Encoding UTF8 | ConvertFrom-Json
$reportDirectory = [string]$config.report.directory

$version = '0.0.0'
$versionPathFull = [System.IO.Path]::GetFullPath((Join-Path $repoRootFull ([string]$config.report.versionFile)))
if ([System.IO.File]::Exists($versionPathFull)) {
    $versionData = Get-Content -LiteralPath $versionPathFull -Raw -Encoding UTF8 | ConvertFrom-Json
    $versionValue = $versionData
    foreach ($segment in (([string]$config.report.versionKey) -split '\.')) {
        if ($null -eq $versionValue -or -not ($versionValue.PSObject.Properties.Name -contains $segment)) { $versionValue = $null; break }
        $versionValue = $versionValue.$segment
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$versionValue)) { $version = [string]$versionValue }
}

$pagePathFull = Join-Path (Join-Path $repoRootFull $reportDirectory) ('{0}{1}.html' -f [string]$config.report.prefix, $version)
$pageExists = [System.IO.File]::Exists($pagePathFull)
$stored = ''
if ($pageExists) {
    $pageText = [System.IO.File]::ReadAllText($pagePathFull, [System.Text.UTF8Encoding]::new($false))
    if ($pageText -match '"fingerprint":"([0-9a-f]*)"') { $stored = $Matches[1] }
}
$current = Get-AuditFingerprint -Root $repoRootFull -Excluded $reportDirectory

if ($pageExists -and $stored.Length -gt 0 -and $stored -eq $current) {
    Write-Host 'The code is unchanged since the last audit.' -ForegroundColor Green
}
else {
    $reason = if (-not $pageExists) { "No audit page for version $version." }
              elseif ($stored.Length -eq 0) { 'The audit page holds no code fingerprint.' }
              else { 'The code changed since the last audit.' }
    Write-Host $reason -ForegroundColor Yellow

    $rerun = $false
    if ([Console]::IsInputRedirected) {
        Write-Host 'No interactive console, so the audit is not rerun.' -ForegroundColor DarkGray
    }
    else {
        $answer = Read-Host 'Rerun the audit? [Y/n]'
        $rerun = [string]::IsNullOrWhiteSpace($answer) -or $answer.Trim().StartsWith('y', [System.StringComparison]::OrdinalIgnoreCase)
    }

    if ($rerun) {
        $quiet = @{}
        if ($NoOpen) { $quiet['NoOpen'] = $true }
        & (Join-Path $PSScriptRoot 'Audit.ps1') -ConfigPath $configPathFull @quiet
        exit $LASTEXITCODE
    }
    if (-not $pageExists) {
        exit 0
    }
}

Write-Host "Report: $pagePathFull"
if (-not $NoOpen) {
    Start-Process -FilePath $pagePathFull
}

exit 0
