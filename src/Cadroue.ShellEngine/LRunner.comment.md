# LRunner.cs

## `public int? LRunnerLevel { get; set; }`

The priority level of this runner's encode, decode-check and job probe children, held per runner.
Left empty, encode and decode children follow the encode preference, and probes keep their default standing.
A caller that needs a fixed standing sets it here instead of changing the shared preference.

## `internal LEmployer LRunnerEmployerCreate() => new(LRunnerProgramPath, LRunnerArgumentPrefix)`

Job-stage execution and decode validation share this factory.
It carries the runner's program path, argument prefix, encode family and priority level.
Sharing it keeps those two paths aligned without constraining other process factories.

## Inline notes

### `lRunnerSchedule.LScheduleRelease(lRunnerId);`

The final worker releases remaining claims only when its batch is still current.
So work claimed by a loop that ended early returns to the queue.
Otherwise it would stay Running behind an owner this process still reports as alive.
Release uses the same dispatch seam as job commits.
An ordered post implementation places it after earlier queued commits.
