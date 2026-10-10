<#
.SYNOPSIS
Read and keep the file renames between consecutive versions for the history audits.

.DESCRIPTION
Dot-sourced by AuditLinesHistory.ps1 and AuditObjectHistory.ps1, after their own
Invoke-Git and ConvertTo-JsonText. Both share one lineage folder.

For each version and the version before it, git diff with rename detection lists
the renamed files under the lineage roots. A rename is kept when the project folder
or the file stem changes; the stem is the file name up to its first dot. Each
version keeps its renames in {version}.json in the lineage folder, written once.
A record found under other rules or for another pair of commits is read again
from git. Without a local repository, no rename is read and the lineage stays as
recorded.
#>
# AUDITHISTORY.LINEAGE - AUDIT GENERATION 21.
#requires -Version 5.1

$script:LineageRoots = @('src', 'tests')
$script:LineageRules = 'similarity=50;roots=' + ($script:LineageRoots -join ',') + ';kept=project,stem;record=1'

# The project folder of a repository path: its root and first segment, or $null above that depth.
function Get-LineageProject {
    param([Parameter(Mandatory = $true)][string]$Relative)

    $parts = $Relative.Split('/')
    if ($parts.Count -lt 3) { return $null }
    return $parts[0] + '/' + $parts[1]
}

function Get-LineageStem {
    param([Parameter(Mandatory = $true)][string]$Relative)

    $name = $Relative.Substring($Relative.LastIndexOf('/') + 1)
    $cut = $name.IndexOf('.')
    if ($cut -le 0) { return $name }
    return $name.Substring(0, $cut)
}

# The renames from one commit to the next that change the project folder or the file stem, or $null when git fails.
function Get-LineageMove {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Before,
        [Parameter(Mandatory = $true)][string]$After
    )

    $result = Invoke-Git -Arguments (@('-C', $Root, '-c', 'core.quotePath=false', 'diff', '-M50%', '-l100000', '--name-status', '--diff-filter=R', $Before, $After, '--') + $script:LineageRoots)
    if ($result.ExitCode -ne 0) { return $null }
    $moves = [System.Collections.Generic.List[object]]::new()
    foreach ($line in $result.Lines) {
        $parts = ([string]$line).Split("`t")
        if ($parts.Count -lt 3) { continue }
        $old = $parts[1].Replace('\', '/')
        $new = $parts[2].Replace('\', '/')
        if ((Get-LineageProject -Relative $old) -ceq (Get-LineageProject -Relative $new) -and (Get-LineageStem -Relative $old) -ceq (Get-LineageStem -Relative $new)) { continue }
        $moves.Add([pscustomobject]@{ From = $old; To = $new })
    }
    $sorted = [System.Collections.Generic.List[object]]::new()
    foreach ($move in @($moves | Sort-Object -Property @{ Expression = { $_.From + "`t" + $_.To } } -CaseSensitive)) { $sorted.Add($move) }
    return , $sorted
}

function Save-LineageRecord {
    param(
        [Parameter(Mandatory = $true)][string]$Folder,
        [Parameter(Mandatory = $true)][object]$Record
    )

    $builder = [System.Text.StringBuilder]::new()
    [void]$builder.Append("{`n")
    [void]$builder.Append('  "version": ' + (ConvertTo-JsonText $Record.Version) + ",`n")
    [void]$builder.Append('  "commit": ' + (ConvertTo-JsonText $Record.Commit) + ",`n")
    [void]$builder.Append('  "previous": ' + (ConvertTo-JsonText $Record.Previous) + ",`n")
    [void]$builder.Append('  "previousCommit": ' + (ConvertTo-JsonText $Record.PreviousCommit) + ",`n")
    [void]$builder.Append('  "rules": ' + (ConvertTo-JsonText $script:LineageRules) + ",`n")
    [void]$builder.Append('  "moves": [')
    for ($i = 0; $i -lt $Record.Moves.Count; $i++) {
        $move = $Record.Moves[$i]
        [void]$builder.Append($(if ($i -eq 0) { "`n" } else { ",`n" }))
        [void]$builder.Append('    [' + (ConvertTo-JsonText $move.From) + ', ' + (ConvertTo-JsonText $move.To) + ']')
    }
    [void]$builder.Append($(if ($Record.Moves.Count -eq 0) { "]`n}`n" } else { "`n  ]`n}`n" }))
    $text = $builder.ToString()

    $encoding = [System.Text.UTF8Encoding]::new($false)
    $path = Join-Path $Folder ($Record.Version + '.json')
    if ([System.IO.File]::Exists($path) -and [System.IO.File]::ReadAllText($path, $encoding) -ceq $text) { return }
    [void][System.IO.Directory]::CreateDirectory($Folder)
    $pending = $path + '.pending'
    [System.IO.File]::WriteAllText($pending, $text, $encoding)
    if ([System.IO.File]::Exists($path)) {
        [System.IO.File]::Replace($pending, $path, [NullString]::Value)
    }
    else {
        [System.IO.File]::Move($pending, $path)
    }
}

function Read-LineageRecord {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not [System.IO.File]::Exists($Path)) { return $null }
    try {
        $item = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    }
    catch {
        return $null
    }
    if ([string]$item.rules -cne $script:LineageRules) { return $null }
    $moves = [System.Collections.Generic.List[object]]::new()
    foreach ($pair in @($item.moves)) {
        if ($null -eq $pair) { continue }
        $moves.Add([pscustomobject]@{ From = [string]$pair[0]; To = [string]$pair[1] })
    }
    return [pscustomobject]@{
        Version        = [string]$item.version
        Commit         = [string]$item.commit
        Previous       = [string]$item.previous
        PreviousCommit = [string]$item.previousCommit
        Moves          = $moves
    }
}

# The renames of every version against the version before it, read from the lineage folder or from git.
function Update-Lineage {
    param(
        [AllowNull()][string]$Root,
        [Parameter(Mandatory = $true)][string]$Folder,
        [Parameter(Mandatory = $true)][hashtable]$VersionMap
    )

    $ordered = @($VersionMap.Values | Sort-Object -Property @{ Expression = { [version]$_.Version } })
    $lineage = [System.Collections.Generic.List[object]]::new()
    $read = 0
    $missing = 0
    for ($i = 1; $i -lt $ordered.Count; $i++) {
        $before = $ordered[$i - 1]
        $after = $ordered[$i]
        $path = Join-Path $Folder ($after.Version + '.json')
        $record = Read-LineageRecord -Path $path
        $current = $null -ne $record -and $record.Commit -ceq $after.Sha -and $record.Previous -ceq $before.Version -and $record.PreviousCommit -ceq $before.Sha
        if (-not $current) {
            $moves = $null
            if (-not [string]::IsNullOrEmpty($Root)) { $moves = Get-LineageMove -Root $Root -Before $before.Sha -After $after.Sha }
            if ($null -eq $moves) {
                $missing++
                if ($null -ne $record) { $lineage.Add($record) }
                continue
            }
            $record = [pscustomobject]@{ Version = $after.Version; Commit = $after.Sha; Previous = $before.Version; PreviousCommit = $before.Sha; Moves = $moves }
            Save-LineageRecord -Folder $Folder -Record $record
            $read++
        }
        $lineage.Add($record)
    }
    Write-Host "Lineage: $($lineage.Count) version steps, $read read from git, $missing without git."
    return , $lineage
}
