# TAuditConvention.cs

## `public sealed class TAuditConvention`

Holds the generation this checkout of the convention tests applies.
A generation names the set of checks the audits apply, not a count of edits.
Every audit and every settings sidecar share one number, so a report from any of them compares across projects.

## `public static string TAuditReportFormat(string audit, string report)`

Puts the audit name and the generation on the first line of every audit report.
The stamp matches the generation the registry and each setting file carry, so reports compare by one grep.

## `public const int TAuditGeneration`

The generation of this tooling.
Raise it only when a check is added, removed, or changed in what it reports.

## `public void AuditConvention_SettingGenerations_MatchTheTooling()`

Reads the generation the generated registry and each hand-written setting carry and compares it with this one.
A mismatch means the tooling moved on while a file did not.
The registry is fixed by regenerating it, and a setting file is fixed by editing it.
