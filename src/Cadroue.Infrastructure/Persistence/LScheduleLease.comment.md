# LScheduleLease.cs

## `public static Func<Guid, DateTimeOffset> LScheduleTimeSeam { get; set; } = _ => DateTimeOffset.MinValue;`

Reads the origin time encoded in a batch id, so older batches are claimed first.
It decodes an id rather than reading a clock, so no clock default fits.
Unattached, every batch reads the same minimum time, so the creation time alone orders claims.

## `public bool LSchedulePendingExist() =>`

Reads the scheduled folder, not the visible list, so work another launch filed counts too.
Only records this launch may claim count, so the answer matches what a claim would find.

## `public LWorkItem? LScheduleClaim(Guid lRunnerId)`

A shared worklist recovers stale work first, since another launch may have died holding it.
High priority goes first, then older batches, then older items.
A record another runner moved first is skipped, so two runners never claim the same work.
