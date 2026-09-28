# PFixTab.cs


## Inline notes

### `if (pList.PListEditableRead() is not { } pFixSelected)`

One pass diagnoses every defect kind, so a single button re-runs the whole checklist for the selected file.
Forced: a stored result never short-circuits it.

### `PFixActiveUpdate()`

Processing-row color represents Apply only.
Refresh it from the in-memory plan independently of whether the current plan can or should be persisted.
