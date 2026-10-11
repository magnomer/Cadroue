# LKeyframeEngine.cs
Hash: `60e897462ddc17d6`

## `public static class LKeyframeEngine`

The single ShellEngine owner of the keyframe range scan the scouts need.
The engine never names the Media seeker, so the host assigns the seam at startup.

## `public static Func<string, TimeSpan, TimeSpan, CancellationToken, Task<IReadOnlyList<LKeyframeEntry>>>`

Scans the keyframes of one file between two times.
Unattached, it finds none, so interval and bridge measurements fall back to their empty paths.
