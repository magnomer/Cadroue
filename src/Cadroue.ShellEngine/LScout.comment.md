# LScout.cs

## `internal static async Task<LWorkMedia?> LScoutSourceRead(
        string lScoutSourcePath, CancellationToken lScoutToken = default)`

Source measurement adds keyframe interval and integrated loudness when those measurements succeed.
`LSubsidiary` caches these figures and stores them on scheduled items for later display.
Jobs reuse stored source facts where available, but duration and missing media can still require fresh probes.
Fresh job and merge probes forward the supplied priority override rather than replacing it with a shared setting.
This keeps runner-controlled measurements aligned without changing source measurement's default standing.

## Inline notes

### `string lScoutBaseArguments = "-hide_banner -nostdin -v error -xerror "`

Read-only decode-to-null.
Never writes the output.
The command decodes the output to a null sink rather than rerunning the repair.
Validation requires a zero exit code and no non-whitespace error output.
Routed through the runner's program path, argument prefix, argument transform, and priority level.
So validation and repair use the identical ffmpeg at the same priority.
