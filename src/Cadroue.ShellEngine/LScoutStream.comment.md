# LScoutStream.cs
Hash: `45fac5e1465c2d69`

## `internal static class LScoutStream`

Reads the first video stream's properties through ffprobe for the bridge planner.
Every missing field degrades to an empty text or zero, never to an error.

## `internal static async Task<LBridgeStream?> LScoutStreamRead(`

Returns null when the file is absent, the probe yields nothing, or the probe fails.
A failure is recorded to the trace and swallowed, so planning goes on without stream facts.
Cancellation is the one exception that propagates, so a cancelled job never reads as a failed probe.

## `internal static IReadOnlyList<string> LScoutStreamCreate(string lScoutMediaPath) =>`

Selects only the first video stream and asks for the fields the bridge match compares.
Quiet logging keeps the output pure JSON.

## `private static LBridgeStream? LScoutStreamParse(string lScoutJson)`

Empty output or a missing or empty stream list means no stream, not an error.

## `private static long LScoutLongRead(JsonElement lScoutElement, string lScoutName)`

Accepts a JSON number or a numeric string, because ffprobe prints some values as text.
Parsing is culture-invariant, and anything unreadable becomes zero.
