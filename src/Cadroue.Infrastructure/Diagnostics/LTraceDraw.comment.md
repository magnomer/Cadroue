# LTraceDraw.cs
Hash: `d4cbcb3759509038`

## `public static void LTraceTimelineAdd(`

Adds one timeline draw and its elapsed milliseconds to the tally.
The caller times the draw on its own GUI clock and hands in the milliseconds.
This is the only door into the tally.
Draws are dropped while verbose is off, checked again under the lock.

## `public static void LTraceDrawTick()`

Flushes the timeline tally as one UI record and starts a fresh one.
A second without draws writes nothing.
The timer calls it once a second, and turning verbose off calls it once more.

## `public static void LTraceReset()`

Drops the pending tally and the stamp of the previous entry.
Clearing the log calls it, so no cleared draw or gap leaks into the next entry.
