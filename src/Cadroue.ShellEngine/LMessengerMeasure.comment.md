# LMessengerMeasure.cs

## `private static void LMessengerSourceResolve(IReadOnlyList<LWorkItem> lMessengerItems)`

Each added item's byte size is read natively and recorded the instant it is added.
That is a cheap Windows file-length read that never waits on anything.
Its media figures (probe, keyframe interval, loudness) are then deferred to `LSubsidiary`.
That is the single serial ffmpeg/ffprobe measurement worker, which yields to running jobs.
Until it reaches an item those rows show "Measuring".
Byte size never waits behind a running job, so it appears at once.

## `public static void LMessengerMeasureCancel() => LSubsidiary.LSubsidiaryCancel()`

Abort all background measurement (Clear all): drops everything still queued in `LSubsidiary` and kills the in-flight ffprobe/ffmpeg child at once.
