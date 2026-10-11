# LJobSalvage.cs
Hash: `76b7a983f90d8f57`

## `private IReadOnlyList<string> lJobSalvaged = Array.Empty<string>()`

The salvage recoveries this run extracted from the original source, awaiting the terminal outcome.
Empty when salvage was off or recovered nothing, in which case the single-output path is left untouched.

## `private void LJobSalvageRecord()`

Each recovery becomes a completed Fix item sharing the source item's batch, lineage and relay fields.
They are filed as delivered, so the Roster and the relay path treat them like any finished Fix output.
