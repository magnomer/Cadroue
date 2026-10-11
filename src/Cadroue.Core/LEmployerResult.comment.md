# LEmployerResult.cs
Hash: `d8db1f80231342c3`

## `public readonly record struct LEmployerResult(int LEmployerExit, string LEmployerError, bool LEmployerStalled = false);`

The outcome of one external process run, carried as plain data.
It lives in Core so the engine reads outcomes without naming the Media spawn.
The stalled flag marks a run ended by its idle limit rather than by the caller.
