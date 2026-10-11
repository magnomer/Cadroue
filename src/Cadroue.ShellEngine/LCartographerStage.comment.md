# LCartographerStage.cs
Hash: `758bc2c824311b83`

## `public static partial class LCartographer`

Delivers a file into a plan stage, running automatic stages and routing funnel stages.

## `private static bool LCartographerStageAccept(`

A stage already visited in this delivery ends it, so a cyclic plan cannot recurse forever.
The file is saved as a pending input first, so it survives a restart before the stage runs.
A stage without automatic relay hands the file to its tab and pauses the plan there.
A Merge stage waits while other work in the batch can still reach it.
Missing or unreadable inputs are dropped and recorded through LTraceEngine.

## `private static bool LCartographerFunnelAccept(`

A file no rule routes onward stays in the funnel tab.
A routed file leaves the funnel's pending inputs only after the target stage accepts it.

## `private static async void LCartographerStageCommit(`

Pending inputs are cleared only for paths the stage run acknowledged.
Unacknowledged inputs stay pending, so a later delivery can run them again.
A failed run is recorded through LTraceEngine and leaves every input pending.

## `public static bool LCartographerMergeCheck(`

Reports whether batch work that can still reach the merge stage has not arrived yet.
Failed and cancelled work never arrives, so it never holds the merge.
Done work already delivered or pending at the merge counts as arrived.
