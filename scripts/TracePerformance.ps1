<#
.SYNOPSIS
    Time every method of the target project, Cadroue.Conduct by default, and rank them from the slowest to the fastest.
.DESCRIPTION
    Runs the test projects that reach the target project with a startup hook in every test process.
    The hook patches every method of the target assembly the moment the assembly loads, so no
    method list is kept anywhere: the assembly itself is the list. Each call is timed with a
    stopwatch, from entry to return, and for a Task-returning method until the Task completes.

    Per method the script reports calls, mean, max, total and self time. Self time is the time not
    spent in another timed method on the same thread. Methods the workload never called are listed
    as not reached. Methods the hook cannot patch are listed as skipped with the reason.

    The workload is every test project under the configured tests folder whose project references
    reach the target project. Nothing is gated: the script only measures and reports.

    Every project-specific value lives in TracePerformance.json. The hook source is held in
    this script and built into the hook folder under the output folder before each run.
    The report goes to the configured report folder as TracePerformance-<stamp>.md with every row.
    The raw per-process timings go to a stamped folder under the output folder, under probe.
.PARAMETER Project
    Test projects to run. Defaults to every test project that reaches the target project.
.PARAMETER Filter
    VSTest filter expression passed to dotnet test.
.PARAMETER Repeat
    dotnet test runs. Every run adds its calls, so the means rest on more calls.
.PARAMETER Top
    Rows each console list shows. The report file lists every row.
.PARAMETER Configuration
    Build configuration. Defaults to the configuration in TracePerformance.json.
.PARAMETER Help
    Display this help and exit. The alias -? is supported.
.EXAMPLE
    traceperformance
    Time every Conduct method over every test project that reaches Conduct.
.EXAMPLE
    traceperformance -Filter "FullyQualifiedName~TEditor" -Repeat 3
    Time the Conduct methods the editor tests reach, over three runs.
.EXAMPLE
    traceperformance -Top 100
    Show the hundred slowest methods on the console.
#>
# TRACEPERFORMANCE - TRACE GENERATION 1.
# ALIAS performance
[CmdletBinding()]
param(
    [string[]]$Project = @(),
    [string]$Filter = '',
    [int]$Repeat = 0,
    [int]$Top = 0,
    [string]$Configuration = '',
    [Alias('?')]
    [switch]$Help
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

# The startup hook. It lives here, not beside the script, so the name audit never reads tooling code.
$script:HookSource = @'
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using HarmonyLib;

internal static class StartupHook
{
    public static void Initialize()
    {
        string? target = Environment.GetEnvironmentVariable("PERFORMANCE_TARGET");
        string? output = Environment.GetEnvironmentVariable("PERFORMANCE_OUTPUT");
        if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(output))
        {
            return;
        }

        string folder = Path.GetDirectoryName(typeof(StartupHook).Assembly.Location) ?? "";
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            string path = Path.Combine(folder, name.Name + ".dll");
            return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
        };
        AppDomain.CurrentDomain.AssemblyLoad += (_, loaded) => Watch(loaded.LoadedAssembly, target, output);
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Watch(assembly, target, output);
        }
    }

    private static void Watch(Assembly assembly, string target, string output)
    {
        if (string.Equals(assembly.GetName().Name, target, StringComparison.OrdinalIgnoreCase))
        {
            Probe.Attach(assembly, output);
        }
    }
}

internal sealed class Entry(string type, string method, string signature)
{
    public readonly string Type = type;
    public readonly string Method = method;
    public readonly string Signature = signature;
    public long Calls;
    public long Inclusive;
    public long Self;
    public long Max;
    public long Pending;
}

internal struct Frame
{
    public long Start;
    public long Child;
}

internal static class Probe
{
    private static readonly object Gate = new();
    private static readonly List<Entry> Entries = [];
    private static readonly List<(string Type, string Method, string Reason)> Skipped = [];
    private static readonly Dictionary<MethodBase, int> Index = [];
    private static string _output = "";
    private static string _target = "";
    private static bool _attached;
    private static double _patch;

    [ThreadStatic]
    private static List<Frame>? _stack;

    public static void Attach(Assembly assembly, string output)
    {
        lock (Gate)
        {
            if (_attached)
            {
                return;
            }

            _attached = true;
        }

        _output = output;
        _target = assembly.GetName().Name ?? "";
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Save();
        Stopwatch watch = Stopwatch.StartNew();
        Harmony harmony = new("performance");
        const BindingFlags Flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        HarmonyMethod prefix = new(typeof(Probe).GetMethod(nameof(Open), BindingFlags.NonPublic | BindingFlags.Static));
        HarmonyMethod plain = new(typeof(Probe).GetMethod(nameof(Close), BindingFlags.NonPublic | BindingFlags.Static));
        HarmonyMethod awaited = new(typeof(Probe).GetMethod(nameof(CloseTask), BindingFlags.NonPublic | BindingFlags.Static));
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types.Where(type => type is not null).Cast<Type>().ToArray();
        }

        foreach (Type type in types.OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            if (Generated(type) || type.IsInterface || typeof(Delegate).IsAssignableFrom(type))
            {
                continue;
            }

            IEnumerable<MethodBase> members = type.GetConstructors(Flags).Cast<MethodBase>().Concat(type.GetMethods(Flags));
            foreach (MethodBase member in members)
            {
                if (member.IsAbstract || member.Name.Contains('<') || member.IsDefined(typeof(CompilerGeneratedAttribute), false))
                {
                    continue;
                }

                string name = Display(type);
                string reason = member.GetMethodBody() is null ? "no body"
                    : type.ContainsGenericParameters ? "generic type"
                    : member.IsGenericMethodDefinition ? "generic method"
                    : "";
                if (reason.Length > 0)
                {
                    Skipped.Add((name, Signature(member), reason));
                    continue;
                }

                bool task = member is MethodInfo info && typeof(Task).IsAssignableFrom(info.ReturnType);
                int index = Entries.Count;
                Entries.Add(new Entry(name, Name(member), Signature(member)));
                Index[member] = index;
                try
                {
                    harmony.Patch(member, prefix: prefix, finalizer: task ? awaited : plain);
                }
                catch (Exception exception)
                {
                    Entries.RemoveAt(index);
                    Index.Remove(member);
                    Skipped.Add((name, Signature(member), "patch failed: " + exception.GetType().Name));
                }
            }
        }

        _patch = watch.Elapsed.TotalMilliseconds;
    }

    private static bool Generated(Type type)
    {
        for (Type? current = type; current is not null; current = current.DeclaringType)
        {
            if (current.Name.Contains('<') || current.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                return true;
            }
        }

        return false;
    }

    private static string Display(Type type)
    {
        string name = type.Name;
        int tick = name.IndexOf('`');
        if (tick >= 0)
        {
            name = name[..tick];
        }

        return type.DeclaringType is null ? name : Display(type.DeclaringType) + "." + name;
    }

    private static string Name(MethodBase member) => member.IsConstructor ? (member.IsStatic ? ".cctor" : ".ctor") : member.Name;

    private static string Signature(MethodBase member) =>
        Name(member) + "(" + string.Join(", ", member.GetParameters().Select(parameter => Parameter(parameter.ParameterType))) + ")";

    private static string Parameter(Type type)
    {
        if (type.IsByRef)
        {
            return "ref " + Parameter(type.GetElementType()!);
        }

        if (type.IsArray)
        {
            return Parameter(type.GetElementType()!) + "[]";
        }

        if (type.IsGenericType)
        {
            return Display(type) + "<" + string.Join(", ", type.GetGenericArguments().Select(Parameter)) + ">";
        }

        return Display(type);
    }

    private static void Open()
    {
        List<Frame> stack = _stack ??= [];
        stack.Add(new Frame { Start = Stopwatch.GetTimestamp() });
    }

    private static void Close(MethodBase __originalMethod)
    {
        Finish(__originalMethod, null);
    }

    private static void CloseTask(MethodBase __originalMethod, object? __result)
    {
        Finish(__originalMethod, __result as Task);
    }

    private static void Finish(MethodBase original, Task? task)
    {
        long now = Stopwatch.GetTimestamp();
        List<Frame>? stack = _stack;
        if (stack is null || stack.Count == 0 || !Index.TryGetValue(original, out int index))
        {
            return;
        }

        Frame frame = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        long elapsed = now - frame.Start;
        if (stack.Count > 0)
        {
            Frame parent = stack[^1];
            parent.Child += elapsed;
            stack[^1] = parent;
        }

        Entry entry = Entries[index];
        Interlocked.Add(ref entry.Self, elapsed - frame.Child);
        if (task is null || task.IsCompleted)
        {
            Count(entry, elapsed);
            return;
        }

        Interlocked.Increment(ref entry.Pending);
        long start = frame.Start;
        task.ContinueWith(
            _ =>
            {
                Interlocked.Decrement(ref entry.Pending);
                Count(entry, Stopwatch.GetTimestamp() - start);
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static void Count(Entry entry, long elapsed)
    {
        Interlocked.Increment(ref entry.Calls);
        Interlocked.Add(ref entry.Inclusive, elapsed);
        long seen = Volatile.Read(ref entry.Max);
        while (elapsed > seen)
        {
            long previous = Interlocked.CompareExchange(ref entry.Max, elapsed, seen);
            if (previous == seen)
            {
                break;
            }

            seen = previous;
        }
    }

    private static void Save()
    {
        Directory.CreateDirectory(_output);
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("target", _target);
            writer.WriteNumber("process", Environment.ProcessId);
            writer.WriteNumber("frequency", Stopwatch.Frequency);
            writer.WriteNumber("patchMs", Math.Round(_patch, 3));
            writer.WriteStartArray("methods");
            foreach (Entry entry in Entries)
            {
                writer.WriteStartObject();
                writer.WriteString("type", entry.Type);
                writer.WriteString("method", entry.Method);
                writer.WriteString("signature", entry.Signature);
                writer.WriteNumber("calls", Interlocked.Read(ref entry.Calls));
                writer.WriteNumber("inclusive", Interlocked.Read(ref entry.Inclusive));
                writer.WriteNumber("self", Interlocked.Read(ref entry.Self));
                writer.WriteNumber("max", Interlocked.Read(ref entry.Max));
                writer.WriteNumber("pending", Interlocked.Read(ref entry.Pending));
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteStartArray("skipped");
            foreach ((string type, string signature, string reason) in Skipped)
            {
                writer.WriteStartObject();
                writer.WriteString("type", type);
                writer.WriteString("signature", signature);
                writer.WriteString("reason", reason);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        string path = Path.Combine(_output, $"probe_{Environment.ProcessId}.json");
        File.WriteAllText(path, Encoding.UTF8.GetString(stream.ToArray()).Replace("\r\n", "\n") + "\n", new UTF8Encoding(false));
    }
}
'@

$script:HookProject = @'
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>%FRAMEWORK%</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>PerformanceHook</AssemblyName>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Lib.Harmony" Version="%HARMONY%" />
  </ItemGroup>

</Project>
'@

if ($Help) {
    Get-Help -Name $PSCommandPath -Detailed
    return
}

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$settingsPath = Join-Path $PSScriptRoot 'TracePerformance.json'
$settings = [System.IO.File]::ReadAllText($settingsPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
$invariant = [System.Globalization.CultureInfo]::InvariantCulture

if ($Configuration -eq '') { $Configuration = [string]$settings.configuration }
if ($Repeat -le 0) { $Repeat = [int]$settings.repeat }
if ($Top -le 0) { $Top = [int]$settings.top }

function Get-OrdinalKey {
    # Sort-Object compares text by culture, which Windows PowerShell 5.1 and pwsh 7 order differently.
    # Uppercase hexadecimal UTF-16 code units compare alike under every culture, so this key sorts ordinally.
    param([string]$Text)
    return [System.BitConverter]::ToString([System.Text.Encoding]::BigEndianUnicode.GetBytes($Text)).Replace('-', '')
}

function Format-Integer {
    param([long]$Value)
    return $Value.ToString('N0', $invariant)
}

function Format-Milliseconds {
    param([double]$Value)
    return $Value.ToString('#,##0.000', $invariant)
}

function Format-Percent {
    param([double]$Value)
    return $Value.ToString('0.0', $invariant) + ' %'
}

function Write-SectionTitle {
    param([Parameter(Mandatory = $true)][string]$Text)

    Write-Host ''
    Write-Host $Text -ForegroundColor Blue
    Write-Host ('-' * $Text.Length) -ForegroundColor DarkGray
}

function New-Column {
    param(
        [string]$Group = '',
        [Parameter(Mandatory = $true)][string]$Name,
        [bool]$Right = $true,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][AllowEmptyString()][string[]]$Values
    )

    return [pscustomobject]@{ Group = $Group; Name = $Name; Right = $Right; Values = $Values; Width = 0 }
}

# Prints a table whose header has two rows when any column carries a group: group names above, column names below.
function Write-ConsoleTable {
    param([Parameter(Mandatory = $true)][object[]]$Columns)

    $gap = '  '
    foreach ($column in $Columns) {
        $width = $column.Name.Length
        foreach ($value in $column.Values) {
            if ($value.Length -gt $width) { $width = $value.Length }
        }
        $column.Width = $width
    }

    $spans = [System.Collections.Generic.List[object]]::new()
    $index = 0
    while ($index -lt $Columns.Count) {
        $last = $index
        if ($Columns[$index].Group -ne '') {
            while ($last + 1 -lt $Columns.Count -and $Columns[$last + 1].Group -eq $Columns[$index].Group) { $last++ }
        }
        $label = if ($Columns[$index].Group -ne '') { $Columns[$index].Group } else { $Columns[$index].Name }
        $spans.Add([pscustomobject]@{ First = $index; Last = $last; Label = $label })
        $index = $last + 1
    }

    foreach ($span in $spans) {
        $width = 0
        for ($i = $span.First; $i -le $span.Last; $i++) { $width += $Columns[$i].Width + $(if ($i -lt $span.Last) { $gap.Length } else { 0 }) }
        if ($span.Label.Length -gt $width) { $Columns[$span.Last].Width += $span.Label.Length - $width }
    }

    $upper = [System.Collections.Generic.List[string]]::new()
    foreach ($span in $spans) {
        $width = 0
        for ($i = $span.First; $i -le $span.Last; $i++) { $width += $Columns[$i].Width + $(if ($i -lt $span.Last) { $gap.Length } else { 0 }) }
        $upper.Add($span.Label.PadRight($width))
    }

    $grouped = @($Columns | Where-Object { $_.Group -ne '' }).Count -gt 0
    Write-Host (($upper -join $gap).TrimEnd()) -ForegroundColor Cyan
    if ($grouped) {
        $lower = foreach ($column in $Columns) {
            $label = if ($column.Group -ne '') { $column.Name } else { '' }
            if ($column.Right) { $label.PadLeft($column.Width) } else { $label.PadRight($column.Width) }
        }
        Write-Host ((@($lower) -join $gap).TrimEnd()) -ForegroundColor Cyan
    }
    Write-Host ((@($Columns | ForEach-Object { '-' * $_.Width }) -join $gap)) -ForegroundColor Cyan

    for ($row = 0; $row -lt $Columns[0].Values.Count; $row++) {
        $cells = foreach ($column in $Columns) {
            $value = $column.Values[$row]
            if ($column.Right) { $value.PadLeft($column.Width) } else { $value.PadRight($column.Width) }
        }
        Write-Host ((@($cells) -join $gap).TrimEnd())
    }
}

function Add-MarkdownTable {
    param(
        [Parameter(Mandatory = $true)][System.Text.StringBuilder]$Text,
        [Parameter(Mandatory = $true)][object[]]$Columns
    )

    $names = foreach ($column in $Columns) { if ($column.Group -ne '') { "$($column.Group) $($column.Name)" } else { $column.Name } }
    $rules = foreach ($column in $Columns) { if ($column.Right) { '---:' } else { '---' } }
    [void]$Text.Append('| ' + (@($names) -join ' | ') + " |`n")
    [void]$Text.Append('|' + (@($rules) -join '|') + "|`n")
    for ($row = 0; $row -lt $Columns[0].Values.Count; $row++) {
        $cells = foreach ($column in $Columns) { ([string]$column.Values[$row]).Replace('|', '\|') }
        [void]$Text.Append('| ' + (@($cells) -join ' | ') + " |`n")
    }
    [void]$Text.Append("`n")
}

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [switch]$Quiet
    )

    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        if ($Quiet) {
            $output = & $Name @Arguments 2>&1
            if ($LASTEXITCODE -ne 0) {
                $output | ForEach-Object { Write-Host $_ }
            }
        }
        else {
            & $Name @Arguments 2>&1 | ForEach-Object { Write-Host $_ }
        }

        return $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }
}

function Build-Project {
    param([Parameter(Mandatory = $true)][string]$Path)

    Write-Host "Building $([System.IO.Path]::GetFileNameWithoutExtension($Path)) ($Configuration)" -ForegroundColor DarkGray
    $code = Invoke-Native -Name 'dotnet' -Arguments @('build', $Path, '-c', $Configuration, '-nologo', '-v', 'q') -Quiet
    if ($code -ne 0) {
        throw "Build failed: $Path"
    }
}

function Save-Text {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text
    )

    $normal = $Text.Replace("`r`n", "`n")
    if (-not $normal.EndsWith("`n")) { $normal += "`n" }
    if ((Test-Path -LiteralPath $Path) -and [System.IO.File]::ReadAllText($Path) -eq $normal) {
        return
    }

    [System.IO.File]::WriteAllText($Path, $normal, [System.Text.UTF8Encoding]::new($false))
}

function Get-ProjectReferences {
    param([Parameter(Mandatory = $true)][string]$Path)

    $folder = Split-Path -Parent $Path
    $text = [System.IO.File]::ReadAllText($Path)
    foreach ($match in [regex]::Matches($text, '<ProjectReference\s+Include="([^"]+)"')) {
        [System.IO.Path]::GetFullPath((Join-Path $folder $match.Groups[1].Value.Replace('\', '/')))
    }
}

function Test-ProjectReach {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Target
    )

    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $queue = [System.Collections.Generic.Queue[string]]::new()
    $queue.Enqueue([System.IO.Path]::GetFullPath($Path))
    while ($queue.Count -gt 0) {
        $current = $queue.Dequeue()
        if (-not $seen.Add($current) -or -not (Test-Path -LiteralPath $current)) { continue }
        foreach ($reference in @(Get-ProjectReferences -Path $current)) {
            if ([string]::Equals($reference, $Target, [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
            $queue.Enqueue($reference)
        }
    }

    return $false
}

# --- Target and workload ---

$targetProject = [System.IO.Path]::GetFullPath((Join-Path $root ([string]$settings.target)))
if (-not (Test-Path -LiteralPath $targetProject)) {
    throw "The target project was not found: $targetProject"
}

$targetText = [System.IO.File]::ReadAllText($targetProject)
$targetName = [System.IO.Path]::GetFileNameWithoutExtension($targetProject)
$assemblyMatch = [regex]::Match($targetText, '<AssemblyName>([^<]+)</AssemblyName>')
if ($assemblyMatch.Success) { $targetName = $assemblyMatch.Groups[1].Value }
$frameworkMatch = [regex]::Match($targetText, '<TargetFramework>([^<]+)</TargetFramework>')
if (-not $frameworkMatch.Success) {
    throw "The target project names no single TargetFramework: $targetProject"
}

$onWindows = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT
$testsFolder = Join-Path $root ([string]$settings.tests)
$workload = [System.Collections.Generic.List[string]]::new()
if ($Project.Count -gt 0) {
    foreach ($name in $Project) {
        $path = Join-Path (Join-Path $testsFolder $name) "$name.csproj"
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Test project was not found: $path"
        }
        $workload.Add($path)
    }
}
else {
    $candidates = @(Get-ChildItem -LiteralPath $testsFolder -Directory | ForEach-Object { Join-Path $_.FullName "$($_.Name).csproj" } | Where-Object { Test-Path -LiteralPath $_ })
    foreach ($path in ($candidates | Sort-Object { Get-OrdinalKey $_ })) {
        if (-not (Test-ProjectReach -Path $path -Target $targetProject)) { continue }
        $framework = [regex]::Match([System.IO.File]::ReadAllText($path), '<TargetFramework>([^<]+)</TargetFramework>').Groups[1].Value
        if (-not $onWindows -and $framework -match '-windows') {
            Write-Host "Skipping $([System.IO.Path]::GetFileNameWithoutExtension($path)) on a host that is not Windows." -ForegroundColor Yellow
            continue
        }
        $workload.Add($path)
    }
}

if ($workload.Count -eq 0) {
    throw "No test project reaches $targetName."
}

$workloadNames = @($workload | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_) })

# --- Hook build ---

$outputRoot = Join-Path $root ([string]$settings.output)
$hookFolder = Join-Path $outputRoot 'hook'
$hookBinary = Join-Path $hookFolder 'bin'
[void][System.IO.Directory]::CreateDirectory($hookFolder)
Save-Text -Path (Join-Path $hookFolder 'Directory.Build.props') -Text '<Project />'
Save-Text -Path (Join-Path $hookFolder 'Directory.Build.targets') -Text '<Project />'
Save-Text -Path (Join-Path $hookFolder 'StartupHook.cs') -Text $script:HookSource
$hookPath = Join-Path $hookFolder 'performance.csproj'
Save-Text -Path $hookPath -Text ($script:HookProject.Replace('%FRAMEWORK%', $frameworkMatch.Groups[1].Value).Replace('%HARMONY%', [string]$settings.harmony))
Write-Host 'Building the timing hook' -ForegroundColor DarkGray
$code = Invoke-Native -Name 'dotnet' -Arguments @('build', $hookPath, '-c', 'Release', '-o', $hookBinary, '-nologo', '-v', 'q') -Quiet
if ($code -ne 0) {
    throw 'Building the timing hook failed.'
}

foreach ($path in $workload) {
    Build-Project -Path $path
}

# --- Measured runs ---

$stamp = (Get-Date).ToString('yyyyMMdd-HHmmss', $invariant)
$output = Join-Path $outputRoot $stamp
$probeFolder = Join-Path $output 'probe'
[void][System.IO.Directory]::CreateDirectory($probeFolder)

$environment = [ordered]@{
    'DOTNET_STARTUP_HOOKS' = (Join-Path $hookBinary 'PerformanceHook.dll')
    'PERFORMANCE_TARGET'   = $targetName
    'PERFORMANCE_OUTPUT'   = $probeFolder
}
$saved = @{}
foreach ($key in $environment.Keys) {
    $saved[$key] = [System.Environment]::GetEnvironmentVariable($key, 'Process')
}

$failed = $false
try {
    foreach ($key in $environment.Keys) {
        [System.Environment]::SetEnvironmentVariable($key, $environment[$key], 'Process')
    }

    for ($pass = 1; $pass -le $Repeat; $pass++) {
        foreach ($path in $workload) {
            Write-Host ("Run {0} of {1}: {2}" -f $pass, $Repeat, [System.IO.Path]::GetFileNameWithoutExtension($path)) -ForegroundColor DarkGray
            $arguments = @('test', $path, '-c', $Configuration, '--no-build', '-nologo', '-v', 'q')
            if ($Filter -ne '') { $arguments += @('--filter', $Filter) }
            $code = Invoke-Native -Name 'dotnet' -Arguments $arguments -Quiet
            if ($code -ne 0) { $failed = $true }
        }
    }
}
finally {
    foreach ($key in $environment.Keys) {
        [System.Environment]::SetEnvironmentVariable($key, $saved[$key], 'Process')
    }
}

# --- Merge ---

$probes = @(Get-ChildItem -LiteralPath $probeFolder -Filter 'probe_*.json' | Sort-Object { Get-OrdinalKey $_.Name })
if ($probes.Count -eq 0) {
    throw "No test process loaded $targetName, so nothing was timed."
}

$methods = [ordered]@{}
$skipped = [ordered]@{}
$patchMs = 0.0
foreach ($probe in $probes) {
    $data = [System.IO.File]::ReadAllText($probe.FullName, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    $scale = 1000.0 / [double]$data.frequency
    $patchMs += [double]$data.patchMs
    foreach ($item in @($data.methods)) {
        $key = "$($item.type).$($item.signature)"
        if (-not $methods.Contains($key)) {
            $methods[$key] = [pscustomobject]@{
                Type = [string]$item.type; Method = [string]$item.method; Signature = [string]$item.signature
                Calls = [long]0; Total = 0.0; Self = 0.0; Max = 0.0; Pending = [long]0
            }
        }
        $entry = $methods[$key]
        $entry.Calls += [long]$item.calls
        $entry.Total += [double]$item.inclusive * $scale
        $entry.Self += [double]$item.self * $scale
        $entry.Pending += [long]$item.pending
        $max = [double]$item.max * $scale
        if ($max -gt $entry.Max) { $entry.Max = $max }
    }
    foreach ($item in @($data.skipped)) {
        $key = "$($item.type).$($item.signature)"
        if (-not $skipped.Contains($key)) {
            $skipped[$key] = [pscustomobject]@{ Name = $key; Reason = [string]$item.reason }
        }
    }
}

# A type's folder comes from the source file that declares it, relative to the target project.
$targetFolder = Split-Path -Parent $targetProject
$typeFolders = @{}
foreach ($file in @(Get-ChildItem -LiteralPath $targetFolder -Recurse -Filter '*.cs' | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })) {
    $relative = $file.DirectoryName.Substring($targetFolder.Length).TrimStart('\', '/').Replace('\', '/')
    if ($relative -eq '') { $relative = '.' }
    foreach ($match in [regex]::Matches([System.IO.File]::ReadAllText($file.FullName), '\b(?:class|struct|record|interface|enum)\s+(\w+)')) {
        $name = $match.Groups[1].Value
        if (-not $typeFolders.ContainsKey($name)) { $typeFolders[$name] = $relative }
    }
}

$overloads = @{}
foreach ($entry in $methods.Values) {
    $key = "$($entry.Type).$($entry.Method)"
    $overloads[$key] = 1 + $(if ($overloads.ContainsKey($key)) { $overloads[$key] } else { 0 })
}

foreach ($entry in $methods.Values) {
    $outer = $entry.Type.Split('.')[0]
    $folder = if ($typeFolders.ContainsKey($outer)) { $typeFolders[$outer] } else { '(unknown)' }
    $name = "$($entry.Type).$($entry.Method)"
    if ($overloads[$name] -gt 1) { $name = "$($entry.Type).$($entry.Signature)" }
    $mean = if ($entry.Calls -gt 0) { $entry.Total / $entry.Calls } else { 0.0 }
    $entry | Add-Member -NotePropertyName Folder -NotePropertyValue $folder
    $entry | Add-Member -NotePropertyName Name -NotePropertyValue $name
    $entry | Add-Member -NotePropertyName Mean -NotePropertyValue $mean
}

$reached = @($methods.Values | Where-Object { $_.Calls -gt 0 } | Sort-Object @{ Expression = { $_.Mean }; Descending = $true }, @{ Expression = { Get-OrdinalKey $_.Name } })
$unreached = @($methods.Values | Where-Object { $_.Calls -eq 0 } | Sort-Object @{ Expression = { Get-OrdinalKey $_.Folder } }, @{ Expression = { Get-OrdinalKey $_.Name } })
$skippedRows = @($skipped.Values | Sort-Object @{ Expression = { Get-OrdinalKey $_.Name } })
$pending = [long]0
foreach ($entry in $methods.Values) { $pending += $entry.Pending }
$selfWhole = 0.0
foreach ($entry in $reached) { $selfWhole += $entry.Self }

$folders = @($methods.Values | Group-Object Folder | ForEach-Object {
    $group = @($_.Group)
    $calls = [long]0; $self = 0.0; $slowest = 0.0; $hit = 0
    foreach ($entry in $group) {
        $calls += $entry.Calls; $self += $entry.Self
        if ($entry.Calls -gt 0) { $hit++ }
        if ($entry.Mean -gt $slowest) { $slowest = $entry.Mean }
    }
    [pscustomobject]@{ Folder = $_.Name; Methods = $group.Count; Reached = $hit; Calls = $calls; Self = $self; Slowest = $slowest }
} | Sort-Object @{ Expression = { $_.Self }; Descending = $true }, @{ Expression = { Get-OrdinalKey $_.Folder } })

# --- Console ---

$generation = [int]$settings.generation
Write-Host ''
Write-Host "TRACEPERFORMANCE - TRACE GENERATION $generation" -ForegroundColor Blue
Write-Host ("Scanned: {0} methods of {1}, timed over {2} run(s) of {3}{4}" -f (Format-Integer $methods.Count), $targetName, $Repeat, ($workloadNames -join ', '), $(if ($Filter -ne '') { " ($Filter)" } else { '' })) -ForegroundColor DarkGray

if ($failed) {
    Write-Host 'A test run failed. Its timings are still reported.' -ForegroundColor Yellow
}
if ($pending -gt 0) {
    Write-Host ("{0} Task call(s) never completed before their process exited and are left out." -f (Format-Integer $pending)) -ForegroundColor Yellow
}

$folderTotalCalls = [long]0; $folderTotalMethods = 0; $folderTotalReached = 0
foreach ($row in $folders) { $folderTotalCalls += $row.Calls; $folderTotalMethods += $row.Methods; $folderTotalReached += $row.Reached }
$folderColumns = @(
    (New-Column -Name 'Folder' -Right $false -Values (@($folders | ForEach-Object { $_.Folder }) + 'Total')),
    (New-Column -Group 'Methods' -Name 'All' -Values (@($folders | ForEach-Object { Format-Integer $_.Methods }) + (Format-Integer $folderTotalMethods))),
    (New-Column -Group 'Methods' -Name 'Reached' -Values (@($folders | ForEach-Object { Format-Integer $_.Reached }) + (Format-Integer $folderTotalReached))),
    (New-Column -Name 'Calls' -Values (@($folders | ForEach-Object { Format-Integer $_.Calls }) + (Format-Integer $folderTotalCalls))),
    (New-Column -Group 'Self time' -Name 'ms' -Values (@($folders | ForEach-Object { Format-Milliseconds $_.Self }) + (Format-Milliseconds $selfWhole))),
    (New-Column -Group 'Self time' -Name 'Share' -Values (@($folders | ForEach-Object { Format-Percent $(if ($selfWhole -gt 0) { $_.Self * 100.0 / $selfWhole } else { 0 }) }) + (Format-Percent 100))),
    (New-Column -Name 'Slowest mean ms' -Values (@($folders | ForEach-Object { Format-Milliseconds $_.Slowest }) + ''))
)
Write-SectionTitle 'Time by folder'
Write-ConsoleTable -Columns $folderColumns

function New-MethodColumns {
    param([AllowEmptyCollection()][object[]]$Rows)

    return @(
        (New-Column -Group 'Time per call ms' -Name 'Mean' -Values @($Rows | ForEach-Object { Format-Milliseconds $_.Mean })),
        (New-Column -Group 'Time per call ms' -Name 'Max' -Values @($Rows | ForEach-Object { Format-Milliseconds $_.Max })),
        (New-Column -Name 'Calls' -Values @($Rows | ForEach-Object { Format-Integer $_.Calls })),
        (New-Column -Group 'Time in all calls ms' -Name 'Total' -Values @($Rows | ForEach-Object { Format-Milliseconds $_.Total })),
        (New-Column -Group 'Time in all calls ms' -Name 'Self' -Values @($Rows | ForEach-Object { Format-Milliseconds $_.Self })),
        (New-Column -Name 'Folder' -Right $false -Values @($Rows | ForEach-Object { $_.Folder })),
        (New-Column -Name 'Method' -Right $false -Values @($Rows | ForEach-Object { $_.Name }))
    )
}

function New-NameColumns {
    param([AllowEmptyCollection()][object[]]$Rows)

    return @(
        (New-Column -Name 'Folder' -Right $false -Values @($Rows | ForEach-Object { $_.Folder })),
        (New-Column -Name 'Method' -Right $false -Values @($Rows | ForEach-Object { $_.Name }))
    )
}

function New-SkippedColumns {
    param([AllowEmptyCollection()][object[]]$Rows)

    return @(
        (New-Column -Name 'Reason' -Right $false -Values @($Rows | ForEach-Object { $_.Reason })),
        (New-Column -Name 'Method' -Right $false -Values @($Rows | ForEach-Object { $_.Name }))
    )
}

function Write-ListSection {
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [AllowEmptyCollection()][object[]]$Rows,
        [Parameter(Mandatory = $true)][scriptblock]$Columns
    )

    if ($Rows.Count -eq 0) { return }
    Write-SectionTitle ("{0} ({1})" -f $Title, (Format-Integer $Rows.Count))
    $shown = @($Rows | Select-Object -First $Top)
    Write-ConsoleTable -Columns (& $Columns $shown)
    if ($Rows.Count -gt $shown.Count) {
        Write-Host ("... and {0} more in the report." -f (Format-Integer ($Rows.Count - $shown.Count)))
    }
}

Write-ListSection -Title 'Methods from the slowest to the fastest' -Rows $reached -Columns ${function:New-MethodColumns}
Write-ListSection -Title 'Not reached' -Rows $unreached -Columns ${function:New-NameColumns}
Write-ListSection -Title 'Skipped' -Rows $skippedRows -Columns ${function:New-SkippedColumns}

# --- Report files ---

$report = [System.Text.StringBuilder]::new()
[void]$report.Append("# Performance estimate: $targetName`n`n")
[void]$report.Append(("Workload: {0}, {1} run(s){2}.`n" -f ($workloadNames -join ', '), $Repeat, $(if ($Filter -ne '') { ", filter ``$Filter``" } else { '' })))
[void]$report.Append(("Methods: {0} timed, {1} reached, {2} not reached, {3} skipped.`n" -f (Format-Integer $methods.Count), (Format-Integer $reached.Count), (Format-Integer $unreached.Count), (Format-Integer $skippedRows.Count)))
[void]$report.Append(("Test processes: {0}, patching took {1} ms in all.`n" -f $probes.Count, (Format-Milliseconds $patchMs)))
if ($failed) { [void]$report.Append("A test run failed. Its timings are still reported.`n") }
if ($pending -gt 0) { [void]$report.Append(("{0} Task call(s) never completed and are left out.`n" -f (Format-Integer $pending))) }
[void]$report.Append("`nMean and max run from entry to return, or until the returned Task completes.`n")
[void]$report.Append("Self time leaves out the time spent in other timed methods on the same thread.`n`n")
[void]$report.Append("## Time by folder`n`n")
Add-MarkdownTable -Text $report -Columns $folderColumns
[void]$report.Append(("## Methods from the slowest to the fastest ({0})`n`n" -f (Format-Integer $reached.Count)))
if ($reached.Count -gt 0) { Add-MarkdownTable -Text $report -Columns (New-MethodColumns $reached) }
if ($unreached.Count -gt 0) {
    [void]$report.Append(("## Not reached ({0})`n`n" -f (Format-Integer $unreached.Count)))
    Add-MarkdownTable -Text $report -Columns (New-NameColumns $unreached)
}
if ($skippedRows.Count -gt 0) {
    [void]$report.Append(("## Skipped ({0})`n`n" -f (Format-Integer $skippedRows.Count)))
    Add-MarkdownTable -Text $report -Columns (New-SkippedColumns $skippedRows)
}
$reportFolder = Join-Path $root ([string]$settings.report.directory)
[System.IO.Directory]::CreateDirectory($reportFolder) | Out-Null
$reportPath = Join-Path $reportFolder ('TracePerformance-' + $stamp + '.md')
Save-Text -Path $reportPath -Text ($report.ToString().TrimEnd() + "`n")

Write-Host ''
Write-Host "Report: $reportPath"
