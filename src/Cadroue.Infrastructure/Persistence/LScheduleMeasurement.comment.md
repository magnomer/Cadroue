# LScheduleMeasurement.cs

## `private static readonly LDepotFolder[] lScheduleSourceFolders`

The folders an added-but-not-yet-done item can be filed under: scheduled, running and cancelled.
The persisted source figures go to the first of these folders that holds the record.

## `private static readonly LDepotFolder[] lScheduleOutputFolders`

The folders a finished item can be filed under: done and failed.
Output loudness lands on it here once the deferred measurement of the finished output completes.

## `public void LScheduleSourceSet(`

Source figures are measured once by the deferred pass after the file is added to the worklist.
Duration is only filled when the item's end is still unset, since a planned range already set is authoritative.
The media snapshot, source bytes and per-input merge bytes are recorded when given.
So the job run never re-measures the source and a deleted source keeps its figures.

## `public void LScheduleBytesSet(Guid lWorkId, long? lWorkSourceBytes, IReadOnlyList<long> lWorkMergeBytes)`

Native byte size, recorded immediately at add time.
Unlike `LScheduleSourceSet` it never marks the source measured or touches duration or media.
Those come from the deferred pass, so the measuring rows stay measuring until that pass lands.

## `public void LScheduleLoudnessSet(Guid lWorkId, double lWorkLoudness)`

The finished output's integrated loudness, measured off the runner loop and recorded here once ready.
It updates the stored output-media snapshot so a reload keeps the figure.
