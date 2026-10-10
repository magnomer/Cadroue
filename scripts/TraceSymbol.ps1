<#
.SYNOPSIS
    Find everything a change inside a symbol can affect.
.DESCRIPTION
    Takes one or more symbol names - a type, method, property or field - and
    walks the code outward from them. Any edit inside the symbol counts, its
    body, its signature or its name, so the walk follows every textual use of the
    name, not only calls.

    The symbol table comes from the graft index: every card under the graft
    folder lists the definitions of one source file with their line spans. graft
    is asked one question first so the index reflects uncommitted edits. Field
    and auto-property declarations, which the cards leave out, are read from the
    source and join the table.

    Every identifier of every indexed source file and every markup file is read
    once. A use of a name inside another symbol makes that symbol affected, and
    the walk goes on from there until nothing new turns up.

    A seed is taken whole: a seed type brings all its parts and members, and every
    mention of every seed name is followed. Past the seeds a change travels through
    members, not types. An affected method, property or field is followed through
    every mention of its name; a name outside the project prefix is private, so it
    is followed only inside its own type. An affected constructor or type is
    followed through its creators: lines that say new, and markup that
    instantiates it. An affected member outside the prefix that is not a field is
    entered by the framework, so its type's creators are followed too. Markup
    belongs to its x:Class type, and its references to that type's own members are
    wiring, not use. Test methods are recorded and never followed further.

    Every project-specific value - graft folder, test markers, name prefix,
    markup extensions - lives in TraceSymbol.json. This script carries none, so the
    file is identical in every project at the same generation.
.PARAMETER Name
    One or more symbol names to start from. Positional.
.PARAMETER Depth
    Maximum number of hops to walk. Defaults to 0, which walks until nothing new turns up.
.PARAMETER Json
    Write the result as JSON instead of the report. Test.ps1 reads this form.
.PARAMETER Help
    Display this help and exit. The alias -? is supported.
.EXAMPLE
    tracesymbol LTwin
    Report every symbol and test a change inside LTwin can affect.
.EXAMPLE
    tracesymbol LTwin LTrail -Depth 2
    Report what lies within two hops of either symbol.
.EXAMPLE
    tracesymbol LTwin -Json
    Write the same result as JSON.
#>
#requires -Version 5.1
# TRACESYMBOL - TRACE GENERATION 1.
# ALIAS detector
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the executed outcome changes.
# Generation 1: the symbol table is read from the graft cards after one graft query refreshes them,
# plus field declarations read from the source; every identifier of every indexed source file and
# markup file is indexed once; seeds are followed through every mention and bring their members;
# past the seeds members are followed by name, private names inside their type only, and
# constructors and types through their creators; markup belongs to its x:Class type and skips its
# own wiring; test methods, found by their marker attribute, are leaves.
# Every project-specific value lives in TraceSymbol.json, so this file is identical in every project at
# this generation.
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string[]]$Name,

    [ValidateRange(0, [int]::MaxValue)]
    [int]$Depth = 0,

    [switch]$Json,

    [Alias('?')]
    [switch]$Help
)

if ($Help) {
    @'
NAME
    TraceSymbol.ps1

SYNOPSIS
    Find everything a change inside a symbol can affect.

SYNTAX
    tracesymbol <name> [<name> ...] [-Depth <hops>] [-Json] [-Help]

OPTIONS
    <name>
        Symbol to start from: a type, method, property or field. Several are allowed.

    -Depth <hops>
        Maximum number of hops to walk. Defaults to 0, no limit.

    -Json
        Write the result as JSON instead of the report.

    -Help, -?
        Display this help and exit.

EXAMPLES
    tracesymbol LTwin
        Report every symbol and test a change inside LTwin can affect.

    tracesymbol LTwin LTrail -Depth 2
        Report what lies within two hops of either symbol.

    tracesymbol LTwin -Json
        Write the same result as JSON.
'@ | Write-Host
    exit 0
}

Write-Host 'TRACESYMBOL - TRACE GENERATION 1' -ForegroundColor Blue

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding

function Get-OrdinalKey {
    # Sort-Object compares text by culture, which Windows PowerShell 5.1 and pwsh 7 order differently.
    # Uppercase hexadecimal UTF-16 code units compare alike under every culture, so this key sorts ordinally.
    param([string]$Text)
    return [System.BitConverter]::ToString([System.Text.Encoding]::BigEndianUnicode.GetBytes($Text)).Replace('-', '')
}

function ConvertTo-DetectorJson {
    # Written by hand because ConvertTo-Json lays out and escapes text differently in Windows
    # PowerShell 5.1 and pwsh 7. This matches the pwsh 7 form, so both write the same text.
    param($Value, [string]$Indent = '')

    if ($null -eq $Value) { return 'null' }
    if ($Value -is [bool]) { return $Value.ToString().ToLowerInvariant() }
    if ($Value -is [string]) {
        $builder = [System.Text.StringBuilder]::new('"')
        foreach ($character in $Value.ToCharArray()) {
            switch ([int]$character) {
                0x22 { [void]$builder.Append('\"') }
                0x5C { [void]$builder.Append('\\') }
                0x08 { [void]$builder.Append('\b') }
                0x09 { [void]$builder.Append('\t') }
                0x0A { [void]$builder.Append('\n') }
                0x0C { [void]$builder.Append('\f') }
                0x0D { [void]$builder.Append('\r') }
                default {
                    if ([int]$character -lt 0x20) { [void]$builder.AppendFormat('\u{0:x4}', [int]$character) }
                    else { [void]$builder.Append($character) }
                }
            }
        }
        return $builder.Append('"').ToString()
    }
    if ($Value -is [ValueType]) { return [System.Convert]::ToString($Value, [System.Globalization.CultureInfo]::InvariantCulture) }

    $inner = $Indent + '  '
    $members = [System.Collections.Generic.List[string]]::new()
    if ($Value -is [System.Collections.IDictionary]) {
        foreach ($key in $Value.Keys) {
            $members.Add($inner + (ConvertTo-DetectorJson -Value ([string]$key)) + ': ' + (ConvertTo-DetectorJson -Value $Value[$key] -Indent $inner))
        }
        if ($members.Count -eq 0) { return '{}' }
        return "{`n" + ($members -join ",`n") + "`n" + $Indent + '}'
    }
    if ($Value -is [System.Collections.IEnumerable]) {
        foreach ($item in $Value) {
            $members.Add($inner + (ConvertTo-DetectorJson -Value $item -Indent $inner))
        }
        if ($members.Count -eq 0) { return '[]' }
        return "[`n" + ($members -join ",`n") + "`n" + $Indent + ']'
    }
    foreach ($property in $Value.PSObject.Properties) {
        $members.Add($inner + (ConvertTo-DetectorJson -Value $property.Name) + ': ' + (ConvertTo-DetectorJson -Value $property.Value -Indent $inner))
    }
    if ($members.Count -eq 0) { return '{}' }
    return "{`n" + ($members -join ",`n") + "`n" + $Indent + '}'
}

function Read-DetectorConfig {
    $configPath = Join-Path $PSScriptRoot 'TraceSymbol.json'
    if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
        throw "The TraceSymbol configuration was not found: $configPath"
    }

    try {
        $config = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The TraceSymbol configuration is not valid JSON: $configPath`n$($_.Exception.Message)"
    }

    foreach ($key in @('generation', 'graft', 'markers', 'prefix', 'markup', 'wiring')) {
        if (-not ($config.PSObject.Properties.Name -contains $key)) {
            throw "The TraceSymbol configuration must contain a $key property: $configPath"
        }
    }

    if ([int]$config.generation -ne 1) {
        throw "The TraceSymbol configuration is generation $($config.generation); this script is generation 1."
    }

    return $config
}

function New-DetectorSymbol([string]$symbolName, [string]$kind, [string]$path, [int]$start, [int]$end, [bool]$test) {
    $symbol = [pscustomobject]@{
        Id      = $symbolTable.Count
        Name    = $symbolName
        Kind    = $kind
        Path    = $path
        Start   = $start
        End     = $end
        Type    = $typeKinds -contains $kind
        Test    = $test
        Owner   = $null
        Members = [System.Collections.Generic.List[object]]::new()
    }

    $symbolTable.Add($symbol)
    $list = $null
    if (-not $byName.TryGetValue($symbolName, [ref]$list)) {
        $list = [System.Collections.Generic.List[object]]::new()
        $byName[$symbolName] = $list
    }
    $list.Add($symbol)

    return $symbol
}

function Read-DetectorLines([string]$text) {
    $lineStarts = [System.Collections.Generic.List[int]]::new()
    $lineStarts.Add(0)
    foreach ($match in $linePattern.Matches($text)) {
        $lineStarts.Add($match.Index + 1)
    }

    return , $lineStarts
}

function Find-DetectorLine($lineStarts, [int]$index) {
    $position = $lineStarts.BinarySearch($index)
    if ($position -ge 0) {
        return $position + 1
    }

    return -bnot $position
}

function Find-DetectorHolder($source, [int]$line) {
    $holder = $null
    foreach ($symbol in $source.Symbols) {
        if ($symbol.Start -gt $line -or $symbol.End -lt $line) {
            continue
        }
        if ($null -eq $holder -or ($symbol.End - $symbol.Start) -lt ($holder.End - $holder.Start)) {
            $holder = $symbol
        }
    }

    return $holder
}

function Read-DetectorCards {
    $entryPattern = [regex]'^- (?<name>\S+) \u00B7 (?<kind>\w+) \u00B7 L(?<start>\d+)-L(?<end>\d+)(?: \u2014 (?<signature>.*))?$'

    foreach ($card in Get-ChildItem -LiteralPath $graftRoot -Recurse -Filter '*.md' -File) {
        $lines = [System.IO.File]::ReadAllLines($card.FullName, [System.Text.Encoding]::UTF8)
        if ($lines.Count -eq 0 -or -not $lines[0].StartsWith('# ')) {
            continue
        }

        $path = $lines[0].Substring(2).Trim()
        $fullPath = Join-Path $root $path
        if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
            continue
        }

        $symbols = [System.Collections.Generic.List[object]]::new()
        foreach ($line in $lines) {
            $match = $entryPattern.Match($line)
            if ($match.Success) {
                $symbols.Add((New-DetectorSymbol $match.Groups['name'].Value $match.Groups['kind'].Value $path ([int]$match.Groups['start'].Value) ([int]$match.Groups['end'].Value) $markerPattern.IsMatch($match.Groups['signature'].Value)))
            }
        }

        foreach ($symbol in $symbols) {
            foreach ($candidate in $symbols) {
                if (-not $candidate.Type -or $candidate.Id -eq $symbol.Id) {
                    continue
                }
                if ($candidate.Start -gt $symbol.Start -or $candidate.End -lt $symbol.End) {
                    continue
                }
                if ($null -eq $symbol.Owner -or ($candidate.End - $candidate.Start) -lt ($symbol.Owner.End - $symbol.Owner.Start)) {
                    $symbol.Owner = $candidate
                }
            }

            if ($null -ne $symbol.Owner) {
                $symbol.Owner.Members.Add($symbol)
            }
        }

        $text = [System.IO.File]::ReadAllText($fullPath)
        $sources.Add([pscustomobject]@{
            Path       = $path
            FullPath   = $fullPath
            Text       = $text
            LineStarts = (Read-DetectorLines $text)
            Symbols    = $symbols
            Holders    = $null
            Own        = $null
        })
    }
}

function Read-DetectorFields {
    $fieldPattern = [regex]'(?m)^[ \t]*(?:\[[^\]\r\n]*\][ \t]*)*(?:(?:public|private|protected|internal|static|readonly|const|volatile|required|new|event)[ \t]+)+(?<type>[^=;{}()\r\n]+?)[ \t]+(?<name>[A-Za-z_]\w*)[ \t]*(?:=(?!>)|;|\{|\r?$)'
    $keywordPattern = [regex]'\b(class|record|struct|interface|enum|delegate|namespace|operator|implicit|explicit)\b'

    foreach ($source in @($sources)) {
        foreach ($match in $fieldPattern.Matches($source.Text)) {
            if ($keywordPattern.IsMatch($match.Groups['type'].Value)) {
                continue
            }

            $line = Find-DetectorLine $source.LineStarts $match.Groups['name'].Index
            $holder = Find-DetectorHolder $source $line
            if ($null -eq $holder -or -not $holder.Type -or $holder.Start -eq $line) {
                continue
            }

            $field = New-DetectorSymbol $match.Groups['name'].Value 'field' $source.Path $line $line $false
            $field.Owner = $holder
            $holder.Members.Add($field)
            $source.Symbols.Add($field)
        }
    }
}

function Read-DetectorMarkup {
    $classPattern = [regex]'x:Class="(?<type>[\w.]+)"'
    $roots = @($sources | ForEach-Object { ($_.Path -split '/')[0] } | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } } -Unique)

    foreach ($folder in $roots) {
        $folderPath = Join-Path $root $folder
        if (-not (Test-Path -LiteralPath $folderPath -PathType Container)) {
            continue
        }

        foreach ($file in Get-ChildItem -LiteralPath $folderPath -Recurse -File) {
            if (-not (@($config.markup) -contains $file.Extension)) {
                continue
            }
            if ($file.FullName -match '[\\/](obj|bin)[\\/]') {
                continue
            }

            $text = [System.IO.File]::ReadAllText($file.FullName)
            $match = $classPattern.Match($text)
            if (-not $match.Success) {
                continue
            }

            $types = $null
            if (-not $byName.TryGetValue(($match.Groups['type'].Value -split '\.')[-1], [ref]$types)) {
                continue
            }

            $holders = @($types | Where-Object { $_.Type })
            $own = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
            foreach ($type in $holders) {
                [void]$own.Add($type.Name)
                foreach ($member in $type.Members) {
                    [void]$own.Add($member.Name)
                }
            }

            $sources.Add([pscustomobject]@{
                Path       = $file.FullName.Substring($root.Length).TrimStart('\', '/').Replace('\', '/')
                FullPath   = $file.FullName
                Text       = $text
                LineStarts = $null
                Symbols    = $null
                Holders    = $holders
                Own        = $own
            })
        }
    }
}

function Add-DetectorReference([string]$key, $holders) {
    $set = $null
    if (-not $references.TryGetValue($key, [ref]$set)) {
        $set = [System.Collections.Generic.HashSet[int]]::new()
        $references[$key] = $set
    }

    foreach ($holder in $holders) {
        [void]$set.Add($holder.Id)
    }
}

function Read-DetectorReferences {
    $wordPattern = [regex]'\w+'
    $creationPattern = [regex]'\bnew\b'
    $wiringPattern = [regex][string]$config.wiring
    $callPattern = [regex]'\G\s*\('
    $methodNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($symbol in $symbolTable) {
        if ($symbol.Kind -eq 'method' -and -not $byName[$symbol.Name].Exists({ param($other) $other.Type })) {
            [void]$methodNames.Add($symbol.Name)
        }
    }

    foreach ($source in $sources) {
        $lineHolders = @{}
        $lineCreates = @{}
        $lineWires = @{}

        foreach ($match in $wordPattern.Matches($source.Text)) {
            if (-not $byName.ContainsKey($match.Value)) {
                continue
            }

            if ($null -ne $source.Holders) {
                if ($source.Own.Contains($match.Value)) {
                    continue
                }

                Add-DetectorReference $match.Value $source.Holders
                Add-DetectorReference ('new ' + $match.Value) $source.Holders
                continue
            }

            $line = Find-DetectorLine $source.LineStarts $match.Index
            if (-not $lineHolders.ContainsKey($line)) {
                $holder = Find-DetectorHolder $source $line
                $lineHolders[$line] = if ($null -ne $holder) { @($holder) } else { @($source.Symbols | Where-Object { $_.Type -and $null -eq $_.Owner }) }

                $lineEnd = if ($line -lt $source.LineStarts.Count) { $source.LineStarts[$line] } else { $source.Text.Length }
                $lineStart = $source.LineStarts[$line - 1]
                $lineText = $source.Text.Substring($lineStart, $lineEnd - $lineStart)
                $lineCreates[$line] = $creationPattern.IsMatch($lineText)
                $lineWires[$line] = $wiringPattern.IsMatch($lineText)
            }

            if ($lineWires[$line] -and $methodNames.Contains($match.Value) -and -not $callPattern.IsMatch($source.Text, $match.Index + $match.Length)) {
                continue
            }

            Add-DetectorReference $match.Value $lineHolders[$line]
            if ($lineCreates[$line]) {
                Add-DetectorReference ('new ' + $match.Value) $lineHolders[$line]
            }
        }
    }
}

function Push-DetectorName([string]$key, [string]$scope) {
    if ($searched.Add($key + '|' + $scope)) {
        $frontier.Add([pscustomobject]@{ Key = $key; Scope = $scope })
    }
}

function Test-DetectorScope($symbol, [string]$scope) {
    $current = $symbol
    while ($null -ne $current) {
        if ($current.Type -and $current.Name -ceq $scope) {
            return $true
        }
        $current = $current.Owner
    }

    return $false
}

function Add-DetectorSymbol($symbol, [int]$hop, [string]$via) {
    if ($reached.ContainsKey($symbol.Id)) {
        return
    }

    $reached[$symbol.Id] = $hop
    $routes[$symbol.Id] = $via

    if ($symbol.Test) {
        return
    }

    if ($symbol.Type) {
        Push-DetectorName ('new ' + $symbol.Name) ''
        if ($hop -gt 0) {
            return
        }

        Push-DetectorName $symbol.Name ''
        foreach ($part in $byName[$symbol.Name]) {
            if ($part.Type) {
                Add-DetectorSymbol $part $hop $symbol.Name
            }
        }
        foreach ($member in $symbol.Members) {
            Add-DetectorSymbol $member $hop $symbol.Name
        }
        return
    }

    if ($null -ne $symbol.Owner -and $symbol.Name -ceq $symbol.Owner.Name) {
        Push-DetectorName ('new ' + $symbol.Name) ''
        return
    }

    if ($symbol.Name -cmatch [string]$config.prefix) {
        Push-DetectorName $symbol.Name ''
        return
    }

    if ($null -eq $symbol.Owner) {
        Push-DetectorName $symbol.Name ''
        return
    }

    Push-DetectorName $symbol.Name $symbol.Owner.Name
    if ($symbol.Kind -ne 'field') {
        Add-DetectorSymbol $symbol.Owner $hop $symbol.Name
    }
}

function Find-DetectorProject([string]$path) {
    $folder = Split-Path -Parent (Join-Path $root $path)
    while ($folder.Length -ge $root.Length) {
        if ($projectCache.ContainsKey($folder)) {
            return $projectCache[$folder]
        }

        $project = Get-ChildItem -LiteralPath $folder -Filter '*.csproj' -File | Select-Object -First 1
        if ($null -ne $project) {
            $projectCache[$folder] = $project.BaseName
            return $project.BaseName
        }

        $folder = Split-Path -Parent $folder
    }

    return ''
}

if ($null -eq $Name -or $Name.Count -eq 0) {
    throw 'At least one symbol name is required. Run tracesymbol -Help for usage.'
}

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')).TrimEnd('\', '/')
$config = Read-DetectorConfig
$graftRoot = Join-Path $root ([string]$config.graft)

if (-not (Test-Path -LiteralPath $graftRoot -PathType Container)) {
    throw "The graft index was not found: $graftRoot"
}

$graft = Get-Command graft -ErrorAction SilentlyContinue
if ($null -eq $graft) {
    throw 'graft is required to refresh the index, but graft was not found on PATH.'
}

Push-Location $root
try {
    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & $graft.Source grep --fixed $Name[0] --json *> $null
    $graftExit = $LASTEXITCODE
    $ErrorActionPreference = $nativePreference
}
finally {
    Pop-Location
}

if ($graftExit -ne 0) {
    throw "graft could not refresh the index (exit code $graftExit)."
}

$typeKinds = @('class', 'interface', 'enum', 'struct', 'record')
$markerPattern = [regex]('^\[(' + ((@($config.markers) | ForEach-Object { [regex]::Escape([string]$_) }) -join '|') + ')\b')
$linePattern = [regex]"`n"
$symbolTable = [System.Collections.Generic.List[object]]::new()
$byName = [System.Collections.Generic.Dictionary[string, System.Collections.Generic.List[object]]]::new([System.StringComparer]::Ordinal)
$references = [System.Collections.Generic.Dictionary[string, System.Collections.Generic.HashSet[int]]]::new([System.StringComparer]::Ordinal)
$sources = [System.Collections.Generic.List[object]]::new()
$reached = [System.Collections.Generic.Dictionary[int, int]]::new()
$routes = [System.Collections.Generic.Dictionary[int, string]]::new()
$searched = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$frontier = [System.Collections.Generic.List[object]]::new()
$projectCache = @{}

Read-DetectorCards
Read-DetectorFields
Read-DetectorMarkup
Read-DetectorReferences

foreach ($seed in $Name) {
    if (-not $byName.ContainsKey($seed)) {
        throw "The symbol was not found in the graft index: $seed"
    }

    foreach ($symbol in $byName[$seed]) {
        Add-DetectorSymbol $symbol 0 ''
    }
    Push-DetectorName $seed ''
}

$hop = 0
while ($frontier.Count -gt 0 -and ($Depth -eq 0 -or $hop -lt $Depth)) {
    $hop++
    $current = $frontier
    $frontier = [System.Collections.Generic.List[object]]::new()

    foreach ($entry in $current) {
        $holders = $null
        if (-not $references.TryGetValue($entry.Key, [ref]$holders)) {
            continue
        }

        foreach ($id in $holders) {
            $symbol = $symbolTable[$id]
            if ($entry.Scope -and -not (Test-DetectorScope $symbol $entry.Scope)) {
                continue
            }

            Add-DetectorSymbol $symbol $hop $entry.Key
        }
    }
}

$affected = @(
    foreach ($pair in $reached.GetEnumerator()) {
        $symbol = $symbolTable[$pair.Key]
        [pscustomobject]@{
            name  = $symbol.Name
            kind  = $symbol.Kind
            path  = $symbol.Path
            start = $symbol.Start
            end   = $symbol.End
            hop   = $pair.Value
            via   = $routes[$pair.Key]
            test  = $symbol.Test
        }
    }
) | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_.path } }, start

$tests = @(
    foreach ($pair in $reached.GetEnumerator()) {
        $symbol = $symbolTable[$pair.Key]
        if (-not $symbol.Test) {
            continue
        }

        [pscustomobject]@{
            project = Find-DetectorProject $symbol.Path
            class   = if ($null -ne $symbol.Owner) { $symbol.Owner.Name } else { '' }
            method  = $symbol.Name
            path    = $symbol.Path
            start   = $symbol.Start
        }
    }
) | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_.project } }, @{ Expression = { Get-OrdinalKey $_.class } }, @{ Expression = { Get-OrdinalKey $_.method } }

$affected = @($affected)
$tests = @($tests)
$hops = if ($affected.Count -gt 0) { [int]($affected | Measure-Object -Property hop -Maximum).Maximum } else { 0 }

if ($Json) {
    ConvertTo-DetectorJson -Value ([ordered]@{
        seeds   = @($Name)
        hops    = $hops
        symbols = $affected
        tests   = $tests
    })
    exit 0
}

$fileCount = @($affected | Select-Object -ExpandProperty path -Unique).Count
$projectCount = @($tests | Select-Object -ExpandProperty project -Unique).Count
Write-Host ("{0}: {1} symbol(s) in {2} file(s), {3} test(s) in {4} project(s), {5} hop(s)." -f ($Name -join ', '), $affected.Count, $fileCount, $tests.Count, $projectCount, $hops) -ForegroundColor Cyan

foreach ($group in $affected | Group-Object -Property path) {
    Write-Host ''
    Write-Host $group.Name -ForegroundColor Yellow
    foreach ($symbol in $group.Group) {
        $route = if ($symbol.via) { "  <- $($symbol.via)" } else { '' }
        Write-Host ("  L{0,-6} {1,-9} {2}{3}" -f $symbol.start, $symbol.kind, $symbol.name, $route)
    }
}

if ($tests.Count -gt 0) {
    Write-Host ''
    Write-Host 'Tests' -ForegroundColor Cyan
    foreach ($group in $tests | Group-Object -Property project) {
        Write-Host ("  {0} ({1})" -f $group.Name, $group.Count) -ForegroundColor Yellow
        foreach ($test in $group.Group) {
            Write-Host ("    {0}.{1}" -f $test.class, $test.method)
        }
    }
}

exit 0
