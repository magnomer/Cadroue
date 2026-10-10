#requires -Version 5.1
# AUDITNAMESNEW - AUDIT GENERATION 21.
[CmdletBinding()]
param(
    [string]$File,
    [string]$Root,
    [Alias('?')]
    [switch]$Help,
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [string[]]$Token
)

if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Split-Path -Parent $PSScriptRoot
}

$script:ManualPath = Join-Path $PSScriptRoot 'AuditNamesNew.comment.md'

function Write-NewNameManual {
    if (Test-Path -LiteralPath $script:ManualPath -PathType Leaf) {
        Get-Content -LiteralPath $script:ManualPath -Encoding UTF8 | Write-Host
    }
    else {
        Write-Host "AuditNamesNew [-File <names.txt>] [base:<Word>] [verb:<Word>] [method:<Name>] [data:<Name>] [test:<Name>] [<Name>] ..."
    }
}

if ($Help) {
    Write-NewNameManual
    exit 0
}

Write-Host 'AUDITNAMESNEW - AUDIT GENERATION 21' -ForegroundColor Blue

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'AuditNames.config.ps1')

$projectRoot = Resolve-AuditRoot -Path $Root
$config = Read-AuditConfig -ProjectRoot $projectRoot

$prefixes = @($config.naming.prefixes)
$componentLimit = [int]$config.naming.componentLimit
$componentReview = [int]$config.naming.componentReview
$componentPattern = [regex]::new($config.naming.componentPattern)

# The registered names come from AuditNames.registry.json, never from docs-internal. SyncNames.ps1 alone
# reads docs-internal and generates the registry, so a missing or malformed one fails with a request to run it.
$nameRegistry = Read-AuditRegistry
$bases = $nameRegistry.Bases
$verbs = $nameRegistry.Verbs
$exempt = $nameRegistry.Exempt
$testPrefix = [string]$config.naming.testPrefix

$entries = New-Object 'System.Collections.Generic.List[object]'
$problems = New-Object 'System.Collections.Generic.List[string]'

function Add-NewNameEntry {
    param([string]$Text, [string]$Origin)

    $kind = 'auto'
    $value = $Text
    $colon = $Text.IndexOf(':')
    if ($colon -ge 0) {
        $kind = $Text.Substring(0, $colon).Trim().ToLowerInvariant()
        $value = $Text.Substring($colon + 1).Trim().Trim('"', "'")
    }

    if ($kind -notin @('auto', 'base', 'verb', 'method', 'data', 'test')) {
        [void]$problems.Add("${Origin}: unknown tag '${kind}:' in '$Text' (use base:, verb:, method:, data:, test:)")
        return
    }

    if ($value.Length -eq 0) {
        [void]$problems.Add("${Origin}: '$Text' carries no word after the tag")
        return
    }

    [void]$entries.Add([pscustomobject]@{ Kind = $kind; Value = $value; Origin = $Origin })
}

if (-not [string]::IsNullOrWhiteSpace($File)) {
    $listPath = if ([System.IO.Path]::IsPathRooted($File)) { $File } else { Join-Path (Get-Location).Path $File }
    if (-not (Test-Path -LiteralPath $listPath -PathType Leaf)) {
        throw "The name list was not found: $listPath"
    }

    $lineNumber = 0
    foreach ($raw in (Get-Content -LiteralPath $listPath -Encoding UTF8)) {
        $lineNumber++
        $hash = $raw.IndexOf('#')
        $line = if ($hash -ge 0) { $raw.Substring(0, $hash) } else { $raw }
        foreach ($piece in ($line -split '[\s,]+')) {
            if ($piece.Length -gt 0) {
                Add-NewNameEntry -Text $piece -Origin "$(Split-Path -Leaf $listPath):$lineNumber"
            }
        }
    }
}

foreach ($piece in @($Token)) {
    if (-not [string]::IsNullOrWhiteSpace($piece)) {
        Add-NewNameEntry -Text $piece.Trim() -Origin 'argument'
    }
}

foreach ($problem in $problems) {
    Write-Host "ERROR $problem"
}

if ($entries.Count -eq 0) {
    Write-Host 'No name was given.' -ForegroundColor Yellow
    Write-Host ''
    Write-NewNameManual
    exit 1
}

$newBases = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$newVerbs = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($entry in $entries) {
    if ($entry.Kind -eq 'base' -and -not $verbs.Contains($entry.Value)) { [void]$newBases.Add($entry.Value) }
    if ($entry.Kind -eq 'verb' -and -not $bases.Contains($entry.Value)) { [void]$newVerbs.Add($entry.Value) }
}

$allBases = [System.Collections.Generic.HashSet[string]]::new($bases, [StringComparer]::Ordinal)
$allBases.UnionWith($newBases)
$allVerbs = [System.Collections.Generic.HashSet[string]]::new($verbs, [StringComparer]::Ordinal)
$allVerbs.UnionWith($newVerbs)

function Test-NewNameWord {
    param([string]$Word, [string]$Role)

    $faults = New-Object 'System.Collections.Generic.List[string]'
    $notes = New-Object 'System.Collections.Generic.List[string]'
    $registry = if ($Role -eq 'base') { $bases } else { $verbs }
    $rival = if ($Role -eq 'base') { $allVerbs } else { $allBases }
    $rivalRole = if ($Role -eq 'base') { 'verb' } else { 'base' }

    if ($Word -cnotmatch '^[A-Za-z]{2,}$') {
        [void]$faults.Add('a registry word is two or more letters and nothing else')
    }
    elseif ($Word -cnotmatch '^[A-Z]') {
        [void]$faults.Add('a registry word starts with a capital letter')
    }
    else {
        $parts = @($componentPattern.Matches($Word) | ForEach-Object { $_.Value })
        if ($parts.Count -ne 1) {
            [void]$faults.Add("splits into $($parts.Count) components ($($parts -join ' ')); a $Role is one word")
        }
    }

    if ($rival.Contains($Word)) {
        [void]$faults.Add("'$Word' is also a $rivalRole; a word is a base or a verb, never both")
    }

    if ($registry.Contains($Word)) {
        [void]$notes.Add('already registered')
    }
    else {
        [void]$notes.Add("add to $(if ($Role -eq 'base') { 'ListObject.md table and AUDIT:BASES block' } else { 'ListVerb.md as a ### heading' })")
    }

    return [pscustomobject]@{ Faults = $faults; Notes = $notes; Shape = $Role }
}

function Read-NewNamePrefix {
    param([string]$Name)

    foreach ($prefix in $prefixes) {
        if (-not $Name.StartsWith($prefix, [StringComparison]::Ordinal) -or $Name.Length -eq $prefix.Length) {
            continue
        }

        $next = $Name[$prefix.Length]
        if ([char]::IsUpper($next) -or [char]::IsDigit($next) -or $next -eq '_') {
            return $prefix
        }
    }

    return $null
}

function Test-NewName {
    param([string]$Name, [string]$Kind)

    $faults = New-Object 'System.Collections.Generic.List[string]'
    $notes = New-Object 'System.Collections.Generic.List[string]'
    $shape = $Kind

    if ($Name -cnotmatch '^_*[A-Za-z][A-Za-z0-9_]*$') {
        [void]$faults.Add('not a C# identifier of letters, digits, and underscores')
        return [pscustomobject]@{ Faults = $faults; Notes = $notes; Shape = $shape }
    }

    $working = $Name.TrimStart('_')
    $prefix = Read-NewNamePrefix -Name $working
    if ($shape -eq 'test') {
        if ($null -eq $prefix) {
            [void]$notes.Add("descriptive: passes while no test method carries the $testPrefix prefix")
        }
        elseif ($prefix -cne $testPrefix -and $prefix -cne $testPrefix.ToLowerInvariant()) {
            [void]$faults.Add("test method must use the $testPrefix prefix, not ``$prefix``")
        }
        else {
            [void]$notes.Add("once one test method carries $testPrefix, every test method must")
        }

        return [pscustomobject]@{ Faults = $faults; Notes = $notes; Shape = $shape }
    }

    if ($null -eq $prefix) {
        [void]$faults.Add("missing required prefix (one of $($prefixes -join ', '), after any leading _)")
        return [pscustomobject]@{ Faults = $faults; Notes = $notes; Shape = $shape }
    }

    $remainder = $working.Substring($prefix.Length)
    $components = New-Object 'System.Collections.Generic.List[string]'
    foreach ($segment in $remainder.Split('_')) {
        if ($segment.Length -eq 0) {
            [void]$faults.Add('an underscore leaves an empty component')
            break
        }

        $parts = @($componentPattern.Matches($segment) | ForEach-Object { $_.Value })
        if (($parts -join '') -cne $segment) {
            [void]$faults.Add("'$segment' does not split into PascalCase components")
            break
        }

        $components.AddRange([string[]]$parts)
    }

    if ($faults.Count -gt 0) {
        return [pscustomobject]@{ Faults = $faults; Notes = $notes; Shape = $shape }
    }

    if ($components.Count -eq 0) {
        [void]$faults.Add('no base after the prefix')
        return [pscustomobject]@{ Faults = $faults; Notes = $notes; Shape = $shape }
    }

    $base = $components[0]
    $last = $components[$components.Count - 1]
    $lastIsVerb = $allVerbs.Contains($last)

    if ($shape -eq 'auto') {
        $shape = if ($components.Count -gt 1 -and $lastIsVerb) { 'method' } else { 'data' }
    }

    if (-not $allBases.Contains($base)) {
        if ($allVerbs.Contains($base)) {
            [void]$faults.Add("'$base' is a verb; a name opens with a base after the prefix")
        }
        else {
            [void]$faults.Add("unregistered base ``$base`` (register it, or pass base:$base to preview)")
        }
    }
    elseif ($newBases.Contains($base)) {
        [void]$notes.Add("base '$base' is new")
    }

    if ($components.Count -gt $componentLimit) {
        [void]$faults.Add("$($components.Count) components after the prefix (limit is $componentLimit)")
    }
    elseif ($components.Count -ge $componentReview) {
        [void]$notes.Add("$($components.Count) components: at the review line")
    }

    if ($shape -eq 'method' -and -not $lastIsVerb) {
        [void]$faults.Add("method does not end in a registered verb (``$last``)")
    }
    elseif ($shape -eq 'data' -and $lastIsVerb) {
        [void]$faults.Add("data or type name ends in a registered verb (``$last``)")
    }
    elseif ($shape -eq 'method' -and $newVerbs.Contains($last)) {
        [void]$notes.Add("verb '$last' is new")
    }

    [void]$notes.Insert(0, "$prefix | $($components -join ' ')")
    return [pscustomobject]@{ Faults = $faults; Notes = $notes; Shape = $shape }
}

$width = ($entries | ForEach-Object { $_.Value.Length } | Measure-Object -Maximum).Maximum
$passCount = 0
$failCount = 0
foreach ($entry in $entries) {
    $result = if ($entry.Kind -in @('base', 'verb')) {
        Test-NewNameWord -Word $entry.Value -Role $entry.Kind
    }
    elseif ($exempt.ContainsKey($entry.Value)) {
        $exemptNotes = New-Object 'System.Collections.Generic.List[string]'
        [void]$exemptNotes.Add("exempt in $($exempt[$entry.Value] -join ', ') (AUDIT:EXEMPT block)")
        [pscustomobject]@{ Faults = (New-Object 'System.Collections.Generic.List[string]'); Notes = $exemptNotes; Shape = 'exempt' }
    }
    else {
        Test-NewName -Name $entry.Value -Kind $entry.Kind
    }

    $ok = $result.Faults.Count -eq 0
    if ($ok) { $passCount++ } else { $failCount++ }

    $mark = if ($ok) { 'PASS' } else { 'FAIL' }
    $color = if ($ok) { 'Green' } else { 'Red' }
    $label = $entry.Value.PadRight($width)
    $shape = ('[' + $result.Shape + ']').PadRight(8)
    Write-Host -NoNewline "$mark " -ForegroundColor $color
    Write-Host "$label $shape $($result.Notes -join '; ')"
    foreach ($fault in $result.Faults) {
        Write-Host "     - $fault"
    }
}

Write-Host ''
Write-Host "Checked: $($entries.Count)  Pass: $passCount  Fail: $failCount"

if ($failCount -gt 0 -or $problems.Count -gt 0) {
    exit 1
}

exit 0
