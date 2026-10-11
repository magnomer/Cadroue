# PSOptionsTimeline.cs
Hash: `e738c2770ab96f00`

## `private UIElement PSTimelineBuild()`

Builds the Timeline page with the strip order, keyframe, overlap and waveform plates.
It also hosts the section palette editor plate.

## `private void PSSpectrumListBuild()`

Reloads the palette registry from disk and rebuilds every palette row.
It runs again after each load or remove, so the list always matches the files.
LSSpectrum falls back to the default palette when the selected one is no longer listed.

## `private void PSSpectrumLoad()`

Imports a chosen palette file through LSSpectrum and selects it when it loads.
A reserved name, a failed copy or an unreadable file each get their own warning.

## `private void PSSpectrumSave()`

Exports the selected palette under a file name the user picks.

## `private UIElement PSSpectrumLineBuild(string pName, Border pRow)`

Only the default palette is fixed, so it alone gets no remove button.

## `private void PSSpectrumRemove(string pName)`

Asks before removing a palette, with a different message for a built-in palette.
The list is rebuilt only when the removal succeeded.

## `private void PSSpectrumHoverApply(string pName, bool pHovered)`

The selected row keeps its accent, so hover never changes it.
