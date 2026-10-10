<#
.SYNOPSIS
Compute the code fingerprint that tells whether the code changed since the last audit run.

.DESCRIPTION
Dot-sourced by Audit.ps1, which stores the fingerprint in its summary page, and by
AuditShow.ps1, which compares the stored one with the current one.

The fingerprint hashes the HEAD commit and, for every file that differs from HEAD or
is untracked and not ignored, its path and content. The report folder is left out,
so writing the reports never changes it. Staging a file does not change it either.
Without git or a repository, the fingerprint is empty and matches nothing.
#>
# AUDIT.FINGERPRINT - AUDIT GENERATION 21.
#requires -Version 5.1

function Get-AuditFingerprint {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Excluded
    )

    # A native stderr line must never end the run, so git runs under Continue.
    $ErrorActionPreference = 'Continue'
    $exclude = ':(exclude)' + $Excluded.Trim().TrimEnd('\', '/')
    $head = (& git -C $Root rev-parse HEAD 2>$null | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $head.Length -eq 0) { return '' }

    $changed = @(& git -C $Root -c core.quotePath=false diff HEAD --name-only --no-renames -- . $exclude 2>$null)
    $untracked = @(& git -C $Root -c core.quotePath=false ls-files --others --exclude-standard -- . $exclude 2>$null)
    $paths = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($path in ($changed + $untracked)) {
        if (-not [string]::IsNullOrWhiteSpace($path)) { [void]$paths.Add(([string]$path).Trim('"')) }
    }

    $builder = [System.Text.StringBuilder]::new()
    [void]$builder.Append($head).Append("`n")
    $present = @($paths | Where-Object { [System.IO.File]::Exists((Join-Path $Root $_)) })
    $hashes = @{}
    if ($present.Count -gt 0) {
        Push-Location -LiteralPath $Root
        try { $objects = @($present | & git hash-object --stdin-paths 2>$null) }
        finally { Pop-Location }
        for ($index = 0; $index -lt $present.Count -and $index -lt $objects.Count; $index++) {
            $hashes[$present[$index]] = [string]$objects[$index]
        }
    }
    foreach ($path in $paths) {
        $object = if ($hashes.ContainsKey($path)) { $hashes[$path] } else { 'deleted' }
        [void]$builder.Append($path).Append(' ').Append($object).Append("`n")
    }

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $bytes = $sha.ComputeHash([System.Text.UTF8Encoding]::new($false).GetBytes($builder.ToString())) }
    finally { $sha.Dispose() }
    return (($bytes | ForEach-Object { $_.ToString('x2') }) -join '')
}
