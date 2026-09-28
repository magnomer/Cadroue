# TAuditLine.cs

## `public sealed class TAuditLine`

Holds every source file within the line limit and every line within the width limit of its extension.
A file or line in the band below a limit prints as a warning and does not fail.
Enforced, a kind fails when it counts above its ceiling.
The advice printed with a hit says to reconsider the code, never to split or wrap it mechanically.
Every hit, warning and ceiling matches auditlines.ps1 on the same tree, though neither reads the other.

## `private static readonly Lazy<IReadOnlyList<TAuditLineRow>> TAuditLineRows = new(TAuditLineRead);`

The walk runs once and every fact reads the same rows.

## `public TAuditLine(ITestOutputHelper output)`

Keeps the runner's output so a passing fact can still print its count and warnings.

## `public void AuditLine_File_HoldWithinLength()`

A file above the line limit is a hit, largest first.
A file above the warning line but within the limit is a warning.

## `public void AuditLine_Line_HoldWithinWidth()`

A line wider than the limit of its extension is a hit, named by file and line number.
A line within the width band below the limit is a warning.
An extension without a width limit is never checked.

## `public void AuditLine_Ceiling_MatchesHits()`

Every ceiling equals its count, so a ceiling left above the count is stale.
This holds even when the line rules are not enforced.

## `private const string TAuditLengthAdvice`

Why a long file must not be split mechanically or given a partial part.
Both leave one big object behind several files, which the object audit then reports.

## `private const string TAuditWidthAdvice`

Why a wide line must not be wrapped mechanically.
Width is a sign of depth, chaining or a long path, and the code is reconsidered first.

## `private static Dictionary<string, int> TAuditHitRead()`

The hit count of each kind, for the ceiling fact.

## `private void TAuditWarningRecord(List<string> warnings, string summary, string advice)`

Prints the warnings with their advice, and nothing when there are none.

## `private void TAuditLineCheck(string kind, List<string> hits, string summary, string advice)`

Prints the count and the ceiling, then asserts the count under the ceiling when enforced.
A failure carries the advice before the hits.

## `private static IReadOnlyList<TAuditLineRow> TAuditLineRead()`

A configured root without a directory fails first, as it does in the script.
Enumerates the sources with Git and reads every file once.
Each row keeps the line count and every line inside the width band or above it.
An empty enumeration fails rather than passing vacuously.
