# TAuditFake.cs

## `public sealed class TAuditFake`

Finds the members nothing live reads, which make the code look fuller than it works.
A member read only by tests is the most suspicious, since a green test then proves nothing the app does.
While `TAuditFakeEnforced` is false every fact passes and reports as a warning.
Enforced, a kind fails when it counts above its ceiling.

## `private static readonly Lazy<IReadOnlyList<TViolation>> TAuditFakeRows = new(TAuditFakeRead);`

The walk runs once and every fact reads the same rows.

## `private static readonly Lazy<string> TAuditFakeWritten = new(TAuditReportSave);`

The report is written once, on the first fact that runs.

## `private static readonly Regex TAuditMarkupWord`

One identifier-shaped word in a markup file.

## `public TAuditFake(ITestOutputHelper output)`

Keeps the runner's output so a passing fact can still print its count.

## `public void AuditFake_Members_HoldNoOrphan()`

A member that neither a live reader nor a test reads.
Each hit names the file, line, member and the fake members that alone read it.

## `public void AuditFake_Members_HoldNoTested()`

A member that tests read and nothing live does.
Each hit names the file, line, member and the tests that read it.

## `public void AuditFake_Ceiling_MatchesHits()`

A ceiling above its count is stale and fails, so a shed hit is locked in.

## `private static int TAuditTallyRead(string kind)`

How many rows carry one kind.

## `private void TAuditFakeCheck(string kind, string summary)`

Prints the count and fails when an enforced kind counts above its ceiling.

## `private static IReadOnlyList<TViolation> TAuditFakeRead()`

Enumerates the tracked tests and markup, collects every markup word and runs the walker.
An empty enumeration fails, since the audit would otherwise pass vacuously.

## `private static string TAuditReportSave()`

Writes the counts and every hit, grouped by kind, to the report under `temp`.
