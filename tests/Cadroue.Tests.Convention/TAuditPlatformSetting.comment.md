# TAuditPlatformSetting.cs

## `internal static class TAuditPlatformSetting`

Hand-written and tracked: the platform table, the frameworks, the Windows markers and the ceilings live here.
No script writes this file.
`auditplatform.ps1` reads its own auditplatform.json and never writes this file.

## `public const bool TAuditPlatformEnforced = true;`

False makes every platform fact a warning that passes.
True fails a fact when a kind counts above its ceiling.

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
It matches `domain.exempt` in scripts/auditplatform.json.

## `public static readonly IReadOnlyDictionary<string, string> TAuditPlatformSilencers`

The project properties, with the value that switches the platform analyzer off.

## `public static readonly string[] TAuditPlatformKinds`

Every kind the walk reports, in report order.

## `public static readonly IReadOnlyDictionary<string, string> TAuditPlatformColumn`

The target platform table, as project name to the portable half of its column.
A portable half is its own column.
A twin names its portable half.
The host has an empty column because it names every project.
Twin naming is not settled, so a twin's name here is provisional.

## `public static readonly string[] TAuditPlatformProperties`

Project properties that pull a Windows desktop framework into a project.

## `public static readonly string[] TAuditPlatformPackages`

Package identifiers that only run on Windows.

## `public static readonly string[] TAuditPlatformPatterns`

Source patterns that name a Windows API, a native import or a platform guard.

## `public static readonly IReadOnlyDictionary<string, int> TAuditPlatformCeiling`

The hit count each kind may reach.
The ceilings stood at the counts on the day the fact was written.
A count above fails the fact, a ceiling above the count is stale and fails too.
Lower a ceiling when a project sheds a hit, never raise one to admit a new one.
