# TAuditTruth.cs

## `public sealed class TAuditTruth`

Keeps ground truth out of the shell.
A shell field may shape the UI or mirror what the engine holds.
It may not carry a value the engine has to be told.
The walk runs once and every fact reads the same hits.
While `TAuditTruthEnforced` is false every fact passes and reports as a warning.
Enforced, a kind fails when it counts above its ceiling, and nothing is waived.

## `private static readonly Lazy<string> TAuditTruthWritten = new(TAuditReportSave);`

The report is written once, on the first fact that runs.

## `public TAuditTruth(ITestOutputHelper output)`

Keeps the runner's output so a passing fact can still print its count.

## `public void AuditTruth_ShellFields_ReachNoRequest()`

A mutable shell field that reaches an engine call, as an argument or as the condition around one.
Also a field the shell toggles before and after a request.
The engine needed the value, so the engine did not hold it.

## `public void AuditTruth_ShellFields_KeepOneWriter()`

A shell field written from an engine result in one place and from the shell in another.
Two writers make a copy that drifts from what the engine holds.

## `public void AuditTruth_ShellFields_HoldNoLogic()`

A shell field or settable property declared with a logic type that is not a handle.
Also a field named as a state, or one that caches the answer to a request.
The shell holds a copy of what the engine already holds.

## `public void AuditTruth_ShellSources_MutateNoLogic()`

A shell line that assigns a logic member, reorders a collection or overrides a built request.

## `public void AuditTruth_ShellShape_DrivesNoRequest()`

A control's state deciding a request, a timer driving one, or a bulletin handler that never reads its bulletin.
The shell decided from its own shape what only a user act or an engine fact should decide.
A shape hit names a control or a member rather than a field.

## `public void AuditTruth_Ceiling_MatchesHits()`

Every ceiling equals its count, so a ceiling left above the count is stale.
This holds even when custody is not enforced.

## `private static int TAuditKindRead(string kind)`

The hits of one kind.

## `private void TAuditTruthCheck(string kind, string summary)`

Prints the count, the ceiling and the report path, then asserts the count under the ceiling when enforced.

## `private static string TAuditReportSave()`

Writes the markdown report under `temp/audit` and returns its repo-relative path.
The report opens with the counts, then the fields by hit count, then every hit by kind.

## `private static IReadOnlyList<TViolation> TAuditTruthRead()`

Enumerates every shell source with Git and runs the walker once, with paths made repo-relative.
The walker compiles all of them and audits the ones under the truth include.
