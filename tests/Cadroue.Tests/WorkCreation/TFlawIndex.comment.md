# TFlawIndex.cs

## Inline notes

### `LDossier? dossier = TInterface.TFlawIndexResolve(`

Sequential read is clean with and without the index.
Yet a boundary seek fails with a message that never says "index": the addressing is broken.

### `Assert.Null(TInterface.TFlawIndexResolve(`

The sequential read already errors.
So the fault belongs to the container or coded detector that owns that error, not to addressing.
