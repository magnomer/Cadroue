# LDepotIndex.cs

## Inline notes

### `catch (SqliteException)`

An existing column makes this migration redundant.
The catch also suppresses other SQLite failures without checking their cause.
