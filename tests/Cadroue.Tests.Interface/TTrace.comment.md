# TTrace.cs
Hash: `8c1127c9d444f1b8`

## `internal sealed record TTraceEntry(`

A test-side copy of one trace entry with the kind as its display name.
Tests assert on it without naming the production entry type.

## `internal sealed class TTrace : IDisposable`

Isolates the process-wide trace for one test.
It points the depot root at a fresh temp folder, so no log reaches the real workspace.
It starts with Verbose and loading off and an empty log.
Dispose restores the earlier Verbose, loading and depot root, then deletes the temp folder.
A failed folder delete is ignored, since the folder is disposable.

## `internal IReadOnlyList<TTraceEntry> TTraceEntries`

Flushes the background writer first, so every appended entry is in the list.

## `internal IReadOnlyList<TTraceEntry> TTracePersistRead()`

Reads the entries back from the written log file, not from the live notice.
It proves an entry survives the format and parse round trip.

## `internal void TTimelineDraw(`

Feeds one timeline draw of a chosen duration into the trace tally.
The duration goes in as milliseconds, so the recorded time is exact.

## `internal void TTraceReset()`

Drops any pending draw tally and the captured entries.
Draws fed before it cannot leak into the next report.
