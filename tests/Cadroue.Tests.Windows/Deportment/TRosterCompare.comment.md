# TRosterCompare.cs
Hash: `2fb7f105cc4931b9`

## `public sealed class TRosterCompare`

Checks the source against output comparison lines of the roster detail plan.
The lines are plain records, so no control is needed.
The Schedule collection keeps it from running beside tests that share the schedule.

## `public void Compare_FlagsOnlyTheOutputWhenItDiffers()`

The source side is never flagged.
Only the output side is marked changed, and only when it differs.

## `public void Compares_StartWithAHeadRow_AndReadMeasuringUntilTheSourceIsMeasured()`

The first row is a heading for both sides.
Until the source is measured its lines show a measuring text instead of values.

## `public void Compares_ShowDimensionFpsAndDurationChanges()`

Size, frame rate and duration are compared one per row.
A final row labels the two columns.
A video with no sound reports no sound section.

## `public void Sounds_AppearWithASampleRate_AndFlagCodecBitrateAndLoudnessChanges()`

The sound section exists once a sample rate is known.
Codec, bitrate and sample rate are flagged when they differ.
Without an active audio step the output shows the source loudness.
So the loudness row is not flagged, despite the test name.
