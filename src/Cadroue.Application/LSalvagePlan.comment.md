# LSalvagePlan.cs

## Inline notes

### `return LSalvageSourceMatch(Path.Combine(lSalvageFolder, lSalvageFileName), lSalvageSourcePath)`

Fix/salvage keeps the source container.
The destination extension mirrors the source, never the export preset's container, and must never equal the source path.
