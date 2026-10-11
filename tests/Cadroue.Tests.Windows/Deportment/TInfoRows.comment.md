# TInfoRows.cs
Hash: `f1cb027c28277104`

## `public sealed class TInfoRows`

Checks the row records the info deportment reads for the media panel, without any control.
The media tests raise a notice on a real viewer and read the rows back.
Row kinds are compared apart from separators.

## `public void NoMedia_ShowsOneMutedNotice_WithoutSeparator()`

An empty viewer shows a single muted row and nothing around it.

## `public void Video_ListsStatusDurationSizeRateCodecs_SeparatedBetweenRows()`

A video lists the status rows first and then the text rows in a fixed order.
Separators sit between rows and never lead.
One media notice raises exactly one change.

## `public void AudioOnly_ShowsCodecRateChannels_LongDurationWithHours()`

An audio-only file drops the picture rows and adds sample rate.
A duration of an hour or more includes hours.

## `public void Errors_ShowMutedShortenedLines_AfterBadStatus()`

An error follows the bad status rows as a muted line.
A long message is shortened so the panel does not grow without bound.
