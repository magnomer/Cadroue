# TAuditConvention.cs

## `public void AuditConvention_SettingGenerations_MatchTheTooling()`

The generated registry and the four hand-written settings each carry the generation they were written for.
A mismatch means the tooling moved on while a file did not.
The registry is fixed by running syncnames, and a setting file is fixed by editing it.

## `private void TAuditTokenCheck(string root, string[] forbidden, string[] tolerated, (string TAuditFile, string TAuditSpelling)[] scoped)`

One token rule: every tracked .cs under the root is scanned line by line, literals and comments stripped.
Tolerated spellings are blanked before the forbidden patterns run.
A fully qualified display call therefore passes and a bare one fails.
Any remaining hit fails the run with its file and line, with no baseline to shrink.

## `private void TAuditScalarCheck()`

The Veneer field rule: a mutable scalar field must end in a transient suffix or be a listed draw cache.
A field whose name reads as a guard (Suppress, Busy, Restoring and peers) fails regardless of type.
Struct members, constants, readonly fields, and the composition root are skipped.
