# TAuditChainSetting.cs

## `internal static class TAuditChainSetting`

Hand-written and tracked: the ring chain, its cut, its surfaces, its floors, its ceilings and its waivers.
There is no host project, so `TAuditChainHost` names one that does not exist.
No script writes this file, and the chain fact reads no script configuration.
The values mirror `auditstructure.json`, kept by hand, so either tool alone holds the chain.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditChainReach`

Every project under `src`, as its name to the one ring it may reach.
A ring reaches its neighbour alone and carries the data of every ring inside it.
The two adapters reach `Cadroue.Core` as spokes, since the ports belong there.
The core reaches nothing.
The chain is the target, not the present: the ceilings below hold the distance still to walk.

## `public static readonly IReadOnlyDictionary<string, string> TAuditChainCapsule`

For a ring, the one capsule project it may reach beside its neighbour.
Empty here, since no ring keeps a capsule.

## `public static readonly string[] TAuditChainCut`

The UI rings above the cut.
A cut ring names only its neighbour, and nothing from below the cut crosses into it.
It must equal the UI roots of `TAuditTruthSetting.TAuditShellInclude`.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditChainSurface`

For a `ring>neighbour` pair, the neighbour types the ring may name at all.
Empty until the deportment names the engine through ports alone.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditSealPrefix`

For a UI ring, the type prefixes it seals.
Empty here, since no Deportment type is a sealed controller yet.

## `public static readonly IReadOnlyDictionary<string, int> TAuditChainFloor`

The fewest source files a ring may hold.
Raise a floor when a plan lifts another clerk in, never lower it.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditChainStray`

Patterns a ring may not declare a type name under.
Empty here.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditChainBanned`

Words no bound source under a folder may name, matching `banned` in scripts/auditstructure.json.
Empty here.

## `public static readonly IReadOnlyDictionary<string, int> TAuditChainCeiling`

The name count each `kind:ring>target` pair may hold.
The ceilings stood at the generation 16 counts on the day the suite came from Llyn.
A count above fails the fact, a ceiling above the count is stale and fails too.
Lower a ceiling when a ring sheds a file, never raise one to admit a new one.
The adapters, the engine and the shell still name each other directly, so every pair is at its baseline.

## `public static readonly string[] TAuditChainWaiver`

The `path:name` rows that break the chain today, one per break, each deleted by a later plan.
A path ending in `/*` waives a whole folder for one name.
Every row must still match a hit, so a fixed break deletes its row.
