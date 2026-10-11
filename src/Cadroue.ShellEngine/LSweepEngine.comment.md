# LSweepEngine.cs
Hash: `0e2575017dc54b4d`

## `public static class LSweepEngine`

The single ShellEngine owner of the window-analysis lane the detection run shares.
The engine never names the Media lane, so the host assigns each seam at startup.
It stands apart from the sweep, which already holds as many members as it may.

## `public static Func<CancellationToken, Task> LSweepClaimSeam { get; set; } = _ => Task.CompletedTask;`

Claims the shared lane before a detection run starts its ffmpeg.
The lane holds one scan, so a run waits while a keyframe, waveform, checkup or trial scan runs.
Unattached, it completes at once, so a run without a host is never gated.

## `public static Action LSweepReleaseSeam { get; set; } = () => { };`

Releases the lane after the detection run, whether it finished, failed or was cancelled.
Unattached, it does nothing, matching the ungated claim.
