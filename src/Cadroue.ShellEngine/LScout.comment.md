# LScout.cs
Hash: `51c3a14fd17f6eb1`

## `internal static async Task<LWorkMedia?> LScoutMediaRead(`

A missing or unreadable file yields null, never an exception, so measurement never fails a job by itself.

## `internal static async Task<double?> LScoutIntervalRead(`

An intra-only codec makes every frame a keyframe, so the interval is one frame and no scan runs.
Fewer than two keyframes leave the interval unknown.

## `internal static async Task<LWorkMedia?> LScoutSourceRead(`

Source measurement adds keyframe interval and integrated loudness when those measurements succeed.
`LSubsidiary` caches these figures and stores them on scheduled items for later display.
Jobs reuse stored source facts where available, but duration and missing media can still require fresh probes.
Fresh job and merge probes forward the supplied priority override rather than replacing it with a shared setting.
This keeps runner-controlled measurements aligned without changing source measurement's default standing.

## `internal static long? LScoutInputRead(LWorkItem lScoutWorkItem, CancellationToken lScoutToken = default)`

A merge counts the bytes of every source.
One unreadable source discards the sum and falls back to the single source size.

## Inline notes

### `string lScoutBaseArguments = "-hide_banner -nostdin -v error -xerror "`

Read-only decode-to-null.
Never writes the output.
The command decodes the output to a null sink rather than rerunning the repair.
Validation requires a zero exit code and no non-whitespace error output.
Routed through the runner's program path, argument prefix, argument transform, and priority level.
So validation and repair use the identical ffmpeg at the same priority, in the encode custody family.
