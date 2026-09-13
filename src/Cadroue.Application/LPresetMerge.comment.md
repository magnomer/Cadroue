# LPresetMerge.cs

## `internal static IReadOnlyList<LPresetRecord> LPresetMergeCreate(`

Storage is shared: another Cadroue process can have committed changes since this one last read the catalogue.
The merge replays this process's own intent, its difference from the baseline it loaded, onto whatever is durable now.
So a write can neither resurrect what this process deleted nor erase what another process committed.
