# QViewfinderRuler.cs
Hash: `7e84ed1b66f3f004`

## `public static class QViewfinderRuler`

Builds the time ruler ticks drawn in the viewfinder lane above the rail.
It holds no state, so the viewfinder calls it once per frame.

## `internal static IReadOnlyList<QViewfinderGraduation> QViewfinderTicksResolve(`

Picks the smallest fixed step that keeps ticks at least 100 pixels apart, up to one hour.
Ticks sit on whole multiples of the step, so labels stay round while scrolling.
