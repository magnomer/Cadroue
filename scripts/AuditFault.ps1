<#
.SYNOPSIS
Finds catch clauses below Conduct that swallow a fault, so it never reaches the user.

.DESCRIPTION
A fault caught in a ring below Conduct and only recorded, or turned into null, never reaches
anyone who can act on it. This audit binds the sources with Roslyn and judges every catch clause
in the configured rings.

  - A clause hands its fault out when its block, nested lambdas and local functions left out:
      H1  holds a throw statement or a throw expression;
      H2  carries the catch variable out;
      H3  invokes a delegate or an event.
  - Carrying means the value is the variable, a cast or `as` of it, an arm of a conditional,
    or a creation, tuple, collection, array or `with` holding it as an argument or initializer.
    The ways out are an assignment to an out or ref parameter, a field or a property, a return,
    an argument to a delegate or event invocation, and an argument to an in-source method of
    the rings whose matching parameter is itself carried out, followed to a fixed point.
  - A call through an interface or into another ring, assigning a local, or returning a
    constant, null, false, an empty collection or a new value not carrying the variable is
    not handing out.
  - A catch of OperationCanceledException, or of a type derived from it, is exempt by shape.
  - Every other clause is a Swallowing hit, written as
      path:line EnclosingType.EnclosingMethod catch (ExceptionType) swallows the fault

These are the rules of the convention test TAuditFault. Exempt rows name path:EnclosingMethod;
an exempt hit counts against no ceiling, and a row that matches no hit fails.

Each ring has a ceiling in ceilings. A ring counting above its ceiling fails. A ceiling above its
count is stale and always fails, so a shed hit is locked in. The console prints the counters, the
rings and the first rows of each ring; a Markdown report with every hit is written to
{report.directory}\{prefix}{version}.md. A page with the same result is written beside it as
{report.directory}\{prefix}{version}.html, filled from the template AuditFault.html. The page
opens in the default browser unless -NoOpen is given.

Binding goes through the shared binder of AuditBinder.cs and AuditBinder.json: the tracked
sources, the generated code of every project and the host build output, with no compile error
allowed. Build the solution first so the generated markup classes and the package assemblies exist.
The helper targets helper.framework and is compiled once per text and SDK into the temp folder.

Everything project-specific lives in AuditFault.json next to this script. No project source is
modified. Git and the .NET SDK are required.

.PARAMETER Root
Project root to audit. Defaults to the directory containing this script's parent.

.PARAMETER ReportDirectory
Overrides report.directory for this run, for the Markdown report and the page. Relative paths
resolve against the project root.

.PARAMETER Top
Overrides console.top: how many rows of each ring the console shows.

.PARAMETER Open
Open the Markdown report after the audit finishes.

.PARAMETER NoOpen
Write the page without opening it.

.PARAMETER NoPause
Do not stop at each console page. Paging is also off when output or input is redirected.

.PARAMETER Help
Display this help and exit. The alias -? is supported.

.EXAMPLE
AuditFault
Audit the current checkout.

.EXAMPLE
AuditFault -ReportDirectory D:\temp\audit -Top 10 -Open -NoOpen
Write the reports elsewhere, show ten rows of each ring, open the Markdown report but not the page.
#>
#requires -Version 5.1
# AUDITFAULT - AUDIT GENERATION 21.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the audited outcome changes.
# A generation names the set of checks the audit applies. Two projects on the same generation audit
# the same things and their reports compare directly, whatever else differs between the files. A check
# added, removed, or changed in what it reports is a new generation; wording, plumbing, and refactoring
# leave it alone. Every audit script shares one generation number with the convention-test settings,
# and each refuses a configuration written at another generation.
# Generation 21: the first generation of this audit. It binds the sources with Roslyn and reports
# every catch clause below Conduct that hands its fault to no one as Swallowing, per ring.
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
    AuditFault.ps1

SYNOPSIS
    Find catch clauses below Conduct that swallow a fault.

SYNTAX
    AuditFault [-Root <path>] [-ReportDirectory <path>] [-Top <n>] [-Open] [-NoOpen] [-NoPause] [-Help]

OPTIONS
    -Root <path>             Project root. Defaults to the parent of the scripts folder.
    -ReportDirectory <path>  Overrides report.directory for the report and the page.
    -Top <n>                 Overrides console.top.
    -Open                    Open the Markdown report when done.
    -NoOpen                  Write the page without opening it.
    -NoPause                 No console paging.
    -Help                    Show this help.

KIND
    Swallowing  a catch clause that neither throws, nor carries its variable out,
                nor invokes a delegate or an event.

COUNTERS
    Above ceiling      rings counting above their ceiling.
    Stale ceilings     rings counting below their ceiling.
    Stale exempt rows  exempt rows that match no hit.

REPORTS
    {report.directory}\{prefix}{version}.md    every hit, ring by ring.
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

Write-AuditLine "AUDITFAULT - AUDIT GENERATION $script:AuditGeneration" -ForegroundColor Blue
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
        'generation', 'project', 'rings', 'ceilings', 'exempt', 'helper.framework',
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
        throw "The fault-audit configuration is generation $($config.generation) but this tooling is generation $script:AuditGeneration.`nA generation names the set of checks applied, so the two must match: $ConfigPath"
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

if (args.Length != 7)
{
    Console.Error.WriteLine("usage: <config> <root> <version> <report> <top> <binder> <page>");
    return 2;
}

string configPath = args[0];
string projectRoot = args[1];
string version = args[2];
string reportPath = args[3];
int top = int.Parse(args[4], CultureInfo.InvariantCulture);
string binderPath = args[5];
string pagePath = args[6];

string[] Strings(JsonElement list) => list.EnumerateArray().Select(item => item.GetString()!).ToArray();
JsonElement config = JsonDocument.Parse(File.ReadAllText(configPath)).RootElement;
string[] rings = Strings(config.GetProperty("rings"));
string[] exempt = Strings(config.GetProperty("exempt"));
List<(string Ring, int Ceiling)> ceilings = config.GetProperty("ceilings").EnumerateObject()
    .Select(item => (item.Name, item.Value.GetInt32()))
    .ToList();
string sourceRoot = JsonDocument.Parse(File.ReadAllText(binderPath)).RootElement.GetProperty("source").GetString()!;

LAuditBinder binder = LAuditBinder.LAuditBinderRead(projectRoot, binderPath);
CSharpCompilation source = binder.LAuditCompilation;
string? RingOf(string relative) => rings.FirstOrDefault(ring =>
    relative.StartsWith(sourceRoot.TrimEnd('/') + "/" + ring + "/", StringComparison.OrdinalIgnoreCase));

List<SemanticModel> models = binder.LAuditTrees
    .Where(tree => RingOf(binder.LAuditRelativeRead(tree.FilePath)) is not null)
    .Select(tree => source.GetSemanticModel(tree, true))
    .ToList();
IReadOnlySet<ISymbol> carried = Fault.CarriedRead(models);
List<CatchClauseSyntax> clauses = [];
List<Hit> hits = [];
foreach (SemanticModel model in models)
{
    foreach (CatchClauseSyntax clause in model.SyntaxTree.GetRoot().DescendantNodes().OfType<CatchClauseSyntax>())
    {
        clauses.Add(clause);
        if (Fault.Read(model, clause, carried) is { } found)
        {
            string path = binder.LAuditRelativeRead(model.SyntaxTree.FilePath);
            string row = $"{path}:{found.Method}";
            hits.Add(new Hit(
                path,
                clause.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                RingOf(path)!,
                found.Method,
                found.Text,
                exempt.Contains(row, StringComparer.Ordinal) ? row : ""));
        }
    }
}

hits = hits.OrderBy(hit => hit.Path, StringComparer.Ordinal).ThenBy(hit => hit.Line).ToList();
List<Hit> held = hits.Where(hit => hit.Exempt.Length == 0).ToList();
List<Hit> excused = hits.Where(hit => hit.Exempt.Length > 0).ToList();
int Tally(string ring) => held.Count(hit => hit.Ring == ring);
List<string> above = rings
    .Where(ring => Tally(ring) > ceilings.Where(pair => pair.Ring == ring).Select(pair => pair.Ceiling).FirstOrDefault())
    .Select(ring => $"{ring}: {Tally(ring)} hit(s), ceiling {ceilings.Where(pair => pair.Ring == ring).Select(pair => pair.Ceiling).FirstOrDefault()}")
    .ToList();
List<string> stale = ceilings
    .Where(pair => Tally(pair.Ring) < pair.Ceiling)
    .Select(pair => $"{pair.Ring}: {Tally(pair.Ring)} hit(s), ceiling {pair.Ceiling}")
    .ToList();
HashSet<string> used = excused.Select(hit => hit.Exempt).ToHashSet(StringComparer.Ordinal);
List<string> unused = exempt.Where(row => !used.Contains(row)).ToList();

Console.WriteLine(
    $"Scanned: {models.Count:N0} ring files of {binder.LAuditTrees.Count:N0} source files, {clauses.Count:N0} catch clauses");
List<(string Gate, int Count, string Meaning)> gates =
[
    ("Above ceiling", above.Count, "rings whose swallowing catches exceed their ceiling"),
    ("Stale ceilings", stale.Count, "ceilings set above their current hits"),
    ("Stale exempt rows", unused.Count, "exempt rows that match no swallowing catch"),
];
Fault.WriteResult(gates);

Fault.WriteHeading("Catches by ring");
List<string[]> ringRows = ceilings
    .Select(pair => new[] { pair.Ring, Tally(pair.Ring).ToString("N0"), pair.Ceiling.ToString("N0") })
    .ToList();
ringRows.Add(["Exempt", excused.Count.ToString("N0"), "-"]);
ringRows.Add(["Total", hits.Count.ToString("N0"), "-"]);
Fault.WriteTable(Fault.TextTable(["Ring", "Swallowing", "Ceiling"], ringRows));

string Line(Hit hit) => $"{hit.Path}:{hit.Line} {hit.Text}";
List<(string Title, List<string> Rows)> sections = rings
    .Select(ring => (ring, held.Where(hit => hit.Ring == ring).Select(Line).ToList()))
    .Append(("Exempt", excused.Select(Line).ToList()))
    .Append(("Above ceiling", above))
    .Append(("Stale ceilings", stale))
    .Append(("Stale exempt rows", unused))
    .ToList();
foreach ((string title, List<string> rows) in sections.Where(section => section.Rows.Count > 0))
{
    Fault.WriteHeading($"{title} ({rows.Count:N0})");
    rows.Take(top).ToList().ForEach(Console.WriteLine);
    if (rows.Count > top)
    {
        Console.WriteLine($"... and {rows.Count - top:N0} more in the report.");
    }
}

Console.WriteLine();
Console.WriteLine($"Report: {reportPath}");

int generation = config.GetProperty("generation").GetInt32();
List<string> lines =
[
    $"# Fault audit {version}",
    string.Empty,
    $"- Generation: {generation}",
];
lines.AddRange(ceilings.Select(pair => $"- {pair.Ring}: {Tally(pair.Ring)}, ceiling {pair.Ceiling}"));
lines.Add($"- Exempt: {excused.Count}");
lines.Add($"- Above ceiling: {above.Count}");
lines.Add($"- Stale ceilings: {stale.Count}");
lines.Add($"- Stale exempt rows: {unused.Count}");
lines.Add(string.Empty);
lines.Add("A catch clause below Conduct swallows its fault unless its block throws, carries the caught "
    + "variable out, or invokes a delegate or an event. A catch of a cancellation is exempt by shape.");
List<(string Title, List<string> Rows)> chapters = rings
    .Select(ring => (ring, held.Where(hit => hit.Ring == ring).Select(hit => $"- `{hit.Path}:{hit.Line}` {hit.Text}").ToList()))
    .Append(("Exempt", excused.Select(hit => $"- `{hit.Path}:{hit.Line}` {hit.Text}").ToList()))
    .Append(("Above ceiling", above.Select(row => "- " + row).ToList()))
    .Append(("Stale ceilings", stale.Select(row => "- " + row).ToList()))
    .Append(("Stale exempt rows", unused.Select(row => "- " + row).ToList()))
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

var page = new
{
    project = config.GetProperty("project").GetString(),
    version,
    generation,
    generated = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
    report = Path.GetFileName(reportPath),
    scanned = new
    {
        sources = binder.LAuditTrees.Count,
        rings = models.Count,
        catches = clauses.Count,
    },
    gates = gates.Select(row => new { status = row.Count > 0 ? "FAIL" : "OK", gate = row.Gate, count = row.Count, meaning = row.Meaning }),
    ceilings = ceilings.Select(pair => new
    {
        ring = pair.Ring,
        count = Tally(pair.Ring),
        ceiling = pair.Ceiling,
        state = Tally(pair.Ring) < pair.Ceiling ? "STALE" : Tally(pair.Ring) <= pair.Ceiling ? "OK" : "ABOVE",
    }),
    above,
    stale,
    unused,
    hits = hits.Select(hit => new { ring = hit.Ring, name = hit.Method, path = hit.Path, line = hit.Line, reason = hit.Text, exempt = hit.Exempt.Length > 0 }),
    scope = new
    {
        source = sourceRoot,
        rings,
        exempt,
        framework = config.GetProperty("helper").GetProperty("framework").GetString(),
        top,
    },
};
File.WriteAllText(pagePath, JsonSerializer.Serialize(page), new UTF8Encoding(false));
return above.Count + stale.Count + unused.Count > 0 ? 3 : 0;

internal static class Fault
{
    private const string CancelName = "System.OperationCanceledException";

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

    public static (string Method, string Text)? Read(SemanticModel model, CatchClauseSyntax clause, IReadOnlySet<ISymbol> carried)
    {
        ITypeSymbol? caught = clause.Declaration is { } declaration ? model.GetTypeInfo(declaration.Type).Type : null;
        for (ITypeSymbol? kind = caught; kind is not null; kind = kind.BaseType)
        {
            if (string.Equals(kind.ToDisplayString(), CancelName, StringComparison.Ordinal))
            {
                return null;
            }
        }

        foreach (SyntaxNode node in Nodes(clause.Block))
        {
            if (node is ThrowStatementSyntax or ThrowExpressionSyntax
                || node is InvocationExpressionSyntax invocation
                && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { MethodKind: MethodKind.DelegateInvoke })
            {
                return null;
            }
        }

        if (clause.Declaration is { } named
            && model.GetDeclaredSymbol(named) is ILocalSymbol variable
            && Carries(model, clause.Block, variable, carried))
        {
            return null;
        }

        ISymbol? owner = model.GetEnclosingSymbol(clause.SpanStart);
        while (owner is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction })
        {
            owner = owner.ContainingSymbol;
        }

        string method = owner switch
        {
            IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor } built =>
                built.ContainingType.Name,
            IMethodSymbol { AssociatedSymbol: { } associated } => associated.Name,
            { } symbol => symbol.Name,
            null => "?",
        };
        string type = owner?.ContainingType?.Name ?? "?";
        string shown = clause.Declaration?.Type.ToString() ?? "Exception";
        return (method, $"{type}.{method} catch ({shown}) swallows the fault");
    }

    public static IReadOnlySet<ISymbol> CarriedRead(IReadOnlyList<SemanticModel> models)
    {
        List<(SemanticModel TAuditCarriedModel,
            IMethodSymbol TAuditCarriedMethod,
            IMethodSymbol TAuditCarriedKey,
            SyntaxNode TAuditCarriedBody)> bodies = [];
        foreach (SemanticModel model in models)
        {
            foreach (SyntaxNode declaration in model.SyntaxTree.GetRoot().DescendantNodes()
                         .Where(node => node is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax))
            {
                SyntaxNode? body = declaration switch
                {
                    BaseMethodDeclarationSyntax member => (SyntaxNode?)member.Body ?? member.ExpressionBody,
                    LocalFunctionStatementSyntax local => (SyntaxNode?)local.Body ?? local.ExpressionBody,
                    _ => null,
                };
                if (body is not null
                    && model.GetDeclaredSymbol(declaration) is IMethodSymbol { Parameters.Length: > 0 } method)
                {
                    bodies.Add((model, method, method.PartialDefinitionPart ?? method, body));
                }
            }
        }

        HashSet<ISymbol> carried = new(SymbolEqualityComparer.Default);
        bool grown = true;
        while (grown)
        {
            grown = false;
            foreach ((SemanticModel model, IMethodSymbol method, IMethodSymbol key, SyntaxNode body) in bodies)
            {
                for (int index = 0; index < method.Parameters.Length; index++)
                {
                    IParameterSymbol parameter = method.Parameters[index];
                    IParameterSymbol keyed = key.Parameters[index].OriginalDefinition;
                    if (carried.Contains(keyed))
                    {
                        continue;
                    }

                    bool returned = body is ArrowExpressionClauseSyntax arrow
                        && !method.ReturnsVoid
                        && Holds(model, arrow.Expression, parameter);
                    if (returned || Carries(model, body, parameter, carried))
                    {
                        carried.Add(keyed);
                        grown = true;
                    }
                }
            }
        }

        return carried;
    }

    private static bool Carries(SemanticModel model, SyntaxNode body, ISymbol variable, IReadOnlySet<ISymbol> carried)
    {
        foreach (SyntaxNode node in Nodes(body))
        {
            switch (node)
            {
                case AssignmentExpressionSyntax assignment when Holds(model, assignment.Right, variable):
                    if (model.GetSymbolInfo(assignment.Left).Symbol
                        is IFieldSymbol or IPropertySymbol or IParameterSymbol { RefKind: RefKind.Out or RefKind.Ref })
                    {
                        return true;
                    }

                    break;
                case ReturnStatementSyntax { Expression: { } returned } when Holds(model, returned, variable):
                    return true;
                case InvocationExpressionSyntax invocation
                    when invocation.ArgumentList.Arguments.Any(argument => Holds(model, argument.Expression, variable)):
                    if (model.GetOperation(invocation) is not IInvocationOperation call)
                    {
                        break;
                    }

                    foreach (IArgumentOperation argument in call.Arguments)
                    {
                        if (argument.Syntax is ArgumentSyntax passed
                            && Holds(model, passed.Expression, variable)
                            && (call.TargetMethod.MethodKind == MethodKind.DelegateInvoke
                                || argument.Parameter is { } parameter && carried.Contains(parameter.OriginalDefinition)))
                        {
                            return true;
                        }
                    }

                    break;
            }
        }

        return false;
    }

    private static bool Holds(SemanticModel model, ExpressionSyntax? expression, ISymbol variable)
    {
        bool TAuditInnerCheck(ExpressionSyntax? inner) => Holds(model, inner, variable);

        return expression switch
        {
            null => false,
            IdentifierNameSyntax name => string.Equals(name.Identifier.ValueText, variable.Name, StringComparison.Ordinal)
                && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(name).Symbol, variable),
            ParenthesizedExpressionSyntax parenthesized => TAuditInnerCheck(parenthesized.Expression),
            PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppressed =>
                TAuditInnerCheck(suppressed.Operand),
            CastExpressionSyntax cast => TAuditInnerCheck(cast.Expression),
            BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression } binary => TAuditInnerCheck(binary.Left),
            ConditionalExpressionSyntax conditional =>
                TAuditInnerCheck(conditional.WhenTrue) || TAuditInnerCheck(conditional.WhenFalse),
            BaseObjectCreationExpressionSyntax creation =>
                (creation.ArgumentList?.Arguments.Any(argument => TAuditInnerCheck(argument.Expression)) ?? false)
                || TAuditInnerCheck(creation.Initializer),
            AnonymousObjectCreationExpressionSyntax anonymous =>
                anonymous.Initializers.Any(member => TAuditInnerCheck(member.Expression)),
            TupleExpressionSyntax tuple => tuple.Arguments.Any(argument => TAuditInnerCheck(argument.Expression)),
            CollectionExpressionSyntax collection => collection.Elements.Any(element => element switch
            {
                ExpressionElementSyntax item => TAuditInnerCheck(item.Expression),
                SpreadElementSyntax spread => TAuditInnerCheck(spread.Expression),
                _ => false,
            }),
            ArrayCreationExpressionSyntax array => TAuditInnerCheck(array.Initializer),
            ImplicitArrayCreationExpressionSyntax array => TAuditInnerCheck(array.Initializer),
            InitializerExpressionSyntax initializer => initializer.Expressions.Any(TAuditInnerCheck),
            AssignmentExpressionSyntax assignment => TAuditInnerCheck(assignment.Right),
            WithExpressionSyntax with => TAuditInnerCheck(with.Expression) || TAuditInnerCheck(with.Initializer),
            _ => false,
        };
    }

    private static IEnumerable<SyntaxNode> Nodes(SyntaxNode body)
    {
        return body.DescendantNodes(node =>
            node == body || node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax));
    }
}

internal sealed record Hit(string Path, int Line, string Ring, string Method, string Text, string Exempt);
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
    $cacheParent = Join-Path ([System.IO.Path]::GetTempPath()) ($ProjectName + '-AuditFault')
    $cacheFolder = Join-Path $cacheParent $hash
    $binaryPath = Join-Path (Join-Path $cacheFolder 'bin') 'AuditFault.Helper.dll'
    if (Test-Path -LiteralPath $binaryPath -PathType Leaf) {
        return $binaryPath
    }

    Write-AuditLine 'Compiling the fault binder once for this SDK...'
    if (Test-Path -LiteralPath $cacheParent) {
        Get-ChildItem -LiteralPath $cacheParent -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }

    $helperFolder = Join-Path $cacheFolder 'helper'
    [System.IO.Directory]::CreateDirectory($helperFolder) | Out-Null
    $encoding = New-Object System.Text.UTF8Encoding($false)
    $projectPath = Join-Path $helperFolder 'AuditFault.Helper.csproj'
    [System.IO.File]::WriteAllText($projectPath, $script:HelperProject.Replace('{TARGET_FRAMEWORK}', $TargetFramework), $encoding)
    [System.IO.File]::WriteAllText((Join-Path $helperFolder 'Program.cs'), $script:HelperProgram, $encoding)
    [System.IO.File]::WriteAllText((Join-Path $helperFolder 'AuditBinder.cs'), $binderSource, $encoding)
    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $buildOutput = & $DotnetPath build $projectPath --configuration Release --nologo --verbosity quiet --output (Join-Path $cacheFolder 'bin') 2>&1
    $ErrorActionPreference = $nativePreference
    if ($LASTEXITCODE -ne 0) {
        Remove-Item -LiteralPath $cacheFolder -Recurse -Force -ErrorAction SilentlyContinue
        throw "The fault binder could not be built.`n$($buildOutput -join [Environment]::NewLine)"
    }

    return $binaryPath
}

$projectRoot = Resolve-ProjectRoot -Path $Root
$configPath = Join-Path $PSScriptRoot 'AuditFault.json'
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

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    throw 'The .NET SDK is required, but dotnet was not found on PATH.'
}

$previousNoLogo = $env:DOTNET_NOLOGO
$previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$temporaryFolder = Join-Path ([System.IO.Path]::GetTempPath()) ($config.project + '-AuditFault-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($temporaryFolder) | Out-Null

try {
    $binaryPath = Get-HelperBinary -DotnetPath $dotnet.Source -ProjectName ([string]$config.project) -TargetFramework ([string]$config.helper.framework)
    $pageDataPath = Join-Path $temporaryFolder 'page.json'

    $arguments = @(
        $binaryPath,
        $configPath,
        $projectRoot,
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
        throw "The fault audit failed.`n$($auditOutput -join [Environment]::NewLine)"
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

    # Page output: the helper's page data inside the template AuditFault.html.
    # The helper writes the JSON, so Windows PowerShell 5.1 and pwsh 7 write the same bytes.
    $pageTemplatePath = Join-Path $PSScriptRoot 'AuditFault.html'
    $pagePath = Join-Path $reportFolder ([string]$config.report.prefix + $version + '.html')
    [System.IO.Directory]::CreateDirectory($reportFolder) | Out-Null
    $utf8 = [System.Text.UTF8Encoding]::new($false)
    if (-not (Test-Path -LiteralPath $pageDataPath -PathType Leaf)) {
        throw "The fault audit wrote no page data: $pageDataPath"
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
