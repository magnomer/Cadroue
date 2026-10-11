# TFlawIndex.cs
Hash: `385f197e10b3c072`

## `public void SeekOnlyFailureOverCleanRead_ProducesIndexDossier()`

Sequential read is clean with and without the index.
Yet a boundary seek fails with a message that never says "index": the addressing is broken.

## `public void SeekFailureOverDamagedRead_IsNotAnIndexDefect()`

The sequential read already errors.
So the fault belongs to the container or coded detector that owns that error, not to addressing.
