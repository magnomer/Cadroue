# LViewfinder.cs
Hash: `beb2475aff116c91`

## `public void LViewfinderDrawStart() => lViewfinderChronometer.QChronometerStart();`

Starts the timing of one viewfinder draw, called by the surface before it resolves the frame.
Draw timing is GUI work, so the clock is private here and not in the trace family.

## `public void LViewfinderDrawRecord()`

Hands the elapsed draw milliseconds to the timeline tally with the trigger and glyph count.

## `public LViewfinderFrame LViewfinderFrameResolve(double lWidth, double lHeight)`

Builds the whole working-range frame as plain records for the Veneer to paint.
A strip too short to read, or a flow without media, gets the background alone.
The glyph count is reset per frame, so the tally never carries an earlier frame's count.

## `public LViewfinderPoint LViewfinderPopupResolve(int lIndex, double lWidth, double lHeight)`

Returns the rename popup offset from the strip centre to the section band centre.
A section outside the working range gives a zero offset, so the popup stays centred.

## `public bool LViewfinderPressHandle(double lX, double lWidth)`

Starts a seek drag and selects the section under the press.
The last section wins where sections overlap, matching the one drawn on top.

## `public void LViewfinderDragClear()`

Ends the drag and tells the flow only when a drag was actually running.

## `private LViewfinderBand? LViewfinderBandResolve(`

Clips the section to the working range and returns no band when none of it is visible.
A visible band is at least one pixel wide, so a short section stays visible.

## `private IReadOnlyList<LViewfinderShape> LViewfinderKeyframesResolve(`

Draws keyframe marks only when every pair keeps the preferred minimum gap.
Crowded marks would hide the rail, so the strip shows none rather than a smear.
