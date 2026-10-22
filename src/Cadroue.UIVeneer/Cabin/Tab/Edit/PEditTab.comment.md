# PEditTab.cs

## `public sealed class PEditTab : PTabSurface`

The Edit tab surface only builds controls and attaches them to their Deportment owner.
The decisions live in `QEditTab`.
The class keeps the two debounce timers, which are Veneer objects.
The color timer waits 80 ms, and the histogram timer waits 220 ms.

## `public PEditTab(QWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout = null)`

The constructor builds `QEditTab` first, so every Veneer subscription can reach its siblings.
Capability gating runs after the processing rows exist, so each row starts with the right enabled state.
The saved layout is applied once the grid holds every panel.
Start runs after the layout, so nothing restored is overwritten.

## `public override void PTabClose()`

Both timers stop before the Deportment closes, so no tick reaches a closed tab.

## `private void PEditHistogramDefer(TimeSpan lPosition)`

The viewer clock reports every position change, so each tick restarts the histogram wait.
Restarting the timer coalesces a burst of requests into one histogram frame read after the pause.
The color deferral from `QEditTab` restarts the color timer the same way, so slider drags coalesce into one color apply.
