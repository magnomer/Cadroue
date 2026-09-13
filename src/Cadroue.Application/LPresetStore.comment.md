# LPresetStore.cs

## `private static bool LPresetTransactionRun(Func<bool> lPresetMutate)`

Every mutation is staged the same way: change memory, write storage, and keep the change only if the write lands.
A failed or blocked write restores the catalogue exactly as it was.
So nothing stays committed in memory that storage does not hold.

## `private static void LPresetMergeApply(IReadOnlyList<LPresetRecord> lPresetRecords)`

The merged list is what storage now holds, including anything another process committed.
So the live catalogue adopts it instead of keeping a view storage no longer matches.
