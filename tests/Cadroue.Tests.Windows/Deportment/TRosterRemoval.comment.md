# TRosterRemoval.cs
Hash: `22a5fe627f65f223`

## `public sealed class TRosterRemoval`

Checks how the roster removes a batch card without any control.
Each test saves the preference and restores it afterwards, because the confirm switch is process-wide.
The Schedule collection keeps it from running beside tests that share the schedule.

## `public void Remove_WithoutConfirmPreference_RunsAtOnce()`

With the destructive confirmation off, no question is raised.
Only the removed batch leaves the schedule and its card leaves the roster.

## `public void Remove_WithConfirmPreference_AsksAndHonoursTheAnswer()`

A refusal keeps every job.
An approval removes the whole batch.
The question carries a title and a text.

## `public void Remove_UnknownBatch_NeitherAsksNorWarns()`

A batch the roster does not hold is ignored silently.
No question and no warning are raised and the schedule is unchanged.
