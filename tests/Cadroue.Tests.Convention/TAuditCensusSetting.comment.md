# TAuditCensusSetting.cs
Hash: `0182df49f7ace071`

## `internal static class TAuditCensusSetting`

Hand-written and tracked.
It holds the floors, the squatting patterns and the smuggling words of the census.
No script writes this file, and the census fact reads no script configuration.
`AuditStructure.json` holds its own copy, kept in step by hand as scripts/principles.md asks.

## `public static readonly IReadOnlyDictionary<string, int> TAuditHollowingFloor`

The fewest source files a ring may hold.
It guards the application ring against being emptied into the core by a mistaken move.
Raise a floor when the ring grows, never lower it.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditSquattingPattern`

Patterns a ring may not declare a type name under, since such a type belongs to another ring.
The core holds no pattern yet, so the fact keeps the slot for the first misplaced kind.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditSmugglingWord`

Words no bound source under a folder may name, matching `smugglingWords` in scripts/AuditStructure.json.
The shell engine lists no word yet, so the fact keeps the slot for the first one.
