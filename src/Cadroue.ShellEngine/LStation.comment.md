# LStation.cs
Hash: `a8ef5397ffbf2d0b`

## `public sealed class LStation`

One labelled processing post that owns a single runner and registers on a process-wide board.
The static seams must be assigned before the first station is built, since the constructor reads them.

## `public static Action<Action>? LStationPost { get; set; }`

The ordered post every runner commit and relay change goes through.
Sharing one post keeps schedule edits from different owners in a single order.

## `public static LStation LStationCreate(string lStationLabel)`

The first visible station adopts the background station, so work it started keeps its runner.
Creating a station retires a leftover background station, but only while its runner is idle.

## `public static LStation LStationInternalRead()`

The background station exists so queued work can run before any worklist tab is open.

## `public static LStation[] LStationBoardRead()`

Falls back to the background station, so callers always see at least one post.

## `public IReadOnlyList<LWorkItem> LStationSelectionRead()`

A failing selection source is recorded through LTraceEngine and reads as an empty selection.

## `public bool LStationBusyCheck() =>`

A suspended runner still counts as busy, because its paused jobs keep their claims.

## `public static bool LStationActiveCheck()`

Background source measurement waits on this before each measurement it runs.
So its whole-file reads never run alongside a job's reads on the same spinning disk.

## `private void LStationAutoApply()`

Starts the runner on a schedule change only while automatic start is on and work is pending.
A paused runner stays paused, so a user pause is never undone by new work.
The runner settings are refreshed from the preference right before each automatic start.

## `public void LStationClose()`

Automatic start is switched off first, so no schedule change restarts a closing station.
Only a station still on the board disposes its runner, so a second close does nothing.
