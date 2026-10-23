# Fix

## `PFixTab`

The Fix surface composes flow, viewer, list, clinic, processing, action and the supplied Export surface around `QFixTab`.
It fills processing from `QFixCatalog` and asks `QFixTab` to attach dispatcher-bound face callbacks.
It applies the saved layout only after building the grid.

Presentation does not own the checkup or workspace subscriptions.
Close delegates to the base surface before calling `QFixClose`.

Missing-preset notices use `PExportNotice.PExportMissingShow`.
List locking covers processing, clinic and Export together.

## Stamp

Commit: `9a34a722aa84`
- `PFixTab.cs` `a6cd2ea9543e793a`
