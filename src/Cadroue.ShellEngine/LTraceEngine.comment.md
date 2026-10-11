# LTraceEngine.cs
Hash: `64d78b8fa141f9ce`

## `public static class LTraceEngine`

The single ShellEngine owner of every trace record the engine makes.
The engine never names the Infrastructure trace, so the host assigns each seam at startup.
The test relay assigns the same seams, so tests record through the same targets.
Each seam starts as a no-op, so an engine run without a host stays silent and deterministic.

## `public static Action<string, Exception?> LTraceErrorSeam { get; set; } = (_, _) => { };`

The exception travels unformatted, so the target decides how much of it to keep.

## `public static Action<string, string?> LTraceFfmpegSeam { get; set; } = (_, _) => { };`

Carries the FFmpeg command lines and stderr, which the trace shows only while verbose.

## `public static Func<bool> LTraceVerboseSeam { get; set; } = () => false;`

Lets the engine skip building verbose detail when nobody will keep it.
Unattached, it reads false, so no detail is built.
