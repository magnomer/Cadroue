# TAuditRingSetting.cs

## `internal static class TAuditRingSetting`

Hand-written and tracked: the project edge table lives here.
No script writes this file.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditRingEdges`

Every `ProjectReference` under `src`, as project name to referenced project names.
The fact holds the `.csproj` files to this table exactly, so a new edge is an edit here first.
An edge is wider than a reach.
The edges stand as the projects reference each other today, and the chain names where each edge should end.
