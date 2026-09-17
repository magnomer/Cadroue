# PInspectorWhitebalanceNeutral.cs

## `private ToggleButton PWhitebalancePickerBuild(`

One eyedropper toggle.
Grey samples a neutral point (strict).
White samples any point on the black-to-white axis (lenient).
Both feed one correction pipeline, differing only in the target they hand the viewer's sampler.

## `private void PWhitebalancePickerSelect(LNeutralTarget pTarget)`

Make this picker the active one: deactivate its peer and the crop tool without firing their disarm side effects.

## Inline notes

### `if (PWhitebalancePeerRead(pTarget).IsChecked == true)`

Switching to the other picker: it will arm the tool itself.
