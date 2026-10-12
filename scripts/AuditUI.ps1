<#
.SYNOPSIS
Audit the surfaces for anything but calls and the drivers for deciding data, standalone counterpart of the UI convention tests.

.DESCRIPTION
A surface is what the user sees: the veneer (Cadroue.UIVeneer) for the GUI and the terminal
(Cadroue.UITerminal) for the CUI. A surface member only calls a function: no branch, no operator,
no assignment, no held state and no reach into the engine. A driver drives its surface: the
deportment (Cadroue.UIDeportment) and the demeanor (Cadroue.UIDemeanor). A driver may control its
medium freely, but it must not decide the data: that stays with the Conduct gates and the engine.

The script binds the source with Roslyn through the shared binder of AuditBinder.cs and walks it
with its own copy of the walkers of the convention tests TAuditStrict, TAuditTruth and
TAuditBoundary. It never reads, runs or depends on the test project: the rules live in AuditUI.json
and the ceilings in AuditUI.ledger.json. The tests and this script audit the same ground truth, so
each tells the truth when the other is broken.

  Strict    the surfaces and the host: Hoarding, Sharing, Meddling, Prying, Overreaching, Branching,
            Homoglyph, Overworking, Tethering, Freelancing, the drivers' Hardwiring, Masquerading and
            Dangling, plus driver disk lines, surface catalog lines and their exemptions.
  Truth     the drivers: Replaying, Gatekeeping, Contesting, Duplicating, Tampering, Misfiring,
            Moonlighting, Homoglyph, Laundering, Spoonfeeding, Mismatching, Puppeteering, Zeroing.
  Boundary  the guards that keep the walkers sound: state builds and compares, hidden code,
            reflection, skipped sources, logic panels, hold timers and stale list rows.

A hit is held while its file sits at or under the ledger ceiling of its kind. A file above its
ceiling fails, and so does a ceiling above its count, so a ceiling only walks down. Every fact
the tests gate is one counter, and the counters sum to zero exactly when those tests pass.

Each run writes two reports. {report.directory}/{report.prefix}{version}.md lists every hit in the
line format of the test reports. {report.directory}/{report.prefix}{version}.html shows the same
result as a page built from the template AuditUI.html, and opens in the default browser unless
-NoOpen is given. Git and the .NET SDK are required, and the solution must be built, since the
binder reads the generated code and the host build output.

.PARAMETER Root
Project root to audit. Defaults to the directory containing this script's parent.

.PARAMETER ConfigPath
Path to the JSON configuration. Defaults to AuditUI.json next to this script.

.PARAMETER Configuration
Build configuration the binder reads generated code and references from: Debug or Release.
Defaults to the configuration in AuditBinder.json.

.PARAMETER OutputPath
Overrides the Markdown report path for this run. The page is written beside it with the .html
extension.

.PARAMETER Open
Open the Markdown report after the audit finishes.

.PARAMETER NoOpen
Write the page without opening it.

.PARAMETER Help
Display this help and exit without running the audit. The alias -? is supported.

.EXAMPLE
AuditUI

.EXAMPLE
AuditUI -Open

.EXAMPLE
AuditUI -NoOpen

.EXAMPLE
AuditUI -Configuration Release
#>
#requires -Version 5.1
# AUDITUI - AUDIT GENERATION 23.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the audited outcome changes.
# A generation names the set of checks the audit applies. Two projects on the same generation audit
# the same things and their reports compare directly, whatever else differs between the files. A check
# added, removed, or changed in what it reports is a new generation; wording, plumbing, and refactoring
# leave it alone. Every audit script shares one generation number with the convention-test settings,
# and each refuses a configuration written at another generation.
# Generation 11: the truth audit also checks that a deportment field reaches no request, keeps one
# writer, holds no logic and treats no engine data.
# Generation 12: the audit is the standalone counterpart of the convention tests. It binds through the
# shared binder of AuditBinder.cs, walks with its own copy of the strict, truth and boundary walkers,
# holds every hit against its own ledger, and counts every fact the tests gate.
# Generation 13: the strict audit also counts every surface markup line that hooks logic into the
# markup, and every surface member that is not a constructor. The veneer may hold no markup file.
# Generation 14: the strict audit also counts a surface markup line that passes a command parameter,
# a command target or a member path, or that sets a literal tag, as a hook.
# Generation 15: the strict audit also counts a driver line holding a pack URI or naming the surface,
# a driver type deriving from a scaffold type, and a contract ID the surface markup never names.
# An x:Class in surface markup is a hook unless it names a surface type.
# Generation 16: nothing this audit reports changes; the number rises with the structure audit, whose
# Unsealing kind counts engine types on the public members of sealed Deportment types.
# Generation 17: findings take one vocabulary of -ing kinds and plain measure names, and the
# configuration keys follow. What the audit counts is unchanged.
# Generation 18: the truth audit stops five false findings. A literal bool comparison, a name inside
# nameof and a chain of presence tests decide nothing, a break or continue leaves only its loop,
# and a receiver behind ! is still only a receiver.
# It also sees a field passed by ref, a delegate stored from a parameter, an interface event, a static
# Conduct value in driver arithmetic, and a surface loaded through a built URI.
# Generation 19: nothing this audit reports changes; the number rises with the comment audit, whose
# hash line ties every comment file to the sources it describes.
# Generation 20: a truth kind named in the informative map gates nothing while a driver folder
# it waits on holds no source. Mismatching waits on Demeanor, so it only informs until the CUI has code.
# Generation 21: two truth kinds join. Puppeteering reports a control member that requests logic,
# counting no event the control raises. Zeroing reports a literal zero at a gate-bound position,
# or one compared with a gate-reaching member.
# Generation 22: nothing this audit reports changes; the number rises with the structure audit,
# whose Unsealing kind walks every public type of a sealed ring, whatever its prefix.
# Generation 23: the truth audit sees an owned field read from outside its type. A read through a
# member that is not private, such as a sibling reading its owner's alias, is checked like one inside.
[CmdletBinding()]
param(
    [string]$Root,
    [string]$ConfigPath,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration,
    [string]$OutputPath,
    [switch]$Open,
    [switch]$NoOpen,
    [Alias('?')]
    [switch]$Help
)

if ($Help) {
    @'
NAME
    AuditUI.ps1

SYNOPSIS
    Audit the surfaces for anything but calls and the drivers for deciding
    data, the standalone counterpart of the TAuditStrict, TAuditTruth and
    TAuditBoundary convention tests.

SYNTAX
    AuditUI [-Root <path>] [-ConfigPath <path>] [-Configuration <Debug|Release>]
        [-OutputPath <path>] [-Open] [-NoOpen] [-Help]

CONFIGURATION
    AuditUI.json          Every rule value of the three tests, the helper
                          framework, the ledger file and the report location.
    AuditUI.ledger.json   The ceiling of every kind per file, for Strict and Truth.
    AuditUI.html          The page template the HTML report is built from.
    AuditBinder.json      The shared binder: source root, build configuration,
                          host project, frameworks and exclusions.

    The script never reads the test project. A test and this script agree
    because both hold the same values, not because one reads the other.

VERDICT
    A hit is held while its file sits at or under the ceiling of its kind.
    A file above its ceiling fails, and a ceiling above its count is stale
    and fails too. Every counter is one fact the tests gate, and the counters
    sum to zero exactly when those tests pass.

OPTIONS
    -Root <path>
        Project root to audit. Defaults to the parent of the script folder.

    -ConfigPath <path>
        JSON configuration file. Defaults to .\AuditUI.json.

    -Configuration <Debug|Release>
        Build output the binder reads. Defaults to AuditBinder.json.

    -OutputPath <path>
        Markdown report path. By default, the project version is used to
        create a path under report.directory. The page is written beside
        it with the .html extension.

    -Open
        Open the Markdown report after the audit finishes.

    -NoOpen
        Write the page without opening it.

    -Help, -?
        Display this help and exit without running the audit.

OUTPUT
    The console follows scripts\report.md: result, hits by kind and one
    section per counter above zero.
    <report.directory>\<report.prefix>{version}.md lists every hit.
    <report.directory>\<report.prefix>{version}.html shows the same result
    as a page, opened in the default browser unless -NoOpen is given.

EXIT STATUS
    0   Every counter is zero.
    1   A file sits above its ceiling, a ceiling is stale, or a fact failed.

EXAMPLES
    AuditUI
        Audit the current checkout and open the page.

    AuditUI -Open
        Audit and open the Markdown report as well.

    AuditUI -NoOpen
        Audit and write both reports without opening the page.

    AuditUI -Configuration Release
        Bind against the Release build output.
'@ | Write-Host
    exit 0
}

# Under Windows PowerShell 5.1 an advanced script evaluates a parameter default before
# $PSScriptRoot is available to it, so defaults are resolved here instead.
if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Split-Path -Parent $PSScriptRoot
}

if ([string]::IsNullOrWhiteSpace($ConfigPath)) {
    $ConfigPath = Join-Path $PSScriptRoot 'AuditUI.json'
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# dotnet and git write UTF-8. A console still on the OEM code page would show every non-ASCII
# line garbled, so this process reads and writes UTF-8. The calling console keeps its own page.
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$OutputEncoding = [Console]::OutputEncoding

$script:AuditGeneration = 23
$script:Invariant = [System.Globalization.CultureInfo]::InvariantCulture
$script:BinderSource = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'AuditBinder.cs'))

Write-Host "AUDITUI - AUDIT GENERATION $script:AuditGeneration" -ForegroundColor Blue

function Format-Count {
    param([long]$Value)

    return $Value.ToString('N0', $script:Invariant)
}

function Write-AuditSection {
    param([string]$Title)

    Write-Host ''
    Write-Host $Title -ForegroundColor Blue
    Write-Host ('-' * $Title.Length) -ForegroundColor DarkGray
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
    Write-Host (& $format $Header) -ForegroundColor Cyan
    Write-Host (($widths | ForEach-Object { '-' * $_ }) -join '  ') -ForegroundColor Cyan
    foreach ($row in $Rows) {
        Write-Host (& $format ([string[]]$row))
    }
}

function Get-ConfigNode {
    param(
        [Parameter(Mandatory = $true)]$Document,
        [Parameter(Mandatory = $true)][string]$Key
    )

    $node = $Document
    foreach ($segment in ($Key -split '\.')) {
        if ($null -eq $node -or -not ($node.PSObject.Properties.Name -contains $segment)) {
            return $null
        }
        $node = $node.$segment
    }

    return $node
}

function Read-AuditConfig {
    # The helper checks every rule key; this reads the keys the script itself needs.
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "The UI-audit configuration was not found: $Path"
    }

    try {
        $config = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The UI-audit configuration is not valid JSON: $Path`n$($_.Exception.Message)"
    }

    $missing = @(@('generation', 'project', 'helper.framework', 'ledger', 'strict', 'truth', 'boundary',
            'report.directory', 'report.versionFile', 'report.versionKey', 'report.prefix', 'report.consoleItems') |
        Where-Object { $null -eq (Get-ConfigNode -Document $config -Key $_) })
    if ($missing.Count -gt 0) {
        throw "The UI-audit configuration is not valid: $Path`n  missing key '" + ($missing -join "'`n  missing key '") + "'"
    }

    if ([int]$config.generation -ne $script:AuditGeneration) {
        throw "The UI-audit configuration is generation $($config.generation) but this tooling is generation $script:AuditGeneration.`nA generation names the set of checks applied, so the two must match: $Path"
    }

    return $config
}

function Join-AuditPath {
    param(
        [Parameter(Mandatory = $true)][string]$Base,
        [Parameter(Mandatory = $true)][string]$Relative
    )

    if ([System.IO.Path]::IsPathRooted($Relative)) {
        return [System.IO.Path]::GetFullPath($Relative)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $Base $Relative))
}

function Read-ProjectVersion {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Key
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "The version file was not found: $Path"
    }

    $value = [string](Get-ConfigNode -Document (Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json) -Key $Key)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "The version file holds no '$Key': $Path"
    }

    return $value
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

# The program: reads the settings, binds, runs the walkers, holds the hits against the ledger,
# writes the Markdown report and one JSON summary of the counters and the kinds.
$script:HelperProgram = @'
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

if (args.Length != 8)
{
    Console.Error.WriteLine("usage: <config> <root> <binder> <ledger> <output> <report> <version> <page>");
    return 2;
}

string configPath = args[0];
string projectRoot = Path.GetFullPath(args[1]);
string binderPath = args[2];
string ledgerPath = args[3];
string outputPath = args[4];
string reportPath = args[5];
string version = args[6];
string pagePath = args[7];

JsonElement config = JsonDocument.Parse(File.ReadAllText(configPath)).RootElement;
LAuditSettingRead.LAuditLoad(config);
LAuditScopeSetting.LAuditLoad(JsonDocument.Parse(File.ReadAllText(binderPath)).RootElement);
LAuditBind.LAuditBindSet(LAuditBinder.LAuditBinderRead(projectRoot, binderPath));
LAuditLedger.LAuditLoad(ledgerPath);

LAuditStrictRun strict = LAuditStrictRun.LAuditRead(projectRoot);
LAuditTruthRun truth = LAuditTruthRun.LAuditRead(projectRoot);
List<LAuditCounter> counters = [];

// Whether a file above the ceiling of this kind fails: an informative truth kind gates nothing yet.
bool EnforceCheck(string audit, string kind) => audit == "Strict"
    ? LAuditStrictSetting.LAuditStrictEnforced
    : LAuditTruthSetting.LAuditTruthEnforced && truth.LAuditEnforceCheck(kind);
List<LAuditKindRow> kinds = [];

foreach (string kind in LAuditStrictRun.LAuditKinds)
{
    counters.Add(LAuditLedger.LAuditOverRead("Strict", kind, strict.LAuditHits, LAuditStrictSetting.LAuditStrictEnforced, kinds));
}

counters.Add(new LAuditCounter("Strict stale ceilings", LAuditLedger.LAuditStaleRead("Strict", LAuditStrictRun.LAuditKinds, strict.LAuditHits)));
counters.Add(new LAuditCounter("Strict unwalked files", LAuditRow.LAuditFileList(LAuditBind.LAuditCoverRead(strict.LAuditSources))));
counters.Add(new LAuditCounter("Driver disk lines", LAuditStrictRun.LAuditSourceScan(
    projectRoot, LAuditStrictSetting.LAuditDeportmentInclude, LAuditStrictSetting.LAuditDiskPatterns, LAuditStrictSetting.LAuditDiskExempt)));
counters.Add(new LAuditCounter("Surface catalog lines", LAuditStrictRun.LAuditSourceScan(
    projectRoot, LAuditStrictSetting.LAuditVeneerInclude, LAuditStrictSetting.LAuditCatalogPatterns, LAuditStrictSetting.LAuditCatalogExempt)));
counters.Add(new LAuditCounter("Strict stale exemptions", LAuditStrictRun.LAuditExemptRead(
        projectRoot, LAuditStrictSetting.LAuditVeneerInclude, LAuditStrictSetting.LAuditCatalogPatterns, LAuditStrictSetting.LAuditCatalogExempt)
    .Concat(LAuditStrictRun.LAuditExemptRead(
        projectRoot, LAuditStrictSetting.LAuditDeportmentInclude, LAuditStrictSetting.LAuditDiskPatterns, LAuditStrictSetting.LAuditDiskExempt))
    .ToList()));
int strictEnd = counters.Count;

foreach (string kind in LAuditTruthRun.LAuditKinds)
{
    counters.Add(LAuditLedger.LAuditOverRead("Truth", kind, truth.LAuditHits, EnforceCheck("Truth", kind), kinds));
}

counters.Add(new LAuditCounter("Truth stale ceilings", LAuditLedger.LAuditStaleRead("Truth", LAuditTruthRun.LAuditKinds, truth.LAuditHits)));
counters.Add(new LAuditCounter("Truth unwalked files", LAuditRow.LAuditFileList(LAuditBind.LAuditCoverRead(truth.LAuditSources))));
int truthEnd = counters.Count;
counters.AddRange(LAuditBoundaryRun.LAuditRead(projectRoot));

LAuditReport.LAuditSave(reportPath, version, config.GetProperty("generation").GetInt32(), strict, truth, counters, kinds);

using (FileStream stream = File.Create(outputPath))
using (Utf8JsonWriter writer = new(stream))
{
    writer.WriteStartObject();
    writer.WriteStartObject("scanned");
    writer.WriteNumber("surface", strict.LAuditShells);
    writer.WriteNumber("host", strict.LAuditHosts);
    writer.WriteNumber("markup", strict.LAuditMarkups);
    writer.WriteNumber("driver", truth.LAuditSources.Count);
    writer.WriteNumber("strict", strict.LAuditHits.Count);
    writer.WriteNumber("truth", truth.LAuditHits.Count);
    writer.WriteEndObject();
    writer.WriteStartArray("counters");
    foreach (LAuditCounter counter in counters)
    {
        writer.WriteStartObject();
        writer.WriteString("label", counter.LAuditLabel);
        writer.WriteString("meaning", LAuditReport.LAuditMeaningRead(counter.LAuditLabel));
        writer.WriteNumber("value", counter.LAuditRows.Count);
        writer.WriteStartArray("rows");
        foreach (LAuditRow row in counter.LAuditRows)
        {
            writer.WriteStringValue(row.LAuditText);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    writer.WriteEndArray();
    writer.WriteStartArray("kinds");
    foreach (LAuditKindRow row in kinds)
    {
        writer.WriteStartObject();
        writer.WriteString("audit", row.LAuditAudit);
        writer.WriteString("kind", row.LAuditKind);
        writer.WriteNumber("hits", row.LAuditHits);
        writer.WriteNumber("files", row.LAuditFiles);
        writer.WriteNumber("ceiling", row.LAuditCeiling);
        writer.WriteNumber("over", row.LAuditOver);
        writer.WriteEndObject();
    }

    writer.WriteEndArray();
    writer.WriteEndObject();
}

// The page data: the same counters, kinds and hits as one JSON document for the template AuditUI.html.
string[] Strings(JsonElement list) => list.EnumerateArray().Select(item => item.GetString()!).ToArray();
string AuditRead(int index) => index < strictEnd ? "Strict" : index < truthEnd ? "Truth" : "Boundary";
JsonElement strictConfig = config.GetProperty("strict");
JsonElement truthConfig = config.GetProperty("truth");
JsonElement boundaryConfig = config.GetProperty("boundary");
var page = new
{
    project = config.GetProperty("project").GetString(),
    version,
    generation = config.GetProperty("generation").GetInt32(),
    generated = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", System.Globalization.CultureInfo.InvariantCulture),
    report = Path.GetFileName(reportPath),
    strictEnforced = LAuditStrictSetting.LAuditStrictEnforced,
    truthEnforced = LAuditTruthSetting.LAuditTruthEnforced,
    surfaceTypes = strict.LAuditVeneers.Count,
    scanned = new
    {
        surface = strict.LAuditShells,
        host = strict.LAuditHosts,
        markup = strict.LAuditMarkups,
        driver = truth.LAuditSources.Count,
        strict = strict.LAuditHits.Count,
        truth = truth.LAuditHits.Count,
    },
    gates = counters.Select((counter, index) => new
    {
        audit = AuditRead(index),
        status = counter.LAuditRows.Count > 0 ? "FAIL" : "OK",
        gate = counter.LAuditLabel,
        count = counter.LAuditRows.Count,
        meaning = LAuditReport.LAuditMeaningRead(counter.LAuditLabel),
        rows = counter.LAuditRows.Select(row => new
        {
            file = row.LAuditFile,
            line = row.LAuditLine,
            kind = row.LAuditKind,
            entry = row.LAuditEntry,
            hits = row.LAuditHits,
            ceiling = row.LAuditCeiling,
            detail = row.LAuditDetail,
        }),
    }),
    kinds = kinds.Select(row => new
    {
        audit = row.LAuditAudit,
        kind = row.LAuditKind,
        hits = row.LAuditHits,
        files = row.LAuditFiles,
        ceiling = row.LAuditCeiling,
        over = row.LAuditOver,
        enforced = EnforceCheck(row.LAuditAudit, row.LAuditKind),
    }),
    ledger = LAuditLedger.LAuditPlaceRead("Strict", LAuditStrictRun.LAuditKinds, strict.LAuditHits)
        .Select(place => new { audit = "Strict", kind = place.Kind, file = place.File, hits = place.Hits, ceiling = place.Ceiling, enforced = EnforceCheck("Strict", place.Kind) })
        .Concat(LAuditLedger.LAuditPlaceRead("Truth", LAuditTruthRun.LAuditKinds, truth.LAuditHits)
            .Select(place => new { audit = "Truth", kind = place.Kind, file = place.File, hits = place.Hits, ceiling = place.Ceiling, enforced = EnforceCheck("Truth", place.Kind) })),
    above = LAuditLedger.LAuditAboveHits.Select(pair => new
    {
        audit = pair.Audit,
        kind = pair.Hit.LViolationKind,
        file = pair.Hit.LViolationPath,
        line = pair.Hit.LViolationLine,
        name = pair.Hit.LViolationName,
        reason = pair.Hit.LViolationReason,
    }),
    surfaces = LAuditReport.LAuditSurfaceRead(strict)
        .Select(row => new { type = row.Type, hoarding = row.Hoarding, meddling = row.Meddling, prying = row.Prying }),
    fields = LAuditReport.LAuditFieldRead(truth).Select(row => new { field = row.Field, hits = row.Hits }),
    hits = LAuditStrictRun.LAuditKinds.SelectMany(kind => LAuditReport.LAuditKindHitRead(kind, strict.LAuditHits))
        .Select(hit => new { audit = "Strict", hit })
        .Concat(LAuditTruthRun.LAuditKinds.SelectMany(kind => LAuditReport.LAuditKindHitRead(kind, truth.LAuditHits))
            .Select(hit => new { audit = "Truth", hit }))
        .Select(pair => new
        {
            audit = pair.audit,
            kind = pair.hit.LViolationKind,
            file = (string?)pair.hit.LViolationPath,
            line = (int?)pair.hit.LViolationLine,
            name = pair.hit.LViolationName,
            reason = pair.hit.LViolationReason,
        })
        .Concat(counters.Skip(truthEnd).SelectMany(counter => counter.LAuditRows.Select(row => new
        {
            audit = "Boundary",
            kind = counter.LAuditLabel["Boundary ".Length..],
            file = row.LAuditFile,
            line = row.LAuditLine,
            name = row.LAuditEntry ?? row.LAuditKind ?? string.Empty,
            reason = row.LAuditDetail ?? string.Empty,
        }))),
    scope = new
    {
        reach = Strings(strictConfig.GetProperty("reachInclude")),
        veneer = Strings(strictConfig.GetProperty("veneerInclude")),
        deportment = Strings(strictConfig.GetProperty("deportmentInclude")),
        host = Strings(strictConfig.GetProperty("hostInclude")),
        capsule = Strings(truthConfig.GetProperty("capsuleInclude")),
        shell = Strings(truthConfig.GetProperty("shellInclude")),
        truth = Strings(truthConfig.GetProperty("truthInclude")),
        tracked = Strings(boundaryConfig.GetProperty("tracked")),
        logic = Strings(boundaryConfig.GetProperty("logic")),
        excludeSegments = LAuditScopeSetting.LAuditSegments,
        excludeSuffixes = LAuditScopeSetting.LAuditSuffixes,
        excludePrefixes = LAuditScopeSetting.LAuditPrefixes,
        framework = config.GetProperty("helper").GetProperty("framework").GetString(),
        consoleItems = config.GetProperty("report").GetProperty("consoleItems").GetInt32(),
    },
};
File.WriteAllText(pagePath, JsonSerializer.Serialize(page), new UTF8Encoding(false));

return 0;

internal sealed record LViolation(
    string LViolationPath,
    int LViolationLine,
    string LViolationName,
    string LViolationKind,
    string LViolationReason);

internal sealed record LAuditCounter(string LAuditLabel, List<LAuditRow> LAuditRows);

// One finding row: the console and Markdown text, and the fields that text is built from.
internal sealed record LAuditRow(string LAuditText)
{
    public string? LAuditFile { get; init; }

    public int? LAuditLine { get; init; }

    public string? LAuditKind { get; init; }

    public string? LAuditEntry { get; init; }

    public int? LAuditHits { get; init; }

    public int? LAuditCeiling { get; init; }

    public string? LAuditDetail { get; init; }

    public static List<LAuditRow> LAuditFileList(IEnumerable<string> paths) =>
        paths.Select(path => new LAuditRow(path) { LAuditFile = path }).ToList();
}

internal sealed record LAuditKindRow(
    string LAuditAudit, string LAuditKind, int LAuditHits, int LAuditFiles, int LAuditCeiling, int LAuditOver);

internal sealed class LAuditStrictRun
{
    public static readonly string[] LAuditKinds =
        [
            "Hoarding", "Sharing", "Meddling", "Prying", "Overreaching", "Branching", "Homoglyph",
            "Overworking", "Tethering", "Freelancing", "Hardwiring", "Masquerading", "Dangling",
        ];

    private static readonly Regex LAuditLiteralPattern = new(
        @"@?""(?:[^""\\]|\\.)*""|//.*$",
        RegexOptions.Compiled);

    public required IReadOnlyList<LViolation> LAuditHits { get; init; }

    public required IReadOnlyList<string> LAuditVeneers { get; init; }

    public required IReadOnlyList<string> LAuditSources { get; init; }

    public required int LAuditShells { get; init; }

    public required int LAuditHosts { get; init; }

    public required int LAuditMarkups { get; init; }

    public static LAuditStrictRun LAuditRead(string repoRoot)
    {
        IReadOnlyList<string> sources = LAuditScopeSetting.LAuditFileRead(repoRoot, LAuditTruthSetting.LAuditShellInclude);
        IReadOnlyList<string> hosts = LAuditScopeSetting.LAuditFileRead(repoRoot, LAuditStrictSetting.LAuditHostInclude);
        IReadOnlyList<string> markups = LAuditScopeSetting.LAuditFileRead(repoRoot, LAuditStrictSetting.LAuditReachInclude);
        IReadOnlyList<string> drivers = LAuditScopeSetting.LAuditFileRead(repoRoot, LAuditStrictSetting.LAuditDeportmentInclude);
        if (sources.Count == 0 || hosts.Count == 0)
        {
            throw new InvalidOperationException(
                "No tracked shell or host file was enumerated, so the audit would pass vacuously.");
        }

        IReadOnlyList<LViolation> hits = LAuditStrictWalker.LAuditRun(sources, out List<string> veneers)
            .Concat(LAuditHostWalker.LAuditRun(hosts))
            .Concat(LAuditReachWalker.LAuditRun(markups))
            .Concat(LAuditContractWalker.LAuditRun(drivers, markups))
            .Select(hit => hit with
            {
                LViolationPath = Path.GetRelativePath(repoRoot, hit.LViolationPath).Replace('\\', '/')
            })
            .OrderBy(hit => hit.LViolationPath, StringComparer.Ordinal)
            .ThenBy(hit => hit.LViolationLine)
            .ToList();
        veneers.Sort(StringComparer.Ordinal);
        return new LAuditStrictRun
        {
            LAuditHits = hits,
            LAuditVeneers = veneers,
            LAuditSources = [.. sources, .. hosts],
            LAuditShells = sources.Count,
            LAuditHosts = hosts.Count,
            LAuditMarkups = markups.Count,
        };
    }

    public static List<LAuditRow> LAuditExemptRead(
        string repoRoot, IReadOnlyList<string> include, IReadOnlyList<string> forbidden, IReadOnlyList<string> exempt)
    {
        const string spare = "holds no line the exemption spares";
        HashSet<string> used = LAuditSourceScan(repoRoot, include, forbidden, [])
            .Select(hit => Path.GetFileName(hit.LAuditText.Trim().Split(':')[0]))
            .ToHashSet(StringComparer.Ordinal);
        return exempt.Where(name => !used.Contains(name))
            .Select(name => new LAuditRow($"{name} {spare}") { LAuditFile = name, LAuditDetail = spare })
            .ToList();
    }

    public static List<LAuditRow> LAuditSourceScan(
        string repoRoot, IReadOnlyList<string> include, IReadOnlyList<string> forbidden, IReadOnlyList<string> exempt)
    {
        List<LAuditRow> hits = [];
        foreach (string path in LAuditScopeSetting.LAuditFileRead(repoRoot, include))
        {
            if (exempt.Contains(Path.GetFileName(path), StringComparer.Ordinal))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(path);
            for (int index = 0; index < lines.Length; index++)
            {
                string bare = LAuditLiteralPattern.Replace(lines[index], string.Empty);
                foreach (string pattern in forbidden)
                {
                    if (Regex.IsMatch(bare, pattern))
                    {
                        string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                        hits.Add(new LAuditRow($"{relative}:{index + 1} {pattern}")
                        {
                            LAuditFile = relative, LAuditLine = index + 1, LAuditDetail = pattern,
                        });
                    }
                }
            }
        }

        return hits;
    }
}

internal sealed class LAuditTruthRun
{
    public static readonly string[] LAuditKinds =
        [
            "Replaying", "Gatekeeping", "Contesting", "Duplicating", "Tampering", "Misfiring", "Moonlighting",
            "Homoglyph", "Laundering", "Spoonfeeding", "Mismatching", "Puppeteering", "Zeroing",
        ];

    public static readonly string[] LAuditLineKinds =
        ["Tampering", "Moonlighting", "Homoglyph", "Laundering", "Spoonfeeding", "Mismatching"];

    public required IReadOnlyList<LViolation> LAuditHits { get; init; }

    public required IReadOnlyList<string> LAuditSources { get; init; }

    public bool LAuditEnforceCheck(string kind)
    {
        return !LAuditTruthSetting.LAuditTruthInformative.TryGetValue(kind, out string[]? folders)
            || folders.All(folder => LAuditSources.Any(source =>
                LAuditBind.LAuditRelativeRead(source).StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase)));
    }

    public static LAuditTruthRun LAuditRead(string repoRoot)
    {
        IReadOnlyList<string> sources = LAuditScopeSetting.LAuditFileRead(repoRoot, LAuditTruthSetting.LAuditTruthInclude);
        if (sources.Count == 0)
        {
            throw new InvalidOperationException(
                $"No tracked file matches {string.Join(' ', LAuditTruthSetting.LAuditTruthInclude)}.");
        }

        IReadOnlySet<ISymbol> readers = LAuditTruthWalker.LAuditReaderRead(sources);
        List<LViolation> hits = LAuditTruthWalker.LAuditRun(sources)
            .Concat(LAuditMoonlightingWalker.LAuditRun(sources))
            .Concat(LAuditLaunderingWalker.LAuditRun(sources, readers))
            .Select(hit => hit with
            {
                LViolationPath = Path.GetRelativePath(repoRoot, hit.LViolationPath).Replace('\\', '/')
            })
            .ToList();
        return new LAuditTruthRun { LAuditHits = hits, LAuditSources = sources };
    }
}

internal static class LAuditBoundaryRun
{
    public static List<LAuditCounter> LAuditRead(string repoRoot)
    {
        List<LAuditCounter> counters =
        [
            new("Boundary state builds",
                LAuditBoundaryScan(repoRoot, static _ => true, LAuditBoundarySetting.LAuditBoundaryForbidden)),
            new("Boundary state compares", LAuditBoundaryScan(
                repoRoot,
                static name => !LAuditBoundarySetting.LAuditBoundaryConverter.Contains(name, StringComparer.Ordinal),
                LAuditBoundarySetting.LAuditBoundaryState)),
            new("Boundary hidden lines",
                LAuditBoundaryScan(repoRoot, static _ => true, LAuditBoundarySetting.LAuditBoundaryHidden)),
            new("Boundary reflections", LAuditBoundaryScan(
                repoRoot,
                static name => !LAuditBoundarySetting.LAuditBoundaryLoader.Contains(name, StringComparer.Ordinal),
                [LAuditBoundarySetting.LAuditBoundaryReflection])),
            new("Boundary skipped sources", LAuditSkipRead(repoRoot)),
            new("Boundary logic panels", LAuditPanelRead(repoRoot)),
            new("Boundary hold timers", LAuditBoundaryScan(
                repoRoot,
                static name => LAuditBoundarySetting.LAuditBoundaryHold.Any(pattern => Regex.IsMatch(name, pattern)),
                [LAuditBoundarySetting.LAuditBoundaryTimer])),
            new("Boundary stale rows", LAuditExemptRead(repoRoot)),
        ];
        return counters;
    }

    private static List<LAuditRow> LAuditSkipRead(string repoRoot)
    {
        return LAuditRow.LAuditFileList(LAuditBinder.LAuditFileRead(repoRoot, LAuditBoundarySetting.LAuditBoundaryTracked, [], [], [], [])
            .Select(path => Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
            .Where(path => path.Split('/').Any(segment =>
                               LAuditScopeSetting.LAuditSegments.Contains(segment, StringComparer.Ordinal))
                           || LAuditScopeSetting.LAuditSuffixes.Any(suffix =>
                               !suffix.Equals(".md", StringComparison.Ordinal)
                               && path.EndsWith(suffix, StringComparison.Ordinal))
                           || LAuditScopeSetting.LAuditPrefixes.Any(prefix =>
                               Path.GetFileName(path).StartsWith(prefix, StringComparison.Ordinal))));
    }

    private static List<LAuditRow> LAuditPanelRead(string repoRoot)
    {
        IReadOnlyList<string> shells = LAuditTruthSetting.LAuditShellInclude
            .Select(pattern => pattern[..pattern.IndexOf('*')].TrimEnd('/'))
            .Distinct(StringComparer.Ordinal)
            .Select(root => Path.Combine(repoRoot, root.Replace('/', Path.DirectorySeparatorChar))
                            + Path.DirectorySeparatorChar)
            .ToList();
        List<LAuditRow> hits = [];
        foreach (string path in LAuditBinder.LAuditFileRead(
                     repoRoot, LAuditBoundarySetting.LAuditBoundaryLogic, [], LAuditScopeSetting.LAuditSegments, [], []))
        {
            if (shells.Any(shell => path.StartsWith(shell, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(path);
            for (int index = 0; index < lines.Length; index++)
            {
                if (Regex.IsMatch(lines[index], LAuditBoundarySetting.LAuditBoundaryPanel))
                {
                    string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                    hits.Add(new LAuditRow($"{relative}:{index + 1}") { LAuditFile = relative, LAuditLine = index + 1 });
                }
            }
        }

        return hits;
    }

    private static List<LAuditRow> LAuditExemptRead(string repoRoot)
    {
        List<LAuditRow> stale = [];
        HashSet<string> converters = LAuditUsedRead(repoRoot, LAuditBoundarySetting.LAuditBoundaryState);
        stale.AddRange(LAuditBoundarySetting.LAuditBoundaryConverter
            .Where(name => !converters.Contains(name))
            .Select(name => LAuditStaleRow("converter", name, "compares no state")));
        HashSet<string> loaders = LAuditUsedRead(repoRoot, [LAuditBoundarySetting.LAuditBoundaryReflection]);
        stale.AddRange(LAuditBoundarySetting.LAuditBoundaryLoader
            .Where(name => !loaders.Contains(name))
            .Select(name => LAuditStaleRow("loader", name, "reflects nothing")));
        HashSet<string> names = LAuditUsedRead(repoRoot, [string.Empty]);
        stale.AddRange(LAuditBoundarySetting.LAuditBoundaryHold
            .Where(pattern => !names.Any(name => Regex.IsMatch(name, pattern)))
            .Select(pattern => LAuditStaleRow("hold pattern", pattern, "names no shell file")));
        return stale;
    }

    private static LAuditRow LAuditStaleRow(string list, string entry, string detail) =>
        new($"{list} {entry} {detail}") { LAuditKind = list, LAuditEntry = entry, LAuditDetail = detail };

    private static HashSet<string> LAuditUsedRead(string repoRoot, IReadOnlyList<string> forbidden)
    {
        return LAuditBoundaryScan(repoRoot, static _ => true, forbidden)
            .Select(hit => Path.GetFileName(hit.LAuditText.Trim().Split(':')[0]))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static List<LAuditRow> LAuditBoundaryScan(string repoRoot, Func<string, bool> chosen, IReadOnlyList<string> forbidden)
    {
        IReadOnlyList<string> sources = LAuditScopeSetting.LAuditFileRead(
            repoRoot, [.. LAuditTruthSetting.LAuditShellInclude, .. LAuditStrictSetting.LAuditReachInclude]);
        if (sources.Count == 0)
        {
            throw new InvalidOperationException("No tracked shell file was enumerated, so the audit would pass vacuously.");
        }

        List<LAuditRow> hits = [];
        foreach (string path in sources)
        {
            if (!chosen(Path.GetFileName(path)))
            {
                continue;
            }

            string[] lines = File.ReadAllLines(path);
            for (int index = 0; index < lines.Length; index++)
            {
                foreach (string pattern in forbidden)
                {
                    if (Regex.IsMatch(lines[index], pattern))
                    {
                        string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
                        hits.Add(new LAuditRow($"{relative}:{index + 1} {pattern}")
                        {
                            LAuditFile = relative, LAuditLine = index + 1, LAuditDetail = pattern,
                        });
                    }
                }
            }
        }

        return hits;
    }
}

internal static class LAuditLedger
{
    private static readonly Dictionary<string, Dictionary<string, Dictionary<string, int>>> LAuditCeilings =
        new(StringComparer.Ordinal);

    public static readonly List<string> LAuditOverLines = [];

    public static readonly List<(string Audit, LViolation Hit)> LAuditAboveHits = [];

    public static void LAuditLoad(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (JsonProperty audit in document.RootElement.EnumerateObject())
        {
            if (audit.Name is not ("Strict" or "Truth"))
            {
                throw new InvalidOperationException($"The ledger names an unknown audit '{audit.Name}': {path}");
            }

            Dictionary<string, Dictionary<string, int>> ledger = new(StringComparer.Ordinal);
            foreach (JsonProperty kind in audit.Value.EnumerateObject())
            {
                ledger[kind.Name] = kind.Value.EnumerateObject()
                    .ToDictionary(entry => entry.Name, entry => entry.Value.GetInt32(), StringComparer.Ordinal);
            }

            LAuditCeilings[audit.Name] = ledger;
        }
    }

    private static Dictionary<string, Dictionary<string, int>> LAuditLedgerRead(string audit) =>
        LAuditCeilings.GetValueOrDefault(audit) ?? new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);

    public static LAuditCounter LAuditOverRead(
        string audit, string kind, IReadOnlyList<LViolation> hits, bool enforced, List<LAuditKindRow> kinds)
    {
        Dictionary<string, int> ceilings = LAuditLedgerRead(audit).GetValueOrDefault(kind) ?? [];
        List<LAuditRow> over = [];
        List<LViolation> held = hits.Where(hit => hit.LViolationKind == kind).ToList();
        List<IGrouping<string, LViolation>> places = held
            .GroupBy(hit => hit.LViolationPath, StringComparer.Ordinal)
            .OrderBy(place => place.Key, StringComparer.Ordinal)
            .ToList();
        foreach (IGrouping<string, LViolation> place in places)
        {
            int ceiling = ceilings.GetValueOrDefault(place.Key);
            if (place.Count() <= ceiling || !enforced)
            {
                continue;
            }

            over.Add(new LAuditRow($"{place.Key}: {place.Count()} hit(s), ceiling {ceiling}")
            {
                LAuditFile = place.Key, LAuditHits = place.Count(), LAuditCeiling = ceiling,
            });
            LAuditOverLines.Add($"{audit} {kind} {place.Key}: {place.Count()} hit(s), ceiling {ceiling}");
            LAuditOverLines.AddRange(place
                .OrderBy(hit => hit.LViolationLine)
                .Select(hit => $"    :{hit.LViolationLine} `{hit.LViolationName}` {hit.LViolationReason}"));
            LAuditAboveHits.AddRange(place.OrderBy(hit => hit.LViolationLine).Select(hit => (audit, hit)));
        }

        kinds.Add(new LAuditKindRow(audit, kind, held.Count, places.Count, ceilings.Values.Sum(), over.Count));
        return new LAuditCounter($"{audit} {kind} over ceiling", over);
    }

    public static List<LAuditRow> LAuditStaleRead(string audit, IReadOnlyList<string> kinds, IReadOnlyList<LViolation> hits)
    {
        Dictionary<string, Dictionary<string, int>> ledger = LAuditLedgerRead(audit);
        SortedDictionary<string, SortedDictionary<string, int>> counts = new(StringComparer.Ordinal);
        foreach (string kind in kinds)
        {
            counts[kind] = new SortedDictionary<string, int>(hits
                .Where(hit => hit.LViolationKind == kind)
                .GroupBy(hit => hit.LViolationPath, StringComparer.Ordinal)
                .ToDictionary(place => place.Key, place => place.Count(), StringComparer.Ordinal),
                StringComparer.Ordinal);
        }

        List<LAuditRow> stale = [];
        foreach ((string kind, Dictionary<string, int> ceilings) in ledger.OrderBy(
                     pair => pair.Key, StringComparer.Ordinal))
        {
            SortedDictionary<string, int> found = counts.GetValueOrDefault(kind) ?? [];
            if (!kinds.Contains(kind, StringComparer.Ordinal))
            {
                stale.Add(new LAuditRow($"{kind} is no kind this audit counts")
                {
                    LAuditKind = kind, LAuditDetail = "is no kind this audit counts",
                });
                continue;
            }

            stale.AddRange(ceilings
                .Where(pair => found.GetValueOrDefault(pair.Key) < pair.Value)
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new LAuditRow(
                    $"{kind} {pair.Key}: {found.GetValueOrDefault(pair.Key)} hit(s), ceiling {pair.Value}")
                {
                    LAuditKind = kind, LAuditFile = pair.Key, LAuditHits = found.GetValueOrDefault(pair.Key), LAuditCeiling = pair.Value,
                }));
        }

        return stale;
    }

    public static List<(string Kind, string File, int Hits, int Ceiling)> LAuditPlaceRead(
        string audit, IReadOnlyList<string> kinds, IReadOnlyList<LViolation> hits)
    {
        Dictionary<string, Dictionary<string, int>> ledger = LAuditLedgerRead(audit);
        List<(string Kind, string File, int Hits, int Ceiling)> places = [];
        foreach (string kind in kinds)
        {
            Dictionary<string, int> ceilings = ledger.GetValueOrDefault(kind) ?? [];
            Dictionary<string, int> found = hits
                .Where(hit => hit.LViolationKind == kind)
                .GroupBy(hit => hit.LViolationPath, StringComparer.Ordinal)
                .ToDictionary(place => place.Key, place => place.Count(), StringComparer.Ordinal);
            places.AddRange(ceilings.Keys.Union(found.Keys, StringComparer.Ordinal)
                .OrderBy(file => file, StringComparer.Ordinal)
                .Select(file => (kind, file, found.GetValueOrDefault(file), ceilings.GetValueOrDefault(file))));
        }

        return places;
    }
}

internal static class LAuditReport
{
    public static void LAuditSave(
        string path,
        string version,
        int generation,
        LAuditStrictRun strict,
        LAuditTruthRun truth,
        IReadOnlyList<LAuditCounter> counters,
        IReadOnlyList<LAuditKindRow> kinds)
    {
        StringBuilder text = new();
        text.Append($"# UI audit {version}\n\n");
        text.Append($"- Generation: {generation}\n");
        text.Append($"- Strict enforced: {LAuditStrictSetting.LAuditStrictEnforced}\n");
        text.Append($"- Truth enforced: {LAuditTruthSetting.LAuditTruthEnforced}\n");
        foreach ((string kind, string[] folders) in LAuditTruthSetting.LAuditTruthInformative
                     .Where(pair => !truth.LAuditEnforceCheck(pair.Key)))
        {
            text.Append($"- Truth informative: {kind}, until {string.Join(", ", folders)} holds a source\n");
        }
        text.Append($"- Surface types: {strict.LAuditVeneers.Count}\n");
        text.Append($"- Strict hits: {strict.LAuditHits.Count}\n");
        text.Append($"- Truth hits: {truth.LAuditHits.Count}\n");
        text.Append('\n');
        text.Append("A hit the ledger holds passes; a file above its ceiling fails, and so does a ceiling above its count.\n");
        text.Append("The ceilings live in `scripts/AuditUI.ledger.json` and the rules in `scripts/AuditUI.json`.\n");

        text.Append("\n## Counters\n\n| Counter | Count |\n|---|---:|\n");
        foreach (LAuditCounter counter in counters)
        {
            text.Append($"| {counter.LAuditLabel} | {counter.LAuditRows.Count} |\n");
        }

        text.Append("\n## Hits by kind\n\n| Audit | Kind | Hits | Files | Ceiling | Over |\n|---|---|---:|---:|---:|---:|\n");
        foreach (LAuditKindRow row in kinds)
        {
            text.Append($"| {row.LAuditAudit} | {row.LAuditKind} | {row.LAuditHits} | {row.LAuditFiles} | {row.LAuditCeiling} | {row.LAuditOver} |\n");
        }

        text.Append($"| | Total | {kinds.Sum(row => row.LAuditHits)} | {kinds.Sum(row => row.LAuditFiles)} | {kinds.Sum(row => row.LAuditCeiling)} | {kinds.Sum(row => row.LAuditOver)} |\n");

        foreach (LAuditCounter counter in counters.Where(counter => counter.LAuditRows.Count > 0))
        {
            text.Append($"\n## {counter.LAuditLabel} ({counter.LAuditRows.Count})\n\n");
            foreach (LAuditRow row in counter.LAuditRows)
            {
                text.Append($"- {row.LAuditText}\n");
            }
        }

        if (LAuditLedger.LAuditOverLines.Count > 0)
        {
            text.Append("\n## Hits above a ceiling\n\n```\n");
            foreach (string line in LAuditLedger.LAuditOverLines)
            {
                text.Append(line).Append('\n');
            }

            text.Append("```\n");
        }

        text.Append("\n## Surface types by member\n\n| Class | Hoarding | Meddling | Prying |\n|---|---|---|---|\n");
        foreach ((string veneer, int hoarding, int meddling, int prying) in LAuditSurfaceRead(strict))
        {
            text.Append($"| {veneer} | {hoarding} | {meddling} | {prying} |\n");
        }

        text.Append("\n## Driver fields by hit count\n\n| Field | Hits |\n|---|---|\n");
        foreach ((string field, int count) in LAuditFieldRead(truth))
        {
            text.Append($"| `{field}` | {count} |\n");
        }

        LAuditHitAppend(text, "Strict", LAuditStrictRun.LAuditKinds, strict.LAuditHits);
        LAuditHitAppend(text, "Truth", LAuditTruthRun.LAuditKinds, truth.LAuditHits);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
    }

    private static void LAuditHitAppend(StringBuilder text, string audit, IReadOnlyList<string> kinds, IReadOnlyList<LViolation> hits)
    {
        foreach (string kind in kinds)
        {
            List<LViolation> found = LAuditKindHitRead(kind, hits);
            text.Append($"\n## {audit} {kind} ({found.Count})\n\n");
            foreach (LViolation hit in found)
            {
                text.Append($"- `{hit.LViolationPath}:{hit.LViolationLine}` `{hit.LViolationName}` {hit.LViolationReason}\n");
            }
        }
    }

    public static List<LViolation> LAuditKindHitRead(string kind, IReadOnlyList<LViolation> hits)
    {
        return hits
            .Where(hit => string.Equals(hit.LViolationKind, kind, StringComparison.Ordinal))
            .OrderBy(hit => hit.LViolationPath, StringComparer.Ordinal)
            .ThenBy(hit => hit.LViolationLine)
            .ToList();
    }

    public static List<(string Type, int Hoarding, int Meddling, int Prying)> LAuditSurfaceRead(LAuditStrictRun strict)
    {
        return strict.LAuditVeneers
            .Select(veneer => (veneer,
                LAuditClassRead(strict.LAuditHits, veneer, "Hoarding"),
                LAuditClassRead(strict.LAuditHits, veneer, "Meddling"),
                LAuditClassRead(strict.LAuditHits, veneer, "Prying")))
            .ToList();
    }

    public static List<(string Field, int Hits)> LAuditFieldRead(LAuditTruthRun truth)
    {
        return truth.LAuditHits
            .Where(hit => !LAuditTruthRun.LAuditLineKinds.Contains(hit.LViolationKind, StringComparer.Ordinal))
            .GroupBy(hit => hit.LViolationName, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => (group.Key, group.Count()))
            .ToList();
    }

    private static readonly Dictionary<string, string> LAuditMeanings = new(StringComparer.Ordinal)
    {
        ["Strict stale ceilings"] = "Strict ledger ceilings set above their hits",
        ["Strict unwalked files"] = "files the Strict walker did not cover",
        ["Driver disk lines"] = "disk access lines in deportment drivers",
        ["Surface catalog lines"] = "catalog lookup lines in veneer surfaces",
        ["Strict stale exemptions"] = "disk or catalog exemptions that match nothing",
        ["Truth stale ceilings"] = "Truth ledger ceilings set above their hits",
        ["Truth unwalked files"] = "files the Truth walker did not cover",
        ["Boundary state builds"] = "lines that build forbidden state types",
        ["Boundary state compares"] = "state comparisons outside converters",
        ["Boundary hidden lines"] = "lines that hide state behind visibility",
        ["Boundary reflections"] = "reflection calls outside loaders",
        ["Boundary skipped sources"] = "sources the boundary scan skipped",
        ["Boundary logic panels"] = "panels that carry logic",
        ["Boundary hold timers"] = "timers in hold files",
        ["Boundary stale rows"] = "boundary exemption rows that match nothing",
    };

    public static string LAuditMeaningRead(string label)
    {
        if (LAuditMeanings.TryGetValue(label, out string? meaning))
        {
            return meaning;
        }

        Match over = Regex.Match(label, @"^(\w+) (\w+) over ceiling$", RegexOptions.IgnoreCase);
        return over.Success ? $"{over.Groups[1].Value} {over.Groups[2].Value} hits above the ledger ceiling" : label;
    }

    private static int LAuditClassRead(IReadOnlyList<LViolation> hits, string type, string kind)
    {
        return hits.Count(hit => string.Equals(hit.LViolationKind, kind, StringComparison.Ordinal)
            && hit.LViolationName.StartsWith(type + ".", StringComparison.Ordinal));
    }
}
'@

# The walkers: the binder queries, the settings and the strict, host, reach, truth, moonlighting
# and laundering walkers, each one rule for rule the counterpart of its convention-test counterpart.
$script:HelperWalker = @'
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

internal static class LAuditBind
{
    private const string LAuditBinderSource = "src/";

    private const string LAuditShellSide = "shell";

    private const string LAuditConductSide = "conduct";

    private const string LAuditEngineSide = "engine";

    private static readonly string[] LAuditLogicSides = [LAuditConductSide, LAuditEngineSide];

    private static readonly Dictionary<SyntaxTree, SemanticModel> LAuditModels = [];

    private static readonly Dictionary<SyntaxNode, ISymbol?> LAuditSymbols = [];

    private static LAuditBinder? LAuditBound;

    public static void LAuditBindSet(LAuditBinder binder) => LAuditBound = binder;

    public static string LAuditRoot => LAuditBound!.LAuditRoot;

    public static IReadOnlyList<SyntaxTree> LAuditTrees => LAuditBound!.LAuditTrees;

    public static CSharpCompilation LAuditCompilation => LAuditBound!.LAuditCompilation;

    public static SemanticModel LAuditModelRead(SyntaxTree tree)
    {
        lock (LAuditModels)
        {
            if (!LAuditModels.TryGetValue(tree, out SemanticModel? model))
            {
                model = LAuditCompilation.GetSemanticModel(tree, true);
                LAuditModels[tree] = model;
            }

            return model;
        }
    }

    public static SemanticModel LAuditModelRead(SyntaxNode node) => LAuditModelRead(node.SyntaxTree);

    public static IReadOnlyList<SyntaxNode> LAuditWalkRead(IReadOnlyList<string> sourcePaths)
    {
        HashSet<string> chosen = new(sourcePaths.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
        return LAuditTrees
            .Where(tree => chosen.Contains(Path.GetFullPath(tree.FilePath)))
            .Select(tree => tree.GetRoot())
            .ToList();
    }

    public static List<string> LAuditCoverRead(IReadOnlyList<string> sourcePaths)
    {
        HashSet<string> walked = new(
            LAuditTrees.Select(tree => Path.GetFullPath(tree.FilePath)), StringComparer.OrdinalIgnoreCase);
        return sourcePaths
            .Where(path => !walked.Contains(Path.GetFullPath(path)))
            .Select(path => LAuditRelativeRead(path))
            .ToList();
    }

    public static bool LAuditWalkCheck(SyntaxNode root)
    {
        string relative = LAuditRelativeRead(root.SyntaxTree.FilePath);
        return LAuditRootRead(LAuditTruthSetting.LAuditTruthInclude)
            .Any(folder => relative.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<string> LAuditRootRead(IEnumerable<string> patterns)
    {
        return patterns
            .Select(pattern => pattern[..pattern.IndexOf('*')].TrimEnd('/'))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public static string LAuditRelativeRead(string path) =>
        Path.GetRelativePath(LAuditRoot, path).Replace('\\', '/');

    public static bool LAuditDataCheck(INamedTypeSymbol type) =>
        type.TypeKind is TypeKind.Enum or TypeKind.Struct or TypeKind.Delegate || type.IsRecord;

    public static ISymbol? LAuditSymbolRead(SemanticModel model, SimpleNameSyntax name)
    {
        SymbolInfo info = model.GetSymbolInfo(name);
        return info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
    }

    public static ISymbol? LAuditSymbolRead(SyntaxNode node)
    {
        if (node is ArgumentSyntax argument)
        {
            node = argument.Expression;
        }

        lock (LAuditSymbols)
        {
            if (LAuditSymbols.TryGetValue(node, out ISymbol? known))
            {
                return known;
            }
        }

        ISymbol? resolved = LAuditSymbolResolve(node);
        lock (LAuditSymbols)
        {
            LAuditSymbols[node] = resolved;
        }

        return resolved;
    }

    public static ITypeSymbol? LAuditTypeRead(SyntaxNode node)
    {
        SemanticModel model = LAuditModelRead(node);
        if (node is ArgumentSyntax argument)
        {
            node = argument.Expression;
        }

        if (node is ExpressionSyntax expression)
        {
            ITypeSymbol? type = model.GetTypeInfo(expression).Type;
            if (type is not null && type.TypeKind != TypeKind.Error)
            {
                return type;
            }
        }

        return LAuditSymbolRead(node) switch
        {
            ILocalSymbol local => local.Type,
            IParameterSymbol parameter => parameter.Type,
            IFieldSymbol field => field.Type,
            IPropertySymbol property => property.Type,
            IMethodSymbol method => method.ReturnType,
            ITypeSymbol type => type,
            _ => null
        };
    }

    public static string? LAuditSourceRead(INamedTypeSymbol type)
    {
        Location? source = type.Locations.FirstOrDefault(location => location.IsInSource);
        return source?.SourceTree is null ? null : LAuditRelativeRead(source.SourceTree.FilePath);
    }

    public static bool LAuditLogicCheck(SyntaxNode node)
    {
        ISymbol? symbol = LAuditSymbolRead(node);
        return LAuditLogicCheck(symbol) || LAuditLogicCheck(LAuditTypeRead(node));
    }

    public static bool LAuditLogicCheck(ISymbol? symbol) => LAuditDepthCheck(symbol, LAuditLogicSides);

    public static bool LAuditLogicCheck(ITypeSymbol? type) => LAuditDepthCheck(type, LAuditLogicSides);

    public static bool LAuditEngineCheck(SyntaxNode node)
    {
        return LAuditDepthCheck(LAuditSymbolRead(node), [LAuditEngineSide])
               || LAuditDepthCheck(LAuditTypeRead(node), [LAuditEngineSide]);
    }

    public static bool LAuditEngineCheck(ISymbol? symbol) => LAuditDepthCheck(symbol, [LAuditEngineSide]);

    public static bool LAuditEngineCheck(ITypeSymbol? type) => LAuditDepthCheck(type, [LAuditEngineSide]);

    public static bool LAuditConductCheck(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol named && LAuditSideRead(named) == LAuditConductSide
               && named.TypeArguments.All(LAuditConductCheck);
    }

    public static bool LAuditShellCheck(ITypeSymbol? type) => LAuditDepthCheck(type, [LAuditShellSide]);

    public static bool LAuditSurfaceCheck(ITypeSymbol? type)
    {
        string? source = type is INamedTypeSymbol named ? LAuditSourceRead(named.OriginalDefinition) : null;
        return source is not null && LAuditRootRead(LAuditStrictSetting.LAuditVeneerInclude)
            .Any(folder => source.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static bool LAuditDepthCheck(ISymbol? symbol, string[] sides)
    {
        return symbol switch
        {
            null => false,
            ILocalSymbol local => LAuditDepthCheck(local.Type, sides),
            IParameterSymbol parameter => LAuditDepthCheck(parameter.Type, sides),
            ITypeSymbol type => LAuditDepthCheck(type, sides),
            _ => sides.Contains(LAuditSideRead(symbol.ContainingType))
        };
    }

    private static bool LAuditDepthCheck(ITypeSymbol? type, string[] sides)
    {
        return type switch
        {
            null => false,
            IArrayTypeSymbol array => LAuditDepthCheck(array.ElementType, sides),
            INamedTypeSymbol named => sides.Contains(LAuditSideRead(named))
                                      || named.TypeArguments.Any(argument => LAuditDepthCheck(argument, sides)),
            _ => false
        };
    }

    public static bool LAuditControlCheck(ITypeSymbol? type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (LAuditTruthSetting.LAuditControlBases.Contains(current.ToDisplayString(), StringComparer.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool LAuditMemberCheck(ISymbol? symbol, IReadOnlyList<string> members)
    {
        return symbol?.ContainingType is { } owner
               && members.Contains(
                   $"{owner.OriginalDefinition.ToDisplayString()}.{symbol.Name}", StringComparer.Ordinal);
    }

    public static bool LAuditNamedCheck(ITypeSymbol? type, IReadOnlyList<string> names)
    {
        return type is not null && names.Contains(type.Name, StringComparer.Ordinal);
    }

    public static string LAuditLabelRead(ISymbol symbol)
    {
        return symbol.ContainingType is null
            ? symbol.Name
            : $"{symbol.ContainingType.Name}.{symbol.Name}";
    }

    public static IReadOnlySet<string> LAuditDeportmentRead()
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        INamespaceSymbol? space = LAuditCompilation.Assembly.GlobalNamespace;
        foreach (string part in LAuditStrictSetting.LAuditDeportmentNamespace.Split('.'))
        {
            space = space?.GetNamespaceMembers()
                .FirstOrDefault(member => string.Equals(member.Name, part, StringComparison.Ordinal));
        }

        foreach (INamedTypeSymbol type in space?.GetTypeMembers() ?? [])
        {
            names.Add(type.Name);
            names.UnionWith(type.GetMembers().Select(member => member.Name));
        }

        Stack<INamespaceOrTypeSymbol> pending = new([LAuditCompilation.Assembly.GlobalNamespace]);
        while (pending.TryPop(out INamespaceOrTypeSymbol? current))
        {
            foreach (INamespaceOrTypeSymbol child in current.GetMembers().OfType<INamespaceOrTypeSymbol>())
            {
                pending.Push(child);
            }

            if (current is INamedTypeSymbol deeper && LAuditLogicSides.Contains(LAuditSideRead(deeper)))
            {
                names.Remove(deeper.Name);
                names.ExceptWith(deeper.GetMembers().Select(member => member.Name));
            }
        }

        return names;
    }

    private static string? LAuditSideRead(INamedTypeSymbol? type)
    {
        string? source = type is null ? null : LAuditSourceRead(type.OriginalDefinition);
        if (source is null)
        {
            return null;
        }

        if (LAuditRootRead(LAuditTruthSetting.LAuditShellInclude)
            .Any(folder => source.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase)))
        {
            return LAuditShellSide;
        }

        return source.StartsWith(LAuditTruthSetting.LAuditConductRoot + "/", StringComparison.OrdinalIgnoreCase)
            ? LAuditConductSide
            : LAuditEngineSide;
    }

    private static ISymbol? LAuditSymbolResolve(SyntaxNode node)
    {
        SemanticModel model = LAuditModelRead(node);
        ISymbol? symbol = node is ExpressionSyntax ? null : model.GetDeclaredSymbol(node);
        if (symbol is null)
        {
            SymbolInfo info = model.GetSymbolInfo(node);
            symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
        }

        return symbol?.OriginalDefinition;
    }
}

internal static class LAuditSettingRead
{
    public static readonly Dictionary<string, string[]> LAuditKeys = new(StringComparer.Ordinal)
    {
        [""] = ["generation", "project", "helper", "ledger", "strict", "truth", "boundary", "report"],
        ["helper"] = ["framework"],
        ["strict"] =
        [
            "enforced", "reachInclude", "veneerInclude", "deportmentInclude", "hostInclude", "deportmentNamespace",
            "queryTypes", "catalogPatterns", "catalogExempt", "diskPatterns", "diskExempt",
            "overreachingNamespaces", "branchingElements", "branchingSlots", "tetheringElements", "tetheringSlots",
            "tetheringExtensions", "tetheringTypes", "tetheringLiterals", "veneerNamespace", "contractType",
            "hardwiringMarkers", "masqueradingTypes", "contractIds"
        ],
        ["truth"] =
        [
            "enforced", "stateSuffix", "bulletinType", "conductRoot", "capsuleInclude", "shellInclude", "truthInclude",
            "controlBases", "orderVerbs", "fillVerbs", "requestPrefix", "sendRoots", "clockTypes", "consoleInput",
            "dialogTypes", "delayMembers", "inputMembers", "focusMembers", "truthHandles", "moonlightingVerbs",
            "informative", "inputBase", "controlPrefixes"
        ],
        ["boundary"] =
        [
            "forbidden", "state", "converter", "hidden", "loader", "reflection", "timer", "hold", "panel", "tracked",
            "logic"
        ],
        ["report"] = ["directory", "versionFile", "versionKey", "prefix", "consoleItems"],
    };

    public static void LAuditLoad(JsonElement config)
    {
        List<string> problems = [];
        foreach ((string section, string[] keys) in LAuditKeys)
        {
            JsonElement node = config;
            if (section.Length > 0 && !config.TryGetProperty(section, out node))
            {
                continue;
            }

            if (node.ValueKind != JsonValueKind.Object)
            {
                problems.Add($"'{section}' is not an object");
                continue;
            }

            string where = section.Length == 0 ? "" : section + ".";
            problems.AddRange(keys.Where(key => !node.TryGetProperty(key, out _)).Select(key => $"missing key '{where}{key}'"));
            problems.AddRange(node.EnumerateObject().Select(item => item.Name)
                .Where(name => !keys.Contains(name, StringComparer.Ordinal))
                .Select(name => $"unknown key '{where}{name}'"));
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException("The UI-audit configuration is not valid:\n  " + string.Join("\n  ", problems));
        }

        JsonElement strict = config.GetProperty("strict");
        LAuditStrictSetting.LAuditStrictEnforced = strict.GetProperty("enforced").GetBoolean();
        LAuditStrictSetting.LAuditReachInclude = LAuditListRead(strict, "reachInclude");
        LAuditStrictSetting.LAuditVeneerInclude = LAuditListRead(strict, "veneerInclude");
        LAuditStrictSetting.LAuditDeportmentInclude = LAuditListRead(strict, "deportmentInclude");
        LAuditStrictSetting.LAuditHostInclude = LAuditListRead(strict, "hostInclude");
        LAuditStrictSetting.LAuditDeportmentNamespace = strict.GetProperty("deportmentNamespace").GetString()!;
        LAuditStrictSetting.LAuditQueryTypes = LAuditListRead(strict, "queryTypes");
        LAuditStrictSetting.LAuditCatalogPatterns = LAuditListRead(strict, "catalogPatterns");
        LAuditStrictSetting.LAuditCatalogExempt = LAuditListRead(strict, "catalogExempt");
        LAuditStrictSetting.LAuditDiskPatterns = LAuditListRead(strict, "diskPatterns");
        LAuditStrictSetting.LAuditDiskExempt = LAuditListRead(strict, "diskExempt");
        LAuditStrictSetting.LAuditOverreachingNamespaces = LAuditListRead(strict, "overreachingNamespaces");
        LAuditStrictSetting.LAuditBranchingElements = LAuditListRead(strict, "branchingElements");
        LAuditStrictSetting.LAuditBranchingSlots = LAuditListRead(strict, "branchingSlots");
        LAuditStrictSetting.LAuditTetheringElements = LAuditListRead(strict, "tetheringElements");
        LAuditStrictSetting.LAuditTetheringSlots = LAuditListRead(strict, "tetheringSlots");
        LAuditStrictSetting.LAuditTetheringExtensions = LAuditListRead(strict, "tetheringExtensions");
        LAuditStrictSetting.LAuditTetheringTypes = LAuditListRead(strict, "tetheringTypes");
        LAuditStrictSetting.LAuditTetheringLiterals = LAuditListRead(strict, "tetheringLiterals");
        LAuditStrictSetting.LAuditVeneerNamespace = strict.GetProperty("veneerNamespace").GetString()!;
        LAuditStrictSetting.LAuditContractType = strict.GetProperty("contractType").GetString()!;
        LAuditStrictSetting.LAuditHardwiringMarkers = LAuditListRead(strict, "hardwiringMarkers");
        LAuditStrictSetting.LAuditMasqueradingTypes = LAuditListRead(strict, "masqueradingTypes");
        LAuditStrictSetting.LAuditContractIds = LAuditListRead(strict, "contractIds");

        JsonElement truth = config.GetProperty("truth");
        LAuditTruthSetting.LAuditTruthEnforced = truth.GetProperty("enforced").GetBoolean();
        LAuditTruthSetting.LAuditStateSuffix = truth.GetProperty("stateSuffix").GetString()!;
        LAuditTruthSetting.LAuditBulletinType = truth.GetProperty("bulletinType").GetString()!;
        LAuditTruthSetting.LAuditConductRoot = truth.GetProperty("conductRoot").GetString()!;
        LAuditTruthSetting.LAuditCapsuleInclude = LAuditListRead(truth, "capsuleInclude");
        LAuditTruthSetting.LAuditShellInclude =
            [.. LAuditListRead(truth, "shellInclude"), .. LAuditTruthSetting.LAuditCapsuleInclude];
        LAuditTruthSetting.LAuditTruthInclude = LAuditListRead(truth, "truthInclude");
        LAuditTruthSetting.LAuditControlBases = LAuditListRead(truth, "controlBases");
        LAuditTruthSetting.LAuditOrderVerbs = LAuditListRead(truth, "orderVerbs");
        LAuditTruthSetting.LAuditFillVerbs = LAuditListRead(truth, "fillVerbs");
        LAuditTruthSetting.LAuditRequestPrefix = truth.GetProperty("requestPrefix").GetString()!;
        LAuditTruthSetting.LAuditSendRoots = LAuditListRead(truth, "sendRoots");
        LAuditTruthSetting.LAuditClockTypes = LAuditListRead(truth, "clockTypes");
        LAuditTruthSetting.LAuditConsoleInput = LAuditListRead(truth, "consoleInput");
        LAuditTruthSetting.LAuditDialogTypes = LAuditListRead(truth, "dialogTypes");
        LAuditTruthSetting.LAuditDelayMembers = LAuditListRead(truth, "delayMembers");
        LAuditTruthSetting.LAuditInputBase = truth.GetProperty("inputBase").GetString()!;
        LAuditTruthSetting.LAuditInputMembers = LAuditListRead(truth, "inputMembers");
        LAuditTruthSetting.LAuditFocusMembers = LAuditListRead(truth, "focusMembers");
        LAuditTruthSetting.LAuditTruthHandles = LAuditListRead(truth, "truthHandles");
        LAuditTruthSetting.LAuditMoonlightingVerbs = LAuditListRead(truth, "moonlightingVerbs");
        JsonElement informative = truth.GetProperty("informative");
        LAuditTruthSetting.LAuditTruthInformative = informative.EnumerateObject()
            .ToDictionary(kind => kind.Name, kind => LAuditListRead(informative, kind.Name), StringComparer.Ordinal);
        LAuditTruthSetting.LAuditControlPrefix = truth.GetProperty("controlPrefixes").EnumerateObject()
            .ToDictionary(ring => ring.Name, ring => ring.Value.GetString()!, StringComparer.Ordinal);

        JsonElement boundary = config.GetProperty("boundary");
        LAuditBoundarySetting.LAuditBoundaryForbidden = LAuditListRead(boundary, "forbidden");
        LAuditBoundarySetting.LAuditBoundaryState = LAuditListRead(boundary, "state");
        LAuditBoundarySetting.LAuditBoundaryConverter = LAuditListRead(boundary, "converter");
        LAuditBoundarySetting.LAuditBoundaryHidden = LAuditListRead(boundary, "hidden");
        LAuditBoundarySetting.LAuditBoundaryLoader = LAuditListRead(boundary, "loader");
        LAuditBoundarySetting.LAuditBoundaryReflection = boundary.GetProperty("reflection").GetString()!;
        LAuditBoundarySetting.LAuditBoundaryTimer = boundary.GetProperty("timer").GetString()!;
        LAuditBoundarySetting.LAuditBoundaryHold = LAuditListRead(boundary, "hold");
        LAuditBoundarySetting.LAuditBoundaryPanel = boundary.GetProperty("panel").GetString()!;
        LAuditBoundarySetting.LAuditBoundaryTracked = LAuditListRead(boundary, "tracked");
        LAuditBoundarySetting.LAuditBoundaryLogic = LAuditListRead(boundary, "logic");
    }

    public static string[] LAuditListRead(JsonElement node, string key) =>
        node.GetProperty(key).EnumerateArray().Select(item => item.GetString()!).ToArray();
}

internal static class LAuditStrictSetting
{
    public static bool LAuditStrictEnforced;
    public static string[] LAuditReachInclude = [];
    public static string[] LAuditVeneerInclude = [];
    public static string[] LAuditDeportmentInclude = [];
    public static string[] LAuditHostInclude = [];
    public static string LAuditDeportmentNamespace = "";
    public static string[] LAuditQueryTypes = [];
    public static string[] LAuditCatalogPatterns = [];
    public static string[] LAuditCatalogExempt = [];
    public static string[] LAuditDiskPatterns = [];
    public static string[] LAuditDiskExempt = [];
    public static string[] LAuditOverreachingNamespaces = [];
    public static string[] LAuditBranchingElements = [];
    public static string[] LAuditBranchingSlots = [];
    public static string[] LAuditTetheringElements = [];
    public static string[] LAuditTetheringSlots = [];
    public static string[] LAuditTetheringExtensions = [];
    public static string[] LAuditTetheringTypes = [];
    public static string[] LAuditTetheringLiterals = [];
    public static string LAuditVeneerNamespace = "";
    public static string LAuditContractType = "";
    public static string[] LAuditHardwiringMarkers = [];
    public static string[] LAuditMasqueradingTypes = [];
    public static string[] LAuditContractIds = [];
}

internal static class LAuditContractWalker
{
    public static IReadOnlyList<LViolation> LAuditRun(
        IReadOnlyList<string> driverPaths, IEnumerable<string> markupPaths)
    {
        List<LViolation> violations = [];
        IReadOnlySet<string> ids = LAuditIdRead(markupPaths);
        foreach (string path in driverPaths)
        {
            LAuditHardwiringScan(path, violations);
        }

        foreach (SyntaxNode root in LAuditBind.LAuditWalkRead(driverPaths))
        {
            LAuditLoadScan(root, violations);
            LAuditMasqueradingScan(root, violations);
            LAuditDanglingScan(root, ids, violations);
        }

        return violations;
    }

    private static void LAuditLoadScan(SyntaxNode root, List<LViolation> violations)
    {
        SemanticModel model = LAuditBind.LAuditModelRead(root);
        foreach (InvocationExpressionSyntax call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol { Name: "LoadComponent" } method
                || method.ContainingType?.ToDisplayString() != "System.Windows.Application")
            {
                continue;
            }

            foreach (ArgumentSyntax argument in call.ArgumentList.Arguments)
            {
                if (model.GetTypeInfo(argument.Expression).Type?.ToDisplayString() != "System.Uri"
                    || argument.Expression is BaseObjectCreationExpressionSyntax
                    {
                        ArgumentList.Arguments: [{ Expression: LiteralExpressionSyntax { Token.Value: string } }, ..]
                    })
                {
                    continue;
                }

                violations.Add(new LViolation(
                    call.SyntaxTree.FilePath,
                    argument.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    argument.Expression.ToString(),
                    "Hardwiring",
                    "driver loads the surface through a built URI"));
            }
        }
    }

    private static void LAuditHardwiringScan(string path, List<LViolation> violations)
    {
        string[] lines = File.ReadAllLines(path);
        for (int index = 0; index < lines.Length; index++)
        {
            string? marker = LAuditStrictSetting.LAuditHardwiringMarkers
                .FirstOrDefault(item => lines[index].Contains(item, StringComparison.Ordinal));
            if (marker is not null)
            {
                violations.Add(new LViolation(path, index + 1, marker, "Hardwiring", "driver line names the surface"));
            }
        }
    }

    private static void LAuditMasqueradingScan(SyntaxNode root, List<LViolation> violations)
    {
        foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (LAuditBind.LAuditSymbolRead(type) is not INamedTypeSymbol symbol)
            {
                continue;
            }

            for (INamedTypeSymbol? shape = symbol.BaseType; shape is not null; shape = shape.BaseType)
            {
                string name = shape.OriginalDefinition.ToDisplayString();
                if (LAuditStrictSetting.LAuditMasqueradingTypes.Contains(name, StringComparer.Ordinal))
                {
                    violations.Add(new LViolation(
                        type.SyntaxTree.FilePath,
                        type.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        type.Identifier.ValueText,
                        "Masquerading",
                        $"driver type derives from {name}"));
                    break;
                }
            }
        }
    }

    private static void LAuditDanglingScan(SyntaxNode root, IReadOnlySet<string> ids, List<LViolation> violations)
    {
        SemanticModel model = LAuditBind.LAuditModelRead(root);
        foreach (InvocationExpressionSyntax call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol method
                || method.ContainingType?.Name != LAuditStrictSetting.LAuditContractType)
            {
                continue;
            }

            TypeDeclarationSyntax? enclosing = call.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (enclosing is not null
                && SymbolEqualityComparer.Default.Equals(model.GetDeclaredSymbol(enclosing), method.ContainingType))
            {
                continue;
            }

            foreach (ArgumentSyntax argument in call.ArgumentList.Arguments)
            {
                if (model.GetTypeInfo(argument.Expression).ConvertedType?.SpecialType != SpecialType.System_String)
                {
                    continue;
                }

                int line = argument.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                Optional<object?> constant = model.GetConstantValue(argument.Expression);
                if (constant is not { HasValue: true, Value: string id })
                {
                    violations.Add(new LViolation(
                        call.SyntaxTree.FilePath, line, argument.Expression.ToString(), "Dangling",
                        "contract ID is not a constant"));
                }
                else if (!ids.Contains(id))
                {
                    violations.Add(new LViolation(
                        call.SyntaxTree.FilePath, line, id, "Dangling",
                        "contract ID has no element or resource in the surface"));
                }
            }
        }
    }

    private static IReadOnlySet<string> LAuditIdRead(IEnumerable<string> markupPaths)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (string path in markupPaths)
        {
            XDocument document;
            try
            {
                document = XDocument.Load(path);
            }
            catch (XmlException)
            {
                continue;
            }

            ids.UnionWith(document.Descendants()
                .SelectMany(element => element.Attributes())
                .Where(attribute => !attribute.IsNamespaceDeclaration
                    && LAuditStrictSetting.LAuditContractIds.Contains(attribute.Name.LocalName, StringComparer.Ordinal))
                .Select(attribute => attribute.Value));
        }

        return ids;
    }
}

internal static class LAuditTruthSetting
{
    public static bool LAuditTruthEnforced;
    public static Dictionary<string, string[]> LAuditTruthInformative = new(StringComparer.Ordinal);
    public static Dictionary<string, string> LAuditControlPrefix = new(StringComparer.Ordinal);
    public static string LAuditStateSuffix = "";
    public static string LAuditBulletinType = "";
    public static string LAuditConductRoot = "";
    public static string[] LAuditCapsuleInclude = [];
    public static string[] LAuditShellInclude = [];
    public static string[] LAuditTruthInclude = [];
    public static string[] LAuditControlBases = [];
    public static string[] LAuditOrderVerbs = [];
    public static string[] LAuditFillVerbs = [];
    public static string LAuditRequestPrefix = "";
    public static string[] LAuditSendRoots = [];
    public static string[] LAuditClockTypes = [];
    public static string[] LAuditConsoleInput = [];
    public static string[] LAuditDialogTypes = [];
    public static string[] LAuditDelayMembers = [];
    public static string LAuditInputBase = "";
    public static string[] LAuditInputMembers = [];
    public static string[] LAuditFocusMembers = [];
    public static string[] LAuditTruthHandles = [];
    public static string[] LAuditMoonlightingVerbs = [];
}

internal static class LAuditBoundarySetting
{
    public static string[] LAuditBoundaryForbidden = [];
    public static string[] LAuditBoundaryState = [];
    public static string[] LAuditBoundaryConverter = [];
    public static string[] LAuditBoundaryHidden = [];
    public static string[] LAuditBoundaryLoader = [];
    public static string LAuditBoundaryReflection = "";
    public static string LAuditBoundaryTimer = "";
    public static string[] LAuditBoundaryHold = [];
    public static string LAuditBoundaryPanel = "";
    public static string[] LAuditBoundaryTracked = [];
    public static string[] LAuditBoundaryLogic = [];
}

internal static class LAuditScopeSetting
{
    public static string[] LAuditSegments = [];
    public static string[] LAuditSuffixes = [];
    public static string[] LAuditPrefixes = [];

    public static void LAuditLoad(JsonElement binder)
    {
        LAuditSegments = LAuditSettingRead.LAuditListRead(binder, "excludeSegments");
        LAuditSuffixes = LAuditSettingRead.LAuditListRead(binder, "excludeSuffixes");
        LAuditPrefixes = LAuditSettingRead.LAuditListRead(binder, "excludePrefixes");
    }

    public static IReadOnlyList<string> LAuditFileRead(string root, IReadOnlyList<string> include) =>
        LAuditBinder.LAuditFileRead(root, include, [], LAuditSegments, LAuditSuffixes, LAuditPrefixes);
}

internal static class LAuditStrictWalker
{
    public static IReadOnlyList<LViolation> LAuditRun(IReadOnlyList<string> sourcePaths, out List<string> veneers)
    {
        List<LViolation> violations = [];
        Dictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>> parts = new(SymbolEqualityComparer.Default);
        foreach (SyntaxNode root in LAuditBind.LAuditWalkRead(sourcePaths))
        {
            foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (LAuditBind.LAuditSymbolRead(type) is not INamedTypeSymbol key)
                {
                    continue;
                }

                if (!parts.TryGetValue(key, out List<TypeDeclarationSyntax>? list))
                {
                    list = [];
                    parts[key] = list;
                }

                list.Add(type);
            }

            if (LAuditVeneerCheck(root))
            {
                LAuditHomoglyphScan(root, violations);
                LAuditPlainScan(root, violations);
            }
        }

        veneers = [];
        foreach ((INamedTypeSymbol symbol, List<TypeDeclarationSyntax> type) in parts)
        {
            bool veneer = type.Any(LAuditVeneerCheck);
            if (veneer)
            {
                veneers.Add(symbol.Name);
            }

            foreach (TypeDeclarationSyntax part in type)
            {
                LAuditHoardingScan(part, veneer, violations);
                if (veneer)
                {
                    LAuditMeddlingScan(part.Identifier.ValueText, part.Members, violations);
                    LAuditEngineScan(part, part.Identifier.ValueText, violations);
                    LAuditFreelancingScan(part, violations);
                }
            }
        }

        return violations;
    }

    private static bool LAuditVeneerCheck(SyntaxNode part)
    {
        IReadOnlyList<string> roots = LAuditBind.LAuditRootRead(LAuditStrictSetting.LAuditVeneerInclude);
        string relative = LAuditBind.LAuditRelativeRead(part.SyntaxTree.FilePath);
        return roots.Any(root => relative.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static void LAuditPlainScan(SyntaxNode root, List<LViolation> violations)
    {
        foreach (EnumDeclarationSyntax listed in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            string owner = listed.Identifier.ValueText;
            LAuditEngineScan(listed, owner, violations);
            foreach (EnumMemberDeclarationSyntax member in listed.Members)
            {
                if (member.EqualsValue is { Value: var value } && value is not LiteralExpressionSyntax)
                {
                    violations.Add(new LViolation(
                        member.SyntaxTree.FilePath,
                        LAuditLineRead(member),
                        $"{owner}.{member.Identifier.ValueText}",
                        "Meddling",
                        $"{value.Kind()} where only a call may stand"));
                }
            }
        }

        foreach (DelegateDeclarationSyntax shape in root.DescendantNodes().OfType<DelegateDeclarationSyntax>())
        {
            LAuditEngineScan(shape, shape.Identifier.ValueText, violations);
        }
    }

    private static void LAuditHoardingScan(TypeDeclarationSyntax part, bool veneer, List<LViolation> violations)
    {
        string owner = part.Identifier.ValueText;
        foreach (BaseFieldDeclarationSyntax field in part.Members.OfType<BaseFieldDeclarationSyntax>())
        {
            bool constant = field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ConstKeyword));
            bool fixture = constant || field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ReadOnlyKeyword));
            bool shared = field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));
            if (veneer ? constant : fixture || !shared)
            {
                continue;
            }

            string reason = veneer
                ? field is EventFieldDeclarationSyntax ? "event field in a surface type" : "field in a surface type"
                : "mutable static field in a driver type";
            foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
            {
                violations.Add(new LViolation(
                    field.SyntaxTree.FilePath,
                    LAuditLineRead(variable),
                    $"{owner}.{variable.Identifier.ValueText}",
                    veneer ? "Hoarding" : "Sharing",
                    reason));
            }
        }

        if (!veneer)
        {
            return;
        }

        foreach (PropertyDeclarationSyntax property in part.Members.OfType<PropertyDeclarationSyntax>()
                     .Where(LAuditAutoCheck))
        {
            violations.Add(new LViolation(
                property.SyntaxTree.FilePath,
                LAuditLineRead(property),
                $"{owner}.{property.Identifier.ValueText}",
                "Hoarding",
                "auto-property in a surface type"));
        }

        foreach (ParameterSyntax parameter in part.ParameterList?.Parameters ?? [])
        {
            violations.Add(new LViolation(
                parameter.SyntaxTree.FilePath,
                LAuditLineRead(parameter),
                $"{owner}.{parameter.Identifier.ValueText}",
                "Hoarding",
                "primary constructor parameter in a surface type"));
        }
    }

    private static void LAuditFreelancingScan(TypeDeclarationSyntax part, List<LViolation> violations)
    {
        string owner = part.Identifier.ValueText;
        foreach (MemberDeclarationSyntax member in part.Members.Where(member =>
                     member is BaseMethodDeclarationSyntax and not ConstructorDeclarationSyntax
                         or BasePropertyDeclarationSyntax))
        {
            violations.Add(new LViolation(
                member.SyntaxTree.FilePath,
                LAuditLineRead(member),
                $"{owner}.{LAuditMemberRead(member)}",
                "Freelancing",
                $"{member.Kind()} where only a constructor may stand"));
        }
    }

    private static bool LAuditAutoCheck(PropertyDeclarationSyntax property)
    {
        return property.ExpressionBody is null
               && property.AccessorList is { } accessors
               && accessors.Accessors.All(accessor => accessor.Body is null && accessor.ExpressionBody is null);
    }

    private static void LAuditMeddlingScan(
        string owner, IEnumerable<MemberDeclarationSyntax> members, List<LViolation> violations)
    {
        foreach (MemberDeclarationSyntax member in members)
        {
            List<SyntaxNode> breaches = [];
            foreach (SyntaxNode body in LAuditBodyRead(member))
            {
                LAuditBodyScan(body, breaches);
            }

            HashSet<int> seen = [];
            foreach (SyntaxNode breach in breaches.SelectMany(LAuditNestRead))
            {
                int line = LAuditLineRead(breach);
                if (seen.Add(line))
                {
                    violations.Add(new LViolation(
                        member.SyntaxTree.FilePath,
                        line,
                        $"{owner}.{LAuditMemberRead(member)}",
                        "Meddling",
                        $"{breach.Kind()} where only a call may stand"));
                }
            }
        }
    }

    private static IEnumerable<SyntaxNode> LAuditNestRead(SyntaxNode breach)
    {
        return breach.DescendantNodesAndSelf().Where(node =>
            node == breach
            || node is StatementSyntax and not BlockSyntax
            || node is SwitchExpressionArmSyntax);
    }

    private static IEnumerable<SyntaxNode> LAuditBodyRead(MemberDeclarationSyntax member)
    {
        IEnumerable<SyntaxNode?> bodies = member switch
        {
            ConstructorDeclarationSyntax constructor =>
                [constructor.Initializer?.ArgumentList, constructor.Body, constructor.ExpressionBody],
            BaseMethodDeclarationSyntax method => [method.Body, method.ExpressionBody],
            BaseFieldDeclarationSyntax field => field.Declaration.Variables
                .Select(variable => (SyntaxNode?)variable.Initializer?.Value),
            PropertyDeclarationSyntax { ExpressionBody: { } arrow } => [arrow],
            IndexerDeclarationSyntax { ExpressionBody: { } arrow } => [arrow],
            BasePropertyDeclarationSyntax { AccessorList: { } accessors } property => accessors.Accessors
                .SelectMany(accessor => new SyntaxNode?[] { accessor.Body, accessor.ExpressionBody })
                .Append((property as PropertyDeclarationSyntax)?.Initializer?.Value),
            _ => []
        };
        return bodies.OfType<SyntaxNode>();
    }

    private static void LAuditBodyScan(SyntaxNode body, List<SyntaxNode> breaches)
    {
        switch (body)
        {
            case BlockSyntax block:
                foreach (StatementSyntax statement in block.Statements)
                {
                    LAuditStatementScan(statement, breaches);
                }

                break;
            case ArrowExpressionClauseSyntax arrow:
                LAuditInvocationScan(arrow.Expression, breaches);
                break;
            case ArgumentListSyntax arguments:
                LAuditArgumentScan(arguments, breaches);
                break;
            case ExpressionSyntax expression:
                LAuditInvocationScan(expression, breaches);
                break;
            default:
                breaches.Add(body);
                break;
        }
    }

    private static void LAuditStatementScan(StatementSyntax statement, List<SyntaxNode> breaches)
    {
        switch (statement)
        {
            case ExpressionStatementSyntax { Expression: var expression }:
                LAuditInvocationScan(expression, breaches);
                break;
            case ReturnStatementSyntax { Expression: { } expression }:
                LAuditInvocationScan(expression, breaches);
                break;
            default:
                breaches.Add(statement);
                break;
        }
    }

    private static void LAuditInvocationScan(ExpressionSyntax expression, List<SyntaxNode> breaches)
    {
        if (expression is not InvocationExpressionSyntax call)
        {
            breaches.Add(expression);
            return;
        }

        switch (call.Expression)
        {
            case SimpleNameSyntax:
                break;
            case MemberAccessExpressionSyntax access when access.IsKind(SyntaxKind.SimpleMemberAccessExpression):
                LAuditOperandScan(access.Expression, breaches);
                break;
            default:
                breaches.Add(call.Expression);
                break;
        }

        if (LAuditBind.LAuditSymbolRead(call) is IMethodSymbol method
            && LAuditStrictSetting.LAuditQueryTypes.Contains(
                (method.ReducedFrom ?? method).ContainingType.ToDisplayString(), StringComparer.Ordinal))
        {
            breaches.Add(call);
        }

        LAuditArgumentScan(call.ArgumentList, breaches);
    }

    private static void LAuditArgumentScan(ArgumentListSyntax arguments, List<SyntaxNode> breaches)
    {
        foreach (ArgumentSyntax argument in arguments.Arguments)
        {
            if (!argument.RefKindKeyword.IsKind(SyntaxKind.None))
            {
                breaches.Add(argument);
                continue;
            }

            LAuditOperandScan(argument.Expression, breaches);
        }
    }

    private static void LAuditOperandScan(ExpressionSyntax operand, List<SyntaxNode> breaches)
    {
        switch (operand)
        {
            case SimpleNameSyntax or ThisExpressionSyntax or BaseExpressionSyntax or PredefinedTypeSyntax
                or LiteralExpressionSyntax:
                break;
            case MemberAccessExpressionSyntax access when access.IsKind(SyntaxKind.SimpleMemberAccessExpression):
                LAuditOperandScan(access.Expression, breaches);
                break;
            case InvocationExpressionSyntax call:
                LAuditInvocationScan(call, breaches);
                break;
            case AnonymousFunctionExpressionSyntax lambda:
                LAuditBodyScan(lambda.Body, breaches);
                break;
            default:
                breaches.Add(operand);
                break;
        }
    }

    private static void LAuditEngineScan(SyntaxNode part, string owner, List<LViolation> violations)
    {
        HashSet<int> seen = [];
        IEnumerable<SimpleNameSyntax> names = part
            .DescendantNodes(node => node == part || node is not BaseTypeDeclarationSyntax)
            .OfType<SimpleNameSyntax>();
        foreach (SimpleNameSyntax name in names)
        {
            if (!LAuditBind.LAuditLogicCheck(name))
            {
                continue;
            }

            int line = LAuditLineRead(name);
            if (!seen.Add(line))
            {
                continue;
            }

            MemberDeclarationSyntax? member = name.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            string label = member is null || member == part ? "type" : LAuditMemberRead(member);
            violations.Add(new LViolation(
                part.SyntaxTree.FilePath,
                line,
                $"{owner}.{label}",
                "Prying",
                $"names {name.Identifier.ValueText} from below the driver"));
        }
    }

    public static void LAuditHomoglyphScan(SyntaxNode root, List<LViolation> violations)
    {
        foreach (SyntaxToken token in root.DescendantTokens())
        {
            if (!token.IsKind(SyntaxKind.IdentifierToken) || token.ValueText.All(char.IsAscii))
            {
                continue;
            }

            violations.Add(new LViolation(
                root.SyntaxTree.FilePath,
                LAuditLineRead(token.Parent ?? root),
                token.ValueText,
                "Homoglyph",
                "identifier carries a non-ASCII glyph"));
        }
    }

    public static ExpressionSyntax LAuditCoreRead(ExpressionSyntax condition)
    {
        ExpressionSyntax core = condition;
        while (true)
        {
            ExpressionSyntax? peeled = core switch
            {
                ParenthesizedExpressionSyntax wrapped => wrapped.Expression,
                PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } negated
                    => negated.Operand,
                _ => null
            };
            if (peeled is null)
            {
                return core;
            }

            core = peeled;
        }
    }

    public static bool LAuditPatternCheck(PatternSyntax pattern)
    {
        return pattern switch
        {
            ConstantPatternSyntax constant => LAuditNullCheck(constant.Expression),
            UnaryPatternSyntax not => LAuditPatternCheck(not.Pattern),
            DeclarationPatternSyntax => true,
            VarPatternSyntax => true,
            TypePatternSyntax => true,
            RecursivePatternSyntax { PositionalPatternClause: null } shape
                => shape.PropertyPatternClause?.Subpatterns.Count is null or 0,
            _ => false
        };
    }

    public static bool LAuditNullCheck(ExpressionSyntax expression)
    {
        return expression.IsKind(SyntaxKind.NullLiteralExpression)
               || expression.IsKind(SyntaxKind.DefaultLiteralExpression);
    }

    public static bool LAuditDataCheck(SyntaxNode node)
    {
        foreach (SyntaxNode child in node.DescendantNodesAndSelf())
        {
            bool logic = child switch
            {
                IdentifierNameSyntax or MemberBindingExpressionSyntax => LAuditBind.LAuditEngineCheck(child),
                _ => false
            };
            if (logic)
            {
                return true;
            }
        }

        return false;
    }

    public static string LAuditMemberRead(MemberDeclarationSyntax member)
    {
        return member switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            ConstructorDeclarationSyntax => "ctor",
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            EventDeclarationSyntax evt => evt.Identifier.ValueText,
            BaseFieldDeclarationSyntax field => field.Declaration.Variables[0].Identifier.ValueText,
            IndexerDeclarationSyntax => "this[]",
            OperatorDeclarationSyntax op => op.OperatorToken.ValueText,
            _ => member.Kind().ToString()
        };
    }

    public static int LAuditLineRead(SyntaxNode node)
    {
        return node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
    }
}

internal static class LAuditHostWalker
{
    public static IReadOnlyList<LViolation> LAuditRun(IReadOnlyList<string> sourcePaths)
    {
        List<LViolation> violations = [];
        foreach (SyntaxNode root in LAuditBind.LAuditWalkRead(sourcePaths))
        {
            List<SyntaxNode> breaches = LAuditBreachRead(root);

            HashSet<int> seen = [];
            foreach (SyntaxNode breach in breaches.SelectMany(breach => breach.DescendantNodesAndSelf()
                         .Where(node => node == breach || node is StatementSyntax and not BlockSyntax)))
            {
                int line = LAuditStrictWalker.LAuditLineRead(breach);
                if (seen.Add(line))
                {
                    MemberDeclarationSyntax? owner = breach.FirstAncestorOrSelf<MemberDeclarationSyntax>(node =>
                        node is not GlobalStatementSyntax);
                    violations.Add(new LViolation(
                        root.SyntaxTree.FilePath,
                        line,
                        owner is null ? Path.GetFileNameWithoutExtension(root.SyntaxTree.FilePath)
                            : LAuditStrictWalker.LAuditMemberRead(owner),
                        "Overworking",
                        $"{breach.Kind()} where only construction and wiring may stand"));
                }
            }
        }

        return violations;
    }

    public static List<SyntaxNode> LAuditBreachRead(SyntaxNode root)
    {
        List<SyntaxNode> breaches = [];
        LAuditBlockScan(root.ChildNodes().OfType<GlobalStatementSyntax>().Select(global => global.Statement)
            .ToList(), breaches);
        foreach (MemberDeclarationSyntax member in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
        {
            switch (member)
            {
                case BaseMethodDeclarationSyntax { Body: { } body }:
                    LAuditBlockScan(body.Statements, breaches);
                    break;
                case BaseMethodDeclarationSyntax { ExpressionBody: { } arrow }:
                    LAuditExpressionScan(arrow.Expression, breaches);
                    break;
                case PropertyDeclarationSyntax { ExpressionBody: { } arrow }:
                    LAuditExpressionScan(arrow.Expression, breaches);
                    break;
                case IndexerDeclarationSyntax { ExpressionBody: { } arrow }:
                    LAuditExpressionScan(arrow.Expression, breaches);
                    break;
                case BasePropertyDeclarationSyntax { AccessorList: { } accessors }:
                    breaches.AddRange(accessors.Accessors.Where(accessor =>
                        accessor.Body is not null || accessor.ExpressionBody is not null));
                    break;
            }
        }

        return breaches;
    }

    private static void LAuditBlockScan(IReadOnlyList<StatementSyntax> statements, List<SyntaxNode> breaches)
    {
        for (int index = 0; index < statements.Count; index++)
        {
            switch (statements[index])
            {
                case LocalDeclarationStatementSyntax local:
                    foreach (VariableDeclaratorSyntax variable in local.Declaration.Variables)
                    {
                        if (variable.Initializer is { Value: var value })
                        {
                            LAuditExpressionScan(value, breaches);
                        }
                    }

                    break;
                case ExpressionStatementSyntax { Expression: var expression }:
                    LAuditExpressionScan(expression, breaches);
                    break;
                case ReturnStatementSyntax closing when index == statements.Count - 1:
                    if (closing.Expression is not null)
                    {
                        LAuditExpressionScan(closing.Expression, breaches);
                    }

                    break;
                case LocalFunctionStatementSyntax { Body: { } body }:
                    LAuditBlockScan(body.Statements, breaches);
                    break;
                default:
                    breaches.Add(statements[index]);
                    break;
            }
        }
    }

    private static void LAuditExpressionScan(ExpressionSyntax expression, List<SyntaxNode> breaches)
    {
        switch (expression)
        {
            case SimpleNameSyntax or ThisExpressionSyntax or BaseExpressionSyntax or PredefinedTypeSyntax
                or LiteralExpressionSyntax or TypeOfExpressionSyntax:
                break;
            case MemberAccessExpressionSyntax access when access.IsKind(SyntaxKind.SimpleMemberAccessExpression):
                LAuditExpressionScan(access.Expression, breaches);
                break;
            case InvocationExpressionSyntax call:
                LAuditExpressionScan(call.Expression, breaches);
                LAuditArgumentScan(call.ArgumentList.Arguments, breaches);
                break;
            case BaseObjectCreationExpressionSyntax creation:
                LAuditArgumentScan(creation.ArgumentList?.Arguments ?? [], breaches);
                foreach (ExpressionSyntax part in creation.Initializer?.Expressions ?? [])
                {
                    LAuditExpressionScan(part, breaches);
                }

                break;
            case AssignmentExpressionSyntax assignment when assignment.IsKind(SyntaxKind.SimpleAssignmentExpression):
                LAuditExpressionScan(assignment.Left, breaches);
                LAuditExpressionScan(assignment.Right, breaches);
                break;
            case AwaitExpressionSyntax waiting:
                LAuditExpressionScan(waiting.Expression, breaches);
                break;
            case AnonymousFunctionExpressionSyntax { Block: { } block }:
                LAuditBlockScan(block.Statements, breaches);
                break;
            case AnonymousFunctionExpressionSyntax { ExpressionBody: { } body }:
                LAuditExpressionScan(body, breaches);
                break;
            default:
                breaches.Add(expression);
                break;
        }
    }

    private static void LAuditArgumentScan(IEnumerable<ArgumentSyntax> arguments, List<SyntaxNode> breaches)
    {
        foreach (ArgumentSyntax argument in arguments)
        {
            if (argument.RefKindKeyword.IsKind(SyntaxKind.None))
            {
                LAuditExpressionScan(argument.Expression, breaches);
            }
            else
            {
                breaches.Add(argument);
            }
        }
    }
}

internal static class LAuditReachWalker
{
    private static readonly Regex LAuditLogicPattern = new(
        @"(?<![A-Za-z0-9_])(L[A-Z][A-Za-z0-9_]*)", RegexOptions.Compiled);

    private static readonly Regex LAuditStaticPattern = new(
        @"x:Static\s+(?:[A-Za-z0-9_]+:)?(L[A-Z][A-Za-z0-9_]*)", RegexOptions.Compiled);

    private static readonly Regex LAuditSlotPattern = new(
        $@"\b({string.Join('|', LAuditStrictSetting.LAuditBranchingSlots)})\s*=", RegexOptions.Compiled);

    private static readonly Regex LAuditTetheringPattern = new(
        @"\{\s*(?:([A-Za-z_][\w.]*):)?([A-Za-z_][\w.]*)", RegexOptions.Compiled);

    public static IReadOnlyList<LViolation> LAuditRun(IEnumerable<string> markupPaths)
    {
        List<LViolation> violations = [];
        IReadOnlySet<string> deportment = LAuditBind.LAuditDeportmentRead();
        Dictionary<string, List<string>> spaces = LAuditSpaceRead();
        foreach (string path in markupPaths)
        {
            XDocument document;
            try
            {
                document = XDocument.Load(path, LoadOptions.SetLineInfo);
            }
            catch (XmlException failure)
            {
                violations.Add(new LViolation(
                    path, failure.LineNumber, "markup", "Overreaching", "does not parse as XML"));
                continue;
            }

            SortedDictionary<int, List<string>> hooks = [];
            foreach (XElement element in document.Descendants())
            {
                LAuditElementScan(path, element, deportment, violations);
                LAuditTetheringScan(element, spaces, hooks);
            }

            violations.AddRange(hooks.Select(hook => new LViolation(
                path, hook.Key, hook.Value[0], "Tethering", $"line hooks logic into markup: {string.Join(", ", hook.Value)}")));
        }

        return violations;
    }

    private static void LAuditTetheringScan(
        XElement element, Dictionary<string, List<string>> spaces, SortedDictionary<int, List<string>> hooks)
    {
        string name = element.Name.LocalName;
        int line = ((IXmlLineInfo)element).LineNumber;
        string property = name[(name.LastIndexOf('.') + 1)..];
        if (LAuditStrictSetting.LAuditTetheringElements.Contains(name, StringComparer.Ordinal)
            || (name.Contains('.', StringComparison.Ordinal)
                && LAuditStrictSetting.LAuditTetheringSlots.Contains(property, StringComparer.Ordinal))
            || LAuditTetheringCheck(element, spaces))
        {
            LAuditTetheringAdd(hooks, line, name);
        }

        foreach (XAttribute attribute in element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration))
        {
            line = ((IXmlLineInfo)attribute).LineNumber;
            if (LAuditStrictSetting.LAuditTetheringSlots.Contains(attribute.Name.LocalName, StringComparer.Ordinal))
            {
                LAuditTetheringAdd(hooks, line, attribute.Name.LocalName);
            }

            string shell = LAuditStrictSetting.LAuditVeneerNamespace + ".";
            if (attribute.Name.LocalName == "Class" && !attribute.Value.StartsWith(shell, StringComparison.Ordinal))
            {
                LAuditTetheringAdd(hooks, line, "x:Class");
            }

            string slot = (element.Attribute("Property")?.Value ?? string.Empty).Trim('(', ')');
            slot = slot[(slot.LastIndexOf('.') + 1)..];
            if (attribute.Name.LocalName == "Property"
                && LAuditStrictSetting.LAuditTetheringSlots.Contains(slot, StringComparer.Ordinal))
            {
                LAuditTetheringAdd(hooks, line, slot);
            }

            bool literal = !attribute.Value.StartsWith('{');
            if (literal
                && (LAuditStrictSetting.LAuditTetheringLiterals.Contains(
                        attribute.Name.LocalName, StringComparer.Ordinal)
                    || (attribute.Name.LocalName == "Value"
                        && LAuditStrictSetting.LAuditTetheringLiterals.Contains(slot, StringComparer.Ordinal))))
            {
                LAuditTetheringAdd(hooks, line, attribute.Name.LocalName == "Value" ? slot : attribute.Name.LocalName);
            }

            foreach (Match match in LAuditTetheringPattern.Matches(attribute.Value))
            {
                string prefix = match.Groups[1].Value;
                string extension = prefix.Length == 0 ? match.Groups[2].Value : $"{prefix}:{match.Groups[2].Value}";
                string space = prefix.Length == 0
                    ? string.Empty
                    : element.GetNamespaceOfPrefix(prefix)?.NamespaceName ?? string.Empty;
                if (LAuditStrictSetting.LAuditTetheringExtensions.Contains(extension, StringComparer.Ordinal)
                    || space.StartsWith("clr-namespace:", StringComparison.Ordinal))
                {
                    LAuditTetheringAdd(hooks, line, extension);
                }
            }
        }
    }

    private static void LAuditTetheringAdd(SortedDictionary<int, List<string>> hooks, int line, string marker)
    {
        if (!hooks.TryGetValue(line, out List<string>? markers))
        {
            markers = [];
            hooks[line] = markers;
        }

        markers.Add(marker);
    }

    private static bool LAuditTetheringCheck(XElement element, Dictionary<string, List<string>> spaces)
    {
        const string prefix = "clr-namespace:";
        string name = element.Name.LocalName;
        string uri = element.Name.NamespaceName;
        if (name.Contains('.', StringComparison.Ordinal))
        {
            return false;
        }

        IEnumerable<string> candidates = uri.StartsWith(prefix, StringComparison.Ordinal)
            ? [uri[prefix.Length..].Split(';')[0]]
            : spaces.GetValueOrDefault(uri) ?? [];
        foreach (string space in candidates)
        {
            for (INamedTypeSymbol? type = LAuditBind.LAuditCompilation.GetTypeByMetadataName($"{space}.{name}");
                 type is not null;
                 type = type.BaseType)
            {
                if (type.Interfaces.Append(type).Any(shape =>
                        LAuditStrictSetting.LAuditTetheringTypes.Contains(
                            shape.ToDisplayString(), StringComparer.Ordinal)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Dictionary<string, List<string>> LAuditSpaceRead()
    {
        CSharpCompilation compilation = LAuditBind.LAuditCompilation;
        Dictionary<string, List<string>> spaces = new(StringComparer.Ordinal);
        IEnumerable<AttributeData> attributes = compilation.References
            .Select(compilation.GetAssemblyOrModuleSymbol)
            .OfType<IAssemblySymbol>()
            .Append(compilation.Assembly)
            .SelectMany(assembly => assembly.GetAttributes());
        foreach (AttributeData attribute in attributes)
        {
            if (attribute.AttributeClass?.Name is not "XmlnsDefinitionAttribute"
                || attribute.ConstructorArguments is not [{ Value: string uri }, { Value: string space }, ..])
            {
                continue;
            }

            if (!spaces.TryGetValue(uri, out List<string>? list))
            {
                list = [];
                spaces[uri] = list;
            }

            list.Add(space);
        }

        return spaces;
    }

    private static void LAuditElementScan(
        string path,
        XElement element,
        IReadOnlySet<string> deportment,
        List<LViolation> violations)
    {
        LAuditBranchingScan(path, element, violations);
        foreach (XAttribute attribute in element.Attributes())
        {
            int line = ((IXmlLineInfo)attribute).LineNumber;
            if (attribute.IsNamespaceDeclaration)
            {
                LAuditNamespaceScan(path, line, attribute.Value, violations);
                continue;
            }

            if (string.Equals(attribute.Name.LocalName, "Class", StringComparison.Ordinal))
            {
                continue;
            }

            LAuditValueScan(path, line, attribute.Name.LocalName, attribute.Value, deportment, violations);
        }

        foreach (XText text in element.Nodes().OfType<XText>())
        {
            LAuditValueScan(path, ((IXmlLineInfo)text).LineNumber, "text", text.Value, deportment, violations);
        }
    }

    private static void LAuditBranchingScan(string path, XElement element, List<LViolation> violations)
    {
        string name = element.Name.LocalName;
        string property = name[(name.LastIndexOf('.') + 1)..];
        if (LAuditStrictSetting.LAuditBranchingElements.Contains(name, StringComparer.Ordinal))
        {
            violations.Add(new LViolation(
                path, ((IXmlLineInfo)element).LineNumber, name, "Branching", "markup branches on a condition"));
        }
        else if (name.Contains('.', StringComparison.Ordinal)
                 && LAuditStrictSetting.LAuditBranchingSlots.Contains(property, StringComparer.Ordinal))
        {
            violations.Add(new LViolation(
                path, ((IXmlLineInfo)element).LineNumber, name, "Branching", $"property element {property} computes"));
        }

        foreach (XAttribute attribute in element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration))
        {
            int line = ((IXmlLineInfo)attribute).LineNumber;
            string slot = attribute.Name.LocalName;
            if (LAuditStrictSetting.LAuditBranchingSlots.Contains(slot, StringComparer.Ordinal))
            {
                violations.Add(new LViolation(path, line, slot, "Branching", $"binding {slot} computes in markup"));
            }

            foreach (Match match in LAuditSlotPattern.Matches(attribute.Value))
            {
                string found = match.Groups[1].Value;
                violations.Add(new LViolation(path, line, found, "Branching", $"binding {found} computes in markup"));
            }
        }
    }

    private static void LAuditNamespaceScan(string path, int line, string value, List<LViolation> violations)
    {
        const string prefix = "clr-namespace:";
        if (!value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        string mapped = value[prefix.Length..].Split(';')[0];
        if (LAuditStrictSetting.LAuditOverreachingNamespaces.Any(space =>
                mapped.Equals(space, StringComparison.Ordinal)
                || mapped.StartsWith(space + ".", StringComparison.Ordinal)))
        {
            violations.Add(new LViolation(path, line, mapped, "Overreaching", "maps a logic namespace"));
        }
    }

    private static void LAuditValueScan(
        string path,
        int line,
        string slot,
        string value,
        IReadOnlySet<string> deportment,
        List<LViolation> violations)
    {
        HashSet<string> named = new(StringComparer.Ordinal);
        foreach (Match match in LAuditStaticPattern.Matches(value))
        {
            string name = match.Groups[1].Value;
            named.Add(name);
            if (!deportment.Contains(name))
            {
                violations.Add(new LViolation(path, line, name, "Overreaching", "reads a logic constant"));
            }
        }

        foreach (Match match in LAuditLogicPattern.Matches(value))
        {
            string name = match.Groups[1].Value;
            if (named.Add(name) && !deportment.Contains(name))
            {
                violations.Add(new LViolation(path, line, name, "Overreaching", $"names logic in {slot}"));
            }
        }
    }
}

internal static partial class LAuditTruthWalker
{
    private static readonly object LAuditGate = new();

    private static IReadOnlyList<SyntaxNode> LAuditRoots = [];

    private static Dictionary<ISymbol, List<IdentifierNameSyntax>> LAuditIndex = new(SymbolEqualityComparer.Default);

    private sealed record LAuditTruthField(
        HashSet<ISymbol> TFieldSymbols,
        string TFieldName,
        ITypeSymbol TFieldType,
        string TFieldPath,
        int TFieldLine,
        bool TFieldShared);

    public static IReadOnlyList<LViolation> LAuditRun(IReadOnlyList<string> sourcePaths)
    {
        lock (LAuditGate)
        {
            List<LViolation> violations = [];
            Dictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>> parts = LAuditPartRead(sourcePaths);
            foreach (SyntaxNode root in LAuditRoots)
            {
                LAuditTamperingScan(root, violations);
                LAuditMisfiringScan(root, violations);
                LAuditZeroingScan(root, violations);
            }

            LAuditMismatchingScan(violations);
            LAuditPuppeteeringScan(violations);
            foreach (List<TypeDeclarationSyntax> type in parts.Values)
            {
                LAuditBaseCheck(type, violations);
                LAuditLocalScan(type, violations);
                LAuditSequenceScan(type, violations);
                foreach (LAuditTruthField field in LAuditFieldRead(type))
                {
                    LAuditFieldCheck(field, type, violations);
                }
            }

            return violations;
        }
    }

    public static IReadOnlySet<ISymbol> LAuditReaderRead(IReadOnlyList<string> sourcePaths)
    {
        lock (LAuditGate)
        {
            LAuditPartRead(sourcePaths);
            return new HashSet<ISymbol>(LAuditReaderNames.Concat(LAuditRelayNames), SymbolEqualityComparer.Default);
        }
    }

    private static Dictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>> LAuditPartRead(
        IReadOnlyList<string> sourcePaths)
    {
        Dictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>> parts = new(SymbolEqualityComparer.Default);
        LAuditRoots = LAuditBind.LAuditWalkRead(sourcePaths).Where(LAuditBind.LAuditWalkCheck).ToList();
        foreach (SyntaxNode root in LAuditRoots)
        {
            foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (type.Ancestors().OfType<TypeDeclarationSyntax>().Any()
                    || LAuditBind.LAuditSymbolRead(type) is not INamedTypeSymbol key)
                {
                    continue;
                }

                if (!parts.TryGetValue(key, out List<TypeDeclarationSyntax>? list))
                {
                    list = [];
                    parts[key] = list;
                }

                list.Add(type);
            }
        }

        LAuditIndex = new Dictionary<ISymbol, List<IdentifierNameSyntax>>(SymbolEqualityComparer.Default);
        foreach (IdentifierNameSyntax identifier in LAuditRoots.SelectMany(root =>
                     root.DescendantNodes(node => !LAuditNameofCheck(node)).OfType<IdentifierNameSyntax>()))
        {
            if (LAuditBind.LAuditSymbolRead(identifier) is not { } symbol)
            {
                continue;
            }

            if (!LAuditIndex.TryGetValue(symbol, out List<IdentifierNameSyntax>? uses))
            {
                uses = [];
                LAuditIndex[symbol] = uses;
            }

            uses.Add(identifier);
        }

        List<TypeDeclarationSyntax> every = parts.Values.SelectMany(type => type).ToList();
        LAuditRelayRead(every);
        LAuditSendResolve(every);
        return parts;
    }

    private static IEnumerable<LAuditTruthField> LAuditFieldRead(IReadOnlyList<TypeDeclarationSyntax> type)
    {
        foreach (TypeDeclarationSyntax part in type.SelectMany(part =>
                     part.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>()))
        {
            foreach (FieldDeclarationSyntax field in part.Members.OfType<FieldDeclarationSyntax>())
            {
                bool fixture = field.Modifiers.Any(modifier =>
                    modifier.IsKind(SyntaxKind.ReadOnlyKeyword)
                    || modifier.IsKind(SyntaxKind.ConstKeyword));
                foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                {
                    if (LAuditBind.LAuditSymbolRead(variable) is not IFieldSymbol symbol
                        || LAuditHandleCheck(symbol.Type))
                    {
                        continue;
                    }

                    HashSet<ISymbol> symbols = LAuditAliasRead(symbol, type);
                    if (!fixture || LAuditBind.LAuditEngineCheck(symbol.Type) || LAuditFillCheck(symbols, type))
                    {
                        yield return new LAuditTruthField(
                            symbols,
                            LAuditBind.LAuditLabelRead(symbol),
                            symbol.Type,
                            field.SyntaxTree.FilePath,
                            LAuditLineRead(variable),
                            symbol.IsStatic);
                    }
                }
            }

            foreach (ParameterSyntax parameter in part.ParameterList?.Parameters ?? [])
            {
                if (LAuditBind.LAuditSymbolRead(parameter) is not IParameterSymbol symbol
                    || LAuditHandleCheck(symbol.Type))
                {
                    continue;
                }

                HashSet<ISymbol> symbols = new([symbol], SymbolEqualityComparer.Default);
                foreach (IPropertySymbol property in symbol.ContainingType?.GetMembers(symbol.Name)
                             .OfType<IPropertySymbol>() ?? [])
                {
                    symbols.Add(property.OriginalDefinition);
                }

                yield return new LAuditTruthField(
                    symbols,
                    LAuditBind.LAuditLabelRead(symbol),
                    symbol.Type,
                    parameter.SyntaxTree.FilePath,
                    LAuditLineRead(parameter),
                    true);
            }

            foreach (PropertyDeclarationSyntax property in part.Members.OfType<PropertyDeclarationSyntax>())
            {
                bool settable = property.AccessorList?.Accessors.Any(accessor =>
                    accessor.IsKind(SyntaxKind.SetAccessorDeclaration)
                    || accessor.IsKind(SyntaxKind.InitAccessorDeclaration)) == true;
                if (!settable
                    || LAuditBind.LAuditSymbolRead(property) is not IPropertySymbol symbol
                    || LAuditHandleCheck(symbol.Type))
                {
                    continue;
                }

                yield return new LAuditTruthField(
                    new HashSet<ISymbol>([symbol], SymbolEqualityComparer.Default),
                    LAuditBind.LAuditLabelRead(symbol),
                    symbol.Type,
                    property.SyntaxTree.FilePath,
                    LAuditLineRead(property),
                    true);
            }
        }
    }

    private static bool LAuditFillCheck(HashSet<ISymbol> symbols, IReadOnlyList<TypeDeclarationSyntax> type)
    {
        return LAuditUseRead(symbols)
            .Where(identifier => LAuditInsideCheck(identifier, type))
            .Select(LAuditReferenceRead)
            .Any(reference => LAuditWriteCheck(reference, out _));
    }

    private static IEnumerable<IdentifierNameSyntax> LAuditUseRead(IEnumerable<ISymbol> symbols)
    {
        return symbols.SelectMany(symbol => LAuditIndex.GetValueOrDefault(symbol) ?? []);
    }

    private static bool LAuditInsideCheck(SyntaxNode node, IReadOnlyList<TypeDeclarationSyntax> type)
    {
        return type.Any(part => part.SyntaxTree == node.SyntaxTree && part.Span.Contains(node.Span));
    }

    private static bool LAuditFieldCheck(IdentifierNameSyntax identifier, HashSet<ISymbol> symbols)
    {
        ISymbol? symbol = LAuditBind.LAuditSymbolRead(identifier);
        return symbol is not null && symbols.Contains(symbol);
    }

    private static void LAuditFieldCheck(
        LAuditTruthField field, IReadOnlyList<TypeDeclarationSyntax> type, List<LViolation> violations)
    {
        if (LAuditBind.LAuditEngineCheck(field.TFieldType))
        {
            violations.Add(new LViolation(
                field.TFieldPath,
                field.TFieldLine,
                field.TFieldName,
                "Duplicating",
                $"holds a {field.TFieldType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}"));
        }
        else if (field.TFieldName.EndsWith(LAuditTruthSetting.LAuditStateSuffix, StringComparison.Ordinal))
        {
            violations.Add(new LViolation(
                field.TFieldPath, field.TFieldLine, field.TFieldName, "Duplicating", "names a state the engine owns"));
        }
        else if (field.TFieldType.SpecialType == SpecialType.System_Object)
        {
            violations.Add(new LViolation(
                field.TFieldPath, field.TFieldLine, field.TFieldName, "Duplicating", "holds an untyped value"));
        }

        HashSet<MemberDeclarationSyntax> scopes = [];
        HashSet<MemberDeclarationSyntax> toggles = [];
        HashSet<ISymbol> writers = LAuditWriterRead(field, type);
        SyntaxNode? engineWrite = null;
        List<SyntaxNode> plainWrites = [];
        List<(SyntaxNode, ExpressionSyntax?)> written = [];

        foreach (IdentifierNameSyntax identifier in LAuditUseRead(writers))
        {
            if (identifier.Parent is InvocationExpressionSyntax
                && LAuditInsideCheck(identifier, type)
                && identifier.FirstAncestorOrSelf<MemberDeclarationSyntax>() is { } caller
                && toggles.Add(caller))
            {
                LAuditToggleCheck(field, caller, writers, violations);
            }
        }

        foreach (IdentifierNameSyntax identifier in LAuditUseRead(field.TFieldSymbols)
                     .OrderBy(identifier => identifier.SyntaxTree.FilePath, StringComparer.Ordinal)
                     .ThenBy(identifier => identifier.SpanStart))
        {
            bool inside = LAuditInsideCheck(identifier, type)
                          || LAuditBind.LAuditSymbolRead(identifier) is IParameterSymbol { RefKind: not RefKind.None };
            bool reached = !inside
                           && LAuditBind.LAuditSymbolRead(identifier) is
                           {
                               DeclaredAccessibility: not Accessibility.Private
                           };
            if (!inside && !reached && !field.TFieldShared)
            {
                continue;
            }

            SyntaxNode reference = LAuditReferenceRead(identifier);
            MemberDeclarationSyntax? scope = reference.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            if (scope is null || scope is FieldDeclarationSyntax)
            {
                continue;
            }

            if (LAuditWriteCheck(reference, out ExpressionSyntax? value))
            {
                if (inside && toggles.Add(scope))
                {
                    LAuditToggleCheck(field, scope, writers, violations);
                }

                if (inside
                    && reference.Parent is AssignmentExpressionSyntax
                    {
                        RawKind: (int)SyntaxKind.CoalesceAssignmentExpression
                    } cache
                    && LAuditRequestCheck(cache.Right))
                {
                    violations.Add(new LViolation(
                        reference.SyntaxTree.FilePath,
                        LAuditLineRead(reference),
                        field.TFieldName,
                        "Duplicating",
                        "caches a request"));
                }

                bool emptied = value is null && reference.Parent is MemberAccessExpressionSyntax;
                foreach (ExpressionSyntax? given in
                         LAuditOriginWalker.LAuditHelperRead(identifier, reference) ?? [value])
                {
                    string verdict = emptied ? "clear" : LAuditOriginWalker.LAuditWriterResolve(given);
                    if (verdict == "engine")
                    {
                        engineWrite ??= reference;
                    }
                    else if (verdict == "plain")
                    {
                        plainWrites.Add(reference);
                    }

                    if (verdict != "clear")
                    {
                        written.Add((reference, given));
                    }
                }

                continue;
            }

            if (!scopes.Add(scope))
            {
                continue;
            }

            LAuditScopeCheck(field, scope, violations);
        }

        if (engineWrite is not null && plainWrites.Count > 0
            && LAuditLookupWalker.LAuditLookupRead(written) is { } looked)
        {
            (engineWrite, plainWrites) = looked;
        }

        if (engineWrite is not null && plainWrites.FirstOrDefault() is { } plainWrite)
        {
            string where = engineWrite.SyntaxTree == plainWrite.SyntaxTree
                ? $"line {LAuditLineRead(engineWrite)}"
                : $"{Path.GetFileName(engineWrite.SyntaxTree.FilePath)}:{LAuditLineRead(engineWrite)}";
            string others = string.Join(", ", plainWrites.Skip(1).Select(write =>
                $"{Path.GetFileName(write.SyntaxTree.FilePath)}:{LAuditLineRead(write)}"));
            string also = others.Length > 0 ? $", also at {others}" : string.Empty;
            violations.Add(new LViolation(
                plainWrite.SyntaxTree.FilePath,
                LAuditLineRead(plainWrite),
                field.TFieldName,
                "Contesting",
                $"written by the engine at {where} and by the shell here{also}"));
        }
    }

    private static void LAuditScopeCheck(
        LAuditTruthField field, MemberDeclarationSyntax scope, List<LViolation> violations)
    {
        HashSet<ISymbol> tainted = LAuditTaintRead(field, scope);
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (IdentifierNameSyntax identifier in scope.DescendantNodes(node => !LAuditNameofCheck(node))
                     .OfType<IdentifierNameSyntax>())
        {
            bool direct = LAuditFieldCheck(identifier, field.TFieldSymbols);
            if (!direct && !LAuditFieldCheck(identifier, tainted))
            {
                continue;
            }

            SyntaxNode reference = LAuditReferenceRead(identifier);
            if (LAuditWriteCheck(reference, out _))
            {
                continue;
            }

            (string LViolationKind, string LViolationReason)? sink = LAuditSinkRead(reference);
            if (sink is null)
            {
                continue;
            }

            string? via = LAuditBind.LAuditSymbolRead(identifier) is IParameterSymbol { RefKind: not RefKind.None }
                ? "ref"
                : direct ? null : "local";
            string reason = via is null
                ? sink.Value.LViolationReason
                : $"{sink.Value.LViolationReason} through {via} '{identifier.Identifier.ValueText}'";
            int line = LAuditLineRead(reference);
            if (seen.Add($"{line}:{sink.Value.LViolationKind}"))
            {
                violations.Add(new LViolation(
                    reference.SyntaxTree.FilePath, line, field.TFieldName, sink.Value.LViolationKind, reason));
            }
        }
    }

    private static HashSet<ISymbol> LAuditTaintRead(LAuditTruthField field, MemberDeclarationSyntax scope)
    {
        HashSet<ISymbol> tainted = new(SymbolEqualityComparer.Default);
        foreach (SyntaxNode node in scope.DescendantNodes())
        {
            switch (node)
            {
                case VariableDeclaratorSyntax { Initializer: not null } declarator
                    when LAuditNameCheck(declarator.Initializer.Value, field.TFieldSymbols):
                    LAuditSymbolAdd(declarator, tainted);
                    break;
                case AssignmentExpressionSyntax { Left: IdentifierNameSyntax local } assignment
                    when LAuditBind.LAuditSymbolRead(local) is ILocalSymbol
                         && LAuditNameCheck(assignment.Right, field.TFieldSymbols):
                    LAuditSymbolAdd(local, tainted);
                    break;
                case IsPatternExpressionSyntax pattern when LAuditNameCheck(pattern.Expression, field.TFieldSymbols):
                    foreach (SingleVariableDesignationSyntax designation in
                             pattern.Pattern.DescendantNodesAndSelf().OfType<SingleVariableDesignationSyntax>())
                    {
                        LAuditSymbolAdd(designation, tainted);
                    }

                    break;
            }
        }

        return tainted;
    }

    private static void LAuditSymbolAdd(SyntaxNode node, HashSet<ISymbol> symbols)
    {
        if (LAuditBind.LAuditSymbolRead(node) is { } symbol)
        {
            symbols.Add(symbol);
        }
    }

    private static SyntaxNode LAuditReferenceRead(IdentifierNameSyntax identifier)
    {
        return identifier.Parent is MemberAccessExpressionSyntax
               {
                   Expression: ThisExpressionSyntax or IdentifierNameSyntax
               } access
               && access.Name == identifier
            ? access
            : identifier;
    }

    internal static bool LAuditWriteCheck(SyntaxNode reference, out ExpressionSyntax? value)
    {
        value = null;
        switch (reference.Parent)
        {
            case AssignmentExpressionSyntax assignment when assignment.Left == reference:
                value = assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) ? assignment.Right : null;
                return true;
            case ElementAccessExpressionSyntax { Parent: AssignmentExpressionSyntax slot } element
                when element.Expression == reference && slot.Left == element:
                value = slot.IsKind(SyntaxKind.SimpleAssignmentExpression) ? slot.Right : null;
                return true;
            case MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax fill } access
                when access.Expression == reference
                     && LAuditTruthSetting.LAuditFillVerbs.Contains(
                         access.Name.Identifier.ValueText, StringComparer.Ordinal):
                value = fill.ArgumentList.Arguments.LastOrDefault()?.Expression;
                return true;
            case PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax:
                return reference.Parent.IsKind(SyntaxKind.PreIncrementExpression)
                       || reference.Parent.IsKind(SyntaxKind.PreDecrementExpression)
                       || reference.Parent.IsKind(SyntaxKind.PostIncrementExpression)
                       || reference.Parent.IsKind(SyntaxKind.PostDecrementExpression);
            case ArgumentSyntax argument:
                return argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword)
                       || argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword);
            default:
                return false;
        }
    }

    private static bool LAuditNameCheck(SyntaxNode node, HashSet<ISymbol> symbols)
    {
        return node.DescendantNodesAndSelf(parent => !LAuditNameofCheck(parent)).OfType<IdentifierNameSyntax>()
            .Any(identifier => LAuditFieldCheck(identifier, symbols));
    }

    private static int LAuditLineRead(SyntaxNode node)
    {
        return node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
    }
}

internal static partial class LAuditTruthWalker
{
    private static void LAuditToggleCheck(
        LAuditTruthField field, MemberDeclarationSyntax scope, HashSet<ISymbol> writers, List<LViolation> violations)
    {
        List<SyntaxNode> writes = scope.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(identifier => LAuditFieldCheck(identifier, field.TFieldSymbols)
                                 || (identifier.Parent is InvocationExpressionSyntax
                                     && LAuditBind.LAuditSymbolRead(identifier) is { } callee
                                     && writers.Contains(callee)))
            .Select(LAuditReferenceRead)
            .Where(reference => reference.Parent is not MemberAccessExpressionSyntax)
            .Where(reference => LAuditWriteCheck(reference, out _) || reference.Parent is InvocationExpressionSyntax)
            .ToList();
        if (writes.Count < 2)
        {
            return;
        }

        bool toggled = scope.DescendantNodes()
            .Where(node => node is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax)
            .Where(node => LAuditCallRead((ExpressionSyntax)node) is not null)
            .Any(request => writes.Any(write => write.SpanStart < request.SpanStart
                                                && write.FirstAncestorOrSelf<BlockSyntax>()?.Span
                                                    .Contains(request.Span) == true)
                            && writes.Any(write => write.SpanStart > request.SpanStart
                                                   && LAuditBlockRead(write)?.Span.Contains(request.Span) == true));
        if (toggled)
        {
            SyntaxNode last = writes[^1];
            string through = last is IdentifierNameSyntax { Parent: InvocationExpressionSyntax } relay
                ? $" through {relay.Identifier.ValueText}"
                : string.Empty;
            violations.Add(new LViolation(
                last.SyntaxTree.FilePath,
                LAuditLineRead(last),
                field.TFieldName,
                "Gatekeeping",
                $"toggled around a request{through}"));
        }
    }

    private static BlockSyntax? LAuditBlockRead(SyntaxNode write)
    {
        BlockSyntax? block = write.FirstAncestorOrSelf<BlockSyntax>();
        while (block?.Parent is FinallyClauseSyntax or CatchClauseSyntax)
        {
            block = block.Parent.Parent?.FirstAncestorOrSelf<BlockSyntax>();
        }

        return block;
    }

    private static HashSet<ISymbol> LAuditWriterRead(LAuditTruthField field, IReadOnlyList<TypeDeclarationSyntax> type)
    {
        HashSet<ISymbol> writers = new(SymbolEqualityComparer.Default);
        foreach (MethodDeclarationSyntax method in type.SelectMany(part =>
                     part.DescendantNodes().OfType<MethodDeclarationSyntax>()))
        {
            if (method.Body is not { Statements.Count: > 0 } body || LAuditRequestCheck(body))
            {
                continue;
            }

            bool writes = LAuditUseRead(field.TFieldSymbols)
                .Where(identifier => identifier.SyntaxTree == body.SyntaxTree && body.Span.Contains(identifier.Span))
                .Select(LAuditReferenceRead)
                .Where(reference => reference.Parent is not MemberAccessExpressionSyntax)
                .Any(reference => LAuditWriteCheck(reference, out _));
            if (writes && LAuditBind.LAuditSymbolRead(method) is { } symbol)
            {
                writers.Add(symbol);
            }
        }

        return writers;
    }

    private static void LAuditBaseCheck(IReadOnlyList<TypeDeclarationSyntax> type, List<LViolation> violations)
    {
        foreach (TypeDeclarationSyntax part in type)
        {
            foreach (BaseTypeSyntax baseType in part.BaseList?.Types ?? [])
            {
                ITypeSymbol? symbol = LAuditBind.LAuditTypeRead(baseType.Type);
                if (symbol is null || !LAuditBind.LAuditEngineCheck(symbol))
                {
                    continue;
                }

                violations.Add(new LViolation(
                    part.SyntaxTree.FilePath,
                    LAuditLineRead(baseType),
                    part.Identifier.ValueText,
                    "Duplicating",
                    $"derives from {symbol.Name}"));
            }
        }
    }

    private static IEnumerable<MemberDeclarationSyntax> LAuditScopeRead(IReadOnlyList<TypeDeclarationSyntax> type)
    {
        return type
            .SelectMany(part => part.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>())
            .SelectMany(part => part.Members)
            .Where(scope => scope is not BaseTypeDeclarationSyntax);
    }

    private static void LAuditLocalScan(IReadOnlyList<TypeDeclarationSyntax> type, List<LViolation> violations)
    {
        foreach (MemberDeclarationSyntax scope in LAuditScopeRead(type))
        {
            HashSet<ISymbol> answered = LAuditAnsweredRead(scope);
            if (answered.Count == 0)
            {
                continue;
            }

            string member = LAuditMemberRead(scope);

            HashSet<string> seen = new(StringComparer.Ordinal);
            foreach (IdentifierNameSyntax identifier in scope.DescendantNodes(node => !LAuditNameofCheck(node))
                         .OfType<IdentifierNameSyntax>())
            {
                if (!LAuditFieldCheck(identifier, answered) || LAuditWriteCheck(identifier, out _))
                {
                    continue;
                }

                SyntaxNode carried = identifier;
                while (carried.Parent is MemberAccessExpressionSyntax { Expression: var owner } access
                       && owner == carried)
                {
                    carried = access;
                }

                (string LViolationKind, string LViolationReason)? sink = LAuditSinkRead(carried);
                if (sink is null)
                {
                    continue;
                }

                string name = identifier.Identifier.ValueText;
                int line = LAuditLineRead(identifier);
                if (seen.Add($"{line}:{name}:{sink.Value.LViolationKind}"))
                {
                    violations.Add(new LViolation(
                        identifier.SyntaxTree.FilePath,
                        line,
                        $"{member}.{name}",
                        sink.Value.LViolationKind,
                        $"engine answer {sink.Value.LViolationReason} through local '{name}'"));
                }
            }
        }
    }

    private static HashSet<ISymbol> LAuditAnsweredRead(MemberDeclarationSyntax scope)
    {
        HashSet<ISymbol> answered = new(SymbolEqualityComparer.Default);
        foreach (SyntaxNode node in scope.DescendantNodes())
        {
            switch (node)
            {
                case VariableDeclaratorSyntax { Initializer.Value: var value } declarator
                    when LAuditAnswerCheck(value):
                    LAuditSymbolAdd(declarator, answered);
                    break;
                case AssignmentExpressionSyntax { Left: IdentifierNameSyntax local } assignment
                    when LAuditBind.LAuditSymbolRead(local) is ILocalSymbol && LAuditAnswerCheck(assignment.Right):
                    LAuditSymbolAdd(local, answered);
                    break;
                case AssignmentExpressionSyntax assignment when LAuditAnswerCheck(assignment.Right):
                    LAuditDesignationAdd(assignment.Left, answered);
                    break;
                case ForEachStatementSyntax loop when LAuditAnswerCheck(loop.Expression):
                    LAuditSymbolAdd(loop, answered);
                    break;
                case ForEachVariableStatementSyntax loop when LAuditAnswerCheck(loop.Expression):
                    LAuditDesignationAdd(loop.Variable, answered);
                    break;
                case IsPatternExpressionSyntax pattern when LAuditAnswerCheck(pattern.Expression):
                    LAuditDesignationAdd(pattern.Pattern, answered);
                    break;
                case InvocationExpressionSyntax call when LAuditCallRead(call) is not null:
                    foreach (ArgumentSyntax argument in call.ArgumentList.Arguments)
                    {
                        if (argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
                        {
                            LAuditDesignationAdd(argument.Expression, answered);
                        }

                        foreach (ParameterSyntax parameter in argument.Expression switch
                                 {
                                     SimpleLambdaExpressionSyntax simple => [simple.Parameter],
                                     ParenthesizedLambdaExpressionSyntax full => full.ParameterList.Parameters,
                                     _ => (IEnumerable<ParameterSyntax>)[]
                                 })
                        {
                            LAuditSymbolAdd(parameter, answered);
                        }
                    }

                    break;
            }
        }

        return answered;
    }

    private static void LAuditDesignationAdd(SyntaxNode node, HashSet<ISymbol> answered)
    {
        foreach (SingleVariableDesignationSyntax designation in
                 node.DescendantNodesAndSelf().OfType<SingleVariableDesignationSyntax>())
        {
            LAuditSymbolAdd(designation, answered);
        }

        foreach (IdentifierNameSyntax name in node.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
        {
            if (name.Parent is TupleExpressionSyntax or ArgumentSyntax { Parent: TupleExpressionSyntax }
                && LAuditBind.LAuditSymbolRead(name) is ILocalSymbol local)
            {
                answered.Add(local);
            }
        }
    }

    private static string LAuditMemberRead(MemberDeclarationSyntax scope)
    {
        return scope switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
            EventDeclarationSyntax happening => happening.Identifier.ValueText,
            BaseFieldDeclarationSyntax field => field.Declaration.Variables[0].Identifier.ValueText,
            _ => scope.Kind().ToString()
        };
    }

    private static bool LAuditAnswerCheck(ExpressionSyntax value)
    {
        return value.DescendantNodesAndSelf(node => !LAuditNameofCheck(node)).Any(node => node switch
        {
            InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax
                => LAuditCallRead((ExpressionSyntax)node) is not null,
            MemberAccessExpressionSyntax or MemberBindingExpressionSyntax
                => LAuditBind.LAuditLogicCheck(LAuditBind.LAuditSymbolRead(node)),
            _ => false
        });
    }
}

internal static partial class LAuditTruthWalker
{
    private static void LAuditTamperingScan(SyntaxNode root, List<LViolation> violations)
    {
        LAuditSpoonfeedingScan(root, violations);
        foreach (AssignmentExpressionSyntax assignment in root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                continue;
            }

            if (assignment.Left is TupleExpressionSyntax tuple
                && tuple.Arguments.Any(argument => argument.Expression is ElementAccessExpressionSyntax))
            {
                ElementAccessExpressionSyntax slot = tuple.Arguments
                    .Select(argument => argument.Expression)
                    .OfType<ElementAccessExpressionSyntax>()
                    .First();
                violations.Add(new LViolation(
                    root.SyntaxTree.FilePath,
                    LAuditLineRead(assignment),
                    slot.Expression.ToString(),
                    "Tampering",
                    "reorders a collection with a swap"));
                continue;
            }

            if (assignment.Parent is InitializerExpressionSyntax { Parent: WithExpressionSyntax }
                && assignment.Left is IdentifierNameSyntax field
                && LAuditBind.LAuditLogicCheck(LAuditBind.LAuditSymbolRead(field)))
            {
                violations.Add(new LViolation(
                    root.SyntaxTree.FilePath,
                    LAuditLineRead(assignment),
                    field.Identifier.ValueText,
                    "Tampering",
                    "overrides a logic member in a record copy"));
                continue;
            }

            if (assignment.Left is ElementAccessExpressionSyntax { Expression: var rows }
                && LAuditStoreCheck(rows)
                && !LAuditFreshCheck(rows))
            {
                violations.Add(new LViolation(
                    root.SyntaxTree.FilePath,
                    LAuditLineRead(assignment),
                    rows.ToString(),
                    "Tampering",
                    "overwrites a slot of a row store"));
                continue;
            }

            if (assignment.Parent is InitializerExpressionSyntax
                || assignment.Left is not MemberAccessExpressionSyntax target
                || !LAuditBind.LAuditLogicCheck(LAuditBind.LAuditSymbolRead(target)))
            {
                continue;
            }

            violations.Add(new LViolation(
                root.SyntaxTree.FilePath,
                LAuditLineRead(assignment),
                target.ToString(),
                "Tampering",
                "assigns a logic member from the shell"));
        }

        foreach (InvocationExpressionSyntax call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (call.Expression is not MemberAccessExpressionSyntax access
                || !LAuditTruthSetting.LAuditOrderVerbs.Contains(
                    access.Name.Identifier.ValueText, StringComparer.Ordinal)
                || !LAuditStoreCheck(access.Expression))
            {
                continue;
            }

            violations.Add(new LViolation(
                root.SyntaxTree.FilePath,
                LAuditLineRead(call),
                access.Expression.ToString(),
                "Tampering",
                $"reorders a collection with {access.Name.Identifier.ValueText}"));
        }
    }

    private static void LAuditSpoonfeedingScan(SyntaxNode root, List<LViolation> violations)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            IEnumerable<ExpressionSyntax> values = node switch
            {
                AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax target } assignment
                    when LAuditSurfaceRead(target.Expression)
                    => [assignment.Right],
                AssignmentExpressionSyntax
                    {
                        Left: IdentifierNameSyntax,
                        Parent: InitializerExpressionSyntax { Parent: BaseObjectCreationExpressionSyntax creation }
                    } assignment
                    when LAuditSurfaceRead(creation)
                    => [assignment.Right],
                InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } call
                    when LAuditSurfaceRead(access.Expression)
                    => call.ArgumentList.Arguments.Select(argument => argument.Expression),
                BaseObjectCreationExpressionSyntax { ArgumentList: { } arguments } creation
                    when LAuditSurfaceRead(creation)
                    => arguments.Arguments.Select(argument => argument.Expression),
                _ => []
            };
            foreach (ExpressionSyntax value in values)
            {
                if (value is AnonymousFunctionExpressionSyntax
                    || LAuditBind.LAuditTypeRead(value) is not { } type
                    || !LAuditBind.LAuditLogicCheck(type))
                {
                    continue;
                }

                int line = LAuditLineRead(value);
                string shown = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                if (seen.Add($"{line}:{shown}"))
                {
                    violations.Add(new LViolation(
                        root.SyntaxTree.FilePath,
                        line,
                        value.ToString(),
                        "Spoonfeeding",
                        $"hands the surface a {shown}"));
                }
            }
        }
    }

    private static bool LAuditSurfaceRead(ExpressionSyntax receiver)
    {
        for (ExpressionSyntax? current = receiver; current is not null;)
        {
            ITypeSymbol? type = LAuditBind.LAuditTypeRead(current);
            if (LAuditBind.LAuditControlCheck(type) || LAuditBind.LAuditSurfaceCheck(type))
            {
                return true;
            }

            current = current switch
            {
                MemberAccessExpressionSyntax access => access.Expression,
                ElementAccessExpressionSyntax element => element.Expression,
                InvocationExpressionSyntax call => call.Expression,
                _ => null
            };
        }

        return false;
    }

    private static void LAuditMismatchingScan(List<LViolation> violations)
    {
        IReadOnlyList<string> drivers = LAuditBind.LAuditRootRead(LAuditTruthSetting.LAuditTruthInclude);
        Dictionary<ISymbol, Dictionary<string, List<SyntaxNode>>> calls = new(SymbolEqualityComparer.Default);
        Dictionary<string, List<INamedTypeSymbol>> declared = drivers.ToDictionary(
            driver => driver, _ => new List<INamedTypeSymbol>(), StringComparer.Ordinal);
        foreach (SyntaxNode root in LAuditRoots)
        {
            string relative = LAuditBind.LAuditRelativeRead(root.SyntaxTree.FilePath);
            string driver = drivers.First(folder =>
                relative.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase));
            declared[driver].AddRange(root.DescendantNodes().OfType<TypeDeclarationSyntax>()
                .Select(type => LAuditBind.LAuditSymbolRead(type)).OfType<INamedTypeSymbol>());
            foreach (SimpleNameSyntax name in root.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (LAuditBind.LAuditSymbolRead(name) is not { } member
                    || member is not (IMethodSymbol or IPropertySymbol)
                    || !LAuditBind.LAuditConductCheck(member.ContainingType))
                {
                    continue;
                }

                if (!calls.TryGetValue(member, out Dictionary<string, List<SyntaxNode>>? sites))
                {
                    sites = new Dictionary<string, List<SyntaxNode>>(StringComparer.Ordinal);
                    calls[member] = sites;
                }

                if (!sites.TryGetValue(driver, out List<SyntaxNode>? found))
                {
                    found = [];
                    sites[driver] = found;
                }

                found.Add(name);
            }
        }

        foreach ((ISymbol member, Dictionary<string, List<SyntaxNode>> sites) in calls)
        {
            string missing = string.Join(", ", drivers.Where(driver => !sites.ContainsKey(driver)));
            foreach (SyntaxNode site in missing.Length == 0 ? [] : sites.Values.SelectMany(found => found)
                         .GroupBy(site => site.SyntaxTree.FilePath, StringComparer.Ordinal)
                         .Select(file => file.First()))
            {
                violations.Add(new LViolation(
                    site.SyntaxTree.FilePath,
                    LAuditLineRead(site),
                    LAuditBind.LAuditLabelRead(member),
                    "Mismatching",
                    $"reaches a Conduct member that {missing} never reaches"));
            }
        }

        foreach (INamedTypeSymbol port in LAuditBind.LAuditCompilation
                     .GetSymbolsWithName(_ => true, SymbolFilter.Type)
                     .OfType<INamedTypeSymbol>()
                     .Where(type => type.TypeKind == TypeKind.Interface && LAuditBind.LAuditConductCheck(type)))
        {
            foreach (string driver in drivers.Where(driver => !declared[driver].Any(type =>
                         type.AllInterfaces.Contains(port, SymbolEqualityComparer.Default))))
            {
                Location place = port.Locations.First(location => location.IsInSource);
                violations.Add(new LViolation(
                    place.SourceTree!.FilePath,
                    place.GetLineSpan().StartLinePosition.Line + 1,
                    port.Name,
                    "Mismatching",
                    $"is a Conduct port no type in {driver} implements"));
            }
        }
    }

    private static bool LAuditFreshCheck(ExpressionSyntax rows)
    {
        if (LAuditBind.LAuditSymbolRead(rows) is not ILocalSymbol local)
        {
            return false;
        }

        return local.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<VariableDeclaratorSyntax>()
            .Any(declarator => declarator.Initializer?.Value
                is CollectionExpressionSyntax { Elements.Count: 0 }
                or BaseObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0, Initializer: null });
    }

    private static bool LAuditStoreCheck(ExpressionSyntax rows)
    {
        ITypeSymbol? type = LAuditBind.LAuditTypeRead(rows);
        if (type is null || type.TypeKind == TypeKind.Error)
        {
            return true;
        }

        IEnumerable<ITypeSymbol> held = type switch
        {
            IArrayTypeSymbol array => [array.ElementType],
            INamedTypeSymbol named => named.TypeArguments,
            _ => []
        };
        return held.Any(part => LAuditBind.LAuditLogicCheck(part)
                                || LAuditBind.LAuditShellCheck(part)
                                || part.SpecialType == SpecialType.System_Object);
    }
}

internal static partial class LAuditTruthWalker
{
    private static HashSet<ISymbol> LAuditRelayNames = new(SymbolEqualityComparer.Default);

    private static HashSet<ISymbol>? LAuditPuppetNames;

    internal static HashSet<ISymbol> LAuditReaderNames = new(SymbolEqualityComparer.Default);

    private static Dictionary<ISymbol, HashSet<int>> LAuditHotNames = new(SymbolEqualityComparer.Default);

    private static Dictionary<ISymbol, HashSet<int>> LAuditZeroingNames = new(SymbolEqualityComparer.Default);

    private static HashSet<ISymbol> LAuditZeroingReads = new(SymbolEqualityComparer.Default);

    private static void LAuditRelayRead(IReadOnlyList<TypeDeclarationSyntax> type)
    {
        LAuditRelayNames = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        LAuditReaderNames = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        LAuditHotNames = new Dictionary<ISymbol, HashSet<int>>(SymbolEqualityComparer.Default);
        LAuditPuppetNames = null;
        List<SyntaxNode> declared = type
            .SelectMany(part => part.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>())
            .SelectMany(part => part.Members)
            .Where(member => member is MethodDeclarationSyntax or PropertyDeclarationSyntax)
            .Cast<SyntaxNode>()
            .ToList();
        declared.AddRange(declared.SelectMany(member => member.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
            .ToList());
        List<(ISymbol LAuditRelaySymbol, SyntaxNode LAuditRelayMember)> members = declared
            .Select(member => (LAuditBind.LAuditSymbolRead(member), member))
            .Where(pair => pair.Item1 is not null)
            .Select(pair => (pair.Item1!, pair.member))
            .ToList();
        List<SyntaxNode> roots = LAuditBind.LAuditTrees.Select(tree => tree.GetRoot()).ToList();
        List<AssignmentExpressionSyntax> wiring = roots
            .SelectMany(root => root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            .Where(assignment => assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                                 || assignment.IsKind(SyntaxKind.AddAssignmentExpression))
            .ToList();
        List<VariableDeclaratorSyntax> locals = roots
            .SelectMany(root => root.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            .Where(declarator => declarator.Initializer is not null)
            .ToList();
        Dictionary<ISymbol, List<ExpressionSyntax>> seams = LAuditSeamRead(roots);
        bool grown = true;
        while (grown)
        {
            grown = false;
            foreach ((ISymbol symbol, SyntaxNode member) in members)
            {
                if (!LAuditRelayNames.Contains(symbol) && LAuditRequestCheck(member))
                {
                    LAuditRelayNames.Add(symbol);
                    grown = true;
                }

                if (!LAuditReaderNames.Contains(symbol) && (LAuditReadCheck(member) || LAuditRequestCheck(member)))
                {
                    LAuditReaderNames.Add(symbol);
                    grown = true;
                }

                ParameterListSyntax? parameters = member switch
                {
                    MethodDeclarationSyntax method => method.ParameterList,
                    LocalFunctionStatementSyntax local => local.ParameterList,
                    _ => null
                };
                if (parameters is not null && LAuditHotRead(symbol, member, parameters))
                {
                    grown = true;
                }
            }

            foreach (VariableDeclaratorSyntax local in locals)
            {
                if (LAuditBind.LAuditSymbolRead(local) is ILocalSymbol { Type.TypeKind: TypeKind.Delegate } held
                    && !LAuditRelayNames.Contains(held)
                    && LAuditSeamCheck(local.Initializer!.Value, seams, new(SymbolEqualityComparer.Default)))
                {
                    LAuditRelayNames.Add(held);
                    grown = true;
                }
            }

            foreach (AssignmentExpressionSyntax assignment in wiring)
            {
                if (LAuditBind.LAuditSymbolRead(assignment.Left) is { } held
                    && held switch
                    {
                        IFieldSymbol field => field.Type,
                        IEventSymbol happening => happening.Type,
                        IPropertySymbol property => property.Type,
                        ILocalSymbol local => local.Type,
                        _ => null
                    } is { TypeKind: TypeKind.Delegate }
                    && (LAuditBind.LAuditShellCheck(held.ContainingType)
                        || held is IEventSymbol { ContainingType.TypeKind: TypeKind.Interface })
                    && (held is not IEventSymbol || LAuditPuppetNames is not null)
                    && !LAuditRelayNames.Contains(held)
                    && LAuditSeamCheck(assignment.Right, seams, new(SymbolEqualityComparer.Default)))
                {
                    LAuditRelayNames.Add(held);
                    grown = true;
                }
            }

            if (!grown && LAuditPuppetNames is null)
            {
                LAuditPuppetNames = new HashSet<ISymbol>(LAuditRelayNames, SymbolEqualityComparer.Default);
                grown = true;
            }
        }

        LAuditZeroingRead(type);
    }

    private static Dictionary<ISymbol, List<ExpressionSyntax>> LAuditSeamRead(IReadOnlyList<SyntaxNode> roots)
    {
        Dictionary<ISymbol, List<ExpressionSyntax>> seams = new(SymbolEqualityComparer.Default);
        foreach (ArgumentSyntax argument in roots.SelectMany(root => root.DescendantNodes().OfType<ArgumentSyntax>()))
        {
            if (LAuditParameterRead(argument) is not { Type.TypeKind: TypeKind.Delegate } parameter)
            {
                continue;
            }

            if (!seams.TryGetValue(parameter, out List<ExpressionSyntax>? passed))
            {
                passed = [];
                seams[parameter] = passed;
            }

            passed.Add(argument.Expression);
        }

        return seams;
    }

    private static bool LAuditSeamCheck(
        ExpressionSyntax value, IReadOnlyDictionary<ISymbol, List<ExpressionSyntax>> seams, HashSet<ISymbol> seen)
    {
        return LAuditDelegateCheck(value)
               || (LAuditBind.LAuditSymbolRead(value) is IParameterSymbol parameter
                   && seen.Add(parameter)
                   && seams.TryGetValue(parameter, out List<ExpressionSyntax>? passed)
                   && passed.Any(argument => LAuditSeamCheck(argument, seams, seen)));
    }

    private static IParameterSymbol? LAuditParameterRead(ArgumentSyntax argument)
    {
        if (argument.Parent is not BaseArgumentListSyntax { Parent: { } call } list
            || call is not (ExpressionSyntax or ConstructorInitializerSyntax or PrimaryConstructorBaseTypeSyntax)
            || LAuditBind.LAuditSymbolRead(call) is not IMethodSymbol callee)
        {
            return null;
        }

        return argument.NameColon is { Name.Identifier.ValueText: var name }
            ? callee.Parameters.FirstOrDefault(parameter => parameter.Name == name)
            : callee.Parameters.ElementAtOrDefault(list.Arguments.IndexOf(argument));
    }

    private static bool LAuditDelegateCheck(ExpressionSyntax value)
    {
        return LAuditRequestCheck(value)
               || value.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(name =>
                   name.Parent is not InvocationExpressionSyntax
                   && LAuditBind.LAuditSymbolRead(name) is { } symbol
                   && (LAuditRelayNames.Contains(symbol)
                       || (symbol is IMethodSymbol method && LAuditBind.LAuditLogicCheck(method))));
    }

    private static bool LAuditHotRead(ISymbol symbol, SyntaxNode method, ParameterListSyntax list)
    {
        if (!LAuditHotNames.TryGetValue(symbol, out HashSet<int>? hot))
        {
            hot = [];
            LAuditHotNames[symbol] = hot;
        }

        List<ISymbol?> parameters = list.Parameters
            .Select(parameter => LAuditBind.LAuditSymbolRead(parameter))
            .ToList();
        bool grown = false;
        foreach (ArgumentSyntax argument in method.DescendantNodes().OfType<ArgumentSyntax>())
        {
            if (argument.Parent?.Parent is not ExpressionSyntax call
                || LAuditCallRead(call) is not { } callee
                || !LAuditHotCheck(callee, argument))
            {
                continue;
            }

            foreach (IdentifierNameSyntax used in argument.Expression.DescendantNodesAndSelf()
                         .OfType<IdentifierNameSyntax>())
            {
                ISymbol? usedSymbol = LAuditBind.LAuditSymbolRead(used);
                int index = usedSymbol is null
                    ? -1
                    : parameters.FindIndex(parameter => SymbolEqualityComparer.Default.Equals(parameter, usedSymbol));
                if (index >= 0 && hot.Add(index))
                {
                    grown = true;
                }
            }
        }

        return grown;
    }

    private static void LAuditZeroingRead(IReadOnlyList<TypeDeclarationSyntax> type)
    {
        LAuditZeroingNames = LAuditHotNames.ToDictionary(
            pair => pair.Key, pair => new HashSet<int>(pair.Value), SymbolEqualityComparer.Default);
        LAuditZeroingReads = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        List<TypeDeclarationSyntax> nested = type
            .SelectMany(part => part.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>())
            .ToList();
        List<SyntaxNode> declared = nested
            .SelectMany(part => part.Members)
            .Where(member => member is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax)
            .Cast<SyntaxNode>()
            .ToList();
        declared.AddRange(declared.SelectMany(member => member.DescendantNodes().OfType<LocalFunctionStatementSyntax>())
            .ToList());
        List<ArgumentSyntax> arguments = type
            .SelectMany(part => part.DescendantNodes().OfType<ArgumentSyntax>())
            .ToList();
        Dictionary<ISymbol, HashSet<ISymbol>> aliases = new(SymbolEqualityComparer.Default);
        bool grown = true;
        while (grown)
        {
            grown = false;
            foreach (IdentifierNameSyntax used in arguments
                         .Where(argument => LAuditZeroingCheck(argument, out _))
                         .SelectMany(argument => argument.Expression.DescendantNodesAndSelf()
                             .OfType<IdentifierNameSyntax>()))
            {
                if (LAuditBind.LAuditSymbolRead(used) is { Kind: SymbolKind.Field or SymbolKind.Property } held
                    && LAuditBind.LAuditShellCheck(held.ContainingType)
                    && LAuditZeroingReads.Add(held))
                {
                    grown = true;
                }
            }

            foreach (SyntaxNode member in declared)
            {
                ParameterListSyntax? list = member switch
                {
                    BaseMethodDeclarationSyntax method => method.ParameterList,
                    LocalFunctionStatementSyntax local => local.ParameterList,
                    _ => null
                };
                if (list is null || LAuditBind.LAuditSymbolRead(member) is not { } symbol)
                {
                    continue;
                }

                if (!LAuditZeroingNames.TryGetValue(symbol, out HashSet<int>? hot))
                {
                    hot = [];
                    LAuditZeroingNames[symbol] = hot;
                }

                List<ISymbol?> parameters = list.Parameters
                    .Select(parameter => LAuditBind.LAuditSymbolRead(parameter))
                    .ToList();
                IEnumerable<ExpressionSyntax> carried = member.DescendantNodes()
                    .OfType<ArgumentSyntax>()
                    .Where(argument => LAuditZeroingCheck(argument, out _))
                    .Select(argument => argument.Expression)
                    .Concat(member.DescendantNodes()
                        .OfType<AssignmentExpressionSyntax>()
                        .Where(assignment => assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                                             && LAuditStoreRead(assignment.Left, nested, aliases)
                                                 .Overlaps(LAuditZeroingReads))
                        .Select(assignment => assignment.Right));
                foreach (IdentifierNameSyntax used in carried.SelectMany(value => value.DescendantNodesAndSelf()
                             .OfType<IdentifierNameSyntax>()))
                {
                    ISymbol? usedSymbol = LAuditBind.LAuditSymbolRead(used);
                    int index = usedSymbol is null
                        ? -1
                        : parameters.FindIndex(parameter => SymbolEqualityComparer.Default.Equals(parameter, usedSymbol));
                    if (index >= 0 && hot.Add(index))
                    {
                        grown = true;
                    }
                }
            }
        }
    }

    private static HashSet<ISymbol> LAuditStoreRead(
        ExpressionSyntax target, IReadOnlyList<TypeDeclarationSyntax> nested, Dictionary<ISymbol, HashSet<ISymbol>> aliases)
    {
        ISymbol? stored = LAuditBind.LAuditSymbolRead(target);
        if (stored is not (IFieldSymbol or IPropertySymbol))
        {
            return [];
        }

        if (!aliases.TryGetValue(stored, out HashSet<ISymbol>? names))
        {
            names = stored is IFieldSymbol field
                ? LAuditAliasRead(field, nested
                    .Where(part => SymbolEqualityComparer.Default.Equals(
                        LAuditBind.LAuditSymbolRead(part), field.ContainingType))
                    .ToList())
                : new HashSet<ISymbol>([stored], SymbolEqualityComparer.Default);
            aliases[stored] = names;
        }

        return names;
    }

    private static bool LAuditReadCheck(SyntaxNode member)
    {
        return member.DescendantNodes(node => !LAuditNameofCheck(node)).Any(node => node switch
        {
            MemberAccessExpressionSyntax or MemberBindingExpressionSyntax
                => LAuditBind.LAuditLogicCheck(LAuditBind.LAuditSymbolRead(node)),
            InvocationExpressionSyntax call
                => LAuditBind.LAuditSymbolRead(call) is { } callee && LAuditReaderNames.Contains(callee),
            _ => false
        });
    }

    private static bool LAuditHandleCheck(ITypeSymbol type)
    {
        string shown = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        return LAuditBind.LAuditConductCheck(type)
               || LAuditTruthSetting.LAuditTruthHandles.Contains(shown, StringComparer.Ordinal)
               || LAuditTruthSetting.LAuditTruthHandles.Contains(shown.TrimEnd('?'), StringComparer.Ordinal);
    }

    private static HashSet<ISymbol> LAuditAliasRead(IFieldSymbol field, IReadOnlyList<TypeDeclarationSyntax> type)
    {
        HashSet<ISymbol> symbols = new([field], SymbolEqualityComparer.Default);
        List<PropertyDeclarationSyntax> getters = type
            .SelectMany(part => part.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>())
            .SelectMany(part => part.Members.OfType<PropertyDeclarationSyntax>())
            .Where(property => property.AccessorList?.Accessors.All(accessor =>
                accessor.IsKind(SyntaxKind.GetAccessorDeclaration)) != false)
            .ToList();
        bool grown = true;
        while (grown)
        {
            grown = false;
            foreach (PropertyDeclarationSyntax property in getters)
            {
                if (LAuditBind.LAuditSymbolRead(property) is IPropertySymbol alias
                    && !symbols.Contains(alias)
                    && !LAuditRequestCheck(property)
                    && LAuditNameCheck(property, symbols))
                {
                    symbols.Add(alias);
                    grown = true;
                }
            }

            foreach (ArgumentSyntax argument in LAuditUseRead(symbols.ToList())
                         .Select(identifier => identifier.Parent)
                         .OfType<ArgumentSyntax>()
                         .Where(argument => !argument.RefKindKeyword.IsKind(SyntaxKind.None)))
            {
                if (LAuditParameterRead(argument) is { } parameter && symbols.Add(parameter))
                {
                    grown = true;
                }
            }
        }

        return symbols;
    }

    private static void LAuditPuppeteeringScan(List<LViolation> violations)
    {
        HashSet<string> walked = LAuditRoots
            .Select(root => root.SyntaxTree.FilePath)
            .ToHashSet(StringComparer.Ordinal);
        List<LViolation> hits = [];
        foreach (ISymbol member in LAuditPuppetNames ?? [])
        {
            if (member is not (IMethodSymbol { MethodKind: not MethodKind.LocalFunction }
                    or IPropertySymbol or IFieldSymbol or IEventSymbol)
                || member.ContainingType is not { } type
                || member.Locations
                    .Where(location => location.SourceTree is { } tree && walked.Contains(tree.FilePath))
                    .OrderBy(location => location.SourceTree!.FilePath, StringComparer.Ordinal)
                    .ThenBy(location => location.SourceSpan.Start)
                    .FirstOrDefault() is not { SourceTree: { } source } location
                || !LAuditControlCheck(type.Name, source.FilePath))
            {
                continue;
            }

            hits.Add(new LViolation(
                source.FilePath,
                location.GetLineSpan().StartLinePosition.Line + 1,
                $"{type.Name}.{member.Name}",
                "Puppeteering",
                "a control member requests logic"));
        }

        violations.AddRange(hits
            .OrderBy(hit => hit.LViolationPath, StringComparer.Ordinal)
            .ThenBy(hit => hit.LViolationLine)
            .ThenBy(hit => hit.LViolationName, StringComparer.Ordinal));
    }

    internal static bool LAuditControlCheck(string name, string path)
    {
        string relative = LAuditBind.LAuditRelativeRead(path);
        return LAuditTruthSetting.LAuditControlPrefix.Any(ring =>
            relative.StartsWith(ring.Key + "/", StringComparison.OrdinalIgnoreCase)
            && name.StartsWith(ring.Value, StringComparison.Ordinal));
    }
}

internal static partial class LAuditTruthWalker
{
    private static HashSet<ISymbol> LAuditSendNames = new(SymbolEqualityComparer.Default);

    private static void LAuditSendResolve(IReadOnlyList<TypeDeclarationSyntax> parts)
    {
        LAuditSendNames = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (MemberDeclarationSyntax member in LAuditScopeRead(parts))
        {
            if (member is not (MethodDeclarationSyntax or PropertyDeclarationSyntax)
                || LAuditBind.LAuditSymbolRead(member) is not { } symbol)
            {
                continue;
            }

            if (LAuditSendRead(member, true).Count > 0)
            {
                LAuditSendNames.Add(symbol);
            }
        }
    }

    private static List<SyntaxNode> LAuditSendRead(SyntaxNode scope, bool direct)
    {
        List<SyntaxNode> sends = [];
        foreach (SyntaxNode node in scope.DescendantNodes())
        {
            bool send = node switch
            {
                InvocationExpressionSyntax call => LAuditSendCheck(call, direct),
                ObjectCreationExpressionSyntax creation
                    => LAuditBind.LAuditTypeRead(creation.Type) is { } built
                       && LAuditBind.LAuditLogicCheck(built)
                       && built.Name.StartsWith(LAuditTruthSetting.LAuditRequestPrefix, StringComparison.Ordinal),
                _ => false
            };
            if (send
                && node.Ancestors().OfType<InvocationExpressionSyntax>().Any(call => LAuditSendCheck(call, direct)))
            {
                continue;
            }

            if (send)
            {
                sends.Add(node);
            }
        }

        return sends;
    }

    private static bool LAuditSendCheck(InvocationExpressionSyntax call, bool direct)
    {
        if (LAuditBind.LAuditSymbolRead(call) is not { } callee)
        {
            return false;
        }

        return (LAuditBind.LAuditLogicCheck(callee)
                && LAuditTruthSetting.LAuditSendRoots.Contains(callee.Name, StringComparer.Ordinal))
               || LAuditGateCheck(callee)
               || (!direct && LAuditSendNames.Contains(callee));
    }

    private static bool LAuditGateCheck(ISymbol callee)
    {
        return callee is IMethodSymbol { MethodKind: MethodKind.Ordinary, IsStatic: false } method
               && LAuditBind.LAuditConductCheck(method.ContainingType);
    }

    private static void LAuditSequenceScan(IReadOnlyList<TypeDeclarationSyntax> type, List<LViolation> violations)
    {
        foreach (MemberDeclarationSyntax scope in LAuditScopeRead(type))
        {
            List<SyntaxNode> sends = LAuditSendRead(scope, false);
            for (int later = 1; later < sends.Count; later++)
            {
                for (int earlier = 0; earlier < later; earlier++)
                {
                    if (LAuditExclusiveCheck(sends[earlier], sends[later]))
                    {
                        continue;
                    }

                    violations.Add(new LViolation(
                        scope.SyntaxTree.FilePath,
                        LAuditLineRead(sends[later]),
                        LAuditMemberRead(scope),
                        "Misfiring",
                        $"sends a second request after line {LAuditLineRead(sends[earlier])}"));
                    later = sends.Count;
                    break;
                }
            }
        }
    }

    private static bool LAuditExclusiveCheck(SyntaxNode earlier, SyntaxNode later)
    {
        HashSet<SyntaxNode> above = new(later.Ancestors());
        SyntaxNode? shared = earlier.Ancestors().FirstOrDefault(above.Contains);
        if (shared is null)
        {
            return false;
        }

        SyntaxNode armEarlier = LAuditArmRead(earlier, shared);
        SyntaxNode armLater = LAuditArmRead(later, shared);
        return shared switch
        {
            IfStatementSyntax branch => branch.Else is not null && armEarlier != armLater,
            ConditionalExpressionSyntax => armEarlier != armLater,
            SwitchStatementSyntax => armEarlier != armLater,
            SwitchExpressionSyntax => armEarlier != armLater,
            _ => armEarlier is IfStatementSyntax jump && LAuditJumpCheck(jump)
        };
    }

    private static SyntaxNode LAuditArmRead(SyntaxNode node, SyntaxNode shared)
    {
        SyntaxNode arm = node;
        while (arm.Parent is not null && arm.Parent != shared)
        {
            arm = arm.Parent;
        }

        return arm;
    }

    private static bool LAuditJumpCheck(IfStatementSyntax branch)
    {
        return branch.Else is null && branch.Statement.DescendantNodesAndSelf().Any(node =>
            node is ReturnStatementSyntax or ThrowStatementSyntax or BreakStatementSyntax
                or ContinueStatementSyntax
            && node.Ancestors().TakeWhile(above => above != branch).All(above => above is BlockSyntax));
    }
}

internal static partial class LAuditTruthWalker
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SyntaxNode, HashSet<ISymbol>>
        LAuditCarriedNames = new();

    private static void LAuditMisfiringScan(SyntaxNode root, List<LViolation> violations)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            (string LViolationKind, string LViolationName, string LViolationReason)? hit = node switch
            {
                IfStatementSyntax branch when LAuditControlRead(branch.Condition) is string control
                                              && !(LAuditStrictWalker.LAuditCoreRead(branch.Condition)
                                                       is IsPatternExpressionSyntax { Pattern: var shape }
                                                   && LAuditPairCheck(shape))
                                              && !LAuditHearingCheck(branch)
                                              && LAuditGatekeepingCheck(branch)
                    => ("Misfiring", control, "control decides a request in an if"),
                ConditionalExpressionSyntax choice when LAuditControlRead(choice.Condition) is string control
                                                        && !LAuditPresenceCheck(choice.Condition)
                                                        && !(LAuditStrictWalker.LAuditCoreRead(choice.Condition)
                                                                 is IsPatternExpressionSyntax { Pattern: var shape }
                                                             && LAuditPairCheck(shape))
                                                        && (LAuditRequestCheck(choice.WhenTrue)
                                                            || LAuditRequestCheck(choice.WhenFalse))
                    => ("Misfiring", control, "control decides a request in a ternary"),
                SwitchSectionSyntax { Parent: SwitchStatementSyntax select } section
                    when section.Labels.Select(label => LAuditCaseRead(select.Expression, label))
                             .FirstOrDefault(read => read is not null) is string control
                         && select.Sections.Any(LAuditRequestCheck)
                    => ("Misfiring", control, "control decides a request in a switch case"),
                SwitchExpressionArmSyntax { Parent: SwitchExpressionSyntax select } arm
                    when LAuditCaseRead(select.GoverningExpression, arm) is string control
                         && select.Arms.Any(other => LAuditRequestCheck(other.Expression))
                    => ("Misfiring", control, "control decides a request in a switch arm"),
                IfStatementSyntax branch when LAuditDialogRead(branch.Condition) is string dialog
                                              && LAuditGatekeepingCheck(branch)
                    => ("Gatekeeping", dialog, "a dialog answer decides a request"),
                IfStatementSyntax branch when LAuditAskedCheck(branch.Condition) && LAuditGatekeepingCheck(branch)
                    => ("Gatekeeping", LAuditExcerptRead(branch.Condition),
                        "an engine answer decides a request in an if"),
                ConditionalExpressionSyntax choice when LAuditAskedCheck(choice.Condition)
                                                        && !LAuditPresenceCheck(choice.Condition)
                                                        && (LAuditRequestCheck(choice.WhenTrue)
                                                            || LAuditRequestCheck(choice.WhenFalse))
                    => ("Gatekeeping", LAuditExcerptRead(choice.Condition),
                        "an engine answer decides a request in a ternary"),
                SwitchStatementSyntax select when LAuditAskedCheck(select.Expression) && LAuditRequestCheck(select)
                    => ("Gatekeeping", LAuditExcerptRead(select.Expression),
                        "an engine answer decides a request in a switch"),
                AssignmentExpressionSyntax
                    {
                        RawKind: (int)SyntaxKind.AddAssignmentExpression,
                        Left: MemberAccessExpressionSyntax clock
                    } wired when LAuditClockCheck(clock.Expression) && LAuditDriveCheck(wired.Right)
                    => ("Misfiring", clock.Expression.ToString(), "a clock drives a request"),
                BaseObjectCreationExpressionSyntax { ArgumentList: { } arguments } creation
                    when LAuditClockCheck(creation) && arguments.Arguments.Any(argument =>
                        LAuditDriveCheck(argument.Expression))
                    => ("Misfiring", LAuditExcerptRead(creation), "a clock built with a callback drives a request"),
                StatementSyntax loop when loop is WhileStatementSyntax or DoStatementSyntax or ForStatementSyntax
                                          && LAuditDelayCheck(loop) && LAuditDriveCheck(loop)
                    => ("Misfiring", LAuditExcerptRead(loop), "a delay loop drives a request"),
                MethodDeclarationSyntax handler when LAuditDeafRead(handler) is string bulletin
                    => ("Misfiring", handler.Identifier.ValueText,
                        $"handles the bulletin '{bulletin}' without reading it"),
                LambdaExpressionSyntax deaf when LAuditLambdaCheck(deaf)
                    => ("Misfiring", LAuditTruthSetting.LAuditBulletinType, "handles a bulletin without reading it"),
                _ => null
            };
            if (hit is null)
            {
                continue;
            }

            int line = LAuditLineRead(node);
            if (seen.Add($"{line}:{hit.Value.LViolationKind}:{hit.Value.LViolationName}"))
            {
                violations.Add(new LViolation(
                    root.SyntaxTree.FilePath,
                    line,
                    hit.Value.LViolationName,
                    hit.Value.LViolationKind,
                    hit.Value.LViolationReason));
            }
        }
    }

    private static string LAuditExcerptRead(SyntaxNode node)
    {
        return node.ToString().Split('\n')[0].Trim();
    }

    private static bool LAuditAskedCheck(ExpressionSyntax condition)
    {
        return LAuditAnswerCheck(condition)
               || condition.DescendantNodesAndSelf(node => !LAuditNameofCheck(node)).Any(node =>
                   node is IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax
                   && LAuditBind.LAuditSymbolRead(node) is { } symbol
                   && LAuditReaderNames.Contains(symbol));
    }

    private static string? LAuditControlRead(ExpressionSyntax condition)
    {
        SyntaxNode? scope = condition.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        return LAuditControlRead(condition, scope is null
            ? new HashSet<ISymbol>(SymbolEqualityComparer.Default)
            : LAuditCarriedNames.GetValue(scope, LAuditCarriedRead));
    }

    private static string? LAuditControlRead(ExpressionSyntax condition, HashSet<ISymbol> carried)
    {
        foreach (SyntaxNode node in condition.DescendantNodesAndSelf())
        {
            if (node is MemberAccessExpressionSyntax access
                && LAuditBind.LAuditControlCheck(LAuditBind.LAuditTypeRead(access.Expression))
                && !LAuditBind.LAuditLogicCheck(access))
            {
                return access.Expression.ToString();
            }

            if (node is IdentifierNameSyntax name
                && carried.Count > 0
                && LAuditBind.LAuditSymbolRead(name) is { } symbol
                && carried.Contains(symbol))
            {
                return name.Identifier.ValueText;
            }

            if (node is InvocationExpressionSyntax call && LAuditConsoleCheck(call))
            {
                return call.Expression.ToString();
            }
        }

        return condition.DescendantNodesAndSelf().OfType<IsPatternExpressionSyntax>()
            .FirstOrDefault(test => test.Pattern.DescendantNodesAndSelf().Any(LAuditShapeCheck))
            ?.Expression.ToString();
    }

    private static HashSet<ISymbol> LAuditCarriedRead(SyntaxNode scope)
    {
        HashSet<ISymbol> carried = new(SymbolEqualityComparer.Default);
        foreach (SyntaxNode node in scope.DescendantNodes())
        {
            switch (node)
            {
                case VariableDeclaratorSyntax { Initializer.Value: var value } declarator
                    when LAuditControlRead(value, carried) is not null:
                    LAuditSymbolAdd(declarator, carried);
                    break;
                case AssignmentExpressionSyntax { Left: IdentifierNameSyntax target } assignment
                    when LAuditBind.LAuditSymbolRead(target) is ILocalSymbol or IParameterSymbol
                         && LAuditControlRead(assignment.Right, carried) is not null:
                    LAuditSymbolAdd(target, carried);
                    break;
                case ArgumentSyntax
                    {
                        Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax call } list
                    } argument
                    when LAuditBind.LAuditSymbolRead(call) is IMethodSymbol
                             { MethodKind: MethodKind.LocalFunction } local
                         && LAuditControlRead(argument.Expression, carried) is not null:
                    int index = argument.NameColon is { Name.Identifier.ValueText: var label }
                        ? local.Parameters.FirstOrDefault(parameter => parameter.Name == label)?.Ordinal ?? -1
                        : list.Arguments.IndexOf(argument);
                    if (index >= 0 && index < local.Parameters.Length)
                    {
                        carried.Add(local.Parameters[index]);
                    }

                    break;
            }
        }

        return carried;
    }

    private static bool LAuditShapeCheck(SyntaxNode node)
    {
        return node is RecursivePatternSyntax { Type: { } type, PropertyPatternClause: { } clause }
               && LAuditBind.LAuditControlCheck(LAuditBind.LAuditTypeRead(type))
               && !clause.Subpatterns.All(part => LAuditPairCheck(part.Pattern));
    }

    private static bool LAuditHearingCheck(IfStatementSyntax branch)
    {
        if (branch.Else is not null)
        {
            return false;
        }

        if (branch.Statement is ReturnStatementSyntax { Expression: null }
            or BlockSyntax { Statements: [ReturnStatementSyntax { Expression: null }] })
        {
            ExpressionSyntax condition = branch.Condition;
            while (condition is ParenthesizedExpressionSyntax wrapped)
            {
                condition = wrapped.Expression;
            }

            return condition is PrefixUnaryExpressionSyntax
                       { RawKind: (int)SyntaxKind.LogicalNotExpression } negated
                   && LAuditFocusCheck(negated.Operand);
        }

        MemberDeclarationSyntax? member = branch.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        bool later = member is not null && member.DescendantNodes()
            .Where(node => node.SpanStart >= branch.Span.End)
            .Any(node => node is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax
                         && LAuditCallRead((ExpressionSyntax)node) is not null);
        return !later && LAuditFocusCheck(branch.Condition);
    }

    private static bool LAuditFocusCheck(ExpressionSyntax condition)
    {
        ExpressionSyntax core = condition;
        while (core is ParenthesizedExpressionSyntax wrapped)
        {
            core = wrapped.Expression;
        }

        return core switch
        {
            BinaryExpressionSyntax chain when chain.IsKind(SyntaxKind.LogicalAndExpression)
                => LAuditFocusCheck(chain.Left) && LAuditFocusCheck(chain.Right),
            _ when LAuditControlRead(core) is null => true,
            MemberAccessExpressionSyntax access
                => LAuditTruthSetting.LAuditFocusMembers.Contains(
                       access.Name.Identifier.ValueText, StringComparer.Ordinal)
                   && LAuditControlRead(access.Expression) is null,
            IsPatternExpressionSyntax
                {
                    Pattern: RecursivePatternSyntax
                    {
                        Type: { } type, PositionalPatternClause: null, PropertyPatternClause: { } clause
                    }
                } test
                => LAuditControlRead(test.Expression) is null
                   && LAuditBind.LAuditControlCheck(LAuditBind.LAuditTypeRead(type))
                   && clause.Subpatterns.All(part =>
                       LAuditPairCheck(part.Pattern)
                       || (part.NameColon is { Name.Identifier.ValueText: var name }
                           && LAuditTruthSetting.LAuditFocusMembers.Contains(name, StringComparer.Ordinal)
                           && part.Pattern is ConstantPatternSyntax
                           {
                               Expression.RawKind: (int)SyntaxKind.TrueLiteralExpression
                           })),
            _ => false
        };
    }

    private static string? LAuditCaseRead(ExpressionSyntax governing, SyntaxNode label)
    {
        bool decided = label switch
        {
            CaseSwitchLabelSyntax constant => !LAuditStrictWalker.LAuditNullCheck(constant.Value),
            CasePatternSwitchLabelSyntax shape => !LAuditPairCheck(shape.Pattern),
            SwitchExpressionArmSyntax arm => arm.Pattern is not DiscardPatternSyntax && !LAuditPairCheck(arm.Pattern),
            _ => false
        };
        WhenClauseSyntax? guard = label switch
        {
            CasePatternSwitchLabelSyntax shape => shape.WhenClause,
            SwitchExpressionArmSyntax arm => arm.WhenClause,
            _ => null
        };
        if (guard is not null
            && LAuditControlRead(guard.Condition) is string control
            && !LAuditPresenceCheck(guard.Condition)
            && !(LAuditStrictWalker.LAuditCoreRead(guard.Condition) is IsPatternExpressionSyntax { Pattern: var test }
                 && LAuditPairCheck(test)))
        {
            return control;
        }

        PatternSyntax? pattern = label switch
        {
            CasePatternSwitchLabelSyntax shape => shape.Pattern,
            SwitchExpressionArmSyntax arm => arm.Pattern,
            _ => null
        };
        return !decided
            ? null
            : LAuditControlRead(governing)
              ?? (pattern?.DescendantNodesAndSelf().Any(LAuditShapeCheck) == true ? governing.ToString() : null);
    }

    private static bool LAuditPairCheck(PatternSyntax pattern)
    {
        return LAuditStrictWalker.LAuditPatternCheck(pattern) || pattern switch
        {
            ConstantPatternSyntax { Expression: var named } => LAuditBind.LAuditSymbolRead(named) is ITypeSymbol,
            ParenthesizedPatternSyntax { Pattern: var inner } => LAuditPairCheck(inner),
            UnaryPatternSyntax { Pattern: var negated } => LAuditPairCheck(negated),
            BinaryPatternSyntax { Left: var left, Right: var right }
                => LAuditPairCheck(left) && LAuditPairCheck(right),
            RecursivePatternSyntax { PositionalPatternClause: null, PropertyPatternClause: { } clause }
                => clause.Subpatterns.All(part => LAuditPairCheck(part.Pattern)),
            _ => false
        };
    }

    private static bool LAuditConsoleCheck(InvocationExpressionSyntax call)
    {
        ISymbol? callee = LAuditBind.LAuditSymbolRead(call);
        return LAuditBind.LAuditMemberCheck(callee, LAuditTruthSetting.LAuditConsoleInput);
    }

    private static string? LAuditDialogRead(ExpressionSyntax condition)
    {
        return condition.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(call => LAuditBind.LAuditSymbolRead(call)?.ContainingType is { } owner
                                    && LAuditTruthSetting.LAuditDialogTypes.Contains(
                                        owner.ToDisplayString(), StringComparer.Ordinal))
            ?.Expression.ToString();
    }

    private static bool LAuditClockCheck(SyntaxNode clock)
    {
        ITypeSymbol? type = clock is BaseObjectCreationExpressionSyntax creation
            ? LAuditBind.LAuditTypeRead(creation)
            : LAuditBind.LAuditTypeRead(clock);
        return LAuditBind.LAuditNamedCheck(type, LAuditTruthSetting.LAuditClockTypes);
    }

    private static bool LAuditDelayCheck(SyntaxNode loop)
    {
        return loop.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(call =>
            LAuditBind.LAuditMemberCheck(LAuditBind.LAuditSymbolRead(call), LAuditTruthSetting.LAuditDelayMembers)
            || (call.Expression is MemberAccessExpressionSyntax access && LAuditClockCheck(access.Expression)));
    }

    private static bool LAuditLambdaCheck(LambdaExpressionSyntax lambda)
    {
        ParameterSyntax? parameter = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter,
            ParenthesizedLambdaExpressionSyntax full => full.ParameterList.Parameters.FirstOrDefault(),
            _ => null
        };
        if (parameter is null
            || LAuditBind.LAuditSymbolRead(parameter) is not IParameterSymbol symbol
            || symbol.Type.Name != LAuditTruthSetting.LAuditBulletinType)
        {
            return false;
        }

        HashSet<ISymbol> symbols = new([symbol], SymbolEqualityComparer.Default);
        return parameter.Identifier.ValueText == "_" || !LAuditNameCheck(lambda.Body, symbols);
    }

    private static bool LAuditDriveCheck(SyntaxNode handler)
    {
        return LAuditRequestCheck(handler)
               || handler.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                   .Any(name =>
                       LAuditBind.LAuditSymbolRead(name) is { } symbol && LAuditRelayNames.Contains(symbol));
    }

    private static string? LAuditDeafRead(MethodDeclarationSyntax handler)
    {
        foreach (ParameterSyntax parameter in handler.ParameterList.Parameters)
        {
            if (parameter.Type is null
                || LAuditBind.LAuditTypeRead(parameter.Type)?.Name != LAuditTruthSetting.LAuditBulletinType
                || LAuditBind.LAuditSymbolRead(parameter) is not { } symbol)
            {
                continue;
            }

            SyntaxNode? body = (SyntaxNode?)handler.Body ?? handler.ExpressionBody;
            HashSet<ISymbol> symbols = new([symbol], SymbolEqualityComparer.Default);
            if (body is not null && !LAuditNameCheck(body, symbols))
            {
                return parameter.Identifier.ValueText;
            }
        }

        return null;
    }
}

internal static partial class LAuditTruthWalker
{
    private static (string LViolationKind, string LViolationReason)? LAuditSinkRead(SyntaxNode reference)
    {
        SyntaxNode receiver = reference;
        while (receiver.Parent is ParenthesizedExpressionSyntax
               or PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression })
        {
            receiver = receiver.Parent;
        }

        if (receiver.Parent is MemberAccessExpressionSyntax { Expression: var owner } && owner == receiver)
        {
            return null;
        }

        if (reference.Parent is ConditionalAccessExpressionSyntax { Expression: var target } access
            && target == reference)
        {
            return LAuditRequestCheck(access.WhenNotNull) ? ("Gatekeeping", "decides a request through ?.") : null;
        }

        foreach (SyntaxNode ancestor in reference.Ancestors())
        {
            switch (ancestor)
            {
                case MemberDeclarationSyntax:
                    return null;
                case ArgumentSyntax { Parent.Parent: ExpressionSyntax call } argument
                    when LAuditCallRead(call) is { } callee && LAuditHotCheck(callee, argument):
                    return ("Replaying", $"passed to {callee.Name}");
                case InitializerExpressionSyntax { Parent: WithExpressionSyntax }:
                    return ("Replaying", "written into a record copy");
                case InitializerExpressionSyntax { Parent: BaseObjectCreationExpressionSyntax creation }
                    when LAuditCallRead(creation) is { } built:
                    return ("Replaying", $"written into new {built.ContainingType?.Name ?? built.Name}");
                case IfStatementSyntax branch
                    when branch.Condition.Span.Contains(reference.Span) && LAuditGatekeepingCheck(branch):
                    return ("Gatekeeping", "decides a request in an if");
                case ConditionalExpressionSyntax choice
                    when choice.Condition.Span.Contains(reference.Span)
                         && !LAuditPresenceCheck(choice.Condition)
                         && (LAuditRequestCheck(choice.WhenTrue) || LAuditRequestCheck(choice.WhenFalse)):
                    return ("Gatekeeping", "decides a request in a ternary");
                case SwitchStatementSyntax select
                    when select.Expression.Span.Contains(reference.Span) && LAuditRequestCheck(select):
                    return ("Gatekeeping", "decides a request in a switch");
                case SwitchExpressionSyntax arms
                    when arms.GoverningExpression.Span.Contains(reference.Span) && LAuditRequestCheck(arms):
                    return ("Gatekeeping", "decides a request in a switch expression");
                case WhileStatementSyntax loop
                    when loop.Condition.Span.Contains(reference.Span) && LAuditRequestCheck(loop.Statement):
                    return ("Gatekeeping", "decides a request in a while");
                case DoStatementSyntax loop
                    when loop.Condition.Span.Contains(reference.Span) && LAuditRequestCheck(loop.Statement):
                    return ("Gatekeeping", "decides a request in a do");
                case ForStatementSyntax { Condition: { } condition } loop
                    when condition.Span.Contains(reference.Span) && LAuditRequestCheck(loop.Statement):
                    return ("Gatekeeping", "decides a request in a for");
                case WhenClauseSyntax { Parent: { } label } clause
                    when LAuditRequestCheck(label.Parent is SwitchSectionSyntax section ? section : label)
                         && clause.Condition.Span.Contains(reference.Span):
                    return ("Gatekeeping", "decides a request in a when clause");
                case CatchFilterClauseSyntax filter
                    when filter.Parent is CatchClauseSyntax { Block: var handler } && LAuditRequestCheck(handler):
                    return ("Gatekeeping", "decides a request in a catch filter");
                case BinaryExpressionSyntax gate
                    when (gate.IsKind(SyntaxKind.LogicalAndExpression)
                          || gate.IsKind(SyntaxKind.LogicalOrExpression)
                          || gate.IsKind(SyntaxKind.CoalesceExpression))
                         && gate.Left.Span.Contains(reference.Span)
                         && LAuditRequestCheck(gate.Right):
                    return ("Gatekeeping", $"decides a request through {gate.OperatorToken.ValueText}");
            }
        }

        return null;
    }

    private static bool LAuditGatekeepingCheck(IfStatementSyntax branch)
    {
        if (LAuditPresenceCheck(branch.Condition))
        {
            return false;
        }

        if (LAuditRequestCheck(branch.Statement)
            || (branch.Else is not null && LAuditRequestCheck(branch.Else)))
        {
            return true;
        }

        MemberDeclarationSyntax? member = branch.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        return branch.Statement.DescendantNodesAndSelf()
            .Select(node => node switch
            {
                ReturnStatementSyntax or ThrowStatementSyntax => member,
                ContinueStatementSyntax => node.Ancestors().FirstOrDefault(loop =>
                    loop is CommonForEachStatementSyntax or ForStatementSyntax or WhileStatementSyntax
                        or DoStatementSyntax),
                BreakStatementSyntax => node.Ancestors().FirstOrDefault(loop =>
                    loop is CommonForEachStatementSyntax or ForStatementSyntax or WhileStatementSyntax
                        or DoStatementSyntax or SwitchSectionSyntax),
                _ => null
            })
            .OfType<SyntaxNode>()
            .Distinct()
            .Any(scope => scope.DescendantNodes()
                .Where(node => node.SpanStart >= branch.SpanStart)
                .Any(node => node is ExpressionSyntax call
                             && (call is InvocationExpressionSyntax || call is BaseObjectCreationExpressionSyntax)
                             && LAuditCallRead(call) is not null));
    }

    private static bool LAuditPresenceCheck(ExpressionSyntax condition)
    {
        ExpressionSyntax core = LAuditStrictWalker.LAuditCoreRead(condition);
        return core switch
        {
            BinaryExpressionSyntax chain
                when chain.IsKind(SyntaxKind.LogicalAndExpression) || chain.IsKind(SyntaxKind.LogicalOrExpression)
                => LAuditPresenceCheck(chain.Left) && LAuditPresenceCheck(chain.Right),
            IsPatternExpressionSyntax { Pattern: var pattern } => LAuditStrictWalker.LAuditPatternCheck(pattern),
            BinaryExpressionSyntax binary
                when binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression)
                => LAuditStrictWalker.LAuditNullCheck(binary.Left) || LAuditStrictWalker.LAuditNullCheck(binary.Right),
            _ => false
        };
    }

    private static bool LAuditRequestCheck(SyntaxNode node)
    {
        return node.DescendantNodesAndSelf().Any(child =>
            child is ExpressionSyntax call
            && (call is InvocationExpressionSyntax || call is BaseObjectCreationExpressionSyntax)
            && LAuditCallRead(call) is not null);
    }

    private static bool LAuditHotCheck(ISymbol callee, ArgumentSyntax argument)
    {
        if (LAuditBind.LAuditLogicCheck(callee))
        {
            return true;
        }

        if (argument.Parent is not BaseArgumentListSyntax list)
        {
            return false;
        }

        int index = argument.NameColon is { Name.Identifier.ValueText: var name }
            ? (callee as IMethodSymbol)?.Parameters.FirstOrDefault(parameter => parameter.Name == name)?.Ordinal ?? -1
            : list.Arguments.IndexOf(argument);
        return LAuditHotNames.TryGetValue(callee, out HashSet<int>? hot) && hot.Contains(index);
    }

    private static bool LAuditZeroingCheck(ArgumentSyntax argument, out ISymbol? callee)
    {
        callee = argument.Parent is ArgumentListSyntax { Parent: ExpressionSyntax call }
                 && call is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax
            ? LAuditCallRead(call) ?? LAuditBind.LAuditSymbolRead(call)
            : null;
        if (callee is null || argument.Parent is not ArgumentListSyntax list)
        {
            return false;
        }

        if (LAuditBind.LAuditLogicCheck(callee))
        {
            return true;
        }

        int index = argument.NameColon is { Name.Identifier.ValueText: var name }
            ? (callee as IMethodSymbol)?.Parameters.FirstOrDefault(parameter => parameter.Name == name)?.Ordinal ?? -1
            : list.Arguments.IndexOf(argument);
        return LAuditZeroingNames.TryGetValue(callee, out HashSet<int>? hot) && hot.Contains(index);
    }

    private static void LAuditZeroingScan(SyntaxNode root, List<LViolation> violations)
    {
        foreach (ExpressionSyntax literal in root.DescendantNodes().OfType<ExpressionSyntax>().Where(LAuditZeroCheck))
        {
            ExpressionSyntax position = literal;
            while (position.Parent is ParenthesizedExpressionSyntax or CastExpressionSyntax
                   || (position.Parent is ConditionalExpressionSyntax choice && choice.Condition != position)
                   || (position.Parent is BinaryExpressionSyntax
                       {
                           RawKind: (int)SyntaxKind.CoalesceExpression
                       } fallback
                       && fallback.Right == position))
            {
                position = (ExpressionSyntax)position.Parent;
            }

            (string LViolationName, string LViolationReason)? hit = position.Parent switch
            {
                ArgumentSyntax argument when LAuditZeroingCheck(argument, out ISymbol? callee)
                    => callee is IMethodSymbol { MethodKind: MethodKind.Constructor, ContainingType: { } built }
                        ? (built.Name, $"passes {literal} to new {built.Name}")
                        : (callee!.Name, $"passes {literal} to {callee.Name}"),
                BinaryExpressionSyntax compare
                    when compare.Kind() is SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression
                             or SyntaxKind.LessThanExpression or SyntaxKind.GreaterThanExpression
                             or SyntaxKind.LessThanOrEqualExpression or SyntaxKind.GreaterThanOrEqualExpression
                         && LAuditComparedRead(compare.Left == position ? compare.Right : compare.Left) is { } member
                    => (member.Name, $"compares {member.Name} with {literal}"),
                ConstantPatternSyntax pattern
                    when (pattern.Parent is UnaryPatternSyntax negated ? negated.Parent : pattern.Parent)
                             is IsPatternExpressionSyntax test
                         && LAuditComparedRead(test.Expression) is { } member
                    => (member.Name, $"compares {member.Name} with {literal}"),
                _ => null
            };
            if (hit is not null)
            {
                violations.Add(new LViolation(
                    root.SyntaxTree.FilePath,
                    LAuditLineRead(literal),
                    hit.Value.LViolationName,
                    "Zeroing",
                    hit.Value.LViolationReason));
            }
        }
    }

    private static bool LAuditZeroCheck(ExpressionSyntax node)
    {
        return node switch
        {
            LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression, Token.Value: 0 or 0L } => true,
            LiteralExpressionSyntax { RawKind: (int)SyntaxKind.DefaultLiteralExpression } => true,
            DefaultExpressionSyntax => true,
            _ => false
        };
    }

    private static ISymbol? LAuditComparedRead(ExpressionSyntax side)
    {
        ExpressionSyntax core = side;
        while (core is ParenthesizedExpressionSyntax or CastExpressionSyntax or ConditionalAccessExpressionSyntax
               or PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression })
        {
            core = core switch
            {
                ParenthesizedExpressionSyntax inner => inner.Expression,
                CastExpressionSyntax cast => cast.Expression,
                ConditionalAccessExpressionSyntax access => access.WhenNotNull,
                PostfixUnaryExpressionSyntax suppress => suppress.Operand,
                _ => core
            };
        }

        return LAuditBind.LAuditSymbolRead(core) is { Kind: SymbolKind.Field or SymbolKind.Property } member
               && (LAuditZeroingReads.Contains(member) || LAuditBind.LAuditLogicCheck(member))
            ? member
            : null;
    }

    internal static ISymbol? LAuditCallRead(ExpressionSyntax call)
    {
        if (call is not (InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax)
            || LAuditBind.LAuditSymbolRead(call) is not { } callee)
        {
            return null;
        }

        if (LAuditBind.LAuditLogicCheck(callee) || LAuditRelayNames.Contains(callee))
        {
            return callee;
        }

        return LAuditDelegateRead(call) is { } held && (LAuditRelayNames.Contains(held) || LAuditInterfaceCheck(held))
            ? held
            : null;
    }

    private static bool LAuditInterfaceCheck(ISymbol held)
    {
        return held is IEventSymbol { ContainingType: { } owner } happening
               && owner.AllInterfaces
                   .SelectMany(face => face.GetMembers(happening.Name).OfType<IEventSymbol>())
                   .Any(member => LAuditRelayNames.Contains(member.OriginalDefinition)
                                  && SymbolEqualityComparer.Default.Equals(
                                      owner.FindImplementationForInterfaceMember(member)?.OriginalDefinition,
                                      happening));
    }

    private static ISymbol? LAuditDelegateRead(ExpressionSyntax call)
    {
        if (call is not InvocationExpressionSyntax invocation)
        {
            return null;
        }

        ExpressionSyntax? target = invocation.Expression switch
        {
            MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Invoke" } access => access.Expression,
            MemberBindingExpressionSyntax { Name.Identifier.ValueText: "Invoke" }
                => invocation.FirstAncestorOrSelf<ConditionalAccessExpressionSyntax>()?.Expression,
            IdentifierNameSyntax or MemberAccessExpressionSyntax => invocation.Expression,
            _ => null
        };
        return target is null ? null : LAuditBind.LAuditSymbolRead(target);
    }

    internal static bool LAuditNameofCheck(SyntaxNode node)
    {
        return node is InvocationExpressionSyntax
               {
                   Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" }
               } named
               && LAuditBind.LAuditSymbolRead(named) is null;
    }
}

internal static class LAuditOriginWalker
{
    private static readonly Dictionary<ISymbol, bool> LAuditOriginNames = new(SymbolEqualityComparer.Default);

    private static Dictionary<string, List<SyntaxNode>> LAuditSiteNames = new(StringComparer.Ordinal);

    private static Compilation? LAuditOriginCompilation;

    internal static string LAuditWriterResolve(ExpressionSyntax? value)
    {
        if (!ReferenceEquals(LAuditOriginCompilation, LAuditBind.LAuditCompilation))
        {
            LAuditOriginCompilation = LAuditBind.LAuditCompilation;
            LAuditOriginNames.Clear();
            IReadOnlyList<string> shell = LAuditBind.LAuditRootRead(LAuditTruthSetting.LAuditShellInclude);
            LAuditSiteNames = LAuditBind.LAuditTrees
                .Where(tree => shell.Any(folder => LAuditBind.LAuditRelativeRead(tree.FilePath)
                    .StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase)))
                .SelectMany(tree => tree.GetRoot().DescendantNodes()
                    .Where(node => node is IdentifierNameSyntax or ImplicitObjectCreationExpressionSyntax
                    or ConstructorInitializerSyntax))
                .GroupBy(node => node is IdentifierNameSyntax name ? name.Identifier.ValueText : "new",
                    StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        }

        return LAuditWriterResolve(value, new HashSet<ISymbol>(SymbolEqualityComparer.Default));
    }

    private static string LAuditWriterResolve(ExpressionSyntax? value, HashSet<ISymbol> visiting)
    {
        if (value is null)
        {
            return "plain";
        }

        if (LAuditBlankCheck(value))
        {
            return "clear";
        }

        bool asked = value.DescendantNodesAndSelf(node => !LAuditTruthWalker.LAuditNameofCheck(node)
                                                          && !LAuditCapsuleCheck(node)).Any(node =>
            node switch
            {
                MemberAccessExpressionSyntax or MemberBindingExpressionSyntax
                    => LAuditBind.LAuditLogicCheck(LAuditBind.LAuditSymbolRead(node)),
                InvocationExpressionSyntax call
                    => LAuditTruthWalker.LAuditCallRead(call) is not null
                       || (LAuditBind.LAuditSymbolRead(call) is { } callee
                           && LAuditTruthWalker.LAuditReaderNames.Contains(callee)),
                BaseObjectCreationExpressionSyntax creation => LAuditTruthWalker.LAuditCallRead(creation) is not null,
                IdentifierNameSyntax name => LAuditBind.LAuditSymbolRead(name) switch
                {
                    ILocalSymbol or IParameterSymbol when LAuditBind.LAuditLogicCheck(name) => true,
                    { } held => LAuditOriginCheck(held, visiting),
                    _ => false
                },
                _ => false
            });
        return asked ? "engine" : "plain";
    }

    private static bool LAuditOriginCheck(ISymbol held, HashSet<ISymbol> visiting)
    {
        bool top = visiting.Count == 0;
        if (top && LAuditOriginNames.TryGetValue(held, out bool known))
        {
            return known;
        }

        if (!visiting.Add(held))
        {
            return false;
        }

        bool engine = false;
        foreach (ExpressionSyntax? write in LAuditOriginRead(held) ?? [])
        {
            if (write is IdentifierNameSyntax or MemberAccessExpressionSyntax
                && LAuditBind.LAuditSymbolRead(write) is { } copied && visiting.Contains(copied))
            {
                continue;
            }

            string verdict = LAuditWriterResolve(write, visiting);
            if (verdict == "clear")
            {
                continue;
            }

            engine = verdict == "engine";
            if (!engine)
            {
                break;
            }
        }

        visiting.Remove(held);
        if (top)
        {
            LAuditOriginNames[held] = engine;
        }

        return engine;
    }

    internal static List<ExpressionSyntax?>? LAuditOriginRead(ISymbol held)
    {
        ISymbol owner = held is IParameterSymbol
        {
            ContainingSymbol: IMethodSymbol
            {
                MethodKind: MethodKind.PropertySet, AssociatedSymbol: IPropertySymbol property
            }
        }
            ? property
            : held;
        if (owner is not (IFieldSymbol or IPropertySymbol { IsIndexer: false } or ILocalSymbol
                or IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Constructor } }
                or IParameterSymbol
                {
                    RefKind: RefKind.None, ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Ordinary }
                })
            || owner.DeclaringSyntaxReferences.IsEmpty
            || !LAuditBind.LAuditShellCheck(owner.ContainingType))
        {
            return null;
        }

        if (owner is IFieldSymbol or IPropertySymbol
            && owner.DeclaredAccessibility != Accessibility.Private
            && owner.ContainingType.AllInterfaces.Any(face =>
                face.ToDisplayString() == "System.Windows.Markup.IComponentConnector"))
        {
            return [null];
        }

        SyntaxNode declaration = owner.DeclaringSyntaxReferences[0].GetSyntax();
        List<ExpressionSyntax?> writes = [];
        if (ReferenceEquals(owner, held))
        {
            switch (owner)
            {
                case IFieldSymbol { Type.Name: "DependencyProperty" or "DependencyPropertyKey" }:
                    return LAuditPropertyRead(owner, declaration);
                case IPropertySymbol when declaration is PropertyDeclarationSyntax body
                                          && LAuditGetterRead(body) is { } getter:
                    return getter;
                case IPropertySymbol positional when declaration is ParameterSyntax:
                    writes.AddRange(LAuditArgumentRead(
                        positional.ContainingType.InstanceConstructors.FirstOrDefault(constructor =>
                            constructor.DeclaringSyntaxReferences.Any(source =>
                                source.GetSyntax() is TypeDeclarationSyntax)),
                        positional.Name));
                    break;
                case IParameterSymbol { ContainingSymbol: IMethodSymbol method } parameter:
                    writes.AddRange(LAuditArgumentRead(method, parameter.Name));
                    break;
                case ILocalSymbol:
                    writes.AddRange(LAuditLocalRead(declaration));
                    break;
                default:
                    if (declaration is VariableDeclaratorSyntax { Initializer.Value: { } start })
                    {
                        writes.Add(start);
                    }
                    else if (declaration is PropertyDeclarationSyntax { Initializer.Value: { } first })
                    {
                        writes.Add(first);
                    }

                    break;
            }
        }

        foreach (IdentifierNameSyntax identifier in LAuditSiteRead(owner))
        {
            SyntaxNode reference = identifier.Parent is MemberAccessExpressionSyntax access && access.Name == identifier
                ? access
                : identifier;
            if (LAuditTruthWalker.LAuditWriteCheck(reference, out ExpressionSyntax? value)
                && (value is not null || reference.Parent is not MemberAccessExpressionSyntax))
            {
                writes.AddRange(LAuditHelperRead(identifier, reference) ?? [value]);
            }
        }

        return writes;
    }

    internal static List<ExpressionSyntax?>? LAuditHelperRead(IdentifierNameSyntax identifier, SyntaxNode reference)
    {
        if (LAuditBind.LAuditSymbolRead(identifier) is IParameterSymbol
            {
                RefKind: RefKind.Ref, ContainingSymbol: IMethodSymbol inner
            } held
            && reference.Parent is AssignmentExpressionSyntax
            && LAuditHelperScan(inner, held) is not null)
        {
            return [];
        }

        if (reference.Parent is not ArgumentSyntax
            {
                Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax call }
            } argument
            || !argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword)
            || LAuditBind.LAuditModelRead(call).GetOperation(call) is not IInvocationOperation invoked)
        {
            return null;
        }

        IParameterSymbol? target = invoked.Arguments.FirstOrDefault(given => given.Syntax == argument)?.Parameter;
        List<IParameterSymbol>? sources = target is null
            ? null
            : LAuditHelperScan(invoked.TargetMethod.OriginalDefinition, target.OriginalDefinition);
        return sources?.Select(source => (invoked.Arguments.FirstOrDefault(given =>
                    SymbolEqualityComparer.Default.Equals(given.Parameter?.OriginalDefinition, source))
                ?.Syntax as ArgumentSyntax)?.Expression)
            .ToList() ?? [null];
    }

    private static List<IParameterSymbol>? LAuditHelperScan(IMethodSymbol method, IParameterSymbol held)
    {
        List<IParameterSymbol> sources = [];
        foreach (IdentifierNameSyntax use in method.DeclaringSyntaxReferences
                     .SelectMany(source => source.GetSyntax().DescendantNodes().OfType<IdentifierNameSyntax>()))
        {
            if (!SymbolEqualityComparer.Default.Equals(LAuditBind.LAuditSymbolRead(use), held)
                || !LAuditTruthWalker.LAuditWriteCheck(use, out _))
            {
                continue;
            }

            if (use.Parent is not AssignmentExpressionSyntax
                {
                    RawKind: (int)SyntaxKind.SimpleAssignmentExpression
                } set
                || set.Left != use
                || LAuditBind.LAuditSymbolRead(set.Right) is not IParameterSymbol { RefKind: RefKind.None } given
                || !SymbolEqualityComparer.Default.Equals(given.ContainingSymbol, method))
            {
                return null;
            }

            sources.Add(given);
        }

        return sources.Count > 0 ? sources : null;
    }

    private static IEnumerable<IdentifierNameSyntax> LAuditSiteRead(ISymbol held)
    {
        return (LAuditSiteNames.GetValueOrDefault(held.Name) ?? [])
            .OfType<IdentifierNameSyntax>()
            .Where(identifier => !identifier.Ancestors().Any(LAuditTruthWalker.LAuditNameofCheck)
                                 && SymbolEqualityComparer.Default.Equals(
                                     LAuditBind.LAuditSymbolRead(identifier), held));
    }

    private static List<ExpressionSyntax?> LAuditPropertyRead(ISymbol held, SyntaxNode declaration)
    {
        List<ExpressionSyntax?> writes = [];
        foreach (BaseObjectCreationExpressionSyntax creation in declaration.DescendantNodes()
                     .OfType<BaseObjectCreationExpressionSyntax>())
        {
            if (LAuditBind.LAuditModelRead(creation).GetOperation(creation) is IObjectCreationOperation built
                && built.Arguments.FirstOrDefault(argument => argument.Parameter?.Name == "defaultValue")
                    ?.Value.Syntax is ExpressionSyntax fallback
                && !LAuditNeutralCheck(fallback))
            {
                writes.Add(fallback);
            }
        }

        foreach (IdentifierNameSyntax identifier in LAuditSiteRead(held))
        {
            if (identifier.Parent is not ArgumentSyntax
                {
                    Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax call } list
                } argument)
            {
                continue;
            }

            switch ((LAuditBind.LAuditSymbolRead(call) as IMethodSymbol)?.Name)
            {
                case "SetValue" or "SetCurrentValue" when list.Arguments.Count == 2 && list.Arguments[0] == argument:
                    writes.Add(list.Arguments[1].Expression);
                    break;
                case "GetValue" or "ReadLocalValue" or "ClearValue" or "GetValueSource" or "InvalidateProperty"
                    or "CoerceValue":
                    break;
                default:
                    writes.Add(null);
                    break;
            }
        }

        return writes;
    }

    private static List<ExpressionSyntax?>? LAuditGetterRead(PropertyDeclarationSyntax property)
    {
        if (property.ExpressionBody is { } body)
        {
            return [body.Expression];
        }

        AccessorDeclarationSyntax? getter = property.AccessorList?.Accessors
            .FirstOrDefault(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
        if (getter?.ExpressionBody is { } arrow)
        {
            return [arrow.Expression];
        }

        return getter?.Body?.DescendantNodes(node => node is not (AnonymousFunctionExpressionSyntax
                or LocalFunctionStatementSyntax))
            .OfType<ReturnStatementSyntax>()
            .Select(exit => exit.Expression)
            .ToList();
    }

    private static List<ExpressionSyntax?> LAuditArgumentRead(IMethodSymbol? method, string name)
    {
        List<ExpressionSyntax?> writes = [];
        List<SyntaxNode?> sites;
        if (method is { MethodKind: MethodKind.Ordinary })
        {
            sites = (LAuditSiteNames.GetValueOrDefault(method.Name) ?? [])
                .OfType<IdentifierNameSyntax>()
                .Where(identifier => !identifier.Ancestors().Any(LAuditTruthWalker.LAuditNameofCheck)
                                     && LAuditBind.LAuditSymbolRead(identifier) is IMethodSymbol called
                                     && SymbolEqualityComparer.Default.Equals(
                                         (called.ReducedFrom ?? called).OriginalDefinition, method))
                .Select(identifier => identifier.Parent is MemberBindingExpressionSyntax binding
                    ? binding
                    : identifier.Parent is MemberAccessExpressionSyntax access && access.Name == identifier
                        ? access
                        : (ExpressionSyntax)identifier)
                .Select(callee => callee.Parent is InvocationExpressionSyntax call && call.Expression == callee
                    ? call
                    : null)
                .ToList<SyntaxNode?>();
            if (sites.Count == 0 || sites.Contains(null) || method.IsOverride || method.IsVirtual
                || method.IsAbstract || method.IsGenericMethod
                || method.ContainingType.AllInterfaces.SelectMany(face => face.GetMembers()).Any(member =>
                    SymbolEqualityComparer.Default.Equals(
                        method.ContainingType.FindImplementationForInterfaceMember(member), method)))
            {
                return [null];
            }
        }
        else
        {
            sites = (LAuditSiteNames.GetValueOrDefault(method?.ContainingType.Name ?? "") ?? [])
                .Select(node => node.Parent is QualifiedNameSyntax qualified ? qualified.Parent : node.Parent)
                .Concat(LAuditSiteNames.GetValueOrDefault("new") ?? [])
                .Where(site => site is not null
                               && SymbolEqualityComparer.Default.Equals(LAuditBind.LAuditSymbolRead(site), method))
                .ToList();
        }

        foreach (SyntaxNode site in sites.OfType<SyntaxNode>())
        {
            ImmutableArray<IArgumentOperation>? built = LAuditBind.LAuditModelRead(site).GetOperation(site) switch
            {
                IObjectCreationOperation creation => creation.Arguments,
                IInvocationOperation chained => chained.Arguments,
                _ => null
            };
            if (built is null)
            {
                writes.Add(null);
                continue;
            }

            foreach (IArgumentOperation argument in built.Value.Where(argument => argument.Parameter?.Name == name))
            {
                ExpressionSyntax? given = argument.ArgumentKind == ArgumentKind.DefaultValue
                    ? (argument.Parameter?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as ParameterSyntax)
                    ?.Default?.Value
                    : argument.Value.Syntax as ExpressionSyntax;
                if (given is null || !LAuditNeutralCheck(given))
                {
                    writes.Add(given);
                }
            }
        }

        return writes;
    }

    private static List<ExpressionSyntax?> LAuditLocalRead(SyntaxNode declaration)
    {
        if (declaration is VariableDeclaratorSyntax variable)
        {
            return variable.Initializer is { } start ? [start.Value] : [];
        }

        if (declaration is ForEachStatementSyntax loop)
        {
            return [loop.Expression];
        }

        if (declaration is not SingleVariableDesignationSyntax || declaration.Parent is DeclarationExpressionSyntax)
        {
            return [null];
        }

        return
        [
            declaration.Ancestors().Select(node => node switch
            {
                IsPatternExpressionSyntax test => test.Expression,
                SwitchStatementSyntax choice => choice.Expression,
                SwitchExpressionSyntax arms => arms.GoverningExpression,
                ForEachVariableStatementSyntax pair => pair.Expression,
                _ => null
            }).FirstOrDefault(tested => tested is not null)
        ];
    }

    private static bool LAuditNeutralCheck(ExpressionSyntax value)
    {
        if (LAuditBlankCheck(value))
        {
            return true;
        }

        Optional<object?> constant = LAuditBind.LAuditModelRead(value).GetConstantValue(value);
        return constant.HasValue && constant.Value switch
        {
            false => true,
            bool or string or char => false,
            IConvertible number => number.ToDouble(System.Globalization.CultureInfo.InvariantCulture) == 0,
            _ => false
        };
    }

    private static bool LAuditBlankCheck(ExpressionSyntax value)
    {
        if (value.IsKind(SyntaxKind.NullLiteralExpression)
            || value.IsKind(SyntaxKind.DefaultLiteralExpression)
            || value is DefaultExpressionSyntax
            || LAuditBind.LAuditSymbolRead(value) is IFieldSymbol
            {
                Name: "Empty", ContainingType.SpecialType: SpecialType.System_String
            })
        {
            return true;
        }

        Optional<object?> constant = LAuditBind.LAuditModelRead(value).GetConstantValue(value);
        return constant.HasValue && constant.Value is null or "";
    }

    private static bool LAuditCapsuleCheck(SyntaxNode node)
    {
        string? source = node is InvocationExpressionSyntax
                         && LAuditBind.LAuditSymbolRead(node) is IMethodSymbol { ContainingType: { } owner }
            ? LAuditBind.LAuditSourceRead(owner.OriginalDefinition)
            : null;
        return source is not null && LAuditBind.LAuditRootRead(LAuditTruthSetting.LAuditCapsuleInclude)
            .Any(folder => source.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase));
    }
}

internal static class LAuditLookupWalker
{
    internal static (SyntaxNode?, List<SyntaxNode>)? LAuditLookupRead(
        IReadOnlyList<(SyntaxNode, ExpressionSyntax?)> writes)
    {
        ISymbol? shared = null;
        SyntaxNode? engine = null;
        List<SyntaxNode> plain = [];
        foreach ((SyntaxNode reference, ExpressionSyntax? value) in writes)
        {
            List<ExpressionSyntax> calls = [];
            if (!LAuditCallScan(value, calls, new HashSet<ISymbol>(SymbolEqualityComparer.Default)))
            {
                return null;
            }

            foreach (ExpressionSyntax call in calls)
            {
                shared ??= LAuditBind.LAuditSymbolRead(call);
                if (!SymbolEqualityComparer.Default.Equals(shared, LAuditBind.LAuditSymbolRead(call)))
                {
                    return null;
                }
            }

            List<string> inputs = calls.Select(LAuditInputResolve).ToList();
            if (inputs.Contains("engine"))
            {
                engine ??= reference;
            }
            else if (inputs.Contains("plain"))
            {
                plain.Add(reference);
            }
        }

        return (engine, plain);
    }

    private static bool LAuditCallScan(ExpressionSyntax? value, List<ExpressionSyntax> calls, HashSet<ISymbol> visiting)
    {
        while (value is ParenthesizedExpressionSyntax or CastExpressionSyntax
               or PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression })
        {
            value = value switch
            {
                ParenthesizedExpressionSyntax group => group.Expression,
                CastExpressionSyntax cast => cast.Expression,
                PostfixUnaryExpressionSyntax forgiven => forgiven.Operand,
                _ => value
            };
        }

        if (value is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax
            && LAuditBind.LAuditSymbolRead(value) is IMethodSymbol callee
            && LAuditBind.LAuditShellCheck(callee.ContainingType)
            && LAuditTruthWalker.LAuditCallRead(value) is null)
        {
            calls.Add(value);
            return true;
        }

        return value is IdentifierNameSyntax or MemberAccessExpressionSyntax
               && LAuditBind.LAuditSymbolRead(value) is { } held
               && visiting.Add(held)
               && LAuditOriginWalker.LAuditOriginRead(held) is { Count: > 0 } writes
               && writes.Where(write => LAuditOriginWalker.LAuditWriterResolve(write) != "clear")
                   .All(write => LAuditCallScan(write, calls, visiting));
    }

    private static string LAuditInputResolve(ExpressionSyntax call)
    {
        IEnumerable<ExpressionSyntax> inputs = call switch
        {
            InvocationExpressionSyntax invocation => invocation.ArgumentList.Arguments
                .Select(argument => argument.Expression)
                .Concat(invocation.Expression is MemberAccessExpressionSyntax access
                        && LAuditBind.LAuditSymbolRead(call) is { IsStatic: false }
                    ? [access.Expression]
                    : []),
            BaseObjectCreationExpressionSyntax creation => (creation.ArgumentList?.Arguments
                    .Select(argument => argument.Expression) ?? [])
                .Concat(creation.Initializer?.Expressions.Select(entry =>
                    entry is AssignmentExpressionSyntax set ? set.Right : entry) ?? []),
            _ => []
        };
        SemanticModel model = LAuditBind.LAuditModelRead(call);
        List<string> verdicts = inputs
            .Where(input => !model.GetConstantValue(input).HasValue)
            .Select(LAuditOriginWalker.LAuditWriterResolve)
            .ToList();
        return verdicts.Contains("engine") ? "engine" : verdicts.Contains("plain") ? "plain" : "key";
    }
}

internal static class LAuditLaunderingWalker
{
    private const string LAuditLogicColour = "logic value";

    private const string LAuditTextColour = "control input";

    private static IReadOnlySet<ISymbol> LAuditReaderNames = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

    public static IReadOnlyList<LViolation> LAuditRun(IReadOnlyList<string> sourcePaths, IReadOnlySet<ISymbol> readers)
    {
        LAuditReaderNames = readers;
        List<LViolation> violations = [];
        foreach (SyntaxNode root in LAuditBind.LAuditWalkRead(sourcePaths).Where(LAuditBind.LAuditWalkCheck))
        {
            foreach (MemberDeclarationSyntax member in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                if (member is BaseTypeDeclarationSyntax or FieldDeclarationSyntax or BaseNamespaceDeclarationSyntax)
                {
                    continue;
                }

                LAuditMemberScan(member, violations);
            }
        }

        return violations;
    }

    private static void LAuditMemberScan(MemberDeclarationSyntax member, List<LViolation> violations)
    {
        Dictionary<ISymbol, string> tainted = LAuditTaintRead(member);
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (SyntaxNode node in member.DescendantNodes())
        {
            if (node is MemberDeclarationSyntax)
            {
                continue;
            }

            (ExpressionSyntax LViolationSource, string LViolationReason)? sink = node switch
            {
                BinaryExpressionSyntax binary
                    when !binary.IsKind(SyntaxKind.CoalesceExpression)
                         && !binary.IsKind(SyntaxKind.LogicalAndExpression)
                         && !binary.IsKind(SyntaxKind.LogicalOrExpression)
                         && !LAuditStrictWalker.LAuditNullCheck(binary.Left)
                         && !LAuditStrictWalker.LAuditNullCheck(binary.Right)
                         && !((binary.IsKind(SyntaxKind.EqualsExpression)
                               || binary.IsKind(SyntaxKind.NotEqualsExpression))
                              && (binary.Left.Kind()
                                      is SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression
                                  || binary.Right.Kind()
                                      is SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression))
                    => (binary, $"in {binary.OperatorToken.ValueText}"),
                InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } query
                    when LAuditTruthSetting.LAuditMoonlightingVerbs.Contains(
                        access.Name.Identifier.ValueText, StringComparer.Ordinal)
                    => (query, $"queried by {access.Name.Identifier.ValueText}"),
                IfStatementSyntax branch when LAuditConditionCheck(branch.Condition)
                    => (branch.Condition, "decides an if"),
                ConditionalExpressionSyntax choice when LAuditConditionCheck(choice.Condition)
                    => (choice.Condition, "decides a ternary"),
                SwitchStatementSyntax select => (select.Expression, "decides a switch"),
                SwitchExpressionSyntax arms => (arms.GoverningExpression, "decides a switch expression"),
                _ => null
            };
            if (sink is null || LAuditStrictWalker.LAuditDataCheck(sink.Value.LViolationSource))
            {
                continue;
            }

            if (LAuditTaintFind(sink.Value.LViolationSource, tainted) is not (string name, string colour))
            {
                continue;
            }

            int line = LAuditLineRead(node);
            string reason = $"{colour} {sink.Value.LViolationReason} through '{name}'";
            if (seen.Add($"{line}:{reason}"))
            {
                string excerpt = node.ToString().Split('\n')[0].Trim();
                violations.Add(new LViolation(node.SyntaxTree.FilePath, line, excerpt, "Laundering", reason));
            }
        }
    }

    private static Dictionary<ISymbol, string> LAuditTaintRead(MemberDeclarationSyntax member)
    {
        Dictionary<ISymbol, string> tainted = new(SymbolEqualityComparer.Default);
        foreach (ParameterSyntax parameter in member.DescendantNodesAndSelf().OfType<ParameterSyntax>())
        {
            if (LAuditBind.LAuditSymbolRead(parameter) is IParameterSymbol symbol
                && LAuditBind.LAuditEngineCheck(symbol.Type))
            {
                tainted[symbol] = LAuditLogicColour;
            }
        }

        foreach (SyntaxNode node in member.DescendantNodes())
        {
            switch (node)
            {
                case VariableDeclaratorSyntax { Initializer.Value: var value } declarator
                    when LAuditColourRead(value, tainted) is string colour:
                    LAuditColourAdd(declarator, colour, tainted);
                    break;
                case AssignmentExpressionSyntax { Left: IdentifierNameSyntax local } assignment
                    when LAuditBind.LAuditSymbolRead(local) is ILocalSymbol
                         && LAuditColourRead(assignment.Right, tainted) is string colour:
                    LAuditColourAdd(local, colour, tainted);
                    break;
                case ForEachStatementSyntax loop when LAuditColourRead(loop.Expression, tainted) is string colour:
                    LAuditColourAdd(loop, colour, tainted);
                    break;
                case IsPatternExpressionSyntax pattern
                    when LAuditColourRead(pattern.Expression, tainted) is string colour:
                    foreach (SingleVariableDesignationSyntax designation in
                             pattern.Pattern.DescendantNodesAndSelf().OfType<SingleVariableDesignationSyntax>())
                    {
                        LAuditColourAdd(designation, colour, tainted);
                    }

                    break;
            }
        }

        return tainted;
    }

    private static void LAuditColourAdd(SyntaxNode node, string colour, Dictionary<ISymbol, string> tainted)
    {
        if (LAuditBind.LAuditSymbolRead(node) is { } symbol)
        {
            tainted[symbol] = colour;
        }
    }

    private static string? LAuditColourRead(ExpressionSyntax value, Dictionary<ISymbol, string> tainted)
    {
        return LAuditTaintFind(value, tainted)?.LViolationColour;
    }

    private static (string LViolationName, string LViolationColour)? LAuditTaintFind(
        SyntaxNode node, Dictionary<ISymbol, string> tainted)
    {
        foreach (SyntaxNode child in node.DescendantNodesAndSelf(parent =>
                     parent is not InvocationExpressionSyntax
                     {
                         Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" }
                     } named
                     || LAuditBind.LAuditSymbolRead(named) is not null))
        {
            switch (child)
            {
                case IdentifierNameSyntax name
                    when LAuditBind.LAuditSymbolRead(name) is { } symbol
                         && tainted.TryGetValue(symbol, out string? colour):
                    return (name.Identifier.ValueText, colour);
                case IdentifierNameSyntax or MemberBindingExpressionSyntax
                    when LAuditBind.LAuditEngineCheck(child):
                    return (child.ToString(), LAuditLogicColour);
                case IdentifierNameSyntax name
                    when LAuditBind.LAuditSymbolRead(name) is
                         {
                             IsStatic: true,
                             Kind: SymbolKind.Field or SymbolKind.Property,
                             ContainingType: { TypeKind: not TypeKind.Enum } owner
                         }
                         && LAuditBind.LAuditConductCheck(owner):
                    return (name.Identifier.ValueText, LAuditLogicColour);
                case IdentifierNameSyntax name
                    when LAuditBind.LAuditSymbolRead(name) is { } symbol && LAuditReaderNames.Contains(symbol):
                    return (name.Identifier.ValueText, LAuditLogicColour);
                case InvocationExpressionSyntax input
                    when LAuditBind.LAuditMemberCheck(
                        LAuditBind.LAuditSymbolRead(input), LAuditTruthSetting.LAuditConsoleInput):
                    return (input.Expression.ToString(), LAuditTextColour);
                case MemberAccessExpressionSyntax input
                    when LAuditTruthSetting.LAuditInputMembers.Contains(
                             input.Name.Identifier.ValueText, StringComparer.Ordinal)
                         && LAuditInputCheck(input.Expression):
                    return (input.Expression.ToString(), LAuditTextColour);
            }
        }

        return null;
    }

    private static bool LAuditInputCheck(ExpressionSyntax receiver)
    {
        if (LAuditBind.LAuditSymbolRead(receiver) is INamespaceSymbol)
        {
            return false;
        }

        ITypeSymbol? type = LAuditBind.LAuditTypeRead(receiver);
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.TypeKind is TypeKind.Error or TypeKind.Dynamic
                || current.ToDisplayString() == LAuditTruthSetting.LAuditInputBase)
            {
                return true;
            }
        }

        return type is null;
    }

    private static bool LAuditConditionCheck(ExpressionSyntax condition)
    {
        bool presence = condition
            .DescendantNodesAndSelf(parent => parent is BinaryExpressionSyntax chain
                                              && (chain.IsKind(SyntaxKind.LogicalAndExpression)
                                                  || chain.IsKind(SyntaxKind.LogicalOrExpression)))
            .OfType<ExpressionSyntax>()
            .Where(operand => !operand.IsKind(SyntaxKind.LogicalAndExpression)
                              && !operand.IsKind(SyntaxKind.LogicalOrExpression))
            .All(operand => operand switch
            {
                IsPatternExpressionSyntax { Pattern: var pattern } => LAuditStrictWalker.LAuditPatternCheck(pattern),
                BinaryExpressionSyntax binary
                    when binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression)
                    => LAuditStrictWalker.LAuditNullCheck(binary.Left)
                       || LAuditStrictWalker.LAuditNullCheck(binary.Right),
                _ => false
            });
        return !presence && !LAuditVerdictCheck(condition);
    }

    private static bool LAuditVerdictCheck(ExpressionSyntax condition)
    {
        ExpressionSyntax core = LAuditStrictWalker.LAuditCoreRead(condition);
        if (core is not (InvocationExpressionSyntax or MemberAccessExpressionSyntax or IdentifierNameSyntax)
            || LAuditBind.LAuditSymbolRead(core) is not { } symbol)
        {
            return false;
        }

        return LAuditBind.LAuditLogicCheck(symbol) || LAuditReaderNames.Contains(symbol);
    }

    private static int LAuditLineRead(SyntaxNode node)
    {
        return node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
    }
}

internal static class LAuditMoonlightingWalker
{
    public static IReadOnlyList<LViolation> LAuditRun(IReadOnlyList<string> sourcePaths)
    {
        List<LViolation> violations = [];
        foreach (SyntaxNode root in LAuditBind.LAuditWalkRead(sourcePaths).Where(LAuditBind.LAuditWalkCheck))
        {
            LAuditStrictWalker.LAuditHomoglyphScan(root, violations);
            LAuditMoonlightingScan(root, violations);
        }

        return violations;
    }

    private static void LAuditMoonlightingScan(SyntaxNode root, List<LViolation> violations)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            string? reason = node switch
            {
                BinaryExpressionSyntax binary
                    when !binary.IsKind(SyntaxKind.CoalesceExpression)
                         && !binary.IsKind(SyntaxKind.LogicalAndExpression)
                         && !binary.IsKind(SyntaxKind.LogicalOrExpression)
                         && !LAuditStrictWalker.LAuditNullCheck(binary.Left)
                         && !LAuditStrictWalker.LAuditNullCheck(binary.Right)
                         && !LAuditSetterCheck(binary)
                         && (LAuditStrictWalker.LAuditDataCheck(binary.Left)
                             || LAuditStrictWalker.LAuditDataCheck(binary.Right))
                    => $"logic value in {binary.OperatorToken.ValueText}",
                InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } query
                    when LAuditTruthSetting.LAuditMoonlightingVerbs.Contains(
                             access.Name.Identifier.ValueText, StringComparer.Ordinal)
                         && (LAuditStrictWalker.LAuditDataCheck(access.Expression)
                             || query.ArgumentList.Arguments.Any(argument =>
                                 LAuditStrictWalker.LAuditDataCheck(argument.Expression)))
                    => $"logic value queried by {access.Name.Identifier.ValueText}",
                CastExpressionSyntax cast when LAuditStrictWalker.LAuditDataCheck(cast.Expression)
                    => $"logic value cast to {cast.Type}",
                TypeOfExpressionSyntax reflected when LAuditBind.LAuditEngineCheck(reflected.Type)
                    => "logic type taken by typeof",
                AttributeArgumentSyntax argument when LAuditStrictWalker.LAuditDataCheck(argument.Expression)
                    => "logic value in an attribute",
                IfStatementSyntax branch when LAuditConditionCheck(branch.Condition)
                    => "logic value decides an if",
                ConditionalExpressionSyntax choice when LAuditConditionCheck(choice.Condition)
                    => "logic value decides a ternary",
                SwitchStatementSyntax select when LAuditStrictWalker.LAuditDataCheck(select.Expression)
                    => "logic value decides a switch",
                SwitchExpressionSyntax arms when LAuditStrictWalker.LAuditDataCheck(arms.GoverningExpression)
                    => "logic value decides a switch expression",
                _ => null
            };

            if (reason is null)
            {
                continue;
            }

            int line = LAuditLineRead(node);
            if (seen.Add($"{line}:{reason}"))
            {
                string excerpt = node.ToString().Split('\n')[0].Trim();
                violations.Add(new LViolation(root.SyntaxTree.FilePath, line, excerpt, "Moonlighting", reason));
            }
        }
    }

    private static bool LAuditConditionCheck(ExpressionSyntax condition)
    {
        if (condition is IsPatternExpressionSyntax { Pattern: var pattern }
            && LAuditStrictWalker.LAuditPatternCheck(pattern))
        {
            return false;
        }

        if (condition is BinaryExpressionSyntax binary
            && (binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression))
            && (LAuditStrictWalker.LAuditNullCheck(binary.Left)
                || LAuditStrictWalker.LAuditNullCheck(binary.Right)
                || LAuditSetterCheck(binary)))
        {
            return false;
        }

        return !LAuditVerdictCheck(condition) && LAuditStrictWalker.LAuditDataCheck(condition);
    }

    private static bool LAuditSetterCheck(BinaryExpressionSyntax binary)
    {
        if (!binary.IsKind(SyntaxKind.EqualsExpression) && !binary.IsKind(SyntaxKind.NotEqualsExpression)
            || binary.FirstAncestorOrSelf<AccessorDeclarationSyntax>() is not { } accessor
            || !accessor.IsKind(SyntaxKind.SetAccessorDeclaration)
            && !accessor.IsKind(SyntaxKind.InitAccessorDeclaration))
        {
            return false;
        }

        return new[] { binary.Left, binary.Right }.Any(side =>
            side is IdentifierNameSyntax { Identifier.ValueText: "value" }
            && LAuditBind.LAuditSymbolRead(side) is IParameterSymbol { IsImplicitlyDeclared: true });
    }

    private static bool LAuditVerdictCheck(ExpressionSyntax condition)
    {
        ExpressionSyntax core = LAuditStrictWalker.LAuditCoreRead(condition);
        return core is InvocationExpressionSyntax or MemberAccessExpressionSyntax or IdentifierNameSyntax
               && LAuditBind.LAuditLogicCheck(LAuditBind.LAuditSymbolRead(core));
    }

    private static int LAuditLineRead(SyntaxNode node)
    {
        return node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
    }
}
'@

function Get-HelperCacheFolder {
    # The helper is compiled once per text, framework and SDK and kept under the temp folder, so a
    # later run skips the restore and the build and pays for the binding alone.
    param(
        [Parameter(Mandatory = $true)][string]$ProjectName,
        [Parameter(Mandatory = $true)][string]$TargetFramework,
        [Parameter(Mandatory = $true)][string]$SdkVersion
    )

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $seed = $script:HelperProject + "`n" + $script:HelperProgram + "`n" + $script:HelperWalker + "`n" + $script:BinderSource + "`n" + $TargetFramework + "`n" + $SdkVersion
        $digest = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($seed))
    }
    finally {
        $sha.Dispose()
    }

    $hash = ([System.BitConverter]::ToString($digest) -replace '-', '').Substring(0, 16).ToLowerInvariant()
    return Join-Path ([System.IO.Path]::GetTempPath()) ($ProjectName + '-AuditUi' + [System.IO.Path]::DirectorySeparatorChar + $hash)
}

function Invoke-NativeCommand {
    # Native stderr must not stop the script under Windows PowerShell 5.1, so the preference is
    # lowered around the call and the exit code is judged instead.
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & $FilePath @Arguments 2>&1 | ForEach-Object { [string]$_ }
    }
    finally {
        $ErrorActionPreference = $nativePreference
    }

    return [pscustomobject]@{ Output = @($output); ExitCode = $LASTEXITCODE }
}

function Invoke-AuditHelper {
    param(
        [Parameter(Mandatory = $true)][string]$ProjectName,
        [Parameter(Mandatory = $true)][string]$TargetFramework,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($null -eq $dotnet) {
        throw 'The .NET SDK is required, but dotnet was not found on PATH.'
    }

    $sdk = Invoke-NativeCommand -FilePath $dotnet.Source -Arguments @('--version')
    if ($sdk.ExitCode -ne 0) {
        throw "The .NET SDK version could not be read.`n$($sdk.Output -join [Environment]::NewLine)"
    }

    $sdkVersion = ([string]($sdk.Output | Select-Object -First 1)).Trim()
    $cacheFolder = Get-HelperCacheFolder -ProjectName $ProjectName -TargetFramework $TargetFramework -SdkVersion $sdkVersion
    $binaryPath = Join-Path (Join-Path $cacheFolder 'bin') 'AuditUi.dll'
    if (-not (Test-Path -LiteralPath $binaryPath -PathType Leaf)) {
        Write-Host 'Compiling the UI walker once for this SDK...'
        $cacheParent = Split-Path -Parent $cacheFolder
        if (Test-Path -LiteralPath $cacheParent) {
            Get-ChildItem -LiteralPath $cacheParent -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        }

        $helperFolder = Join-Path $cacheFolder 'helper'
        [System.IO.Directory]::CreateDirectory($helperFolder) | Out-Null
        $encoding = New-Object System.Text.UTF8Encoding($false)
        $projectPath = Join-Path $helperFolder 'AuditUi.csproj'
        [System.IO.File]::WriteAllText($projectPath, $script:HelperProject.Replace('{TARGET_FRAMEWORK}', $TargetFramework), $encoding)
        [System.IO.File]::WriteAllText((Join-Path $helperFolder 'Program.cs'), $script:HelperProgram, $encoding)
        [System.IO.File]::WriteAllText((Join-Path $helperFolder 'Walker.cs'), $script:HelperWalker, $encoding)
        [System.IO.File]::WriteAllText((Join-Path $helperFolder 'AuditBinder.cs'), $script:BinderSource, $encoding)
        $build = Invoke-NativeCommand -FilePath $dotnet.Source -Arguments @('build', $projectPath, '--configuration', 'Release',
            '--nologo', '--verbosity', 'quiet', '--output', (Join-Path $cacheFolder 'bin'))
        if ($build.ExitCode -ne 0) {
            Remove-Item -LiteralPath $cacheFolder -Recurse -Force -ErrorAction SilentlyContinue
            throw "The UI walker could not be built.`n$($build.Output -join [Environment]::NewLine)"
        }
    }

    $run = Invoke-NativeCommand -FilePath $dotnet.Source -Arguments (@($binaryPath) + $Arguments)
    if ($run.ExitCode -ne 0) {
        throw "The UI walker failed.`n$($run.Output -join [Environment]::NewLine)"
    }
}

$projectRoot = [System.IO.Path]::GetFullPath($Root)
$configFull = Join-AuditPath -Base (Get-Location).Path -Relative $ConfigPath
$config = Read-AuditConfig -Path $configFull
$configFolder = Split-Path -Parent $configFull
$ledgerFull = Join-AuditPath -Base $configFolder -Relative ([string]$config.ledger)
if (-not (Test-Path -LiteralPath $ledgerFull -PathType Leaf)) {
    throw "The UI-audit ledger was not found: $ledgerFull"
}

$version = Read-ProjectVersion -Path (Join-AuditPath -Base $projectRoot -Relative ([string]$config.report.versionFile)) -Key ([string]$config.report.versionKey)
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path (Join-AuditPath -Base $projectRoot -Relative ([string]$config.report.directory)) ('{0}{1}.md' -f [string]$config.report.prefix, $version)
}
$reportFull = Join-AuditPath -Base (Get-Location).Path -Relative $OutputPath
$consoleItems = [int]$config.report.consoleItems

$previousNoLogo = $env:DOTNET_NOLOGO
$previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$temporaryFolder = Join-Path ([System.IO.Path]::GetTempPath()) ([string]$config.project + '-AuditUi-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($temporaryFolder) | Out-Null
try {
    # -Configuration changes the build output the binder reads, through a copy of its settings.
    $binderFull = Join-Path $PSScriptRoot 'AuditBinder.json'
    if ($PSBoundParameters.ContainsKey('Configuration')) {
        $binderText = [System.IO.File]::ReadAllText($binderFull)
        $binderText = [regex]::Replace($binderText, '"configuration"\s*:\s*"[^"]*"', ('"configuration": "' + $Configuration + '"'))
        $binderFull = Join-Path $temporaryFolder 'AuditBinder.json'
        [System.IO.File]::WriteAllText($binderFull, $binderText, (New-Object System.Text.UTF8Encoding($false)))
    }

    $summaryFull = Join-Path $temporaryFolder 'summary.json'
    $pageDataPath = Join-Path $temporaryFolder 'page.json'
    Invoke-AuditHelper -ProjectName ([string]$config.project) -TargetFramework ([string]$config.helper.framework) -Arguments @(
        $configFull, $projectRoot, $binderFull, $ledgerFull, $summaryFull, $reportFull, $version, $pageDataPath)
    $summary = Get-Content -LiteralPath $summaryFull -Raw -Encoding UTF8 | ConvertFrom-Json
    if (-not (Test-Path -LiteralPath $pageDataPath -PathType Leaf)) {
        throw "The UI walker wrote no page data: $pageDataPath"
    }
    $utf8 = [System.Text.UTF8Encoding]::new($false)
    $pageData = [System.IO.File]::ReadAllText($pageDataPath, $utf8)
}
finally {
    $env:DOTNET_NOLOGO = $previousNoLogo
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = $previousTelemetry
    if (Test-Path -LiteralPath $temporaryFolder) {
        Remove-Item -LiteralPath $temporaryFolder -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$scanned = $summary.scanned
$scannedCounts = @((Format-Count $scanned.surface), (Format-Count $scanned.host), (Format-Count $scanned.markup),
    (Format-Count $scanned.driver), (Format-Count $scanned.strict), (Format-Count $scanned.truth))
Write-Host ('Scanned: {0} shell, {1} host, {2} markup and {3} driver files; {4} strict and {5} truth hits' -f $scannedCounts) -ForegroundColor DarkGray

$counters = @($summary.counters)
# The helper names each gate's meaning, so the console and the page read one text.
$gateRows = @(foreach ($counter in $counters) {
    $label = [string]$counter.label
    [pscustomobject]@{ Gate = $label; Count = [long]$counter.value; Meaning = [string]$counter.meaning; Section = $label }
})
Write-AuditSection 'Result'
$statusWidth = 6
$countWidth = [Math]::Max(5, ($gateRows | ForEach-Object { (Format-Count $_.Count).Length } | Measure-Object -Maximum).Maximum)
$gateWidth = [Math]::Max(4, ($gateRows | ForEach-Object { $_.Gate.Length } | Measure-Object -Maximum).Maximum)
$meaningWidth = [Math]::Max(7, ($gateRows | ForEach-Object { $_.Meaning.Length } | Measure-Object -Maximum).Maximum)
Write-Host ('{0}  {1}  {2}  Meaning' -f 'Status'.PadRight($statusWidth), 'Count'.PadLeft($countWidth), 'Gate'.PadRight($gateWidth)) -ForegroundColor Cyan
Write-Host (@(('-' * $statusWidth), ('-' * $countWidth), ('-' * $gateWidth), ('-' * $meaningWidth)) -join '  ') -ForegroundColor Cyan
foreach ($gateRow in $gateRows) {
    $failing = $gateRow.Count -gt 0
    $status = if ($failing) { 'FAIL' } else { 'OK' }
    Write-Host $status -NoNewline -ForegroundColor $(if ($failing) { 'Red' } else { 'Green' })
    Write-Host ('{0}  {1}  {2}  {3}' -f ''.PadRight($statusWidth - $status.Length), (Format-Count $gateRow.Count).PadLeft($countWidth), $gateRow.Gate.PadRight($gateWidth), $gateRow.Meaning)
}
$failedGates = @($gateRows | Where-Object { $_.Count -gt 0 })
Write-Host ''
if ($failedGates.Count -eq 0) {
    Write-Host ('PASS: all {0} gates at 0.' -f $gateRows.Count) -ForegroundColor Green
}
else {
    Write-Host ('FAIL: {0} of {1} gates above 0. See {2}.' -f $failedGates.Count, $gateRows.Count, (($failedGates | ForEach-Object { '"' + $_.Section + '"' }) -join ', ')) -ForegroundColor Red
}

Write-AuditSection 'Hits by kind'
$kindRows = New-Object 'System.Collections.Generic.List[object]'
$totals = @(0, 0, 0, 0)
foreach ($kind in @($summary.kinds)) {
    [void]$kindRows.Add(@([string]$kind.audit, [string]$kind.kind, (Format-Count $kind.hits), (Format-Count $kind.files),
        (Format-Count $kind.ceiling), (Format-Count $kind.over)))
    $totals = @(($totals[0] + [long]$kind.hits), ($totals[1] + [long]$kind.files), ($totals[2] + [long]$kind.ceiling), ($totals[3] + [long]$kind.over))
}
[void]$kindRows.Add(@('Total', '', (Format-Count $totals[0]), (Format-Count $totals[1]), (Format-Count $totals[2]), (Format-Count $totals[3])))
Write-AuditTable -Header @('Audit', 'Kind', 'Hits', 'Files', 'Ceiling', 'Over') -Rows $kindRows.ToArray()

$failed = 0
foreach ($counter in $counters) {
    $value = [long]$counter.value
    $failed += $value
    if ($value -eq 0) {
        continue
    }

    $rows = @($counter.rows)
    Write-AuditSection ('{0} ({1})' -f [string]$counter.label, (Format-Count $value))
    foreach ($row in @($rows | Select-Object -First $consoleItems)) {
        Write-Host ([string]$row)
    }
    if ($rows.Count -gt $consoleItems) {
        Write-Host ('... and {0} more in the report.' -f (Format-Count ($rows.Count - $consoleItems)))
    }
}

Write-Host ''
Write-Host "Report: $reportFull"

# Page output: the helper's page data inside the template AuditUI.html, beside the Markdown report.
# The helper writes the JSON, so Windows PowerShell 5.1 and pwsh 7 write the same bytes.
$pageTemplatePath = Join-Path $PSScriptRoot 'AuditUI.html'
$pagePath = [System.IO.Path]::ChangeExtension($reportFull, '.html')
[System.IO.Directory]::CreateDirectory((Split-Path -Parent $pagePath)) | Out-Null
$pageTemplate = [System.IO.File]::ReadAllText($pageTemplatePath, $utf8)
foreach ($marker in @('/*__DATA__*/', '__TITLE__')) {
    if (-not $pageTemplate.Contains($marker)) { throw "The page template lacks the marker $marker : $pageTemplatePath" }
}
$pageText = $pageTemplate.Replace('__TITLE__', [System.Net.WebUtility]::HtmlEncode([string]$config.project)).Replace('/*__DATA__*/', $pageData.Trim())
[System.IO.File]::WriteAllText($pagePath, ($pageText -replace "`r`n", "`n"), $utf8)
Write-Host "Report: $pagePath"

if ($Open) {
    Start-Process -FilePath $reportFull
}

if (-not $NoOpen) {
    Start-Process -FilePath $pagePath
}

if ($failed -gt 0) {
    exit 1
}

exit 0
