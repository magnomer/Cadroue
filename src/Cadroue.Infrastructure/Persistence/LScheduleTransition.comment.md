# LScheduleTransition.cs

## `internal static void LSchedulePartialRemove(LWorkDocument lWorkRecord)`

Deletes the half-written output of an interrupted run before it is queued again.
An output path naming one of the sources is never deleted, so a source is never lost.
A file that cannot be deleted is left in place, so the release still goes ahead.
