# QPlayer.cs
Hash: `2dd364fa7aa142a9`

## `public sealed class QPlayer`

Drives one preview engine through the seam its wrapper hands in, so no engine type reaches the Deportment.
An engine without filter strings or a paused refresh hands `null` for those seam slots.
The player then skips the call and still records the applied value.

## `public void QPlayerEngineSet(QPlayerSeam? lSeam)`

Disposes the previous engine and forgets the applied filters, since a new engine starts clean.
Setting the same seam again does nothing.

## `public Task QPlayerOpenStart(string lSourcePath)`

Opens on a background thread, so a slow open never blocks the UI.
The seam is read before the hop, so a later engine swap cannot redirect the open.

## `public void QPlayerSeek(TimeSpan lPosition) =>`

Clamps the target to the last video frame, so a seek past it still shows a picture.

## `public void QPlayerFactsRecord(string lReason, double? lMilliseconds = null) =>`

The engine owns its renderer facts, so the Deportment supplies only the reason and the time.

## `public void QPlayerCreateStart() => qPlayerChronometer.QChronometerStart();`

Starts timing the creation of the engine wrapper this player drives.
Only the Flyleaf wrapper times its creation, calling this before it builds the engine.

## `public double QPlayerCreateRead() => qPlayerChronometer.QChronometerRead();`

Reads the creation milliseconds once the host holds the player.
The clock stays private, so no stamp reaches the Veneer.

## `public void QPlayerPreviewApply(LPreviewState lState, string lReason)`

Contrast reaches the engine only while Flyleaf is active, since only its shader applies it.

## `public bool QPlayerAccurateSet()`

Marks an accurate seek as running and reports whether one already was.

## `public bool QPlayerSeekCommit(int lSeekMilliseconds)`

Ends the accurate seek and reports whether the renderer facts are now due.
They are due once, after the first completed seek following an open.

## `public bool QPlayerFilterApply(string lFilter)`

Sends the filter only when it differs from the one applied.
A rejected filter is cleared, so a half-applied chain never stays on screen.
The export never depends on it, so the failure is only recorded.
