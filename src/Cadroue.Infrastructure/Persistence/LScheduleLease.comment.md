# LScheduleLease.cs
Hash: `f4c2a3160c41cace`

## `public static Func<bool> LScheduleSharedSeam { get; set; } = () => false;`

Whether the worklist is shared across launches, read through a seam the host binds to the preference.
Unattached, it answers false, as an empty preference would, so only this launch's work and unsigned work match.

## `public static Func<Guid, DateTimeOffset> LScheduleTimeSeam { get; set; } = _ => DateTimeOffset.MinValue;`

Reads the origin time encoded in a batch id, so older batches are claimed first.
It decodes an id rather than reading a clock, so no clock default fits.
Unattached, every batch reads the same minimum time, so the creation time alone orders claims.

## `internal static bool LScheduleSignetMatch(LWorkDocument lWorkRecord) =>`

A record with no signet predates signets, so every launch may claim it.

## `internal static bool LScheduleRelayMatch(LWorkDocument lWorkRecord) =>`

Relayed work stays with the process it was relayed to while that process lives.
Once the sentinel finds that owner gone, any launch may claim the record.

## `public LWorkItem? LScheduleClaim(Guid lRunnerId)`

A shared worklist recovers stale work first, since another launch may have died holding it.
High priority goes first, then older batches, then older items.
A record another runner moved first is skipped, so two runners never claim the same work.

## `public int LScheduleStaleClaim() => LScheduleStale.LScheduleStaleClaim();`

The contract entry for stale recovery, whose whole role lives in `LScheduleStale`.

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

## `internal static void LSchedulePartialRemove(LWorkDocument lWorkRecord)`

Deletes the half-written output of an interrupted run before it is queued again.
An output path naming one of the sources is never deleted, so a source is never lost.
A file that cannot be deleted is left in place, so the release still goes ahead.
