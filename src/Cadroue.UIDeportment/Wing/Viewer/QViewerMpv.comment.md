# QViewerMpv.cs
Hash: `3d51084a18ae93a4`

## `public async Task QViewerMpvApply(string lSourcePath, LMediaInfo? lMediaInfo, string? lFfmpegError, int lSerial)`

Opens the source on mpv, creating the engine when none is ready.
A persistent crop filter is applied before the open, so the first frame already shows it.
An audio-only source in a tab that refuses audio commits an error cargo without opening.
A failed open goes to the rebuild, a superseded one to the restore.

## `private void QViewerMpvRestore()`

Puts the viewer back on its kept source after a superseded mpv open.
Without command the reopen waits as an intent until the tab is shown.
Without a kept source the player stops and the host hides.

## `public void QViewerMpvCommit(LCargo lCargo)`

Commits an mpv open and records its outcome through `LMediaTrace`.
Without a preview the player is disposed and playback is reported as stopped.

## `private async Task QViewerMpvRebuild(`

Handles an mpv open failure after the trace records it.
An Edit-eligible tab commits the media without a preview, since mpv is its only preview engine.
Every other tab falls back to Flyleaf for this file.
