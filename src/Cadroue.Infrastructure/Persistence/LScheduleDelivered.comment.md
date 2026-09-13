# LScheduleDelivered.cs

## `public IReadOnlyList<LWorkItem> LScheduleDeliveredAdd(IReadOnlyList<LWorkItem> lWorkItems)`

Files already-finished derived outputs (e.g.
Fix salvage recoveries) straight into the Done store as completed work.
Each item is expected to arrive terminal, carrying the source item's batch and lineage.
So it flows through the existing Roster/Summary grouping and the relay path exactly like any other finished output.
