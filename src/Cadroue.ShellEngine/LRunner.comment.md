# LRunner.cs
Hash: `e0514b46f7f24142`

## `public LRunner(LScheduleContract lSchedule, Action<Action> lRunnerPostAction)`

Registers the runner with the host's liveness registry through the add seam at construction.
So its claims count as owned by a live runner before the first job starts.

## `public int? LRunnerLevel { get; set; }`

The priority level of this runner's encode, decode-check and job probe children, held per runner.
Left empty, encode and decode children follow the encode preference, and probes keep their default standing.
A caller that needs a fixed standing sets it here instead of changing the shared preference.
Job stages and decode checks hand the employer seam this level with the program path, prefix and encode family.

## `internal static void LRunnerRecord(string lRunnerMessage, Exception? lRunnerException = null)`

Job code records through this one call, and the presence of an exception picks the error kind.
It reaches the trace only through `LTraceEngine`, so the runner holds no trace seam of its own.

## `internal async Task LRunnerResume(CancellationToken lRunnerToken)`

Despite its name, it waits between stages while the runner is suspended, and returns once play resumes.
So a job never starts its next stage during a pause.

## `public void LRunnerStart()`

Play after a pause resumes the suspended children and keeps the running batch.
A fresh batch starts only when none is running, so play never doubles the worker loop.

## `public void LRunnerPause()`

Pause suspends the live FFmpeg children instead of ending them, so their encodes continue on resume.
The runner counts as suspended only when a child was suspended or none was running.
Work ids without a live child are held too, so a stage starting during the pause starts suspended.
The suspended message is set only once the pause succeeds.
When every live child refuses suspension, the holds are released, the failure is recorded and encoding continues.

## `public Task LRunnerCancel()`

Cancels the batch and resumes suspended children, so each job can observe the cancellation.
The returned task completes once every job running at that moment has drained.

## `public void LRunnerJobCancel(Guid lWorkId)`

A job not yet started is remembered, so it is cancelled the moment the runner reaches it.

## `internal void LRunnerProcessAttach(Guid lWorkId)`

A stage registers its work id before the employer starts its child.
While the runner is suspended, the id is held at once, so the employer starts that child suspended.
The runner names children only by work id, and the employer holds the processes.

## `internal void LRunnerProcessDetach(Guid lWorkId)`

Forgets a work id once its stage ends, and releases any hold the employer keeps for it.
Without the release, a retry under the same id could start suspended behind a running queue.

## `private void LRunnerProcessResume()`

Resumes and releases every registered work id, including ids held before their child started.
A refusal keeps the paused flag, so the next play retries the resume.

## `private async Task LRunnerLoopRun(LRunnerBatch lRunnerActive, CancellationToken lRunnerToken)`

Claims and runs one job at a time until the queue holds nothing pending.
Pending work it cannot claim yet is polled again after a short delay.
An unexpected error is recorded and ends the loop, which then stops its batch.

## `internal void LRunnerFailureApply()`

Stops claiming further work after a failure, but only when the pause-on-failure preference is on.

## Inline notes

### `lRunnerSchedule.LScheduleRelease(lRunnerId);`

The final worker releases remaining claims only when its batch is still current.
So work claimed by a loop that ended early returns to the queue.
Otherwise it would stay Running behind an owner this process still reports as alive.
Release uses the same dispatch seam as job commits.
An ordered post implementation places it after earlier queued commits.
