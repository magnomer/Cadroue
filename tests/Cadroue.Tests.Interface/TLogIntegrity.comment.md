# TLogIntegrity.cs
Hash: `70cc070b7bf0840f`

## `internal static class TLogIntegrity`

Drives the trace writer against real temp folders to prove its durability and move guarantees.
Each root-changing probe points the depot root at a fresh folder and restores the previous root afterwards.
It persists pending entries first, so no earlier test's entries leak into its result.
The read and archive probes work on a private temp folder and leave the depot root alone.

## `internal static TLogReadResult TLogReadResolve()`

Reads an empty file, a corrupt archive and a missing file.
Each must report a result rather than throw.

## `internal static TLogCommitResult TLogCommitRead()`

The handler ignores every entry except its own.
A pending lost-entry report from an earlier test may be saved and notified first.
When the notification arrives, the log file must already hold the entry text.

## `internal static TLogCallbackResult TLogCallbackPersist()`

Persists from inside the append notification, proving a subscriber cannot deadlock the writer.
The persist uses a short wait, so a stuck writer shows as false.

## `internal static TLogMoveResult TWorkspaceMove()`

Entries recorded before and after a workspace move must all survive in the log read back.

## `internal static TLogMoveResult TLogConcurrentMove()`

Records an entry while the root move is held open mid-way.
That entry must not be lost or written to the old root.
Both gates time out after five seconds, so a broken move fails instead of hanging.

## `internal static TLogPersistResult TLogTimeoutPersist()`

The root is a file, so storage is unavailable and the persist cannot succeed.
The persist must give up near its short wait and report false.

## `internal static TLogArchiveResult TLogArchiveRun()`

Two simultaneous archive saves of one file must leave one valid archive and no temporary files.

## `internal static TLogLossResult TLogStorageRestore()`

An entry that could not be saved to blocked storage must be reported as lost once storage recovers.
The captured summaries show that report arriving after the recovery.
