# PSEncoderChip.cs
Hash: `4d1056c98884a56c`

## `private static DataTemplate PSEncoderChipBuild()`

Builds the item template both encoder combos share.
An item text ending in a ` / ` token shows that token as a blue chip on the right.
The name keeps the shared field text colour, while the chip uses its own fixed blue.

## `private sealed class PSEncoderChipConverter : IValueConverter`

Splits one item string at its last separator, so a name may itself contain the separator.
Items without a separator show the whole text and hide the chip.
