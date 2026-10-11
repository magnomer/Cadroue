# LTraceTally.cs
Hash: `0f86fb7608834ce6`

## `private sealed class LTraceDrawTally`

Counts the draws of one timeline surface within a tally period.
It keeps total and peak time, glyph count and the triggers in first-seen order.

## `private sealed class LTraceTimelineTally`

Joins the surface tallies into one timeline record per period.
Surfaces appear in the order they first drew.
The cursor and source of the last draw head the record, since they describe what was on screen.
