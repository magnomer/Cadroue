<#
.SYNOPSIS
Read and validate AuditNames.json for the naming audit tooling.

.DESCRIPTION
Dot-sourced by AuditNames.ps1, AuditNamesNew.ps1 and SyncNames.ps1. Holds the
generation number, the configuration schema, and the strict reader they use. No project-specific
value appears here: this file is identical in every project at this generation.
#>
#requires -Version 5.1
# AUDITNAMES.CONFIG - AUDIT GENERATION 21.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the audited outcome changes.
# A configuration is total: every key in the schema must be present, a missing key is an error rather
# than a default, and an unknown key is an error rather than a silent no-op. A typo that quietly does
# nothing is the worst failure mode a configuration like this has, so it is made loud.
# AuditNames.json holds the naming settings the scripts apply, and the report block names the report
# folder, the version file and key, and the report prefix AuditNames.ps1 writes under. Anything else
# only one tool reads is named by that tool: docs-internal is named by SyncNames.ps1 alone.
# The name registry chain is docs-internal -> SyncNames.ps1 -> AuditNames.registry.json, which both
# audits read, and TAuditNameRegistry.cs with TAuditVerbRegistry.cs, which the convention tests compile. The registry file is
# shared by all three scripts, so its name and its strict reader live here.
# The convention tests never read this file: the hand-written TAuditNameSetting.cs mirrors every key
# but naming.componentReview, which only sets the script's review count and gates nothing, and the
# report block, which only places the script's reports.
# Generation 8 hands the line limit to AuditLines.ps1, which generates its own sidecar, and the
# name audit now enumerates sources through a scope it builds from its own sidecar alone.
# Generation 11: nothing the name audit reports changes; the number rises with the truth audit,
# which checks that a deportment field reaches no request, keeps one writer, holds no logic and
# treats no engine data.
# Generation 12: nothing the name audit reports changes; the number rises with the convention tests,
# which bind with no compile error, count chain ceilings in names, count a using or a call on a
# deeper record as a reach, and exempt a contract name only where the type declares the interface.
# Generation 13: nothing this audit reports changes; the number rises with the UI audit, which
# counts every surface markup line that hooks logic into the markup and every surface member
# that is not a constructor.
# Generation 14: nothing this audit reports changes; the number rises with the UI audit, which
# also counts command parameters, member paths and literal tags in surface markup as hooks.
# Generation 15: the name audit also counts every type whose prefix lies outside the turf of its
# project, against a ceiling. The UI audit rises with it.
# Generation 16: nothing this audit reports changes; the number rises with the structure audit, whose
# Unsealing kind counts engine types on the public members of sealed Deportment types.

# Generation 17: findings take one vocabulary of -ing kinds and plain measure names, and the
# configuration keys follow. What the audit counts is unchanged.
# Generation 18: nothing this audit reports changes; the number rises with the UI audit, whose
# truth detector stops five false findings.
# Generation 19: nothing this audit reports changes; the number rises with the comment audit, whose
# hash line ties every comment file to the sources it describes.
# Generation 20: nothing this audit reports changes; the number rises with the UI audit, whose
# Mismatching kind only informs while a driver folder it waits on holds no source.
# Generation 21: an out-of-turf type declared public in a sealed turf fails at once, outside the
# prefix ceiling.
$script:AuditGeneration = 21

# The generated name registry beside this file. SyncNames.ps1 writes it from docs-internal; the audits
# read it and never docs-internal.
$script:AuditRegistryFile = 'AuditNames.registry.json'
$script:AuditRegistrySchema = [ordered]@{
    'generation' = 'int'
    'bases'      = 'string[]'
    'verbs'      = 'string[]'
    'exempt'     = 'map[]'
}

# Each entry is a path through the document and the kind of value that must be found there.
# 'string', 'int', 'string[]', or 'map[]' for an object whose every property is an array of strings.
$script:AuditSchema = [ordered]@{
    'generation'                       = 'int'
    'project'                          = 'string'
    'sources.include'                  = 'string[]'
    'sources.excludeSegments'          = 'string[]'
    'sources.excludeSuffixes'          = 'string[]'
    'sources.excludePrefixes'          = 'string[]'
    'sources.selfExclude'              = 'string[]'
    'naming.prefixes'                  = 'string[]'
    'naming.prefixTurfs'               = 'map[]'
    'naming.prefixCeiling'             = 'int'
    'naming.sealedTurfs'               = 'string[]'
    'naming.testPrefix'                = 'string'
    'naming.componentLimit'            = 'int'
    'naming.componentReview'           = 'int'
    'naming.componentPattern'          = 'string'
    'kinds.method'                     = 'string[]'
    'kinds.verbForbidden'              = 'string[]'
    'platform.xamlNamespace'           = 'string'
    'platform.testAttributes'          = 'string[]'
    'platform.generatedAttributes'     = 'string[]'
    'platform.externalAttributes'      = 'string[]'
    'platform.commandAttributes'       = 'string[]'
    'platform.commandCancelArgument'   = 'string'
    'platform.commandAsyncSuffix'      = 'string'
    'platform.commandSuffix'           = 'string'
    'platform.commandCancelSuffix'     = 'string'
    'platform.frameworkContracts'      = 'map[]'
    'report.directory'                 = 'string'
    'report.versionFile'               = 'string'
    'report.versionKey'                = 'string'
    'report.prefix'                    = 'string'
}

function Resolve-AuditRoot {
    param([Parameter(Mandatory = $true)][string]$Path)

    $item = Get-Item -LiteralPath $Path -ErrorAction SilentlyContinue
    if ($null -eq $item) {
        throw "The project root was not found: $Path"
    }

    if (-not $item.PSIsContainer) {
        throw "The project root is not a directory: $Path"
    }

    return $item.FullName.TrimEnd([char[]]@('\', '/'))
}

function Join-AuditPath {
    # A configured path is written with forward slashes and is always relative to the project root.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)][string]$Relative
    )

    $native = $Relative.Replace('/', [System.IO.Path]::DirectorySeparatorChar).Replace('\', [System.IO.Path]::DirectorySeparatorChar)
    return Join-Path $ProjectRoot $native
}

function Get-AuditNode {
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

function Get-AuditKeyPaths {
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
            -not $script:AuditSchema.Contains($path)

        if ($isBranch) {
            foreach ($child in (Get-AuditKeyPaths -Node $property.Value -Prefix $path)) {
                [void]$paths.Add($child)
            }
        }
        else {
            [void]$paths.Add($path)
        }
    }

    return , $paths.ToArray()
}

function Test-AuditValue {
    param(
        [Parameter(Mandatory = $true)][string]$Kind,
        $Value
    )

    switch ($Kind) {
        'string' { return $Value -is [string] -and -not [string]::IsNullOrWhiteSpace($Value) }
        'int' { return $Value -is [int] -or $Value -is [long] }
        'string[]' {
            if ($null -eq $Value -or -not ($Value -is [System.Array])) { return $false }
            foreach ($item in $Value) {
                if (-not ($item -is [string])) { return $false }
            }

            return $true
        }
        'map[]' {
            if ($null -eq $Value -or -not ($Value -is [System.Management.Automation.PSCustomObject])) { return $false }
            foreach ($property in $Value.PSObject.Properties) {
                if ($null -eq $property.Value -or -not ($property.Value -is [System.Array])) { return $false }
                foreach ($item in $property.Value) {
                    if (-not ($item -is [string])) { return $false }
                }
            }

            return $true
        }
        default { throw "Unknown schema kind: $Kind" }
    }
}

function Read-AuditConfig {
    param([Parameter(Mandatory = $true)][string]$ProjectRoot)

    $configPath = Join-Path $PSScriptRoot 'AuditNames.json'
    if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
        throw "The audit configuration was not found: $configPath"
    }

    try {
        $config = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The audit configuration is not valid JSON: $configPath`n$($_.Exception.Message)"
    }

    $problems = New-Object 'System.Collections.Generic.List[string]'

    foreach ($key in $script:AuditSchema.Keys) {
        $node = Get-AuditNode -Document $config -Key $key
        if (-not $node.Found) {
            [void]$problems.Add("missing key: $key")
            continue
        }

        if (-not (Test-AuditValue -Kind $script:AuditSchema[$key] -Value $node.Value)) {
            [void]$problems.Add("key '$key' must be $($script:AuditSchema[$key])")
        }
    }

    foreach ($path in (Get-AuditKeyPaths -Node $config)) {
        if (-not $script:AuditSchema.Contains($path)) {
            [void]$problems.Add("unknown key: $path")
        }
    }

    if ($problems.Count -gt 0) {
        throw "The audit configuration is not valid: $configPath`n  " + ($problems -join "`n  ")
    }

    if ($config.generation -ne $script:AuditGeneration) {
        throw "The audit configuration is generation $($config.generation) but this tooling is generation $script:AuditGeneration.`nA generation names the set of checks applied, so the two must match: $configPath"
    }

    return $config
}

function Get-AuditRegistryPath {
    return Join-Path $PSScriptRoot $script:AuditRegistryFile
}

function Read-AuditRegistry {
    # The registry is generated, so any fault in it is cured the same way: run SyncNames.ps1 again.
    # Returns the bases and verbs as ordinal sets and the exemptions as name -> granted file names.
    $registryPath = Get-AuditRegistryPath
    $cure = 'Run SyncNames.ps1 to generate it.'
    if (-not (Test-Path -LiteralPath $registryPath -PathType Leaf)) {
        throw "The name registry was not found: $registryPath`n$cure"
    }

    try {
        $document = Get-Content -LiteralPath $registryPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The name registry is not valid JSON: $registryPath`n$($_.Exception.Message)`n$cure"
    }

    $problems = New-Object 'System.Collections.Generic.List[string]'
    if ($null -eq $document -or -not ($document -is [System.Management.Automation.PSCustomObject])) {
        [void]$problems.Add('the document is not an object')
    }
    else {
        foreach ($key in $script:AuditRegistrySchema.Keys) {
            if (-not ($document.PSObject.Properties.Name -contains $key)) {
                [void]$problems.Add("missing key: $key")
            }
            elseif (-not (Test-AuditValue -Kind $script:AuditRegistrySchema[$key] -Value $document.$key)) {
                [void]$problems.Add("key '$key' must be $($script:AuditRegistrySchema[$key])")
            }
        }

        foreach ($name in $document.PSObject.Properties.Name) {
            if (-not $script:AuditRegistrySchema.Contains($name)) {
                [void]$problems.Add("unknown key: $name")
            }
        }
    }

    if ($problems.Count -eq 0) {
        if (@($document.bases).Count -eq 0) { [void]$problems.Add('no base is registered') }
        if (@($document.verbs).Count -eq 0) { [void]$problems.Add('no verb is registered') }
        if ($document.generation -ne $script:AuditGeneration) {
            [void]$problems.Add("generation $($document.generation) does not match the tooling generation $script:AuditGeneration")
        }
    }

    if ($problems.Count -gt 0) {
        throw "The name registry is not valid: $registryPath`n  " + ($problems -join "`n  ") + "`n$cure"
    }

    $bases = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
    foreach ($word in $document.bases) { [void]$bases.Add($word) }

    $verbs = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
    foreach ($word in $document.verbs) { [void]$verbs.Add($word) }

    $exempt = New-Object 'System.Collections.Generic.Dictionary[string,string[]]' ([System.StringComparer]::Ordinal)
    foreach ($property in $document.exempt.PSObject.Properties) {
        $exempt[$property.Name] = [string[]]@($property.Value)
    }

    return [pscustomobject]@{ Path = $registryPath; Bases = $bases; Verbs = $verbs; Exempt = $exempt }
}
