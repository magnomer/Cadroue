# CEdit.cs
Hash: `abeca5b8896a860f`

## `public enum CEditVerdict`

A run's answer to the driver: Missing and Incompatible raise the preset notices, Empty does nothing, Ran means the run went.

## `public sealed class CEdit`

The Edit runs' gate, one per atelier, built by `CAtelier` from `LEditPort` and reached as `CAtelierEdit`.
It decides the preset verdict and which docket items run; the preset owner answers valid and supported.
Open row: the single run makes no engine call, as its plan reaches a gate only once the inspector tools are dismantled.

## `public (CEditVerdict CEditVerdict, string? CEditPath, Guid CEditBatch) CEditRun(bool presetValid, bool presetSupported, string? path, IReadOnlyList<(string CEditPath, Guid CEditBatch, bool CEditLocked)> items)`

A missing preset is answered before an incompatible one; then the first item matching the current path, ignoring case as the docket's find does, runs only while unlocked.
A run answers that item's path and batch, so the driver dispatches it without reading the docket again.

## `public CEditVerdict CEditAllRun(bool presetValid, bool presetSupported, string presetName, IReadOnlyList<(string CEditPath, Guid CEditBatch, bool CEditLocked)> items, IReadOnlyList<string>? paths, Guid relayTarget, Guid sourceTab)`

After the same preset verdict, picks every unlocked item, or only those among the paths ignoring case, in docket order.
It asks the port once, even with nothing picked, so the engine reports what it queued.
