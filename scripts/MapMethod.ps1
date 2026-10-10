#requires -Version 5.1
<#
.SYNOPSIS
    Write an interactive 3D map of the methods and the calls between them.
.DESCRIPTION
    Refreshes the graft graph with graft build, then reads its wiring graph
    named in MapMethod.json. Every method becomes a dot and every call a line.

    Each method is placed in its project by path and in its class by the
    innermost class span of its file. A call between projects is classed by
    the layer list of MapMethod.json: a call to a named neighbour is in chain,
    a call further down skips a layer, any other call runs against the chain,
    and a call to or from a project outside the list is marked as outside.

    The data and the graph library from MapMethod.json are placed into the
    page template MapMethod.html, written as {prefix}{version}.html into the
    report folder named in MapMethod.json, and opened in the default browser.
    The page is self-contained and works offline. It reads nothing at view
    time, so it shows the code as it stood when the script ran.
.PARAMETER NoBuild
    Read the wiring graph as it stands, without running graft build first.
.PARAMETER NoOpen
    Write the page without opening it.
.PARAMETER Help
    Display this help and exit. The alias -? is supported.
.EXAMPLE
    MapMethod
    Refresh the graph, then write and open the page.
.EXAMPLE
    MapMethod -NoBuild -NoOpen
    Write the page from the current graph without opening it.
#>
# MAPMETHOD - MAP GENERATION 1.
[CmdletBinding()]
param(
    [switch]$NoBuild,
    [switch]$NoOpen,
    [Alias('?')]
    [switch]$Help
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Help) {
    Get-Help -Name $PSCommandPath -Detailed
    return
}

Write-Host 'MAPMETHOD - MAP GENERATION 1' -ForegroundColor Blue

$configPath = Join-Path $PSScriptRoot 'MapMethod.json'
$templatePath = Join-Path $PSScriptRoot 'MapMethod.html'
foreach ($path in @($configPath, $templatePath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "A required file is missing: $path" }
}
$config = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$graphPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ([string]$config.graph)))
$libraryPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ([string]$config.library)))
if (-not (Test-Path -LiteralPath $libraryPath -PathType Leaf)) { throw "The graph library is missing: $libraryPath" }

if (-not $NoBuild) {
    if ($null -eq (Get-Command -Name graft -ErrorAction SilentlyContinue)) {
        throw 'graft is not installed, so the graph cannot be refreshed. Run with -NoBuild to use the graph as it stands.'
    }
    Write-Host 'Refreshing the graft graph...'
    Push-Location -LiteralPath $repositoryRoot
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $buildOutput = @(& graft build 2>&1)
        $buildExit = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
        Pop-Location
    }
    if ($buildExit -ne 0) {
        $buildOutput | Select-Object -Last 20 | ForEach-Object { Write-Host ([string]$_) }
        throw "graft build failed with exit code $buildExit."
    }
}
if (-not (Test-Path -LiteralPath $graphPath -PathType Leaf)) { throw "The wiring graph is missing: $graphPath. Run graft build first." }

$projectRoots = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($root in @($config.projectRoots)) { [void]$projectRoots.Add([string]$root) }

# Layer entries by project; names is the set of short names the project may call, or null for any.
$layers = @{}
foreach ($entry in @($config.layers)) {
    $names = $null
    if (-not ($entry.names -is [string] -and $entry.names -eq '*')) {
        $names = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        foreach ($name in @($entry.names)) { [void]$names.Add([string]$name) }
    }
    $layers[[string]$entry.project] = [pscustomobject]@{ Short = [string]$entry.short; Rank = [double]$entry.rank; Names = $names }
}

function Get-ProjectName {
    param([Parameter(Mandatory = $true)][string]$Path)

    $parts = $Path.Split('/')
    if ($parts.Count -gt 2 -and $projectRoots.Contains($parts[0])) { return $parts[1] }
    if ($parts.Count -gt 1) { return $parts[0] + '/' + $parts[1] }
    return $parts[0]
}

function Get-SpanStart {
    param([AllowNull()][string]$Span)

    if ([string]::IsNullOrEmpty($Span)) { return 0 }
    $match = [System.Text.RegularExpressions.Regex]::Match($Span, '^L(\d+)(?:-L(\d+))?$')
    if (-not $match.Success) { return 0 }
    return [int]$match.Groups[1].Value
}

function Get-SpanEnd {
    param([AllowNull()][string]$Span)

    $match = [System.Text.RegularExpressions.Regex]::Match([string]$Span, '^L(\d+)(?:-L(\d+))?$')
    if (-not $match.Success) { return 0 }
    if ($match.Groups[2].Success) { return [int]$match.Groups[2].Value }
    return [int]$match.Groups[1].Value
}

function ConvertTo-JsonText {
    param([AllowNull()][string]$Value)

    if ($null -eq $Value) { return 'null' }
    if (-not [System.Text.RegularExpressions.Regex]::IsMatch($Value, '["\\\u0000-\u001F]')) { return '"' + $Value + '"' }
    $builder = [System.Text.StringBuilder]::new($Value.Length + 2)
    [void]$builder.Append('"')
    foreach ($char in $Value.ToCharArray()) {
        switch ($char) {
            '"' { [void]$builder.Append('\"') }
            '\' { [void]$builder.Append('\\') }
            "`n" { [void]$builder.Append('\n') }
            "`r" { [void]$builder.Append('\r') }
            "`t" { [void]$builder.Append('\t') }
            default {
                if ([int]$char -lt 0x20) { [void]$builder.Append(('\u{0:x4}' -f [int]$char)) }
                else { [void]$builder.Append($char) }
            }
        }
    }
    [void]$builder.Append('"')
    return $builder.ToString()
}

Write-Host "Reading $graphPath..."
$graph = Get-Content -LiteralPath $graphPath -Raw -Encoding UTF8 | ConvertFrom-Json

# Class and interface spans per file, to find the innermost owner of each method.
$classSpans = @{}
foreach ($node in $graph.nodes) {
    if ($node.kind -ne 'class' -and $node.kind -ne 'interface') { continue }
    if (-not $classSpans.ContainsKey($node.path)) { $classSpans[$node.path] = [System.Collections.Generic.List[object]]::new() }
    $classSpans[$node.path].Add([pscustomobject]@{ Start = (Get-SpanStart $node.span); End = (Get-SpanEnd $node.span); Name = [string]$node.name })
}

$projects = [System.Collections.Generic.List[object]]::new()
$projectIndex = @{}
$classes = [System.Collections.Generic.List[object]]::new()
$classIndex = @{}
$methods = [System.Collections.Generic.List[object]]::new()
$methodIndex = [System.Collections.Generic.Dictionary[string, int]]::new([System.StringComparer]::Ordinal)

foreach ($node in $graph.nodes) {
    if ($node.kind -ne 'method') { continue }
    $path = [string]$node.path
    $project = Get-ProjectName -Path $path
    if (-not $projectIndex.ContainsKey($project)) {
        $projectIndex[$project] = $projects.Count
        $layer = $layers[$project]
        $projects.Add([pscustomobject]@{
                Id    = $project
                Short = $(if ($null -ne $layer) { $layer.Short } else { $project -replace '^Cadroue\.', '' })
                Rank  = $(if ($null -ne $layer) { $layer.Rank } else { 0 })
                Src   = ($null -ne $layer)
            })
    }
    $line = Get-SpanStart $node.span
    $owner = $null
    if ($classSpans.ContainsKey($path)) {
        foreach ($span in $classSpans[$path]) {
            if ($span.Start -le $line -and $line -le $span.End -and ($null -eq $owner -or $span.Start -ge $owner.Start)) { $owner = $span }
        }
    }
    $className = $(if ($null -ne $owner) { $owner.Name } else { [System.IO.Path]::GetFileNameWithoutExtension($path) })
    $classKey = $project + '|' + $className
    if (-not $classIndex.ContainsKey($classKey)) {
        $classIndex[$classKey] = $classes.Count
        $classes.Add([pscustomobject]@{ Name = $className; Project = $projectIndex[$project] })
    }
    $methodIndex[[string]$node.id] = $methods.Count
    $signature = $(if ($null -ne $node.signature) { [string]$node.signature } else { '' })
    $methods.Add([pscustomobject]@{
            Name = [string]$node.name; Project = $projectIndex[$project]; Class = $classIndex[$classKey]
            Path = $path; Line = $line; Signature = $signature
        })
}

# 0 same project, 1 in chain, 2 skips a layer, 3 against the chain, 4 outside the layer list.
function Get-CallKind {
    param([int]$Source, [int]$Target)

    if ($Source -eq $Target) { return 0 }
    $from = $projects[$Source]
    $to = $projects[$Target]
    if (-not ($from.Src -and $to.Src)) { return 4 }
    $names = $layers[$from.Id].Names
    if ($null -eq $names -or $names.Contains($to.Short)) { return 1 }
    if ($to.Rank -lt $from.Rank) { return 2 }
    return 3
}

$edges = [System.Collections.Generic.List[string]]::new()
$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$kindCounts = @(0, 0, 0, 0, 0)
foreach ($edge in $graph.edges) {
    $source = 0
    $target = 0
    if (-not $methodIndex.TryGetValue([string]$edge.source, [ref]$source)) { continue }
    if (-not $methodIndex.TryGetValue([string]$edge.target, [ref]$target)) { continue }
    if ($source -eq $target -or -not $seen.Add("$source>$target")) { continue }
    $kind = Get-CallKind -Source $methods[$source].Project -Target $methods[$target].Project
    $kindCounts[$kind]++
    $inferred = $(if ($edge.confidence -eq 'inferred') { 1 } else { 0 })
    $edges.Add("[$source,$target,$inferred,$kind]")
}

$builder = [System.Text.StringBuilder]::new()
[void]$builder.Append('{"projects":[')
for ($i = 0; $i -lt $projects.Count; $i++) {
    $p = $projects[$i]
    if ($i -gt 0) { [void]$builder.Append(',') }
    [void]$builder.Append(('{{"id":{0},"short":{1},"rank":{2},"src":{3}}}' -f (ConvertTo-JsonText $p.Id), (ConvertTo-JsonText $p.Short),
            $p.Rank.ToString([System.Globalization.CultureInfo]::InvariantCulture), $p.Src.ToString().ToLowerInvariant()))
}
[void]$builder.Append('],"classes":[')
for ($i = 0; $i -lt $classes.Count; $i++) {
    if ($i -gt 0) { [void]$builder.Append(',') }
    [void]$builder.Append('[' + (ConvertTo-JsonText $classes[$i].Name) + ',' + $classes[$i].Project + ']')
}
[void]$builder.Append('],"methods":[')
for ($i = 0; $i -lt $methods.Count; $i++) {
    $m = $methods[$i]
    if ($i -gt 0) { [void]$builder.Append(',') }
    [void]$builder.Append('[' + (ConvertTo-JsonText $m.Name) + ',' + $m.Project + ',' + $m.Class + ',' + (ConvertTo-JsonText $m.Path) + ',' + $m.Line + ',' + (ConvertTo-JsonText $m.Signature) + ']')
}
[void]$builder.Append('],"edges":[')
[void]$builder.Append(($edges -join ','))
[void]$builder.Append(']}')

$utf8 = [System.Text.UTF8Encoding]::new($false)
$template = [System.IO.File]::ReadAllText($templatePath, $utf8)
foreach ($marker in @('/*__DATA__*/', '/*__LIBRARY__*/', '__TITLE__', '__STAMP__')) {
    if (-not $template.Contains($marker)) { throw "The template lacks the marker $marker : $templatePath" }
}
$library = [System.IO.File]::ReadAllText($libraryPath, $utf8)
$versionPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ([string]$config.report.versionFile)))
$version = [string](Get-Content -LiteralPath $versionPath -Raw -Encoding UTF8 | ConvertFrom-Json).([string]$config.report.versionKey)
if ([string]::IsNullOrWhiteSpace($version)) { throw "The version file lacks the key $($config.report.versionKey): $versionPath" }
$stamp = $version + ' - ' + [DateTimeOffset]::Now.ToString('yyyy-MM-dd HH:mm', [System.Globalization.CultureInfo]::InvariantCulture)
$pageTitle = Split-Path -Leaf $repositoryRoot

# Replace the library last, so no marker text inside it is ever replaced.
$pageText = $template.Replace('__TITLE__', [System.Net.WebUtility]::HtmlEncode($pageTitle)).Replace('__STAMP__', [System.Net.WebUtility]::HtmlEncode($stamp))
$pageText = $pageText.Replace('/*__DATA__*/', $builder.ToString().Replace('</', '<\/'))
$pageText = $pageText.Replace('/*__LIBRARY__*/', $library.Replace('</script', '<\/script'))

$visualDirectory = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ([string]$config.report.directory)))
$visualPath = Join-Path $visualDirectory ([string]$config.report.prefix + $version + '.html')
[void][System.IO.Directory]::CreateDirectory($visualDirectory)
[System.IO.File]::WriteAllText($visualPath, $pageText, $utf8)

Write-Host ("Methods: {0}. Classes: {1}. Calls: {2}." -f $methods.Count, $classes.Count, $edges.Count)
Write-Host ("Calls by kind: same project {0}, in chain {1}, skips a layer {2}, against the chain {3}, outside {4}." -f $kindCounts[0], $kindCounts[1], $kindCounts[2], $kindCounts[3], $kindCounts[4])
Write-Host "Page: $visualPath"
if (-not $NoOpen) {
    Start-Process -FilePath $visualPath
}
