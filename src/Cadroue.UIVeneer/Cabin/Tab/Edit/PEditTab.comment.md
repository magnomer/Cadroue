# PEditTab.cs

## `public sealed class PEditTab : PTabSurface`

The Edit tab composes list, processing, inspector, viewer, flow, action and Export surfaces around `QEditTab`.
The class keeps two debounce timers.
The color timer waits 80 ms, and the histogram timer waits 220 ms.

## `public PEditTab(QWorkspace lWorkspace, PExport pExport, LSceneTabEntry? lPreferenceTabLayout = null)`

The constructor receives `PExport` through its parameter.
It places Export with list, processing, inspector and viewer panels, then coordinates their locking.

The constructor creates its controls and `QEditTab` before attaching their cross-surface events.
Capability gating runs after the processing rows are added.
List locking covers processing, inspector and the supplied Export surface together.
The saved layout is applied once the grid holds every panel.
`QEditStart` runs after the saved layout is applied.

## `public override void PTabClose()`

Both timers stop before `QEditTab` closes.
The base close runs last.

## `private void PEditHistogramDefer(TimeSpan lPosition)`

Each viewer clock tick defers the histogram timer.
The histogram and color deferred events from `QEditTab` defer their corresponding timers.
The timer callbacks run `QEditHistogramRun` and `QEditColorApply`.

Missing and incompatible preset notifications route to the shared Export notices.
The supplied Export surface does not decide them.
