# LRunner.cs

## Inline notes

### `lRunnerSchedule.LScheduleRelease(lRunnerId)`

Whatever this batch still holds is released here.
So work claimed by a loop that ended early returns to the queue.
Otherwise it would stay Running behind an owner this process still reports as alive.
It is posted rather than run on the loop thread so it follows every commit the finished jobs already queued.
