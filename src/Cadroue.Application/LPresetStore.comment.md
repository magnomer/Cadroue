# LPresetStore.cs
Hash: `ccd8bda8a34b76df`

## `public static void LPresetPrepare()`

The stores return preset documents raw, so every native and stored record is normalized here first.
When storage loaded, the baseline holds the same normalized records the live catalogue was built from.

## `private static bool LPresetTransactionRun(Func<bool> lPresetMutate)`

Every mutation is staged the same way: change memory, write storage, and keep the change only if the write lands.
A failed or blocked write restores the catalogue exactly as it was.
So nothing stays committed in memory that storage does not hold.

## `private static void LPresetMergeApply(IReadOnlyList<LPresetDocument> lPresetRecords)`

The merged list is what storage now holds, including anything another process committed.
So the live catalogue adopts it instead of keeping a view storage no longer matches.

## `private static bool LPresetPersist()`

The save resolver normalizes the catalogue just read before merging, since the store returns it raw.

## `public static LPresetDocument? LPresetSelectionRead(string lName)`

Adapts the table read to the selection's load seam, which exchanges documents rather than presets.
It lives on the owner, so the store stays free of preset logic and the host assigns a method group.

## `public static bool LPresetSelectionSave(string lName, LPresetDocument lRecord)`

Turns the selection's document into a preset state before the table saves it.

## `public static bool LPresetSelectionSet(string lOldName, string lNewName, LPresetDocument lRecord)`

A rename carries the selection's current document, so the renamed preset never keeps stale values.

## `public static LEncoding LPresetSelectionResolve(LPresetDocument lRecord)`

The output spec is built through the preset state, so a document and a stored preset resolve alike.
