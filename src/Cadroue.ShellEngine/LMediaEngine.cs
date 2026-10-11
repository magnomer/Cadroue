using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LMediaEngine
{
    public static Func<string, CancellationToken, int?, Task<LMediaInfo>> LMediaProbeSeam { get; set; } =
        (_, _, _) => Task.FromException<LMediaInfo>(new InvalidOperationException("No media reader is attached."));

    public static Func<string, CancellationToken, Task<double?>> LMediaLoudnessSeam { get; set; } =
        (_, _) => Task.FromResult<double?>(null);
}
