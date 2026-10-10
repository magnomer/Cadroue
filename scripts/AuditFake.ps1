<#
.SYNOPSIS
Finds fake members: source methods and values that nothing live reads, or that only tests read.

.DESCRIPTION
A member can compile, pass its tests and still do nothing for the application. This audit binds
the sources with Roslyn, binds the tests against them, and records who reads every member.

  - A read is a call, a get, a handler subscription or any use as a value.
    Assigning a field or auto-property, incrementing it or passing it as `out` only writes it.
  - A member is live when a live reader reads it. Constructors, overrides, interface
    implementations, generated markup code, markup words and JSON-serialized properties are
    live by themselves.
  - Every member left over is fake:
      Orphan  nothing live and no test reads it
      Tested  only tests read it

A member read only by other fake members is fake too, so a whole dead chain is reported, not
just its head. A non-static class is tracked as well, though never reported: markup naming it as
an element, a construction or a live subclass keeps it live, and a read inside a constructor or a
field initializer belongs to the class. Markup is read as XML: attribute names and values and
element names, never comments. These are the rules of the convention test TAuditFake.

Each kind has a ceiling in ceilings. While enforced is true a kind counting above its ceiling
fails. A ceiling above its count is stale and always fails, so a shed hit is locked in. The
console prints the counters, the kinds and the first rows of each kind; a Markdown report with
every hit is written to {report.directory}\{prefix}{version}.md, in the same form as the report
of the convention test. A page with the same result is written beside it as
{report.directory}\{prefix}{version}.html, filled from the template AuditFake.html. The page
opens in the default browser unless -NoOpen is given.

Binding goes through the shared binder of AuditBinder.cs and AuditBinder.json: the tracked
sources, the generated code of every project and the host build output, with no compile error
allowed. Build the solution first so the generated markup classes and the package assemblies exist.
The scope line counts the source files the binder walked. The helper targets helper.framework,
the framework of the convention tests, and is compiled once per text and SDK into the temp folder.

Everything project-specific lives in AuditFake.json next to this script. No project source is
modified. Git and the .NET SDK are required.

.PARAMETER Root
Project root to audit. Defaults to the directory containing this script's parent.

.PARAMETER ReportDirectory
Overrides report.directory for this run, for the Markdown report and the page. Relative paths
resolve against the project root.

.PARAMETER Top
Overrides console.top: how many rows of each kind the console shows.

.PARAMETER Open
Open the Markdown report after the audit finishes.

.PARAMETER NoOpen
Write the page without opening it.

.PARAMETER NoPause
Do not stop at each console page. Paging is also off when output or input is redirected.

.PARAMETER Help
Display this help and exit. The alias -? is supported.

.EXAMPLE
AuditFake
Audit the current checkout.

.EXAMPLE
AuditFake -ReportDirectory D:\temp\audit -Top 10 -Open -NoOpen
Write the reports elsewhere, show ten rows of each kind, open the Markdown report but not the page.
#>
#requires -Version 5.1
# AUDITFAKE - AUDIT GENERATION 21.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the audited outcome changes.
# A generation names the set of checks the audit applies. Two projects on the same generation audit
# the same things and their reports compare directly, whatever else differs between the files. A check
# added, removed, or changed in what it reports is a new generation; wording, plumbing, and refactoring
# leave it alone. Every audit script shares one generation number with the convention-test settings,
# and each refuses a configuration written at another generation.
# Generation 11: the first generation of this audit. It binds the sources and the tests with Roslyn
# and reports every member nothing live reads as Orphan, or only tests read as Tested.
# Generation 12: the audit applies the rules of the convention test: class liveness, construction
# as a use, holder ownership inside constructors and initializers, XML markup reading, and the
# shared binder with no compile error.
# Generation 13: nothing this audit reports changes; the number rises with the UI audit, which
# counts every surface markup line that hooks logic into the markup and every surface member
# that is not a constructor.
# Generation 14: nothing this audit reports changes; the number rises with the UI audit, which
# also counts command parameters, member paths and literal tags in surface markup as hooks.
# Generation 15: nothing this audit reports changes; the number rises with the name audit, which
# counts prefix turfs, and the UI audit, which counts pack URIs, scaffold types and contract IDs.
# Generation 16: nothing this audit reports changes; the number rises with the structure audit, whose
# Unsealing kind counts engine types on the public members of sealed Deportment types.
# Generation 17: nothing this audit reports changes; the number rises with the vocabulary revision
# of the UI, object, structure and name audits.
# Generation 18: nothing this audit reports changes; the number rises with the UI audit, whose
# truth detector stops five false findings.
# Generation 19: nothing this audit reports changes; the number rises with the comment audit, whose
# hash line ties every comment file to the sources it describes.
# Generation 20: nothing this audit reports changes; the number rises with the UI audit, whose
# Mismatching kind only informs while a driver folder it waits on holds no source.
# Generation 21: nothing this audit reports changes; the number rises with the structure, name and
# UI audits, which gained kinds, and with the new fault audit.
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

if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Split-Path -Parent $PSScriptRoot
}

if ($Help) {
    @'
NAME
    AuditFake.ps1

SYNOPSIS
    Find source members that nothing live reads, or that only tests read.

SYNTAX
    AuditFake [-Root <path>] [-ReportDirectory <path>] [-Top <n>] [-Open] [-NoOpen] [-NoPause] [-Help]

OPTIONS
    -Root <path>             Project root. Defaults to the parent of the scripts folder.
    -ReportDirectory <path>  Overrides report.directory for the report and the page.
    -Top <n>                 Overrides console.top.
    -Open                    Open the Markdown report when done.
    -NoOpen                  Write the page without opening it.
    -NoPause                 No console paging.
    -Help                    Show this help.

KINDS
    Orphan  nothing live and no test reads the member.
    Tested  only tests read the member.

COUNTERS
    Above ceiling   kinds counting above their ceiling while enforced is true.
    Stale ceilings  kinds counting below their ceiling, enforced or not.

REPORTS
    {report.directory}\{prefix}{version}.md    every hit, as the convention test writes it.
    {report.directory}\{prefix}{version}.html  the same result as a page, opened unless -NoOpen.
'@ | Write-Host
    exit 0
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:AuditGeneration = 21

[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [Console]::OutputEncoding

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

function Write-AuditLine {
    param(
        [Parameter(Position = 0)][AllowEmptyString()][string]$Text = '',
        [ConsoleColor]$ForegroundColor,
        [string]$Lead = '',
        [ConsoleColor]$LeadColor = [ConsoleColor]::Gray
    )

    if ($script:PageLimit -gt 0) {
        $rows = [Math]::Max(1, [Math]::Ceiling($Text.Length / [double]$script:PageWidth))
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

    if ($Lead -ne '') {
        Write-Host $Lead -ForegroundColor $LeadColor -NoNewline
        Write-Host $Text.Substring([Math]::Min($Lead.Length, $Text.Length))
    }
    elseif ($PSBoundParameters.ContainsKey('ForegroundColor')) {
        Write-Host $Text -ForegroundColor $ForegroundColor
    }
    else {
        Write-Host $Text
    }
}

Write-AuditLine "AUDITFAKE - AUDIT GENERATION $script:AuditGeneration" -ForegroundColor Blue
$script:PathSeparators = [char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)

function Resolve-ProjectRoot {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw 'The project root is empty.'
    }

    $item = Get-Item -LiteralPath $Path -ErrorAction Stop
    if (-not $item.PSIsContainer) {
        throw "The project root is not a directory: $Path"
    }

    return $item.FullName.TrimEnd($script:PathSeparators)
}

function Read-AuditConfig {
    param([string]$ConfigPath)

    if (-not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) {
        throw "The audit configuration was not found: $ConfigPath"
    }

    try {
        $config = Get-Content -LiteralPath $ConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "The audit configuration is not valid JSON: $ConfigPath`n$($_.Exception.Message)"
    }

    $required = @(
        'generation', 'project', 'enforced', 'ceilings', 'helper.framework',
        'sources.roots', 'sources.tests', 'sources.extensions', 'sources.markup',
        'sources.excludeSegments', 'sources.excludeSuffixes', 'sources.excludePrefixes',
        'implicitUsings',
        'console.top',
        'report.directory', 'report.versionFile', 'report.versionKey', 'report.prefix'
    )

    foreach ($key in $required) {
        $node = $config
        foreach ($segment in $key.Split('.')) {
            if ($null -eq $node -or -not ($node.PSObject.Properties.Name -contains $segment)) {
                throw "The audit configuration has no key '$key': $ConfigPath"
            }
            $node = $node.$segment
        }
    }

    if ([int]$config.generation -ne $script:AuditGeneration) {
        throw "The fake-audit configuration is generation $($config.generation) but this tooling is generation $script:AuditGeneration.`nA generation names the set of checks applied, so the two must match: $ConfigPath"
    }

    return $config
}

function Read-ProjectVersion {
    param([string]$ProjectRoot, $Config)

    $versionPath = Join-Path $ProjectRoot $Config.report.versionFile
    if (-not (Test-Path -LiteralPath $versionPath -PathType Leaf)) {
        throw "Version file was not found: $versionPath"
    }

    try {
        $versionData = Get-Content -LiteralPath $versionPath -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        throw "Version file is not valid JSON: $versionPath`n$($_.Exception.Message)"
    }

    $version = [string]$versionData.($Config.report.versionKey)
    if ([string]::IsNullOrWhiteSpace($version)) {
        return 'unknown'
    }

    return $version
}

function Test-ExcludedRelativePath {
    param([string]$RelativePath, $Config)

    $segments = $RelativePath -split '[\\/]'
    foreach ($segment in $segments) {
        if ($Config.sources.excludeSegments -contains $segment) {
            return $true
        }
    }

    $fileName = $segments[$segments.Length - 1]
    foreach ($suffix in $Config.sources.excludeSuffixes) {
        if ($fileName.EndsWith($suffix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    foreach ($prefix in $Config.sources.excludePrefixes) {
        if ($fileName.StartsWith($prefix, [System.StringComparison]::Ordinal)) {
            return $true
        }
    }

    return $false
}

function Get-TrackedFiles {
    param([string]$ProjectRoot, $Config, [string[]]$Roots, [string[]]$Extensions)

    $git = Get-Command git -ErrorAction SilentlyContinue
    if ($null -eq $git) {
        throw 'Git is required to enumerate project source files, but git was not found on PATH.'
    }

    if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot '.git'))) {
        throw "The project root is not a Git working tree: $ProjectRoot"
    }

    $pathspecs = @(foreach ($folder in $Roots) {
            foreach ($extension in $Extensions) {
                ':(icase)' + ([string]$folder).TrimEnd('/') + '/*' + $extension
            }
        })
    $lsArguments = @('-c', 'core.quotePath=false', '-C', $ProjectRoot,
        'ls-files', '--cached', '--others', '--exclude-standard', '--') + $pathspecs
    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $gitOutput = & $git.Source @lsArguments 2>&1
    $ErrorActionPreference = $nativePreference
    if ($LASTEXITCODE -ne 0) {
        throw "Git could not enumerate source files.`n$($gitOutput -join [Environment]::NewLine)"
    }

    $separator = [string][System.IO.Path]::DirectorySeparatorChar
    $rootPrefix = $ProjectRoot.TrimEnd($script:PathSeparators) + $separator
    $files = New-Object 'System.Collections.Generic.List[string]'

    foreach ($entry in $gitOutput) {
        $relativePath = ([string]$entry).Trim()
        if ([string]::IsNullOrWhiteSpace($relativePath) -or
            (Test-ExcludedRelativePath -RelativePath $relativePath -Config $Config)) {
            continue
        }

        $fullPath = $rootPrefix + $relativePath.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
            $files.Add($fullPath)
        }
    }

    return @($files.ToArray() | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } } -Unique)
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
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length != 8)
{
    Console.Error.WriteLine("usage: <config> <root> <manifest> <version> <report> <top> <binder> <page>");
    return 2;
}

string configPath = args[0];
string projectRoot = args[1];
string manifestPath = args[2];
string version = args[3];
string reportPath = args[4];
int top = int.Parse(args[5]);
string binderPath = args[6];
string pagePath = args[7];

JsonElement config = JsonDocument.Parse(File.ReadAllText(configPath)).RootElement;
string[] implicitUsings = config.GetProperty("implicitUsings").EnumerateArray().Select(item => item.GetString()!).ToArray();

Dictionary<char, List<string>> manifest = File.ReadAllLines(manifestPath)
    .Where(line => line.Length > 2)
    .GroupBy(line => line[0])
    .ToDictionary(group => group.Key, group => group.Select(line => line[2..]).ToList());
List<string> Take(char tag) => manifest.TryGetValue(tag, out List<string>? list) ? list : [];

CSharpParseOptions parseOptions = new(LanguageVersion.Preview, DocumentationMode.None, SourceCodeKind.Regular);
SyntaxTree Parse(string path) => CSharpSyntaxTree.ParseText(File.ReadAllText(path), parseOptions, path);

LAuditBinder binder = LAuditBinder.LAuditBinderRead(projectRoot, binderPath);
CSharpCompilation source = binder.LAuditCompilation;

List<SyntaxTree> testTrees = Take('T').AsParallel().AsOrdered().Select(Parse).ToList();
testTrees.Add(CSharpSyntaxTree.ParseText(string.Concat(implicitUsings.Select(space => $"global using {space};\n")), parseOptions));
CSharpCompilation tests = CSharpCompilation.Create(
    "AuditFakeTests",
    testTrees,
    source.References.Append(source.ToMetadataReference()),
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));

Regex word = new("[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);
List<XElement> nodes = Take('M').SelectMany(path => XDocument.Load(path).Descendants()).ToList();
HashSet<string> markup = nodes
    .SelectMany(node => node.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration))
    .SelectMany(attribute => word.Matches(attribute.Value).Select(match => match.Value).Append(attribute.Name.LocalName))
    .Concat(nodes.Select(node => node.Name.LocalName[(node.Name.LocalName.LastIndexOf('.') + 1)..]))
    .ToHashSet(StringComparer.Ordinal);
HashSet<string> elements = nodes
    .Select(node => node.Name.LocalName)
    .Where(name => !name.Contains('.', StringComparison.Ordinal))
    .ToHashSet(StringComparer.Ordinal);

Dictionary<string, Member> members = new(StringComparer.Ordinal);
foreach (SyntaxTree tree in binder.LAuditTrees)
{
    Fake.ScanMembers(source.GetSemanticModel(tree, true), binder, members);
}

Fake.AddLineage(source, members);

HashSet<string> names = new(members.Values.Select(member => member.Name), StringComparer.Ordinal);
HashSet<string> serialized = new(StringComparer.Ordinal);
foreach (SyntaxTree tree in source.SyntaxTrees)
{
    SemanticModel model = source.GetSemanticModel(tree, true);
    Fake.ScanUses(model, members, names, false);
    Fake.ScanPersisted(model, serialized);
}

foreach (SyntaxTree tree in tests.SyntaxTrees)
{
    Fake.ScanUses(tests.GetSemanticModel(tree, true), members, names, true);
}

Fake.MarkLive(members, markup, elements, serialized);
List<Hit> hits = members.Values
    .Where(member => !member.Live)
    .Where(member => !member.Key.StartsWith(Fake.TypeMark, StringComparison.Ordinal))
    .Select(member => Fake.CreateHit(member, members))
    .OrderBy(hit => hit.Path, StringComparer.Ordinal)
    .ThenBy(hit => hit.Line)
    .ToList();

int generation = config.GetProperty("generation").GetInt32();
bool enforced = config.GetProperty("enforced").GetBoolean();
List<(string Kind, int Ceiling)> ceilings = config.GetProperty("ceilings").EnumerateObject()
    .Select(item => (item.Name, item.Value.GetInt32()))
    .ToList();
int Tally(string kind) => hits.Count(hit => hit.Kind == kind);
List<string> above = ceilings
    .Where(pair => enforced && Tally(pair.Kind) > pair.Ceiling)
    .Select(pair => $"{pair.Kind}: {Tally(pair.Kind)} hit(s), ceiling {pair.Ceiling}")
    .ToList();
List<string> stale = ceilings
    .Where(pair => Tally(pair.Kind) < pair.Ceiling)
    .Select(pair => $"{pair.Kind}: {Tally(pair.Kind)} hit(s), ceiling {pair.Ceiling}")
    .ToList();

Console.WriteLine(
    $"Scanned: {binder.LAuditTrees.Count:N0} source files, {Take('T').Count:N0} test files, "
    + $"{Take('M').Count:N0} markup files, ceilings {(enforced ? "enforced" : "not enforced")}");
List<(string Gate, int Count, string Meaning)> gates =
[
    ("Above ceiling", above.Count, "kinds whose hits exceed their enforced ceiling"),
    ("Stale ceilings", stale.Count, "ceilings set above their current hits"),
];
Fake.WriteResult(gates);

Fake.WriteHeading("Members by kind");
List<string[]> kindRows = ceilings
    .Select(pair => new[] { pair.Kind, Tally(pair.Kind).ToString("N0"), pair.Ceiling.ToString("N0") })
    .ToList();
kindRows.Add(["Total", hits.Count.ToString("N0"), "-"]);
Fake.WriteTable(Fake.TextTable(["Kind", "Members", "Ceiling"], kindRows));

List<(string Title, List<string> Rows)> sections = ceilings
    .Select(pair => (pair.Kind, hits.Where(hit => hit.Kind == pair.Kind)
        .Select(hit => $"{hit.Path}:{hit.Line} {hit.Name}: {hit.Reason}").ToList()))
    .Append(("Above ceiling", above))
    .Append(("Stale ceilings", stale))
    .ToList();
foreach ((string title, List<string> rows) in sections.Where(section => section.Rows.Count > 0))
{
    string heading = $"{title} ({rows.Count:N0})";
    Fake.WriteHeading(heading);
    rows.Take(top).ToList().ForEach(Console.WriteLine);
    if (rows.Count > top)
    {
        Console.WriteLine($"... and {rows.Count - top:N0} more in the report.");
    }
}

Console.WriteLine();
Console.WriteLine($"Report: {reportPath}");

List<string> lines =
[
    $"# Fake audit {version}",
    string.Empty,
    $"- Generation: {generation}",
    $"- Enforced: {enforced}",
];
lines.AddRange(ceilings.Select(pair => $"- {pair.Kind}: {Tally(pair.Kind)}, ceiling {pair.Ceiling}"));
lines.Add($"- Above ceiling: {above.Count}");
lines.Add($"- Stale ceilings: {stale.Count}");
lines.Add(string.Empty);
lines.Add("A member is live when a live reader, a constructor, an override, generated code, markup or "
    + "the serializer reads it. Orphan is read by nothing live. Tested is read only by tests.");
List<(string Title, List<string> Rows)> chapters = ceilings
    .Select(pair => (pair.Kind, hits.Where(hit => hit.Kind == pair.Kind)
        .Select(hit => $"- `{hit.Path}:{hit.Line}` `{hit.Name}`: {hit.Reason}").ToList()))
    .Append(("Above ceiling", above.Select(row => "- " + row).ToList()))
    .Append(("Stale ceilings", stale.Select(row => "- " + row).ToList()))
    .ToList();
foreach ((string title, List<string> rows) in chapters)
{
    lines.Add(string.Empty);
    lines.Add($"## {title}");
    if (rows.Count > 0)
    {
        lines.Add(string.Empty);
        lines.AddRange(rows);
    }
}

string? reportFolder = Path.GetDirectoryName(reportPath);
if (!string.IsNullOrWhiteSpace(reportFolder))
{
    Directory.CreateDirectory(reportFolder);
}

File.WriteAllText(reportPath, string.Join("\n", lines) + "\n", new UTF8Encoding(false));

string[] Strings(JsonElement list) => list.EnumerateArray().Select(item => item.GetString()!).ToArray();
JsonElement sources = config.GetProperty("sources");
var page = new
{
    project = config.GetProperty("project").GetString(),
    version,
    generation,
    enforced,
    generated = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
    report = Path.GetFileName(reportPath),
    scanned = new
    {
        sources = binder.LAuditTrees.Count,
        tests = Take('T').Count,
        markup = Take('M').Count,
        members = members.Keys.Count(key => !key.StartsWith(Fake.TypeMark, StringComparison.Ordinal)),
        types = members.Keys.Count(key => key.StartsWith(Fake.TypeMark, StringComparison.Ordinal)),
    },
    gates = gates.Select(row => new { status = row.Count > 0 ? "FAIL" : "OK", gate = row.Gate, count = row.Count, meaning = row.Meaning }),
    ceilings = ceilings.Select(pair => new
    {
        kind = pair.Kind,
        count = Tally(pair.Kind),
        ceiling = pair.Ceiling,
        state = Tally(pair.Kind) < pair.Ceiling ? "STALE" : Tally(pair.Kind) <= pair.Ceiling ? "OK" : enforced ? "ABOVE" : "OVER",
    }),
    above,
    stale,
    hits = hits.Select(hit => new { kind = hit.Kind, name = hit.Name, path = hit.Path, line = hit.Line, reason = hit.Reason }),
    scope = new
    {
        roots = Strings(sources.GetProperty("roots")),
        tests = Strings(sources.GetProperty("tests")),
        extensions = Strings(sources.GetProperty("extensions")),
        markup = Strings(sources.GetProperty("markup")),
        excludeSegments = Strings(sources.GetProperty("excludeSegments")),
        excludeSuffixes = Strings(sources.GetProperty("excludeSuffixes")),
        excludePrefixes = Strings(sources.GetProperty("excludePrefixes")),
        implicitUsings,
        framework = config.GetProperty("helper").GetProperty("framework").GetString(),
        top,
    },
};
File.WriteAllText(pagePath, JsonSerializer.Serialize(page), new UTF8Encoding(false));
return above.Count + stale.Count > 0 ? 3 : 0;

internal static class Fake
{
    public const string TypeMark = "T:";

    private const string RootReader = "";

    public static void WriteHeading(string title)
    {
        Console.WriteLine();
        Console.WriteLine(title);
        Console.WriteLine(new string('-', title.Length));
    }

    public static void WriteTable(IEnumerable<string> lines)
    {
        foreach (string line in lines)
        {
            Console.WriteLine(line);
        }
    }

    public static void WriteResult(List<(string Gate, int Count, string Meaning)> rows)
    {
        WriteHeading("Result");
        int countWidth = Math.Max(5, rows.Max(row => row.Count.ToString("N0").Length));
        int gateWidth = Math.Max(4, rows.Max(row => row.Gate.Length));
        int meaningWidth = Math.Max(7, rows.Max(row => row.Meaning.Length));
        Console.WriteLine($"{"Status",-6}  {"Count".PadLeft(countWidth)}  {"Gate".PadRight(gateWidth)}  Meaning");
        Console.WriteLine($"{new string('-', 6)}  {new string('-', countWidth)}  {new string('-', gateWidth)}  {new string('-', meaningWidth)}");
        foreach ((string gate, int count, string meaning) in rows)
        {
            Console.WriteLine($"{(count > 0 ? "FAIL" : "OK"),-6}  {count.ToString("N0").PadLeft(countWidth)}  {gate.PadRight(gateWidth)}  {meaning}");
        }

        List<string> failed = rows.Where(row => row.Count > 0).Select(row => $"\"{row.Gate}\"").ToList();
        Console.WriteLine();
        Console.WriteLine(failed.Count == 0
            ? $"PASS: all {rows.Count} gates at 0."
            : $"FAIL: {failed.Count} of {rows.Count} gates above 0. See {string.Join(", ", failed)}.");
    }

    public static IEnumerable<string> TextTable(string[] header, List<string[]> rows)
    {
        int[] widths = header.Select((cell, column) => Math.Max(cell.Length, rows.Count == 0 ? 0 : rows.Max(row => row[column].Length))).ToArray();
        bool[] numeric = header.Select((cell, column) => rows.Count > 0 && rows.All(row => row[column] == "-" || double.TryParse(row[column], out _))).ToArray();
        string Line(string[] cells) => string.Join("  ", cells.Select((cell, column) => numeric[column] ? cell.PadLeft(widths[column]) : cell.PadRight(widths[column]))).TrimEnd();
        yield return Line(header);
        yield return string.Join("  ", widths.Select(width => new string('-', width)));
        foreach (string[] row in rows)
        {
            yield return Line(row);
        }
    }

    public static void ScanMembers(SemanticModel model, LAuditBinder binder, Dictionary<string, Member> members)
    {
        string path = binder.LAuditRelativeRead(model.SyntaxTree.FilePath);
        foreach (TypeDeclarationSyntax declaration in model.SyntaxTree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol type || type.TypeKind == TypeKind.Enum)
            {
                continue;
            }

            if (type is { TypeKind: TypeKind.Class, IsStatic: false })
            {
                Add(members, type, declaration, path, false);
            }

            if (declaration is RecordDeclarationSyntax { ParameterList: { } parameters })
            {
                foreach (ParameterSyntax parameter in parameters.Parameters)
                {
                    if (type.GetMembers(parameter.Identifier.ValueText).OfType<IPropertySymbol>().FirstOrDefault() is IPropertySymbol property)
                    {
                        Add(members, property, parameter, path, true);
                    }
                }
            }

            foreach (MemberDeclarationSyntax member in declaration.Members)
            {
                foreach (ISymbol symbol in Declared(model, member))
                {
                    if (IsCandidate(symbol))
                    {
                        Add(members, symbol, member, path, IsStored(member));
                    }
                }
            }
        }
    }

    public static void AddLineage(CSharpCompilation source, Dictionary<string, Member> members)
    {
        foreach (INamedTypeSymbol type in source.GetSymbolsWithName(_ => true, SymbolFilter.Type).OfType<INamedTypeSymbol>())
        {
            if (type.BaseType is { } parent
                && members.TryGetValue(Key(parent), out Member? inherited)
                && members.ContainsKey(Key(type)))
            {
                inherited.Readers.Add(Key(type));
            }
        }
    }

    public static void ScanUses(SemanticModel model, Dictionary<string, Member> members, HashSet<string> names, bool test)
    {
        SyntaxNode root = model.SyntaxTree.GetRoot();
        foreach (SimpleNameSyntax name in root.DescendantNodes().OfType<SimpleNameSyntax>())
        {
            if (!names.Contains(name.Identifier.ValueText))
            {
                continue;
            }

            SymbolInfo info = model.GetSymbolInfo(name);
            IEnumerable<ISymbol> bound = info.Symbol is ISymbol symbol ? [symbol] : info.CandidateSymbols;
            foreach (ISymbol target in bound)
            {
                AddUse(model, name, target, members, test);
            }
        }

        foreach (SyntaxNode creation in root.DescendantNodes().Where(node => node is BaseObjectCreationExpressionSyntax or AttributeSyntax))
        {
            if (model.GetSymbolInfo(creation).Symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } built)
            {
                AddUse(model, creation, built.ContainingType, members, test);
            }
        }

        foreach (CommonForEachStatementSyntax loop in root.DescendantNodes().OfType<CommonForEachStatementSyntax>())
        {
            ForEachStatementInfo info = model.GetForEachStatementInfo(loop);
            ISymbol?[] targets = [info.GetEnumeratorMethod, info.MoveNextMethod, info.CurrentProperty];
            foreach (ISymbol target in targets.OfType<ISymbol>())
            {
                AddUse(model, loop, target, members, test);
            }
        }
    }

    public static void ScanPersisted(SemanticModel model, HashSet<string> serialized)
    {
        foreach (InvocationExpressionSyntax call in model.SyntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol method
                || method.ContainingType?.ToDisplayString() != "System.Text.Json.JsonSerializer")
            {
                continue;
            }

            IEnumerable<ITypeSymbol?> types = method.TypeArguments
                .Concat(call.ArgumentList.Arguments.Select(argument => model.GetTypeInfo(argument.Expression).Type));
            foreach (ITypeSymbol? type in types)
            {
                AddPersisted(type, serialized, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));
            }
        }
    }

    public static void MarkLive(Dictionary<string, Member> members, HashSet<string> markup, HashSet<string> elements, HashSet<string> serialized)
    {
        Dictionary<string, List<Member>> dependents = new(StringComparer.Ordinal);
        Queue<Member> queue = new();
        foreach (Member member in members.Values)
        {
            foreach (string reader in member.Readers.Where(members.ContainsKey))
            {
                if (!dependents.TryGetValue(reader, out List<Member>? list))
                {
                    list = [];
                    dependents.Add(reader, list);
                }

                list.Add(member);
            }

            bool typed = member.Key.StartsWith(TypeMark, StringComparison.Ordinal);
            if ((typed ? elements : markup).Contains(member.Name)
                || serialized.Contains(member.Key)
                || member.Readers.Any(reader => !members.ContainsKey(reader)))
            {
                member.Live = true;
                queue.Enqueue(member);
            }
        }

        while (queue.TryDequeue(out Member? live))
        {
            foreach (Member member in dependents.GetValueOrDefault(live.Key) ?? [])
            {
                if (!member.Live)
                {
                    member.Live = true;
                    queue.Enqueue(member);
                }
            }
        }
    }

    public static Hit CreateHit(Member member, Dictionary<string, Member> members)
    {
        List<string> readers = member.Readers.Select(reader => members[reader].Name).Order(StringComparer.Ordinal).ToList();
        List<string> testers = member.Testers.Order(StringComparer.Ordinal).ToList();
        List<string> reasons = [];
        if (testers.Count > 0)
        {
            reasons.Add("by tests " + FormatList(testers));
        }

        if (readers.Count > 0)
        {
            reasons.Add("by fake members " + FormatList(readers));
        }

        string reason = reasons.Count == 0 ? "read by nothing" : "read only " + string.Join(" and ", reasons);
        return new Hit(member.Path, member.Line, member.Name, testers.Count > 0 ? "Tested" : "Orphan", reason);
    }

    private static void Add(Dictionary<string, Member> members, ISymbol symbol, SyntaxNode node, string path, bool stored)
    {
        string key = Key(symbol);
        if (!members.ContainsKey(key))
        {
            members.Add(key, new Member(key, symbol.Name, path, node.GetLocation().GetLineSpan().StartLinePosition.Line + 1, stored));
        }
    }

    private static IEnumerable<ISymbol> Declared(SemanticModel model, MemberDeclarationSyntax member) => member switch
    {
        BaseTypeDeclarationSyntax or DelegateDeclarationSyntax => [],
        BaseFieldDeclarationSyntax field => field.Declaration.Variables.Select(variable => model.GetDeclaredSymbol(variable)).OfType<ISymbol>(),
        _ => model.GetDeclaredSymbol(member) is ISymbol symbol ? [symbol] : [],
    };

    private static bool IsStored(MemberDeclarationSyntax member) => member switch
    {
        BaseFieldDeclarationSyntax => true,
        PropertyDeclarationSyntax { ExpressionBody: null, AccessorList: { } accessors } =>
            accessors.Accessors.All(accessor => accessor.Body is null && accessor.ExpressionBody is null),
        _ => false,
    };

    private static bool IsCandidate(ISymbol symbol)
    {
        if (symbol.IsImplicitlyDeclared || symbol.IsOverride || symbol.IsExtern)
        {
            return false;
        }

        bool kind = symbol switch
        {
            IMethodSymbol method => method.MethodKind == MethodKind.Ordinary && !(method.IsStatic && method.Name == "Main"),
            IPropertySymbol property => !property.IsIndexer && property.ExplicitInterfaceImplementations.Length == 0,
            IEventSymbol eventSymbol => eventSymbol.ExplicitInterfaceImplementations.Length == 0,
            IFieldSymbol field => field.AssociatedSymbol is null,
            _ => false,
        };
        return kind && Contracts(symbol).Count == 0;
    }

    private static List<ISymbol> Contracts(ISymbol symbol)
    {
        if (symbol.ContainingType is not INamedTypeSymbol type || symbol is IFieldSymbol)
        {
            return [];
        }

        return type.AllInterfaces
            .SelectMany(face => face.GetMembers())
            .Where(member => SymbolEqualityComparer.Default.Equals(
                type.FindImplementationForInterfaceMember(member)?.OriginalDefinition, symbol.OriginalDefinition))
            .ToList();
    }

    private static void AddUse(SemanticModel model, SyntaxNode site, ISymbol target, Dictionary<string, Member> members, bool test)
    {
        ISymbol normal = Normal(target);
        List<Member> reached = new[] { normal }
            .Concat(Contracts(normal))
            .Select(symbol => members.GetValueOrDefault(Key(symbol)))
            .OfType<Member>()
            .ToList();
        if (reached.Count == 0)
        {
            return;
        }

        string? owner = Owner(model, site, members);
        foreach (Member member in reached)
        {
            if (owner == member.Key || !IsRead(site, normal, member))
            {
                continue;
            }

            if (test)
            {
                member.Testers.Add(Label(model, site));
            }
            else
            {
                member.Readers.Add(owner ?? RootReader);
            }
        }
    }

    private static bool IsRead(SyntaxNode site, ISymbol target, Member member)
    {
        if (site is not ExpressionSyntax expression || target is IMethodSymbol)
        {
            return true;
        }

        while (expression.Parent is MemberAccessExpressionSyntax access && access.Name == expression)
        {
            expression = access;
        }

        if (expression.Parent is MemberBindingExpressionSyntax binding && binding.Name == expression)
        {
            expression = binding;
        }

        SyntaxNode? parent = expression.Parent;
        if (target is IEventSymbol)
        {
            return !member.Stored
                || parent is AssignmentExpressionSyntax
                {
                    RawKind: (int)SyntaxKind.AddAssignmentExpression or (int)SyntaxKind.SubtractAssignmentExpression,
                } subscription && subscription.Left == expression;
        }

        if (!member.Stored)
        {
            return true;
        }

        return parent switch
        {
            AssignmentExpressionSyntax assignment when assignment.Left == expression =>
                assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression),
            PostfixUnaryExpressionSyntax or PrefixUnaryExpressionSyntax => parent.Kind() is not (
                SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression
                or SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression),
            ArgumentSyntax argument when argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword) => false,
            ArgumentSyntax { Parent: TupleExpressionSyntax tuple } =>
                !(tuple.Parent is AssignmentExpressionSyntax deconstruction && deconstruction.Left == tuple),
            _ => true,
        };
    }

    private static string? Owner(SemanticModel model, SyntaxNode site, Dictionary<string, Member> members)
    {
        foreach (SyntaxNode ancestor in site.Ancestors())
        {
            switch (ancestor)
            {
                case BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax:
                case VariableDeclaratorSyntax { Parent.Parent: BaseFieldDeclarationSyntax }:
                    if (model.GetDeclaredSymbol(ancestor) is not ISymbol owner)
                    {
                        return null;
                    }

                    string key = Key(owner);
                    string? holder = owner.ContainingType is { } type ? Key(type) : null;
                    bool root = owner is IMethodSymbol { MethodKind: MethodKind.StaticConstructor };
                    return members.ContainsKey(key) || root || holder is null || !members.ContainsKey(holder) ? key : holder;
                case BaseTypeDeclarationSyntax:
                    return null;
            }
        }

        return null;
    }

    private static string Label(SemanticModel model, SyntaxNode site)
    {
        foreach (SyntaxNode ancestor in site.Ancestors())
        {
            if (ancestor is BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax
                && model.GetDeclaredSymbol(ancestor) is ISymbol owner)
            {
                return $"{owner.ContainingType?.Name}.{owner.Name}";
            }
        }

        return Path.GetFileName(site.SyntaxTree.FilePath);
    }

    private static void AddPersisted(ITypeSymbol? type, HashSet<string> serialized, HashSet<ITypeSymbol> seen)
    {
        if (type is null || !seen.Add(type))
        {
            return;
        }

        if (type is IArrayTypeSymbol array)
        {
            AddPersisted(array.ElementType, serialized, seen);
            return;
        }

        if (type is not INamedTypeSymbol named)
        {
            return;
        }

        foreach (ITypeSymbol argument in named.TypeArguments)
        {
            AddPersisted(argument, serialized, seen);
        }

        if (named.Locations.All(location => !location.IsInSource))
        {
            return;
        }

        serialized.Add(Key(named));
        foreach (IPropertySymbol property in named.GetMembers().OfType<IPropertySymbol>())
        {
            serialized.Add(Key(property));
            AddPersisted(property.Type, serialized, seen);
        }
    }

    private static string FormatList(List<string> names)
    {
        string shown = string.Join(", ", names.Take(3));
        return names.Count > 3 ? $"{shown} and {names.Count - 3} more" : shown;
    }

    private static ISymbol Normal(ISymbol symbol)
    {
        if (symbol is IMethodSymbol method)
        {
            method = method.ReducedFrom ?? method;
            method = method.PartialDefinitionPart ?? method;
            return method.AssociatedSymbol is ISymbol associated ? associated.OriginalDefinition : method.OriginalDefinition;
        }

        return symbol.OriginalDefinition;
    }

    private static string Key(ISymbol symbol)
    {
        ISymbol normal = Normal(symbol);
        return normal.GetDocumentationCommentId() ?? normal.ToDisplayString();
    }
}

internal sealed class Member(string key, string name, string path, int line, bool stored)
{
    public string Key { get; } = key;
    public string Name { get; } = name;
    public string Path { get; } = path;
    public int Line { get; } = line;
    public bool Stored { get; } = stored;
    public HashSet<string> Readers { get; } = new(StringComparer.Ordinal);
    public HashSet<string> Testers { get; } = new(StringComparer.Ordinal);
    public bool Live { get; set; }
}

internal sealed record Hit(string Path, int Line, string Name, string Kind, string Reason);
'@

function Get-HelperBinary {
    param([string]$DotnetPath, [string]$ProjectName, [string]$TargetFramework)

    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $sdkOutput = & $DotnetPath --version 2>&1
    $ErrorActionPreference = $nativePreference
    if ($LASTEXITCODE -ne 0) {
        throw "The .NET SDK version could not be read.`n$($sdkOutput -join [Environment]::NewLine)"
    }

    $sdkVersion = ([string]($sdkOutput | Select-Object -First 1)).Trim()
    $binderSource = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'AuditBinder.cs'))
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $seed = $script:HelperProject + "`n" + $script:HelperProgram + "`n" + $binderSource + "`n" + $TargetFramework + "`n" + $sdkVersion
        $digest = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($seed))
    }
    finally {
        $sha.Dispose()
    }

    $hash = ([System.BitConverter]::ToString($digest) -replace '-', '').Substring(0, 16).ToLowerInvariant()
    $cacheParent = Join-Path ([System.IO.Path]::GetTempPath()) ($ProjectName + '-AuditFake')
    $cacheFolder = Join-Path $cacheParent $hash
    $binaryPath = Join-Path (Join-Path $cacheFolder 'bin') 'AuditFake.Helper.dll'
    if (Test-Path -LiteralPath $binaryPath -PathType Leaf) {
        return $binaryPath
    }

    Write-AuditLine 'Compiling the fake binder once for this SDK...'
    if (Test-Path -LiteralPath $cacheParent) {
        Get-ChildItem -LiteralPath $cacheParent -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }

    $helperFolder = Join-Path $cacheFolder 'helper'
    [System.IO.Directory]::CreateDirectory($helperFolder) | Out-Null
    $encoding = New-Object System.Text.UTF8Encoding($false)
    $projectPath = Join-Path $helperFolder 'AuditFake.Helper.csproj'
    [System.IO.File]::WriteAllText($projectPath, $script:HelperProject.Replace('{TARGET_FRAMEWORK}', $TargetFramework), $encoding)
    [System.IO.File]::WriteAllText((Join-Path $helperFolder 'Program.cs'), $script:HelperProgram, $encoding)
    [System.IO.File]::WriteAllText((Join-Path $helperFolder 'AuditBinder.cs'), $binderSource, $encoding)
    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $buildOutput = & $DotnetPath build $projectPath --configuration Release --nologo --verbosity quiet --output (Join-Path $cacheFolder 'bin') 2>&1
    $ErrorActionPreference = $nativePreference
    if ($LASTEXITCODE -ne 0) {
        Remove-Item -LiteralPath $cacheFolder -Recurse -Force -ErrorAction SilentlyContinue
        throw "The fake binder could not be built.`n$($buildOutput -join [Environment]::NewLine)"
    }

    return $binaryPath
}

$projectRoot = Resolve-ProjectRoot -Path $Root
$configPath = Join-Path $PSScriptRoot 'AuditFake.json'
$config = Read-AuditConfig -ConfigPath $configPath
$version = Read-ProjectVersion -ProjectRoot $projectRoot -Config $config

if ($Top -le 0) {
    $Top = [int]$config.console.top
}

$reportFolder = if ([string]::IsNullOrWhiteSpace($ReportDirectory)) { [string]$config.report.directory } else { $ReportDirectory }
if (-not [System.IO.Path]::IsPathRooted($reportFolder)) {
    $reportFolder = Join-Path $projectRoot $reportFolder
}
$reportPath = Join-Path $reportFolder ([string]$config.report.prefix + $version + '.md')

[string[]]$testFiles = @(Get-TrackedFiles -ProjectRoot $projectRoot -Config $config -Roots $config.sources.tests -Extensions $config.sources.extensions)
[string[]]$markupFiles = @(Get-TrackedFiles -ProjectRoot $projectRoot -Config $config -Roots $config.sources.roots -Extensions $config.sources.markup)
if ($testFiles.Length -eq 0) {
    throw "No tracked test file was enumerated, so the audit would pass vacuously: $projectRoot"
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    throw 'The .NET SDK is required, but dotnet was not found on PATH.'
}

$previousNoLogo = $env:DOTNET_NOLOGO
$previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$temporaryFolder = Join-Path ([System.IO.Path]::GetTempPath()) ($config.project + '-AuditFake-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($temporaryFolder) | Out-Null

try {
    $binaryPath = Get-HelperBinary -DotnetPath $dotnet.Source -ProjectName ([string]$config.project) -TargetFramework ([string]$config.helper.framework)
    $manifestPath = Join-Path $temporaryFolder 'sources.txt'
    $pageDataPath = Join-Path $temporaryFolder 'page.json'
    $manifest = @($testFiles | ForEach-Object { "T|$_" }) +
        @($markupFiles | ForEach-Object { "M|$_" })
    [System.IO.File]::WriteAllLines($manifestPath, [string[]]$manifest, [System.Text.UTF8Encoding]::new($false))

    $arguments = @(
        $binaryPath,
        $configPath,
        $projectRoot,
        $manifestPath,
        $version,
        $reportPath,
        $Top,
        (Join-Path $PSScriptRoot 'AuditBinder.json'),
        $pageDataPath
    )

    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $auditOutput = & $dotnet.Source @arguments 2>&1
    $ErrorActionPreference = $nativePreference
    $auditExitCode = $LASTEXITCODE

    if ($auditExitCode -ne 0 -and $auditExitCode -ne 3) {
        throw "The fake audit failed.`n$($auditOutput -join [Environment]::NewLine)"
    }

    $relayLines = @($auditOutput | ForEach-Object { [string]$_ })
    $headerRows = @{}
    for ($index = 1; $index -lt $relayLines.Count; $index++) {
        $rule = $relayLines[$index]
        $above = $relayLines[$index - 1]
        if ($rule -notmatch '^-+(  -+)*$' -or $above -eq '' -or $above -match '^-+(  -+)*$') { continue }
        if ($rule -match '^-+$' -and $above.Length -eq $rule.Length) { continue }
        $headerRows[$index - 1] = $true
        if ($index -ge 2) {
            $crown = $relayLines[$index - 2]
            $prior = if ($index -ge 3) { $relayLines[$index - 3] } else { '' }
            if ($crown -ne '' -and $crown -notmatch '^-+(  -+)*$' -and $prior -eq '') { $headerRows[$index - 2] = $true }
        }
    }
    $inResult = $false
    for ($index = 0; $index -lt $relayLines.Count; $index++) {
        $text = $relayLines[$index]
        $next = if ($index + 1 -lt $relayLines.Count) { $relayLines[$index + 1] } else { '' }
        if ($text.StartsWith('Scanned: ', [System.StringComparison]::Ordinal)) {
            Write-AuditLine $text -ForegroundColor DarkGray
        }
        elseif ($text -ne '' -and $next -match '^-+$' -and $next.Length -eq $text.Length) {
            $inResult = $text -eq 'Result'
            Write-AuditLine $text -ForegroundColor Blue
        }
        elseif ($text -match '^-+(  -+)*$') {
            Write-AuditLine $text -ForegroundColor $(if ($headerRows.ContainsKey($index - 1)) { 'Cyan' } else { 'DarkGray' })
        }
        elseif ($headerRows.ContainsKey($index)) {
            Write-AuditLine $text -ForegroundColor Cyan
        }
        elseif ($inResult -and $text -match '^(OK|FAIL) ') {
            Write-AuditLine $text -Lead $Matches[1] -LeadColor $(if ($Matches[1] -eq 'OK') { 'Green' } else { 'Red' })
        }
        elseif ($text.StartsWith('PASS: ', [System.StringComparison]::Ordinal)) {
            Write-AuditLine $text -ForegroundColor Green
        }
        elseif ($text.StartsWith('FAIL: ', [System.StringComparison]::Ordinal)) {
            Write-AuditLine $text -ForegroundColor Red
        }
        else {
            if ($text -eq '') { $inResult = $false }
            Write-AuditLine $text
        }
    }

    # Page output: the helper's page data inside the template AuditFake.html.
    # The helper writes the JSON, so Windows PowerShell 5.1 and pwsh 7 write the same bytes.
    $pageTemplatePath = Join-Path $PSScriptRoot 'AuditFake.html'
    $pagePath = Join-Path $reportFolder ([string]$config.report.prefix + $version + '.html')
    [System.IO.Directory]::CreateDirectory($reportFolder) | Out-Null
    $utf8 = [System.Text.UTF8Encoding]::new($false)
    if (-not (Test-Path -LiteralPath $pageDataPath -PathType Leaf)) {
        throw "The fake audit wrote no page data: $pageDataPath"
    }
    $pageData = [System.IO.File]::ReadAllText($pageDataPath, $utf8)
    $pageTemplate = [System.IO.File]::ReadAllText($pageTemplatePath, $utf8)
    foreach ($marker in @('/*__DATA__*/', '__TITLE__')) {
        if (-not $pageTemplate.Contains($marker)) { throw "The page template lacks the marker $marker : $pageTemplatePath" }
    }
    $pageText = $pageTemplate.Replace('__TITLE__', [System.Net.WebUtility]::HtmlEncode([string]$config.project)).Replace('/*__DATA__*/', $pageData.Trim())
    [System.IO.File]::WriteAllText($pagePath, ($pageText -replace "`r`n", "`n"), $utf8)
    Write-AuditLine "Report: $pagePath"
}
finally {
    $env:DOTNET_NOLOGO = $previousNoLogo
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = $previousTelemetry
    if (Test-Path -LiteralPath $temporaryFolder) {
        Remove-Item -LiteralPath $temporaryFolder -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if ($Open -and (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    Invoke-Item -LiteralPath $reportPath
}

if (-not $NoOpen) {
    Start-Process -FilePath $pagePath
}

exit ([int]($auditExitCode -ne 0))
