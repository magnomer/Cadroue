# PEditTab.cs
Hash: `6a97066b2533ddca`


## `public sealed class PEditTab : PTabSurface`

The Edit tab surface, which only builds controls and attaches them to their Deportment owner.
Every decision lives in `LEditTab`.
The class keeps the two debounce timers because timers are Veneer objects.

## `public PEditTab(LPresetSelection lPresetOwner, LSceneTabEntry? lPreferenceTabLayout = null)`

Builds `LEditTab` first, so every Veneer subscription can reach its siblings.
Capability gating runs after the processing rows exist, so each row starts with the right enabled state.
The saved layout is applied once the grid holds every panel.
Start runs after the layout, so nothing restored is overwritten.

## `public override void PTabClose()`

Both timers stop before the Deportment closes, so no tick reaches a closed tab.

## `private void PEditHistogramDefer(TimeSpan lPosition)`

The viewer clock reports every position change, so each tick restarts the histogram wait.

## `private void PEditHistogramDefer()`

Restarting the timer coalesces a burst of requests into one histogram frame read after the pause.

## `private void PEditColorDefer()`

Restarting the timer coalesces slider drags into one color apply after the pause.
