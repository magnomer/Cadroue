# LMediaEngine.cs
Hash: `53b198638ccc92a6`

## `public static class LMediaEngine`

The single ShellEngine owner of the media reads more than one engine owner needs.
The engine never names the Media reader, so the host assigns each seam at startup.
Neither read claims the analysis lane, so worklist callers run them ungated.

## `public static Func<string, CancellationToken, int?, Task<LMediaInfo>> LMediaProbeSeam { get; set; } =`

Reads the ffprobe facts of one file at an optional priority level.
Unattached, the read faults with an invalid operation, which every caller already treats as unreadable.

## `public static Func<string, CancellationToken, Task<double?>> LMediaLoudnessSeam { get; set; } =`

Reads the integrated loudness of one file, or null when it cannot be measured.
Unattached, it reads null, so a measurement simply carries no loudness.
