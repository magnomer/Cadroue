# TAuditConvention.cs

## `public void AuditConvention_SettingGenerations_MatchTheTooling()`

The generated registry and the three hand-written settings each carry the generation they were written for.
A mismatch means the tooling moved on while a file did not.
The registry is fixed by running syncnames, and a setting file is fixed by editing it.
