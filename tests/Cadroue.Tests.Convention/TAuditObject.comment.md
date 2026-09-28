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
Every fact reads it, so the report exists whichever fact runs first.

## `public TAuditObject(ITestOutputHelper output)`

Keeps the runner's output so a passing fact can still print its count.

## `public void AuditObject_SplitTypes_HoldNoMonolith()`

A split type that reaches both floors and is still one object once its hubs are lifted out.
Each hit names the type, its parts, lines, cross references, hub-free weave and density.

## `public void AuditObject_SingleTypes_HoldNoLarge()`

No more single-part types reach a large threshold than the `Large` ceiling allows.
A large type is the monolith a split would hide, caught before it is split.

## `public void AuditObject_State_HoldNoHub()`

A state slot reached from as many parts as the hub reach allows, or more.
Each hit names the type, the slot and how many parts reach it.

## `public void AuditObject_Parts_HoldWithinCeiling()`

Every split type is spread over no more parts than its ceiling.
A split type without a ceiling may hold one part, so a new partial split fails at once.

## `public void AuditObject_Ceiling_MatchesHits()`

Every ceiling equals its count, so a ceiling left above the count is stale.
This holds even when the object rules are not enforced.

## `private static List<string> TAuditHitRead(string kind)`

One line per hit of a kind, in the words the facts and the report share.
A monolith names its parts, lines, cross references, hub-free weave and density.
A hub names its type, its slot and its reach.
A large type names its lines, members and state slots.

## `private static Dictionary<string, int> TAuditPartRead()`

The part count of every type split over more than one part.

## `private static List<string> TAuditOverRead()`

One line per split type spread over more parts than its ceiling.

## `private static List<string> TAuditAboveRead()`

One line per kind and per split type above its ceiling, and none while not enforced.

## `private static List<string> TAuditStaleRead()`

One line per kind or part ceiling that sits above its count.

## `private void TAuditObjectCheck(string kind, List<string> hits, string summary)`

Prints the count, the ceiling and the report path, then asserts the count under the ceiling when enforced.

## `private static IReadOnlyList<TAuditObjectRow> TAuditObjectRead()`

Enumerates the sources with Git and runs the walker once.
An empty enumeration fails rather than passing vacuously.
A list the binder walks none of fails too, as auditobject.ps1 does.

## `private static string TAuditReportSave()`

Writes the markdown report under `temp/audit` and returns its repo-relative path.
The report opens with the counts, the ceilings and the rule, then every split type.
Every monolith, hub, large type, overrun and stale ceiling follows in its own section.
The text matches the report of auditobject.ps1 line for line, so a diff of the two shows any drift.
Lines end in LF alone, as every file in the repository does.
