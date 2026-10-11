# LSubsidiary.cs
Hash: `438a92875dd83fd9`

## `internal static class LSubsidiary`

The single serial owner of every non-job ffmpeg or ffprobe measurement the worklist needs.
It measures each added source, and the loudness of each finished output.
One worker runs one item at a time, so at most one measuring child exists here.
Two queues feed that worker, and finished-output loudness jumps ahead of queued source measurement.
Every item first waits while a station is processing, whatever its queue.
So its whole-file reads never seek against a running encode on a spinning disk.
Native byte size is never queued here, since `LMessenger` reads it at add time.

## `public static void LSubsidiaryOutputDefer(LWorkItem lSubsidiaryItem, string lSubsidiaryOutputPath)`

Queues the integrated-loudness measurement of one finished output on the high queue.
It waits only for the measurement already in flight, never for pending source items.

## `public static void LSubsidiarySourceDefer(IReadOnlyList<LWorkItem> lSubsidiaryItems)`

Queues each added item's source measurement on the low queue.
That covers the probe, the keyframe interval and the loudness.

## `public static void LSubsidiaryCancel()`

Aborts all measurement when the worklist is cleared.
It drops everything queued and cancels the in-flight measurement, so the employer kills its child at once.
A fresh token source arms the next run.

## `private static async Task LSubsidiarySourceRun(LWorkItem lSubsidiaryItem, CancellationToken lSubsidiaryToken)`

A merge item records only its per-source byte sizes, since one probe cannot describe several sources.
The duration prefers the item's own end, and falls back to the summed measured durations.

## `private static async Task<LSubsidiarySample> LSubsidiarySampleRead(`

A measured source is reused while the file is unchanged in path, length and write time.
Only a new or changed file is measured afresh.
A file that cannot be keyed is measured every time and never cached.

## `private static void LSubsidiaryDefer(Action lSubsidiaryAction)`

The schedule mutation raises UI events and touches the depot.
So it runs on the post thread the rest of the worklist writes on.
It falls back to inline when no post owner is wired.

## Inline notes

### `CancellationToken lSubsidiaryToken = lSubsidiaryCancellation.Token`

Measurement reads the file end to end, through the keyframe scan and the loudness decode.
A running job reads the same disk, and on a spinning drive the two stall each other.
Measurement is never urgent, so every item waits until no post is processing.
That includes a finished-output item from the high queue.

### `catch (OperationCanceledException)`

The in-flight measurement was cancelled by a clear, so both queues are already drained.

### `if (!lSubsidiaryHigh.IsEmpty || !lSubsidiaryLow.IsEmpty)`

An item queued between the queues draining and the busy flag clearing would otherwise wait for the next add.
Restarting the worker picks it up.
