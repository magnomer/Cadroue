# LConsole.cs

## `public void LConsoleAll()`

Aborting a live encode (and deleting its half-written output) is severe enough that the confirm is unconditional here.
It ignores the "confirm destructive" preference that `LConsoleAskResolve` honours.
So a running Clear all is never silent.

## `private async Task LConsoleAllRun(bool lAnswer)`

Kill first, clear second.
Cancelling each runner interrupts its ffmpeg and releases the running item back to the queue.
Cancelling measurement kills the in-flight ffprobe and drops what is still queued.
Only then does the folder clear remove the files.
So no process is left writing an output whose record has just been deleted.
