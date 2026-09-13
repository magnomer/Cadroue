# LFixItemsCreate.cs

## Inline notes

### `string lFixExtension = Path.GetExtension(lFixSourcePath).TrimStart('.')`

Fix is a source-representation pass-through: the copy stage keeps the source container and stream layout.
So the destination extension must mirror the source, never the export preset's container.
