#requires -Version 5.1
# CHECKPLACEMENT - CHECK GENERATION 1.
[CmdletBinding(PositionalBinding = $false)]
param(
    [switch]$Schema,
    [switch]$Next,
    [switch]$Plan,
    [string]$File = '',
    [Alias('?')]
    [switch]$Help,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Tokens = @()
)

$modeCount = @($Schema, $Next, $Plan | Where-Object { $_ }).Count
if ($Help -or $modeCount -eq 0) {
    @'
NAME
    CheckPlacement.ps1

SYNOPSIS
    Suggest, before any code is written, which source projects under src a GUI
    work touches, what each carries, and what may pass between them.
    The result is advisory. The structure document decides.

SYNTAX
    checkplacement -Schema
    checkplacement -Next <token> ...
    checkplacement -Next -File <work.txt>
    checkplacement -Plan <token> ...
    checkplacement -Plan -File <work.txt>

OPTIONS
    -Schema
        Print the whole gate tree, with each role resolved to this project's
        source project.

    -Next
        Print, for each part, the gates still open with their allowed values,
        and every input error so far.

    -Plan
        Print the suggested plan. A work with open gates or input errors is
        answered as -Next would answer it.

    -File <work.txt>
        Read one part per line. Blank lines and lines starting with # are
        skipped. The part on line n is part pn.
        Without -File, the remaining arguments are the tokens of one part.

    -Help
        Display this help and exit.

TOKENS
    A token is gate=value. A gate taking several values joins them with
    commas: held-state=conduct,transient. Every token is lowercase ASCII
    letters, digits and hyphens. The first gate of a part is kind, which is
    action or presentation. Every gate an answer opens must be answered.

OUTPUT
    JSON on standard output. The header line goes to the error stream.
    A plan names source projects only, never a folder inside one.
    Cautions point at answers worth a second look; they never block a plan.
    Exit code 0: a plan, or open gates with no input error.
    Exit code 1: one or more input errors.
    Exit code 2: the text file or the sidecar is missing or unreadable.

SETTINGS
    CheckPlacement.json beside this script maps every role to the project's
    source project. The script itself names no project.

EXAMPLES
    checkplacement -Schema
    checkplacement -Next kind=action entry=new-element
    checkplacement -Plan -File work.txt
'@ | Write-Output
    exit 0
}

[Console]::Error.WriteLine('CHECKPLACEMENT - CHECK GENERATION 1')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Stop-Fault {
    param([string]$Text)
    [Console]::Error.WriteLine($Text)
    exit 2
}

function Get-Prop {
    # StrictMode throws on a missing property, so every optional sidecar field is read through here.
    param($Object, [string]$Name, $Default = $null)
    if ($null -eq $Object) { return $Default }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { return $Default }
    return $property.Value
}

function Get-OrdinalSorted {
    param([string[]]$Items)
    $list = [System.Collections.Generic.List[string]]::new()
    foreach ($item in $Items) { if (-not $list.Contains($item)) { $list.Add($item) } }
    $list.Sort([System.StringComparer]::Ordinal)
    return , $list.ToArray()
}

# ---------------------------------------------------------------- JSON writer
# ConvertTo-Json escapes and indents differently under 5.1 and 7, so the output is written by hand.

function Format-JsonString {
    param([string]$Text)
    $builder = [System.Text.StringBuilder]::new()
    [void]$builder.Append('"')
    foreach ($char in $Text.ToCharArray()) {
        $code = [int]$char
        if ($char -eq '"') { [void]$builder.Append('\"') }
        elseif ($char -eq '\') { [void]$builder.Append('\\') }
        elseif ($code -eq 10) { [void]$builder.Append('\n') }
        elseif ($code -eq 13) { [void]$builder.Append('\r') }
        elseif ($code -eq 9) { [void]$builder.Append('\t') }
        elseif ($code -lt 32 -or $code -gt 126) { [void]$builder.Append(('\u{0:x4}' -f $code)) }
        else { [void]$builder.Append($char) }
    }
    [void]$builder.Append('"')
    return $builder.ToString()
}

function ConvertTo-JsonText {
    param($Value, [int]$Depth = 0)
    $pad = '  ' * $Depth
    $inner = '  ' * ($Depth + 1)
    if ($null -eq $Value) { return 'null' }
    if ($Value -is [bool]) { if ($Value) { return 'true' } else { return 'false' } }
    if ($Value -is [int] -or $Value -is [long]) { return $Value.ToString([System.Globalization.CultureInfo]::InvariantCulture) }
    if ($Value -is [string]) { return Format-JsonString $Value }
    if ($Value -is [System.Collections.IDictionary]) {
        if ($Value.Count -eq 0) { return '{}' }
        $lines = [System.Collections.Generic.List[string]]::new()
        foreach ($key in $Value.Keys) {
            $lines.Add($inner + (Format-JsonString ([string]$key)) + ': ' + (ConvertTo-JsonText $Value[$key] ($Depth + 1)))
        }
        return "{`n" + ($lines -join ",`n") + "`n$pad}"
    }
    if ($Value -is [System.Collections.IEnumerable]) {
        $lines = [System.Collections.Generic.List[string]]::new()
        foreach ($item in $Value) { $lines.Add($inner + (ConvertTo-JsonText $item ($Depth + 1))) }
        if ($lines.Count -eq 0) { return '[]' }
        return "[`n" + ($lines -join ",`n") + "`n$pad]"
    }
    return Format-JsonString ([string]$Value)
}

function Write-Json {
    param($Value)
    [Console]::Out.Write((ConvertTo-JsonText $Value) + "`n")
}

# ---------------------------------------------------------------- Sidecar

$sidecarPath = Join-Path $PSScriptRoot 'CheckPlacement.json'
if (-not (Test-Path -LiteralPath $sidecarPath -PathType Leaf)) { Stop-Fault "The sidecar $sidecarPath is missing." }
try {
    $side = [System.IO.File]::ReadAllText($sidecarPath, [System.Text.UTF8Encoding]::new($false)) | ConvertFrom-Json
} catch {
    Stop-Fault "The sidecar $sidecarPath is not JSON: $($_.Exception.Message)"
}

$roleOrder = @('surface', 'driver', 'capsule', 'conduct', 'connection', 'workflow', 'model', 'adapter', 'twin', 'wiring', 'start')

$layerByRole = @{}
$layerById = @{}
foreach ($layer in @(Get-Prop $side 'layers' @())) {
    $id = [string](Get-Prop $layer 'id' '')
    $role = [string](Get-Prop $layer 'role' '')
    if ($id -eq '' -or $roleOrder -notcontains $role -or $role -eq 'twin') { Stop-Fault "The sidecar holds a layer without an id or with an unknown role: '$id' '$role'." }
    if ($layerByRole.ContainsKey($role)) { Stop-Fault "The sidecar maps the role $role twice." }
    $layerByRole[$role] = $layer
    $layerById[$id] = $layer
}
foreach ($role in @('surface', 'driver', 'conduct')) {
    if (-not $layerByRole.ContainsKey($role)) { Stop-Fault "The sidecar maps no layer to the role $role." }
}

$namesFrom = @{}
foreach ($edge in @(Get-Prop $side 'names' @())) {
    $from = [string](Get-Prop $edge 'from' '')
    $to = [string](Get-Prop $edge 'to' '')
    if (-not $namesFrom.ContainsKey($from)) { $namesFrom[$from] = [System.Collections.Generic.List[string]]::new() }
    $namesFrom[$from].Add($to)
}

$sourcePath = [string](Get-Prop (Get-Prop $side 'source') 'path' '')
$sourceHash = [string](Get-Prop (Get-Prop $side 'source') 'hash' '')
$stale = $true
$sourceFull = Join-Path $root $sourcePath
if ($sourcePath -ne '' -and (Test-Path -LiteralPath $sourceFull -PathType Leaf)) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $actual = [System.BitConverter]::ToString($sha.ComputeHash([System.IO.File]::ReadAllBytes($sourceFull))).Replace('-', '').ToLowerInvariant()
    } finally { $sha.Dispose() }
    $stale = ($actual -ne $sourceHash.ToLowerInvariant())
}

$errors = [System.Collections.Generic.List[object]]::new()
$cautions = [System.Collections.Generic.List[object]]::new()

function Add-Error {
    param($Part, $Line, $Token, $Gate, [string]$Rule)
    $errors.Add([ordered]@{ part = $Part; line = $Line; token = $Token; gate = $Gate; rule = $Rule })
}

function Add-Caution {
    param($Part, $Gate, $Value, [string]$Source, [string]$Note)
    $cautions.Add([ordered]@{ part = $Part; gate = $Gate; value = $Value; source = $Source; note = $Note })
}

# ---------------------------------------------------------------- Gate tree
# A condition answers $true (open), $false (closed) or $null (waiting on an earlier gate).

function Get-One {
    param($Answers, [string]$Gate)
    if ($Answers.ContainsKey($Gate)) { return $Answers[$Gate][0] }
    return $null
}

function Test-Holds {
    param($Answers, [string]$Gate, [string[]]$Values)
    if (-not $Answers.ContainsKey($Gate)) { return $null }
    foreach ($value in $Values) { if ($Answers[$Gate] -contains $value) { return $true } }
    return $false
}

function New-Gate {
    param([string]$Name, [string[]]$Values, [bool]$Multi, [string]$Opener, [scriptblock]$When)
    return [pscustomobject]@{ Name = $Name; Values = $Values; Multi = $Multi; Opener = $Opener; When = $When }
}

$always = { param($a) $true }
$readsData = { param($a) Test-Holds $a 'data' @('read', 'read-write') }
$engineOpen = {
    param($a)
    $data = Get-One $a 'data'
    $rules = Test-Holds $a 'rules' @('workflow', 'model', 'adapter')
    if (($null -ne $data -and $data -ne 'none') -or $rules -eq $true) { return $true }
    if ($null -eq $data -or $null -eq $rules) { return $null }
    return $false
}
$storageOpen = {
    param($a)
    $data = Get-One $a 'data'
    $rules = Test-Holds $a 'rules' @('adapter')
    if (($null -ne $data -and $data -ne 'none') -or $rules -eq $true) { return $true }
    if ($null -eq $data -or $null -eq $rules) { return $null }
    return $false
}
$keyShown = { param($a) Test-Holds $a 'text' @('existing-key', 'new-key') }
$engineGates = @(
    (New-Gate 'connection' @('existing', 'new') $false 'data other than none, or rules below conduct' $engineOpen),
    (New-Gate 'workflow' @('untouched', 'changed') $false 'data other than none, or rules below conduct' $engineOpen),
    (New-Gate 'model' @('untouched', 'changed') $false 'data other than none, or rules below conduct' $engineOpen),
    (New-Gate 'storage' @('untouched', 'changed-adapter', 'new-adapter') $false 'data other than none, or rules holding adapter' $storageOpen)
)
$textGates = @(
    (New-Gate 'text' @('none', 'existing-key', 'new-key', 'new-term') $true 'always' $always),
    (New-Gate 'key-chosen-by' @('driver', 'conduct', 'model') $false 'text holding a key' $keyShown)
)

$gateTree = [ordered]@{
    'action' = @(
        (New-Gate 'entry' @('new-element', 'existing-element', 'driver-built', 'key', 'pointer') $true 'always' $always),
        (New-Gate 'result' @('none', 'existing-element', 'new-element', 'driver-built') $false 'always' $always),
        (New-Gate 'asks-user' @('no', 'driver-dialog', 'existing-port', 'new-port-method') $false 'always' $always),
        (New-Gate 'held-state' @('none', 'conduct', 'connection', 'transient', 'persisted') $true 'always' $always),
        (New-Gate 'rules' @('none', 'conduct', 'workflow', 'model', 'adapter') $true 'always' $always),
        (New-Gate 'data' @('none', 'read', 'write', 'read-write') $false 'always' $always),
        (New-Gate 'shows-engine-data' @('no', 'yes') $false 'data holding read' $readsData)
    ) + $engineGates + @(
        (New-Gate 'conduct-type' @('none', 'existing', 'new') $false 'always' $always)
    ) + $textGates + @(
        (New-Gate 'platform' @('none', 'windows-api') $false 'always' $always),
        (New-Gate 'platform-serves' @('gui-presentation', 'owner-behaviour') $false 'platform=windows-api' {
                param($a)
                $platform = Get-One $a 'platform'
                if ($null -eq $platform) { return $null }
                return ($platform -eq 'windows-api')
            }),
        (New-Gate 'platform-owner' @('connection', 'workflow', 'model', 'adapter') $false 'platform-serves=owner-behaviour' {
                param($a)
                if ((Get-One $a 'platform') -eq 'none') { return $false }
                $serves = Get-One $a 'platform-serves'
                if ($null -eq $serves) { return $null }
                return ($serves -eq 'owner-behaviour')
            })
    )
    'presentation' = @(
        (New-Gate 'structure' @('none', 'new', 'driver-built') $false 'always' $always),
        (New-Gate 'visual-by-logic' @('none', 'self-placed', 'surface-placed') $false 'always' $always),
        (New-Gate 'answer-shared' @('no', 'yes') $false 'always' $always),
        (New-Gate 'decides-request' @('no', 'yes') $false 'always' $always),
        (New-Gate 'held-state' @('none', 'transient', 'persisted') $true 'always' $always),
        (New-Gate 'conduct-state' @('none', 'existing', 'new') $false 'always' $always),
        (New-Gate 'data' @('none', 'read') $false 'always' $always),
        (New-Gate 'rules' @('none', 'workflow', 'model') $true 'data=read' {
                param($a)
                $data = Get-One $a 'data'
                if ($null -eq $data) { return $null }
                return ($data -eq 'read')
            })
    ) + $engineGates + @(
        (New-Gate 'conduct-type' @('none', 'existing', 'new') $false 'always' $always)
    ) + $textGates
}

$needs = @('gate', 'rule', 'held', 'mapping', 'conduct-type', 'state', 'event', 'call', 'entry', 'result',
    'contract', 'structure', 'visual', 'dialog', 'presentation', 'port', 'port-implementation', 'key-choice',
    'text-show', 'state-read', 'subscription', 'decision', 'store', 'connection', 'workflow', 'model',
    'contract-model', 'adapter', 'windows-call', 'twin-port', 'twin', 'wiring')

$boundaryMeanings = [ordered]@{
    'contract' = 'The driver pulls each surface part by contract ID; the surface gets the driver''s own shapes, never conduct types.'
    'cut' = 'Upper face of the cut: only .NET values and conduct types cross between the driver and conduct.'
    'cut-below' = 'Lower face of the cut: conduct names only what the connection project exposes to it, and passes none of it up.'
    'port' = 'A port declared by the lower side and implemented by the upper side: the driver for conduct, a twin for its owner.'
    'capsule' = 'Only .NET values; the capsule stores and holds no rule.'
    'layer' = 'The lower project''s own types, below the cut.'
    'wiring' = 'Construction only; the wiring project holds no behaviour.'
}

# ---------------------------------------------------------------- Reading parts

$tokenPattern = '^[a-z0-9-]+=[a-z0-9-]+(,[a-z0-9-]+)*$'

function Read-Part {
    param([int]$Line, [string[]]$Words)
    $id = 'p' + $Line
    $answers = @{}
    $tokenOf = @{}
    foreach ($word in $Words) {
        if ($word -cnotmatch $tokenPattern) {
            Add-Error $id $Line $word $null 'A token is gate=value in lowercase letters, digits and hyphens.'
            continue
        }
        $at = $word.IndexOf('=')
        $gate = $word.Substring(0, $at)
        if ($answers.ContainsKey($gate)) {
            Add-Error $id $Line $word $gate 'A gate appears at most once on a line.'
            continue
        }
        $answers[$gate] = @($word.Substring($at + 1).Split(','))
        $tokenOf[$gate] = $word
    }
    $part = [pscustomobject]@{ Id = $id; Line = $Line; Kind = $null; Answers = $answers; Open = [System.Collections.Generic.List[object]]::new() }

    $kind = Get-One $answers 'kind'
    if ($null -eq $kind) {
        $part.Open.Add([ordered]@{ gate = 'kind'; values = @($gateTree.Keys); multi = $false })
        return $part
    }
    if (-not $gateTree.Contains($kind) -or $answers['kind'].Count -ne 1) {
        Add-Error $id $Line $tokenOf['kind'] 'kind' 'The kind is action or presentation.'
        return $part
    }
    $part.Kind = $kind
    $gates = $gateTree[$kind]
    $known = @($gates | ForEach-Object { $_.Name }) + 'kind'
    foreach ($gate in @($answers.Keys)) {
        if ($known -notcontains $gate) {
            Add-Error $id $Line $tokenOf[$gate] $gate "The kind $kind has no such gate."
            $answers.Remove($gate)
        }
    }

    foreach ($gate in $gates) {
        $state = & $gate.When $answers
        $answered = $answers.ContainsKey($gate.Name)
        if ($state -eq $false) {
            if ($answered) {
                Add-Error $id $Line $tokenOf[$gate.Name] $gate.Name 'This gate is closed by an earlier answer.'
                $answers.Remove($gate.Name)
            }
            continue
        }
        $values = $gate.Values
        if ($answered) {
            $given = $answers[$gate.Name]
            $fault = $null
            if (-not $gate.Multi -and $given.Count -ne 1) { $fault = 'This gate takes one value.' }
            elseif ($gate.Multi -and $given.Count -gt 1 -and $given -contains 'none') { $fault = 'none stands alone.' }
            else {
                foreach ($value in $given) { if ($values -notcontains $value) { $fault = "The gate offers: $($values -join ', ')." } }
            }
            if ($null -ne $fault) {
                Add-Error $id $Line $tokenOf[$gate.Name] $gate.Name $fault
                $answers.Remove($gate.Name)
                if ($state -eq $true) { $part.Open.Add([ordered]@{ gate = $gate.Name; values = $values; multi = $gate.Multi }) }
            }
        } elseif ($state -eq $true) {
            $part.Open.Add([ordered]@{ gate = $gate.Name; values = $values; multi = $gate.Multi })
        }
    }
    return $part
}

function Get-ChangedConcerns {
    param($Answers)
    $changed = [System.Collections.Generic.List[string]]::new()
    if ((Get-One $Answers 'connection') -eq 'new') { $changed.Add('connection') }
    if ((Get-One $Answers 'workflow') -eq 'changed') { $changed.Add('workflow') }
    if ((Get-One $Answers 'model') -eq 'changed') { $changed.Add('model') }
    if (@('changed-adapter', 'new-adapter') -contains (Get-One $Answers 'storage')) { $changed.Add('adapter') }
    return , $changed.ToArray()
}

function Test-PartCautions {
    # Cautions are advisory: each points at an answer the structure document questions, and none blocks the plan.
    param($Part)
    $a = $Part.Answers
    $p = $Part.Id
    $changed = Get-ChangedConcerns $a
    foreach ($concern in @('workflow', 'model', 'adapter')) {
        if ((Test-Holds $a 'rules' @($concern)) -eq $true -and $changed -notcontains $concern) {
            Add-Caution $p 'rules' $concern 'Where logic goes' "A rule in $concern usually changes $concern; check the matching gate."
        }
    }
    $owner = Get-One $a 'platform-owner'
    if ($null -ne $owner -and $changed -notcontains $owner) {
        Add-Caution $p 'platform-owner' $owner 'Where logic goes' 'The part changes nothing in this concern; check the owner.'
    }
    if ($Part.Kind -eq 'presentation') {
        if ((Get-One $a 'answer-shared') -eq 'yes') {
            Add-Caution $p 'answer-shared' 'yes' 'Driver' 'An answer another medium would share is behaviour; it usually belongs in conduct.'
        }
        if ((Get-One $a 'decides-request') -eq 'yes') {
            Add-Caution $p 'decides-request' 'yes' 'Driver' 'A driver choosing what to ask conduct is medium work only when it rests on the medium, such as visibility; otherwise it belongs in conduct.'
        }
        if ((Get-One $a 'data') -eq 'read' -and (Get-One $a 'conduct-state') -eq 'none') {
            Add-Caution $p 'conduct-state' 'none' 'Data and the cut' 'Engine data reaches the driver only through conduct; conduct-state is usually existing or new.'
        }
    }
    if ((Get-One $a 'key-chosen-by') -eq 'driver') {
        Add-Caution $p 'key-chosen-by' 'driver' 'Driver' 'Choosing a localization key is behaviour unless the key is fixed to one control.'
    }
}

# ---------------------------------------------------------------- Placement

$logics = [ordered]@{}
$notes = [System.Collections.Generic.List[object]]::new()

function Resolve-Layer {
    param([string]$Role, [string]$OwnerRole = '')
    if ($Role -eq 'twin') {
        $owner = $null
        if ($layerByRole.ContainsKey($OwnerRole)) { $owner = $layerByRole[$OwnerRole] }
        $ownerId = [string](Get-Prop $owner 'id' $OwnerRole)
        foreach ($twin in @(Get-Prop $side 'twins' @())) {
            if ((Get-Prop $twin 'owner' '') -eq $ownerId) {
                return [pscustomobject]@{ Key = 'twin:' + $ownerId; Id = [string](Get-Prop $twin 'twin' ''); Role = 'twin'; Owner = $ownerId
                    Path = Get-Prop $twin 'path'; Exists = [bool](Get-Prop $twin 'exists' $false); Tests = @(Get-Prop $twin 'tests' @()) }
            }
        }
        return [pscustomobject]@{ Key = 'twin:' + $ownerId; Id = $ownerId + '.Windows'; Role = 'twin'; Owner = $ownerId; Path = $null; Exists = $false; Tests = @() }
    }
    if ($layerByRole.ContainsKey($Role)) {
        $layer = $layerByRole[$Role]
        return [pscustomobject]@{ Key = $Role; Id = [string](Get-Prop $layer 'id' ''); Role = $Role; Owner = $null
            Path = Get-Prop $layer 'path'; Exists = [bool](Get-Prop $layer 'exists' $false); Tests = @(Get-Prop $layer 'tests' @()) }
    }
    return [pscustomobject]@{ Key = $Role; Id = $null; Role = $Role; Owner = $null; Path = $null; Exists = $false; Tests = @() }
}

function Add-Need {
    param([string]$Role, [string]$Part, [string]$Need, [string]$OwnerRole = '')
    $layer = Resolve-Layer $Role $OwnerRole
    if (-not $logics.Contains($layer.Key)) {
        $logics[$layer.Key] = [pscustomobject]@{ Layer = $layer; Carries = [System.Collections.Generic.List[object]]::new(); Seen = @{} }
    }
    $logic = $logics[$layer.Key]
    $seenKey = $Part + '|' + $Need
    if (-not $logic.Seen.ContainsKey($seenKey)) {
        $logic.Seen[$seenKey] = $true
        $logic.Carries.Add([ordered]@{ part = $Part; need = $Need })
    }
}

function Add-Note {
    param([string]$Part, [string]$Note, [string[]]$Files)
    $notes.Add([ordered]@{ part = $Part; note = $Note; files = @($Files) })
}

function Add-SurfacePiece {
    param([string]$Part, [string]$Value, [string]$Need)
    if ($Value -eq 'new-element' -or $Value -eq 'new') {
        Add-Need 'surface' $Part 'structure'
        Add-Need 'driver' $Part 'contract'
    }
    if ($Value -eq 'driver-built') { Add-Need 'driver' $Part 'visual' }
    if ($null -ne $Need) { Add-Need 'driver' $Part $Need }
}

function Add-SharedPlacement {
    # The engine, the conduct type and the text gates mean the same in both kinds of part.
    param($Part)
    $a = $Part.Answers
    $p = $Part.Id
    if ((Get-One $a 'connection') -eq 'new') { Add-Need 'connection' $p 'connection' }
    if ((Get-One $a 'workflow') -eq 'changed') { Add-Need 'workflow' $p 'workflow' }
    if ((Get-One $a 'model') -eq 'changed') { Add-Need 'model' $p 'model' }
    switch (Get-One $a 'storage') {
        'changed-adapter' { Add-Need 'adapter' $p 'adapter' }
        'new-adapter' { Add-Need 'model' $p 'contract-model'; Add-Need 'adapter' $p 'adapter'; Add-Need 'wiring' $p 'wiring' }
    }
    foreach ($rule in @($(if ($a.ContainsKey('rules')) { $a['rules'] } else { @() }))) {
        switch ($rule) {
            'conduct' { Add-Need 'conduct' $p 'rule' }
            'workflow' { Add-Need 'workflow' $p 'rule' }
            'model' { Add-Need 'model' $p 'rule' }
            'adapter' { Add-Need 'adapter' $p 'rule' }
        }
    }
    if ((Get-One $a 'shows-engine-data') -eq 'yes' -or ($Part.Kind -eq 'presentation' -and (Get-One $a 'data') -eq 'read')) {
        Add-Need 'conduct' $p 'mapping'
    }
    if ((Get-One $a 'conduct-type') -eq 'new') { Add-Need 'conduct' $p 'conduct-type' }
    if ((Test-Holds $a 'text' @('existing-key', 'new-key')) -eq $true) {
        Add-Need 'driver' $p 'text-show'
        switch (Get-One $a 'key-chosen-by') {
            'conduct' { Add-Need 'conduct' $p 'key-choice' }
            'model' { Add-Need 'model' $p 'key-choice' }
        }
    }
    if ((Test-Holds $a 'text' @('new-key')) -eq $true) { Add-Note $p 'A new localization key goes into every locale file.' @(Get-Prop $side 'locales' @()) }
    if ((Test-Holds $a 'text' @('new-term')) -eq $true) { Add-Note $p 'A new Terms entry goes into every locale file.' @(Get-Prop $side 'locales' @()) }
}

function Add-ActionPlacement {
    param($Part)
    $a = $Part.Answers
    $p = $Part.Id
    Add-Need 'conduct' $p 'gate'
    Add-Need 'driver' $p 'call'
    foreach ($entry in $a['entry']) { Add-SurfacePiece $p $entry 'entry' }
    $result = Get-One $a 'result'
    if ($result -ne 'none') { Add-SurfacePiece $p $result 'result' }
    switch (Get-One $a 'asks-user') {
        'driver-dialog' { Add-Need 'driver' $p 'dialog' }
        'new-port-method' {
            Add-Need 'conduct' $p 'port'
            Add-Need 'driver' $p 'port-implementation'
            Add-Note $p 'Every driver implementing the port gains the new method, whatever its medium.' @()
        }
    }
    foreach ($held in $a['held-state']) {
        switch ($held) {
            'conduct' { Add-Need 'conduct' $p 'held' }
            'connection' { Add-Need 'connection' $p 'held' }
            'transient' { Add-Need 'driver' $p 'held' }
            'persisted' { Add-Need 'capsule' $p 'store'; Add-Need 'driver' $p 'decision' }
        }
    }
    Add-SharedPlacement $Part
    switch (Get-One $a 'platform-serves') {
        'gui-presentation' { Add-Need 'driver' $p 'windows-call' }
        'owner-behaviour' {
            $owner = Get-One $a 'platform-owner'
            Add-Need $owner $p 'twin-port'
            Add-Need 'twin' $p 'twin' $owner
            Add-Need 'wiring' $p 'wiring'
        }
    }
}

function Add-PresentationPlacement {
    param($Part)
    $a = $Part.Answers
    $p = $Part.Id
    Add-Need 'driver' $p 'presentation'
    Add-SurfacePiece $p (Get-One $a 'structure') $null
    switch (Get-One $a 'visual-by-logic') {
        'self-placed' { Add-Need 'driver' $p 'visual' }
        'surface-placed' { Add-Need 'driver' $p 'visual'; Add-Need 'surface' $p 'structure' }
    }
    foreach ($held in $a['held-state']) {
        if ($held -eq 'transient') { Add-Need 'driver' $p 'held' }
        if ($held -eq 'persisted') { Add-Need 'capsule' $p 'store'; Add-Need 'driver' $p 'decision' }
    }
    $state = Get-One $a 'conduct-state'
    if ($state -eq 'existing' -or $state -eq 'new' -or (Get-One $a 'data') -eq 'read') {
        Add-Need 'driver' $p 'subscription'
        Add-Need 'driver' $p 'state-read'
    }
    if ($state -eq 'new') {
        Add-Need 'conduct' $p 'state'
        Add-Need 'conduct' $p 'event'
    }
    Add-SharedPlacement $Part
}

# ---------------------------------------------------------------- Edges and boundaries

$engineRoles = @('conduct', 'connection', 'workflow', 'model', 'adapter')

function Get-Boundaries {
    param([string]$From, [string]$To, $FromLogic, $ToLogic)
    $result = [System.Collections.Generic.List[string]]::new()
    if ($From -eq 'surface' -and $To -eq 'driver') { $result.Add('contract') }
    if ($From -eq 'driver' -and $To -eq 'conduct') {
        $result.Add('cut')
        $implements = $null -ne $FromLogic -and @($FromLogic.Carries | Where-Object { $_.need -eq 'port-implementation' }).Count -gt 0
        if ($implements) { $result.Add('port') }
    }
    if ($From -eq 'driver' -and $To -eq 'capsule') { $result.Add('capsule') }
    if ($From -eq 'conduct' -and $engineRoles -contains $To) { $result.Add('cut-below') }
    elseif ($engineRoles -contains $From -and $engineRoles -contains $To) { $result.Add('layer') }
    if ($From -eq 'twin') { $result.Add('port') }
    if ($From -eq 'wiring') { $result.Add('wiring') }
    return , $result.ToArray()
}

function Get-RoleOfId {
    param([string]$Id)
    foreach ($role in $layerByRole.Keys) { if ((Get-Prop $layerByRole[$role] 'id' '') -eq $Id) { return $role } }
    return $null
}

function Get-PartLayerIds {
    # The layers a part reaches: those it adds code to, plus untouched ones its answers use.
    param($Part, $Ordered)
    $ids = [System.Collections.Generic.List[string]]::new()
    foreach ($logic in $Ordered) {
        if ($null -ne $logic.Layer.Id -and $logic.Layer.Role -ne 'twin' -and $logic.Layer.Role -ne 'wiring' -and
            @($logic.Carries | Where-Object { $_.part -eq $Part.Id }).Count -gt 0) { $ids.Add($logic.Layer.Id) }
    }
    $a = $Part.Answers
    $conductId = [string](Get-Prop $layerByRole['conduct'] 'id' '')
    $driverId = [string](Get-Prop $layerByRole['driver'] 'id' '')
    if (-not $ids.Contains($conductId) -and @('existing', 'new') -contains (Get-One $a 'conduct-state')) { $ids.Add($conductId) }
    if (-not $ids.Contains($driverId)) { $ids.Add($driverId) }
    $usesEngine = ($null -ne (Get-One $a 'data') -and (Get-One $a 'data') -ne 'none')
    if ($usesEngine) {
        if (-not $ids.Contains($conductId)) { $ids.Add($conductId) }
        if ($layerByRole.ContainsKey('connection')) {
            $connectionId = [string](Get-Prop $layerByRole['connection'] 'id' '')
            if (-not $ids.Contains($connectionId)) { $ids.Add($connectionId) }
        }
    }
    return , $ids.ToArray()
}

function Get-Edges {
    param($Ordered, $Parts)
    $byId = @{}
    foreach ($logic in $Ordered) { if ($null -ne $logic.Layer.Id) { $byId[$logic.Layer.Id] = $logic } }
    $wiringId = [string](Get-Prop $layerByRole['wiring'] 'id' '')
    $edges = [ordered]@{}
    foreach ($part in $Parts) {
        $reach = Get-PartLayerIds $part $Ordered
        foreach ($start in $reach) {
            $queue = [System.Collections.Generic.Queue[object]]::new()
            $queue.Enqueue(@($start, @()))
            $visited = @{ $start = $true }
            while ($queue.Count -gt 0) {
                $item = $queue.Dequeue()
                $node = $item[0]
                $path = $item[1]
                if (-not $namesFrom.ContainsKey($node)) { continue }
                foreach ($to in $namesFrom[$node]) {
                    if ($to -eq $wiringId -or $visited.ContainsKey($to)) { continue }
                    $visited[$to] = $true
                    if ($reach -contains $to) {
                        $key = $start + '>' + $to
                        if (-not $edges.Contains($key)) {
                            $fromLogic = $null; if ($byId.ContainsKey($start)) { $fromLogic = $byId[$start] }
                            $toLogic = $null; if ($byId.ContainsKey($to)) { $toLogic = $byId[$to] }
                            $edges[$key] = [ordered]@{ from = $start; to = $to
                                boundaries = (Get-Boundaries (Get-RoleOfId $start) (Get-RoleOfId $to) $fromLogic $toLogic)
                                through = @($path); touched = ($byId.ContainsKey($start) -and $byId.ContainsKey($to))
                                parts = [System.Collections.Generic.List[string]]::new() }
                        }
                        if (-not $edges[$key]['parts'].Contains($part.Id)) { $edges[$key]['parts'].Add($part.Id) }
                    } else {
                        $queue.Enqueue(@($to, (@($path) + $to)))
                    }
                }
            }
        }
    }
    foreach ($logic in $Ordered) {
        $partIds = @(Get-OrdinalSorted @($logic.Carries | ForEach-Object { $_.part }))
        if ($logic.Layer.Role -eq 'twin' -and $byId.ContainsKey($logic.Layer.Owner)) {
            $edges['twin>' + $logic.Layer.Id] = [ordered]@{ from = $logic.Layer.Id; to = $logic.Layer.Owner; boundaries = @('port'); through = @(); touched = $true; parts = $partIds }
        }
        if ($byId.ContainsKey($wiringId) -and ($logic.Layer.Role -eq 'adapter' -or $logic.Layer.Role -eq 'twin')) {
            $edges['wiring>' + $logic.Layer.Id] = [ordered]@{ from = $wiringId; to = $logic.Layer.Id; boundaries = @('wiring'); through = @(); touched = $true; parts = $partIds }
        }
    }
    $list = [System.Collections.Generic.List[object]]::new()
    foreach ($key in $edges.Keys) {
        $edge = $edges[$key]
        $edge['parts'] = @($edge['parts'])
        $list.Add($edge)
    }
    return , $list.ToArray()
}

# ---------------------------------------------------------------- Output

function New-Head {
    return [ordered]@{
        generation = [int](Get-Prop $side 'generation' 1)
        project = [string](Get-Prop $side 'project' '')
        stale = $stale
        advisory = 'A suggestion of source projects, not a ruling. The structure document decides.'
    }
}

function Write-Schema {
    $out = New-Head
    $roles = [ordered]@{}
    foreach ($role in $roleOrder) {
        if ($role -eq 'twin') {
            $roles[$role] = @(@(Get-Prop $side 'twins' @()) | ForEach-Object { [string](Get-Prop $_ 'path' '') })
        } elseif ($layerByRole.ContainsKey($role)) {
            $roles[$role] = [string](Get-Prop $layerByRole[$role] 'path' '')
        } else {
            $roles[$role] = $null
        }
    }
    $out['roles'] = $roles
    $kinds = [ordered]@{}
    foreach ($kind in $gateTree.Keys) {
        $kinds[$kind] = @($gateTree[$kind] | ForEach-Object {
                [ordered]@{ gate = $_.Name; values = $_.Values; multi = $_.Multi; 'opened-by' = $_.Opener }
            })
    }
    $out['kinds'] = $kinds
    $out['needs'] = $needs
    $out['boundaries'] = $boundaryMeanings
    Write-Json $out
}

function Write-Open {
    param($Parts)
    $out = New-Head
    $open = [System.Collections.Generic.List[object]]::new()
    foreach ($part in $Parts) {
        if ($part.Open.Count -gt 0) { $open.Add([ordered]@{ part = $part.Id; line = $part.Line; gates = $part.Open.ToArray() }) }
    }
    $out['open'] = $open.ToArray()
    $out['errors'] = $errors.ToArray()
    Write-Json $out
}

function Write-Plan {
    param($Parts)
    foreach ($part in $Parts) {
        Test-PartCautions $part
        if ($part.Kind -eq 'action') { Add-ActionPlacement $part }
        if ($part.Kind -eq 'presentation') { Add-PresentationPlacement $part }
    }
    $sortKeys = @{}
    foreach ($key in $logics.Keys) {
        $logic = $logics[$key]
        $index = [array]::IndexOf($roleOrder, $logic.Layer.Role)
        $sortKeys[('{0:d2}|{1}' -f $index, [string]$logic.Layer.Id)] = $logic
    }
    $ordered = @(Get-OrdinalSorted @($sortKeys.Keys) | ForEach-Object { $sortKeys[$_] })

    $tests = [System.Collections.Generic.List[string]]::new()
    $newProjects = [System.Collections.Generic.List[object]]::new()
    $projects = [System.Collections.Generic.List[object]]::new()
    foreach ($logic in $ordered) {
        $layer = $logic.Layer
        foreach ($test in $layer.Tests) { if ([string]$test -ne '') { $tests.Add([string]$test) } }
        if (-not $layer.Exists) { $newProjects.Add([ordered]@{ project = $layer.Path; role = $layer.Role }) }
        $projects.Add([ordered]@{ project = $layer.Path; role = $layer.Role; carries = $logic.Carries.ToArray() })
    }

    $out = New-Head
    $out['count'] = $projects.Count
    $out['projects'] = $projects.ToArray()
    $out['edges'] = Get-Edges $ordered $Parts
    $out['tests'] = Get-OrdinalSorted $tests.ToArray()
    $out['new-projects'] = $newProjects.ToArray()
    $out['notes'] = $notes.ToArray()
    $out['cautions'] = $cautions.ToArray()
    Write-Json $out
}

# ---------------------------------------------------------------- Main

if ($modeCount -gt 1) { Stop-Fault 'Give exactly one of -Schema, -Next and -Plan.' }
if ($Schema) { Write-Schema; exit 0 }

$parts = [System.Collections.Generic.List[object]]::new()
if ($File -ne '') {
    if ($Tokens.Count -gt 0) { Stop-Fault 'Give either -File or tokens, not both.' }
    $filePath = $File
    if (-not [System.IO.Path]::IsPathRooted($filePath)) { $filePath = Join-Path (Get-Location).Path $filePath }
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) { Stop-Fault "The file $File is missing." }
    try {
        $lines = [System.IO.File]::ReadAllLines($filePath, [System.Text.UTF8Encoding]::new($false))
    } catch {
        Stop-Fault "The file $File is unreadable: $($_.Exception.Message)"
    }
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $text = $lines[$i].Trim()
        if ($text -eq '' -or $text.StartsWith('#')) { continue }
        $parts.Add((Read-Part ($i + 1) @($text -split '\s+')))
    }
} elseif ($Tokens.Count -gt 0) {
    $parts.Add((Read-Part 1 $Tokens))
}

if ($parts.Count -eq 0) { Add-Error $null $null $null $null 'The work holds no part.' }

$openCount = @($parts | Where-Object { $_.Open.Count -gt 0 }).Count
if ($Plan -and $errors.Count -eq 0 -and $openCount -eq 0) {
    Write-Plan $parts
    exit 0
}
Write-Open $parts
if ($errors.Count -gt 0) { exit 1 }
exit 0
