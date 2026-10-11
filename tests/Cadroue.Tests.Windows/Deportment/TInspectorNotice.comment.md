# TInspectorNotice.cs
Hash: `b968f4dcb1dc4052`

## `public sealed class TInspectorNotice`

Checks that each inspector section deportment raises its change notice only on a real change.
The viewer redraws on every notice, so a repeated or clamped-away value must stay silent.

## `public void ToneContrast_ClampsToRange_ThenStaysQuiet()`

A value is clamped before it is compared.
Setting the clamped limit afterwards counts as no change.

## `public void GammaSlot_ChangesOnlyThatSlot_ResetRaisesOnce()`

Each slot is independent of the others.
Reset raises once when something changed and nothing when already at the defaults.

## `public void ExposureValue_ClampsToStops_SameValueIsSilent()`

An out-of-range exposure is clamped to the limit before the change check.

## `public void SkipFlags_RaiseOnlyOnChange()`

Setting a flag to the state it already holds raises nothing.

## `public void CropboxState_SameCropAndRatio_RaiseNothing()`

Equal crop values and an unchanged ratio must not raise the state notice.
