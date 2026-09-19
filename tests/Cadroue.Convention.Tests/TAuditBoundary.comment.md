# TAuditBoundary.cs

## `public sealed class TAuditBoundary`

Keeps the shell where the Roslyn walkers can see it.
A construct the walkers cannot follow would let a hit hide, so the shell may not use one.

## `private static readonly string[] TAuditBoundaryHidden =`

The constructs that keep code out of a syntax walk: a preprocessor branch, reflection, `dynamic`, inline markup code.
Also an enum parsed from text and a `using` alias, which each give a name a second spelling.
The walkers parse without symbols, so a branch would be skipped, and reflection names nothing the walk can follow.

## `private const string TAuditBoundaryReflection = @"\bSystem\.Reflection\b";`

The namespace that reaches a member by its name as text.

## `public void AuditBoundary_ShellSources_HideNothing()`

Scans every shell source and markup for a construct that hides code from the walkers.

## `public void AuditBoundary_PanelSources_ReflectNothing()`

Scans every shell source for the reflection namespace.
The version and the embedded catalogs are read below the shell, so no shell file needs it.

## `public void AuditBoundary_Tracked_SkipNoSource()`

Lists every tracked file under `src` and `tests` that the audits would skip by segment, suffix or prefix.
A source named like a generated file would otherwise never be walked.

## `public void AuditBoundary_LogicSources_HoldNoPanel()`

Scans every source outside the shell roots for a `P` or `PS` type.
A panel declared in another project would sit outside every shell audit.

## `private static List<string> TAuditBoundaryScan(Func<string, bool> chosen, IReadOnlyList<string> forbidden)`

The one walk the facts share, over the shell sources and markup the chooser admits.
The shell is whatever the truth and reach includes name, so a new shell assembly joins without an edit here.
An empty enumeration fails rather than passing vacuously.
Each hit is one line naming the file, the line and the pattern found.
The chooser sees the file name and the patterns are regular expressions.

## `private static IReadOnlyList<string> TAuditShellRead(string repoRoot)`

The shell roots as full directory prefixes, read from the truth include patterns.
