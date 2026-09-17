# PConsoleCommand.cs

## `private bool PConsoleProcessingConfirm(string pConsoleQuestion)`

Aborting a live encode (and deleting its half-written output) is severe enough that the confirm is unconditional here.
It ignores the "confirm destructive" preference that `PConsoleDestructiveConfirm` honours.
So a running Clear all is never silent.

## Inline notes

### `foreach (LStation pConsoleClearStation in LStation.LStationBoardRead())`

Kill first, clear second.
Cancelling each runner interrupts its ffmpeg and releases the running item back to the queue.
Cancelling measurement kills the in-flight ffprobe and drops what is still queued.
Only then does the folder clear remove the files.
So no process is left writing an output whose record has just been deleted.
