# LScheduleSource.cs

## `public void LScheduleSourceSet(`

Source figures measured once when the file is added to the worklist.
Duration is only filled when still unknown, since a planned range already set is authoritative.
The media snapshot, source bytes, and per-input merge bytes are always recorded.
So the job run never re-measures the source and a deleted source keeps its figures.
