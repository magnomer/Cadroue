using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LKeyframeEngine
{
    public static Func<string, TimeSpan, TimeSpan, CancellationToken, Task<IReadOnlyList<LKeyframeEntry>>>
        LKeyframeScanSeam { get; set; } =
        (_, _, _, _) => Task.FromResult<IReadOnlyList<LKeyframeEntry>>(Array.Empty<LKeyframeEntry>());
}
