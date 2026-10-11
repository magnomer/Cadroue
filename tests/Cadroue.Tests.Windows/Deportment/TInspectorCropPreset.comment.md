# TInspectorCropPreset.cs
Hash: `8aca5440852f2391`

## `public sealed class TInspectorCropPreset`

Checks the crop section ratio presets, custom ratio commits and lenient lock of the inspector deportment.
No control is built.
Every test starts from a fresh inspector, and all but one give it a known source size.

## `public void PresetSelect_Index_SetsRatioAndReadsBack(int index, string width, string height)`

Each preset index fixes one ratio and reads back as that width and height text.
The custom row stays hidden while a preset is chosen.

## `public void PresetSelect_FitsInsideSource(double sourceWidth, double sourceHeight)`

The crop a preset produces must lie inside the source for landscape, portrait and odd sizes.
Without that the crop box could leave the picture.

## `public void PresetSelect_NoSource_LeavesRatioFree()`

A ratio cannot be fitted without a source size.
The selection falls back to custom and the ratio stays free.

## `public void PresetSelect_Custom_AfterPreset_ResetsRatio()`

Choosing custom after a preset drops the fixed ratio.
A stale width text then reads zero, and an empty height text stays empty.

## `public void RatioCommit_EchoAfterPreset_KeepsPreset()`

The veneer echoes the formatted ratio and edges back as untyped commits.
Those echoes must not change the chosen preset, the lenient lock or the crop.

## `public void RatioCommit_CustomText_KeepsIndexZero_FixedShowsMismatch()`

A typed ratio that matches no preset stays custom.
When the ratio is fixed the section shows a notice that the crop does not match it.

## `public void LenientSet_WithinTolerance_HidesNotice_LenientNeedsFixed()`

Lenient only applies to a fixed ratio and is refused otherwise.
A crop within tolerance of the ratio shows no notice.

## `public void RatioFormat_FreeRatio_ShowsNormalizedCropSize()`

With a free ratio the boxes show the current crop size reduced to its smallest ratio.
A text that already reads the shown number is kept as typed.

## `public void ApplySet_Off_ResetsRatio_OnKeepsIt()`

Turning the crop apply off clears the fixed ratio.
Turning it on again keeps the ratio.
