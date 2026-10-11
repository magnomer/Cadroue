# TTraceAggregation.cs
Hash: `8571406fb8db6eba`

## `public sealed class TTraceCollection`

The trace and its draw tally are process-wide statics.
So every test touching them runs alone in the `Logging` collection.

## `public sealed class TTraceAggregation`

Checks that timeline draws fold into one UI record per draw tick.
Each draw goes in with an exact duration, so the average, peak and total are asserted as exact text.

## `public void DisablingVerbose_FlushesDrawsAcceptedBeforeTheTransition()`

Turning Verbose off stops the draw timer.
Draws already accepted must still reach the log before the off notice.
