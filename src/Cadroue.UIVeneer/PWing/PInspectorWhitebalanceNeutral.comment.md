# PInspectorWhitebalanceNeutral.cs

## `private ToggleButton PWhitebalancePickerBuild(LNeutralTarget pTarget, Image pPickerIcon, string pTooltipKey)`

One eyedropper toggle.
Grey samples a neutral point (strict).
White samples any point on the black-to-white axis (lenient).
Both feed one correction pipeline, differing only in the target they hand the viewer's sampler.

## `private void PWhitebalanceToolUpdate()`

Writes both toggles from `LWhitebalance` and raises the viewer notice only when the armed state or target changed.
