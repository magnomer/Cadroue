# LJob.cs

## `internal async Task LJobRun()`

Destination preparation belongs to the same failure transaction as the encode.
Validation, the lease, the output folder, collision handling and the first probe all touch the file system.
So a throw there must end as a committed Failed job with the lease released.
It must never be an exception that leaves the record claimed and stops the worker loop.

## Inline notes

### `lJobOwner.lRunnerSchedule.LScheduleOutputCommit(`

Persist the resolved output path before the encode runs, synchronously so the stored record is durable first.
A retry or stale-job recovery then acts on the reserved name, never the original pre-existing file.
The record is this job's own running entry (owner-guarded, atomic replace), safe to write off the post thread.

### `if (lJobItem.LWorkKind == LWorkKind.LWorkKindFix && !pSucceeded)`

A Fix that does not end resolved must leave nothing behind.
The copied (and any partially repaired) output is discarded.
So an unrepaired file never persists as if it were a valid result.

### `if (pSucceeded && lJobItem.LWorkAudio.LWorkAudioActive`

A re-encoded audio stream's loudness differs from the source and was left unmeasured above.
Hand it to `LSubsidiary`, which measures the finished output at high priority once the drive is free.
High priority means ahead of any queued source measurement.
It then records the figure.
