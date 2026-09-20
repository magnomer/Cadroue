# TAuditObjectSetting.cs

## `internal static class TAuditObjectSetting`

Hand-written and tracked: the object-audit switch, the report path, the floors and the ceilings live here.
No script writes this file.
`auditobject.ps1` reads its own auditobject.json and never writes this file.

## `public const bool TAuditObjectEnforced = true;`

False makes every object fact a warning that passes.
True fails a fact when a kind counts above its ceiling.

## `public const string TAuditObjectReport = "temp/audit/Object-{0}.md";`

Where the report lands, with the version in the name.
`temp` is ignored by Git, so a run never dirties the tree.

## `public const int TAuditPartFloor = 5;`

A type declared in fewer files than this is never a monolith.

## `public const int TAuditLineFloor = 1000;`

A type whose merged declarations hold fewer lines than this is never a monolith.

## `public const int TAuditHubReach = 5;`

A state slot reached from this many parts or more is a hub.
Its declaring part counts as reaching it.

## `public const double TAuditWeaveFloor = 0.75;`

The share of parts the largest member component may span before the parts count as one object.
Hub state is removed before the components are formed, so one shared field alone never weaves a type.

## `public const double TAuditDensityFloor = 0.5;`

Cross-part references per member at which the parts count as one object whatever the weave says.

## `public static readonly IReadOnlyDictionary<string, int> TAuditObjectCeiling`

The hit count each kind may reach.
`Monolith` counts the split types that are one object behind many files.
`Hub` counts the state slots reached from many parts, over every type.
A count above fails the fact, a ceiling above the count is stale and fails too.
Lower a ceiling when a type sheds a hit, never raise one to admit a new one.

## `public static readonly IReadOnlyDictionary<string, int> TAuditPartCeiling`

The parts a named type may be split over, keyed by its full name.
Empty until a plan names the type whose part count it walks down, and a part is never added.
Lower a ceiling when a plan lifts a part into its own class, never raise one.

## `public static readonly string[] TAuditObjectInclude`

The `git ls-files` patterns of the sources the walk compiles.
