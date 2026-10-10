# TAuditObject.cs
Hash: `be82ffc8a21e284a`

## `public sealed class TAuditObject`

Grades every type on a ladder of creature verdicts, worst first, and records every flag it hits.
Each flag is measured on the merged type, so splitting a type into partial files escapes none.
The verdict is for display, and each flag is held to its own ceiling.
A Kraken therefore still counts under its Octopus or Spider flag, and fixing one axis lowers one counter.
While `TAuditObjectEnforced` is false, the flag, hub and part facts pass and only print their counts.
Enforced, such a fact fails when its count rises above its ceiling.
The stale-ceiling fact fails either way.

`AuditObject.ps1` is the counterpart script, and neither side reads the other.
On the same tree both reach the same results, and each side keeps its own presentation.
The only allowed difference in results is a config fault.
The script throws on any missing key in its json, a missing ceiling key included.
This test reads a missing ceiling key as 0, so that flag's fact fails once the flag has a hit.
A missing limit key throws on both sides.

## `private static readonly string[] TAuditObjectVerdicts`

The ladder, worst first: Hydra, Kraken, Spider, Chameleon, Octopus, Centipede, Serpent, Colony, Hermit.
The first seven are flags, and the last two are the healthy verdicts of a type with no flag.
The report counts each verdict and orders the verdict table by it.

## `private static readonly Lazy<IReadOnlyList<TAuditObjectRow>> TAuditObjectRows = new(TAuditObjectRead);`

The walk runs once and every fact reads the same rows.

## `private static readonly Lazy<string> TAuditObjectWritten = new(TAuditReportSave);`

The report is written once, on the first fact that runs.
Every fact reads it, so the report exists whichever fact runs first.

## `public TAuditObject(ITestOutputHelper output)`

Keeps the runner's output so a passing fact can still print its count.

## `public void AuditObject_Types_HoldNoHydra()`

No more types are a Hydra than the `Hydra` ceiling allows.
A Hydra reaches the Parts and Lines limits, and the Fused or the Density limit.
It is one object behind many parts, big and tangled inside.

## `public void AuditObject_Types_HoldNoKraken()`

No more types are a Kraken than the `Kraken` ceiling allows.
A Kraken hits the size axis and the coupling axis together.
The size axis is a Serpent or Centipede condition, the coupling axis an Octopus or Spider condition.
It is big and coupled outward, so no single move fixes it.
Mutable is its own axis, so a Chameleon condition never makes a Kraken.

## `public void AuditObject_Types_HoldNoSpider()`

No more types are a Spider than the `Spider` ceiling allows.
A Spider reaches both the Outgoing and the Incoming limit.
It uses many types and is used by many.

## `public void AuditObject_Types_HoldNoChameleon()`

No more types are a Chameleon than the `Chameleon` ceiling allows.
A Chameleon reaches the Mutable limit.
It holds many slots it can rebind after construction.

## `public void AuditObject_Types_HoldNoOctopus()`

No more types are an Octopus than the `Octopus` ceiling allows.
An Octopus reaches the Outgoing limit, so it uses many other codebase types.

## `public void AuditObject_Types_HoldNoCentipede()`

No more types are a Centipede than the `Centipede` ceiling allows.
A Centipede reaches the Members limit, so it declares many members.

## `public void AuditObject_Types_HoldNoSerpent()`

No more types are a Serpent than the `Serpent` ceiling allows.
A Serpent reaches the Lines limit over its merged parts.

## `public void AuditObject_Slots_HoldNoHub()`

No more state slots are hubs than the `Hub` ceiling allows.
A hub is a state slot reached from as many parts as the hub limit, or more.
It is a finding on the slot, outside the ladder, so it never sets a verdict.
Each hit names the owning type, which keeps a hub visible beside a healthy verdict.
A type whose only finding is a hub stays a Colony, with its hub count in the verdict table.

## `public void AuditObject_Parts_HoldsWithinCeiling()`

Every split type is declared in no more parts than its ceiling.
A split type without a ceiling may hold one part, so a new partial split fails at once.

## `public void AuditObject_Ceiling_MatchesHits()`

Every ceiling equals its count, so a ceiling left above the count is stale.
This holds even when the object rules are not enforced.

## `private static List<string> TAuditHitRead(string kind)`

One line per hit of a flag or of the hub, in the words the facts and the report share.
A flag hit names the type and the metrics its rule reads.
A hub names its owning type, its slot and how many parts reach it.

## `private static Dictionary<string, int> TAuditPartsRead()`

The part count of every type declared in more than one part.

## `private static List<string> TAuditOverRead()`

One line per split type declared in more parts than its ceiling.

## `private static List<string> TAuditAboveRead()`

One line per flag, the hub and each split type above its ceiling, and none while not enforced.

## `private static List<string> TAuditStaleRead()`

One line per flag, hub or part ceiling that sits above its count.
A part row whose type is whole again counts as one part.

## `private void TAuditObjectCheck(string kind, string summary)`

Reads the hits of one flag or the hub, and prints the count, the ceiling and the report path.
Then it asserts the count at or under the ceiling when enforced.
A kind missing from `TAuditObjectCeiling` reads as a ceiling of 0.

## `private static List<string> TAuditVerdictRead(IReadOnlyList<TAuditObjectRow> rows)`

The report table of every type with a flag, several parts or a hub, in ladder order.
Each row shows the verdict, every flag and the hub count beside them.
A hub never changes the verdict, so the count keeps it in sight.
A closing line counts the Hermits left out.

## `private static IReadOnlyList<TAuditObjectRow> TAuditObjectRead()`

Enumerates the sources with Git and runs the walker once.
An empty enumeration fails rather than passing vacuously.
A list the binder walks none of fails too, as AuditObject.ps1 does.

## `private static string TAuditReportSave()`

Writes the markdown report under `temp/audit` and returns its repo-relative path.
The head lists the generation, the switch, the type count and the count of each verdict.
Each flag and the hub follow against their ceilings, then the above and stale counts and the rules.
The verdict table follows, then every split type with its metrics.
Each flag and the hub get a section of hits, then the above and the stale ceilings.
A part overrun is listed among the above-ceiling lines.
Lines end in LF alone, as every file in the repository does.
