# TAuditConvention.cs

## `public void AuditConvention_SettingGenerations_MatchTheTooling()`

The generated registry and the four hand-written settings each carry the generation they were written for.
A mismatch means the tooling moved on while a file did not.
The registry is fixed by running syncnames, and a setting file is fixed by editing it.

## `private void TAuditGateCheck(string root, string[] forbidden, string[] known)`

One gate rule: every tracked .cs under the root is scanned for the forbidden tokens.
A file on the known list is reported as known, not as a failure, until plans 03-04 empty it.
A new offender fails the run, and a cleared baseline entry is printed so the list can shrink.
