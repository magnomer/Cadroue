# TAuditStrict.cs

## `public sealed class TAuditStrict`

The veneer rules: the veneer is what the user sees, and a veneer member only calls a function.
A veneer holds no state, never branches or computes, and never reaches the engine.
The deportment drives the veneer and is its only path to the engine.
While `TAuditStrictEnforced` is false every fact passes and reports as a warning.
Enforced, a kind fails when it counts above its ceiling.

## `private static readonly Lazy<(IReadOnlyList<TViolation> TAuditHits, IReadOnlyList<string> TAuditVeneers)>`

The walk runs once and every fact reads the same hits and the same veneer list.

## `private static readonly Lazy<string> TAuditStrictWritten = new(TAuditReportSave);`

The report is written once, on the first fact that runs.

## `public TAuditStrict(ITestOutputHelper output)`

Keeps the runner's output so a passing fact can still print its count.

## `public void AuditStrict_VeneerFields_HoldNothing()`

A field, event field, auto-property or primary constructor parameter in a veneer type, or a mutable static anywhere.

## `public void AuditStrict_VeneerMembers_CallOnly()`

A veneer line that is not a plain call: a branch, a loop, an operator, an assignment or a declaration.

## `public void AuditStrict_VeneerSources_ReachNoEngine()`

A veneer line that names an engine symbol or holds a value of an engine type.

## `public void AuditStrict_VeneerMarkup_ReachNoEngine()`

A markup line that maps an engine namespace, reads an engine constant, or names an engine member.

## `public void AuditStrict_VeneerMarkup_BranchNever()`

A markup trigger, or a binding that converts, formats or falls back.
Each is a branch or a computation standing in the veneer.

## `private static readonly string[] TAuditStrictKinds = ["Storage", "Call", "Engine", "Reach", "Trigger"];`

The hit kinds in report order.

## `public void AuditStrict_DeportmentSources_ReachNoMarkup()`

A deportment line that names the file system, outside the exempt files.
Deportment uses WPF freely, but disk work stays behind the engine's ports.

## `public void AuditStrict_VeneerSources_HoldNoCatalog()`

A veneer line that reads a file, parses JSON, runs a regex, starts a process or starts a task.
Each of those is work the engine or `LUsher` does, and the veneer only asks for the answer.
The files whose stream use is the framework's own are exempt by name.

## `public void AuditStrict_Ceiling_MatchesHits()`

Every ceiling equals its count, so a ceiling left above the count is stale.
This holds even when the strict rules are not enforced.

## `private void TAuditStrictCheck(string kind, string summary)`

Prints the count, the ceiling and the report path, then asserts the count under the ceiling when enforced.

## `private static readonly Regex TAuditLiteralPattern`

A string literal or a line comment, the text the source scan reads past.

## `private static List<string> TAuditSourceScan(IReadOnlyList<string> include, IReadOnlyList<string> forbidden, IReadOnlyList<string> exempt)`

Every line of every included source that matches a forbidden pattern, outside the exempt files.
A string literal and a line comment are blanked first, so a word inside a message is not a hit.
No hit count is kept for these, since both start at zero and stay there.

## `private static (IReadOnlyList<TViolation> TAuditHits, IReadOnlyList<string> TAuditVeneers) TAuditStrictRead()`

Enumerates the UI sources and the veneer markup with Git and runs both walkers once, with paths made repo-relative.

## `private static string TAuditReportSave()`

Writes the markdown report under `temp/audit` and returns its repo-relative path.
The report opens with the counts, then a per-veneer table, then every hit by kind.

## `private static int TAuditClassRead(IReadOnlyList<TViolation> hits, string type, string kind)`

The hits of one kind whose name starts with the type.
