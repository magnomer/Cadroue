# TAuditChainWalker.cs

## `internal static class TAuditChainWalker`

Reads, for every type one ring names from another, which ring declares it and whether it is data.
The verdict comes from the binder, not from the position of the name in the line.

## `public static IReadOnlyList<TAuditHit> TAuditRun()`

Scans each tree of a ring on its own thread and returns every hit ordered by file and line.

## `public static IReadOnlyList<TAuditHit> TAuditDeclaredRead()`

Every type declared inside a ring, as a `declared` hit naming the ring and the type.

## `public static IReadOnlyDictionary<string, int> TAuditFileRead()`

The source file count of every ring.

## `private static Dictionary<string, HashSet<string>> TAuditInnerRead()`

The rings each ring may see at any depth: its reach, then the reach of that reach, and so on.

## `private static List<TAuditHit> TAuditTreeScan(SemanticModel model, string relative, string ring, Dictionary<string, HashSet<string>> inner)`

Walks one tree and gives every simple name bound to a type of another ring its verdict.
It is `root` in a root file and `neighbour` for the ring the table names.
Past the neighbour it is `outward` beyond the closure, `carry` for data and `reach` for behaviour.
One hit per line, kind, target and name, so a name used twice on a line counts once.
