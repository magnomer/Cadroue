# TAuditPlatform.cs
Hash: `238370aaf071caf6`

## `public sealed class TAuditPlatform`

Holds every project under `src` to the platform table.
A layer splits into a portable half and a Windows twin, and this fact keeps that split.
The layer chain itself is the charter and border facts' concern.
`scripts/AuditPlatform.ps1` reports the same kinds from its own configuration.
The two never read each other, yet they give the same counts, hits and wording.
Project names, frameworks, packages, property names and rule codes compare without case, as MSBuild does.

This class classifies each project against the table and judges the counts.
`TAuditPlatformPortable` scans a portable half's sources for suppressions and Windows names.
`TAuditPlatformAnalyzer` decides whether the platform rule is an error for one source.
`TAuditPlatformProject` reads properties from project files, and `TAuditPlatformFile` finds and reads the files themselves.

## `private const string TAuditProjectExtension = ".csproj";`

The extension that marks a project file among the enumerated sources.

## `private static readonly Lazy<IReadOnlyList<TAuditHit>> TAuditPlatformHits = new(TAuditPlatformRead);`

The walk runs once and both facts read the same hits.

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
A twin may also reference the capsule project the capsule table names for it.
A portable half is also read for suppressions of the platform rule.
The analyzer hit names the first failing file by path, ordered without case and then exactly.
A property and a package are reported once each, however often the file names them.
A twin outside the UI column is read for types that implement nothing of its portable half.
Every project file and import is read for implicit usings, which the binder would miss.

## `private static string TAuditRowRead(TAuditHit hit)`

One hit as the script prints it, the project first, then the path and line when known, then the text.

## `private static IEnumerable<TAuditHit> TAuditDomainRead(string repoRoot, string column, IEnumerable<string> held, string name)`

Every top-level type of a twin with no interface or base type declared in its portable half.
A twin only maps a port call to a Windows API and back.
So a type serving no port holds domain logic.
The UI column is left to the driver and surface audits.

## `private static bool TAuditWindowsCheck(string project, IReadOnlyDictionary<string, string> table, IReadOnlyDictionary<string, string> projects)`

True for a twin in the table, or for any project whose file targets a Windows framework.
