# LJob.cs

## `private async Task LJobRun()`

Destination preparation belongs to the same failure transaction as the encode.
Validation, the lease, the output folder, collision handling and the first probe all touch the file system.
Preparation faults enter retry or failure handling, while cancellation cancels or releases the job.
Fresh job probes receive the runner's priority override, keeping measurement standing aligned with its execution configuration.
Final cleanup stops the lease in each case.

## Inline notes

### `lJobOwner.lRunnerSchedule.LScheduleOutputCommit(`

Synchronous output-path persistence lets recovery use the collision-resolved name when the save succeeds.
Only a matching running owner may update the record.
The save replaces the record atomically, but a missing owner or failed write does not stop encoding.

### `if (lJobItem.LWorkKind == LWorkKind.LWorkKindFix && !pSucceeded)`

An unresolved Fix attempts to discard its own copied or partially repaired output.
Cleanup preserves input files and retries deletion, but a persistent deletion failure can leave the output on disk.

### `if (pSucceeded && lJobItem.LWorkAudio.LWorkAudioActive`

Audio processing can change loudness, so the output does not inherit the source's measured figure.
`LSubsidiary` queues the finished output for measurement after active stations become idle.
Its high queue wins at the next dequeue, without preempting source work already selected.
A successful measurement updates the schedule when a schedule is available.
