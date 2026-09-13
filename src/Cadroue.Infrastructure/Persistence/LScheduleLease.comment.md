# LScheduleLease.cs

## `private static readonly LDepotFolder[] lScheduleBytesFolders`

The folders an added-but-not-yet-done item can be filed under.
Source byte size lands on the item here the instant the file is added, without waiting for the deferred measurement.

## `private static readonly LDepotFolder[] lScheduleOutputFolders`

The folders a finished item can be filed under.
Output loudness lands on it here once the deferred measurement of the finished output completes.

## `public void LScheduleBytesSet(Guid lWorkId, long? lWorkSourceBytes, IReadOnlyList<long> lWorkMergeBytes)`

Native byte size, recorded immediately at add time.
Unlike `LScheduleSourceSet` it never marks the source measured or touches duration/media.
Those come from the deferred probe/keyframe/loudness pass, so the "Measuring" rows stay measuring until that pass lands.

## `public void LScheduleLoudnessSet(Guid lWorkId, double lWorkLoudness)`

The finished output's integrated loudness, measured off the runner loop and recorded here once ready.
Updates the stored output-media snapshot so a reload keeps the figure.
