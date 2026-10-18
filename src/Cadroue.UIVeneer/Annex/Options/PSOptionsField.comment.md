# PSOptionsField.cs
Hash: `5aab40656f02375f`

## `private static Slider PSOptionsSliderBuild(double pValue, double pMinimum, double pMaximum)`

The stored value is clamped into the slider range, so an out-of-range preference cannot break the page.

## `private static UIElement PSOptionsFieldBuild(string pLabel, Slider pSlider, string pUnit)`

Pairs a slider with a live readout and its unit in the shared field row.
Options pages use it so every slider row aligns the same way.
The readout shows whole numbers only.
