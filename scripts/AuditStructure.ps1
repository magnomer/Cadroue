<#
.SYNOPSIS
Audits the rings of the source and the project edges, and reports every step past a neighbour.

.DESCRIPTION
A ring may lean on the one neighbour the ring table names for it and on the data of any ring
inside it, and on nothing else. This audit reads the ring table from AuditStructure.json, binds
the tracked source with Roslyn, and performs these actions:

  1. Sorts every symbol one ring names from another ring into a kind of four families:
       Border   a ring may name the behaviour of its one neighbour (Commuting) and the data
                of any inner ring at any depth (Ferrying). Behaviour past the neighbour is
                Leapfrogging and any outward name is Trespassing. A using of a deeper ring
                counts as data, and a method call on a deeper record counts as behaviour.
                Undercutting, Leaking, Unsealing and Poaching guard the UI cut and the offers.
                Drifting holds every offer list to its neighbour: each entry resolves to one
                public type of the neighbour ring carrying an offerPrefixes prefix, a cut
                pair offers every public type of its neighbour, and pairs sharing a
                neighbour offer the same list. Lingering holds every Conduct += of an event
                declared below Conduct to a -= of the same event and handler in the same
                type, and counts a lambda handler always, since it cannot be removed.
       Purity   a pure ring names only the framework namespaces its frame lists (Foraging)
                and never touches an eavesdropping member such as the clock, the environment
                or the file system (Eavesdropping).
       Census   each ring holds its floor of files (Hollowing), its own types (Squatting)
                and no smuggled word (Smuggling).
       Charter  the project files under the source folder hold the ring table edge by edge
                (Rerouting), no project hides a reference from the table (Backdooring), and
                the cut projects that still compile transitively stay at a ceiling
                (Piggybacking).
  2. Clears every Border or Purity hit an exemption row in exempt.border or exempt.purity
     matches. A row is 'path:Name' or 'folder/*:Name', matched ordinally.
  3. Counts the hits of every held kind per pair Kind:Ring>Target against the ceiling written
     for it, an unwritten ceiling being zero. A pair above its ceiling fails, and a ceiling
     above its count is stale and fails too, so a ceiling only walks down. A kind that allows
     none fails on its first hit, and an exemption row that cleared nothing fails as stale.
  4. Prints the Result table, the findings by kind, the pairs and the first console.top rows
     of every gate above zero.
  5. Writes a Markdown report with every finding to {report.directory}\{prefix}{version}.md,
     and the same result as a page beside it as {report.directory}\{prefix}{version}.html,
     filled from the template AuditStructure.html. The page opens in the default browser
     unless -NoOpen is given.

A configuration that gives a ring more than one neighbour, or whose cut differs from its shell
folders, is refused before the audit runs. A ring may also name one capsule: a leaf ring with no
neighbour that no other ring names.

Binding goes through the shared binder of AuditBinder.cs and AuditBinder.json: the tracked
sources, the generated code of every project and the host build output, with no compile error
allowed. Build the solution first so the generated markup classes and the package assemblies exist.
The helper targets helper.framework and is compiled once per text and SDK into the temp folder.

The script reads no convention test file and no test report. The convention tests TAuditBorder,
TAuditCensus, TAuditPurity and TAuditCharter audit the same ground truth from their own settings,
so either side still tells the truth when the other is broken.

Everything project-specific lives in AuditStructure.json next to this script. No project source
is modified. Git and the .NET SDK are required.

AuditStructure.json shape:
  {
    "generation": 21,
    "project": "Cadroue",
    "helper": { "framework": "net10.0" },
    "source": "src/",
    "rings": [ { "name": "Cadroue.Core", "neighbour": [], "capsule": [], "frame": [ "System" ], "pure": true, "cut": false } ],
    "host": { "name": "Cadroue.Host", "references": [ "Cadroue.Core" ] },
    "shells": [ "src/Cadroue.UIVeneer" ],
    "imports": [ "Directory.Build.props" ],
    "eavesdroppingMembers": [ "System.DateTime.Now" ],
    "ceilings": { "Leapfrogging:Cadroue.ShellEngine>Cadroue.Core": 15, "Piggybacking": 4 },
    "offers": { "Cadroue.Conduct>Cadroue.ShellEngine": [ "LEntryPort" ] },
    "offerPrefixes": { "Cadroue.ShellEngine": [ "L" ] },
    "unsealingPrefixes": { "Cadroue.UIDeportment": [ "L" ] },
    "hollowingFloors": { "Cadroue.Application": 91 },
    "squattingPatterns": { "Cadroue.Core": [ "^LRequest" ] },
    "smugglingWords": { "src/Cadroue.ShellEngine": [ "LVaultSessionStart" ] },
    "exempt": { "border": [], "purity": [] },
    "console": { "top": 50 },
    "report": { "directory": "docs-analysis", "versionFile": "version.json", "versionKey": "current-version", "prefix": "AuditStructure-" }
  }

A missing key is an error, never a default, and an unknown key is an error rather than a silent
no-op.

.PARAMETER Root
Project root to audit. Defaults to the directory containing this script's parent.

.PARAMETER ReportDirectory
Overrides report.directory for this run, for the Markdown report and the page. Relative paths
resolve against the project root.

.PARAMETER Top
Overrides console.top: how many rows of each list the console shows.

.PARAMETER Open
Open the Markdown report after the audit finishes.

.PARAMETER NoOpen
Write the page without opening it.

.PARAMETER NoPause
Do not stop at each console page. Paging is also off when output or input is redirected.

.PARAMETER Help
Display this help and exit. The alias -? is supported.

.EXAMPLE
AuditStructure
Audit the current checkout.

.EXAMPLE
AuditStructure -ReportDirectory D:\temp\audit -Top 10 -Open -NoOpen
Write the reports elsewhere, show ten rows of each list, open the Markdown report but not the page.
#>
#requires -Version 5.1
# AUDITSTRUCTURE - AUDIT GENERATION 21.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the audited outcome changes.
# A generation names the set of checks the audit applies. Two projects on the same generation audit
# the same things and their reports compare directly, whatever else differs between the files. A check
# added, removed, or changed in what it reports is a new generation; wording, plumbing, and refactoring
# leave it alone. Every audit script shares one generation number with the convention-test settings,
# and each refuses a configuration written at another generation.
# Generation 10 binds the source with Roslyn and holds every ring to one neighbour: a ring names the
# behaviour of the single neighbour the table gives it (Commuting), carries the data of any inner
# ring (Ferrying), and names nothing outward (Trespassing). A pure ring names only the framework
# namespaces its frame lists (Foraging) and never touches an eavesdropping member (Eavesdropping).
# Behaviour past the neighbour (Leapfrogging) is a call, a construction, an implementation, a field
# or a base. Ferried data is a record, an enum, a struct or a delegate named in a signature or
# passed through. The distinction is the kind of the type named, read from the binder, not the
# position of the name in the line.
# A configuration is total: a missing key is an error, never a default, and an unknown key is an
# error rather than a silent no-op. A ceiling is the hit count a held pair may hold; a count above
# fails, a ceiling above the count is stale and fails too, so a ceiling only walks down. A kind
# without a ceiling fails on its first hit. An exemption row is 'path:Name' or 'folder/*:Name' and
# clears a Border or Purity hit alone; a row that matched nothing is stale and fails the run.
# Generation 11: nothing the structure audit reports changes; the number rises with the truth audit,
# which checks that a deportment field reaches no request, keeps one writer, holds no logic and
# treats no engine data.
# Generation 12: the audit reports what the convention tests TAuditBorder, TAuditCensus,
# TAuditPurity and TAuditCharter report. It binds through the shared binder of AuditBinder.cs with
# no compile error, counts a ceiling in hits, counts a using of a deeper ring and a method call on a
# deeper record as Leapfrogging, walks using static names, and adds the Leaking, Poaching,
# Squatting, Smuggling, Hollowing, Rerouting, Backdooring and Piggybacking kinds.
# Generation 13: nothing this audit reports changes; the number rises with the UI audit, which
# counts every surface markup line that hooks logic into the markup and every surface member
# that is not a constructor.
# Generation 14: nothing this audit reports changes; the number rises with the UI audit, which
# also counts command parameters, member paths and literal tags in surface markup as hooks.
# Generation 15: nothing this audit reports changes; the number rises with the name audit, which
# counts prefix turfs, and the UI audit, which counts pack URIs, scaffold types and contract IDs.
# Generation 16: the Unsealing kind counts every public member of a sealed-prefix type in a UI ring
# that names a type from below its neighbour. Each unsealingPrefixes row names a ring and the type
# prefixes it seals.
# Generation 17: findings take one vocabulary of -ing kinds and plain measure names, and the
# configuration keys follow. What the audit counts is unchanged.
# Generation 18: nothing this audit reports changes; the number rises with the UI audit, whose
# truth detector stops five false findings.
# Generation 19: nothing this audit reports changes; the number rises with the comment audit, whose
# hash line ties every comment file to the sources it describes.
# Generation 20: nothing this audit reports changes; the number rises with the UI audit, whose
# Mismatching kind only informs while a driver folder it waits on holds no source.
# Generation 21: two border kinds join. Drifting reports an offer list that no longer equals the
# public types of its neighbour. Lingering reports a Conduct subscription to an engine event with
# no matching removal.
[CmdletBinding()]
param(
    [string]$Root,
    [string]$ReportDirectory,
    [int]$Top = 0,
    [switch]$Open,
    [switch]$NoOpen,
    [switch]$NoPause,
    [Alias('?')]
    [switch]$Help
)

# Under Windows PowerShell 5.1 an advanced script evaluates a parameter default before
# $PSScriptRoot is available to it, so -Root arrives empty there while pwsh 7 resolves it.
if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Split-Path -Parent $PSScriptRoot
}

if ($Help) {
    @'
NAME
    AuditStructure.ps1

SYNOPSIS
    Report every step one ring takes past its neighbour, and every project edge off the table.

SYNTAX
    AuditStructure [-Root <path>] [-ReportDirectory <path>] [-Top <n>] [-Open] [-NoOpen] [-NoPause] [-Help]

OPTIONS
    -Root <path>             Project root. Defaults to the parent of the scripts folder.
    -ReportDirectory <path>  Overrides report.directory for the report and the page.
    -Top <n>                 Overrides console.top.
    -Open                    Open the Markdown report when done.
    -NoOpen                  Write the page without opening it.
    -NoPause                 No console paging.
    -Help                    Show this help.

KINDS
    Border (counterpart TAuditBorder):
        Trespassing, Leapfrogging, Undercutting, Leaking, Unsealing
            held by a ceiling, one per pair Kind:Ring>Target, counted in hits
        Poaching, Drifting, Lingering
                            never allowed, failing on the first hit
        Commuting, Ferrying reported only
    Census (counterpart TAuditCensus):
        Squatting, Smuggling, Hollowing
            never allowed, failing on the first hit
    Purity (counterpart TAuditPurity):
        Foraging, Eavesdropping
            held by a ceiling, one per pair Kind:Ring>Target, counted in hits
    Charter (counterpart TAuditCharter):
        Rerouting, Backdooring
            never allowed, failing on the first hit
        Piggybacking        held by the single ceiling Piggybacking, counted
                            in cut projects

RESULT
    Above ceiling     Pairs with more hits than their ceiling.
    Stale ceilings    Pairs with fewer hits than their ceiling.
    Stale exemptions  Exemption rows that cleared no finding.
    Poaching, Drifting, Lingering, Squatting, Smuggling, Hollowing,
    Rerouting, Backdooring
                      Hits of a kind that allows none.

REPORTS
    {report.directory}\{prefix}{version}.md    every finding, pair and exemption.
    {report.directory}\{prefix}{version}.html  the same result as a page, opened unless -NoOpen.

EXIT CODES
    0   Every Result gate is at zero.
    1   A Result gate is above zero.
'@ | Write-Host
    exit 0
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:AuditGeneration = 21

# dotnet and git write UTF-8. A console still on the OEM code page would show every non-ASCII line
# garbled, so this process reads and writes UTF-8. Process-local: the calling console keeps its own.
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding

$script:ConfigDocument = 'AuditStructure.json'
$script:BinderSource = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'AuditBinder.cs'))
$script:PathSeparators = [char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
$script:ItemLimit = 0

# Console paging. A page is one window of rows; the audit stops at each page boundary and waits
# for a key so the reader can inspect the output before it scrolls away. Any key shows the next
# page, Q shows the rest without stopping. Paging is off when -NoPause is given or when either
# stream is redirected, so a pipeline or a log file never blocks on a key.
$script:PageLimit = 0
$script:PageWidth = 0
$script:PageCount = 0
if (-not $NoPause) {
    try {
        if (-not [Console]::IsOutputRedirected -and -not [Console]::IsInputRedirected) {
            $script:PageLimit = [Math]::Max(0, $Host.UI.RawUI.WindowSize.Height - 2)
            $script:PageWidth = [Math]::Max(1, $Host.UI.RawUI.WindowSize.Width)
        }
    }
    catch {
        $script:PageLimit = 0
    }
}

function Get-OrdinalKey {
    # Sort-Object compares text by culture, which Windows PowerShell 5.1 and pwsh 7 order differently.
    # Uppercase hexadecimal UTF-16 code units compare alike under every culture, so this key sorts ordinally.
    param([string]$Text)
    return [System.BitConverter]::ToString([System.Text.Encoding]::BigEndianUnicode.GetBytes($Text)).Replace('-', '')
}

function Get-OrdinalSorted {
    param([string[]]$Items)

    $copy = [string[]]@($Items)
    [System.Array]::Sort($copy, [System.StringComparer]::Ordinal)
    return , $copy
}

function Write-AuditLine {
    param(
        [Parameter(Position = 0)][AllowEmptyString()][string]$Text = '',
        [ConsoleColor]$ForegroundColor,
        [string]$Lead = '',
        [ConsoleColor]$LeadColor = [ConsoleColor]::Gray
    )

    if ($script:PageLimit -gt 0) {
        $rows = [Math]::Max(1, [Math]::Ceiling(($Lead.Length + $Text.Length) / [double]$script:PageWidth))
        if ($script:PageCount + $rows -gt $script:PageLimit -and $script:PageCount -gt 0) {
            $prompt = '-- More -- (any key: next page, Q: no more pauses)'
            Write-Host $prompt -ForegroundColor Yellow -NoNewline
            $key = [Console]::ReadKey($true)
            Write-Host ("`r" + (' ' * $prompt.Length) + "`r") -NoNewline
            if ($key.Key -eq [ConsoleKey]::Q) {
                $script:PageLimit = 0
            }
            $script:PageCount = 0
        }
        $script:PageCount += $rows
    }

    if ($Lead.Length -gt 0) {
        Write-Host $Lead -ForegroundColor $LeadColor -NoNewline
    }
    if ($PSBoundParameters.ContainsKey('ForegroundColor')) {
        Write-Host $Text -ForegroundColor $ForegroundColor
    }
    else {
        Write-Host $Text
    }
}

function Write-AuditSection {
    param([string]$Title)

    Write-AuditLine ''
    Write-AuditLine $Title -ForegroundColor Blue
    Write-AuditLine ('-' * $Title.Length) -ForegroundColor DarkGray
}

function Write-AuditResult {
    param([object[]]$Rows)

    Write-AuditSection -Title 'Result'
    $statusWidth = 6
    $countWidth = [Math]::Max(5, ($Rows | ForEach-Object { $_.Count.ToString('N0').Length } | Measure-Object -Maximum).Maximum)
    $gateWidth = [Math]::Max(4, ($Rows | ForEach-Object { $_.Gate.Length } | Measure-Object -Maximum).Maximum)
    $meaningWidth = [Math]::Max(7, ($Rows | ForEach-Object { $_.Meaning.Length } | Measure-Object -Maximum).Maximum)
    Write-AuditLine ("{0}  {1}  {2}  Meaning" -f 'Status'.PadRight($statusWidth), 'Count'.PadLeft($countWidth), 'Gate'.PadRight($gateWidth)) -ForegroundColor Cyan
    Write-AuditLine (@(('-' * $statusWidth), ('-' * $countWidth), ('-' * $gateWidth), ('-' * $meaningWidth)) -join '  ') -ForegroundColor Cyan
    foreach ($row in $Rows) {
        $failing = $row.Count -gt 0
        $status = if ($failing) { 'FAIL' } else { 'OK' }
        $text = "{0}  {1}  {2}  {3}" -f ''.PadRight($statusWidth - $status.Length), $row.Count.ToString('N0').PadLeft($countWidth), $row.Gate.PadRight($gateWidth), $row.Meaning
        Write-AuditLine $text -Lead $status -LeadColor $(if ($failing) { 'Red' } else { 'Green' })
    }

    $failed = @($Rows | Where-Object { $_.Count -gt 0 })
    Write-AuditLine ''
    if ($failed.Count -eq 0) {
        Write-AuditLine ("PASS: all {0} gates at 0." -f $Rows.Count) -ForegroundColor Green
    }
    else {
        $sections = ($failed | ForEach-Object { '"' + $_.Section + '"' }) -join ', '
        Write-AuditLine ("FAIL: {0} of {1} gates above 0. See {2}." -f $failed.Count, $Rows.Count, $sections) -ForegroundColor Red
    }
}

function Write-AuditTable {
    param([string[]]$Header, [object[]]$Rows)

    $widths = @(for ($column = 0; $column -lt $Header.Count; $column++) {
        $cells = @($Header[$column]) + @($Rows | ForEach-Object { [string]$_[$column] })
        ($cells | Measure-Object -Property Length -Maximum).Maximum
    })
    $numeric = @(for ($column = 0; $column -lt $Header.Count; $column++) {
        $Rows.Count -gt 0 -and @($Rows | Where-Object { [string]$_[$column] -notmatch '^(-|-?[\d,]+(\.\d+)?( %)?)$' }).Count -eq 0
    })
    $format = {
        param([string[]]$Cells)
        $parts = for ($column = 0; $column -lt $Cells.Count; $column++) {
            if ($numeric[$column]) { $Cells[$column].PadLeft($widths[$column]) } else { $Cells[$column].PadRight($widths[$column]) }
        }
        ($parts -join '  ').TrimEnd()
    }
    Write-AuditLine (& $format $Header) -ForegroundColor Cyan
    Write-AuditLine (($widths | ForEach-Object { '-' * $_ }) -join '  ') -ForegroundColor Cyan
    foreach ($row in $Rows) {
        Write-AuditLine (& $format ([string[]]$row))
    }
}

function Write-AuditItems {
    # A list section: the heading counts every row, and a long list is cut short with a pointer to the report.
    param([string]$Title, [string[]]$Lines)

    if ($Lines.Count -eq 0) {
        return
    }

    Write-AuditSection -Title "$Title ($($Lines.Count.ToString('N0')))"
    $shown = [Math]::Min($Lines.Count, $script:ItemLimit)
    for ($index = 0; $index -lt $shown; $index++) {
        Write-AuditLine $Lines[$index]
    }

    if ($Lines.Count -gt $shown) {
        Write-AuditLine ("... and {0:N0} more in the report." -f ($Lines.Count - $shown))
    }
}

Write-AuditLine "AUDITSTRUCTURE - AUDIT GENERATION $script:AuditGeneration" -ForegroundColor Blue

# Each entry is a path through the document and the kind of value that must be found there.
# 'string', 'count' for an integer of zero or more, 'string[]', 'ring[]' for the ring table,
# 'map[]' for an object whose every property is an array of strings, or 'map[int]' for an object
# whose every property is an integer of zero or more.
$script:AuditSchema = [ordered]@{
    'generation'       = 'count'
    'project'          = 'string'
    'helper.framework' = 'string'
    'source'           = 'string'
    'rings'            = 'ring[]'
    'host.name'        = 'string'
    'host.references'  = 'string[]'
    'shells'           = 'string[]'
    'imports'          = 'string[]'
    'eavesdroppingMembers' = 'string[]'
    'offers'           = 'map[]'
    'offerPrefixes'    = 'map[]'
    'unsealingPrefixes' = 'map[]'
    'squattingPatterns' = 'map[]'
    'smugglingWords'   = 'map[]'
    'hollowingFloors'  = 'map[int]'
    'ceilings'         = 'map[int]'
    'exempt.border'    = 'string[]'
    'exempt.purity'    = 'string[]'
    'console.top'      = 'count'
    'report.directory' = 'string'
    'report.versionFile' = 'string'
    'report.versionKey' = 'string'
    'report.prefix'    = 'string'
}

$script:RingSchema = [ordered]@{
    'name'  = 'string'
    'neighbour' = 'string[]'
    'capsule' = 'string[]'
    'frame' = 'string[]'
    'pure'  = 'bool'
    'cut'   = 'bool'
}

# Every kind belongs to one family, the family its counterpart convention test is named after.
# Border: what one ring names from another. Census: what each ring holds. Purity: what a pure ring
# names from the framework. Charter: what the project files reference.
$script:BorderChecks = @('Trespassing', 'Leapfrogging', 'Undercutting', 'Leaking', 'Unsealing', 'Poaching', 'Drifting', 'Lingering', 'Commuting', 'Ferrying')
$script:CensusChecks = @('Squatting', 'Smuggling', 'Hollowing')
$script:PurityChecks = @('Foraging', 'Eavesdropping')
$script:CharterChecks = @('Rerouting', 'Backdooring', 'Piggybacking')

# A held kind counts its hits per pair against a ceiling. A hard kind allows no hit at all.
# A reported kind never gates. Piggybacking counts cut projects against its own single ceiling.
$script:HeldChecks = @('Trespassing', 'Leapfrogging', 'Undercutting', 'Leaking', 'Unsealing', 'Foraging', 'Eavesdropping')
$script:HardChecks = @('Poaching', 'Drifting', 'Lingering', 'Squatting', 'Smuggling', 'Hollowing', 'Rerouting', 'Backdooring')
$script:SingleCheck = 'Piggybacking'

# The order is the order the kinds are reported in, heaviest first.
$script:CheckOrder = @('Trespassing', 'Leapfrogging', 'Undercutting', 'Leaking', 'Unsealing', 'Foraging', 'Eavesdropping',
    'Piggybacking', 'Poaching', 'Drifting', 'Lingering', 'Squatting', 'Smuggling', 'Hollowing', 'Rerouting', 'Backdooring', 'Ferrying', 'Commuting')

$script:CheckTitles = @{
    'Trespassing'   = 'Outer ring named from an inner ring'
    'Leapfrogging'  = 'Behaviour reached past the neighbour ring'
    'Undercutting'  = 'Name from below the cut inside a UI ring'
    'Leaking'       = 'Offered signature naming a type from below its neighbour'
    'Unsealing'     = 'Sealed member naming a type from below its neighbour'
    'Foraging'      = 'Framework namespace outside the frame of a pure ring'
    'Eavesdropping' = 'Eavesdropping member touched from a pure ring'
    'Piggybacking'  = 'Cut project compiling against rings past its neighbour'
    'Poaching'      = 'Neighbour name outside the offers the ring may name'
    'Drifting'      = 'Offer list differing from the public types of its neighbour'
    'Lingering'     = 'Engine event subscription its Conduct type never removes'
    'Squatting'     = 'Type declared in a ring that may not hold it'
    'Smuggling'     = 'Listed word inside a folder'
    'Hollowing'     = 'Ring holding fewer source files than its floor'
    'Rerouting'     = 'Project edge differing from the ring table'
    'Backdooring'   = 'Reference or source link bypassing the ring table'
    'Ferrying'      = 'Data carried from a deeper ring'
    'Commuting'     = 'Neighbour ring named'
}

function Get-CheckFamily {
    param([string]$Check)

    if ($script:BorderChecks -ccontains $Check) { return 'Border' }
    if ($script:CensusChecks -ccontains $Check) { return 'Census' }
    if ($script:PurityChecks -ccontains $Check) { return 'Purity' }
    if ($script:CharterChecks -ccontains $Check) { return 'Charter' }
    throw "Unknown kind: $Check"
}

function Get-CheckGate {
    param([string]$Check)

    if ($script:HeldChecks -ccontains $Check -or $Check -ceq $script:SingleCheck) { return 'held by ceiling' }
    if ($script:HardChecks -ccontains $Check) { return 'none allowed' }
    return 'reported only'
}

function Resolve-ProjectRoot {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw 'The project root is empty.'
    }

    $item = Get-Item -LiteralPath $Path -ErrorAction SilentlyContinue
    if ($null -eq $item) {
        throw "The project root was not found: $Path"
    }

    if (-not $item.PSIsContainer) {
        throw "The project root is not a directory: $Path"
    }

    return $item.FullName.TrimEnd($script:PathSeparators)
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
        if ($null -eq $node -or -not ($node -is [System.Management.Automation.PSCustomObject]) -or
            -not ($node.PSObject.Properties.Name -ccontains $segment)) {
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

function Test-StringArray {
    param($Value)

    if ($null -eq $Value -or -not ($Value -is [System.Array])) { return $false }
    foreach ($item in $Value) {
        if (-not ($item -is [string])) { return $false }
    }

    return $true
}

function Test-AuditValue {
    param(
        [Parameter(Mandatory = $true)][string]$Kind,
        $Value
    )

    switch ($Kind) {
        'string' { return $Value -is [string] -and -not [string]::IsNullOrWhiteSpace($Value) }
        'count' { return ($Value -is [int] -or $Value -is [long]) -and $Value -ge 0 }
        'bool' { return $Value -is [bool] }
        'string[]' { return Test-StringArray -Value $Value }
        'ring[]' {
            if ($null -eq $Value -or -not ($Value -is [System.Array]) -or $Value.Count -eq 0) { return $false }
            foreach ($ring in $Value) {
                if ($null -eq $ring -or -not ($ring -is [System.Management.Automation.PSCustomObject])) { return $false }
                foreach ($key in $script:RingSchema.Keys) {
                    if (-not ($ring.PSObject.Properties.Name -ccontains $key)) { return $false }
                    if (-not (Test-AuditValue -Kind $script:RingSchema[$key] -Value $ring.$key)) { return $false }
                }

                foreach ($property in $ring.PSObject.Properties) {
                    if (-not $script:RingSchema.Contains($property.Name)) { return $false }
                }
            }

            return $true
        }
        'map[]' {
            if ($null -eq $Value -or -not ($Value -is [System.Management.Automation.PSCustomObject])) { return $false }
            foreach ($property in $Value.PSObject.Properties) {
                if (-not (Test-StringArray -Value $property.Value)) { return $false }
            }

            return $true
        }
        'map[int]' {
            if ($null -eq $Value -or -not ($Value -is [System.Management.Automation.PSCustomObject])) { return $false }
            foreach ($property in $Value.PSObject.Properties) {
                if (-not ($property.Value -is [int] -or $property.Value -is [long]) -or $property.Value -lt 0) { return $false }
            }

            return $true
        }
        default { throw "Unknown schema kind: $Kind" }
    }
}

function Read-AuditConfig {
    param([Parameter(Mandatory = $true)][string]$ProjectRoot)

    $configPath = Join-Path $PSScriptRoot $script:ConfigDocument
    if (-not (Test-Path -LiteralPath $configPath -PathType Leaf)) {
        throw "The structure configuration was not found: $configPath"
    }

    try {
        $config = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The structure configuration is not valid JSON: $configPath`n$($_.Exception.Message)"
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

    $ringsNode = Get-AuditNode -Document $config -Key 'rings'
    if ($ringsNode.Found -and (Test-AuditValue -Kind 'ring[]' -Value $ringsNode.Value)) {
        $names = [string[]]@($ringsNode.Value | ForEach-Object { [string]$_.name })
        $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
        foreach ($name in $names) {
            if (-not $seen.Add($name)) {
                [void]$problems.Add("ring '$name' is declared more than once")
            }
        }

        foreach ($ring in $ringsNode.Value) {
            $neighbour = @($ring.neighbour)
            if ($neighbour.Count -gt 1) {
                [void]$problems.Add("ring '$($ring.name)' names $($neighbour -join ', ') as neighbour, more than one ring")
            }

            $capsule = @($ring.capsule)
            if ($capsule.Count -gt 1) {
                [void]$problems.Add("ring '$($ring.name)' names $($capsule -join ', '), more than one capsule")
            }

            foreach ($target in $capsule) {
                $owned = @($ringsNode.Value | Where-Object { $_.name -ceq $target })
                if ($owned.Count -eq 0) {
                    [void]$problems.Add("ring '$($ring.name)' names an undeclared capsule '$target'")
                }
                elseif (@($owned[0].neighbour).Count -gt 0 -or @($owned[0].capsule).Count -gt 0) {
                    [void]$problems.Add("capsule '$target' names a neighbour, so it is no capsule")
                }

                $holders = @($ringsNode.Value | Where-Object { @($_.capsule) -ccontains $target })
                if ($holders.Count -gt 1) {
                    [void]$problems.Add("capsule '$target' is named by more than one ring")
                }
            }

            foreach ($target in $neighbour) {
                if (-not ($names -ccontains $target)) {
                    [void]$problems.Add("ring '$($ring.name)' names an undeclared ring '$target' as neighbour")
                }

                if ($target -ceq $ring.name) {
                    [void]$problems.Add("ring '$($ring.name)' names itself as neighbour")
                }
            }

            if ($ring.pure -and @($ring.frame).Count -eq 0) {
                [void]$problems.Add("pure ring '$($ring.name)' lists no frame namespace")
            }

            if (-not $ring.pure -and @($ring.frame).Count -gt 0) {
                [void]$problems.Add("ring '$($ring.name)' is not pure, so no Foraging check reads its frame; leave it empty")
            }
        }

        $unsealingNode = Get-AuditNode -Document $config -Key 'unsealingPrefixes'
        if ($unsealingNode.Found -and (Test-AuditValue -Kind 'map[]' -Value $unsealingNode.Value)) {
            foreach ($property in $unsealingNode.Value.PSObject.Properties) {
                if (-not ($names -ccontains $property.Name)) {
                    [void]$problems.Add("unsealingPrefixes row '$($property.Name)' names an undeclared ring")
                }
            }
        }

        $hostNode = Get-AuditNode -Document $config -Key 'host.name'
        if ($hostNode.Found -and $names -ccontains [string]$hostNode.Value) {
            [void]$problems.Add("host '$($hostNode.Value)' is also declared as a ring")
        }

        $shellsNode = Get-AuditNode -Document $config -Key 'shells'
        if ($shellsNode.Found -and (Test-StringArray -Value $shellsNode.Value)) {
            $cut = Get-OrdinalSorted -Items @($ringsNode.Value | Where-Object { $_.cut } | ForEach-Object { [string]$_.name })
            $shells = Get-OrdinalSorted -Items @($shellsNode.Value | ForEach-Object {
                $folder = ([string]$_).Replace('\', '/').TrimEnd('/')
                $folder.Substring($folder.LastIndexOf('/') + 1)
            })
            if (($cut -join "`n") -cne ($shells -join "`n")) {
                [void]$problems.Add("the cut holds [$($cut -join ', ')] but the UI rings are [$($shells -join ', ')]")
            }
        }
    }

    $ceilingsNode = Get-AuditNode -Document $config -Key 'ceilings'
    if ($ceilingsNode.Found -and (Test-AuditValue -Kind 'map[int]' -Value $ceilingsNode.Value)) {
        $held = $script:HeldChecks -join '|'
        foreach ($property in $ceilingsNode.Value.PSObject.Properties) {
            if ($property.Name -cnotmatch "^($held):[^>]+>.+$" -and $property.Name -cne $script:SingleCheck) {
                [void]$problems.Add("ceiling '$($property.Name)' must be written as Kind:Ring>Target for a held kind ($($script:HeldChecks -join ', ')) or as $script:SingleCheck")
            }
        }
    }

    foreach ($list in @('border', 'purity')) {
        $exemptNode = Get-AuditNode -Document $config -Key "exempt.$list"
        if ($exemptNode.Found -and (Test-StringArray -Value $exemptNode.Value)) {
            foreach ($row in $exemptNode.Value) {
                $split = $row.LastIndexOf(':')
                if ($split -le 0 -or $split -ge $row.Length - 1) {
                    [void]$problems.Add("exemption '$row' in exempt.$list must be written as path:Name or folder/*:Name")
                }
            }
        }
    }

    if ($problems.Count -gt 0) {
        throw "The structure configuration is not valid: $configPath`n  " + ($problems -join "`n  ")
    }

    if ($config.generation -ne $script:AuditGeneration) {
        throw "The structure configuration is generation $($config.generation) but this tooling is generation $script:AuditGeneration.`nA generation names the set of checks applied, so the two must match: $configPath"
    }

    return $config
}

function Read-ProjectVersion {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)]$Config
    )

    $versionPath = Join-AuditPath -ProjectRoot $ProjectRoot -Relative $Config.report.versionFile
    if (-not (Test-Path -LiteralPath $versionPath -PathType Leaf)) {
        throw "The version file was not found: $versionPath"
    }

    try {
        $versionData = Get-Content -LiteralPath $versionPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The version file is not valid JSON: $versionPath`n$($_.Exception.Message)"
    }

    $version = [string]$versionData.($Config.report.versionKey)
    if ($version -notmatch '^\d+\.\d+\.\d+$') {
        throw "The version must contain three numeric components: '$version'"
    }

    return $version
}

function Test-ExemptRow {
    # The row format of the convention tests: 'path:Name' or 'folder/*:Name', every part ordinal.
    param(
        [Parameter(Mandatory = $true)][string]$Row,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Path,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Name
    )

    $split = $Row.LastIndexOf(':')
    $pattern = $Row.Substring(0, $split)
    if (-not [string]::Equals($Row.Substring($split + 1), $Name, [System.StringComparison]::Ordinal)) {
        return $false
    }

    if ($pattern.EndsWith('/*', [System.StringComparison]::Ordinal)) {
        return $Path.StartsWith($pattern.Substring(0, $pattern.Length - 1), [System.StringComparison]::Ordinal)
    }

    return [string]::Equals($pattern, $Path, [System.StringComparison]::Ordinal)
}

$script:HelperProject = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{TARGET_FRAMEWORK}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
    <NuGetAudit>false</NuGetAudit>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" />
  </ItemGroup>
</Project>
'@

$script:HelperProgram = @'
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length != 4)
{
    Console.Error.WriteLine("usage: <config> <root> <binder> <output>");
    return 2;
}

string configPath = args[0];
string projectRoot = args[1];
string binderPath = args[2];
string outputPath = args[3];

JsonElement config = JsonDocument.Parse(File.ReadAllText(configPath)).RootElement;
string project = config.GetProperty("project").GetString()!;
string source = config.GetProperty("source").GetString()!.Replace('\\', '/').Trim('/') + "/";
List<Ring> rings = config.GetProperty("rings").EnumerateArray()
    .Select(ring => new Ring(
        ring.GetProperty("name").GetString()!,
        source + ring.GetProperty("name").GetString()! + "/",
        Strings(ring.GetProperty("neighbour")),
        Strings(ring.GetProperty("capsule")),
        Strings(ring.GetProperty("frame")),
        ring.GetProperty("pure").GetBoolean(),
        ring.GetProperty("cut").GetBoolean()))
    .ToList();
string hostName = config.GetProperty("host").GetProperty("name").GetString()!;
string[] hostReferences = Strings(config.GetProperty("host").GetProperty("references"));
string[] imports = Strings(config.GetProperty("imports"));
string[] eavesdropping = Strings(config.GetProperty("eavesdroppingMembers"));
Dictionary<string, string[]> offers = Map(config.GetProperty("offers"));
Dictionary<string, string[]> offerPrefixes = Map(config.GetProperty("offerPrefixes"));
Dictionary<string, string[]> unsealing = Map(config.GetProperty("unsealingPrefixes"));
Dictionary<string, string[]> squatting = Map(config.GetProperty("squattingPatterns"));
Dictionary<string, string[]> smuggling = Map(config.GetProperty("smugglingWords"));
Dictionary<string, int> floors = config.GetProperty("hollowingFloors").EnumerateObject()
    .ToDictionary(item => item.Name, item => item.Value.GetInt32(), StringComparer.Ordinal);
string[] itemNames = ["ProjectReference", "Reference", "Compile"];

Dictionary<string, HashSet<string>> inner = new(StringComparer.Ordinal);
foreach (Ring ring in rings)
{
    HashSet<string> seen = new(StringComparer.Ordinal);
    Queue<string> pending = new(ring.Neighbour);
    while (pending.Count > 0)
    {
        string name = pending.Dequeue();
        if (!seen.Add(name))
        {
            continue;
        }

        foreach (string deeper in rings.FirstOrDefault(candidate => candidate.Name == name)?.Neighbour ?? [])
        {
            pending.Enqueue(deeper);
        }
    }

    inner[ring.Name] = seen;
}

LAuditBinder binder = LAuditBinder.LAuditBinderRead(projectRoot, binderPath);
CSharpCompilation compilation = binder.LAuditCompilation;

ConcurrentDictionary<string, Ring?> ringOfPath = new(StringComparer.Ordinal);
Ring? RingOf(string relative) => ringOfPath.GetOrAdd(relative, key => rings
    .FirstOrDefault(candidate => key.StartsWith(candidate.Path, StringComparison.OrdinalIgnoreCase)));

Ring? SpaceRing(string space) => rings
    .Where(candidate => space == candidate.Name || space.StartsWith(candidate.Name + ".", StringComparison.Ordinal))
    .MaxBy(candidate => candidate.Name.Length);

Ring? SourceRing(INamedTypeSymbol type)
{
    Location? location = type.Locations.FirstOrDefault(place => place.IsInSource);
    return location?.SourceTree is null ? null : RingOf(binder.LAuditRelativeRead(location.SourceTree.FilePath));
}

static bool IsData(INamedTypeSymbol type) =>
    type.TypeKind is TypeKind.Enum or TypeKind.Struct or TypeKind.Delegate || type.IsRecord;

static bool IsPublic(INamedTypeSymbol type)
{
    for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
    {
        if (current.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }
    }

    return true;
}

static INamedTypeSymbol? TypeOf(ISymbol symbol) => symbol switch
{
    IAliasSymbol alias => TypeOf(alias.Target),
    INamedTypeSymbol named => named.IsTupleType || named.IsAnonymousType ? null : named.OriginalDefinition,
    IArrayTypeSymbol array => TypeOf(array.ElementType),
    INamespaceSymbol or ITypeParameterSymbol or ILocalSymbol or IParameterSymbol => null,
    IRangeVariableSymbol or IDiscardSymbol or ILabelSymbol or IPreprocessingSymbol => null,
    _ => symbol.ContainingType?.OriginalDefinition,
};

static bool IsHeader(SimpleNameSyntax name)
{
    SyntaxNode? parent = name.Parent;
    if (parent is QualifiedNameSyntax qualified)
    {
        parent = qualified.Parent;
    }

    return parent is UsingDirectiveSyntax or NamespaceDeclarationSyntax or FileScopedNamespaceDeclarationSyntax;
}

static string NamespaceOf(INamespaceSymbol? space) =>
    space is null || space.IsGlobalNamespace ? "" : space.ToDisplayString();

string LineText(SyntaxTree tree, int line) => line <= 0 ? "" : tree.GetText().Lines[line - 1].ToString().Trim();

int outside = 0;
ConcurrentBag<List<Finding>> bags = [];
Parallel.ForEach(binder.LAuditTrees, tree =>
{
    string relative = binder.LAuditRelativeRead(tree.FilePath);
    Ring? ring = RingOf(relative);
    List<Finding> findings = [];

    foreach ((string folder, string[] words) in smuggling)
    {
        if (!relative.StartsWith(folder.Trim('/') + "/", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        Microsoft.CodeAnalysis.Text.TextLineCollection lines = tree.GetText().Lines;
        for (int index = 0; index < lines.Count; index++)
        {
            string text = lines[index].ToString();
            foreach (string word in words.Where(word => text.Contains(word, StringComparison.Ordinal)))
            {
                findings.Add(new Finding(relative, index + 1, ring?.Name ?? "", "Smuggling", folder, word, text.Trim()));
            }
        }
    }

    if (ring is null)
    {
        Interlocked.Increment(ref outside);
        bags.Add(findings);
        return;
    }

    SemanticModel model = compilation.GetSemanticModel(tree, true);
    HashSet<string> taken = new(StringComparer.Ordinal);

    void Add(int line, string check, string target, string name)
    {
        if (taken.Add($"{line}|{check}|{target}|{name}"))
        {
            findings.Add(new Finding(relative, line, ring.Name, check, target, name, LineText(tree, line)));
        }
    }

    void Border(int line, Ring target, string name, bool data)
    {
        if (target.Name == ring.Name)
        {
            return;
        }

        string check = ring.Neighbour.Contains(target.Name) || ring.Capsule.Contains(target.Name) ? "Commuting"
            : !inner[ring.Name].Contains(target.Name) ? "Trespassing"
            : ring.Cut && !target.Cut ? "Undercutting"
            : data ? "Ferrying"
            : "Leapfrogging";
        Add(line, check, target.Name, name);
    }

    SyntaxNode rootNode = tree.GetRoot();
    foreach (UsingDirectiveSyntax directive in rootNode.DescendantNodes().OfType<UsingDirectiveSyntax>())
    {
        int line = directive.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        if (ring.Pure && directive.Name is not null)
        {
            string space = directive.Name.ToString();
            if (!space.StartsWith(project + ".", StringComparison.Ordinal) && !ring.Frame.Contains(space))
            {
                Add(line, "Foraging", space, space);
            }
        }

        if (directive.NamespaceOrType is { } named
            && model.GetSymbolInfo(named).Symbol is INamespaceSymbol spaceSymbol
            && SpaceRing(spaceSymbol.ToDisplayString()) is { } target
            && !ring.Neighbour.Contains(target.Name) && !ring.Capsule.Contains(target.Name))
        {
            Border(line, target, "using " + spaceSymbol.ToDisplayString(), true);
        }
    }

    foreach (SimpleNameSyntax name in rootNode.DescendantNodes().OfType<SimpleNameSyntax>())
    {
        bool header = IsHeader(name);
        bool held = name.FirstAncestorOrSelf<UsingDirectiveSyntax>()?.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) == true;
        if (header && !held)
        {
            continue;
        }

        SymbolInfo info = model.GetSymbolInfo(name);
        ISymbol? symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
        if (symbol is null || TypeOf(symbol) is not INamedTypeSymbol type)
        {
            continue;
        }

        int line = name.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        Location? location = type.Locations.FirstOrDefault(place => place.IsInSource);
        if (location is null)
        {
            if (header || !ring.Pure)
            {
                continue;
            }

            string space = NamespaceOf(type.ContainingNamespace);
            if (space.Length > 0 && !ring.Frame.Contains(space))
            {
                Add(line, "Foraging", space, space);
            }

            string full = space.Length > 0 ? space + "." + type.Name : type.Name;
            if (symbol is not ITypeSymbol and not IAliasSymbol and not IMethodSymbol { MethodKind: MethodKind.Constructor })
            {
                full += "." + symbol.Name;
            }

            string? pattern = eavesdropping.FirstOrDefault(
                candidate => full == candidate || full.StartsWith(candidate + ".", StringComparison.Ordinal));
            if (pattern is not null)
            {
                Add(line, "Eavesdropping", pattern, pattern);
            }

            continue;
        }

        Ring? targetRing = RingOf(binder.LAuditRelativeRead(location.SourceTree!.FilePath));
        if (targetRing is null)
        {
            continue;
        }

        bool behaviour = symbol is IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.ReducedExtension };
        Border(line, targetRing, type.Name, IsData(type) && !behaviour);
    }

    foreach (BaseTypeDeclarationSyntax declaration in rootNode.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
    {
        string declared = declaration.Identifier.Text;
        int line = declaration.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        foreach (string pattern in squatting.GetValueOrDefault(ring.Name, []).Where(pattern => Regex.IsMatch(declared, pattern)))
        {
            findings.Add(new Finding(relative, line, ring.Name, "Squatting", pattern, declared, LineText(tree, line)));
        }
    }

    bags.Add(findings);
});

List<Finding> all = bags.SelectMany(bag => bag).ToList();

HashSet<string> leaked = new(StringComparer.Ordinal);
foreach ((string pair, string[] offer) in offers)
{
    string ringName = pair[..pair.IndexOf('>')];
    string neighbour = pair[(pair.IndexOf('>') + 1)..];
    if (rings.FirstOrDefault(candidate => candidate.Name == ringName) is not { Cut: true })
    {
        continue;
    }

    IEnumerable<ISymbol> members = offer
        .SelectMany(name => compilation.GetSymbolsWithName(name, SymbolFilter.Type))
        .OfType<INamedTypeSymbol>()
        .Where(type => SourceRing(type)?.Name == neighbour)
        .SelectMany(PublicOf)
        .Where(member => member.DeclaredAccessibility == Accessibility.Public && !member.IsImplicitlyDeclared);
    foreach (ISymbol member in members)
    {
        Location? location = member.Locations.FirstOrDefault(place => place.IsInSource);
        if (location is null)
        {
            continue;
        }

        string relative = binder.LAuditRelativeRead(location.SourceTree!.FilePath);
        int line = location.GetLineSpan().StartLinePosition.Line + 1;
        foreach (INamedTypeSymbol type in SignatureOf(member))
        {
            if (SourceRing(type) is { } target && inner[neighbour].Contains(target.Name)
                && leaked.Add($"{relative}|{line}|{ringName}|{target.Name}|{type.Name}"))
            {
                all.Add(new Finding(relative, line, ringName, "Leaking", target.Name, type.Name,
                    LineText(location.SourceTree, line)));
            }
        }
    }
}

Dictionary<string, List<(string? Ring, bool Public)>> typesOf = compilation
    .GetSymbolsWithName(_ => true, SymbolFilter.Type)
    .OfType<INamedTypeSymbol>()
    .GroupBy(type => type.Name, StringComparer.Ordinal)
    .ToDictionary(
        group => group.Key,
        group => group.Select(type => (SourceRing(type)?.Name, IsPublic(type))).ToList(),
        StringComparer.Ordinal);
foreach ((string pair, string[] offer) in offers)
{
    string ringName = pair[..pair.IndexOf('>')];
    string neighbour = pair[(pair.IndexOf('>') + 1)..];
    string where = source + neighbour;
    string[] allowed = offerPrefixes.GetValueOrDefault(neighbour, []);
    HashSet<string> listed = new(offer, StringComparer.Ordinal);
    foreach (string entry in offer.Distinct(StringComparer.Ordinal))
    {
        List<(string? Ring, bool Public)> found = typesOf.GetValueOrDefault(entry, []);
        string? clause = found.Count == 0 ? "(a) resolves to no type"
            : found.Count > 1 ? $"(a) resolves to {found.Count} types"
            : found[0].Ring != neighbour ? $"(a) is declared in {found[0].Ring ?? "no ring"}"
            : !found[0].Public ? "(a) is not public"
            : !allowed.Any(prefix => entry.StartsWith(prefix, StringComparison.Ordinal))
                ? $"(a) lacks the prefix {string.Join(", ", allowed)}"
            : null;
        if (clause is not null)
        {
            all.Add(new Finding(where, 0, ringName, "Drifting", neighbour, $"{entry} {clause}", ""));
        }
    }

    if (rings.FirstOrDefault(candidate => candidate.Name == ringName) is { Cut: true })
    {
        IEnumerable<string> unlisted = typesOf
            .Where(item => item.Value.Any(type => type.Ring == neighbour && type.Public))
            .Select(item => item.Key)
            .Where(name => !listed.Contains(name))
            .Order(StringComparer.Ordinal);
        foreach (string name in unlisted)
        {
            all.Add(new Finding(where, 0, ringName, "Drifting", neighbour,
                $"{name} (b) is public in {neighbour} but not offered", ""));
        }
    }

    foreach ((string other, string[] otherOffer) in offers
                 .Where(item => item.Key != pair && item.Key[(item.Key.IndexOf('>') + 1)..] == neighbour))
    {
        foreach (string name in otherOffer.Distinct(StringComparer.Ordinal).Where(name => !listed.Contains(name)))
        {
            all.Add(new Finding(where, 0, ringName, "Drifting", neighbour,
                $"{name} (c) is offered by {other} but not here", ""));
        }
    }
}

HashSet<string> sealedHits = new(StringComparer.Ordinal);
foreach ((string ringName, string[] prefixes) in unsealing)
{
    if (rings.FirstOrDefault(candidate => candidate.Name == ringName) is not { Neighbour: [string neighbour] })
    {
        continue;
    }

    IEnumerable<ISymbol> members = compilation
        .GetSymbolsWithName(
            name => prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)), SymbolFilter.Type)
        .OfType<INamedTypeSymbol>()
        .Where(type => type.ContainingType is null && type.DeclaredAccessibility == Accessibility.Public
            && SourceRing(type)?.Name == ringName)
        .SelectMany(PublicOf)
        .Where(member => member.DeclaredAccessibility == Accessibility.Public && !member.IsImplicitlyDeclared);
    foreach (ISymbol member in members)
    {
        Location? location = member.Locations.FirstOrDefault(place => place.IsInSource);
        if (location is null)
        {
            continue;
        }

        string relative = binder.LAuditRelativeRead(location.SourceTree!.FilePath);
        int line = location.GetLineSpan().StartLinePosition.Line + 1;
        foreach (INamedTypeSymbol type in SignatureOf(member))
        {
            if (SourceRing(type) is { } target && inner[neighbour].Contains(target.Name)
                && sealedHits.Add($"{relative}|{line}|{ringName}|{target.Name}|{type.Name}"))
            {
                all.Add(new Finding(relative, line, ringName, "Unsealing", target.Name, type.Name,
                    LineText(location.SourceTree, line)));
            }
        }
    }
}

foreach (Finding finding in all.Where(finding => finding.Check == "Commuting").ToList())
{
    if (offers.TryGetValue($"{finding.Ring}>{finding.Target}", out string[]? offer)
        && !offer.Contains(finding.Name, StringComparer.Ordinal))
    {
        all.Add(finding with { Check = "Poaching" });
    }
}

string conduct = project + ".Conduct";
HashSet<string> below = inner.GetValueOrDefault(conduct, []);
HashSet<string> removed = new(StringComparer.Ordinal);
List<(Finding Hit, string Key)> subscribed = [];
foreach (SyntaxTree tree in binder.LAuditTrees)
{
    string relative = binder.LAuditRelativeRead(tree.FilePath);
    if (RingOf(relative)?.Name != conduct)
    {
        continue;
    }

    SemanticModel model = compilation.GetSemanticModel(tree, true);
    foreach (AssignmentExpressionSyntax assignment in tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>())
    {
        bool adding = assignment.IsKind(SyntaxKind.AddAssignmentExpression);
        if ((!adding && !assignment.IsKind(SyntaxKind.SubtractAssignmentExpression))
            || model.GetSymbolInfo(assignment.Left).Symbol is not IEventSymbol handled
            || assignment.FirstAncestorOrSelf<TypeDeclarationSyntax>() is not { } holder
            || model.GetDeclaredSymbol(holder) is not INamedTypeSymbol owner)
        {
            continue;
        }

        IEventSymbol handledEvent = handled.OriginalDefinition;
        INamedTypeSymbol declaring = handledEvent.ContainingType.OriginalDefinition;
        if (SymbolEqualityComparer.Default.Equals(declaring, owner.OriginalDefinition))
        {
            continue;
        }

        ExpressionSyntax handler = assignment.Right;
        while (handler is ParenthesizedExpressionSyntax parenthesized)
        {
            handler = parenthesized.Expression;
        }

        bool anonymous = handler is AnonymousFunctionExpressionSyntax;
        SymbolInfo handlerInfo = model.GetSymbolInfo(handler);
        ISymbol? handlerSymbol = anonymous ? null
            : (handlerInfo.Symbol ?? handlerInfo.CandidateSymbols.FirstOrDefault())?.OriginalDefinition;
        string key = owner.OriginalDefinition.ToDisplayString() + "|" + handledEvent.ToDisplayString() + "|"
            + (handlerSymbol?.ToDisplayString() ?? handler.ToString());
        if (!adding)
        {
            if (!anonymous)
            {
                removed.Add(key);
            }

            continue;
        }

        if (SourceRing(declaring) is not { } target || !below.Contains(target.Name))
        {
            continue;
        }

        int line = assignment.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        string label = anonymous ? "(lambda)" : handlerSymbol?.Name ?? "(expression)";
        Finding hit = new(relative, line, conduct, "Lingering", target.Name,
            $"{owner.Name} {declaring.Name}.{handledEvent.Name} {label}", LineText(tree, line));
        subscribed.Add((hit, anonymous ? "" : key));
    }
}

HashSet<string> lingered = new(StringComparer.Ordinal);
foreach ((Finding hit, string key) in subscribed)
{
    if ((key.Length == 0 || !removed.Contains(key)) && lingered.Add($"{hit.Path}|{hit.Line}|{hit.Name}"))
    {
        all.Add(hit);
    }
}

Dictionary<string, int> files = binder.LAuditTrees
    .Select(tree => RingOf(binder.LAuditRelativeRead(tree.FilePath))?.Name)
    .Where(name => name is not null)
    .GroupBy(name => name!, StringComparer.Ordinal)
    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
foreach ((string ringName, int floor) in floors)
{
    int count = files.GetValueOrDefault(ringName);
    if (count < floor)
    {
        all.Add(new Finding(source + ringName, 0, ringName, "Hollowing", ringName, $"{count} source file(s), floor {floor}", ""));
    }
}

List<string> projectFiles = LAuditBinder.LAuditFileRead(projectRoot, [source + "*.csproj"], [], [], [], []);
Dictionary<string, string> projectOf = projectFiles
    .ToDictionary(path => Path.GetFileNameWithoutExtension(path), path => path, StringComparer.Ordinal);
Dictionary<string, string[]> edges = rings.ToDictionary(
    ring => ring.Name, ring => ring.Neighbour.Concat(ring.Capsule).ToArray(), StringComparer.Ordinal);
edges[hostName] = hostReferences;
foreach (string name in projectOf.Keys.Union(edges.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
{
    string? projectFile = projectOf.GetValueOrDefault(name);
    string[] actual = projectFile is null ? [] : EdgeOf(projectFile);
    string[] expected = edges.GetValueOrDefault(name, []);
    string where = projectFile is null ? source + name : binder.LAuditRelativeRead(projectFile);
    foreach (string edge in actual.Except(expected, StringComparer.Ordinal))
    {
        all.Add(new Finding(where, 0, name, "Rerouting", edge, $"{name} -> {edge} is referenced but not in the ring table", ""));
    }

    foreach (string edge in expected.Except(actual, StringComparer.Ordinal))
    {
        all.Add(new Finding(where, 0, name, "Rerouting", edge, $"{name} -> {edge} is in the ring table but not referenced", ""));
    }
}

foreach (string projectFile in projectFiles)
{
    string folder = Path.GetDirectoryName(projectFile)!;
    string owner = binder.LAuditRelativeRead(projectFile);
    string name = Path.GetFileNameWithoutExtension(projectFile);
    foreach (XElement item in XDocument.Load(projectFile).Descendants())
    {
        string include = (string?)item.Attribute("Include") ?? string.Empty;
        bool linked = item.Name.LocalName == "Compile"
            && (item.Attribute("Link") is not null
                || !Path.GetFullPath(Path.Combine(folder, include)).StartsWith(
                    folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
        bool binary = item.Name.LocalName == "Reference" && item.Elements().Any(child => child.Name.LocalName == "HintPath");
        if (linked || binary)
        {
            all.Add(new Finding(owner, 0, name, "Backdooring", item.Name.LocalName, $"{item.Name.LocalName} {include}", ""));
        }
    }

    bool closed = XDocument.Load(projectFile).Descendants()
        .Where(node => node.Name.LocalName == "DisableTransitiveProjectReferences")
        .Any(node => node.Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase));
    if (rings.Any(ring => ring.Cut && ring.Name == name) && !closed)
    {
        all.Add(new Finding(owner, 0, name, "Piggybacking", name, $"{name} compiles against rings past its neighbour", ""));
    }
}

foreach (string import in LAuditBinder.LAuditFileRead(projectRoot, imports.Select(name => "*" + name), [], [], [], []))
{
    string owner = binder.LAuditRelativeRead(import);
    all.AddRange(XDocument.Load(import).Descendants()
        .Where(item => itemNames.Contains(item.Name.LocalName, StringComparer.Ordinal))
        .Select(item => new Finding(owner, 0, "", "Backdooring", item.Name.LocalName, item.Name.LocalName, "")));
}

all = all
    .OrderBy(finding => finding.Check, StringComparer.Ordinal)
    .ThenBy(finding => finding.Ring, StringComparer.Ordinal)
    .ThenBy(finding => finding.Target, StringComparer.Ordinal)
    .ThenBy(finding => finding.Path, StringComparer.Ordinal)
    .ThenBy(finding => finding.Line)
    .ThenBy(finding => finding.Name, StringComparer.Ordinal)
    .ToList();

int audited = binder.LAuditTrees.Count - outside;
JsonSerializerOptions options = new() { WriteIndented = false };
File.WriteAllText(outputPath, JsonSerializer.Serialize(new Summary(audited, outside, all), options));
return 0;

static string[] EdgeOf(string projectFile) => XDocument.Load(projectFile).Descendants()
    .Where(node => node.Name.LocalName == "ProjectReference")
    .Select(node => ((string?)node.Attribute("Include") ?? string.Empty).Replace('\\', '/'))
    .Select(include => Path.GetFileNameWithoutExtension(include[(include.LastIndexOf('/') + 1)..]))
    .Order(StringComparer.Ordinal)
    .ToArray();

static IEnumerable<ISymbol> PublicOf(INamedTypeSymbol type)
{
    foreach (ISymbol member in type.GetMembers())
    {
        yield return member;
        if (member is INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public } nested)
        {
            foreach (ISymbol deeper in PublicOf(nested))
            {
                yield return deeper;
            }
        }
    }
}

static IEnumerable<INamedTypeSymbol> SignatureOf(ISymbol member)
{
    IEnumerable<ITypeSymbol> types = member switch
    {
        IMethodSymbol { AssociatedSymbol: null } method =>
            method.Parameters.Select(parameter => parameter.Type).Prepend(method.ReturnType),
        IPropertySymbol property => property.Parameters.Select(parameter => parameter.Type).Prepend(property.Type),
        IEventSymbol handler => [handler.Type],
        IFieldSymbol field => [field.Type],
        INamedTypeSymbol nested => nested.BaseType is null ? nested.Interfaces : nested.Interfaces.Prepend(nested.BaseType),
        _ => [],
    };
    Stack<ITypeSymbol> pending = new(types);
    while (pending.Count > 0)
    {
        ITypeSymbol type = pending.Pop();
        if (type is IArrayTypeSymbol array)
        {
            pending.Push(array.ElementType);
        }
        else if (type is INamedTypeSymbol named)
        {
            yield return named.OriginalDefinition;
            foreach (ITypeSymbol argument in named.TypeArguments)
            {
                pending.Push(argument);
            }
        }
    }
}

static string[] Strings(JsonElement element) => element.EnumerateArray().Select(item => item.GetString()!).ToArray();

static Dictionary<string, string[]> Map(JsonElement element) => element.EnumerateObject()
    .ToDictionary(item => item.Name, item => Strings(item.Value), StringComparer.Ordinal);

sealed record Ring(string Name, string Path, string[] Neighbour, string[] Capsule, string[] Frame, bool Pure, bool Cut);

sealed record Finding(string Path, int Line, string Ring, string Check, string Target, string Name, string Text);

sealed record Summary(int Files, int Outside, List<Finding> Findings);
'@

function Write-AuditHelper {
    # The binder is Roslyn 4.14.0, the version the convention tests pin, restored once into the
    # package cache. The helper reads the configuration, binds every file through the shared binder,
    # and writes the file counts and every finding to the output path as JSON.
    param(
        [Parameter(Mandatory = $true)][string]$HelperFolder,
        [Parameter(Mandatory = $true)][string]$TargetFramework
    )

    $projectPath = Join-Path $HelperFolder 'AuditStructure.csproj'
    $programPath = Join-Path $HelperFolder 'Program.cs'
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($projectPath, $script:HelperProject.Replace('{TARGET_FRAMEWORK}', $TargetFramework), $encoding)
    [System.IO.File]::WriteAllText($programPath, $script:HelperProgram, $encoding)
    [System.IO.File]::WriteAllText((Join-Path $HelperFolder 'AuditBinder.cs'), $script:BinderSource, $encoding)
    return $projectPath
}

function Get-HelperCacheFolder {
    # The helper is compiled once per text and SDK and kept under the temp folder, so a later run
    # skips the restore and the build and pays for the binding alone. The build folder is not used
    # because the build tooling clears it on install.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectName,
        [Parameter(Mandatory = $true)][string]$TargetFramework,
        [Parameter(Mandatory = $true)][string]$SdkVersion
    )

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $seed = $script:HelperProject + "`n" + $script:HelperProgram + "`n" + $script:BinderSource + "`n" + $TargetFramework + "`n" + $SdkVersion
        $digest = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($seed))
    }
    finally {
        $sha.Dispose()
    }

    $hash = ([System.BitConverter]::ToString($digest) -replace '-', '').Substring(0, 16).ToLowerInvariant()
    return Join-Path ([System.IO.Path]::GetTempPath()) ($ProjectName + '-AuditStructure' + [System.IO.Path]::DirectorySeparatorChar + $hash)
}

function Invoke-AuditHelper {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectRoot,
        [Parameter(Mandatory = $true)][string]$ConfigPath,
        [Parameter(Mandatory = $true)][string]$ProjectName,
        [Parameter(Mandatory = $true)][string]$TargetFramework
    )

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($null -eq $dotnet) {
        throw 'The .NET SDK is required, but dotnet was not found on PATH.'
    }

    if ($null -eq (Get-Command git -ErrorAction SilentlyContinue)) {
        throw 'Git is required to enumerate project source files, but git was not found on PATH.'
    }

    if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot '.git'))) {
        throw "The project root is not a Git working tree: $ProjectRoot"
    }

    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $sdkOutput = & $dotnet.Source --version 2>&1
    $ErrorActionPreference = $nativePreference
    if ($LASTEXITCODE -ne 0) {
        throw "The .NET SDK version could not be read.`n$($sdkOutput -join [Environment]::NewLine)"
    }

    $sdkVersion = ([string]($sdkOutput | Select-Object -First 1)).Trim()

    $previousNoLogo = $env:DOTNET_NOLOGO
    $previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
    $env:DOTNET_NOLOGO = '1'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

    $temporaryFolder = Join-Path ([System.IO.Path]::GetTempPath()) ($ProjectName + '-AuditStructure-' + [Guid]::NewGuid().ToString('N'))
    [System.IO.Directory]::CreateDirectory($temporaryFolder) | Out-Null

    try {
        $cacheFolder = Get-HelperCacheFolder -ProjectName $ProjectName -TargetFramework $TargetFramework -SdkVersion $sdkVersion
        $binaryPath = Join-Path (Join-Path $cacheFolder 'bin') 'AuditStructure.dll'
        if (-not (Test-Path -LiteralPath $binaryPath -PathType Leaf)) {
            Write-AuditLine 'Compiling the structure binder once for this SDK...'
            $cacheParent = Split-Path -Parent $cacheFolder
            if (Test-Path -LiteralPath $cacheParent) {
                Get-ChildItem -LiteralPath $cacheParent -Directory |
                    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
            }

            $helperFolder = Join-Path $cacheFolder 'helper'
            [System.IO.Directory]::CreateDirectory($helperFolder) | Out-Null
            $projectPath = Write-AuditHelper -HelperFolder $helperFolder -TargetFramework $TargetFramework
            $buildArguments = @('build', $projectPath, '--configuration', 'Release', '--nologo', '--verbosity', 'quiet',
                '--output', (Join-Path $cacheFolder 'bin'))
            $nativePreference = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            $buildOutput = & $dotnet.Source @buildArguments 2>&1
            $ErrorActionPreference = $nativePreference
            if ($LASTEXITCODE -ne 0) {
                Remove-Item -LiteralPath $cacheFolder -Recurse -Force -ErrorAction SilentlyContinue
                throw "The structure binder could not be built.`n$($buildOutput -join [Environment]::NewLine)"
            }
        }

        $outputPath = Join-Path $temporaryFolder 'findings.json'

        $nativePreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        $helperOutput = & $dotnet.Source $binaryPath $ConfigPath $ProjectRoot (Join-Path $PSScriptRoot 'AuditBinder.json') $outputPath 2>&1
        $ErrorActionPreference = $nativePreference
        if ($LASTEXITCODE -ne 0) {
            throw "The structure binder failed.`n$($helperOutput -join [Environment]::NewLine)"
        }

        return (Get-Content -LiteralPath $outputPath -Raw -Encoding UTF8 | ConvertFrom-Json)
    }
    finally {
        $env:DOTNET_NOLOGO = $previousNoLogo
        $env:DOTNET_CLI_TELEMETRY_OPTOUT = $previousTelemetry
        if (Test-Path -LiteralPath $temporaryFolder) {
            Remove-Item -LiteralPath $temporaryFolder -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

function Format-ReportCell {
    param([string]$Value)

    if ($null -eq $Value) {
        return ''
    }

    $single = $Value -replace '\s+', ' '
    if ($single.Length -gt 120) {
        $single = $single.Substring(0, 117) + '...'
    }

    return $single.Replace('|', '\|').Trim()
}

function Format-Location {
    # A source hit is path:line; a project, folder or ring finding has no line and prints its path.
    param($Finding)

    if ($Finding.Line -gt 0) {
        return "$($Finding.Path):$($Finding.Line)"
    }

    return $Finding.Path
}

function Format-FindingLine {
    param($Finding)

    $location = Format-Location -Finding $Finding
    switch -CaseSensitive ($Finding.Check) {
        'Poaching' { return "$location $($Finding.Name) outside $($Finding.Ring)>$($Finding.Target)" }
        'Drifting' { return "$location $($Finding.Ring)>$($Finding.Target) $($Finding.Name)" }
        'Lingering' { return "$location $($Finding.Name) never removed" }
        'Squatting' { return "$location $($Finding.Name) ~ $($Finding.Target)" }
        default { return "$location $($Finding.Name)" }
    }
}

$projectRoot = Resolve-ProjectRoot -Path $Root
$config = Read-AuditConfig -ProjectRoot $projectRoot
$configPath = Join-Path $PSScriptRoot $script:ConfigDocument
$version = Read-ProjectVersion -ProjectRoot $projectRoot -Config $config
$source = ([string]$config.source).Replace('\', '/').Trim('/') + '/'

if ($Top -le 0) {
    $Top = [int]$config.console.top
}
$script:ItemLimit = $Top

$reportFolder = if ([string]::IsNullOrWhiteSpace($ReportDirectory)) { [string]$config.report.directory } else { $ReportDirectory }
if (-not [System.IO.Path]::IsPathRooted($reportFolder)) {
    $reportFolder = Join-AuditPath -ProjectRoot $projectRoot -Relative $reportFolder
}
$reportPath = Join-Path $reportFolder ([string]$config.report.prefix + $version + '.md')
$pagePath = Join-Path $reportFolder ([string]$config.report.prefix + $version + '.html')

$result = Invoke-AuditHelper -ProjectRoot $projectRoot -ConfigPath $configPath -ProjectName ([string]$config.project) -TargetFramework ([string]$config.helper.framework)
$auditedFiles = [int]$result.Files
$skippedFiles = [int]$result.Outside
if ($auditedFiles -eq 0) {
    throw "No tracked source file sits inside a declared ring under: $projectRoot"
}

Write-AuditLine ("Scanned: {0:N0} source files inside a ring, {1:N0} outside" -f $auditedFiles, $skippedFiles) -ForegroundColor DarkGray

$findings = New-Object 'System.Collections.Generic.List[object]'
foreach ($finding in @($result.Findings | ForEach-Object { $_ })) {
    [void]$findings.Add([pscustomobject]@{
        Path   = [string]$finding.Path
        Line   = [int]$finding.Line
        Ring   = [string]$finding.Ring
        Check  = [string]$finding.Check
        Target = [string]$finding.Target
        Name   = [string]$finding.Name
        Text   = [string]$finding.Text
        Exempt = $false
    })
}

# An exemption clears a Border hit from the border list or a Purity hit from the purity list, the
# first matching row winning. A Census or Charter finding is never exempt.
$exemptRows = New-Object 'System.Collections.Generic.List[object]'
foreach ($list in @('border', 'purity')) {
    foreach ($row in @($config.exempt.$list)) {
        [void]$exemptRows.Add([pscustomobject]@{ List = $list; Row = [string]$row; Used = 0 })
    }
}

if ($exemptRows.Count -gt 0) {
    foreach ($finding in $findings) {
        $list = if ($script:BorderChecks -ccontains $finding.Check) { 'border' }
            elseif ($script:PurityChecks -ccontains $finding.Check) { 'purity' }
            else { '' }
        if ($list.Length -eq 0) {
            continue
        }

        foreach ($row in $exemptRows) {
            if ($row.List -ceq $list -and (Test-ExemptRow -Row $row.Row -Path $finding.Path -Name $finding.Name)) {
                $finding.Exempt = $true
                $row.Used++
                break
            }
        }
    }
}

$staleExempt = @($exemptRows | Where-Object { $_.Used -eq 0 })

$live = New-Object 'System.Collections.Generic.List[object]'
$counts = @{}
foreach ($check in $script:CheckOrder) {
    $counts[$check] = 0
}

foreach ($finding in $findings) {
    if ($finding.Exempt) {
        continue
    }

    [void]$live.Add($finding)
    if (-not $counts.ContainsKey($finding.Check)) {
        throw "The structure binder reported an unknown kind: $($finding.Check)"
    }

    $counts[$finding.Check]++
}

# A pair is one held kind from one ring to one target. Its count is the number of distinct hits,
# one per file, line, kind, target and name, as in the convention tests. A pair is held to the
# ceiling written for it, an unwritten ceiling being zero. The Piggybacking pair counts cut projects.
$pairOf = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([System.StringComparer]::Ordinal)
function Get-AuditPair {
    param([string]$Key, [string]$Check, [string]$Ring, [string]$Target)

    if (-not $pairOf.ContainsKey($Key)) {
        $pairOf[$Key] = [pscustomobject]@{
            Key     = $Key
            Check   = $Check
            Ring    = $Ring
            Target  = $Target
            Files   = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
            Hits    = New-Object 'System.Collections.Generic.List[object]'
            Ceiling = 0
        }
    }

    return $pairOf[$Key]
}

foreach ($finding in $live) {
    if ($script:HeldChecks -ccontains $finding.Check) {
        $pair = Get-AuditPair -Key "$($finding.Check):$($finding.Ring)>$($finding.Target)" -Check $finding.Check -Ring $finding.Ring -Target $finding.Target
    }
    elseif ($finding.Check -ceq $script:SingleCheck) {
        $pair = Get-AuditPair -Key $script:SingleCheck -Check $script:SingleCheck -Ring '' -Target ''
    }
    else {
        continue
    }

    [void]$pair.Files.Add($finding.Path)
    [void]$pair.Hits.Add($finding)
}

foreach ($property in $config.ceilings.PSObject.Properties) {
    if ($property.Name -ceq $script:SingleCheck) {
        if ([int]$property.Value -gt 0) {
            $pair = Get-AuditPair -Key $script:SingleCheck -Check $script:SingleCheck -Ring '' -Target ''
            $pair.Ceiling = [int]$property.Value
        }

        continue
    }

    $parts = $property.Name -split '[:>]', 3
    $pair = Get-AuditPair -Key $property.Name -Check $parts[0] -Ring $parts[1] -Target $parts[2]
    $pair.Ceiling = [int]$property.Value
}

$pairs = @($pairOf.Values | Sort-Object -Property @{ Expression = { [array]::IndexOf($script:CheckOrder, $_.Check) } }, @{ Expression = { Get-OrdinalKey $_.Ring } }, @{ Expression = { Get-OrdinalKey $_.Target } })
$overCeiling = @($pairs | Where-Object { $_.Hits.Count -gt $_.Ceiling })
$staleCeiling = @($pairs | Where-Object { $_.Hits.Count -lt $_.Ceiling })
$heldPairs = @($pairs | Where-Object { $_.Hits.Count -gt 0 -and $_.Hits.Count -le $_.Ceiling })

$hardOf = @{}
foreach ($check in $script:HardChecks) {
    $hardOf[$check] = New-Object 'System.Collections.Generic.List[object]'
}

foreach ($finding in $live) {
    if ($hardOf.ContainsKey($finding.Check)) {
        [void]$hardOf[$finding.Check].Add($finding)
    }
}

# The violations are the hits of every pair above its ceiling and every hit of a kind that allows none.
$violations = New-Object 'System.Collections.Generic.List[object]'
foreach ($pair in $overCeiling) {
    foreach ($hit in $pair.Hits) {
        [void]$violations.Add($hit)
    }
}

foreach ($check in $script:HardChecks) {
    foreach ($hit in $hardOf[$check]) {
        [void]$violations.Add($hit)
    }
}

$heldHits = New-Object 'System.Collections.Generic.List[object]'
foreach ($pair in $heldPairs) {
    foreach ($hit in $pair.Hits) {
        [void]$heldHits.Add($hit)
    }
}

$countTotal = 0
foreach ($check in $script:CheckOrder) {
    $countTotal += $counts[$check]
}

$counters = [ordered]@{
    'Above ceiling'    = $overCeiling.Count
    'Stale ceilings'   = $staleCeiling.Count
    'Stale exemptions' = $staleExempt.Count
}
foreach ($check in $script:HardChecks) {
    $counters[$check] = $hardOf[$check].Count
}

$gated = @($live | Where-Object { $script:HeldChecks -ccontains $_.Check -or $script:HardChecks -ccontains $_.Check })

function Get-PairStanding {
    param($Pair)

    if ($Pair.Hits.Count -gt $Pair.Ceiling) { return 'above ceiling' }
    if ($Pair.Hits.Count -lt $Pair.Ceiling) { return 'stale ceiling' }
    return 'at ceiling'
}

# The names and files that step past a ring, heaviest first, for the report and the page.
$nameOf = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([System.StringComparer]::Ordinal)
$fileOf = New-Object 'System.Collections.Generic.Dictionary[string,int]' ([System.StringComparer]::Ordinal)
foreach ($finding in $gated) {
    $key = "$($finding.Name)`n$($finding.Target)"
    if (-not $nameOf.ContainsKey($key)) {
        $nameOf[$key] = [pscustomobject]@{
            Name  = $finding.Name
            Target = $finding.Target
            Hits  = 0
            Files = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
            Heaviest = $finding.Check
        }
    }

    $entry = $nameOf[$key]
    $entry.Hits++
    [void]$entry.Files.Add($finding.Path)
    if ([array]::IndexOf($script:CheckOrder, $finding.Check) -lt [array]::IndexOf($script:CheckOrder, $entry.Heaviest)) {
        $entry.Heaviest = $finding.Check
    }

    if (-not $fileOf.ContainsKey($finding.Path)) {
        $fileOf[$finding.Path] = 0
    }

    $fileOf[$finding.Path]++
}

$nameRows = @($nameOf.Values | Sort-Object -Property @{ Expression = { $_.Hits }; Descending = $true }, @{ Expression = { Get-OrdinalKey $_.Name } }, @{ Expression = { Get-OrdinalKey $_.Target } })
$fileRows = @($fileOf.GetEnumerator() | Sort-Object -Property @{ Expression = { $_.Value }; Descending = $true }, @{ Expression = { Get-OrdinalKey $_.Key } })

$report = New-Object 'System.Collections.Generic.List[string]'
[void]$report.Add("# Structure audit - $($config.project) $version")
[void]$report.Add('')
[void]$report.Add("- Generation: $script:AuditGeneration")
[void]$report.Add("- Files audited: $auditedFiles")
[void]$report.Add("- Files outside a declared ring: $skippedFiles")
foreach ($label in $counters.Keys) {
    [void]$report.Add("- ${label}: $($counters[$label])")
}
[void]$report.Add("- Hits held by ceilings: $($heldHits.Count)")
[void]$report.Add('')
[void]$report.Add('The audit binds the source and reads the kind of every type one ring names from another. A')
[void]$report.Add('ring names the one neighbour the table gives it and carries the data of any ring inside it;')
[void]$report.Add('a class or an interface named from deeper than the neighbour is Leapfrogging, and a name from')
[void]$report.Add('an outer ring is Trespassing. A pure ring names only the framework namespaces its frame lists')
[void]$report.Add('and never touches an eavesdropping member. The project files hold the ring table edge by edge.')
[void]$report.Add('')
[void]$report.Add('## Rings')
[void]$report.Add('')
[void]$report.Add('| Ring | Folder | Neighbour | Pure | Cut |')
[void]$report.Add('|---|---|---|---|---|')
foreach ($ring in $config.rings) {
    $neighbours = if (@($ring.neighbour).Count -eq 0) { 'nothing' } else { (@($ring.neighbour) | ForEach-Object { "``$_``" }) -join ', ' }
    [void]$report.Add("| ``$($ring.name)`` | ``$source$($ring.name)`` | $neighbours | $(if ($ring.pure) { 'yes' } else { 'no' }) | $(if ($ring.cut) { 'yes' } else { 'no' }) |")
}
$hostReferences = (@($config.host.references) | ForEach-Object { "``$_``" }) -join ', '
[void]$report.Add("| ``$($config.host.name)`` (host) | ``$source$($config.host.name)`` | $hostReferences | no | no |")

[void]$report.Add('')
[void]$report.Add('## Kinds')
[void]$report.Add('')
[void]$report.Add('| Kind | Family | Meaning | Gate | Hits |')
[void]$report.Add('|---|---|---|---|---|')
foreach ($check in $script:CheckOrder) {
    [void]$report.Add("| ``$check`` | $(Get-CheckFamily -Check $check) | $($script:CheckTitles[$check]) | $(Get-CheckGate -Check $check) | $($counts[$check]) |")
}
[void]$report.Add("| Total | | | | $countTotal |")

[void]$report.Add('')
[void]$report.Add('## Pairs')
[void]$report.Add('')
if ($pairs.Count -eq 0) {
    [void]$report.Add('No held pair has a hit or a ceiling.')
}
else {
    [void]$report.Add('| Pair | Files | Hits | Ceiling | Standing |')
    [void]$report.Add('|---|---|---|---|---|')
    foreach ($pair in $pairs) {
        [void]$report.Add("| ``$($pair.Key)`` | $($pair.Files.Count) | $($pair.Hits.Count) | $($pair.Ceiling) | $(Get-PairStanding -Pair $pair) |")
    }
}

function Add-FindingTable {
    param($Lines, $Items, [string]$Empty)

    if ($Items.Count -eq 0) {
        [void]$Lines.Add($Empty)
        return
    }

    [void]$Lines.Add('| Location | Kind | Ring | Target | Name | Source |')
    [void]$Lines.Add('|---|---|---|---|---|---|')
    foreach ($finding in $Items) {
        [void]$Lines.Add("| ``$(Format-Location -Finding $finding)`` | ``$($finding.Check)`` | ``$($finding.Ring)`` | ``$($finding.Target)`` | ``$(Format-ReportCell -Value $finding.Name)`` | ``$(Format-ReportCell -Value $finding.Text)`` |")
    }
}

[void]$report.Add('')
[void]$report.Add('## Violations')
[void]$report.Add('')
[void]$report.Add('Every violation below is a move until the user says otherwise. A ring that reaches past its')
[void]$report.Add('neighbour makes the ring between them optional: the engine that calls a vault is a second')
[void]$report.Add('use-case layer, the view that names the engine is a second presenter. Resolve Leapfrogging')
[void]$report.Add('by handing the work to the neighbour ring, Foraging by moving the framework call behind a')
[void]$report.Add('port, and Eavesdropping by taking the clock or the file through a port. Never resolve one by')
[void]$report.Add('turning a class into a record so it reads as data.')
[void]$report.Add('')
[void]$report.Add('**Only the user grants an exemption** by adding a row to the `exempt` block of the audit')
[void]$report.Add('configuration, and **only the user lowers a ceiling**, never raises one. The audit reads the')
[void]$report.Add('binder alone, so a finding it raises may still be correct code; say why, and cite the')
[void]$report.Add('`file:line` that was read.')
[void]$report.Add('')
Add-FindingTable -Lines $report -Items $violations -Empty 'No structural violation was found.'

[void]$report.Add('')
[void]$report.Add('## Held by ceilings')
[void]$report.Add('')
[void]$report.Add('These hits sit within the ceiling of their pair. They pass today, and each one fixed lowers a ceiling.')
[void]$report.Add('')
Add-FindingTable -Lines $report -Items $heldHits -Empty 'No hit is held by a ceiling.'

[void]$report.Add('')
[void]$report.Add('## Names')
[void]$report.Add('')
if ($nameRows.Count -eq 0) {
    [void]$report.Add('No name stepped past its ring.')
}
else {
    [void]$report.Add('| Name | Target | Hits | Files | Heaviest kind |')
    [void]$report.Add('|---|---|---|---|---|')
    foreach ($entry in $nameRows) {
        [void]$report.Add("| ``$($entry.Name)`` | ``$($entry.Target)`` | $($entry.Hits) | $($entry.Files.Count) | ``$($entry.Heaviest)`` |")
    }
}

[void]$report.Add('')
[void]$report.Add('## Files')
[void]$report.Add('')
if ($fileRows.Count -eq 0) {
    [void]$report.Add('No file stepped past its ring.')
}
else {
    [void]$report.Add('| File | Hits |')
    [void]$report.Add('|---|---|')
    foreach ($row in $fileRows) {
        [void]$report.Add("| ``$($row.Key)`` | $($row.Value) |")
    }
}

[void]$report.Add('')
[void]$report.Add('## Exemptions')
[void]$report.Add('')
if ($exemptRows.Count -eq 0) {
    [void]$report.Add('The configuration declares no exemption.')
}
else {
    [void]$report.Add('| List | Row | Cleared |')
    [void]$report.Add('|---|---|---|')
    foreach ($row in $exemptRows) {
        [void]$report.Add("| $($row.List) | ``$($row.Row)`` | $($row.Used) |")
    }
}

[void]$report.Add('')
[void]$report.Add('## Stale exemptions')
[void]$report.Add('')
if ($staleExempt.Count -eq 0) {
    [void]$report.Add('Every declared exemption cleared a finding.')
}
else {
    [void]$report.Add('| List | Row |')
    [void]$report.Add('|---|---|')
    foreach ($row in $staleExempt) {
        [void]$report.Add("| $($row.List) | ``$($row.Row)`` |")
    }
}

$utf8 = [System.Text.UTF8Encoding]::new($false)
[System.IO.Directory]::CreateDirectory($reportFolder) | Out-Null
$stagingPath = $reportPath + '.tmp'
[System.IO.File]::WriteAllText($stagingPath, (($report.ToArray() -join "`n") + "`n"), $utf8)
Move-Item -LiteralPath $stagingPath -Destination $reportPath -Force

$meanings = @{
    'Above ceiling'     = 'ring pairs with more hits than their ceiling'
    'Stale ceilings'    = 'ring pairs with fewer hits than their ceiling'
    'Stale exemptions'  = 'exemption rows that cleared no finding'
    'Poaching'          = 'names reached outside a pair''s listed offers'
    'Drifting'          = 'offer entries or public types where a list and its neighbour differ'
    'Lingering'         = 'engine event subscriptions a Conduct type never removes'
    'Squatting'         = 'types matching a home pattern found elsewhere'
    'Smuggling'         = 'listed words found in their folder'
    'Hollowing'         = 'projects below their floor'
    'Rerouting'         = 'ring table rows that disagree with the projects'
    'Backdooring'       = 'references the table does not declare'
}
$gateRows = @(foreach ($label in $counters.Keys) {
    [pscustomobject]@{ Gate = $label; Count = [int]$counters[$label]; Meaning = $meanings[$label]; Section = $label }
})
Write-AuditResult -Rows $gateRows

Write-AuditSection -Title 'Findings by kind'
$checkRows = @($script:CheckOrder | ForEach-Object { , @($_, (Get-CheckFamily -Check $_), (Get-CheckGate -Check $_), $counts[$_].ToString('N0')) })
$checkRows += , @('Total', '', '', $countTotal.ToString('N0'))
Write-AuditTable -Header @('Kind', 'Family', 'Gate', 'Hits') -Rows $checkRows

if ($pairs.Count -gt 0) {
    Write-AuditSection -Title "Pairs ($($pairs.Count.ToString('N0')))"
    $pairRows = @($pairs | ForEach-Object {
        , @($_.Key, $_.Files.Count.ToString('N0'), $_.Hits.Count.ToString('N0'), $_.Ceiling.ToString('N0'), (Get-PairStanding -Pair $_))
    })
    Write-AuditTable -Header @('Pair', 'Files', 'Hits', 'Ceiling', 'Standing') -Rows $pairRows
}

Write-AuditItems -Title 'Above ceiling' -Lines ([string[]]@($overCeiling | ForEach-Object { "$($_.Key): $($_.Hits.Count.ToString('N0')) hit(s), ceiling $($_.Ceiling.ToString('N0'))" }))
Write-AuditItems -Title 'Hits above ceiling' -Lines ([string[]]@($overCeiling | ForEach-Object { $_.Hits } | ForEach-Object { Format-FindingLine -Finding $_ }))
Write-AuditItems -Title 'Stale ceilings' -Lines ([string[]]@($staleCeiling | ForEach-Object { "$($_.Key): $($_.Hits.Count.ToString('N0')) hit(s), ceiling $($_.Ceiling.ToString('N0'))" }))
Write-AuditItems -Title 'Stale exemptions' -Lines ([string[]]@($staleExempt | ForEach-Object { "exempt.$($_.List) $($_.Row)" }))
foreach ($check in $script:HardChecks) {
    Write-AuditItems -Title $check -Lines ([string[]]@($hardOf[$check] | ForEach-Object { Format-FindingLine -Finding $_ }))
}

# Page output: the full result as data inside the template AuditStructure.html.
# The JSON is written by hand, so Windows PowerShell 5.1 and pwsh 7 produce the same bytes.
function ConvertTo-PageJson {
    param([AllowNull()][object]$Value)

    if ($null -eq $Value) { return 'null' }
    if ($Value -is [bool]) { return $(if ($Value) { 'true' } else { 'false' }) }
    if ($Value -is [string]) {
        if ($Value -notmatch '[^ -~]|["<>&]|\\') { return '"' + $Value + '"' }
        $builder = [System.Text.StringBuilder]::new('"')
        foreach ($character in $Value.ToCharArray()) {
            $code = [int]$character
            if ($character -eq '"') { [void]$builder.Append('\"') }
            elseif ($character -eq '\') { [void]$builder.Append('\\') }
            elseif ($code -lt 0x20 -or $code -gt 0x7E -or $character -eq '<' -or $character -eq '>' -or $character -eq '&') { [void]$builder.Append('\u' + $code.ToString('x4')) }
            else { [void]$builder.Append($character) }
        }
        return $builder.Append('"').ToString()
    }
    if ($Value -is [int] -or $Value -is [long]) { return $Value.ToString([System.Globalization.CultureInfo]::InvariantCulture) }
    if ($Value -is [System.Collections.IDictionary]) {
        $pairs = @(foreach ($key in $Value.Keys) { (ConvertTo-PageJson ([string]$key)) + ':' + (ConvertTo-PageJson $Value[$key]) })
        return '{' + ($pairs -join ',') + '}'
    }
    if ($Value -is [System.Collections.IEnumerable]) {
        $items = @(foreach ($item in $Value) { ConvertTo-PageJson $item })
        return '[' + ($items -join ",`n") + ']'
    }
    throw "The page data holds a value of an unexpected type: $($Value.GetType().FullName)"
}

function ConvertTo-PageLink {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([string]::Equals([System.IO.Path]::GetDirectoryName($Path), $reportDirectoryFull.TrimEnd('\', '/'), [System.StringComparison]::OrdinalIgnoreCase)) {
        return [System.IO.Path]::GetFileName($Path)
    }
    return [System.Uri]::new($Path).AbsoluteUri
}

function ConvertTo-PageFinding {
    param($Finding)

    return [ordered]@{
        kind = [string]$Finding.Check
        ring = [string]$Finding.Ring
        target = [string]$Finding.Target
        name = [string]$Finding.Name
        path = [string]$Finding.Path
        line = [int]$Finding.Line
        text = [string](Format-ReportCell -Value $Finding.Text)
    }
}

# The page: every value below is one the console and the Markdown report already show.
$reportDirectoryFull = [System.IO.Path]::GetFullPath($reportFolder)
$pageData = [ordered]@{
    project = [string]$config.project
    version = $version
    generation = $script:AuditGeneration
    generated = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz')
    root = $projectRoot
    scanned = [int]$auditedFiles
    outside = [int]$skippedFiles
    total = [int]$countTotal
    gates = @($gateRows | ForEach-Object {
        [ordered]@{ status = $(if ($_.Count -gt 0) { 'FAIL' } else { 'OK' }); gate = [string]$_.Gate; count = [int]$_.Count; meaning = [string]$_.Meaning; section = [string]$_.Section }
    })
    kinds = @($script:CheckOrder | ForEach-Object {
        [ordered]@{ kind = [string]$_; family = [string](Get-CheckFamily -Check $_); gate = [string](Get-CheckGate -Check $_); meaning = [string]$script:CheckTitles[$_]; count = [int]$counts[$_] }
    })
    pairs = @($pairs | ForEach-Object {
        [ordered]@{ pair = [string]$_.Key; kind = [string]$_.Check; files = [int]$_.Files.Count; hits = [int]$_.Hits.Count; ceiling = [int]$_.Ceiling; standing = [string](Get-PairStanding -Pair $_) }
    })
    violations = @($violations | ForEach-Object { ConvertTo-PageFinding -Finding $_ })
    held = @($heldHits | ForEach-Object { ConvertTo-PageFinding -Finding $_ })
    staleExemptions = @($staleExempt | ForEach-Object { [ordered]@{ list = [string]$_.List; row = [string]$_.Row } })
    exemptions = @($exemptRows | ForEach-Object { [ordered]@{ list = [string]$_.List; row = [string]$_.Row; cleared = [int]$_.Used } })
    names = @($nameRows | ForEach-Object { [ordered]@{ name = [string]$_.Name; target = [string]$_.Target; hits = [int]$_.Hits; files = [int]$_.Files.Count; heaviest = [string]$_.Heaviest } })
    files = @($fileRows | ForEach-Object { [ordered]@{ path = [string]$_.Key; hits = [int]$_.Value } })
    rings = @(@($config.rings) | ForEach-Object {
        [ordered]@{
            name = [string]$_.name
            folder = $source + [string]$_.name
            neighbour = @(@($_.neighbour) | ForEach-Object { [string]$_ })
            capsule = @(@($_.capsule) | ForEach-Object { [string]$_ })
            frame = @(@($_.frame) | ForEach-Object { [string]$_ })
            pure = [bool]$_.pure
            cut = [bool]$_.cut
        }
    })
    scope = [ordered]@{
        source = $source
        helper = [string]$config.helper.framework
        host = [string]$config.host.name
        hostReferences = @(@($config.host.references) | ForEach-Object { [string]$_ })
        shells = @(@($config.shells) | ForEach-Object { [string]$_ })
        imports = @(@($config.imports) | ForEach-Object { [string]$_ })
        eavesdropping = @(@($config.eavesdroppingMembers) | ForEach-Object { [string]$_ })
    }
    reports = @(
        [ordered]@{ name = [System.IO.Path]::GetFileName($reportPath); link = ConvertTo-PageLink -Path $reportPath }
    )
}

$pageTemplatePath = Join-Path $PSScriptRoot 'AuditStructure.html'
$pageTemplate = [System.IO.File]::ReadAllText($pageTemplatePath, $utf8)
foreach ($marker in @('/*__DATA__*/', '__TITLE__')) {
    if (-not $pageTemplate.Contains($marker)) { throw "The page template lacks the marker $marker : $pageTemplatePath" }
}
$pageText = $pageTemplate.Replace('__TITLE__', [System.Net.WebUtility]::HtmlEncode([string]$config.project)).Replace('/*__DATA__*/', (ConvertTo-PageJson $pageData))
[System.IO.File]::WriteAllText($pagePath, ($pageText -replace "`r`n", "`n"), $utf8)

Write-AuditLine ''
Write-AuditLine "Report: $reportPath"
Write-AuditLine "Report: $pagePath"

if ($Open -and (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    Invoke-Item -LiteralPath $reportPath
}

if (-not $NoOpen) {
    Start-Process -FilePath $pagePath
}

foreach ($label in $counters.Keys) {
    if ($counters[$label] -gt 0) {
        exit 1
    }
}

exit 0
