# QViewerTeardown.cs
Hash: `359abdcd0095852a`

## `public sealed class QViewerTeardown`

Owns the final teardown of the viewer when its panel unloads.
The player stop stays on the media owner, because media close shares it.

## `public QViewerTeardown(LViewer lOwner, LViewerMedia lMedia, Action lProbeDispose, Action<string, Exception?> lErrorRecord)`

Takes the probe dispose and the error record as delegates from the media owner.
So this file names no engine type, and the probe stays owned by one class.

## `public void LViewerClose()`

Tears the viewer down for good when its panel unloads.
The unloaded flag makes a second close do nothing.
The serial change makes any load still in flight drop its result.
Each stage is isolated, so a failing probe dispose never keeps the player alive.

## `public void LViewerStageRun(string lStage, Action lAction)`

Runs one close stage and records its failure without letting it escape.
It is public so the veneer's host disposals share the same trace text and error record.
