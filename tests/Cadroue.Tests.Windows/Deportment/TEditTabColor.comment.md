# TEditTabColor.cs
Hash: `2f083e3d4b1f46f8`


## `public sealed class TEditTabColor`

Protects the color paths of the Edit tab: histogram, white balance samples and preview color.
Each test builds a real tab over a seeded preset.

## `public void HistogramApply_Frame_ReachesCurveBins()`

A decoded frame must reach the curve section as per-channel counts.
A missing frame clears the counts instead of leaving the previous ones on display.

## `public void NeutralHandle_ValidSample_SetsManualGains()`

A picked neutral sample switches white balance to the manual method and activates its step.
Otherwise the gains would be stored but never applied.

## `public void EstimateHandle_ManualStaysQuiet_AutomaticAsksViewer()`

The manual method needs no automatic estimate, so only an automatic method asks the viewer for one.
An estimate result must reach the wheel as a present dot.

## `public void ColorApply_SkipActive_PushesNeutralColor()`

Activating Skip must push a neutral color to the preview, so skipped edits are not shown.
The first push carries the brightness, so the final neutral push comes from Skip.
