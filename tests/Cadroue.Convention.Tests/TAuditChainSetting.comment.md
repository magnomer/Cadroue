# TAuditChainSetting.cs

## `internal static class TAuditChainSetting`

Hand-written and tracked: the ring chain, its root, its surfaces, its floors, its ceilings and its waivers.
No script writes this file, and the chain fact reads no script configuration.
The values mirror `auditstructure.json`, kept by hand, so either tool alone holds the chain.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditChainReach`

Every project under `src`, as its name to the one ring it may reach.
A ring reaches its neighbour alone and carries the data of every ring inside it.
The two adapters reach `Cadroue.Core` as spokes, since the ports belong there.
The core reaches nothing.
The chain is the target, not the present: the ceilings below hold the distance still to walk.

## `public static readonly string[] TAuditChainRoot`

The composition root files, free of the chain.
`App.xaml.cs` builds the engine and its adapters until a factory below the shell takes that over.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditChainSurface`

For a `ring>neighbour` pair, the neighbour types the ring may name at all.
Empty until the deportment names the engine through ports alone.

## `public static readonly IReadOnlyDictionary<string, int> TAuditChainFloor`

The fewest source files a ring may hold.
Raise a floor when a plan lifts another clerk in, never lower it.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditChainStray`

Patterns a ring may not declare a type name under.
Empty here.

## `public static readonly IReadOnlyDictionary<string, int> TAuditChainCeiling`

The file count each `kind:ring>target` pair may hold.
A count above fails the fact, a ceiling above the count is stale and fails too.
Lower a ceiling when a ring sheds a file, never raise one to admit a new one.
The adapters, the engine and the shell still name each other directly, so every pair is at its baseline.

## `public static readonly string[] TAuditChainWaiver`

The `path:name` rows that break the chain today, one per break, each deleted by a later plan.
A path ending in `/*` waives a whole folder for one name.
Every row must still match a hit, so a fixed break deletes its row.
