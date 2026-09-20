# TAuditFrameSetting.cs

## `internal static class TAuditFrameSetting`

Hand-written and tracked: the pure rings, the frame, the ambient rows, the ceilings and the waivers.
No script writes this file, and the frame fact reads no script configuration.
The values mirror the ring frames in `auditstructure.json`, kept by hand.

## `public static readonly string[] TAuditFramePure`

The rings held to the frame and kept from every ambient member.
The veneer and the adapters are outside the chain's purity and name what they need.

## `public static readonly string[] TAuditFrameAllowed`

The framework namespaces a pure ring may name.
`System.IO`, `System.Xml`, `System.Reflection` and `System.Text.Json` are outside it and belong behind a port.

## `public static readonly string[] TAuditFrameAmbient`

The members a pure ring never touches: the clock, the environment, an id, a random, the console, the disk.
A member matches when its full name equals a row or sits under it.

## `public static readonly IReadOnlyDictionary<string, int> TAuditFrameCeiling`

The file count each `kind:ring>target` pair may hold.
A count above fails the fact, a ceiling above the count is stale and fails too.
Lower a ceiling when a ring sheds a file, never raise one to admit a new one.
Every row is at its baseline: the pure rings still read the disk, the clock and the process directly.

## `public static readonly string[] TAuditFrameWaiver`

The `path:name` rows that break the frame today, one per break, each deleted by a later plan.
A path ending in `/*` waives a whole folder for one name.
Every row must still match a hit, so a fixed break deletes its row.
