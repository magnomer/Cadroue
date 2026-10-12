# QViewerMedia.cs
Hash: `1f1b2231bd5fa99f`

## `public QViewerMedia(QViewer lOwner)`

Builds the loupe and teardown siblings, which reach shared state through this owner.
The teardown takes the probe dispose and the error record as delegates, so it names no engine type.

## `public void QViewerCommandApply(bool lCommandActive)`

Turns the viewer's command over playback on or off when its tab is shown or hidden.
Turning it off suspends playback and bumps the load serial, so a pending open lands nowhere.
Turning it on first tries an engine restore, which replaces the plain resume.

## `public void QViewerHostRecord(string lStage)`

Traces the host's visible state under a running stamp number.
The stamp advances even when the UI trace is off, so stamps stay comparable across a toggle.

## `public bool QViewerMediaClose(bool lForce)`

Closes the open media and raises an empty cargo, so every subscriber clears.
Without force it acts only while the viewer has command.
False means the close was refused or there was nothing to close.

## `public bool QViewerLoadCancel()`

Drops a pending open by clearing the intent and bumping the serial.

## `public Task QViewerLoadStart(QViewerIntent lRequest)`

Starts a probe for the requested source while the viewer has command.
The new serial supersedes every earlier open still in flight.
The intent keeps the resolved load path, so the probe outcome can be matched to it.

## `private async Task QViewerProbeRun(string lSourcePath)`

Applies a probe outcome only when it still matches the pending intent.
A failed probe commits a cargo carrying the error.
Cancelled, unloaded and obsolete outcomes commit nothing.

## `public async Task QViewerFlyleafApply(string lSourcePath, LMediaInfo? lMediaInfo, string? lFfmpegError, int lSerial)`

Creates the Flyleaf player when none is ready, then opens the source on it.
Only the open is timed, and its duration goes into the opened facts line.
Each call times on its own local clock, because two opens can overlap.
An audio-only source in a tab that refuses audio commits an error cargo without opening.
A result superseded by a newer serial is discarded, and a player created for it is disposed.

## `private void QViewerFlyleafCommit(LCargo lCargo, bool lReused)`

Commits a Flyleaf open and records its outcome through `LMediaTrace` first.
The record runs before the cargo applies, so the trace shows the open ahead of its effects.

## `public QViewerIntent? QViewerCargoApply(LCargo lCargo)`

Commits the cargo to the viewer and returns the intent it consumed.
The crop overlay survives only when crop is persistent.

## `public void QViewerIntentApply(QViewerIntent? lRequest)`

Seeks to the intent's position and starts or pauses playback.
Without an intent the autoplay preference decides.
