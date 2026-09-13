# LScout.cs

## `internal static LWorkMedia? LScoutSourceRead(string lScoutSourcePath, CancellationToken lScoutToken = default)`

Every figure the worklist shows for a source is measured here once, when the file is added.
It is stored on the item.
The job run never re-measures it and a deleted source still shows its recorded figures.
Enriches the base probe with the keyframe interval (video) and integrated loudness (audio) the probe does not carry.

## Inline notes

### `string lScoutBaseArguments = "-hide_banner -nostdin -v error -xerror "`

Read-only decode-to-null.
Never writes the output.
Only re-runs the affected operation to confirm the repaired stream decodes cleanly.
Routed through the runner's configured program path, argument prefix, and argument transform so validation and repair use the identical ffmpeg.
