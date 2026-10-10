# TAuditCharterSetting.cs
Hash: `da68a9761a892d66`

## `internal static class TAuditCharterSetting`

Hand-written and tracked.
The project edge table lives here.
No script writes this file.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditCharterEdges`

Every `ProjectReference` under `src`, as project name to referenced project names.
The fact holds the `.csproj` files to this table exactly, so a new edge is an edit here first.
An edge is wider than a neighbour.
The engine references the application alone and ferries core records through it.

## `public static readonly IReadOnlyDictionary<string, int> TAuditCharterCeiling`

The count each charter kind may hold.
`Piggybacking` counts the cut projects that still compile against rings past their neighbour.
Every cut project disables transitive project references, so the ceiling holds at zero.
A count above fails the fact, a ceiling above the count is stale and fails too.
