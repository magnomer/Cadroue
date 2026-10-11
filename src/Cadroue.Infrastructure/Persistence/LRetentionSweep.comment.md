# LRetentionSweep.cs
Hash: `1785584a13e76093`

## `public static Func<string, bool> LRetentionSweptSeam { get; set; } = _ => false;`

The rule deciding whether a depot-relative path may be swept at all.
The host binds it to the retention rules, so this file keeps only the walk and the delete.
Unattached, it protects every path, so nothing is deleted.

## `public static Func<DateTime, DateTime, int, bool> LRetentionExpiredSeam { get; set; } = (_, _, _) => false;`

The rule deciding whether a file's write time is older than the kept days.
Unattached, no file ever expires, so nothing is deleted.

## `public static void LRetentionSweepStart(bool lRetentionActive, int lRetentionDays)`

Runs the sweep off the calling thread, so startup never waits on a folder walk.

## `public static int LRetentionRun(int lRetentionDays)`

Skips the whole sweep while any job runs, so no file a running job needs disappears.
The index is rebuilt and compacted afterwards, so it no longer lists removed records.

## `private static bool LRetentionFileRemove(string lRetentionPath, DateTime lRetentionNow, int lRetentionDays)`

Deletes through an exclusive handle, so a file another process holds open is left alone.
