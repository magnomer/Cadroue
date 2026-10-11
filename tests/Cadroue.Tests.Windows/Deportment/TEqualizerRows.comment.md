# TEqualizerRows.cs
Hash: `a57860e972d5da5f`

## `public sealed class TEqualizerRows`

Checks the row records and choice handling of the equalizer deportment without any control.
The veneer builds one row per band record, so the records must follow the bands.

## `public void Rows_MirrorBands_WithIndexAndText()`

Rows follow the bands one to one, in order, with the frequency text of each band.

## `public void RowsChange_OnlyWhenCountChanges()`

The rows notice fires only when a band is added or removed.
A gain edit raises the value notice alone, so the veneer updates values without a rebuild.

## `public void FrequencyCommit_ParsesAndEchoes_OneChange()`

A valid frequency is stored and echoed back for the box.
Unparseable text keeps the stored value, echoes it, and raises nothing.

## `public void Choice_SelectByIndex_ThenGainEditBreaksMatch()`

Selecting a preset by index records its token.
Editing a gain moves the choice to the custom entry but keeps the token.

## `public void ChoiceResolve_SkipsCustom_AndAlreadyMatched()`

Selecting the custom entry or an out-of-range index resolves to no token.
Selecting the preset that already matches also resolves to nothing.
A drifted value reads as the custom entry.
