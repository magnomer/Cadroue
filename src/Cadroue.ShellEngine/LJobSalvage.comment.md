# LJobSalvage.cs

## `private IReadOnlyList<string> lJobSalvaged = Array.Empty<string>()`

The salvage recoveries this run extracted from the original source, awaiting the terminal outcome.
Empty when salvage was off or recovered nothing, in which case the single-output path is left untouched.

## `private void LJobSalvageRecord()`

Record each salvage recovery as a completed derived work item sharing the source item's batch and lineage.
Then file them as delivered.
So the Roster/Summary display and the relay path treat each like any other finished Fix output.
