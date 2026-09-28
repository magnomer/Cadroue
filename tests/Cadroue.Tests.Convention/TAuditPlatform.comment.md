# TAuditPlatform.cs

## `public sealed class TAuditPlatform`

Holds every project under `src` to the platform table.
A layer splits into a portable half and a Windows twin, and this fact keeps that split.
The layer chain itself is the ring and chain facts' concern.
`scripts/auditplatform.ps1` reports the same kinds from its own configuration.
The two never read each other, yet they give the same counts, hits and wording.
Project names, frameworks, packages, property names and rule codes compare without case, as MSBuild does.

## `private const string TAuditProjectExtension = ".csproj";`

The extension that marks a project file among the enumerated sources.

## `private static readonly string[] TAuditImportNames`

The files MSBuild imports on its own from every folder above a project.

## `private static readonly string[] TAuditConfigNames`

The analyzer configuration files the compiler reads from every folder above a project.

## `private static readonly SortedDictionary<string, string> TAuditUnreadable = new(StringComparer.Ordinal);`

Every file that could not be read, keyed by its full path, with the reason.
A file read many times is still one unreadable file.

## `private static readonly Lazy<IReadOnlyList<TAuditHit>> TAuditPlatformHits = new(TAuditPlatformRead);`

The walk runs once and both facts read the same hits.

## `private static readonly Regex TAuditPragmaPattern`

A `#pragma warning disable` line.

## `private static readonly Regex TAuditBarePattern`

A disable that names no rule, which silences every rule the platform one included.

## `private static readonly Regex TAuditSectionPattern`

A section header of an analyzer configuration file, with its glob captured.

## `private static readonly Regex TAuditPairPattern`

A key and value line of an analyzer configuration file.

## `public TAuditPlatform(ITestOutputHelper output)`

Keeps the runner's output so a passing fact can still print its counts.

## `public void AuditPlatform_Projects_HoldWithinCeiling()`

Every kind counts no more hits than its ceiling, and every file was read.
An unwritten ceiling is zero, and the hits of an over kind are listed under it.
The hits are ordered by project, path, line and text, as the script prints them.
An unreadable file fails the fact even when the ceilings are not enforced.

## `public void AuditPlatform_Ceiling_MatchesHits()`

Every ceiling equals its count, so a ceiling left above the count is stale.

## `private static IReadOnlyList<TAuditHit> TAuditPlatformRead()`

Reads every project file and source under `src` and classifies each break of the split.
Two project files that share one name stop the audit, since no table row could tell them apart.
A hit carries the project as its ring and the kind's subject as its target.
An unmapped project is reported once and never checked further.
The host may reference every project, so its edges are never read.
A portable half is also read for suppressions of the platform rule.
The analyzer hit names the first failing file by path, ordered without case and then exactly.
A property and a package are reported once each, however often the file names them.
A twin outside the UI column is read for types that implement nothing of its portable half.
Every project file and import is read for implicit usings, which the binder would miss.

## `private static string TAuditRowRead(TAuditHit hit)`

One hit as the script prints it, the project first, then the path and line when known, then the text.

## `private static IEnumerable<string> TAuditImportRead(string repoRoot)`

The tracked `Directory.Build` imports anywhere in the tree.

## `private static IEnumerable<TAuditHit> TAuditSuppressRead(`

Every way a portable half silences the platform rule.
`NoWarn` naming it, an analyzer switched off, a `#pragma` disabling it or every rule, or a `SuppressMessage` naming it.

## `private static IEnumerable<TAuditHit> TAuditDomainRead(`

Every top-level type of a twin that implements no interface and derives from no type of its portable half.
A twin only maps a port call to a Windows API and back.
So a type serving no port holds domain logic.
The UI column is left to the driver and surface audits.

## `private static IEnumerable<TAuditHit> TAuditSourceScan(string repoRoot, string source, string project)`

Every line of one portable source that names a Windows API.
A line is reported once even when several patterns match it.

## `private static bool TAuditWindowsCheck(`

True for a twin in the table, or for any project whose file targets a Windows framework.

## `private static bool TAuditAnalyzerCheck(string repoRoot, string project, string source)`

True when the platform rule is an error for one source file.
Global configurations apply first, then editor configurations from the outermost to the nearest.
A folder above a `root = true` configuration is not read.
Only a section whose glob matches the file applies, and the last matching severity wins.
The rule escalates when a project file lists it as an error.
It also escalates when every warning is an error and the rule is not spared.
A severity of error always holds, a severity of warning holds only when escalated, and a lower severity never holds.
Without any severity, the rule holds only when escalated, since a warning is its default.

## `private static bool TAuditTrueCheck(string value)`

MSBuild reads a boolean property without regard to case, so the check does too.

## `private static string[] TAuditTextRead(string path)`

The lines of one file, or none when it cannot be read.
An unreadable file is recorded rather than thrown, so the fact lists every one.

## `private static string? TAuditLevelRead(string config, string source, string key)`

The last value one configuration gives the key for the file, or null when it gives none.
A global configuration applies to every file.

## `private static bool TAuditGlobCheck(string glob, string relative)`

True when an editor configuration glob matches the file's path relative to the configuration.
A glob without a slash matches the file in any folder below.

## `private static bool TAuditListCheck(XDocument document, string element, string rule)`

True when one property of a project file lists the rule among its codes.
Codes split at semicolons, commas and white space as the compiler reads them, and compare without case.

## `private static IEnumerable<string> TAuditChainRead(string repoRoot, string project, string[] names)`

The named files from the project's folder up to the repository root, nearest first.

## `private static string[] TAuditFrameworkRead(XDocument document)`

Every framework one project file targets, whether written singly or as a list.

## `private static IEnumerable<string> TAuditValueRead(XDocument document, string element)`

The trimmed text of every element with that name in any case and any namespace.

## `private static IEnumerable<string> TAuditIncludeRead(XDocument document, string element)`

The `Include` value of every element with that name in any case.

## `private static string TAuditRelativeRead(string repoRoot, string path)`

A path relative to the repository root with forward slashes.
