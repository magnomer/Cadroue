# TViewerPlayback.cs
Hash: `82b591a3d676d1e2`

## `public sealed class TViewerPlayback`

Checks the viewer playback sibling without any control.
Setup gives the viewer an active command state, a recording clock, and a fake engine seam.
The seam records seeks and reports a fixed time and end state.
The Logging collection keeps it from running beside tests that share the trace.

## `public void Play_StartsClock_AndMarksPlaying()`

Play starts the position clock and takes the position from the engine.
Pause stops the clock.

## `public void Play_AfterEnd_SeeksToStart()`

Playing after the end was reached restarts from zero and clears the end flag.

## `public void Play_WithPendingIntent_OnlyRewritesIntent()`

While a load is pending the engine is not touched.
Play, pause and seek only update the stored intent, so the load applies it later.

## `public void Play_CommandInactive_OrNoEngine_DoesNothing()`

Without an engine or an active command, play changes no state and starts no clock.

## `public void Loupe_ForwardsTransport_AndSyncsSeek()`

With the loupe active the transport goes to the loupe, not to the docked engine.
The viewer position still follows the seek.

## `public void Seek_AccurateOnFlyleaf_MarksRequest_AndClearsEnd()`

A seek marks the accurate request so the engine lands on the exact frame.

## `public void Tick_AtEnd_StopsClock_AndMarksEnd()`

An engine reporting the end stops playback instead of raising a position.

## `public void Suspend_RemembersPlaying_Resume_RestartsIt()`

Suspend stops the clock but remembers playback was running.
Resume restarts it only for that remembered case.

## `public void VolumeSet_ClampsAndRaises_OnlyWhenCommanded()`

Volume above the maximum is clamped to 100.
An inactive command state raises nothing and leaves the volume unchanged.
