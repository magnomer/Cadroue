# TAuditCharterSetting.cs
Hash: `3b4337431df2ff84`

## `internal static class TAuditCharterSetting`

Hand-written and tracked.
The project edge table and the charter ceiling live here.
No script writes this file.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditCharterEdges`

Every `ProjectReference` under `src`, as project name to referenced project names.
The fact holds the `.csproj` files to this table exactly, so a new edge is an edit here first.
An edge is wider than a neighbour.
The rows still carry rings Cadroue lacks, such as `Cadroue.UIDemeanor` and `Cadroue.UITerminal`.
Those rows match no project, so the Rerouting fact stays red until the table follows the real edges.

## `public static readonly IReadOnlyDictionary<string, int> TAuditCharterCeiling`

The count each charter kind may hold.
`Piggybacking` counts the cut projects that leave transitive project references on.
Such a project compiles against rings past its neighbour without naming them.
A count above fails the fact, a ceiling above the count is stale and fails too.
Lower it when a cut project disables transitive references, never raise it.
