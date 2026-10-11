# LTraceLog.cs
Hash: `6893ffdbb6aa2d9f`

## `public static class LTraceLog`

The trace front names no Application type, so Infrastructure keeps its one-way dependency.

## `public static void LTraceFfmpegRecord(string lMessage, string? lDetail = null)`

Records an FFmpeg command or stderr line under the FFmpeg kind, kept only while verbose.
It gives the host a method group for the engine's FFmpeg seam, so the host composes nothing.

## `public static void LTraceClear()`

Empties the written log and resets the in-memory draw tally and entry stamp together.

## `public static string LTraceVersionRead()`

Reads the program version for the log header and the about window.
The read uses reflection, which the Deportment may not use, so it lives in `Trace`.
The build mark after `+` is cut so the version reads as released.
