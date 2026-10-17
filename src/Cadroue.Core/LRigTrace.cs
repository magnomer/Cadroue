namespace Cadroue.Core;

public sealed record LRigTrace(
    Action<string> LRigTraceInfo,
    Action<string, string?> LRigTraceWarning,
    Action<string, Exception?> LRigTraceError,
    Action<string, string?> LRigTraceFfmpeg,
    Func<bool> LRigTraceVerbose,
    Action<bool, string, string?> LRigTraceDetail,
    Action<string, TimeSpan, string?, string, double, int> LRigTraceTimeline);
