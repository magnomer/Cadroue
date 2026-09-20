# TAuditObject.cs

## `public sealed class TAuditObject`

Finds the giant object hiding behind split files.
A partial type can look tidy on disk while the compiler still sees one object.
While `TAuditObjectEnforced` is false every fact passes and reports as a warning.
Enforced, a kind fails when it counts above its ceiling.

## `private static readonly Lazy<IReadOnlyList<TAuditObjectRow>> TAuditObjectRows = new(TAuditObjectRead);`

The walk runs once and every fact reads the same rows.

## `private static readonly Lazy<string> TAuditObjectWritten = new(TAuditReportSave);`

The report is written once, on the first fact that runs.

## `public TAuditObject(ITestOutputHelper output)`

Keeps the runner's output so a passing fact can still print its count.

## `public void AuditObject_SplitTypes_HoldNoMonolith()`

A split type that reaches both floors and is still one object once its hubs are lifted out.
Each hit names the type, its parts, lines, cross references, hub-free weave and density.

## `public void AuditObject_State_HoldNoHub()`

A state slot reached from as many parts as the hub reach allows, or more.
Each hit names the type, the slot and how many parts reach it.

## `public void AuditObject_Parts_HoldWithinCeiling()`

Every type with a part ceiling is split over no more parts than it.

## `public void AuditObject_Ceiling_MatchesHits()`

Every ceiling equals its count, so a ceiling left above the count is stale.
This holds even when the object rules are not enforced.

## `private static Dictionary<string, int> TAuditPartRead()`

The part count of every type a part ceiling names.

## `private void TAuditObjectCheck(string kind, List<string> hits, string summary)`

Prints the count, the ceiling and the report path, then asserts the count under the ceiling when enforced.

## `private static IReadOnlyList<TAuditObjectRow> TAuditObjectRead()`

Enumerates the sources with Git and runs the walker once.
An empty enumeration fails rather than passing vacuously.

## `private static string TAuditReportSave()`

Writes the markdown report under `temp/audit` and returns its repo-relative path.
The report opens with the counts and the rule, then every split type, then every hub.
