namespace Cadroue.Core;

public sealed record LRigMedia(
    Func<string, CancellationToken, int?, Task<LMediaInfo>> LRigMediaProbe,
    Func<string, CancellationToken, Task<double?>> LRigMediaLoudness,
    Func<string, TimeSpan, TimeSpan, CancellationToken, Task<IReadOnlyList<LKeyframeEntry>>> LRigMediaKeyframe,
    Func<CancellationToken, Task> LRigMediaAdmission,
    Action LRigMediaDischarge);
