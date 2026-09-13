# LSubsidiary.cs

## `internal static class LSubsidiary`

The single serial owner of every non-job ffmpeg/ffprobe measurement the worklist needs.
That is source probe/keyframe/loudness when a file is added, and finished-output loudness when a job ends.
One low-priority worker runs one item at a time, so at most one ffmpeg/ffprobe child ever exists here.
Two priority lanes share that one worker: finished-output loudness (high) jumps ahead of queued source measurement (low).
Whatever the lane, every item first yields while a station is processing.
So its whole-file disk reads never seek against a running encode on a spinning disk.
Native byte size is never queued here — it is read instantly at add time, off this worker (see `LMessenger`).

## `public static void LSubsidiaryOutputDefer(LWorkItem lSubsidiaryItem, string lSubsidiaryOutputPath)`

Queue a finished output's integrated-loudness measurement at high priority.
It runs before any pending source measurement, waiting only for the single in-flight measurement to finish.

## `public static void LSubsidiarySourceDefer(IReadOnlyList<LWorkItem> lSubsidiaryItems)`

Queue each added item's source measurement (probe, keyframe interval, loudness) at low priority.

## `public static void LSubsidiaryCancel()`

Abort all measurement (Clear all).
Drop everything still queued and cancel the in-flight probe so its ffprobe/ffmpeg child is killed at once.
A fresh token source arms the next run.

## `private static LSubsidiarySample LSubsidiarySampleRead(`

A measured source is reused whenever the file is unchanged (same path, length, and write time).
Only a new or changed file is measured afresh.

## `private static void LSubsidiaryDefer(Action lSubsidiaryAction)`

The schedule mutation raises UI events and touches the depot.
Route it onto the post thread the rest of the worklist writes on.
Fall back to inline when no post owner is wired.

## Inline notes

### `CancellationToken lSubsidiaryToken = lSubsidiaryCancellation.Token`

Measurement reads the file end to end (keyframe scan, loudness decode).
A running job reads the same disk.
On a spinning drive the two sets of reads seek against each other and stall the encode.
Since measurement is never urgent, hold every item until no post is processing.
That includes a high-priority output item.
So its disk work only runs while the drive is otherwise idle.

### `catch (OperationCanceledException)`

The in-flight probe was cancelled (Clear all), so both queues are already drained.

### `if (!lSubsidiaryHigh.IsEmpty || !lSubsidiaryLow.IsEmpty)`

An item queued between the queues draining and the flag clearing would otherwise wait for the next add.
Restart the worker to pick it up.
