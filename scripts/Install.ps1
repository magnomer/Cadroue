<#
.SYNOPSIS
    Install the bare-name dispatchers so every script here runs as a plain command.
.DESCRIPTION
    Writes one small .cmd dispatcher per script in this folder into a folder on the
    user PATH, so typing run, build, stop or any other script name in cmd or
    PowerShell runs the matching script of the nearest project: the dispatcher
    walks up from the current folder until it finds <name>.ps1 in a scripts folder
    or, for a project still keeping its scripts at the root, in the folder itself,
    then runs it with pwsh when present and Windows PowerShell otherwise. The
    dispatchers are byte-identical and name nothing of this project, so one
    installation serves every project on the machine; a dispatcher this project
    has no script for is left alone, since another project may own it.

    A bare name reaches its script in any letter case. Windows finds the
    dispatcher and the script without regard to case, so audit, auditlines,
    auditlineshistory, auditcomments, auditencoding, auditfake, auditfault,
    auditnames, auditnamesnew, auditobject, auditobjecthistory, auditplatform,
    auditstructure and auditui run Audit.ps1, AuditLines.ps1,
    AuditLinesHistory.ps1, AuditComments.ps1, AuditEncoding.ps1, AuditFake.ps1,
    AuditFault.ps1, AuditNames.ps1, AuditNamesNew.ps1, AuditObject.ps1,
    AuditObjectHistory.ps1, AuditPlatform.ps1, AuditStructure.ps1 and AuditUI.ps1.
    The helpers work the same way, for example timemachine and syncnames.
    An older dispatcher file named in another case keeps working and is
    updated in place.

    A script may carry a second name: each "# ALIAS <name>" line in its header
    writes one more dispatcher under that name. An alias dispatcher still runs
    <alias>.ps1 when the nearest project has one, and its script otherwise, so
    detector runs TraceSymbol.ps1 here and detector.ps1 in a project not yet
    renamed.

    The destination folder is created when missing and appended to the user PATH
    when absent; the current session's PATH is updated as well. Consoles already
    open elsewhere see the new PATH only after they are reopened.
.PARAMETER Destination
    Folder the dispatchers are written to. Defaults to bin under the user profile.
.PARAMETER Help
    Display this help and exit without writing anything. The alias -? is supported.
.EXAMPLE
    install
    Install the dispatchers into %USERPROFILE%\bin and put that folder on the PATH.
.EXAMPLE
    install -Destination D:\tools
    Install the dispatchers into D:\tools instead.
#>
#requires -Version 5.1
# INSTALL - INSTALL GENERATION 1.
[CmdletBinding()]
param(
    [string]$Destination = (Join-Path $env:USERPROFILE 'bin'),
    [Alias('?')]
    [switch]$Help
)

if ($Help) {
    @'
NAME
    Install.ps1

SYNOPSIS
    Install the bare-name dispatchers so every script here runs as a plain command.

SYNTAX
    install [-Destination <folder>] [-Help]

OPTIONS
    -Destination <folder>
        Folder the dispatchers are written to. Defaults to %USERPROFILE%\bin.

    -Help, -?
        Display this help and exit without writing anything.

EXAMPLES
    install
        Install the dispatchers into %USERPROFILE%\bin and put that folder on the PATH.

    install -Destination D:\tools
        Install the dispatchers into D:\tools instead.
'@ | Write-Host
    exit 0
}

Write-Host 'INSTALL - INSTALL GENERATION 1' -ForegroundColor Blue

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$dispatcher = @'
@echo off
setlocal
set "DIR=%CD%"
:find
if exist "%DIR%\scripts\%~n0.ps1" (set "SCRIPT=%DIR%\scripts\%~n0.ps1" & goto found)
if exist "%DIR%\%~n0.ps1" (set "SCRIPT=%DIR%\%~n0.ps1" & goto found)
for %%I in ("%DIR%\..") do set "PARENT=%%~fI"
if /I "%PARENT%"=="%DIR%" goto missing
set "DIR=%PARENT%"
goto find
:missing
echo No %~n0.ps1 was found in a scripts folder or at the root of this folder or any parent.
exit /b 1
:found
set "PS=powershell"
where pwsh >nul 2>nul && set "PS=pwsh"
%PS% -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
exit /b %ERRORLEVEL%
'@

# An alias dispatcher is the dispatcher with one more pair of probes, for its script, after the
# probes for its own name. The rem line marks it, so the upgrade below never flattens it.
function Get-AliasDispatcher {
    param([string]$Target)
    $probes = 'if exist "%DIR%\scripts\' + $Target + '.ps1" (set "SCRIPT=%DIR%\scripts\' + $Target + '.ps1" & goto found)' + "`n" +
        'if exist "%DIR%\' + $Target + '.ps1" (set "SCRIPT=%DIR%\' + $Target + '.ps1" & goto found)'
    $text = $dispatcher.Replace("@echo off`n", "@echo off`nrem alias of $Target`n")
    $text = $text.Replace("`nfor %%I in", "`n$probes`nfor %%I in")
    return $text -replace "`r?`n", "`r`n"
}

$Destination = [System.IO.Path]::GetFullPath($Destination)
New-Item -ItemType Directory -Path $Destination -Force | Out-Null

function Get-OrdinalKey {
    # Sort-Object compares text by culture, which Windows PowerShell 5.1 and pwsh 7 order differently.
    # Uppercase hexadecimal UTF-16 code units compare alike under every culture, so this key sorts ordinally.
    param([string]$Text)
    return [System.BitConverter]::ToString([System.Text.Encoding]::BigEndianUnicode.GetBytes($Text)).Replace('-', '')
}

$scripts = @(
    Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1' -File |
        Where-Object { $_.Name -notlike '*.config.ps1' } |
        Sort-Object -Property @{ Expression = { Get-OrdinalKey $_.Name } }
)

$content = $dispatcher -replace "`r?`n", "`r`n"

# One entry per dispatcher: each script under its own name, then under every alias its header declares.
$commands = New-Object 'System.Collections.Generic.List[object]'
$aliases = New-Object 'System.Collections.Generic.List[string]'
foreach ($script in $scripts) {
    $target = [System.IO.Path]::GetFileNameWithoutExtension($script.Name)
    [void]$commands.Add([pscustomobject]@{ Name = $target; Content = $content; Script = $script })
    foreach ($match in [regex]::Matches([System.IO.File]::ReadAllText($script.FullName), '(?m)^# ALIAS ([A-Za-z0-9]+)\r?$')) {
        $alias = $match.Groups[1].Value
        if (@($scripts | Where-Object { $_.BaseName -eq $alias }).Count -gt 0) {
            throw "$($script.Name) declares the alias $alias, which is already a script name."
        }

        [void]$commands.Add([pscustomobject]@{ Name = $alias; Content = (Get-AliasDispatcher -Target $target); Script = $script })
        [void]$aliases.Add("$alias=$target")
    }
}

$written = 0
$shadowed = New-Object 'System.Collections.Generic.List[string]'
foreach ($entry in $commands) {
    $name = $entry.Name
    $script = $entry.Script
    $path = Join-Path $Destination ($name + '.cmd')
    $current = if (Test-Path -LiteralPath $path -PathType Leaf) { [System.IO.File]::ReadAllText($path) } else { $null }
    if ($current -ne $entry.Content) {
        [System.IO.File]::WriteAllText($path, $entry.Content, [System.Text.ASCIIEncoding]::new())
        $written++
    }

    # An alias, function or cmdlet of the same name wins over PATH in PowerShell, and a program
    # earlier on the PATH wins in both shells, so the bare name would never reach the dispatcher.
    # Said aloud rather than discovered later.
    foreach ($shadow in @(Get-Command -Name $name -All -ErrorAction SilentlyContinue)) {
        if ($shadow.Source -eq $path -or $shadow.Source -eq $script.FullName) {
            continue
        }

        $origin = if ($shadow.CommandType -eq 'Application') { $shadow.Source } elseif ($shadow.ModuleName) { "module $($shadow.ModuleName)" } else { 'built in' }
        [void]$shadowed.Add("$name - $($shadow.CommandType.ToString().ToLowerInvariant()), $origin")
    }
}

# The folder serves every project on this machine, so a dispatcher this project has no script for
# is never removed: another project may own it. Dispatchers that older installs wrote in an earlier
# shape are brought up to the current text, since every dispatcher is the same file. An alias
# dispatcher is not, so it is left as its own project wrote it.
$upgraded = 0
foreach ($other in Get-ChildItem -LiteralPath $Destination -Filter '*.cmd' -File) {
    $text = [System.IO.File]::ReadAllText($other.FullName)
    if ($text -ne $content -and $text -match '(?m)^if exist "%DIR%\\scripts\\%~n0\.ps1"' -and $text -notmatch '(?m)^rem alias of ') {
        [System.IO.File]::WriteAllText($other.FullName, $content, [System.Text.ASCIIEncoding]::new())
        $upgraded++
    }
}

$userPath = [System.Environment]::GetEnvironmentVariable('Path', 'User')
$entries = @($userPath -split ';' | Where-Object { $_ -ne '' } | ForEach-Object { $_.TrimEnd('\') })
$onPath = $entries -contains $Destination.TrimEnd('\')
if (-not $onPath) {
    $joined = (($entries + $Destination) -join ';')
    [System.Environment]::SetEnvironmentVariable('Path', $joined, 'User')
}

$sessionEntries = @($env:Path -split ';' | ForEach-Object { $_.TrimEnd('\') })
if ($sessionEntries -notcontains $Destination.TrimEnd('\')) {
    $env:Path = $env:Path.TrimEnd(';') + ';' + $Destination
}

Write-Host 'Dispatchers installed' -ForegroundColor Green
Write-Host ("  {0,-11}{1}" -f 'Folder', $Destination)
Write-Host ("  {0,-11}{1}" -f 'Commands', (($scripts | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Name) }) -join ' '))
Write-Host ("  {0,-11}{1}" -f 'Aliases', $(if ($aliases.Count -gt 0) { $aliases -join ' ' } else { 'none' }))
Write-Host ("  {0,-11}{1} written, {2} unchanged, {3} other project's upgraded" -f 'Files', $written, ($commands.Count - $written), $upgraded)
Write-Host ("  {0,-11}{1}" -f 'User PATH', $(if ($onPath) { 'already held the folder' } else { 'folder appended; reopen other consoles to see it' }))
if ($shadowed.Count -gt 0) {
    Write-Host ("  {0,-11}{1}" -f 'Shadowed', 'these bare names reach something else first:') -ForegroundColor Yellow
    foreach ($item in $shadowed) {
        Write-Host ("  {0,-11}  {1}" -f '', $item) -ForegroundColor Yellow
    }
}
