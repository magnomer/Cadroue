# LFlow.cs
Hash: `b9d9a356df1324a1`

## `public LFlow()`

Builds the edit, spool and stamp siblings first, so they exist before any child reaches them.
State used by several roles, such as the spool, cursor and command flag, stays on the owner.

## `public TimeSpan LFlowDuration`

Stays on the owner because the section, keyframe, waveform and losslesscut roles all read it.

## `public int LFlowMapRow => LFlowOrderCheck() ? 1 : 3;`

The map takes the top row unless the timeline order preference puts the viewfinder first.

## `public bool LFlowScanCheck() =>`

A keyframe scan needs an active, loaded timeline with probed media and a source path.

## `public void LFlowPlayingAttach(Func<bool>? lPlayingSource) => lFlowPlayingSource = lPlayingSource;`

The workspace hands in the viewer's playing read, so a drag knows whether there is playback to pause.

## `public void LFlowPaletteApply(int lPaletteCount) => lFlowPaletteCount = Math.Max(1, lPaletteCount);`

Keeps at least one colour, so the colour rotation of new sections never divides by zero.

## `public void LFlowAttach(LMediaInfo lMediaInfo, string? lSourcePath, TimeSpan lCursorTime)`

Ignored while the timeline is inactive.
Reopening the same source keeps the current cursor instead of the time handed in.
The strips get their attach notice before the saved sections load and announce themselves.
On the same source the cursor is sent back to the viewer, so playback resumes where it was.

## `public bool LFlowClear()`

Returns false when nothing is loaded and no section exists, so a repeat clear raises no notice.

## `public void LFlowClose()`

Runs once, and closes the keyframe and waveform siblings for good.

## `public void LFlowCursorUpdate(TimeSpan lCursor)`

The playback path moves the cursor without seeking the viewer back, and keeps the keyframe scan following.

## `public void LFlowCursorSeek(TimeSpan lCursor)`

A user seek suspends the keyframe scan and asks the viewer to seek.

## `public void LFlowCursorPropagate(TimeSpan lCursor, bool lViewerSeek, bool lKeyframeRestart)`

Every cursor move drops a pending keyframe direction, because the move outdates it.

## `public bool LFlowWheelHandle(int lDelta)`

The wheel action preference picks zoom, volume or seek, and the result says whether the wheel was used.

## `public void LFlowDragHandle(bool lDragging)`

With the drag-pause preference on, playback pauses during a drag.
Only playback this class paused is resumed when the drag ends.

## `public bool LFlowShortcutRun(string lToken)`

Runs one shortcut token and reports whether the timeline handled it.
Section tokens run only while the edit sibling reports editing allowed.

## `public void LFlowSectionSet(bool lSectionActive)`

The section band flag stays beside the command flag, because the workspace sets both on tab show.

## `public void LFlowCommandSet(bool lCommandActive)`

Deactivation stops any keyframe timer and drops a pending navigation direction.

## `public bool LFlowWaveformSet(bool lWaveformActive)`

Only a real change raises the notice and hands the new state to the waveform sibling.
