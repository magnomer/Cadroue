# LTraceWriterRead.cs
Hash: `ae2933b4f4ae8a67`

## `internal static LTraceReadResult<string> LTraceWriterRead(out long lTraceCommitted)`

Reads the live trace file and the commit count it reflects, under the writer lock.
Pending entries are flushed first, so the read shows everything recorded so far.
A missing file reads as empty, since nothing has been written yet.

## `public static LTraceReadResult<List<string>> LTraceFilesRead()`

Lists the current and archived trace files, newest first.
A missing folder lists nothing and is no failure.

## `public static LTraceReadResult<string> LTraceFileRead(string lTracePath)`

Reads any trace file, decompressing an archive, and defers to the live read for the current file.

## `private static LTraceReadResult<LTracePayload> LTraceReadRecord<LTracePayload>(`

Records a failed read or list as an error before the result goes back.
The reader owns the failure, so the log window only shows the notice.
The caller names the file or folder, and the error follows it.
Error is the level `LUsher` uses for a failed open.
The record is written after the writer lock is released.
