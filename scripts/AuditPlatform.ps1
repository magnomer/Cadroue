<#
.SYNOPSIS
Audit the platform axis of the source and report every project that breaks the portable and twin split.

.DESCRIPTION
Platform is a second axis that crosses every layer. A layer splits into a portable half that
targets the portable framework alone and holds no Windows code, and a Windows twin that holds
only that layer's Windows adaptation. This audit reads the platform table from AuditPlatform.json,
reads every project file under the configured root, and reports:

  Unmapped   a project on disk that the table does not name
  Absent     a project the table names that is not on disk
  Framework  a portable project not targeting exactly the portable framework, or a twin not
             targeting the Windows framework
  Reference  a portable project that references a twin or any Windows project
  Column     a twin that references a project outside its own column: its portable half and
             the other twins of that half, or the one capsule its table row names
  Analyzer   a portable project where the platform analyzer rule is not an error for one of
             its C# files, judged per file through the .editorconfig and .globalconfig chain
             (sections, root and precedence) and then the MSBuild warning properties
  Windows    a portable project that enables a Windows property, references a Windows package,
             or holds a source line that names a Windows API
  Empty      a twin that holds no source file
  Suppress   a portable project that silences the analyzer rule: NoWarn, an analyzer switch,
             a pragma or a SuppressMessage attribute
  Implicit   a project or Directory.Build file that turns implicit usings on
  Domain     a twin type that implements no port of its portable half, bound with Roslyn
             through the shared binder in AuditBinder.cs and AuditBinder.json

The host may reference every project, so its edges are never reported. The layer chain itself is
the structure audit's concern and is not repeated here.

Project names, frameworks, package names, property names and rule codes compare without regard
to case, as MSBuild and the compiler read them. Two project files that share one name stop the
audit with an error, since no table row could tell them apart.

Every kind has a ceiling in AuditPlatform.json, and an unwritten ceiling is zero. A kind above
its ceiling fails the run while enforced is true. A written ceiling above its count is stale and
always fails, so a ceiling only walks down. A file that cannot be read is counted and fails too.

Every run writes the Markdown report to the configured report folder as {prefix}{version}.md,
and the full result as a page next to it as {prefix}{version}.html. The page opens in the
default browser unless -NoOpen is given. Git and the .NET SDK are required, and the solution must be built so the binder finds the generated code.
The binder helper targets helper.framework from the configuration. No project source is modified.

This script carries no project-specific value of its own. Everything a project chooses - the
table, the frameworks, the analyzer rule, the Windows markers - lives in AuditPlatform.json.

.PARAMETER Root
Project root to audit. Defaults to the directory containing this script's parent.

.PARAMETER ReportDirectory
Overrides report.directory for this run, for both reports. Relative paths resolve against the
project root.

.PARAMETER Open
Open the Markdown report after the audit finishes.

.PARAMETER NoOpen
Write the page without opening it.

.PARAMETER NoPause
Do not stop at each console page. Paging is also off when output or input is redirected.

.PARAMETER Help
Display this help and exit. The alias -? is supported.

.EXAMPLE
AuditPlatform
Audit the current checkout.

.EXAMPLE
AuditPlatform -ReportDirectory D:\temp\audit -Open
Write both reports elsewhere and open the Markdown report as well as the page.

.EXAMPLE
AuditPlatform -NoOpen
Audit the current checkout and write the page without opening it.
#>
#requires -Version 5.1
# AUDITPLATFORM - AUDIT GENERATION 21.
# A generation is not a revision count. It names functionality, not edits, so editing one of these
# files is never on its own a reason to raise it. Raise it only when the audited outcome changes.
# A generation names the set of checks the audit applies. Two projects on the same generation audit
# the same things and their reports compare directly, whatever else differs between the files. A check
# added, removed, or changed in what it reports is a new generation; wording, plumbing, and refactoring
# leave it alone. Every audit script shares one generation number with the convention-test settings,
# and each refuses a configuration written at another generation.
# Generation 11: the first generation of this audit. It reads the project files and the portable
# sources and reports eight kinds.
# Generation 12: the audit reports the eleven kinds of the convention test, Suppress, Implicit and
# Domain included, and judges the analyzer severity per file as the compiler does.
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
    AuditPlatform.ps1

SYNOPSIS
    Report every project that breaks the portable and twin split.

SYNTAX
    AuditPlatform [-Root <path>] [-ReportDirectory <path>] [-Open] [-NoOpen] [-NoPause] [-Help]

OPTIONS
    -Root <path>             Project root. Defaults to the parent of the scripts folder.
    -ReportDirectory <path>  Overrides report.directory for this run, for both reports.
    -Open                    Open the Markdown report when done.
    -NoOpen                  Write the page without opening it.
    -NoPause                 No console paging.
    -Help                    Show this help.

REPORTS
    Every run writes a Markdown report to {report.directory}\{prefix}{version}.md
    and the full result as a page to {report.directory}\{prefix}{version}.html.
    The page opens in the default browser unless -NoOpen is given.

KINDS
    Unmapped, Absent, Framework, Reference, Column, Analyzer, Windows, Empty,
    Suppress, Implicit, Domain.
    See the script header for definitions.

RESULT
    Above ceiling     Kinds whose hits exceed their ceiling while enforced is true.
    Stale ceilings    Written ceilings that sit above their kind's hits.
    Unreadable files  Source or configuration files that could not be read.

EXIT CODES
    0   Every kind sits at or below its ceiling, no ceiling is stale, and every file was read.
    1   A Result gate is above zero.
'@ | Write-Host
    exit 0
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:AuditGeneration = 21
$script:AuditKinds = @('Unmapped', 'Absent', 'Framework', 'Reference', 'Column', 'Analyzer', 'Windows', 'Empty',
    'Suppress', 'Implicit', 'Domain')

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

function Get-DistinctText {
    # Keeps the first spelling of every value and drops later ones that differ only in case.
    param([string[]]$Values)

    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    $kept = New-Object 'System.Collections.Generic.List[string]'
    foreach ($value in @($Values)) {
        if ($seen.Add($value)) {
            $kept.Add($value)
        }
    }

    return @($kept.ToArray())
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
        Write-Host $Text
    }
    elseif ($PSBoundParameters.ContainsKey('ForegroundColor')) {
        Write-Host $Text -ForegroundColor $ForegroundColor
    }
    else {
        Write-Host $Text
    }
}

Write-AuditLine "AUDITPLATFORM - AUDIT GENERATION $script:AuditGeneration" -ForegroundColor Blue
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
        'generation', 'project', 'enforced', 'ceilings',
        'projects.root', 'projects.table',
        'framework.portable', 'framework.twin', 'helper.framework',
        'analyzer.rule', 'analyzer.silencers', 'domain.exempt',
        'windows.properties', 'windows.packages', 'windows.patterns',
        'sources.extensions', 'sources.excludeSegments', 'sources.excludeSuffixes', 'sources.excludePrefixes',
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
        throw "The platform-audit configuration is generation $($config.generation) but this tooling is generation $script:AuditGeneration.`nA generation names the set of checks applied, so the two must match: $ConfigPath"
    }

    if ($config.enforced -isnot [bool]) {
        throw "The audit configuration key 'enforced' must be true or false: $ConfigPath"
    }
    foreach ($ceiling in $config.ceilings.PSObject.Properties) {
        if ($script:AuditKinds -cnotcontains $ceiling.Name) {
            throw "The audit configuration writes a ceiling for the unknown kind '$($ceiling.Name)': $ConfigPath"
        }
        if (-not ($ceiling.Value -is [int] -or $ceiling.Value -is [long]) -or [long]$ceiling.Value -lt 0) {
            throw "The ceiling of '$($ceiling.Name)' must be a whole number of zero or more: $ConfigPath"
        }
    }

    $names = @{}
    foreach ($row in @($config.projects.table)) {
        $name = [string]$row.name
        $role = [string]$row.role
        if ([string]::IsNullOrWhiteSpace($name)) {
            throw "A platform table row has no name: $ConfigPath"
        }
        if ($names.ContainsKey($name)) {
            throw "The platform table names '$name' twice: $ConfigPath"
        }
        if (@('portable', 'twin', 'host') -notcontains $role) {
            throw "The platform table gives '$name' the unknown role '$role': $ConfigPath"
        }
        $names[$name] = $row
    }

    foreach ($row in @($config.projects.table)) {
        if ([string]$row.role -ne 'twin') {
            continue
        }
        $half = if ($row.PSObject.Properties.Name -contains 'half') { [string]$row.half } else { '' }
        if (-not $names.ContainsKey($half) -or [string]$names[$half].role -ne 'portable') {
            throw "The twin '$($row.name)' names no portable half in the platform table: $ConfigPath"
        }
        if ($row.PSObject.Properties.Name -contains 'capsule') {
            $capsule = [string]$row.capsule
            if (-not $names.ContainsKey($capsule) -or [string]$names[$capsule].role -ne 'portable') {
                throw "The twin '$($row.name)' names a capsule that is no portable project: $ConfigPath"
            }
        }
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
    if ($version -notmatch '^\d+\.\d+\.\d+$') {
        throw "Version must contain three numeric components: '$version'"
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
    param([string]$ProjectRoot, $Config)

    $git = Get-Command git -ErrorAction SilentlyContinue
    if ($null -eq $git) {
        throw 'Git is required to enumerate project files, but git was not found on PATH.'
    }

    if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot '.git'))) {
        throw "The project root is not a Git working tree: $ProjectRoot"
    }

    $rootPrefix = ([string]$Config.projects.root).Trim('/') + '/'
    $pathspecs = @(@('.csproj') + @($Config.sources.extensions) | ForEach-Object { ':(icase)' + $rootPrefix + '*' + [string]$_ })
    $lsArguments = @(@('-c', 'core.quotePath=false', '-C', $ProjectRoot,
        'ls-files', '--cached', '--others', '--exclude-standard', '--') + $pathspecs)
    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $gitOutput = & $git.Source @lsArguments 2>&1
    $ErrorActionPreference = $nativePreference
    if ($LASTEXITCODE -ne 0) {
        throw "Git could not enumerate project files.`n$($gitOutput -join [Environment]::NewLine)"
    }

    $files = New-Object 'System.Collections.Generic.List[string]'
    foreach ($entry in $gitOutput) {
        $relativePath = ([string]$entry).Trim()
        if ([string]::IsNullOrWhiteSpace($relativePath) -or (Test-ExcludedRelativePath -RelativePath $relativePath -Config $Config)) {
            continue
        }

        $fullPath = Join-Path $ProjectRoot ($relativePath.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar))
        if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
            $files.Add($relativePath)
        }
    }

    return @($files.ToArray() | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } } -Unique)
}

function Get-GitFiles {
    param([string]$ProjectRoot, [string[]]$Patterns)

    $nativePreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $gitOutput = & git -c core.quotePath=false -C $ProjectRoot ls-files --cached --others --exclude-standard -- @Patterns 2>&1
    $ErrorActionPreference = $nativePreference
    if ($LASTEXITCODE -ne 0) {
        throw "Git could not enumerate files.`n$($gitOutput -join [Environment]::NewLine)"
    }

    return @($gitOutput | ForEach-Object { ([string]$_).Trim() } | Where-Object {
        $_.Length -gt 0 -and (Test-Path -LiteralPath (Join-Path $ProjectRoot $_) -PathType Leaf)
    })
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length != 4)
{
    Console.Error.WriteLine("usage: <root> <binder> <jobs> <output>");
    return 2;
}

LAuditBinder binder = LAuditBinder.LAuditBinderRead(args[0], args[1]);
Dictionary<string, SyntaxTree> trees = binder.LAuditTrees
    .ToDictionary(tree => Path.GetFullPath(tree.FilePath), StringComparer.OrdinalIgnoreCase);
List<string> rows = [];
string[] jobs = File.ReadAllLines(args[2]);
for (int index = 0; index < jobs.Length; index++)
{
    string[] parts = jobs[index].Split('\t');
    if (parts.Length != 2)
    {
        continue;
    }

    string portable = parts[0];
    if (!trees.TryGetValue(Path.GetFullPath(parts[1]), out SyntaxTree? tree))
    {
        continue;
    }

    SemanticModel model = binder.LAuditCompilation.GetSemanticModel(tree, true);
    foreach (TypeDeclarationSyntax declared in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
                 .Where(type => type.Parent is not TypeDeclarationSyntax))
    {
        if (model.GetDeclaredSymbol(declared)?.OriginalDefinition is not INamedTypeSymbol type)
        {
            continue;
        }

        IEnumerable<INamedTypeSymbol> bases = type.AllInterfaces;
        for (INamedTypeSymbol? current = type.BaseType; current is not null; current = current.BaseType)
        {
            bases = bases.Append(current);
        }

        bool ported = bases.Any(held =>
            held.OriginalDefinition.Locations.FirstOrDefault(location => location.IsInSource)?.SourceTree is SyntaxTree home
            && binder.LAuditRelativeRead(home.FilePath).StartsWith(portable, StringComparison.OrdinalIgnoreCase));
        if (!ported)
        {
            int line = declared.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            rows.Add(string.Join('\t', index, binder.LAuditRelativeRead(tree.FilePath), line, type.Name));
        }
    }
}

File.WriteAllLines(args[3], rows);
return 0;
'@

function Invoke-DomainHelper {
    param([string]$ProjectRoot, [string[]]$JobLines)

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($null -eq $dotnet) {
        throw 'The .NET SDK is required to bind the twins, but dotnet was not found on PATH.'
    }

    $targetFramework = [string]$config.helper.framework
    $binderSource = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'AuditBinder.cs'))
    $binderConfig = Join-Path $PSScriptRoot 'AuditBinder.json'

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $seed = $script:HelperProject + "`n" + $script:HelperProgram + "`n" + $binderSource + "`n" + $targetFramework
        $digest = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($seed))
    }
    finally {
        $sha.Dispose()
    }
    $hash = ([System.BitConverter]::ToString($digest) -replace '-', '').Substring(0, 16).ToLowerInvariant()
    $cacheParent = Join-Path ([System.IO.Path]::GetTempPath()) ([string]$config.project + '-AuditPlatform')
    $cacheFolder = Join-Path $cacheParent $hash
    $binaryPath = Join-Path (Join-Path $cacheFolder 'bin') 'AuditPlatform.dll'

    $previousNoLogo = $env:DOTNET_NOLOGO
    $previousTelemetry = $env:DOTNET_CLI_TELEMETRY_OPTOUT
    $env:DOTNET_NOLOGO = '1'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $temporaryFolder = Join-Path ([System.IO.Path]::GetTempPath()) ([string]$config.project + '-AuditPlatform-' + [Guid]::NewGuid().ToString('N'))
    [System.IO.Directory]::CreateDirectory($temporaryFolder) | Out-Null
    try {
        if (-not (Test-Path -LiteralPath $binaryPath -PathType Leaf)) {
            Write-AuditLine 'Compiling the platform binder once for this helper text...'
            if (Test-Path -LiteralPath $cacheParent) {
                Get-ChildItem -LiteralPath $cacheParent -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
            }
            $helperFolder = Join-Path $cacheFolder 'helper'
            [System.IO.Directory]::CreateDirectory($helperFolder) | Out-Null
            $encoding = New-Object System.Text.UTF8Encoding($false)
            $projectPath = Join-Path $helperFolder 'AuditPlatform.csproj'
            [System.IO.File]::WriteAllText($projectPath, $script:HelperProject.Replace('{TARGET_FRAMEWORK}', $targetFramework), $encoding)
            [System.IO.File]::WriteAllText((Join-Path $helperFolder 'Program.cs'), $script:HelperProgram, $encoding)
            [System.IO.File]::WriteAllText((Join-Path $helperFolder 'AuditBinder.cs'), $binderSource, $encoding)
            $nativePreference = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            $buildOutput = & $dotnet.Source build $projectPath --configuration Release --nologo --verbosity quiet --output (Join-Path $cacheFolder 'bin') 2>&1
            $ErrorActionPreference = $nativePreference
            if ($LASTEXITCODE -ne 0) {
                Remove-Item -LiteralPath $cacheFolder -Recurse -Force -ErrorAction SilentlyContinue
                throw "The platform binder could not be built.`n$($buildOutput -join [Environment]::NewLine)"
            }
        }

        $jobsPath = Join-Path $temporaryFolder 'jobs.txt'
        $outputPath = Join-Path $temporaryFolder 'domain.txt'
        [System.IO.File]::WriteAllLines($jobsPath, $JobLines, [System.Text.UTF8Encoding]::new($false))
        $nativePreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        $helperOutput = & $dotnet.Source $binaryPath $ProjectRoot $binderConfig $jobsPath $outputPath 2>&1
        $ErrorActionPreference = $nativePreference
        if ($LASTEXITCODE -ne 0) {
            throw "The platform binder failed.`n$($helperOutput -join [Environment]::NewLine)"
        }

        return @([System.IO.File]::ReadAllLines($outputPath))
    }
    finally {
        $env:DOTNET_NOLOGO = $previousNoLogo
        $env:DOTNET_CLI_TELEMETRY_OPTOUT = $previousTelemetry
        Remove-Item -LiteralPath $temporaryFolder -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Read-XmlText {
    param([string]$Path)

    [xml]$document = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    return $document
}

function Get-XmlElements {
    # MSBuild reads property and item names without regard to case, so the element name matches alike.
    param([xml]$Document, [string]$Element)

    return @($Document.SelectNodes('//*') | Where-Object { [string]::Equals($_.LocalName, $Element, [System.StringComparison]::OrdinalIgnoreCase) })
}

function Get-XmlValues {
    param([xml]$Document, [string]$Element)

    return @(Get-XmlElements -Document $Document -Element $Element | ForEach-Object { [string]$_.InnerText.Trim() })
}

function Get-XmlIncludes {
    param([xml]$Document, [string]$Element)

    return @(Get-XmlElements -Document $Document -Element $Element |
        ForEach-Object { [string]$_.GetAttribute('Include') } |
        Where-Object { $_.Length -gt 0 })
}

function Get-ImportChain {
    param([string]$ProjectRoot, [string]$ProjectFolder)

    $chain = New-Object 'System.Collections.Generic.List[string]'
    $folder = $ProjectFolder
    while ($true) {
        foreach ($name in @('Directory.Build.props', 'Directory.Build.targets')) {
            $candidate = Join-Path $folder $name
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                $chain.Add($candidate)
            }
        }
        if ($folder.TrimEnd($script:PathSeparators) -eq $ProjectRoot) {
            break
        }
        $parent = Split-Path -Parent $folder
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $folder) {
            break
        }
        $folder = $parent
    }

    return @($chain.ToArray())
}

function Get-ConfigChain {
    param([string]$ProjectRoot, [string]$ProjectFolder)

    $chain = New-Object 'System.Collections.Generic.List[string]'
    $folder = $ProjectFolder
    while ($true) {
        foreach ($name in @('.editorconfig', '.globalconfig')) {
            $candidate = Join-Path $folder $name
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                $chain.Add($candidate)
            }
        }
        if ($folder.TrimEnd($script:PathSeparators) -eq $ProjectRoot) {
            break
        }
        $parent = Split-Path -Parent $folder
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $folder) {
            break
        }
        $folder = $parent
    }

    return @($chain.ToArray())
}

function Test-GlobMatch {
    param([string]$Glob, [string]$Relative)

    $pattern = if ($Glob.Contains('/')) { $Glob.TrimStart('/') } else { '**/' + $Glob }
    $text = New-Object System.Text.StringBuilder
    [void]$text.Append('^')
    for ($index = 0; $index -lt $pattern.Length; $index++) {
        $next = [string]$pattern[$index]
        if ($next -eq '*' -and $index + 1 -lt $pattern.Length -and [string]$pattern[$index + 1] -eq '*') {
            $slash = $index + 2 -lt $pattern.Length -and [string]$pattern[$index + 2] -eq '/'
            if ($slash) { [void]$text.Append('(?:.*/)?'); $index += 2 } else { [void]$text.Append('.*'); $index += 1 }
            continue
        }
        $piece = switch -CaseSensitive ($next) {
            '*' { '[^/]*' }
            '?' { '[^/]' }
            '{' { '(?:' }
            '}' { ')' }
            ',' { '|' }
            '[' { '[' }
            ']' { ']' }
            default { [regex]::Escape($next) }
        }
        [void]$text.Append($piece)
    }
    [void]$text.Append('$')
    return [regex]::IsMatch($Relative, $text.ToString(), [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
}

$script:SectionPattern = [regex]::new('^\s*\[(?<glob>.+)\]\s*$')
$script:PairPattern = [regex]::new('^\s*(?<key>[^=#;]+?)\s*=\s*(?<value>[^#;]*?)\s*$')

function Get-ConfigLevel {
    param([string]$ConfigPath, [string]$SourcePath, [string]$Key)

    $applies = $ConfigPath.EndsWith('.globalconfig', [System.StringComparison]::Ordinal)
    $configFolder = (Split-Path -Parent $ConfigPath).TrimEnd($script:PathSeparators)
    $relative = $SourcePath.Substring($configFolder.Length).TrimStart($script:PathSeparators).Replace('\', '/')
    $level = $null
    foreach ($line in (Read-AuditLines -Path $ConfigPath)) {
        $section = $script:SectionPattern.Match($line)
        if ($section.Success) {
            $applies = Test-GlobMatch -Glob $section.Groups['glob'].Value -Relative $relative
            continue
        }
        $pair = $script:PairPattern.Match($line)
        if ($applies -and $pair.Success -and $pair.Groups['key'].Value -eq $Key) {
            $level = $pair.Groups['value'].Value
        }
    }

    return $level
}

function Test-ConfigRoot {
    param([string]$ConfigPath)

    foreach ($line in (Read-AuditLines -Path $ConfigPath)) {
        $pair = $script:PairPattern.Match($line)
        if ($pair.Success -and $pair.Groups['key'].Value -eq 'root' -and $pair.Groups['value'].Value -eq 'true') {
            return $true
        }
    }

    return $false
}

function Test-ListedRule {
    param([xml]$Document, [string]$Element, [string]$Rule)

    foreach ($value in (Get-XmlValues -Document $Document -Element $Element)) {
        if (@([regex]::Split($value, '[;,\s]+')) -contains $Rule) {
            return $true
        }
    }

    return $false
}

function Test-AnalyzerError {
    param([string]$ProjectRoot, [string]$ProjectPath, [string]$SourcePath, [string]$Rule)

    $configs = @(Get-ConfigChain -ProjectRoot $ProjectRoot -ProjectFolder (Split-Path -Parent $SourcePath))
    $editors = @($configs | Where-Object { $_.EndsWith('.editorconfig', [System.StringComparison]::Ordinal) })
    $globals = @($configs | Where-Object { $_.EndsWith('.globalconfig', [System.StringComparison]::Ordinal) })
    $kept = New-Object 'System.Collections.Generic.List[string]'
    foreach ($editor in $editors) {
        $kept.Add($editor)
        if (Test-ConfigRoot -ConfigPath $editor) {
            break
        }
    }
    [array]::Reverse($globals)
    $nearest = $kept.ToArray()
    [array]::Reverse($nearest)

    $level = $null
    foreach ($config in @(@($globals) + @($nearest))) {
        $found = Get-ConfigLevel -ConfigPath $config -SourcePath $SourcePath -Key "dotnet_diagnostic.$Rule.severity"
        if ($null -ne $found) {
            $level = $found
        }
    }

    $listed = $false
    $spared = $false
    $every = $false
    foreach ($path in @(@(Get-ImportChain -ProjectRoot $ProjectRoot -ProjectFolder (Split-Path -Parent $ProjectPath)) + $ProjectPath)) {
        $document = Read-XmlText -Path $path
        if (Test-ListedRule -Document $document -Element 'WarningsAsErrors' -Rule $Rule) { $listed = $true }
        if (Test-ListedRule -Document $document -Element 'WarningsNotAsErrors' -Rule $Rule) { $spared = $true }
        foreach ($value in (Get-XmlValues -Document $document -Element 'TreatWarningsAsErrors')) {
            if ($value -eq 'true') { $every = $true }
        }
    }

    $escalated = $listed -or ($every -and -not $spared)
    if ($null -eq $level) {
        return $escalated
    }
    if ($level -eq 'error') {
        return $true
    }
    if ($level -eq 'warning') {
        return $escalated
    }

    return $false
}

function Read-ProjectFacts {
    param([string]$ProjectRoot, [string]$RelativePath)

    $fullPath = Join-Path $ProjectRoot ($RelativePath.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar))
    $document = Read-XmlText -Path $fullPath
    $frameworks = @(@(Get-XmlValues -Document $document -Element 'TargetFramework') +
        @(Get-XmlValues -Document $document -Element 'TargetFrameworks') |
        ForEach-Object { ([string]$_).Split([char]';') } |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_.Length -gt 0 })
    $references = @(Get-DistinctText -Values @(Get-XmlIncludes -Document $document -Element 'ProjectReference' |
        ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension(($_ -split '[\\/]')[-1]) }))
    $packages = @(Get-DistinctText -Values @(Get-XmlIncludes -Document $document -Element 'PackageReference'))

    return [pscustomobject]@{
        Name       = [System.IO.Path]::GetFileNameWithoutExtension($RelativePath)
        Path       = $RelativePath
        FullPath   = $fullPath
        Folder     = [System.IO.Path]::GetDirectoryName($RelativePath).Replace('\', '/')
        Document   = $document
        Frameworks = $frameworks
        References = $references
        Packages   = $packages
    }
}

$script:Hits = New-Object 'System.Collections.Generic.List[object]'
$script:ReadErrors = New-Object 'System.Collections.Generic.SortedDictionary[string,string]' ([System.StringComparer]::Ordinal)

function Read-AuditLines {
    # A file read many times is still one unreadable file, so the errors are keyed by path.
    param([Parameter(Mandatory = $true)][string]$Path)

    try {
        return , [System.IO.File]::ReadAllLines($Path)
    }
    catch {
        $failure = $_.Exception
        if ($failure -is [System.Management.Automation.MethodInvocationException] -and $null -ne $failure.InnerException) {
            $failure = $failure.InnerException
        }
        $script:ReadErrors[$Path] = $failure.Message
        return , @()
    }
}

function Add-AuditHit {
    param([string]$Kind, [string]$Project, [string]$Path, [int]$Line, [string]$Detail)

    $script:Hits.Add([pscustomobject]@{
        Kind    = $Kind
        Project = $Project
        Path    = $Path
        Line    = $Line
        Detail  = $Detail
    })
}

$projectRoot = Resolve-ProjectRoot -Path $Root
$configPath = Join-Path $PSScriptRoot 'AuditPlatform.json'
$config = Read-AuditConfig -ConfigPath $configPath
$version = Read-ProjectVersion -ProjectRoot $projectRoot -Config $config

$reportFolder = if ([string]::IsNullOrWhiteSpace($ReportDirectory)) { [string]$config.report.directory } else { $ReportDirectory }
if (-not [System.IO.Path]::IsPathRooted($reportFolder)) {
    $reportFolder = Join-Path $projectRoot $reportFolder
}
$reportPath = Join-Path $reportFolder ([string]$config.report.prefix + $version + '.md')
$pagePath = Join-Path $reportFolder ([string]$config.report.prefix + $version + '.html')

[string[]]$trackedFiles = @(Get-TrackedFiles -ProjectRoot $projectRoot -Config $config)
[string[]]$projectFiles = @($trackedFiles | Where-Object { $_.EndsWith('.csproj', [System.StringComparison]::OrdinalIgnoreCase) })
if ($projectFiles.Length -eq 0) {
    throw "No project files were found under: $(Join-Path $projectRoot $config.projects.root)"
}
Write-AuditLine ("Scanned: {0:N0} project files" -f $projectFiles.Length) -ForegroundColor DarkGray

$table = @{}
foreach ($row in @($config.projects.table)) {
    $table[[string]$row.name] = $row
}

$sharedNames = @{}
foreach ($relativePath in $projectFiles) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($relativePath)
    if (-not $sharedNames.ContainsKey($name)) {
        $sharedNames[$name] = New-Object 'System.Collections.Generic.List[string]'
    }
    $sharedNames[$name].Add($relativePath)
}
$twice = @($sharedNames.Keys | Where-Object { $sharedNames[$_].Count -gt 1 } | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } } | ForEach-Object {
    $_ + ': ' + (@($sharedNames[$_] | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } }) -join ', ')
})
if ($twice.Count -gt 0) {
    throw "Project files share one name, so the audit cannot tell them apart:`n$($twice -join "`n")"
}

$facts = @{}
foreach ($relativePath in $projectFiles) {
    $fact = Read-ProjectFacts -ProjectRoot $projectRoot -RelativePath $relativePath
    $facts[$fact.Name] = $fact
}

$portableFramework = [string]$config.framework.portable
$twinFramework = [string]$config.framework.twin

function Get-ProjectRole {
    param([string]$Name)

    if ($table.ContainsKey($Name)) {
        return [string]$table[$Name].role
    }
    return ''
}

function Get-ProjectColumn {
    param([string]$Name)

    # A column is spelled as the table spells its portable half, whatever case the caller used.
    $role = Get-ProjectRole -Name $Name
    if ($role -eq 'twin') {
        return [string]$table[[string]$table[$Name].half].name
    }
    if ($role -eq 'portable') {
        return [string]$table[$Name].name
    }
    return ''
}

function Test-WindowsProject {
    param([string]$Name)

    if ((Get-ProjectRole -Name $Name) -eq 'twin') {
        return $true
    }
    if ($facts.ContainsKey($Name)) {
        foreach ($framework in $facts[$Name].Frameworks) {
            if ($framework.IndexOf('-windows', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                return $true
            }
        }
    }
    return $false
}

foreach ($name in ($facts.Keys | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } })) {
    if (-not $table.ContainsKey($name)) {
        Add-AuditHit -Kind 'Unmapped' -Project $name -Path $facts[$name].Path -Line 0 -Detail 'the platform table does not name this project'
    }
}

foreach ($row in @($config.projects.table)) {
    $name = [string]$row.name
    if (-not $facts.ContainsKey($name)) {
        Add-AuditHit -Kind 'Absent' -Project $name -Path '' -Line 0 -Detail "the table names this $($row.role), but no project file exists"
    }
}

$sourceFiles = @($trackedFiles | Where-Object { -not $_.EndsWith('.csproj', [System.StringComparison]::OrdinalIgnoreCase) })
$domainJobs = New-Object 'System.Collections.Generic.List[object]'
$patterns = @($config.windows.patterns | ForEach-Object { [regex]::new([string]$_, [System.Text.RegularExpressions.RegexOptions]::Compiled) })

foreach ($name in ($facts.Keys | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } })) {
    if (-not $table.ContainsKey($name)) {
        continue
    }

    $fact = $facts[$name]
    $role = Get-ProjectRole -Name $name
    $frameworkText = ($fact.Frameworks -join ';')

    if ($role -eq 'portable') {
        if ($fact.Frameworks.Length -ne 1 -or -not [string]::Equals($fact.Frameworks[0], $portableFramework, [System.StringComparison]::OrdinalIgnoreCase)) {
            Add-AuditHit -Kind 'Framework' -Project $name -Path $fact.Path -Line 0 -Detail "targets '$frameworkText', not exactly '$portableFramework'"
        }

        foreach ($reference in $fact.References) {
            if (Test-WindowsProject -Name $reference) {
                Add-AuditHit -Kind 'Reference' -Project $name -Path $fact.Path -Line 0 -Detail "references the Windows project $reference"
            }
        }

        $rule = [string]$config.analyzer.rule
        $heldCode = @($sourceFiles | Where-Object {
            $_.StartsWith($fact.Folder + '/', [System.StringComparison]::Ordinal) -and $_.EndsWith('.cs', [System.StringComparison]::Ordinal)
        })
        $analyzerOrder = @($heldCode | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_.ToUpperInvariant() } }, @{ Expression = { Get-OrdinalKey $_ } })
        foreach ($source in $analyzerOrder) {
            $sourcePath = Join-Path $projectRoot ($source.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar))
            if (-not (Test-AnalyzerError -ProjectRoot $projectRoot -ProjectPath $fact.FullPath -SourcePath $sourcePath -Rule $rule)) {
                Add-AuditHit -Kind 'Analyzer' -Project $name -Path $source -Line 0 -Detail "$rule is not an error here"
                break
            }
        }

        $chainDocuments = @(@(Get-ImportChain -ProjectRoot $projectRoot -ProjectFolder (Split-Path -Parent $fact.FullPath)) + $fact.FullPath |
            ForEach-Object { Read-XmlText -Path $_ })
        if (@($chainDocuments | Where-Object { Test-ListedRule -Document $_ -Element 'NoWarn' -Rule $rule }).Count -gt 0) {
            Add-AuditHit -Kind 'Suppress' -Project $name -Path $fact.Path -Line 0 -Detail "NoWarn holds $rule"
        }
        foreach ($silencer in $config.analyzer.silencers.PSObject.Properties) {
            $silenced = $false
            foreach ($document in $chainDocuments) {
                foreach ($value in (Get-XmlValues -Document $document -Element $silencer.Name)) {
                    if ($value -eq [string]$silencer.Value) { $silenced = $true }
                }
            }
            if ($silenced) {
                Add-AuditHit -Kind 'Suppress' -Project $name -Path $fact.Path -Line 0 -Detail "$($silencer.Name) is $($silencer.Value)"
            }
        }
        foreach ($source in $heldCode) {
            $sourcePath = Join-Path $projectRoot ($source.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar))
            $lines = Read-AuditLines -Path $sourcePath
            for ($index = 0; $index -lt $lines.Length; $index++) {
                $line = $lines[$index]
                $pragma = $line -cmatch '^\s*#\s*pragma\s+warning\s+disable\b' -and ($line.Contains($rule) -or $line -cmatch 'disable\s*$')
                $attribute = $line.Contains('SuppressMessage') -and $line.Contains($rule)
                if ($pragma -or $attribute) {
                    Add-AuditHit -Kind 'Suppress' -Project $name -Path $source -Line ($index + 1) -Detail $line.Trim()
                }
            }
        }

        foreach ($property in @($config.windows.properties)) {
            if (@(Get-XmlValues -Document $fact.Document -Element ([string]$property)) -contains 'true') {
                Add-AuditHit -Kind 'Windows' -Project $name -Path $fact.Path -Line 0 -Detail "enables $property"
            }
        }

        foreach ($package in $fact.Packages) {
            if (@($config.windows.packages) -contains $package) {
                Add-AuditHit -Kind 'Windows' -Project $name -Path $fact.Path -Line 0 -Detail "references the Windows package $package"
            }
        }

        foreach ($source in $sourceFiles) {
            if (-not $source.StartsWith($fact.Folder + '/', [System.StringComparison]::Ordinal)) {
                continue
            }
            $sourcePath = Join-Path $projectRoot ($source.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar))
            $lines = Read-AuditLines -Path $sourcePath
            for ($index = 0; $index -lt $lines.Length; $index++) {
                foreach ($pattern in $patterns) {
                    $match = $pattern.Match($lines[$index])
                    if ($match.Success) {
                        Add-AuditHit -Kind 'Windows' -Project $name -Path $source -Line ($index + 1) -Detail "names $($match.Value)"
                        break
                    }
                }
            }
        }
    }
    elseif ($role -eq 'twin') {
        if ($fact.Frameworks.Length -ne 1 -or -not $fact.Frameworks[0].StartsWith($twinFramework, [System.StringComparison]::OrdinalIgnoreCase)) {
            Add-AuditHit -Kind 'Framework' -Project $name -Path $fact.Path -Line 0 -Detail "targets '$frameworkText', not '$twinFramework'"
        }

        $column = Get-ProjectColumn -Name $name
        $capsule = if ($table[$name].PSObject.Properties.Name -contains 'capsule') { [string]$table[$name].capsule } else { '' }
        foreach ($reference in $fact.References) {
            if ((Get-ProjectColumn -Name $reference) -ne $column -and $reference -ne $capsule) {
                Add-AuditHit -Kind 'Column' -Project $name -Path $fact.Path -Line 0 -Detail "references $reference outside the $column column"
            }
        }

        $held = @($sourceFiles | Where-Object { $_.StartsWith($fact.Folder + '/', [System.StringComparison]::Ordinal) })
        if ($held.Length -eq 0) {
            Add-AuditHit -Kind 'Empty' -Project $name -Path $fact.Path -Line 0 -Detail 'the twin holds no source file'
        }

        if (@($config.domain.exempt) -notcontains $column) {
            $portablePath = ([string]$config.projects.root).Trim('/') + '/' + $column + '/'
            foreach ($source in @($held | Where-Object { $_.EndsWith('.cs', [System.StringComparison]::Ordinal) })) {
                $domainJobs.Add([pscustomobject]@{ Project = $name; Column = $column; Portable = $portablePath; Source = $source })
            }
        }
    }
}

$importFiles = @(Get-GitFiles -ProjectRoot $projectRoot -Patterns @(':(icase)*Directory.Build.props', ':(icase)*Directory.Build.targets'))
foreach ($settings in @(@($projectFiles) + @($importFiles))) {
    $settingsPath = Join-Path $projectRoot ($settings.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar))
    $implicit = @(Get-XmlValues -Document (Read-XmlText -Path $settingsPath) -Element 'ImplicitUsings' | Where-Object { $_ -eq 'enable' -or $_ -eq 'true' })
    if ($implicit.Count -gt 0) {
        Add-AuditHit -Kind 'Implicit' -Project ([System.IO.Path]::GetFileNameWithoutExtension($settings)) -Path $settings -Line 0 -Detail 'turns implicit usings on, so the binder would miss their global usings'
    }
}

if ($domainJobs.Count -gt 0) {
    # Each helper row names its job by index, so a file held by nested twins is credited to each of them.
    $jobLines = @($domainJobs | ForEach-Object { $_.Portable + "`t" + (Join-Path $projectRoot ($_.Source.Replace([char]'/', [System.IO.Path]::DirectorySeparatorChar))) })
    foreach ($row in (Invoke-DomainHelper -ProjectRoot $projectRoot -JobLines $jobLines)) {
        $parts = ([string]$row).Split("`t")
        if ($parts.Length -ne 4) { continue }
        $job = $domainJobs[[int]$parts[0]]
        Add-AuditHit -Kind 'Domain' -Project $job.Project -Path $parts[1] -Line ([int]$parts[2]) -Detail "$($parts[3]) implements no port of $($job.Column), so it holds more than a Windows adaptation"
    }
}

$ceilings = @{}
foreach ($property in $config.ceilings.PSObject.Properties) {
    $ceilings[$property.Name] = [int]$property.Value
}
$enforced = [bool]$config.enforced

$kindRows = New-Object 'System.Collections.Generic.List[object]'
foreach ($kind in $script:AuditKinds) {
    $kindHits = @($script:Hits | Where-Object { $_.Kind -eq $kind } |
        Sort-Object -Property @{ Expression = { Get-OrdinalKey $_.Project } }, @{ Expression = { Get-OrdinalKey $_.Path } }, Line, @{ Expression = { Get-OrdinalKey $_.Detail } })
    $ceiling = if ($ceilings.ContainsKey($kind)) { $ceilings[$kind] } else { 0 }
    $standing = if ($kindHits.Length -gt $ceiling) { 'above ceiling' } elseif ($kindHits.Length -lt $ceiling) { 'stale ceiling' } else { 'at ceiling' }
    $kindRows.Add([pscustomobject]@{
        Kind     = $kind
        Hits     = $kindHits
        Ceiling  = $ceiling
        Written  = $ceilings.ContainsKey($kind)
        Standing = $standing
    })
}
$overRows = @($kindRows | Where-Object { $_.Standing -eq 'above ceiling' })
$staleRows = @($kindRows | Where-Object { $_.Written -and $_.Standing -eq 'stale ceiling' })
$aboveCount = if ($enforced) { $overRows.Count } else { 0 }

function Get-HitWhere {
    param($Hit, [string]$Quote)

    if ($Hit.Path -eq '') {
        return ''
    }
    if ($Hit.Line -gt 0) {
        return " $Quote$($Hit.Path):$($Hit.Line)$Quote"
    }
    return " $Quote$($Hit.Path)$Quote"
}

$report = New-Object System.Text.StringBuilder
[void]$report.AppendLine("# Platform audit - $($config.project) $version")
[void]$report.AppendLine()
[void]$report.AppendLine("- Generation: $script:AuditGeneration")
[void]$report.AppendLine("- Enforced: $($enforced.ToString().ToLowerInvariant())")
[void]$report.AppendLine("- Above ceiling: $aboveCount")
[void]$report.AppendLine("- Stale ceilings: $($staleRows.Count)")
[void]$report.AppendLine("- Unreadable files: $($script:ReadErrors.Count)")
[void]$report.AppendLine()
[void]$report.AppendLine('## Table')
[void]$report.AppendLine()
[void]$report.AppendLine('| Project | Role | Column | On disk | Targets | References |')
[void]$report.AppendLine('|---|---|---|---|---|---|')
$names = @(Get-DistinctText -Values @(@($table.Keys) + @($facts.Keys)) | Sort-Object -Property @{ Expression = { Get-OrdinalKey $_ } })
foreach ($name in $names) {
    $role = Get-ProjectRole -Name $name
    $roleText = if ($role -eq '') { 'unmapped' } else { $role }
    $column = Get-ProjectColumn -Name $name
    $onDisk = if ($facts.ContainsKey($name)) { 'yes' } else { 'no' }
    $targets = if ($facts.ContainsKey($name)) { ($facts[$name].Frameworks -join ', ') } else { '' }
    $references = if ($facts.ContainsKey($name)) { ($facts[$name].References -join ', ') } else { '' }
    [void]$report.AppendLine("| $name | $roleText | $column | $onDisk | $targets | $references |")
}
[void]$report.AppendLine()
[void]$report.AppendLine('## Counts')
[void]$report.AppendLine()
[void]$report.AppendLine('| Kind | Hits | Ceiling | Standing |')
[void]$report.AppendLine('|---|---|---|---|')
foreach ($row in $kindRows) {
    [void]$report.AppendLine("| $($row.Kind) | $($row.Hits.Length) | $($row.Ceiling) | $($row.Standing) |")
}
[void]$report.AppendLine("| Total | $($script:Hits.Count) | | |")

foreach ($row in $kindRows) {
    if ($row.Hits.Length -eq 0) {
        continue
    }
    [void]$report.AppendLine()
    [void]$report.AppendLine("## $($row.Kind)")
    [void]$report.AppendLine()
    foreach ($hit in $row.Hits) {
        [void]$report.AppendLine("- $($hit.Project)$(Get-HitWhere -Hit $hit -Quote '`') - $($hit.Detail)")
    }
}

if ($script:ReadErrors.Count -gt 0) {
    [void]$report.AppendLine()
    [void]$report.AppendLine('## Unreadable files')
    [void]$report.AppendLine()
    foreach ($pair in $script:ReadErrors.GetEnumerator()) {
        [void]$report.AppendLine("- ``$($pair.Key)``: $($pair.Value)")
    }
}

[System.IO.Directory]::CreateDirectory($reportFolder) | Out-Null
$stagingPath = $reportPath + '.tmp'
[System.IO.File]::WriteAllText($stagingPath, ($report.ToString() -replace "`r`n", "`n"), [System.Text.UTF8Encoding]::new($false))
Move-Item -LiteralPath $stagingPath -Destination $reportPath -Force

# Page output: the full result as data inside the template AuditPlatform.html.
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

function Write-AuditSection {
    param([string]$Title)

    Write-AuditLine ''
    Write-AuditLine $Title -ForegroundColor Blue
    Write-AuditLine ('-' * $Title.Length) -ForegroundColor DarkGray
}

$gateRows = @(
    [pscustomobject]@{ Gate = 'Above ceiling'; Count = $aboveCount; Meaning = 'enforced kinds whose hits exceed their ceiling'; Section = 'Above ceiling' },
    [pscustomobject]@{ Gate = 'Stale ceilings'; Count = $staleRows.Count; Meaning = 'ceilings set above their kind hits'; Section = 'Stale ceilings' },
    [pscustomobject]@{ Gate = 'Unreadable files'; Count = $script:ReadErrors.Count; Meaning = 'files the audit could not read'; Section = 'Unreadable files' }
)
Write-AuditSection -Title 'Result'
$statusWidth = 6
$countWidth = [Math]::Max(5, @($gateRows | ForEach-Object { $_.Count.ToString('N0').Length } | Measure-Object -Maximum)[0].Maximum)
$gateWidth = [Math]::Max(4, @($gateRows | ForEach-Object { $_.Gate.Length } | Measure-Object -Maximum)[0].Maximum)
$meaningWidth = [Math]::Max(7, @($gateRows | ForEach-Object { $_.Meaning.Length } | Measure-Object -Maximum)[0].Maximum)
Write-AuditLine ("{0}  {1}  {2}  Meaning" -f 'Status'.PadRight($statusWidth), 'Count'.PadLeft($countWidth), 'Gate'.PadRight($gateWidth)) -ForegroundColor Cyan
Write-AuditLine (@(('-' * $statusWidth), ('-' * $countWidth), ('-' * $gateWidth), ('-' * $meaningWidth)) -join '  ') -ForegroundColor Cyan
foreach ($gate in $gateRows) {
    $failing = $gate.Count -gt 0
    $status = if ($failing) { 'FAIL' } else { 'OK' }
    $text = "{0}  {1}  {2}  {3}" -f (" " * ($statusWidth - $status.Length)), $gate.Count.ToString('N0').PadLeft($countWidth), $gate.Gate.PadRight($gateWidth), $gate.Meaning
    Write-AuditLine $text -Lead $status -LeadColor $(if ($failing) { 'Red' } else { 'Green' })
}
$failedGates = @($gateRows | Where-Object { $_.Count -gt 0 })
Write-AuditLine ''
if ($failedGates.Count -eq 0) {
    Write-AuditLine ("PASS: all {0} gates at 0." -f $gateRows.Count) -ForegroundColor Green
}
else {
    $failedSections = ($failedGates | ForEach-Object { '"' + $_.Section + '"' }) -join ', '
    Write-AuditLine ("FAIL: {0} of {1} gates above 0. See {2}." -f $failedGates.Count, $gateRows.Count, $failedSections) -ForegroundColor Red
}

Write-AuditSection -Title 'Kinds'
$kindCells = @($kindRows | ForEach-Object { , @($_.Kind, $_.Hits.Length.ToString('N0'), $_.Ceiling.ToString('N0'), $_.Standing) })
$kindCells += , @('Total', $script:Hits.Count.ToString('N0'), '', '')
$kindHeader = @('Kind', 'Hits', 'Ceiling', 'Standing')
$widths = @(for ($column = 0; $column -lt $kindHeader.Count; $column++) {
    @(@($kindHeader[$column]) + @($kindCells | ForEach-Object { [string]$_[$column] }) | ForEach-Object { $_.Length } | Measure-Object -Maximum)[0].Maximum
})
$rightAligned = @($false, $true, $true, $false)
$kindLine = 0
foreach ($cells in @(, $kindHeader) + @(, @($widths | ForEach-Object { '-' * $_ })) + $kindCells) {
    $parts = for ($column = 0; $column -lt $kindHeader.Count; $column++) {
        if ($rightAligned[$column]) { ([string]$cells[$column]).PadLeft($widths[$column]) } else { ([string]$cells[$column]).PadRight($widths[$column]) }
    }
    if ($kindLine -eq 1) { Write-AuditLine (($parts -join '  ').TrimEnd()) -ForegroundColor Cyan } elseif ($kindLine -eq 0) { Write-AuditLine (($parts -join '  ').TrimEnd()) -ForegroundColor Cyan } else { Write-AuditLine (($parts -join '  ').TrimEnd()) }
    $kindLine++
}

if ($aboveCount -gt 0) {
    Write-AuditSection -Title "Above ceiling ($($aboveCount.ToString('N0')))"
    foreach ($row in $overRows) {
        Write-AuditLine "$($row.Kind): $($row.Hits.Length) hit(s), ceiling $($row.Ceiling)"
    }
}

if ($staleRows.Count -gt 0) {
    Write-AuditSection -Title "Stale ceilings ($($staleRows.Count.ToString('N0')))"
    foreach ($row in $staleRows) {
        Write-AuditLine "$($row.Kind): $($row.Hits.Length) hit(s), ceiling $($row.Ceiling)"
    }
}

foreach ($row in $kindRows) {
    if ($row.Hits.Length -eq 0) {
        continue
    }
    Write-AuditSection -Title "$($row.Kind) ($($row.Hits.Length.ToString('N0')))"
    foreach ($hit in $row.Hits) {
        Write-AuditLine "$($hit.Project)$(Get-HitWhere -Hit $hit -Quote '') - $($hit.Detail)"
    }
}

if ($script:ReadErrors.Count -gt 0) {
    Write-AuditSection -Title "Unreadable files ($($script:ReadErrors.Count.ToString('N0')))"
    foreach ($pair in $script:ReadErrors.GetEnumerator()) {
        Write-AuditLine "$($pair.Key): $($pair.Value)"
    }
}

# The page: every value below is one the console and the Markdown report already show.
$reportDirectoryFull = [System.IO.Path]::GetFullPath($reportFolder)
$hitsByProject = New-Object 'System.Collections.Generic.Dictionary[string,int]' ([System.StringComparer]::OrdinalIgnoreCase)
foreach ($hit in $script:Hits) {
    if ($hitsByProject.ContainsKey($hit.Project)) { $hitsByProject[$hit.Project] = $hitsByProject[$hit.Project] + 1 } else { $hitsByProject[$hit.Project] = 1 }
}
$layerRows = @(@($config.projects.table) | Where-Object { [string]$_.role -eq 'portable' } | ForEach-Object {
    $half = [string]$_.name
    [ordered]@{
        portable = $half
        twins = @(@($config.projects.table) | Where-Object { [string]$_.role -eq 'twin' -and [string]$_.half -eq $half } | ForEach-Object { [string]$_.name })
    }
})
$pageData = [ordered]@{
    project = [string]$config.project
    version = $version
    generation = $script:AuditGeneration
    generated = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss zzz')
    root = $projectRoot
    scanned = [int]$projectFiles.Length
    total = [int]$script:Hits.Count
    enforced = $enforced
    gates = @($gateRows | ForEach-Object {
        [ordered]@{ status = $(if ($_.Count -gt 0) { 'FAIL' } else { 'OK' }); gate = [string]$_.Gate; count = [int]$_.Count; meaning = [string]$_.Meaning; section = [string]$_.Section }
    })
    kinds = @($kindRows | ForEach-Object {
        [ordered]@{
            kind = [string]$_.Kind
            count = [int]$_.Hits.Length
            ceiling = [int]$_.Ceiling
            written = [bool]$_.Written
            standing = [string]$_.Standing
            hits = @($_.Hits | ForEach-Object { [ordered]@{ project = [string]$_.Project; path = [string]$_.Path; line = [int]$_.Line; detail = [string]$_.Detail } })
        }
    })
    above = @(if ($aboveCount -gt 0) { $overRows | ForEach-Object { [ordered]@{ kind = [string]$_.Kind; hits = [int]$_.Hits.Length; ceiling = [int]$_.Ceiling } } })
    stale = @($staleRows | ForEach-Object { [ordered]@{ kind = [string]$_.Kind; hits = [int]$_.Hits.Length; ceiling = [int]$_.Ceiling } })
    unreadable = @($script:ReadErrors.GetEnumerator() | ForEach-Object { [ordered]@{ path = [string]$_.Key; message = [string]$_.Value } })
    scope = [ordered]@{
        projectsRoot = [string]$config.projects.root
        portable = $portableFramework
        twin = $twinFramework
        helper = [string]$config.helper.framework
        layers = $layerRows
        rule = [string]$config.analyzer.rule
        silencers = @($config.analyzer.silencers.PSObject.Properties | ForEach-Object { [string]$_.Name + ' = ' + [string]$_.Value })
        domainExempt = @(@($config.domain.exempt) | ForEach-Object { [string]$_ })
        windowsProperties = @(@($config.windows.properties) | ForEach-Object { [string]$_ })
        windowsPackages = @(@($config.windows.packages) | ForEach-Object { [string]$_ })
        windowsPatterns = @(@($config.windows.patterns) | ForEach-Object { [string]$_ })
        extensions = @(@($config.sources.extensions) | ForEach-Object { [string]$_ })
        excludeSegments = @(@($config.sources.excludeSegments) | ForEach-Object { [string]$_ })
        excludeSuffixes = @(@($config.sources.excludeSuffixes) | ForEach-Object { [string]$_ })
        excludePrefixes = @(@($config.sources.excludePrefixes) | ForEach-Object { [string]$_ })
    }
    projects = @($names | ForEach-Object {
        $name = [string]$_
        $role = Get-ProjectRole -Name $name
        [ordered]@{
            project = $name
            role = $(if ($role -eq '') { 'unmapped' } else { $role })
            column = [string](Get-ProjectColumn -Name $name)
            onDisk = $(if ($facts.ContainsKey($name)) { 'yes' } else { 'no' })
            targets = @(if ($facts.ContainsKey($name)) { $facts[$name].Frameworks | ForEach-Object { [string]$_ } })
            references = @(if ($facts.ContainsKey($name)) { $facts[$name].References | ForEach-Object { [string]$_ } })
            hits = $(if ($hitsByProject.ContainsKey($name)) { [int]$hitsByProject[$name] } else { 0 })
        }
    })
    reports = @(
        [ordered]@{ name = [System.IO.Path]::GetFileName($reportPath); link = ConvertTo-PageLink -Path $reportPath }
    )
}

$utf8 = [System.Text.UTF8Encoding]::new($false)
$pageTemplatePath = Join-Path $PSScriptRoot 'AuditPlatform.html'
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

if (($aboveCount + $staleRows.Count + $script:ReadErrors.Count) -gt 0) {
    exit 1
}

exit 0
