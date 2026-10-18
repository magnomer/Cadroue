# QPlayerMpv.cs
Hash: `b31af93947fc8790`

## `private void QPlayerOpen(string lSourcePath)`

Waits up to 15 seconds for mpv to load the file.
A file that never loads throws, so the viewer sees the open fail.

## `private void QPlayerUpdate() => qPlayerMpvLibrary.LMpvSeek(qPlayerMpvLibrary.LMpvTimeRead());`

Seeks to the current time, which redraws a paused frame after a filter change.

## `private static void QPlayerPreviewApply(LPreviewApplication lApplication)`

Does nothing, since mpv takes the preview through its filter strings instead.

## `private static void QPlayerFactsRecord(string lReason, double? lMilliseconds)`

The mpv engine reports no renderer facts, so its seam records nothing.
This keeps mpv out of the Flyleaf renderer lines.

## `private void QPlayerDispose()`

A failing teardown is swallowed, so an engine swap always completes.
