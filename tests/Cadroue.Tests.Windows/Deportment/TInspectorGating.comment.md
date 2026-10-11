# TInspectorGating.cs
Hash: `9a3737bfa66402db`

## `public sealed class TInspectorGating`

Checks the small state gates of the inspector and its sections.
They cover source presence, silent repeats, capability switches and the once-only latches.
Notices are counted to prove that repeated values stay silent.

## `public void Source_ZeroSize_ReadsAbsent()`

A source with a zero dimension counts as no source and its size reads as zero.
Every set still raises a notice.

## `public void Step_SameName_IsSilent()`

Choosing the step already shown raises no notice.
Minimizing is a separate state and does raise one.

## `public void Capable_Off_TurnsPreviewOff()`

A section that loses the preview capability stops previewing.
It cannot remain on while the engine cannot show it.

## `public void ToneCapable_RepeatedValue_IsSilent()`

Setting the capability to its current value raises no notice.

## `public void WhitebalanceCapable_Off_DisarmsPicker()`

Losing the capability disarms an armed picker.
While incapable, the picker cannot be armed again.

## `public void Owner_FirstPath_ReadsFirst()`

The first owner set reports true so the caller can tell a first owner.
Later sets report false and still replace the stored path.

## `public void Failure_SamePath_RecordsOnce()`

A save failure is traced once per path, ignoring case.
Clearing the latch with a null path allows the same path to be traced again.
