# TInspectorValueBox.cs
Hash: `dadf97eaec6c5853`

## `public sealed class TInspectorValueBox`

Checks the number box commit, format and slide rules shared by every inspector value box.
They are static reads, so no inspector is built.

## `public void ValueCommit_AboveFloor_KeepsTypedNumber()`

A typed number inside the bounds is kept exactly.

## `public void ValueCommit_Clamps_FallsBackWhenUnparsable()`

Out of range input is clamped to the bounds.
Text that is not a number falls back to the default value.
Missing bounds leave the number unbounded.

## `public void ValueFormat_EchoesEquivalentText_ElseFormats()`

Text that already parses to the value is echoed unchanged.
That keeps the user's own formatting from being rewritten while typing.
Any other text is replaced by the value in the given format.

## `public void SlideCommit_OnlyWhenSliderLeavesClampedCurrent()`

A slider move that equals the clamped current value commits nothing.
This stops layout echoes from rewriting a value that sits outside the slider range.
