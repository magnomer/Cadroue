# TAuditRing.cs

## `public sealed class TAuditRing`

Keeps the project edges as written: every `ProjectReference` under `src` matches the ring table.
What a source may name across an edge is the chain's concern, not this one.

## `private const string TAuditRingSource = "src/";`

The folder every project lives under.

## `private static readonly Regex TAuditRingReference`

A `ProjectReference` as it stands in a `.csproj`, with the referenced project name captured.

## `public void AuditRing_Projects_ReferenceInward()`

Reads every `.csproj` under `src` and holds its edges to the ring table exactly.
An edge added or dropped without an edit to the table fails.

## `private static string[] TAuditEdgeRead(string csproj)`

The project names one `.csproj` references, sorted.
