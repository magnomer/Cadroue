# QMap.cs
Hash: `16f8ed5368ae5f6b`

## `public void QMapDrawStart() => qMapChronometer.QChronometerStart();`

Starts the timing of one map draw, called by the surface before it resolves the frame.
Draw timing is GUI work, so the clock is private here and not in the trace family.

## `public void QMapDrawRecord() =>`

Hands the elapsed draw milliseconds to the timeline tally with the trigger and glyph count.

## `public QMapFrame QMapFrameResolve(double lWidth, double lHeight)`

Returns only the background while no media is loaded or the strip is too short to draw.
The glyph count is the number of section bands, whether or not a badge fits.
The navigator and the cursor appear only while the working range has width.

## `public QMapHitKind QMapLeaveResolve() =>`

While a drag runs, leaving the strip keeps the hover cursor, since capture still follows the mouse.

## `public bool QMapPressHandle(double lX, double lY, double lWidth, double lHeight)`

Remembers the edge time at press, so an edge drag moves from where it started.
A press outside the edges and the move grip seeks at once.

## `public bool QMapMoveHandle(double lX, double lWidth)`

An edge drag sets the edge from the press origin plus the total offset.
A body drag moves the range by the step since the last move.
A reversal after the range hits a clamp therefore moves it at once.

## `private void QMapTriggerApply(string lTrigger)`

Names why the next draw runs, so the trace tally can attribute redraws.

## `private QMapHitKind QMapHitResolve(double lX, double lY, double lWidth, double lHeight)`

The edges win over the body, and anything outside the move area counts as a cursor seek.

## `private IReadOnlyList<QMapBand> QMapBandsResolve(LSpool lSpool, double lWidth, double lRailHeight)`

Bands span the whole media duration, not the working range.
Empty sections are skipped, and every drawn band is at least one pixel wide.

## `private void QMapCoverageAdd(List<QMapShape> lOver, LSpool lSpool, double lWidth, double lCoverageTop)`

Marks the keyframe ranges already scanned, placed across the whole media duration.
Each mark is at least one pixel wide, so a short scan stays visible.
