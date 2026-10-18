# QFlow.cs
Hash: `b9d9a356df1324a1`

## `public QFlow()`

Builds the edit, spool and stamp siblings first, so they exist before any child reaches them.
State used by several roles, such as the spool, cursor and command flag, stays on the owner.

## `public TimeSpan QFlowDuration`

Stays on the owner because the section, keyframe, waveform and losslesscut roles all read it.

## `public int QFlowMapRow => QFlowOrderCheck() ? 1 : 3;`

The map takes the top row unless the timeline order preference puts the viewfinder first.

## `public bool QFlowScanCheck() =>`

A keyframe scan needs an active, loaded timeline with probed media and a source path.

## `public void QFlowPlayingAttach(Func<bool>? lPlayingSource) => qFlowPlayingSource = lPlayingSource;`

The workspace hands in the viewer's playing read, so a drag knows whether there is playback to pause.

## `public void QFlowPaletteApply(int lPaletteCount) => qFlowPaletteCount = Math.Max(1, lPaletteCount);`

Keeps at least one colour, so the colour rotation of new sections never divides by zero.

## `public void QFlowAttach(LMediaInfo lMediaInfo, string? lSourcePath, TimeSpan lCursorTime)`

Ignored while the timeline is inactive.
Reopening the same source keeps the current cursor instead of the time handed in.
The strips get their attach notice before the saved sections load and announce themselves.
On the same source the cursor is sent back to the viewer, so playback resumes where it was.

## `public bool QFlowClear()`

Returns false when nothing is loaded and no section exists, so a repeat clear raises no notice.

## `public void QFlowClose()`

Runs once, and closes the keyframe and waveform siblings for good.

## `public void QFlowCursorUpdate(TimeSpan lCursor)`

The playback path moves the cursor without seeking the viewer back, and keeps the keyframe scan following.

## `public void QFlowCursorSeek(TimeSpan lCursor)`

A user seek suspends the keyframe scan and asks the viewer to seek.

## `public void QFlowCursorPropagate(TimeSpan lCursor, bool lViewerSeek, bool lKeyframeRestart)`

Every cursor move drops a pending keyframe direction, because the move outdates it.

## `public bool QFlowWheelHandle(int lDelta)`

The wheel action preference picks zoom, volume or seek, and the result says whether the wheel was used.

## `public void QFlowDragHandle(bool lDragging)`

With the drag-pause preference on, playback pauses during a drag.
Only playback this class paused is resumed when the drag ends.

## `public bool QFlowShortcutRun(string lToken)`

Runs one shortcut token and reports whether the timeline handled it.
Section tokens run only while the edit sibling reports editing allowed.

## `public void QFlowSectionSet(bool lSectionActive)`

The section band flag stays beside the command flag, because the workspace sets both on tab show.

## `public void QFlowCommandSet(bool lCommandActive)`

Deactivation stops any keyframe timer and drops a pending navigation direction.

## `public bool QFlowWaveformSet(bool lWaveformActive)`

Only a real change raises the notice and hands the new state to the waveform sibling.
