# QSectionRow.cs

## `public sealed record QSectionRow(`

The record holds the strings and flags one section row shows, so the veneer draws without reading the gate.
The unnamed flag lets the veneer style the placeholder differently from a real name.
Test `Rows_ComeFromPieces_WithNumbersTimesAffixesAndSelection` pins the numbers, times, affixes and selection.

## `public static QSectionRow QSectionRowCreate(CSectionRow lRow)`

An unnamed row shows a numbered default name from the localization catalog, and a named row shows its name.
The number is the index plus one.
Each affix gets a slash separator prefix, and the span is wrapped in parentheses.

## `private static string QSectionTimeFormat(TimeSpan lTime)`

Hours show only when the time reaches one hour.
Minutes show without padding in the short form, and seconds are always two digits.
