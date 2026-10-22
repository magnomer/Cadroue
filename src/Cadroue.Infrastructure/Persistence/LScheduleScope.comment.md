# LScheduleScope.cs

## `public static Func<bool> LScheduleSharedSeam { get; set; } = () => false;`

Whether the worklist is shared across launches, read through a seam the host binds to the preference.
Unattached, it answers false, as an empty preference would, so only this launch's work and unsigned work match.

## `internal static bool LScheduleSignetMatch(LWorkDocument lWorkRecord) =>`

A record with no signet predates signets, so every launch may claim it.

## `internal static bool LScheduleRelayMatch(LWorkDocument lWorkRecord) =>`

Relayed work stays with the process it was relayed to while that process lives.
Once the sentinel finds that owner gone, any launch may claim the record.
