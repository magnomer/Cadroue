# LScheduleStale.cs
Hash: `c4251d79f88609d8`

## `internal static class LScheduleStale`

The stale-recovery role of the schedule, kept apart from claiming, leasing and releasing.
It returns work whose owner died to the queue, or fails it after too many recoveries.
It holds no state, so the schedule instance only forwards its contract call here.

## `internal static int LScheduleStaleClaim()`

Walks the running folder and acts only on records the sentinel calls stale.
A partial output is removed first, so a retry never meets a half-written file.
A record recovered as often as the attempt limit is failed instead of returned.
The count tells the caller how many records changed folder.

## `private static string LSchedulePhaseFormat(string lWorkPhase)`

Turns the stored phase into the words the recovery message and trace use.

## `private static bool LScheduleFailedSet(LWorkDocument lWorkRecord, string lScheduleMessage)`

Clears the owner and moves the record to the failed folder with its message.
