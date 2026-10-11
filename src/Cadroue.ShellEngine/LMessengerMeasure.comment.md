# LMessengerMeasure.cs
Hash: `0281727284b7ee4a`

## `private static void LMessengerSourceResolve(IReadOnlyList<LWorkItem> lMessengerItems)`

Each added item's byte size is read natively and recorded the instant it is added.
That file-length read never waits on anything, so the size appears at once.
The media figures are then deferred to `LSubsidiary`, the serial measurement worker that yields to running jobs.
Until the worker reaches an item, its row carries no media figures.

## `private static void LMessengerBytesSet(LScheduleContract lMessengerSchedule, LWorkItem lMessengerItem)`

A merge item records one size per source and no single size.
An unreadable merge source counts as zero bytes, so the list keeps one entry per source.

## `public static void LMessengerMeasureCancel() => LSubsidiary.LSubsidiaryCancel();`

Aborts all background measurement when the worklist is cleared.
It drops everything queued in `LSubsidiary` and kills the in-flight child at once.
