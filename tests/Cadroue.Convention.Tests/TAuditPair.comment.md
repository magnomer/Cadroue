# TAuditPair.cs

## `internal static class TAuditPair`

The ceiling arithmetic every bound audit shares.
A pair is one kind from one ring to one target, counted in files.
Each pair is held to the ceiling written for it.

## `public static void TAuditPairCheck(string audit, IReadOnlyList<TAuditHit> hits, string kind, IReadOnlyDictionary<string, int> ceilings, IReadOnlyList<string> waivers, string summary)`

Counts the files of every pair under one kind and fails when a pair is above its ceiling.
An unwritten ceiling is zero, and the hits of an over pair are listed under it.

## `public static void TAuditStaleCheck(string audit, IReadOnlyList<TAuditHit> hits, IReadOnlyList<string> kinds, IReadOnlyDictionary<string, int> ceilings, IReadOnlyList<string> waivers)`

Fails when a ceiling sits above its count, so a shed file lowers its ceiling.

## `public static void TAuditWaiverCheck(string audit, IReadOnlyList<TAuditHit> hits, IReadOnlyList<string> waivers)`

Fails when a waiver row matches no hit, so a fixed break deletes its row.
