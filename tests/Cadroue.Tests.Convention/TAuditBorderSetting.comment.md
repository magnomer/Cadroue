# TAuditBorderSetting.cs
Hash: `bad21c1428b06c03`

## `internal static class TAuditBorderSetting`

Hand-written and tracked settings for the ring structure of the border.
They hold the neighbours, the capsule, the cut, the offer and sealed prefixes, the ceilings and the exempt rows.
The offers each pair may name live in `TAuditEngineSetting`, `TAuditDeportmentSetting` and `TAuditDemeanorSetting`.
No script writes this file, and the border fact reads no script configuration.
`AuditStructure.json` holds its own copy, kept in step by hand as scripts/principles.md asks.
The neighbours and the cut are tied to the charter and the UI roots by facts, not by hand.

## `public const int TAuditGeneration = 21;`

Numbers the revision of the border settings this file holds.

## `public const string TAuditBorderHost = "Cadroue.Host";`

The one project that names every project, so it is no ring of the border.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditBorderNeighbour`

Every ring project under `src`, Host excepted, as its name to the one ring it may name as neighbour.
A ring names its neighbour alone and ferries the data of every ring inside it.
The two adapters name `Cadroue.Core` as spokes, since the contracts they implement live there.
The core names no neighbour.

## `public static readonly IReadOnlyDictionary<string, string> TAuditBorderCapsule`

For a ring, the one capsule ring it may name beside its neighbour.
A capsule declares no neighbour of its own and belongs to one ring alone.

## `public static readonly string[] TAuditBorderCut`

The UI rings above the cut.
A cut ring names only its neighbour, and nothing from below the cut undercuts into it.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditOfferPrefix`

For a neighbour ring an offer names, the prefixes every offered type must carry.
Conduct offers `C` types and the engine offers `L` types.
A neighbour missing here fails every entry offered from it, so a new pair needs its row.
The script holds the same map as `offerPrefixes`.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditUnsealingPrefix`

For a UI ring, the type prefixes it seals.
A public type of that ring whose name starts with one is a sealed controller.
Its public members may name no type from below the ring's neighbour.
Deportment seals `L`, the medium-free controllers that later sink into Conduct.

## `public static readonly IReadOnlyDictionary<string, int> TAuditBorderCeiling`

The name count each `Kind:Ring>Target` pair may hold.
A count above fails the fact, a ceiling above the count is stale and fails too.
Lower a ceiling when a ring sheds a name, never raise one to admit a new one.
Every `Undercutting` and `Unsealing` ceiling is zero.
Every `Leaking` ceiling is zero, since no driver reads a Core or engine type through Conduct.

## `public static readonly string[] TAuditBorderExempt`

The `path:name` rows that break the border today, one row per break.
A path ending in `/*` exempts a whole folder for one name.
Every row must still match a hit, so a fixed break deletes its row.
