# LEmployer.cs
Hash: `0c0bc3a23e2c8828`

## `public async Task<LEmployerResult> LEmployerRun(`

The engine seam passes a work id instead of a process, so the engine never names an OS process.
With a work id, the started child is registered under it and forgotten once the run ends.
Without one, the run attaches nothing, as probes and scans never pause with the queue.
The registration happens right after the start, so a held work id suspends the child at once.

## `internal ProcessStartInfo LEmployerInfoCreate(string? lEmployerArguments, IReadOnlyList<string>? lEmployerList)`

An argument list is passed verbatim, so a text prefix cannot be merged into it and is refused.

## `private async Task<LEmployerResult> LEmployerProcessRun(`

Every child joins the custody job right after the start, so none outlives the app.
A run ended by its idle limit returns exit -1 marked stalled, while a caller cancellation rethrows.
A child that survives the kill for five seconds raises, because its outcome cannot be trusted.

## `private void LEmployerPriorityApply(Process lEmployerProcess)`

An explicit level wins over the family's chosen level.
A run naming neither drops to below normal, so stray children never compete with the interface.

## `private static async Task<string> LEmployerErrorRead(`

Only the last stretch of stderr is kept, so a chatty child cannot exhaust memory.
A truncated result says so in its first line.
Each stderr line also feeds the idle pulse, since ffmpeg reports progress there.
