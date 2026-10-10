# TAuditObjectSetting.cs
Hash: `d70a0b16b7450716`

## `internal static class TAuditObjectSetting`

Hand-written and tracked, this file holds the object-audit settings grouped by verdict.
No script writes this file.
`AuditObject.ps1` keeps its own copy in AuditObject.json, and neither side reads the other.
Each threshold dictionary is named for its verdict and ends in `Limit`.
The ratchet holds every name ending in `Limit`, so its values may only fall.
Raising a limit spares more types, which would loosen the audit.
Every limit is reached at its value or above.
Kraken has no dictionary, since it reuses the Serpent, Centipede, Octopus and Spider limits.

## `public const int TAuditGeneration = 21;`

The generation of the audit settings, kept by hand beside the ceilings.

## `public const bool TAuditObjectEnforced = true;`

False makes every flag, hub and part fact a warning that passes.
True fails such a fact when its count rises above its ceiling.

## `public const string TAuditObjectReport = "temp/audit/Object-{0}.md";`

Where the report lands, with the version in the name.
`temp` is ignored by Git, so a run never dirties the tree.

## `public static readonly IReadOnlyDictionary<string, double> TAuditHydraLimit`

The Parts, Lines, Fused and Density a type must reach to be a Hydra.
It needs Parts and Lines, and Fused or Density too.
Fused is Glued once hub state is removed, so one shared field alone never makes a Hydra.
It holds doubles, since Fused and Density are shares.

## `public static readonly IReadOnlyDictionary<string, int> TAuditSpiderLimit`

The Outgoing and Incoming a type must both reach to be a Spider.
Its Outgoing sits below the Octopus Outgoing, so a Spider need not be an Octopus.
Incoming alone flags nothing, since a widely used record or helper is healthy.

## `public static readonly IReadOnlyDictionary<string, int> TAuditChameleonLimit`

The Mutable count at which a type is a Chameleon.

## `public static readonly IReadOnlyDictionary<string, int> TAuditOctopusLimit`

The Outgoing count at which a type is an Octopus.

## `public static readonly IReadOnlyDictionary<string, int> TAuditCentipedeLimit`

The Members count at which a type is a Centipede.

## `public static readonly IReadOnlyDictionary<string, int> TAuditSerpentLimit`

The Lines count at which a type is a Serpent.

## `public static readonly IReadOnlyDictionary<string, int> TAuditHubLimit`

The Parts a state slot must be reached from to be a hub.
Its declaring part counts as reaching it.

## `public static readonly IReadOnlyDictionary<string, int> TAuditObjectCeiling`

The hit count each flag may reach, and the hub slots over every type.
A type counts once under every flag it hits, so a Kraken still counts as an Octopus or Spider.
A count above fails the fact, and a ceiling above the count is stale and fails too.
Lower a ceiling when a type sheds a flag, never raise one to admit a new one.
A key missing here reads as a ceiling of 0.
Each key is kept equal to its twin under `ceiling` in AuditObject.json, by hand.

## `public static readonly IReadOnlyDictionary<string, int> TAuditPartsCeiling`

The parts a split type may be declared in, keyed by its full name.
Every split type is listed at its count, and a type not listed may hold one part.
Dropping a row once its type is whole tightens, so the ratchet lets it go.
Lower a ceiling when a plan lifts a part into its own type, never raise one.

## `public static readonly string[] TAuditObjectInclude`

The `git ls-files` patterns of the sources the walk compiles.
