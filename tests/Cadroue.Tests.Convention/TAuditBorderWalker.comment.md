# TAuditBorderWalker.cs
Hash: `24d841dbc314714b`

## `internal static class TAuditBorderWalker`

Reads, for every type one ring names from another, which ring declares it and whether it is data.
The verdict comes from the binder, not from the position of the name in the line.

## `public static IReadOnlyList<TAuditHit> TAuditRun()`

Scans each tree of a ring on its own thread and returns every hit ordered by file and line.

## `public static IReadOnlyList<TAuditHit> TAuditOfferScan()`

Walks every public member of each offer type written for a cut pair.
Each signature type from a ring below the neighbour is a `Leaking` hit.
Nested public types are walked with their members.
The hit sits at the member line and names the cut ring and the deeper ring.

## `public static IReadOnlyList<TAuditHit> TAuditSealScan()`

Walks every public member of each public top-level type declared in a ring `TAuditUnsealingRing` lists, whatever its prefix.
Each signature type from a ring below the ring's neighbour is an `Unsealing` hit.
Nested public types are walked with their members, as the offer scan walks them.
A ring that names more or fewer than one neighbour is skipped, since it has no single neighbour.

## `private static IEnumerable<INamedTypeSymbol> TAuditSignatureRead(ISymbol member)`

Every named type in a member signature, with array elements and type arguments unwrapped.
A plain method, a property, an event, a field and a nested type's bases each have a signature.
A property accessor is covered by its property.

## `internal static Dictionary<string, HashSet<string>> TAuditInnerRead()`

The rings each ring may see at any depth, which are its neighbour, that neighbour's neighbour, and so on.
The linger scan reads it too, so it is internal.

## `private static List<TAuditHit> TAuditTreeScan(SemanticModel model, string relative, string ring, Dictionary<string, HashSet<string>> inner)`

Walks one tree and gives every simple name bound to a type of another ring its verdict.
It is `Commuting` for the neighbour the table names and for the ring's capsule.
Past the neighbour it is `Trespassing` beyond the closure, `Ferrying` for data and `Leapfrogging` for behaviour.
A method called on a data type counts as behaviour, not data.
In a cut ring, a name from outside the UI rings is `Undercutting` ahead of `Ferrying` and `Leapfrogging`.
A `using` of a deeper ring counts as data.
It is `Undercutting` from a cut ring to an uncut one and `Ferrying` otherwise.
One hit per line, kind, target and name, so a name used twice on a line counts once.

## `private static string? TAuditSpaceRead(string space)`

The ring whose name is the longest prefix of the namespace, or null outside every ring.
