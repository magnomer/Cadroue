# LSchedule.cs
Hash: `9350a8c10c0f77a3`

## `public sealed partial class LSchedule : LScheduleContract`

The schedule over the depot folders, holding the visible work items and the live claimed ones.
It reads no Application type, so preference reads arrive through its seams.

## `public void LScheduleLoad()`

Recovers stale work before reading, so a reload never shows a dead owner as running.
The index is rebuilt or bypassed when it is marked dirty.

## `private void LScheduleItemsBuild(IEnumerable<LScheduleEntry> lScheduleEntries)`

A running record this launch claimed keeps its live item, so progress shown on it survives a reload.
A live item whose record left the running folder is dropped from the claimed table.
Records outside this launch's scope never reach the visible list.

## `public bool LSchedulePendingExist() =>`

Reads the scheduled folder, not the visible list, so work another launch filed counts too.
Only records this launch may claim count, so the answer matches what a claim would find.

## `private static LWorkState LScheduleStateRead(LDepotFolder lDepotFolder, LWorkState lScheduleFileState) =>`

The folder a record sits in decides its state over the state stored in its file.
A done or failed record keeps a finer terminal state stored in its file.
A scheduled record still stamped running reads as pending, since no runner holds it.
