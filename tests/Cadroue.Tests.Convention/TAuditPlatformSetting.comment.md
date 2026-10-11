# TAuditPlatformSetting.cs
Hash: `3cfffe8a27215272`

## `internal static class TAuditPlatformSetting`

Hand-written and tracked.
The platform table, the frameworks, the Windows markers and the ceilings live here.
No script writes this file.
`AuditPlatform.ps1` reads its own AuditPlatform.json and never writes this file.

## `public const bool TAuditPlatformEnforced = true;`

False lets the ceiling fact pass above its ceilings.
True fails it when a kind counts above its ceiling.
Unreadable files and stale ceilings fail the facts either way.

## `public const string TAuditPlatformRoot = "src/";`

The folder every project lives under.

## `public const string TAuditPlatformPortable = "net10.0";`

The one framework a portable half targets, matched exactly.

## `public const string TAuditPlatformTwin = "net10.0-windows";`

The framework a twin targets, matched as a prefix so a Windows version may follow.

## `public const string TAuditPlatformRule = "CA1416";`

The platform analyzer rule that must be an error in every portable half.
With it, a Windows API in a portable half fails the build.

## `public static readonly string[] TAuditPlatformShell`

The columns whose twins are the UI projects, which the domain rule leaves to the UI audits.
Deportment and the Veneer both sit in the Conduct column.
It matches `domain.exempt` in scripts/AuditPlatform.json.

## `public static readonly IReadOnlyDictionary<string, string> TAuditPlatformSilencers`

The project properties, with the value that switches the platform analyzer off.

## `public static readonly string[] TAuditPlatformKinds`

Every kind the walk reports, in report order.

## `public static readonly IReadOnlyDictionary<string, string> TAuditPlatformColumn`

The target platform table, as project name to the portable half of its column.
A portable half is its own column.
A twin names its portable half.
The host has an empty column because it names every project.
Rows for projects Cadroue lacks, such as `Cadroue.Core.Windows` and `Cadroue.UIDeportment.Capsule`, count as `Absent`.
`Cadroue.Media` has no row, so it reports as unmapped.

## `public static readonly IReadOnlyDictionary<string, string> TAuditPlatformCapsule`

For a twin, the one capsule project it may reference outside its column.
The capsule keeps its own portable column, so that reference is no `Column` hit.
Cadroue has no `Cadroue.UIDeportment.Capsule` project, so the row grants nothing.

## `public static readonly string[] TAuditPlatformProperties`

Project properties that pull a Windows desktop framework into a project.

## `public static readonly string[] TAuditPlatformPackages`

Package identifiers that only run on Windows.

## `public static readonly string[] TAuditPlatformPatterns`

Source patterns that name a Windows API, a native import or a platform guard.

## `public static readonly IReadOnlyDictionary<string, int> TAuditPlatformCeiling`

The hit count each kind may reach.
Each caps the platform breaks Cadroue's projects hold today, since none is split into twins yet.
A count above fails the fact, a ceiling above the count is stale and fails too.
Lower a ceiling when a project sheds a hit, never raise one to admit a new one.
