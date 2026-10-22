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
