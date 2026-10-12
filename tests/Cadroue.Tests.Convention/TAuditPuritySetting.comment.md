# TAuditPuritySetting.cs
Hash: `61a8b4e9b7a825a4`

## `internal static class TAuditPuritySetting`

Hand-written and tracked: the pure rings, the frame, the eavesdropping members, the ceilings and the exempt rows.
No script writes this file, and the purity fact reads no script configuration.
Both copies change together, as scripts/principles.md asks.
`AuditStructure.json` holds its own copy of the ring frames for the structure script.

## `public static readonly string[] TAuditPurityRing`

The rings held to the frame and kept from every eavesdropping member.
Conduct is one, since it sits between the engine and the UI and needs no platform.
Infrastructure, Media, the UI projects and the host are outside the purity and name what they need.

## `public static readonly string[] TAuditPurityFrame`

The framework namespaces a pure ring may name.
`System.IO`, `System.Xml`, `System.Reflection` and `System.Text.Json` are outside it and belong behind a port.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditPurityExtra`

The namespaces one pure ring adds to the shared frame, matching the ring frames of scripts/AuditStructure.json.
The shell engine and Conduct may rethrow a captured exception, so only they name `System.Runtime.ExceptionServices`.

## `public static readonly string[] TAuditEavesdroppingMember`

The members a pure ring never touches.
They cover the clock, the environment, an id, a random, the console, the disk and a process.
A member matches when its full name equals a row or sits under it.

## `public static readonly IReadOnlyDictionary<string, int> TAuditPurityCeiling`

The name count each `Kind:Ring>Target` pair may hold.
The rows cap the eavesdropping members and foraged namespaces the core, application and engine still reach.
Conduct has no row, so any break there fails at once.
A row falls as its breaks move behind a port, and a row at zero keeps a cleared break shut.
A count above fails the fact, a ceiling above the count is stale and fails too.
Lower a ceiling when a ring sheds a name, never raise one to admit a new one.

## `public static readonly string[] TAuditPurityExempt`

The `path:name` rows that break the purity by design, one row per break.
A path ending in `/*` exempts a whole folder for one name.
Every row must still match a hit, so a fixed break deletes its row.
No row stands, so every break counts against a ceiling.
