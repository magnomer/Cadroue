# TAuditBoundary.cs

## `public sealed class TAuditBoundary`

Keeps the shell on its side of the state rule.
The shell sends what was written and shows what the engine holds.
It never turns text or a chosen id into a stored state itself.
Every name and file the facts hold to lives in `TAuditBoundarySetting`.

## `public void AuditBoundary_ShellSources_BuildNoStateValue()`

Scans every shell source for the calls that read text or an id into a state.
Any hit names the file and line.
The request that should carry the raw field instead is easy to find.

## `public void AuditBoundary_Veneer_ComparesNoState()`

Scans every shell source and markup but the listed converters for a state name.
A panel that needs to know whether a value is unknown asks a verdict rather than reading the state.

## `public void AuditBoundary_ShellSources_HideNothing()`

Scans every shell source and markup for a construct that hides code from the walkers.

## `public void AuditBoundary_PanelSources_ReflectNothing()`

Scans every shell source but the loaders for the reflection namespace.

## `public void AuditBoundary_Tracked_SkipNoSource()`

Lists every tracked file under `src` and `tests` that the audits would skip by segment, suffix or prefix.
A source named like a generated file would otherwise never be walked.

## `public void AuditBoundary_LogicSources_HoldNoPanel()`

Scans every source outside the shell roots for a `P` or `PS` type.
A panel declared in another project would sit outside every shell audit.

## `public void AuditBoundary_HoldSources_KeepNoTimer()`

Scans every file whose name ends in `Hold.cs` or starts with `PEditor` for a timer of its own.
The tenure waits out the typing, so a second wait in the panel would write twice or out of order.

## `public void AuditBoundary_Exempt_MatchesSource()`

Every listed converter still compares a state, every loader still reflects, and every hold pattern names a file.
A row that spares or names nothing is stale and must be removed.

## `private static HashSet<string> TAuditUsedRead(IReadOnlyList<string> forbidden)`

The file names that hold at least one line matching a pattern.

## `private static List<string> TAuditBoundaryScan(Func<string, bool> chosen, IReadOnlyList<string> forbidden)`

The one walk the facts share, over the shell sources and markup the chooser admits.
The shell is whatever the truth and reach includes name, so a new shell assembly joins without an edit here.
An empty enumeration fails rather than passing vacuously.
Each hit is one line naming the file, the line and the pattern found.
The chooser sees the file name and the patterns are regular expressions.

## `private static IReadOnlyList<string> TAuditShellRead(string repoRoot)`

The shell roots as full directory prefixes, read from the truth include patterns.
