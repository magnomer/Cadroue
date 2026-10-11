# TAuditEngineSetting.cs
Hash: `414d504053fc8f22`

## `internal static class TAuditEngineSetting`

Hand-written and tracked settings for the engine pair of the border.
It holds the offer for `Cadroue.Conduct>Cadroue.ShellEngine` alone.
No script writes this file.
`AuditStructure.json` holds its own copy, kept in step by hand as scripts/principles.md asks.
The border facts read it together with the other offer settings.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditEngineOffer`

For the `ring>neighbour` pair, the neighbour types the ring may name at all.
The list is empty, since Cadroue's Conduct holds no source and names no engine type.
An engine type Conduct comes to name is listed here first.
Every engine type left off the list is a helper Conduct may not name.
Each name stands as a literal, so the ratchet can read every entry.
