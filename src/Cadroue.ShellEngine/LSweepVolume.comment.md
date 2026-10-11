# LSweepVolume.cs
Hash: `4e488411217ff098`

## `public static partial class LSweep`

The volume and loudness detection of Smart Split, one metric stream turned into boundaries.

## `private const double LSweepVolumeFloor = -70.0;`

Samples at or below this level are silence placeholders, so they never count as measurements.

## `public static string LSweepVolumeFormat(string lSweepSource, LDetectorMetricMode lSweepMode)`

Loudness mode measures momentary LUFS, any other mode measures the RMS level per window.
Only the first audio stream is mapped, and no output is written.

## `public static IReadOnlyList<LSweepSample> LSweepVolumeParse(IEnumerable<string> lSweepLines)`

A level pairs with the latest timestamp line before it, so a level seen before any timestamp is dropped.
Levels not above the floor are dropped as well.

## `private static double? LSweepLoudnessRead(string lSweepLine, string lSweepKey)`

Reads the token after the key up to the next whitespace.
Unparseable text yields null, so it is skipped like a missing value.

## `public static async Task<IReadOnlyList<TimeSpan>> LSweepVolumeScan(`

A blank source returns no boundaries without running ffmpeg.
The duration only scales the progress report.
Raw boundaries are filtered by the minimum length last.
