# TAuditTruth.cs

## `public sealed class TAuditTruth`

The deportment rules: the deportment may control the UI but must not decide the data.
A deportment field may shape the UI, but it may not carry a value the engine has to be told.
A deportment line may not compute over engine data, which only the engine decides.
The walk runs once and every fact reads the same hits.
While `TAuditTruthEnforced` is false every fact passes and reports as a warning.
Enforced, a kind fails when it counts above its ceiling, and nothing is waived.

## `private static readonly Lazy<string> TAuditTruthWritten = new(TAuditReportSave);`

The report is written once, on the first fact that runs.

## `private static readonly string[] TAuditTruthKinds`

The hit kinds in report order.

## `private static readonly string[] TAuditLineKinds = ["Mutation", "Treat", "Taint"];`

The kinds whose hits name a line rather than a field, left out of the field table.

## `public TAuditTruth(ITestOutputHelper output)`

Keeps the runner's output so a passing fact can still print its count.

## `public void AuditTruth_DeportmentFields_ReachNoRequest()`

A mutable deportment field that reaches an engine call, as an argument or as the condition around one.
Also a field the deportment toggles before and after a request.
The engine needed the value, so the engine did not hold it.

## `public void AuditTruth_DeportmentFields_KeepOneWriter()`

A deportment field written from an engine result in one place and from the deportment in another.
Two writers make a copy that drifts from what the engine holds.

## `public void AuditTruth_DeportmentFields_HoldNoLogic()`

A deportment field or settable property declared with a logic type that is not a handle.
Also a field named as a state, or one that caches the answer to a request.
The deportment holds a copy of what the engine already holds.

## `public void AuditTruth_DeportmentSources_MutateNoLogic()`

A deportment line that assigns a logic member, reorders a collection or overrides a built request.

## `public void AuditTruth_DeportmentShape_DrivesNoRequest()`

A control's state deciding a request, a timer driving one, or a bulletin handler that never reads its bulletin.
The deportment decided from the UI's shape what only a user act or an engine fact should decide.
A shape hit names a control or a member rather than a field.

## `public void AuditTruth_DeportmentSources_TreatNoData()`

A deportment line that compares, computes or queries over a logic value.
A bare logic verdict deciding a branch is asking the engine and is not treatment.

## `public void AuditTruth_DeportmentLocals_CarryNoData()`

A deportment line that computes over a carried logic value, or over the input of a control.
The value reached the line one hop from logic, so the treat rule alone did not see it.

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

Enumerates every UI source with Git and runs the custody, treat and taint walkers once, with paths made repo-relative.
The walkers compile all of them and audit the ones under the truth include.
