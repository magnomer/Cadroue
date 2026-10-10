# TAuditBorderDrift.cs
Hash: `80233353cf79d45a`

## `internal static class TAuditBorderDrift`

Holds each offer list to the public types its neighbour ring really declares.
A list that drifts from the code is a `Drifting` hit.

## `public static IReadOnlyList<TAuditHit> TAuditDriftScan()`

Reads every source type with its name, its ring and whether it is public through every containing type.
It merges the three offer settings and hands them with the cut, the prefixes and those types to `TAuditDriftRead`.

## `public static IReadOnlyList<TAuditHit> TAuditDriftRead(IReadOnlyDictionary<string, string[]> offers, IReadOnlyCollection<string> cut, IReadOnlyDictionary<string, string[]> prefixes, IReadOnlyList<(string TAuditDriftName, string? TAuditDriftRing, bool TAuditDriftPublic)> types)`

The pure drift comparison, which binds nothing, so a specimen can feed it a hand-written list.
Clause (a) holds every entry to exactly one public type of the neighbour carrying its prefix.
Each entry reports only its first failing check, so one broken entry gives one hit.
Clause (b) holds a cut pair to every public type of its neighbour.
It reports only the unlisted types, since (a) already catches every listed type that is not public there.
Clause (c) holds pairs sharing a neighbour to one list, reporting each entry the other pair adds.
The engine pair sits outside the cut, so it skips (b) and may omit engine types.
Every hit sits at the neighbour folder with line zero, as the script places it.

## `private static bool TAuditPublicCheck(INamedTypeSymbol type)`

Whether the type and every type containing it are declared public, so the type is reachable from outside.

## `internal static IEnumerable<ISymbol> TAuditPublicRead(INamedTypeSymbol type)`

Every member of the type and of each public type nested in it.
The offer and seal scans of `TAuditBorderWalker` walk members through it, so it is internal.
