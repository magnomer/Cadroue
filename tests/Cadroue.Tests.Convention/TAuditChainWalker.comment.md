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

## `public static IReadOnlyList<TAuditHit> TAuditSurfaceScan()`

Walks every public member of each surface type written for a cut pair.
Each signature type from a ring below the neighbour is an `expose` hit.
Nested public types are walked with their members.
The hit sits at the member line and names the cut ring and the deeper ring.

## `public static IReadOnlyList<TAuditHit> TAuditSealScan()`

Walks every public member of each public top-level type a seal prefix names in its ring.
Each signature type from a ring below the ring's neighbour is a `seal` hit.
Nested public types are walked with their members, as the surface scan walks them.
A ring that reaches more or fewer than one ring is skipped, since it has no single neighbour.

## `private static bool TAuditSealCheck(string name, string[] prefixes)`

Whether a type name starts with one of the ring's seal prefixes.

## `private static IEnumerable<ISymbol> TAuditPublicRead(INamedTypeSymbol type)`

Every member of the type and of each public type nested in it.

## `private static IEnumerable<INamedTypeSymbol> TAuditSignatureRead(ISymbol member)`

Every named type in a member signature, with array elements and type arguments unwrapped.
A method of any kind, a property, an event, a field and a nested type's bases each have a signature.

## `private static Dictionary<string, HashSet<string>> TAuditInnerRead()`

The rings each ring may see at any depth: its reach, then the reach of that reach, and so on.

## `private static List<TAuditHit> TAuditTreeScan(SemanticModel model, string relative, string ring, Dictionary<string, HashSet<string>> inner)`

Walks one tree and gives every simple name bound to a type of another ring its verdict.
It is `neighbour` for the ring the table names.
Past the neighbour it is `outward` beyond the closure, `carry` for data and `reach` for behaviour.
A method called on a data type is behaviour, so it is `reach`.
In a cut ring, a name from outside the UI rings is `cross` ahead of `carry`.
A `using` of a deeper ring's namespace counts as data, so it is `cross` above the cut and `carry` below.
One hit per line, kind, target and name, so a name used twice on a line counts once.

## `private static string? TAuditSpaceRead(string space)`

The ring whose name is the longest prefix of the namespace, or null outside every ring.
