# PFixTab.cs

## `private static readonly PFixStep[] pFixSteps`

Presentation order is by real-world defect frequency (most common first).
So the defect a user most likely faces is nearest the top.
It deliberately differs from the actual repair order, which `LRemedy` fixes by safety and dependency.
Lossless carriage repairs come first, lossy decode-reencode last.
List position never decides repair semantics.

## Inline notes

### `if (pList.PListEditableRead() is not { } pFixSelected)`

One pass diagnoses every defect kind, so a single button re-runs the whole checklist for the selected file.
Forced: a stored result never short-circuits it.

### `PFixActiveUpdate()`

Processing-row color represents Apply only.
Refresh it from the in-memory plan independently of whether the current plan can or should be persisted.
