<#
.SYNOPSIS
    Remove generated build output and stale WPF temporary projects safely.
.DESCRIPTION
    Deletes obj/, bin/, and TestResults/ directories outside the protected
    top-level .git/, build/, publish/, debug/, run/, and snapshots/ trees. The protected trees are
    never traversed, so immutable snapshots cannot be altered and cleanup time
    does not grow with the snapshot collection.

    Also removes stray '*_wpftmp.*' files outside those protected trees,
    optionally resets the VS Code C# Dev Kit workspace cache, and maintains
    .vscode/settings.json exclusions for generated files.

    The only project-specific value - the solution the editor opens - lives
    in snapshot.json. This script carries none, so the file is identical in
    every project at the same generation.
.PARAMETER TempOnly
    Remove only stray WPF temporary-project files. Build output is left intact.
.PARAMETER IdeCache
    Also reset the VS Code C# Dev Kit workspace cache.
.PARAMETER Help
    Display this help and exit without deleting anything. The alias -? is supported.
.EXAMPLE
    clean
    Remove generated output and stale temporary projects.
.EXAMPLE
    clean -WhatIf
    Preview every removal without changing the filesystem.
.EXAMPLE
    clean -TempOnly
    Remove only stray WPF temporary-project files.
.EXAMPLE
    clean -IdeCache
    Clean generated output and reset the IDE cache.
#>
#requires -Version 5.1
# CLEAN - SNAPSHOT GENERATION 1.
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
[CmdletBinding(SupportsShouldProcess)]
param(
    [switch]$TempOnly,
    [switch]$IdeCache,
    [Alias('?')]
    [switch]$Help
)

if ($Help) {
    @'
NAME
    Clean.ps1

SYNOPSIS
    Remove generated build output and stale WPF temporary projects safely.

SYNTAX
    clean [-TempOnly] [-IdeCache] [-WhatIf] [-Help]

OPTIONS
    -TempOnly
        Remove only stray WPF temporary-project files.

    -IdeCache
        Also reset the VS Code C# Dev Kit workspace cache.

    -WhatIf
        Preview removals without changing the filesystem.

    -Help, -?
        Display this help and exit without deleting anything.

EXAMPLES
    clean
        Remove generated output and stale temporary projects.

    clean -WhatIf
        Preview every removal.

    clean -TempOnly
        Remove only stray WPF temporary-project files.

    clean -IdeCache
        Clean generated output and reset the IDE cache.
'@ | Write-Host
    exit 0
}

Write-Host 'CLEAN - SNAPSHOT GENERATION 1' -ForegroundColor Blue

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$protectedTopLevelNames = @('.git', 'build', 'publish', 'debug', 'run', 'snapshots')
$generatedDirectoryNames = @('obj', 'bin', 'TestResults')

function Get-OrdinalKey {
    # Sort-Object compares text by culture, which Windows PowerShell 5.1 and pwsh 7 order differently.
    # Uppercase hexadecimal UTF-16 code units compare alike under every culture, so this key sorts ordinally.
    param([string]$Text)
    return [System.BitConverter]::ToString([System.Text.Encoding]::BigEndianUnicode.GetBytes($Text)).Replace('-', '')
}

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

$config = Read-SnapshotConfig

function Get-SafeTopLevelDirectory {
    param([string]$Root)

    return @(
        Get-ChildItem -LiteralPath $Root -Directory -Force -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -notin $protectedTopLevelNames }
    )
}

function Get-WpfTempFile {
    param([string]$Root)

    $files = @(
        Get-ChildItem -LiteralPath $Root -File -Force -Filter '*_wpftmp.*' -ErrorAction SilentlyContinue
    )

    foreach ($directory in (Get-SafeTopLevelDirectory -Root $Root)) {
        $files += @(
            Get-ChildItem -LiteralPath $directory.FullName -Recurse -File -Force -Filter '*_wpftmp.*' -ErrorAction SilentlyContinue
        )
    }

    return @($files | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_.FullName } } -Unique)
}

function Remove-WpfTempFile {
    param([string]$Root)

    $removed = 0
    foreach ($stray in (Get-WpfTempFile -Root $Root)) {
        if ($PSCmdlet.ShouldProcess($stray.FullName, 'Remove WPF temp-project file')) {
            Remove-Item -LiteralPath $stray.FullName -Force -ErrorAction SilentlyContinue
            $removed++
        }
    }

    return $removed
}

function Get-GeneratedDirectory {
    param([string]$Root)

    $targets = @()

    foreach ($directory in (Get-SafeTopLevelDirectory -Root $Root)) {
        if ($directory.Name -in $generatedDirectoryNames) {
            $targets += $directory
            continue
        }

        $targets += @(
            Get-ChildItem -LiteralPath $directory.FullName -Recurse -Directory -Force -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -in $generatedDirectoryNames }
        )
    }

    # Remove the deepest directories first in the unlikely event that generated
    # directories are nested inside one another.
    return @(
        $targets |
            Sort-Object -Property @{ Expression = { $_.FullName.Length }; Descending = $true }, @{ Expression = { Get-OrdinalKey $_.FullName } } -Unique
    )
}

function Convert-JsonToTable {
    param($Value)

    if ($Value -is [System.Management.Automation.PSCustomObject]) {
        $table = @{}
        foreach ($property in $Value.PSObject.Properties) {
            $table[$property.Name] = Convert-JsonToTable -Value $property.Value
        }

        return $table
    }

    if ($Value -is [System.Array]) {
        return , @(foreach ($item in $Value) { , (Convert-JsonToTable -Value $item) })
    }

    return $Value
}

function ConvertTo-SettingJson {
    # ConvertTo-Json lays out differently in Windows PowerShell 5.1 and pwsh 7, and a hashtable has
    # no stable order, so the settings are written by hand: two-space indent, keys in ordinal order.
    param(
        $Value,
        [string]$Indent = ''
    )

    $inner = $Indent + '  '
    if ($null -eq $Value) {
        return 'null'
    }

    if ($Value -is [bool]) {
        return $Value.ToString().ToLowerInvariant()
    }

    if ($Value -is [string]) {
        $builder = New-Object System.Text.StringBuilder
        [void]$builder.Append('"')
        foreach ($character in $Value.ToCharArray()) {
            switch ($character) {
                '"' { [void]$builder.Append('\"') }
                '\' { [void]$builder.Append('\\') }
                "`n" { [void]$builder.Append('\n') }
                "`r" { [void]$builder.Append('\r') }
                "`t" { [void]$builder.Append('\t') }
                default {
                    if ([int]$character -lt 0x20) { [void]$builder.AppendFormat('\u{0:x4}', [int]$character) }
                    else { [void]$builder.Append($character) }
                }
            }
        }

        [void]$builder.Append('"')
        return $builder.ToString()
    }

    if ($Value -is [System.Collections.IDictionary]) {
        $keys = [string[]]@($Value.Keys)
        if ($keys.Count -eq 0) {
            return '{}'
        }

        [System.Array]::Sort($keys, [System.StringComparer]::Ordinal)
        $members = foreach ($key in $keys) {
            $inner + (ConvertTo-SettingJson -Value $key) + ': ' + (ConvertTo-SettingJson -Value $Value[$key] -Indent $inner)
        }

        return "{`n" + ($members -join ",`n") + "`n" + $Indent + '}'
    }

    if ($Value -is [System.Collections.IEnumerable]) {
        $items = @(foreach ($item in $Value) { $inner + (ConvertTo-SettingJson -Value $item -Indent $inner) })
        if ($items.Count -eq 0) {
            return '[]'
        }

        return "[`n" + ($items -join ",`n") + "`n" + $Indent + ']'
    }

    if ($Value -is [System.IFormattable]) {
        return $Value.ToString($null, [System.Globalization.CultureInfo]::InvariantCulture)
    }

    return ConvertTo-SettingJson -Value ([string]$Value)
}

function Merge-SettingSection {
    param(
        [hashtable]$Settings,
        [string]$Section,
        [hashtable]$Entries
    )

    $existing = @{}
    if ($Settings.ContainsKey($Section) -and $Settings[$Section] -is [hashtable]) {
        $existing = $Settings[$Section]
    }

    foreach ($key in $Entries.Keys) {
        $existing[$key] = $Entries[$key]
    }

    $Settings[$Section] = $existing
}

function Set-EditorSetting {
    param([string]$Root)

    $folder = Join-Path $Root '.vscode'
    $file = Join-Path $folder 'settings.json'
    $settings = @{}

    if (Test-Path -LiteralPath $file -PathType Leaf) {
        $raw = Get-Content -LiteralPath $file -Raw -Encoding UTF8
        try {
            $settings = Convert-JsonToTable -Value ($raw | ConvertFrom-Json -ErrorAction Stop)
        }
        catch {
            Write-Warning "$file could not be parsed (comments or trailing commas?); left untouched."
            return
        }

        if ($settings -isnot [hashtable]) {
            $settings = @{}
        }
    }

    Merge-SettingSection -Settings $settings -Section 'files.watcherExclude' -Entries @{
        '**/*_wpftmp.*' = $true
        '**/obj/**'     = $true
        '**/bin/**'     = $true
    }

    Merge-SettingSection -Settings $settings -Section 'files.exclude' -Entries @{
        '**/*_wpftmp.*' = $true
    }

    Merge-SettingSection -Settings $settings -Section 'search.exclude' -Entries @{
        '**/obj'       = $true
        '**/bin'       = $true
        '**/publish'   = $true
        '**/debug'     = $true
        '**/snapshots' = $true
    }

    $settings['dotnet.defaultSolution'] = [string]$config.solution

    $json = ConvertTo-SettingJson -Value $settings
    if (-not $PSCmdlet.ShouldProcess($file, 'Write Dev Kit exclusion settings')) {
        return
    }

    if (-not (Test-Path -LiteralPath $folder -PathType Container)) {
        New-Item -ItemType Directory -Path $folder | Out-Null
    }

    [System.IO.File]::WriteAllText(
        $file,
        ($json -replace "`r`n", "`n") + "`n",
        (New-Object System.Text.UTF8Encoding($false)))

    Write-Host "Dev Kit settings maintained: $file" -ForegroundColor DarkGray
}

function Clear-IdeCache {
    param([string]$Root)

    $storage = Join-Path $env:APPDATA 'Code\User\workspaceStorage'
    if (-not (Test-Path -LiteralPath $storage -PathType Container)) {
        Write-Warning 'No VS Code workspace storage found; Dev Kit cache not cleared.'
        return
    }

    $rootUriPart = $Root.Replace('\', '/')
    $cleared = 0

    foreach ($workspace in (Get-ChildItem -LiteralPath $storage -Directory -Force -ErrorAction SilentlyContinue)) {
        $descriptor = Join-Path $workspace.FullName 'workspace.json'
        if (-not (Test-Path -LiteralPath $descriptor -PathType Leaf)) {
            continue
        }

        $descriptorText = Get-Content -LiteralPath $descriptor -Raw -Encoding UTF8
        if ($descriptorText -notlike "*$rootUriPart*") {
            continue
        }

        foreach ($extension in @('ms-dotnettools.csdevkit', 'ms-dotnettools.csharp')) {
            $cache = Join-Path $workspace.FullName $extension
            if (-not (Test-Path -LiteralPath $cache -PathType Container)) {
                continue
            }

            if ($PSCmdlet.ShouldProcess($cache, 'Remove Dev Kit workspace cache')) {
                Remove-Item -LiteralPath $cache -Recurse -Force -ErrorAction SilentlyContinue
                $cleared++
            }
        }
    }

    if ($cleared -eq 0) {
        Write-Warning 'No Dev Kit workspace cache matched this repository.'
    }
    else {
        Write-Host "Dev Kit workspace cache cleared ($cleared folder(s)). Reload the VS Code window." -ForegroundColor DarkGray
    }
}

$strayCount = Remove-WpfTempFile -Root $root

if (-not $TempOnly) {
    $targets = @(Get-GeneratedDirectory -Root $root)

    $vs = Join-Path $root '.vs'
    if (Test-Path -LiteralPath $vs -PathType Container) {
        $targets += Get-Item -LiteralPath $vs -Force
    }

    foreach ($directory in $targets) {
        if ($PSCmdlet.ShouldProcess($directory.FullName, 'Remove directory')) {
            Remove-Item -LiteralPath $directory.FullName -Recurse -Force
        }
    }

    Set-EditorSetting -Root $root
    if ($IdeCache) {
        Clear-IdeCache -Root $root
    }
}

if ($strayCount -gt 0) {
    Write-Host "Removed $strayCount WPF temp-project file(s)." -ForegroundColor DarkGray
}
