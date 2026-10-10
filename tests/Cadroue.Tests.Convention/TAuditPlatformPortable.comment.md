# TAuditPlatformPortable.cs
Hash: `691f001f8ce04da3`

## `internal static class TAuditPlatformPortable`

Scans one portable half for what would let Windows code into it unseen.
`TAuditPlatform` calls it for every project the table names as portable.

## `private static readonly Regex TAuditPragmaPattern`

A `#pragma warning disable` line.

## `private static readonly Regex TAuditBarePattern`

A disable that names no rule, which silences every rule the platform one included.

## `public static IEnumerable<TAuditHit> TAuditSuppressRead(string repoRoot, string project, IEnumerable<string> held, string name)`

Every way a portable half silences the platform rule.
`NoWarn` naming it, an analyzer switched off, a `#pragma` disabling it or every rule, or a `SuppressMessage` naming it.

## `public static IEnumerable<TAuditHit> TAuditSourceScan(string repoRoot, string source, string project)`

Every line of one portable source that names a Windows API.
A line is reported once even when several patterns match it.
