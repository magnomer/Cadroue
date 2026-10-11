# LEmployerEngine.cs
Hash: `f934e3351878c197`

## `public static class LEmployerEngine`

The single ShellEngine owner of raw process execution.
The engine never names the Media employer, so the host assigns each seam at startup.
The test relay assigns the same seams, so tests spawn through the same employer.

## `private const string LEmployerDetachedError = "No process employer is attached.";`

The error text both seams report when no host has attached an employer.

## `public static Func<string, string, LCustodyFamily?, int?, TimeSpan?, string, CancellationToken,`

Runs one program with a single argument string, as job stages, scans and probes need.
It carries the program, prefix, custody family, priority level and idle limit the employer takes.
A job stage passes its work id, so the employer can hold and suspend that child for the runner.
Other callers pass no work id, since probes and scans never pause with the queue.
The line readers pass through unchanged.
Unattached, it reports a failed run with a clear error and never throws.

## `public static Func<string, LCustodyFamily?, int?, TimeSpan?, IReadOnlyList<string>, CancellationToken,`

Runs one program with an argument list, which keeps paths unquoted and unsplit.
It takes no prefix, since the employer refuses a prefix beside an argument list.
Unattached, it reports a failed run with a clear error and never throws.

## `public static Func<Guid, bool> LEmployerSuspendSeam { get; set; } = _ => true;`

Suspends the live child under a work id, or holds the id so its coming child starts suspended.
It reports false only when a live child refuses, which the runner's pause counts as a failure.
Unattached, it reports true, so a runner without an employer still pauses its claiming.

## `public static Func<Guid, bool> LEmployerResumeSeam { get; set; } = _ => true;`

Resumes the child under a work id and releases its hold.
It reports false only when a live child stays suspended, so the runner keeps its paused flag.
Unattached, it reports true.

## `public static Func<Guid, bool> LEmployerLiveSeam { get; set; } = _ => true;`

Reports whether a started child under a work id is still running.
The pause needs it, because only live children decide whether the pause succeeded.
Unattached, it reports true.
